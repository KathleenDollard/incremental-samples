
using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace IncrementalGeneratorSamples.Test
{
    public class TestHelpers
    {

        public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
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
    }
}