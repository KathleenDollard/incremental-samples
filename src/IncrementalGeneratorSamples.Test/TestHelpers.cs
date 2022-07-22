using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace IncrementalGeneratorSamples.Test
{
    public static class TestHelpers
    {
        public static CancellationToken CancellationTokenForTesting => new CancellationTokenSource().Token;

        public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
            => GetInputCompilation<TGenerator>(outputKind, code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray());

        public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params SyntaxTree[] syntaxTrees)
        {
            // Create the initial syntax tree, add using statements, and get the updated tree
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

        public static IEnumerable<Diagnostic> WarningAndErrors(IEnumerable<Diagnostic> diagnostics)
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
            if (!continueOnInputErrors && WarningAndErrors(inputDiagnostics).Any())
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

        // expect to use this with end to end testing
        public static (Compilation compilation, GeneratorDriverRunResult runResult)
            Generate<TGenerator>(Compilation inputCompilation)
            where TGenerator : IIncrementalGenerator, new()
        {
            var generator = new TGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation,
                out var compilation, out var _);

            var runResult = driver.GetRunResult();
            return (compilation, runResult);
        }



        public static void GenerateIntoProject<T>(IntegrationTestConfiguration configuration)
            where T : IIncrementalGenerator, new()
        {
            SyntaxTree[] syntaxTrees = GetSyntaxTrees(configuration);
            var inputCompilation = GetInputCompilation<T>(configuration.OutputKind, syntaxTrees);
            var inputDiagnostics = inputCompilation.GetDiagnostics();
            CheckCompilation(configuration, inputCompilation, inputDiagnostics, diagnosticFilter: x => x.Id != "CS0103");

            var (outputCompilation, outputDiagnostics) = RunGenerator<T>(inputCompilation, new T());
            CheckCompilation(configuration, outputCompilation, outputDiagnostics, syntaxTreeCount: configuration.SyntaxTreeCount);

            OutputGeneratedTrees(configuration, outputCompilation);
            var exeProcess = CompileOutput(configuration);
            Assert.NotNull(exeProcess);
            Assert.True(exeProcess!.HasExited);

            var output = exeProcess.StandardOutput.ReadToEnd(); // available for debugging - can be a pain to get in VS
            var error = exeProcess.StandardError.ReadToEnd();
            Console.WriteLine(output);
            Assert.Equal(0, exeProcess.ExitCode);
            Assert.Equal("", error);
        }

        private static SyntaxTree[] GetSyntaxTrees(IntegrationTestConfiguration configuration)
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                MatchCasing = MatchCasing.PlatformDefault,
                MatchType = MatchType.Simple,
            };
            var files = Directory.GetFiles(configuration.TestInputPath, "*.cs", options);

            return files
                .Select(fileName => TreeFromFileInInputPath(configuration, fileName)).ToArray();
        }

        public static string? RunCommand<T>(IntegrationTestConfiguration configuration, string arguments)
            where T : IIncrementalGenerator, new()
                => RunProject(configuration, arguments);

        public static SyntaxTree TreeFromFileInInputPath(IntegrationTestConfiguration configuration, string fileName)
        {
            fileName = fileName.EndsWith(".cs")
                ? Path.Combine(configuration.TestInputPath, fileName)
                : Path.Combine(configuration.TestInputPath, fileName + ".cs");

            return CSharpSyntaxTree.ParseText(File.ReadAllText(fileName));
        }

        public static string? RunProject(IntegrationTestConfiguration configuration, string arguments)
            => TestHelpers.RunGeneratedProject(arguments, configuration.TestSetName, configuration.TestBuildPath);

        public static string IfOsIsWindows(string windowsString, string unixString)
             => Environment.OSVersion.Platform == PlatformID.Unix
                 ? unixString
                 : windowsString;

        //public CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTrees)
        //    => IntegrationHelpers.TestCreatingCompilation(syntaxTrees);

        public static void CheckCompilation(IntegrationTestConfiguration configuration,
                                            Compilation compilation,
                                            IEnumerable<Diagnostic> diagnostics,
                                            Func<Diagnostic, bool>? diagnosticFilter = null,
                                            int? syntaxTreeCount = null)
        {
            Assert.NotNull(compilation);
            var filteredDiagnostics = diagnosticFilter is null
                ? WarningAndErrors(diagnostics)
                : WarningAndErrors(diagnostics).Where(diagnosticFilter);
            Assert.Empty(filteredDiagnostics);
            if (syntaxTreeCount.HasValue)
            { Assert.Equal(syntaxTreeCount.Value, compilation.SyntaxTrees.Count()); }
        }

        public static void OutputGeneratedTrees(IntegrationTestConfiguration configuration, Compilation generatedCompilation)
            => TestHelpers.OutputGeneratedTrees(generatedCompilation,
                                                       configuration.TestGeneratedCodePath);

        public static Process? CompileOutput(IntegrationTestConfiguration configuration)
            => TestHelpers.CompileOutput(configuration.TestInputPath);

        //private static (CSharpCompilation compilation, ImmutableArray<Diagnostic> inputDiagnostics)
        //    GetCompilation<T>(IntegrationTestConfiguration configuration, params SyntaxTree[] syntaxTrees)
        //    where T : IIncrementalGenerator, new()
        //    => TestHelpers.GetCompilation<T>(configuration.OutputKind, syntaxTrees);

        //public static (Compilation compilation, ImmutableArray<Diagnostic> inputDiagnostics)
        //    RunGenerator<T>(Compilation inputCompilation)
        //    where T : IIncrementalGenerator, new()
        //{
        //    var generator = new T();
        //    return RunGenerator(inputCompilation, generator);
        //}

        public static void OutputGeneratedTrees(Compilation generatedCompilation, string outputDir)
        {
            // the presence of a file path is used to indicate generated. this feels weak.
            foreach (var tree in generatedCompilation.SyntaxTrees)
            {
                if (string.IsNullOrWhiteSpace(tree.FilePath))
                { continue; }
                var fileName = Path.Combine(outputDir, Path.GetFileName(tree.FilePath));
                File.WriteAllText(fileName, tree.ToString());
            }
        }

        public static Process? CompileOutput(string testInputPath)
        {
            ProcessStartInfo startInfo = new()
            {
                ////startInfo.CreateNoWindow = false;
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = testInputPath,
                FileName = "dotnet",
                Arguments = "build"
            };
            Process? exeProcess = Process.Start(startInfo);
            Assert.NotNull(exeProcess);
            if (exeProcess is not null)
            {
                exeProcess.WaitForExit(30000);
            }

            return exeProcess;
        }

        public static string? RunGeneratedProject(string arguments, string setName, string buildPath)
        {

            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var fieName = Path.Combine(buildPath, setName);

            startInfo.FileName = $"{fieName}{IfOsIsWindows(".exe", "")}";
            startInfo.Arguments = arguments;

            Process? exeProcess = Process.Start(startInfo);
            Assert.NotNull(exeProcess);
            if (exeProcess is not null)
            {
                exeProcess.WaitForExit();

                var output = exeProcess.StandardOutput.ReadToEnd();
                var error = exeProcess.StandardError.ReadToEnd();

                Assert.Equal(0, exeProcess.ExitCode);
                Assert.Equal("", error);
                return output;
            }
            return null;
        }

        public static (Compilation outputCompilation, ImmutableArray<Diagnostic> outputDiagnostics)
            RunGenerator<T>(Compilation compilation, T generator)
            where T : IIncrementalGenerator, new()
                {
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            return (outputCompilation, diagnostics);
        }
    }
}
