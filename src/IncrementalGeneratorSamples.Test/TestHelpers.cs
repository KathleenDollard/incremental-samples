using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace IncrementalGeneratorSamples.Test
{
    public static class TestHelpers
    {
        public static CancellationToken CancellationTokenForTesting => new CancellationTokenSource().Token;

        public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
        {
            // Create the initial syntax tree, add using statements, and get the updated tree
            var syntaxTrees = code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray();

            var newUsings = new UsingDirectiveSyntax[] {
            SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.IO")),
            SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Collections.Generic")),
            SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Linq")),
            SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System")) };
            var updatedSyntaxTrees = syntaxTrees
                .Select(x => x.GetCompilationUnitRoot().AddUsings(newUsings).SyntaxTree);

            // REVIEW: Is there a better way to get the references
            // Add assemblies from the current (test) project
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


            // Create the compilation options and return the new compilation
            return CSharpCompilation.Create("compilation",
                                            updatedSyntaxTrees,
                                            references,
                                            compilationOptions);
        }

        // expect to use this with end to end testing
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

        public static IEnumerable<Diagnostic> ErrorAndWarnings(IEnumerable<Diagnostic> diagnostics)
            => diagnostics.Where(
                    x => x.Severity == DiagnosticSeverity.Error ||
                         x.Severity == DiagnosticSeverity.Warning);

        public static (SyntaxNode? syntaxNode, ISymbol? symbol, SemanticModel? semanticModel, CancellationToken cancellationToken, IEnumerable<Diagnostic> inputDiagnostics)
            GetTransformInfoForClass(string sourceCode, Func<ClassDeclarationSyntax, bool>? filter = null, bool continueOnInputErrors = false)
        {
            // create a dummy cancellation token. These tests do not test cancellation
            var cancellationToken = new CancellationTokenSource().Token;

            // Get the compilation and check its state
            var compilation = GetInputCompilation<Generator>(
                    OutputKind.DynamicallyLinkedLibrary, sourceCode);
            var inputDiagnostics = compilation.GetDiagnostics();
            if (!continueOnInputErrors && TestHelpers.ErrorAndWarnings(inputDiagnostics).Any())
            { return (null, null, null, cancellationToken, inputDiagnostics); }

            // Get the syntax tree and filter to expected node
            var tree = compilation.SyntaxTrees.Single(); // tests are expected to have just one
            var matchQuery = tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>();
            if (filter is not null)
            { matchQuery = matchQuery.Where(x => filter(x)); }
            var matches = matchQuery.ToList();
            Assert.Single(matches);
            var syntaxNode = matches.Single();

            // Return, null values are only returned on failure
            var semanticModel = compilation.GetSemanticModel(tree);
            return (syntaxNode, semanticModel.GetDeclaredSymbol(syntaxNode), semanticModel, cancellationToken, inputDiagnostics);
        }
    }
}