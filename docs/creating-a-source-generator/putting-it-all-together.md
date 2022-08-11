# Putting it all together - the Generator

Creating and testing [the initial extraction](initial-extraction.md), [additional transformation](further-transformations.md), and [code outputting](output-code.md) based on a [good understanding of what you are trying to build](design-output.md) and the [input you are using](design-input-data.md) make creating the generator code straightforward.

The general form is:

```csharp
using IncrementalGeneratorSamples.InternalModels;  // The namespace of your models
using Microsoft.CodeAnalysis;
using System.Threading; // For the CancellationToken

namespace IncrementalGeneratorSamples
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // Initial extraction definition - creates models that have value equality

            // Further transformation definition

            // Output code definition for code without dependencies on input

            // Output code definition that depends on input
 
        }
    }
}
```

The `Initialize` method does no work when it runs - it just collects the definitions that make up the pipeline. The generation infrastructure executes this pipeline as needed. Incremental value providers behave similarly to LINQ - they allow you to define operations that will be executed later and if the results are not used, they do not execute.

## Initial extraction

Initial extractions use one of the [incremental source generation providers discussed in the Pipelines article](../pipeline.md#providers).  Each provider returns an `IncrementalValueProvider` or an `IncrementalValuesProvider` that is passed on to the next step. You can define more than one initial extraction step for your generator each of which uses a different provider. 

 The [pipeline article](./pipeline.md#providers) covers performance aspects of the different inputs. The `SyntaxProvider.ForAttributeWithMetadataName` is strongly recommended starting with Visual Studio 17.3, so it will be the one covered here. Incremental generators are highly tuned for the performance of of this approach, including an index (dictionary) containing the attribute usage in  applications using your generator.

An example of `ForAttributeWithMetadataName` usage is:

```csharp
var classModelValues = initContext.SyntaxProvider
    .ForAttributeWithMetadataName(
        fullyQualifiedMetadataName: "IncrementalGeneratorSamples.Runtime.CommandAttribute",
        predicate: (_, _1) => true,
        transform: GetModelFromAttribute);

```

`_1` in the predicate is a discard because the latest discard syntax is not present in C# 7.3. The predicate returns true because attribute filtering is sufficient. If you do not pass a predicate, attributed nodes will not be found. 

`GetModelFromAttribute` is a method. Using the name without parentheses provides a delegate that has the expected signature for the transform: a `GeneratorAttributeSyntaxContext` and a `CancellationToken`. `GeneratorAttributeSyntaxContext` provides the target syntax node decorated with the attribute, it's associated symbol, the semantic model, and `AttributeData` for all of the attributes for the syntax node.

When `GetModelFromAttribute` is runs later as part of generation, it returns an instance of a class that contains the minimal data needed for further generation. It is essential that the return value of all initial extraction steps be simple, have value equality, and not retain portions of the syntax tree or semantic model, because they do not have value equality and tightly coupled across the entire syntax tree or semantic model.

> [!TIP]
> There are aspects of the syntax nodes that have value equality and can be passed on, however, they are larger than necessary and it is easier to remove all attributes during the initial extraction than to remember which aspects are safe, and which are not.

The type of the variable `classModelValues` is `IncrementalValuesProvider<ClassModel>` where `ClassModel` is the type returned from `GetModelFromAttribute`.

You can find out more in the [Initial extraction article](initial-extractions.md).

## Further transformations

Further extractions allow you to shape your data for easy outputting of code. The generation of code can become difficult to follow if you are doing calculations and conditions that could have been precalculated. Because of caching, generation steps may be skipped if the result of the transformation is the unchanged. In this set of transformations, the first removes nulls, the second transforms each `ClassModel` to a `CommandModel`that is friendly for generation, and the third creates an `IncrementalValueProvider` with a collection of all the classes:

```c#
classModelValues = classModelValues
    .Where(classModel => !(classModel is null));

var commandModelValues = classModelValues
    .Select(ModelBuilder.GetCommandModel);

var rootCommandValue = commandModelValues
    .Collect()
    .Select(ModelBuilder.GetRootCommandModel);
```

The delegate passed to `Select` accepts an instance of the `IncrementalValuesProvider` type and a cancellation token. The incremental values provider `classModelValues` contains `ClassModel` instances, `commandModelValues` contains `CommandModel` instances. `rootCommandValue` (used later) is an `IncrementalValueProvider` (singular) that contains a `RootCommandModel`. You can see these models in the [Create models article](create-models.md) and the `ModelBuilder.GetRootCommandModel` method in [Initial extraction](initial-extraction.md).

The signature for `Select` is the type of the type of the `IncrementalValuesProvider`, in this case, `ClassModel`.

The root command generation requires a single element because it creates a single file. `Collect` provides this element which is `ImmutableArray<CommandModel>`. Generating the two summary files needs only three  pieces of information from each command in this array: that there is at least one command, the namespace for the set and the name of each command. Extracting this data from the array of `CommandModel` means outputting these files can be skipped if other aspects of the command changed, like an option was added or removed. `null` is used to represent no commands being present.

Early processing like this contributes to a faster generator, although it is not initially intuitive that more steps create a faster generator.

You can find out more in the [Further transformations article](further-transformations.md).

## Outputting code

There are three ways to output code - all of which are defined using methods on the `IncrementalGeneratorInitializationContext`. You can find more about these methods in the [Pipeline article](..\pipeline.md#output).

### Output constant code

In most cases, file that needs to be available whenever the generator is referenced can be placed into a runtime library and generation is not needed. However, a class will need to be generated if it is a partial class that pairs with a file that needs to be generated. The `RegisterPostInitializationOutput` method specifies code that should always be created and does depend on any input values and therefore will not change during the lifetime of the generator. Code that is output in this way is added to the input compilation prior to generation.

An example is:

```csharp
initContext.RegisterPostInitializationOutput((postinitContext) =>
            postinitContext.AddSource("Cli.g.cs", CodeOutput.ConsistentCli));
```

The values passed to `AddSource` are the file name, and the text of code to output.

### Output code that depends on input

Files that are created during generation may or may not change the user's editing experience. An example of code that changes the users experience is a class that contains properties the user may access. The user will want IntelliSense and will not want unnecessary squiggles. An example of code that does not change the user experience is code that includes a partial method with an implementation that is only used at runtime, and optimizing code if it does not affect the user's experience. Code that does not affect the user's experience should be output using `RegisterImplementationSourceOutput`.`RegisterImplementationSourceOutput` has the same signature and behavior as `RegisterSourceOutput` discussed below. The only difference is whether generation runs during design time builds.

> [!NOTE]
> The implementation of this feature in the generator has been delayed so the code is currently output regardless of whether this method is used. It should still be called where appropriate so it shortcuts immediately and with no effort on your part in an upcoming release of Visual Studio.

If code does affect the user's experience, define the output using `RegisterSourceOutput`. This method takes an `IncrementalValueProvider` or an `IncrementalValuesProvider` and a delegate that defines the code to output. The `IncrementalValueProvider` overload produces one file. The `IncrementalValuesProvider` overload produces one file per item. The signature of this method is:

```csharp
public void RegisterSourceOutput<TSource>(
    IncrementalValueProvider<TSource> source, 
    Action<SourceProductionContext, TSource> action) 
```

The delegate passed is an action and thus, does not return a value. Code is output using the `SourceProductionContext.AddSource` method which takes a file name and the source code to output as parameters.

> [!TIP]
> While multiple classes can be put into one file, users of your generator will probably be most comfortable if you produce a file per class. When you output a file per item, create the file name based on data in the model.

## Example

This example builds on [Initial extraction](initial-extraction.md#example), [Further transformations](further-tranformations.md#example) and [Output code](output-code.md#example). You can see how to test this code in [Integration testing](integration-testing.md#example).

If your generator uses methods that you have designed and unit tested in isolation, pulling them together in the generator becomes much more straightforward:

```csharp
[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
            // Initial extraction - creates models that have value equality
            var classModelValues = initContext.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: "IncrementalGeneratorSamples.Runtime.CommandAttribute",
                    predicate: (_, _1) => true,
                    transform: GetModelFromAttribute);

            // Further transformations
            classModelValues = classModelValues.Where(classModel => !(classModel is null));

            var commandModelValues = classModelValues
                .Select( ModelBuilder.GetCommandModel);

            var rootCommandValue = commandModelValues
                .Collect()
                .Select(ModelBuilder.GetRootCommandModel);

            // Output code that does not depend on input and is added prior to compilation
            initContext.RegisterPostInitializationOutput((postinitContext) =>
                postinitContext.AddSource("Cli.g.cs", CodeOutput.ConsistentCli));

            // Output code that depends on input
            initContext.RegisterSourceOutput(
                rootCommandValue,
                (outputContext, rootModel) =>
                    outputContext.AddSource("Cli.Partial.g.cs",
                            CodeOutput.PartialCli(rootModel, outputContext.CancellationToken)));

            initContext.RegisterSourceOutput(
                commandModelValues,
                (outputContext, model) =>
                        outputContext.AddSource(CodeOutput.FileName(model),
                             CodeOutput.CommandCode(model, outputContext.CancellationToken)));
    }

    private static InitialClassModel GetModelFromAttribute(GeneratorAttributeSyntaxContext generatorContext,
                                CancellationToken cancellationToken)
    => ModelBuilder.GetInitialModel(generatorContext.TargetSymbol, cancellationToken);
}
```

Comments in the source code identify how the [initial extraction](initial-extraction.md), the [further transformations](further-tranformations.md) and the [code output steps](output-code.md) fit together.

## Testing the example

Testing generation as a whole requires running the generator. There are a number of supporting methods that expose more details of compilation and generation that are generally needed to successfully write generators. Since these methods can seem complicated they are covered in [Integration testing](integration-testing.md). 

Next Step: [Integration testing](integration-testing.md)
