using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IncrementalGeneratorSamples.Test
{
    public static class TestHelpers
    {
    public static (SyntaxNode? syntaxNode, ISymbol? symbol, SemanticModel? semanticModel, CancellationToken cancellationToken, IEnumerable<Diagnostic> inputDiagnostics)
        GetTransformInfoForClass<T>(string sourceCode, Func<T, bool>? filter = null, bool continueOnInputErrors = false)
        where T: SyntaxNode
    {
        // create a dummy cancellation token. These tests do not test cancellation
        var cancellationToken = new CancellationTokenSource().Token;

        // Get the compilation and check its state
        var compilation = TestHelpersCommon.GetInputCompilation<Generator>(
                OutputKind.DynamicallyLinkedLibrary, sourceCode);
        var inputDiagnostics = compilation.GetDiagnostics();
        if (!continueOnInputErrors && TestHelpersCommon.WarningAndErrors(inputDiagnostics).Any())
        { return (null, null, null, cancellationToken, inputDiagnostics); }

        // Get the syntax tree and filter to expected node
        var tree = compilation.SyntaxTrees.Single(); // tests are expected to have just one
        var matchQuery = tree.GetRoot()
            .DescendantNodes()
            .OfType<T>();
        if (filter is not null)
        { matchQuery = matchQuery.Where(x => filter(x)); }
        var matches = matchQuery.ToList();
        Assert.Single(matches);
        var syntaxNode = matches.Single();

        // Return, null values are only returned on failure
        var semanticModel = compilation.GetSemanticModel(tree);
        return (syntaxNode, semanticModel.GetDeclaredSymbol(syntaxNode), semanticModel, cancellationToken, inputDiagnostics);
    }

        public static void GenerateAndCompileProject<TGenerator>(IntegrationTestFromPathConfiguration configuration)
            where TGenerator : IIncrementalGenerator, new( )
            => TestHelpersCommon.GenerateAndCompileProject<TGenerator>(configuration.TestInputPath,
               configuration.TestGeneratedCodePath, configuration.GeneratedSubDirectoryName, configuration.OutputKind);


        public static string? RunCommand(IntegrationTestFromPathConfiguration configuration, string arguments)
            => TestHelpersCommon.RunCommand(configuration.TestBuildPath, configuration.ExecutableName, arguments);   
    }
}
