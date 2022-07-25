# Putting it all together - the Generator

The generator builds on your work creating and testing [the initial extraction](initial-extraction.md), [additional transformation](further-transformations.md), and [code outputting](output-code.md) based on a [good understanding of what you are trying to build](design-output.md) and the [input you are using](design-input-data.md).

This generator creates outputs three files and an additional file per each command. This generation is part of the example used throughout this walk-through.

## Initial extraction

Initial extractions use one of the [incremental source generation providers discussed in the Pipelines article](../pipeline.md#providers). Each provider is an `IncrementalValueProvider` or an `IncrementalValuesProvider` (they differ by whether `Value` is plural). They are defined in the `Intialize` method, which his the only member of the `IGenerator` interface. You can define more than one incremental value provider for your generator. Incremental value providers behave similarly to LINQ - they allow you to define operations that will be executed later and if the results are not used, they do execute.

 The [pipeline article](./pipeline.md#providers) covers performance aspects of the different inputs. The `SyntaxProvider.ForAttributeWithMetadataName` is strongly recommended starting with Visual Studio 17.3, so it will be the one covered here. Incremental generators are highly tuned for the performance of of this approach, including an index (dictionary) containing the attribute usage in  applications using your generator.

An example of `ForAttributeWithMetadataName` usage is:

```csharp
var classModelValues = initContext.SyntaxProvider
    .ForAttributeWithMetadataName(
        fullyQualifiedMetadataName: "IncrementalGeneratorSamples.Runtime.CommandAttribute",
        predicate: (_, _1) => true,
        transform: GetModelFromAttribute);

```

`_1` in the predicate is a discard because the latest discard syntax is not present in C# 7.3. The predicate returns true because attribute filtering is sufficient.

`GetModelFromAttribute` is a method. Using the name without parentheses provides a delegate that has the expected signature for the transform: a `GeneratorAttributeSyntaxContext` and a `CancellationToken`. `GeneratorAttributeSyntaxContext` provides the target syntax node of the attribute, it's associated symbol, the semantic model, and `AttributeData` for all of the attributes for the syntax node.

When `GetModelFromAttribute` is later run as part of generation, it returns a class with the minimal data needed for further generation. It is essential that the return value of all initial extraction steps be simple, have value equality, and not retain portions of the syntax tree or semantic model, because they do not have value equality and tightly coupled across the entire syntax tree or semantic model.

The type of `classModelValues` is `IncmrentalValuesProvider<ClassModel>` where `ClassModel` is the type returned from `GetModelFromAttribute`.

You can find out more in the [Initial extraction article](initial-extractions.md).

## Further transformations

Further extractions allow you to shape your data for easy outputting of code. The generation of code can become difficult to follow if you are doing calculations and conditions that could have been precalculated. Because of caching, generation steps may be skipped if the result of the transformation is the unchanged. In this set of transformations, the first removes nulls, the second prepares individual generation, and the third creates a summary:

```c#
classModelValues = classModelValues.Where(classModel => !(classModel is null));

var commandModelValues = classModelValues
    .Select(ModelBuilder.GetCommandModel);

var rootCommandValue = commandModelValues
    .Collect()
    .Select(ModelBuilder.GetRootCommandModel);
```

The delegate passed to `Select` must accept an instance of the contained type and a cancellation token. The incremental values provider `classModelValues` contains `ClassModel` instances, `commandModelValues` contains `CommandModel` instances. `rootCommandValue` (used later) is an incremental value (singular) provider that contains a `RootCommandModel`. You can see these models in the [Create models article](create-models.md)

The signature for `Select` is the type of the type of the `IncrementalValuesProvider`, in this case, `ClassModel`.

It is not initially intuitive that more steps create a faster generator. In this case, the root command generation requires a single element because it creates a single file. `Collect` provides this element which is `ImmutableArray<CommandModel>`. Generating the two summary files needs only three  pieces of information from each command in this array: that there is at least one command, the namespace for the set and the name of each command. Extracting this data from the array of `CommandModel` means outputting these files can be skipped if some other aspects of the command changed, like an option was added or removed. `null` is used to represent no commands being present.

You can find out more in the [Further transformations article](further-transformations.md).

## Outputting code

There are three ways to output code - all of which are defined using methods on the `IncrementalGeneratorInitializationContext`. You can find more about these methods in the [Pipeline article](..\pipeline.md#output).

In most cases, file that needs to be available whenever the generator is referenced can be placed into a runtime library and generation is not needed. However, it will need to be generated if it is a partial class that pairs with a file that needs to be generated. The `RegisterPostInitializationOutput` method specifies code that should always be part created and does depend on any input values and therefore will not change during the lifetime of the generator. Code that is output in this way is added to the compilation, so generation does not produce errors that it is missing. The same is true of code in the runtime library.

An example is:

```csharp
initContext.RegisterPostInitializationOutput((postinitContext) =>
            postinitContext.AddSource("Cli.g.cs", CodeOutput.ConsistentCli));
``` 

The values passed to `AddSource` are the file name, and the code to output.

Files that are created during generation may or may not change the user's editing experience. An example of code that changes the users experience is a class that contains properties the user may access. The user will want IntelliSense and will not want unnecessary squiggles. An example of code that does not change the user experience is code that includes a partial method with an implementation that is only used at runtime, and optimizing code if it does not affect the user's experience. Code that does not affect the user's experience should be output using `RegisterImplementationSourceOutput`.`RegisterImplementationSourceOutput` has the same signature and behavior as `RegisterSourceOutput` discussed below. The only difference is whether generation runs during design time builds. 

> [!NOTE]
> The implementation of this feature in the generator has been delayed so the code is output regardless of whether this method is used. It should still be called where appropriate so it shortcuts immediately and with no effort on your part in an upcoming release of Visual Studio.

If code does affect the user's experience, define the output using `RegisterSourceOutput`. This method takes an `IncrementalValueProvider` or an `IncrementalValuesProvider` and a delegate that defines the code to output. The signature of this method is:
 
```csharp
public void RegisterSourceOutput<TSource>(
    IncrementalValueProvider<TSource> source, 
    Action<SourceProductionContext, TSource> action) 
```

The delegate passed is an action and thus, does not return a value. Code is output using the `SourceProductionContext.AddSource` method which takes a file name and the source code to output as parameters.

## Example

This example builds on the design of the [Further transformation article](further-transformations.md#example). You can see how this code is used in [Putting it all together](putting-it-all-together.md#example).

If you're generator uses methods that you have designed and unit tested in isolation, pulling them together in the generator becomes much more straightforward:

```csharp
[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // Initial extraction - there may be multiple for some generators
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

        // Output code on einput could produce several outputs, and the reverse
        initContext.RegisterSourceOutput(
            rootCommandValue,
            (outputContext, rootModel) =>
                outputContext.AddSource("Cli.Partial.g.cs",
                        CodeOutput.PartialCli(rootModel, outputContext.CancellationToken)));

        initContext.RegisterSourceOutput(
            commandModelValues,
            (outputContext, model) =>
                    outputContext.AddSource(CodeOutput.FileName(model),
                        CodeOutput.GenerateCommandCode(model, outputContext.CancellationToken)));

        initContext.RegisterSourceOutput(
            rootCommandValue,
            (outputContext, rootModel) =>
                    outputContext.AddSource("Root.g.cs",
                        CodeOutput.GenerateRootCommandCode(rootModel, outputContext.CancellationToken)));

    }

    private static InitialClassModel GetModelFromAttribute(GeneratorAttributeSyntaxContext generatorContext,
                                CancellationToken cancellationToken)
    => ModelBuilder.GetInitialModel(generatorContext.TargetSymbol, cancellationToken);
}
```

Comments in the source code identify how the [initial extraction](initial-extraction.md), the [further transformations](further-tranformations.md) and the [code output steps](output-code.md) fit together.

The `GetModelFromAttribute` isolates the extraction implementation from source generator types. As shown in the section on the [initial extraction article on testing](initial-extraction.md#testing-the-example), you can create a symbol for testing. You cannot create an instance of `GeneratorAttributeSyntaxContext`. Your version of this method may use the `TargetNode` syntax token, the semantic model or the other attributes on the node, which are also supplied via the `GeneratorAttributeSyntaxContext`.

## Testing the example

Testing generation as a whole requires running the generator. There are a number of supporting methods that expose more details of compilation and generation that are generally needed to successfully write generators. Since these methods can seem complicated they are covered in [Integration testing](integration-testing.md). 

Using these helper methods to test the example looks similar to other tests:

[[ Delaying further work on integration testing until the rest gets out.]]


