using IncrementalGeneratorSamples;
using IncrementalGeneratorSamples.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace IncrementalGeneratorSamples.Test;

[UsesVerify]
public class ModelBuildingTests
{
    private const string SimpleClass = @"
using IncrementalGeneratorSamples.Runtime;

[Command]
public partial class Command
{
    public int Delay { get;  }
}
";

    private const string ClassWithXmlComment = @"
using IncrementalGeneratorSamples.Runtime;

[CommandAttribute]
public partial class Command
{
    /// <summary>
    /// Delay between lines, specified as milliseconds per character in a line.
    /// </summary>
    public int Delay { get;  }
}
";

    private const string CompleteClass = @"
using IncrementalGeneratorSamples.Runtime;
using System.IO;

#nullable enable

[Command]
public partial class CompleteCommand
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
";

    [Theory]
    [InlineData(1, SimpleClass)]
    [InlineData(1, ClassWithXmlComment)]
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

    private CommandModel? GetModelForTesting(string sourceCode)
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

    [Fact]
    public void Should_build_model_from_SimpleClass()
    {
        var model = GetModelForTesting(SimpleClass); 
        Assert.NotNull(model);
        // REVIEW: Is there a better way to quiet the warning left after the NotNull assertion?
        if (model is null) return; // to appease NRT
        Assert.Equal("Command", model.CommandName);
        Assert.Single(model.Options);
        Assert.Equal("Delay", model.Options.First().Name);
        Assert.Equal("int", model.Options.First().Type);
    }

    [Fact]
    public void Should_build_model_from_CompleteClass()
    {
        var model = GetModelForTesting(CompleteClass);
        Assert.NotNull(model);
        if (model is null) return; // to appease 
        Assert.Equal("CompleteCommand", model.CommandName);
        Assert.Equal(2, model.Options.Count());
        Assert.Equal("File", model.Options.First().Name);
        Assert.Equal("System.IO.FileInfo?", model.Options.First().Type);
        Assert.Equal("Delay", model.Options.Skip(1).First().Name);
        Assert.Equal("int", model.Options.Skip(1).First().Type);
    }

    [Fact]
    public void Should_include_Xml_description_in_model()
    {
        var model = GetModelForTesting(ClassWithXmlComment);
        Assert.NotNull(model);
        if (model is null) return; // to appease 
        Assert.Equal("Command", model.CommandName);
        Assert.Single(model.Options);
        Assert.Equal("Delay between lines, specified as milliseconds per character in a line.", model.Options.First().Description);
    }



    //[Theory]
    //[InlineData("SimpleClass", SimpleClass)]
    //[InlineData("ClassWithXmlComments", ClassWithXmlComment)]
    //[InlineData("CompleteClass", CompleteClass)]
    //public Task Should_build_correct_model(string fileName, string sourceCode)
    //{
    //    var (inputCompilation, inputDiagnostics ) = TestHelpers.GetInputCompilation<ModelTestGenerator>(OutputKind.DynamicallyLinkedLibrary, sourceCode);
    //    // TODO: You may need  to exclude specific diagnostics here, such as CS0103 is you use symbols that are created during generation. 
    //    Assert.Empty(inputDiagnostics);

    //    var (output, outputDiagnostics) = TestHelpers.Generate<ModelTestGenerator>(inputCompilation);
    //    Assert.Empty(outputDiagnostics);

    //    return Verifier.Verify(output).UseDirectory("ModelSnapshots").UseTextForParameters(fileName);
    //}
}