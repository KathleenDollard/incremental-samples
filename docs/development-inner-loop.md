# Developing incremental source generators

 The process of writing source generators is complicated by the fact you are writing code to create code, which increases the number of abstractions to consider and the ways that you need to test. Roslyn generators further complicate the picture because you are writing code that will become part of the current compilation and they are deployed as NuGet packages. Dependencies on NuGet packages that you are actively developing is challenging. And finally, you need to ensure your generator is fast.

Splitting generator creation into discreet steps lets you focus on each step:

* [Structure the solution and project layout.](#structure-the-solution-and-project-layout)
* [Create at least one sample project.](#create-at-least-one-example-project)
* [Design a data model and check it for completeness.](#design-a-data-model-and-check-it-for-completeness)
* [Create and test the code that builds the data model.](#create-and-test-the-code-that-builds-the-data-model)
  * Create and fill the initial data model.
  * Combining, collection and transforming data models.
  * Test data model creation.
* [Create the code that outputs the generated code and test it.](#create-the-code-that-outputs-the-generated-code)
* [Create the generator and test end to end.](#create-the-generator-and-test-end-to-end)
* [Test performance.](#test-performance)
* [Create a NuGet package.](#create-a-nuget-package)

You can use these steps to build a solution that has a good development inner loop for your generator - meaning you can easily make changes, see the impact of those changes on generation, and test. This article covers what's important in each of these steps. The [Roslyn incremental generator tutorial](tutorial.md) creates a sample project following the steps that are outlined here. 

Before you get started, check the [limitations of Roslyn incremental source generators in the Overview](overview.md#limitations-of-generators). It will also be helpful to read the about the [Roslyn incremental generators pipeline](pipeline.md).

## The incremental generator

Before diving into the end to end details of building a generator, its helpful to understand how the generator initialization method structures the generator. The initialization method is the only method of the IIncrementalGenerator interface and defines a [pipeline that will run during generation](pipeline.md). As an example:

```csharp
using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {

        var commandModelValues = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: ModelBuilder.IsSyntaxInteresting,
                transform: ModelBuilder.GetModel)
            .Where(static m => m is not null)!;

        var rootCommandValue = commandModelValues.Collect();

        initContext.RegisterPostInitializationOutput((postInitContext) =>
            postInitContext.AddSource("Cli.g.cs", CodeOutput.AlwaysCli));

        initContext.RegisterSourceOutput(
           rootCommandValue,
           static (outputContext, modelData) =>
                   outputContext.AddSource("Cli.Partial.g.cs",
                        CodeOutput.PartialCli(
                            modelData, outputContext.CancellationToken)));

        initContext.RegisterSourceOutput(
            commandModelValues,
            static (outputContext, modelData) =>
                    outputContext.AddSource(CodeOutput.FileName(modelData),
                        CodeOutput.GenerateCommandCode(
                            modelData, outputContext.CancellationToken)));
    }
}
```

The first statement of this method calls `CreateSyntaxProvider` passing two delegates, which are the `IsSyntaxInteresting` and `GetModel` method names without parentheses. `IsSyntaxInteresting` is a predicate that filters the syntax nodes, and `GetModel` builds a cacheable data model for each syntax node. This is used to output a file for each of the data models.

Next, the `Collect` method creates a single item which is a collection of data models. This is used to create the single output file `Cli.Partial.g.cs`.

The last three statements output new source code into the user's compilation. The `RegisterPostInitializationOutput` immediately after initialization has run. It takes no inputs, and so cannot refer to any source code written by the user, or any other compiler inputs. In this case it outputs part of a partial class that is always available in the calling code.

The first `RegisterSourceOutput` outputs the other portion of this partial class.

The second `RegisterSourceOutput` outputs a syntax tree and file per data model.

The initialization method sets up a pipeline which the generator infrastructure uses for generation.

## Structure the solution and project layout

The project layout of your solution needs to manage test and production concerns, generation-time and runtime concerns, and NuGet package references used to deploy generators.

Plan for a solution that is at least 4 or 5 projects:

* The *example project*.
* The project containing the *runtime library* - optional but may include attributes and base classes.
* The *incremental generator*.
* The *unit test project*.
* An *integration test project*.

You may need additional projects to organize code sharing between projects. 

The dependencies of these projects are:

| Project               | Dependencies         |
|-----------------------|----------------------|
| Example               | Runtime              |
| Runtime library       |                      |
| Incremental generator | Runtime (project ref in development, package ref in release)|
| Unit test             | Generator            |
| Integration test      | Unit test, generator |

All of these dependencies are normal project references, except the dependency of the incremental generator on the runtime library.

The runtime library is separate from the generator because it includes code that is needed at both compile time and runtime. The generator depends on the runtime library and this will be a NuGet package reference when the generator is deployed. However, this package reference is challenging to manage during inner-loop development, so you can treat it as a project reference during development and package reference for deployment using a conditional MSBuild property:

```xml
<ItemGroup Condition="'$(CreatePackage)' == 'true'">
    <PackageReference Include="IncrementalGeneratorSamples.Runtime" Version="0.0.1-alpha" />
</ItemGroup>

<ItemGroup Condition="'$(CreatePackage)' != 'true'">
    <ProjectReference Include="../IncrementalGeneratorSamples.Runtime/IncrementalGeneratorSamples.Runtime.csproj" />
</ItemGroup>
```

## Create at least one example project

The important first step of writing a source generator is to create a working example that shows exactly what you want to output. This example also shows how your generated code will behave in the context of a working project.

Developers often get to a level of understanding of a problem and begin writing code. If you have that level of understanding and begin to write the code of your generator, rather than your example project, you will work out the details in a relatively slow inner loop. If you create an example project you can ensure it works correctly before working on the generator. Later, you can copy parts of it as the starting point for outputting code and use it as the basis for defining input. The details will already be worked out.

Your example project is just a normal .NET project. You might create it via the .NET templates in the .NET CLI or an editor like Visual Studio, or you might use a sample project that you've already created. If it is an executable, you can manually test it or you can write unit tests. If it is a library, you'll need unit tests. If you write unit tests for the example project, you can use them as the basis for integration tests.

Within this example project isolate the code you plan to generate into separate files, and isolate these files in a subdirectory. This will:

* Let you design the interaction between generated classes and their base classes and any[partial classes and methods](https://docs.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods).
* Provide the basis for designing how developers communicate with your generator to provide input (attributes, interfaces, well known names, etc.)
* Be available to copy as a starting point for outputting code.
* Provide the context for end to end testing - especially being able to compile your generated code in context.
* (Optional) Provide examples for unit tests to ensure your generated code runs correctly in the context of a project.
* Provide an example of how your generator works to use as part of your documentation.

Later this article suggests copying the directory that contains the code you will generate and overwriting the original. Because of this, you might name this subdirectory something like "OverwrittenInTests".

Ensuring your example project runs and does what is expected before you begin will save time as you create your generator.

## Design a data model and check it for completeness.

Your generator will gather input data from sources like the user's code or external files. Regardless of the source, an explicit data model allows you to extract information from the sources into a cacheable model.

Incremental generators rely on caching, caching relies on value equality, and your data models can supply that value equality. You might only use one model, and it might contain only one value, or you may have several models that you transform during development. The key is that you extract the data you need from the underlying source in a discreet operation as early in the pipeline processing as possible. This discrete operation will be created either by calling `SyntaxValueProvider.CreateSyntaxProvider` or `Select` on any of the other providers of `IncrementalGeneratorInitializationContext`. The [provider section or the incremental source generator pipeline article](pipeline.md#providers) and the [incremental generator specification](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md) have more information on providers.

Each data model must have value equality, including using `SequenceEquals` for any collections. This is most easily done using records or record structs and customizing the equality to accommodate collections or any other data that doesn't naturally have value equality.

Although the data model is generally built from code and is used to create code, it should be in the language of your domain. For example, if the domain is a command line definition, like System.CommandLine, the domain would include commands, arguments and options. 

If the domain is code itself, where there is no logical alternative but your data model names aligning with code features like properties and classes, you probably need to clarify whether the data model members align with the input or output code. You might do this with `input` and `output` prefixes.

Once you have an example project and a data model, map the generated code to the data model. This will ensure the data model contains all of the variable information. If user-entered source code will provide the input for the data model, also map the data model to the user-entered source code in the example project to understand where each value originates. You may need to update the example project to better communicate the generation input. If the input data comes from another source, such as an additional file or configuration, map the data model to that source. These mappings can be simple bulleted lists.

## Create and test the code that builds the data model

Some generators have a single source for the data model used for generation, and no further transformations are needed. Other generators have a transformation pipeline that combines data from multiple sources and generates numerous files from different data models. For complex generators, extra effort at planning transformations may be needed.

Regardless of the number of sources you need, first extract each one independently into an initial model that supports value equality.



### Create the initial data model

Unless the data model is so simple you can use a single value or a small tuple, you will need a data model. You can use records, which provides value equality unless your record contains a collection or other members whose default equality is reference equality.

Generally your data model is a couple of records and the path from input to 

#### Extracting and testing a data model based on an attribute in C# or VB code

[[ Notes on Cyrus's tool]]

#### Extracting and testing an initial data model from C# or VB code

If the input comes from source code, use the `IncrementalGeneratorInitializationContext.SyntaxValueProvider.CreateSyntaxProvider` method to create an `IncrementalValuesProvider` specific to your needs. To use this you need two delegates: the predicate and the transform. The simplest way to make these is to use methods. When using methods as a delegate, you can just omit the parentheses.

The first of these delegates is a predicate that takes a `SyntaxNode` and returns a Boolean. This method should be very fast. For example, pattern matching the `SyntaxNode` against a syntax type like `ClassDeclarationSyntax` is very fast. Once you make that first cut at filtering syntax nodes, you can further refine checking for specific attributes or well known names. The goal of the predicate is to *exclude* as many syntax nodes as possible from further consideration. If you need the semantic model to complete filtering, you will need to return true and filter further in the transform.

The transform is called for each syntax that passes the predicate. It takes a `GeneratorSyntaxContext` which provides the syntax node and the `SemanticModel`. You can use the semantic model's `GetSymbol` method to retrieve the corresponding `ISymbol` or the `GetOperation ` method to retrieve an `IOperation`. These give you more information that you can use to build your data model. 

When the extra information leads you to discard the node from further consideration, return `null` and use `Where` to filter out these entries. This may happen at unexpected times because the code is complete. For example, if you pattern match against a symbol that is not currently resolved, you will get a symbol representing an error instead of the expected symbol. Your generator will be called more often with invalid code than code that successfully compiles, so graciously manage this, generally by returning null and skipping generation.

Both of these methods are passed a `CancellationToken`. Ideally, all work in the predicate is very fast and in that case, the cancellation token can be discarded. The `CancellationToken` should always be passed to the methods of the semantic model that have a `CancellationToken` parameter, and should be checked before and after any slow operations and within any unbounded loops.

An example of a syntax provider is:

```csharp
var commandModelValues = initContext.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: ModelBuilder.IsSyntaxInteresting,
        transform: ModelBuilder.GetModel)
    .Where(static m => m is not null)!;
```

#### Extracting and testing an initial data model from other source

It is straightforward to use the providers on `IncrementalGeneratorInitializationContext`. You can find out more about the [available providers](pipeline.md#pipeline-operations). 

The all work similarly to the `AdditionalTextsProvider`. The provider supplies an `IncrementalValuesProvider<AdditionalText>` object with a member for each file in the project that is not part of the compilation. `AdditionalText` is not itself cacheable. You can use the LINQ like methods on `IncrementalValuesProvider` to filter and transform the provided data into a cacheable form:  

```c#
// get the contents of files that end with .txt
IncrementalValuesProvider<(string fileName, string content)> textFiles = 
    context.AdditionalTextsProvider
        .Where(static f => f.Path.EndsWith(".txt"))
        .Select((additionalText, cancellationToken) => 
            (fileName: Path.GetFileNameWithoutExtension(additionalText.Path), 
             content: additionalText.GetText(cancellationToken)!.ToString()));
```

Note that the `Select` method of `IncrementalValuesProvider` supplies a `CancellationToken`. Where a cancellation token is available, be sure to pass it on to any you methods that have a `CancellationToken` parameter.

This is a case where [considering the order of operations can improve performance](performance-guidelines.md#consider-operation-order). the `AdditionalText` objects returned from the provider are not cacheable. The result of the `Where` clause is also `AdditionalText`and not cacheable. The result of the `Select` is cacheable, and thus it would be possible to provide a cacheable result a little earlier in the pipeline. However, opening each file is expensive, so performance is better if the available files filtered prior to `GetText`.

### Combining, collection and transforming data models

You may have more than one initial data model, you may need to coalesce the members of an `IncrementalValuesProvider` into a single member, or you may need to transform models in some other way. [The available `IncrementalValuesProvider` methods are listed in the pipelines article](pipeline.md#pipeline-operations) and the [Roslyn incremental source generator specification](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md).

`Select`, `SelectMany` and `Where` work very similar to the way they work in LINQ, with the addition that `Select` and `SelectMany` take a cancellation token. By implication, if you need to include a cancellable method in filtering, first use `Select` to gather the data, and then filter.

#### Collect

`Collect` allows you to create a single item that is a collection of the members. The signature of this method clarifies that it takes a `..Values..` and returns a `..Value..` provider that contains an `ImmutableArray`:

```csharp
IncrementalValueProvider<ImmutableArray<TSource>> Collect<TSource>(this IncrementalValuesProvider<TSource> source);
```

An example of when you may need to do this is calling `RegisterSourceOutput`. Calling this with a `..Values..` provider results in a new syntax tree per member. You might want this if you were creating a new class per item.Calling it with the collection in a `..Value..` provider results in one new syntax tree for the collection. You would need this if you were creating a new property per item.

#### Combine

`Combine` is the most powerful and complicated of the transforming methods. There are three overloads that differ by when `..Value..` and `..Values..` is used:

```csharp
IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(
    this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2);
IncrementalValueProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(
    this IncrementalValueProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2);
IncrementalValuesProvider<(TLeft Left, TRight Right)> Combine<TLeft, TRight>(
    this IncrementalValuesProvider<TLeft> provider1, IncrementalValueProvider<TRight> provider2);
```

Note that the return value matches the first parameter type - whether it is `..Value..` or `..Values..`. The generic type of the resulting provider is a tuple.

Combining two `..Value..` providers results in a `..Value..` provider with a tuple of the left and right values. You would use this if you had a `..Value..` provider that contained collection of data models extracted from syntax, and a second `..Value..` provider that contained a data model with details extracted from the compilation.

Combining a `..Value..` provider and a `..Values..` provider results in a `..Value..` provider with a tuple of the left value and an `ImmutableArray` of the right values. You would use this if you had a `..Value..` provider  that contained a data model with details extracted from the compilation, and a `..Values ..` provider that contained data models extracted from individual syntax and you wanted a single result. For example, you would use this if the left model held information needed to create a class and the right model contained information for creating properties.

Combining a `..Values..` provider and a `..Value..` provider results in a `..Values..` provider where each member is a tuple of one of the left values and the right value. You would use this if you had a `..Values ..` provider that contained data models extracted from individual syntax and a `..Value..` provider that contained a data model with details extracted from the compilation, and you wanted a multiple individual results. For example, you would use this if the left model held information like a namespace that was needed to generate a set of classes and the right model contained information for each, and you wanted each class to be in a separate syntax tree or file.

`Combine` does not provide a `..Values` to  `Values` overload by design because it would result in a cross product. If you need to join two collections, you can use `Collect` to create two `..Value..` providers, use `Combine` to create a single `..Value..` provider, and then use `Select` or `SelectMany` to manipulate the collections.

The [Roslyn incremental source generator specification](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md) has diagrams illustrating the different `Combine` overloads.

### Testing data model creation

It is often sufficient to test the individual steps, particularly non-trivial `Select`, `SelectMany`, and `Where` clauses. This is most easily done by using methods as delegates as the arguments. You do this by omitting the parentheses. You can then create unit tests that exercise these methods and ensure you get the right results.

When needed you can test the data model via a test-only generator that generates output serialized form into a C# class. An example of when you would want to test your model via a test-only generator if there is a complicated set of `Collect` and `Combine` steps. Creating a test-only generator is the same as [creating and testing a normal generator](), except the location of the generator and that you output a serialized data model. See the [tutorial](tutorial.md#test-data-model-creation) for an example.

## Create the code that outputs the generated code

The `Register..` methods of the `IncrementalGeneratorInitializationContext` output source code.

`RegisterPostInitializationOutput` takes a delegate that has a `IncrementalGeneratorPostInitializationContext` parameter. This struct has a `CancellationToken` property and `AddSource` overloads for strings and `SourceText`. When you use this method to output something quite simple, like a literal string, you can ignore the cancellation token.

`RegisterSourceOutput` has overloads for `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>`. Each of these overloads take a delegate that has a `SourceProductionContext` parameter. This struct has a `CancellationToken` property and `AddSource` overloads for strings and `SourceText`. It also includes a `ReportDiagnostic` method.

[[ @chsienki When would you want to use SourceText? ]]

### Selecting the output method

There are three methods that output source code from your generated. These methods are:

* `RegisterSourceOutput` is the most common way to output source code.
* `RegisterImplementationSourceOutput` is the same as `RegisterSourceOutput`, but also indicates that there is no design time impact.
* `RegisterPostInitializationOutput` is used to output source code prior to the generator being run and that cannot depend on any aspect of generation.

You cannot output files other than source code.

`RegisterSourceOutput` and `RegisterImplementationSourceOutput` have a parameters for your data model and a cancellation token. `RegisterPostInitializationOutput` has only an `IncrementalGeneratorInitializationContext` parameter.

`RegisterPostInitializationOutput` runs prior to generation, immediately after initialization and is included in the compilation used for the remainder of generation. You can use this method to add code that are important to generators. For example, you can use `RegisterPostInitializationOutput` to output a static portion of a partial class which you always want available in your code. Attributes can be provided to users either through `RegisterPostInitializationOutput` or through a traditional runtime library.

### Output techniques

Once you know which method to use, there are a few approaches to creating generated code. If the individual files of your output will be large or be created with a large number of small chunks, use a `StringBuilder`. In many cases, your files will be small enough you can use string interpolation. The `Append` and `AppendLine` methods of `StringBuilder` let you add individual lines of code. You may find syntax details like semi-colons and curly brackets to be tedious. Indentation can be challenging, although that only affects the user's ability to read your code, not the way it compiles. String interpolation generally avoids these problems.

To use string interpolation, create a method that accepts your data model and a `CancellationToken` for each file pattern you plan to create. For each one, create a string variable and copy the contents of the corresponding file from your example project. If you copy code into a verbatim interpolated string in a recent version of Visual Studio, it will do the necessary doubling of the curly brackets and double quotes. Starting in Visual Studio 17.4 and .NET Core SDK 7.0.100, you can use raw string literals. For example,

```csharp
var x = $@"
using System;

#nullable enable

namespace TestExample
{{
    public partial class AddLine
    {{
        Option<System.IO.FileInfo?> fileOption = 
            new Option<System.IO.FileInfo?>(""--file"", 
                ""The file to read and display on the console."");
        Option<string> lineOption = 
            new Option<string>(""--line"", 
                ""Delay between lines."");

// rest of class skipped for readability
";
```

The variable parts of this code is the class name and the options. Access properties of your data model  of these with methods that return the required contents and write methods that return the appropriate strings:

```csharp
var x = $@"
using System;

#nullable enable

namespace TestExample
{{
    public partial class AddLine
    {{
        Option<System.IO.FileInfo?> fileOption = 
            new Option<System.IO.FileInfo?>(""--file"", 
                ""The file to read and display on the console."");
        Option<string> lineOption = 
            new Option<string>(""--line"", 
                ""Delay between lines."");

// rest of class skipped for readability
";
```

The interpolated string calls the `OptionFields` method to generate fields. This shows how you can handle loops in the generation pattern. It also shows how you can use methods to provide looping without making it difficult to read. The method for creating options might be:

```csharp
static string OptionFields(IEnumerable<OptionModel> options)
    => string.Join("\n        ", options.Select(
        o => $"Option<{o.Type}> {o.Name.AsField()}Option =" + 
        $"\n            new Option<{o.Type}>({OptionAlias(o)}," + 
        $"\n                {o.Description.InQuotes()});"));
```

### Test outputting code

You can test your output methods by creating an instance of your model within the test, and passing it an a cancellation token to the method. You can compare the resulting output with the output expected - copied from your example project.

You can do this comparison with the asserts of your unit test framework, but a verification framework like Verify or ApprovalTests offer a difference comparison with the expected output and let you find and resolve problems much more quickly.

If you have plan to output code via `RegisterPostInitializationOutput`, copy it to a string that is available to the generator. Because this code is an explicit string, testing may not be needed.



## Create the generator and test end to end

And incremental generator is a class that implements the `IIncrementalGenerator` interface which has a single method: `Initialize`. You define the pipeline for your generator in this method. The [full class implementation](#the-incremental-generator) appears at the start of this article and details are discussed here.

### Creating the generator

The example in this section uses the following methods, assuming they are written and tested:

* `ModelBuilder.IsSyntaxInteresting` which is a predicate that takes a SyntaxNode and returns a `true` if the node should be considered further.
* `ModelBuilder.GetModel` is a method that takes uses syntax node and the semantic model to build a cacheable data model.
* `CodeOutput.AlwaysCli` is a method that returns a string that is a class that is always the same and that should always be available to the user when the generator is referenced.
* `CodeOutput.PartialCli` is a method that returns a string if there are any data models. This string is the other portion of the Cli partial class.
* `CodeOutput.FileName` is a method that returns a string with the file name for a given data model.
* `CodeOutput.GenerateCommandCode` is a method that returns a string which is the source code for a given data model.

```csharp
public void Initialize(IncrementalGeneratorInitializationContext initContext)
{
    var commandModelValues = initContext.SyntaxProvider
        .CreateSyntaxProvider(
            predicate: ModelBuilder.IsSyntaxInteresting,
            transform: ModelBuilder.GetModel)
        .Where(static m => m is not null)!;
```

The first step of the generator builds a cacheable model. In this case, no additional sources are needed so the models are complete. The `commandModelValues` is of type `IncrementalValuesProvider<Models.CommandModel?>`. `Where` removes any `null` records. Since generation runs on code even when it has compile errors, expect invalid data even if correctly compiling code would not include it.

[[  @chsienki why does `Where` return a nullable? ]]

This generator outputs a single file if any data models exist. `Collect` provides this:

```csharp
    var rootCommandValue = commandModelValues.Collect();
```

The type of `rootCommandValue` is `IncrementalValueProvider<System.Collections.Immutable.ImmutableArray<Models.CommandModel?>>`. You could simplify this content by calling `Select`.

There are two kinds of output in this generator. This code uses a partial class that includes a portion that contains a method that is always available for discoverability. The other partial of this class provides the implementation. The partial class that is always available is output using the `RegisterPostInitializationOutput` method, and the part that depends on the the presence of data models is output using the `RegisterSourceOutput` method:

```csharp
    initContext.RegisterPostInitializationOutput((postInitContext) =>
        postInitContext.AddSource("Cli.g.cs", CodeOutput.AlwaysCli));

    initContext.RegisterSourceOutput(
        rootCommandValue,
        static (outputContext, modelData) =>
                outputContext.AddSource("Cli.Partial.g.cs",
                    CodeOutput.PartialCli(
                        modelData, outputContext.CancellationToken)));
```

The `CodeOutput.PartialCli` checks the contained collection and returns an empty string if there are no data models.

[[  @chsienki should this return an empty string or null. at what point should output use cancellation, this output is simple. ]]

Finally, a file is output for each data model using the `RegisterSourceOutput` method:

```csharp
    initContext.RegisterSourceOutput(
        commandModelValues,
        static (outputContext, modelData) =>
                outputContext.AddSource(CodeOutput.FileName(modelData),
                    CodeOutput.GenerateCommandCode(
                        modelData, outputContext.CancellationToken)));
}
```


### Testing the generator

With some helper methods, you can test your generator with the generator driver. 

#### Creating the input compilation

A helper method to get the input compilation needs the input source code as strings, and the output type of the compilation - console app, library, etc. This method needs to:

* Convert each source code to a `SyntaTree`
* Add using statements to each `SyntaxTree`
* Define the assemblies to use in the compilation
* Create a `CSharpCompilationOptions` instance
* Return the newly created compilation

An example of this helper method:

```csharp
public static Compilation GetInputCompilation<TGenerator>(
    OutputKind outputKind, params string[] code)
{
    var syntaxTrees = code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray();
    var newUsings = new UsingDirectiveSyntax[] {
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.IO")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Collections.Generic")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Linq")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System")) };
    var updatedSyntaxTrees = syntaxTrees
        .Select(x => x.GetCompilationUnitRoot().AddUsings(newUsings).SyntaxTree);

    // REVIEW: Is there a better way to get the references
    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
    var references = assemblies
        .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
        .Select(_ => MetadataReference.CreateFromFile(_.Location))
        .Concat(new[]
        {
            MetadataReference.CreateFromFile(typeof(TGenerator).Assembly.Location),
        });

    var compilationOptions = new CSharpCompilationOptions(
        outputKind,
        nullableContextOptions: NullableContextOptions.Enable);


    return CSharpCompilation.Create("compilation",
                                    updatedSyntaxTrees,
                                    references,
                                    compilationOptions);
}
```

Use helper methods like these to to ignore unimportant diagnostics when you are checking for success in your tests:

```csharp
public static IEnumerable<Diagnostic> ErrorAndWarnings(Compilation compilation)
    => ErrorAndWarnings(compilation.GetDiagnostics());

public static IEnumerable<Diagnostic> ErrorAndWarnings(IEnumerable<Diagnostic> diagnostics) 
    => diagnostics.Where(
            x => x.Severity == DiagnosticSeverity.Error ||
                    x.Severity == DiagnosticSeverity.Warning);
```

### Generate code in a test


```csharp
    public static (Compilation compilation, GeneratorDriverRunResult runResult) 
        GenerateTrees<TGenerator>(Compilation inputCompilation)
        where TGenerator : IIncrementalGenerator, new()
    {
        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, 
            out var compilation, out var _);

        var runResult = driver.GetRunResult();
        return (compilation, runResult);
    }
```

### The tests


```csharp
[Fact]
public void Can_generate_test()
{
    var inputCompilation = TestHelpers.GetInputCompilation<Generator>(
        OutputKind.DynamicallyLinkedLibrary, SimpleClass);
    Assert.NotNull(inputCompilation);
    Assert.Empty(TestHelpers.ErrorAndWarnings(inputCompilation));

    var (outputCompilation, runResult) = TestHelpers.GenerateTrees<Generator>(
        inputCompilation);
    var trees = runResult.GeneratedTrees;
    Assert.NotNull(outputCompilation);
    Assert.Empty(TestHelpers.ErrorAndWarnings(runResult.Diagnostics));

    Assert.Equal(4,trees.Count());
    Assert.Equal(expected1, trees[0].ToString());
    Assert.Equal(expected2, trees[1].ToString());
    Assert.Equal(expected3, trees[2].ToString());
    Assert.Equal(expected4, trees[3].ToString());
}
```

## Test performance

[[ @chsienki, etc I need help here ]]

## Create a NuGet package

[[ ]]

