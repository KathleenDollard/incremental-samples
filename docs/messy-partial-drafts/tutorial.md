---
title: How to build a Roslyn incremental source generator
description: Explore the steps to creating a Roslyn incremental source generator.
author: KathleenDollard
ms.author: kdollard
ms.date: 6/11/2022 
ms.topic: tutorial
---
# Tutorial: Building an incremental generator

read the developing inner loop doc

There are 3 execution steps in incremental generators;

* Data extraction into a model.
* Transforming models (when needed).
* Outputting source code.

This tutorial builds and tests individual elements of the incremental generator pipeline before creating the actual generator. This will be easier to follow if you are familiar with the [Incremental source generator pipeline](pipeline.md).

## Create the example project

As discussed [Developing incremental source generators](development-inner-loop.md#structure-of-the-solution-and-packages) the first step of creating a generator is building a working sample project so you know exactly what you are going to generate. In the example project, this code is in the subdirectory *GeneratedViaTest*.

This example is a simple approach to wrapping the [System.CommandLine API](https://docs.microsoft.com/dotnet/standard/commandline/). While a more complete approach to System.CommandLine would include subcommands and arguments, this simple approach just allows the user to specify the options on they root command using code like the following:

```csharp
using IncrementalGeneratorSamples.Runtime;

namespace TestExample;
[Command]
public partial class Command
{
    /// <summary>
    /// The file to read and display on the console.
    /// </summary>
    public FileInfo? File { get;  }

    /// <summary>
    /// Delay between lines, specified as milliseconds per character in a line.
    /// </summary>
    public int Delay { get;  }

    public int DoWork() 
    {
        // do work, such as displaying the file here
        return 0;
    }
}
```

An adjacent library, IncrementalGeneratorSamples.Runtime,includes the CommandAttribute and base classes. The user does not even need a using statement for System.CommandLine, although the reference is needed and will be included as a dependency of the generator package.

In order to run the example project and ensure it works correctly, you need an entry point which could be a `main` method or top level statements:

```csharp
using TestExample;

Command.Invoke(args)
```

The generator will create code based on the example user code. But to begin creating your generator, you need to manually write the code that will later be generated. This code will serve as a guide during development, can be copied as a starting pointing to code output, and will be used in integration testing:

```csharp
using System.CommandLine;
using System.CommandLine.Invocation;

#nullable enable

namespace TestExample;
public partial class Command
{
    public Command(FileInfo? file, int delay)
    {
        File = file;
        Delay = delay;
    }

    public static void Invoke(string[] args)
        => CommandHandler.Invoke(args);

    internal class CommandHandler : IncrementalGeneratorSamples.Runtime.CommandHandler<CommandHandler>
    {
        private Option<FileInfo?> fileOption;
        private Option<int> delayOption;

        public CommandHandler()
        {
            fileOption = new Option<FileInfo?>("--file", "The file to read and display on the console.");
            RootCommand.AddOption(fileOption);
            delayOption = new Option<int>("--delay", "Delay between lines, specified as milliseconds per character in a line.");
            RootCommand.AddOption(delayOption);
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new Command(GetValueForSymbol(fileOption, commandResult), GetValueForSymbol(delayOption, commandResult));
            return command.DoWork();
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }
    }

}
```

## Design a data model

The data model should contain the minimal information required for generation and will generally be scoped to internal. It must provide value equality.

In order to generate a command line CLI the model needs to contain commands and options:

[[ From tutorial. Not in proper form until initial review.]]

```csharp
namespace IncrementalGeneratorSamples.Generator.Models;

internal record GenerationModel
{

    internal GenerationModel(string commandName, IEnumerable<OptionModel> options)
    {
        CommandName = commandName;
        Options = options;
    }

    internal string CommandName { get; }


    internal IEnumerable<OptionModel> Options { get; }

    public virtual bool Equals(GenerationModel model)
        => model is not null && 
            model.CommandName == CommandName &&
            model.Options.SequenceEqual(this.Options);

    public override int GetHashCode()
    {
        var hash = CommandName.GetHashCode();
        foreach(var prop in Options)
        {
            hash ^= prop.GetHashCode();
        }
        return hash;
    }
}

internal record OptionModel
{
    internal OptionModel(string name, string type, string description)
    {
        Name = name;
        Type = type;
        Description = description;
    }

    internal string Name { get; }
    internal string Type { get; }
    internal string Description { get; }
}  
```

## Check the data model for completeness

Review the data model in relation to the example project. Map each variable element in the generated code to the input to check that the data in the model can be extracted:

* The class for the root element will be marked with the `IncrementalGeneratorSamples.Runtime.CommandAttribute`.
* The name and type of each property will be the name and type of the option.
* Casing is adjusted on output as CLI options are almost always lower case.
* The description will be retrieved from the XML comments.

## Filter syntax nodes

The first step in an incremental generator that uses source code as input is to gather the syntax nodes. This is done with via the predicate passed to the `CreateSyntaxProvider` method. This predicate is passed every node in the `SyntaxTree`and returning true if the node should be further considered by the generation pipeline. Doing this in a separate method makes it easy to test in isolation.

When this method is called from the generator, a syntax node and a cancellation token are passed. If possible, the work done in this method is very fast, and cancellation can be handled by the generator. The implementation here is very fast because it relies only on pattern matching against the type, and for classes checking for the `Command` attribute:

```csharp
        public static bool IsSyntaxInteresting(SyntaxNode syntaxNode, CancellationToken _)
            // REVIEW: What's the best way to check the qualified name? 
            // REVIEW: This should be very fast. Is it ok to ignore the cancelation token in that case?
            // REVIEW: Will this catch all the ways people can use attributes
            => syntaxNode is ClassDeclarationSyntax cls &&
                cls.AttributeLists.Any(x => x.Attributes.Any(a =>
                     a.Name.ToString() == "Command" || 
                     a.Name.ToString() == "CommandAttribute"));

```

## Test filtering syntax nodes

Testing this code requires creating a number of `SyntaxNode` and a `CancellationToken`. Create syntax nodes from source code mimics what will happen when your data runs. You can create data for your tests as strings. This sample source code is similar to the [tutorial for System.CommandLine](https://docs.microsoft.com/dotnet/standard/commandline/get-started-tutorial) :

```csharp
private const string SimpleClass = @"
using IncrementalGeneratorSamples.Runtime;

[Command]
public partial class Command
{
    public int Delay { get;  }
}
";
```

If we assume other tests will catch if the wrong `SyntaxNode` is returned, you can test the count of matching syntax nodes, which can take advantage of XUnit's theory feature:

```csharp
[Theory]
[InlineData(1, SimpleClass)]
[InlineData(1, CompleteClass)]
public void Should_select_attributed_syntax_nodes(int expectedCount, string sourceCode)
{
    var cancellationToken = new CancellationTokenSource().Token;
    var tree = CSharpSyntaxTree.ParseText(sourceCode);
    var matches = tree.GetRoot()
        .DescendantNodes()
        .Where(node => ModelBuilder.IsSyntaxInteresting(node, cancellationToken));
    Assert.Equal(expectedCount, matches.Count());
}
```

These passing tests provide confidence that the initial filtering of syntax nodes in the generator will be successful. 

## Create the data model

The next step in the incremental generator is to create an initial data model for each `SyntaxNode` selected by the predicate. This is done in the transform delegate that is passed to the `CreateSyntaxProvider` method. The data models are returned as an `IncrementalValuesProvider<GenerationModel>` (note the *s* on values indicating a collection).

Sometimes additional transformations, such as combining the initial data model with others, or summarizing the data in these models. Additional transformations will take and also return an `IncrementalValueSourceProvider` or an `IncrementalValuesProvider`. Additional transformations are not needed for this tutorials, and you can find out more about them in the [Incremental Generators specification](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md#syntaxvalueprovider).

The signature for the transform delegate includes a `GeneratorSyntaxContext` object that cannot be created in a test. Instead, a trivial method can deconstruct the `GeneratorSyntaxContext` into a testable method:

```csharp
public static GenerationModel? GetModel(GeneratorSyntaxContext generatorContext, 
                                        CancellationToken cancellationToken)
    => GetModel(generatorContext.Node, generatorContext.SemanticModel, cancellationToken);

public static GenerationModel? GetModel(SyntaxNode syntaxNode, 
                                        SemanticModel semanticModel, 
                                        CancellationToken cancellationToken)
{
    // actual work here
}
```

The first part of creating the model is find the associated symbol from the semantic model. Note that a cancellation token is passed. The symbol is then cast to `ITypeModel`. This cast should always succeed because the predicate in the previous step of the pipeline filtered to `ClassDeclarationSyntax`. But if it does not, return null:

```csharp
var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
if (symbol is not ITypeSymbol typeSymbol)
{ return null; }
```

The `typeModel` allows access to the members of the type, which can be filtered to just the properties for this transform. Because this collection is not bounded, cancellation of the iteration is supported. This loop creates an `OptionModel` for each property (the `GetPropertyDescription` is covered later in this section):

```csharp
var properties = typeSymbol.GetMembers().OfType<IPropertySymbol>();
var options = new List<OptionModel>();
foreach (var property in properties)
{
    // since we do not know how big this list is, so we will check cancellation token
    // REVIEW: Should this return null or throw? 
    if (cancellationToken.IsCancellationRequested)
    { return null; }
    var description = GetPropertyDescription(property);
    options.Add(new OptionModel(property.Name, property.Type.ToString(), description));
}
```

The `typeSymbol.Name` and the `OptionModel` collection are used to create the new `GenerationModel`:

```csharp
return new GenerationModel(typeSymbol.Name, options);
```

One of the many gems in the `SemanticModel` is access to XML documentation. In particular, the description of types, parameters, properties and other members which this sample later uses as the description of the options. Here, this is done in a local static method:

```csharp
static string GetPropertyDescription(IPropertySymbol prop)
{
    var doc = prop.GetDocumentationCommentXml();
    if (string.IsNullOrEmpty(doc))
    { return ""; }
    var xDoc = XDocument.Parse(doc);
    var desc = xDoc.DescendantNodes()
        .OfType<XElement>()
        .FirstOrDefault(x => x.Name == "summary")
        ?.Value;
    return desc is null
        ? ""
        : desc.Replace("\n","").Replace("\r", "").Trim();
}
```

The full `GetModel` method:

```csharp
{
    var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
    if (symbol is not ITypeSymbol typeSymbol)
    { return null; }

    var properties = typeSymbol.GetMembers().OfType<IPropertySymbol>();
    var options = new List<OptionModel>();
    foreach (var property in properties)
    {
        // since we do not know how big this list is, so we will check cancellation token
        // REVIEW: Should this return null or throw?
        if (cancellationToken.IsCancellationRequested)
        { return null; }
        var description = GetPropertyDescription(property);
        options.Add(new OptionModel(property.Name, property.Type.ToString(), description));
    }
    return new GenerationModel(typeSymbol.Name, options);

    static string GetPropertyDescription(IPropertySymbol prop)
    {
        // REVIEW: Not crazy about the repeated Parsing of small things.
        var doc = prop.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(doc))
        { return ""; }
        var xDoc = XDocument.Parse(doc);
        var desc = xDoc.DescendantNodes()
            .OfType<XElement>()
            .FirstOrDefault(x => x.Name == "summary")
            ?.Value;
        return desc is null
            ? ""
            : desc.Replace("\n","").Replace("\r", "").Trim();
    }
}
```

## Test data model creation

To allow reuse, the code to create a data model is in a separate method. This method can be called by tests which provide the source code and assert the results. This method is:

```csharp
private GenerationModel? GetModelForTesting(string sourceCode)
{
    var cancellationToken = new CancellationTokenSource().Token;
    var (compilation, diagnostics) = TestHelpers.GetInputCompilation<Generator>(
            OutputKind.DynamicallyLinkedLibrary, sourceCode);
    Assert.Empty(diagnostics);
    var tree = compilation.SyntaxTrees.Single();
    var matches = tree.GetRoot()
        .DescendantNodes()
        .Where(node => ModelBuilder.IsSyntaxInteresting(node, cancellationToken));
    Assert.Single(matches);
    var syntaxNode = matches.Single();
    return ModelBuilder.GetModel(syntaxNode, 
                                 compilation.GetSemanticModel(tree), 
                                 cancellationToken);
}
```

A cancellationToken is created for later use. The source code is used to create a compilation via a helper method available in the full project. It is very easy to make mistakes when writing  source code as a string, so checking the syntax here can prevent wasting time over a typo or forgotten using statement. The source code used here is the same as used in the earlier testing of the syntax node filtering. and the same filtering is done to prepare to create the model.

Once it has the correct syntax node, it passes it to the same `GetModel` method that will be used by the generator.

Because the assertions are unique to each of the tests, individual tests rather than Theory tests are used. One of these tests is:

```csharp
[Fact]
public void Should_build_model_from_SimpleClass()
{
    var model = GetModelForTesting(SimpleClass); 
    Assert.NotNull(model);
    if (model is null) return; // to appease NRT
    Assert.Equal("Command", model.CommandName);
    Assert.Single(model.Options);
    Assert.Equal("Delay", model.Options.First().Name);
    Assert.Equal("int", model.Options.First().Type);
}
```

After retrieving the model, assertions ensure that the `GenerationModel` was built correctly.

## Creating output

The `RegisterSourceOutput` calls a delegate that is passed a `SourceProductionContext` and the model l in the pipeline. The `SourceProductionContext` has an `AddSource` method that takes a *hint* name and the source code to output as a string. The hint name is the filename for the output that is placed in the `obj` folder.

GeneratedCode method creates the source output via an interpolated string. The start of this method illustrates the approach of inserting method calls to iterate over the option collection: 

```csharp
public static string GeneratedCode(GenerationModel? modelData)
{
    if (modelData is null)
    { return ""; }

    return $@"
using System.CommandLine;
using System.CommandLine.Invocation;

#nullable enable

namespace TestExample;

public partial class {modelData.CommandName}
{{
    public {modelData.CommandName}({Parameters(modelData.Options)})
    {{
        {CtorAssignments(modelData.Options)}
    }}
```

Using normal literal string interpolation, you need to double C# curly brackets. If you are using C# 11 you can use raw string literals, which makes it easier to lay out this code. [Check the tip about backwards compatibility before using new C# or compiler features.](tips.md#warning-about-api-availability).

To create a method like this for your generator, copy the example code you created earlier and insert calls to the model as needed. Complex output will require care retain readability

Loops are handled via local functions such as `CtorAssignments`:

```csharp
static string CtorAssignments(IEnumerable<OptionModel> options)
    => string.Join("\n        ", options.Select(o => $"{o.Name.AsProperty()} = {o.Name.AsField()};"));
```

The full method for outputting the example is:

```csharp
    public static string GeneratedCode(GenerationModel? modelData)
    {
        if (modelData is null)
        { return ""; }

        return $@"
using System.CommandLine;
using System.CommandLine.Invocation;

#nullable enable

namespace TestExample;

public partial class {modelData.CommandName}
{{
    public {modelData.CommandName}({Parameters(modelData.Options)})
    {{
        {CtorAssignments(modelData.Options)}
    }}

    public static void Invoke(string[] args)
        => CommandHandler.Invoke(args);

    internal class CommandHandler : IncrementalGeneratorSamples.Runtime.CommandHandler<CommandHandler>
    {{
        {OptionFields(modelData.Options)}

        public CommandHandler()
        {{
            {OptionCreate(modelData.Options)}
            {OptionAssign(modelData.Options)}
        }}

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {{
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new Command({CommandParams(modelData.Options)});
            return command.DoWork();
        }}

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {{
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }}
    }}

}}
";
        static string Parameters(IEnumerable<OptionModel> options)
            => string.Join(", ", options.Select(o => $"{o.Type} {o.Name.AsField()}"));

        static string CtorAssignments(IEnumerable<OptionModel> options)
            => string.Join("\n        ", options.Select(o => $"{o.Name.AsProperty()} = {o.Name.AsField()};"));

        static string OptionFields(IEnumerable<OptionModel> options)
            => string.Join("\n        ", options.Select(o => $"Option<{o.Type}> {o.Name.AsField()}Option;"));

        static string OptionCreate(IEnumerable<OptionModel> options)
            => string.Join("\n            ", options.Select(o => $"{o.Name.AsField()}Option = new Option<{o.Type}>({o.Name.AsAlias().InQuotes()}, {o.Description.InQuotes()});"));

        static string OptionAssign(IEnumerable<OptionModel> options)
            => string.Join("\n            ", options.Select(o => $"RootCommand.AddOption({o.Name.AsField()}Option);"));

        static string CommandParams(IEnumerable<OptionModel> options)
            => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.Name.AsField()}Option, commandResult)"));
    }
```

## Test code output



## Writing tests for data extraction

[There are two approaches to testing Roslyn generators](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators), whether they are V1 generators or incremental generators. This tutorial uses the conceptually simpler approach to focus on incremental generators themselves. These tests will use explicitly run generation, and the data extraction tests will output serialized data as comments in C# syntax by using a separate testing generator. This also means your first incremental will produce very simple code, making it easier to follow the process. The generated content is compared to previously approved content using [Verify](https://github.com/VerifyTests/Verify); alternatively you could use a similar library like [ApprovalTests](https://github.com/approvals/ApprovalTests.Net).

The generator for this test will be a new class in the generator project and will consist of extraction and outputting the serialized model. 







You can find and implementation example of these methods in the [tutorial](tutorial.md).

The delegate takes a `SyntaxNode` and returns true if the syntax is interesting.  Retrieving syntax marked with a specific attribute is so common that a new API is planned for Visual Studio 17.3 - [check the tip on backwards compatibility if you plan to use this API](tips.md#backwards-compatibility-and-the-roslyn-api).

[[ **** REVIEW ****  and see review notes in code below ]]

```csharp
public static bool IsSyntaxInteresting(SyntaxNode syntaxNode, CancellationToken _)
    // REVIEW: What's the best way to check the qualified name? 
    // REVIEW: This should be very fast. Is it ok to ignore the cancelation token in that case?
    // REVIEW: Will this catch all the ways people can use attributes
    => syntaxNode is ClassDeclarationSyntax cls &&
        cls.AttributeLists.Any(x => 
            x.Attributes.Any(a => 
                a.Name.ToString() == "Command" || a.Name.ToString() == "CommandAttribute"));
```

If `syntaxNode ` is not a `ClassDeclarationSyntax`, false is immediately returned from a very fast check. All class declarations in your code have their attribute lists checked. If there are no `AttributeLists` or `Attributes`, false is also returned very quickly. The slightly slower string comparison only occurs for attributes on syntax nodes. 

The second delegate, `GetModel` is split into two overloads to allow testing. The generator calls the first overload passing a `GeneratorSyntaxContext` which is not available in unit tests. The second takes the only two values that are used: the `SyntaxNode` and the `SyntacticModel`. This second overload builds the data model:

```csharp
public static CommandModel? GetModel(GeneratorSyntaxContext generatorContext,
                                        CancellationToken cancellationToken)
    => GetModel(generatorContext.Node, generatorContext.SemanticModel, cancellationToken);

public static CommandModel? GetModel(SyntaxNode syntaxNode,
                                        SemanticModel semanticModel,
                                        CancellationToken cancellationToken)
{
// get the model here 
}
```

`GetModel` retrieves the symbol from the `SemanticModel`l Depending on what you are doing, you may want to retrieve other things from the semantic model, such as the `IOperation`. Most of the methods of the `SemanticModel` accept a cancellation token, and you should pass on the one passed to the delegate. Generation runs on code that does not successfully compile, so there may be no corresponding symbol. You can return `null` and filter out null values in the next pipeline step:
  
```c#
    var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
    if (symbol is not ITypeSymbol typeSymbol)
    { return null; }
```

One of the 

```c#
    var description = GetXmlDescription(symbol.GetDocumentationCommentXml());
    var properties = typeSymbol.GetMembers().OfType<IPropertySymbol>();
    var options = new List<OptionModel>();
    foreach (var property in properties)
    {
        // since we do not know how big this list is, so we will check cancellation token
        cancellationToken.ThrowIfCancellationRequested();

        var propDescription = GetXmlDescription(property.GetDocumentationCommentXml());
        options.Add(new OptionModel(property.Name, property.Type.ToString(), propDescription));
    }
    return new CommandModel(typeSymbol.Name,description, options);
```

```c#
    static string GetXmlDescription(string? doc)
    {
        if (string.IsNullOrEmpty(doc))
        { return ""; }
        var xDoc = XDocument.Parse(doc);
        var desc = xDoc.DescendantNodes()
            .OfType<XElement>()
            .FirstOrDefault(x => x.Name == "summary")
            ?.Value;
        return desc is null
            ? ""
            : desc.Replace("\n","").Replace("\r", "").Trim();
    }
}
```

It's time to build your generator! It will look something like this:

```csharp
[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // Output code that is always available. `common` is a string field
        initContext.RegisterPostInitializationOutput((postInitContext) =>
            postInitContext.AddSource("CommonAttribute.g.cs", common));

        // Retrieve syntax, build a model, and filter out nulls
        var models = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: ModelBuilder.IsSyntaxInteresting,
                transform: ModelBuilder.GetModel)
            .Where(static m => m is not null)!;

        // If needed, create a single IncrementalValueProvider with a collection of your data model.
        var modelCollection = models.Collect();

        // Output a file for each data model. Code output retrieves data from the model.
        initContext.RegisterSourceOutput(
            models,
            static (context, modelData) =>
                    context.AddSource(CodeOutput.FileName(modelData),
                                      CodeOutput.GenerateFromModel(modelData, context.CancellationToken)));

        // Output a single file for all data models. Code output retrieves data from the model.
        initContext.RegisterSourceOutput(
            modelCollection,
            static (context, modelData) =>
                    context.AddSource("Root.g.cs",
                                      CodeOutput.GenerateFromCollection(modelData, context.CancellationToken)));

    }
}
```

Testing this generator requires Roslyn features and your own wrapping methods. The first step is to build a compilation of your example project without the generated code. It's a good idea to copy the code plan to generate from the subdirectory (`OverwrittenInTests` was the name suggested earlier) into a solution folder.

### Testing the example project compilation

The first step is to ensure you can create a compilation in tests. You can use these helper methods or build your own: 

```csharp
public static (Compilation inputCompilation, IEnumerable<Diagnostic> inputDiagnostics) 
    GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
{
    var inputCompilation = CreateInputCompilation<TGenerator>(outputKind, code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray());
    var inputDiagnostics = inputCompilation.GetDiagnostics()
        .Where(x => x.Severity == DiagnosticSeverity.Error || x.Severity == DiagnosticSeverity.Warning);
    return (inputCompilation, inputDiagnostics);
}
```

This code creates an compilation and returns the compilation and filtered diagnostics. It is called the input compilation because it is the input to the source generator.

The CreateInputCompilation method creates the compilation. Creating compilation needs the kind of the output (library, console app, etc.), usings, and the metadata references, along with the syntax trees that make up the compilation:

```csharp
private static Compilation CreateInputCompilation<TGenerator>(
    OutputKind outputKind, SyntaxTree[] syntaxTrees)
{
    // REVIEW: Is there a better way to get the references
    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
    var references = assemblies
        .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
        .Select(_ => MetadataReference.CreateFromFile(_.Location))
        .Concat(new[]
        {
            MetadataReference.CreateFromFile(typeof(TGenerator).Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(EnumExtensionsAttribute).Assembly.Location)
        });

    var newUsings = new UsingDirectiveSyntax[] {
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.IO")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Collections.Generic")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Linq")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System")) };

    var updatedSyntaxTrees = syntaxTrees
        .Select(x => x.GetCompilationUnitRoot().AddUsings(newUsings).SyntaxTree);

    return CSharpCompilation.Create("compilation",
                updatedSyntaxTrees,
                references,
                new CSharpCompilationOptions(outputKind,
                                            nullableContextOptions: NullableContextOptions.Enable));
}
```

You may find that there are diagnostics in this compilation that you need to ignore because they will be resolved by the generation - such a missing class that you create. Getting the compilation correct can be one of the more frustrating aspects of creating end to end testing, so it is quite helpful to create one or more unit tests that successfully create the compilation:

```csharp
[Fact]
public void Can_compile_input()
{
    var (inputCompilation, inputDiagnostics) = TestHelpers.GetInputCompilation<Generator>(OutputKind.DynamicallyLinkedLibrary, SimpleClass);
    Assert.NotNull(inputCompilation);
    Assert.Empty(inputDiagnostics);
}
```

When that test passes, you're ready to generate code.

### Testing the generator

```csharp
public static (Compilation compilation, IEnumerable<SyntaxTree> outputTrees, 
               IEnumerable<Diagnostic> outputDiagnostics) 
    GenerateTrees<TGenerator>(Compilation inputCompilation)
    where TGenerator : IIncrementalGenerator, new()
{
    var generator = new TGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, 
                                                      out var compilation, 
                                                      out var _);

    var runResult = driver.GetRunResult();
    var outputDiagnostics = runResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error || x.Severity == DiagnosticSeverity.Warning);
    return (compilation, runResult.GeneratedTrees, outputDiagnostics);
}
```


```csharp
[Fact]
public void Can_generate_test()
{
    var (inputCompilation, inputDiagnostics) = 
        TestHelpers.GetInputCompilation<Generator>( 
            OutputKind.DynamicallyLinkedLibrary, SimpleClass);
    var (outputCompilation, trees, outputDiagnostics) = 
        TestHelpers.GenerateTrees<Generator>(inputCompilation);
    Assert.NotNull(outputCompilation);
    Assert.Empty(outputDiagnostics);
    Assert.Equal(4,trees.Count());
}
```