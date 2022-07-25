using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;

namespace IncrementalGeneratorSamples.Test;

public class TestHelpersCommon
{
    private static readonly string[] commonUsings = new string[]
    {
       "System.IO",
       "System.Collections.Generic",
       "System.Linq",
       "System" };

    public static CancellationToken CancellationTokenForTesting => new CancellationTokenSource().Token;

    public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
        => GetInputCompilation<TGenerator>(outputKind, code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray());
    public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, string[] commonUsings, params string[] code)
        => GetInputCompilation<TGenerator>(outputKind, commonUsings, code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray());
    public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params SyntaxTree[] syntaxTrees)
        => GetInputCompilation<TGenerator>(outputKind, commonUsings, syntaxTrees);
    public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, string[] commonUsings, params SyntaxTree[] syntaxTrees)
    {
        // Create the initial syntax tree, add using statements, and get the updated tree
        var newUsings = commonUsings.Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x))).ToArray();

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

    public static void GenerateAndCompileProject<T>(string testInputPath, string testGeneratedCodePath, string generatedSubDirectoryName, OutputKind outputKind)
        where T : IIncrementalGenerator, new()
    {
        SyntaxTree[] syntaxTrees = GetSyntaxTrees(testInputPath, generatedSubDirectoryName);
        var inputCompilation = GetInputCompilation<T>(outputKind, syntaxTrees);
        var inputDiagnostics = inputCompilation.GetDiagnostics();
        // CS0103: "The name 'identifier' does not exist in the current context" is probably because this code is incomplete
        CheckCompilation(inputCompilation, inputDiagnostics, diagnosticFilter: x => x.Id != "CS0103");

        var (outputCompilation, runResult) = Generate<T>(inputCompilation);
        var outputDiagnostics = runResult.Diagnostics;
        CheckCompilation(outputCompilation, outputDiagnostics);

        OutputGeneratedTrees(outputCompilation, testGeneratedCodePath);
        var exeProcess = CompileOutput(testInputPath);
        Assert.NotNull(exeProcess);
        Assert.True(exeProcess!.HasExited);

        var output = exeProcess.StandardOutput.ReadToEnd(); // available for debugging - can be a pain to get in VS
        var error = exeProcess.StandardError.ReadToEnd();
        Console.WriteLine(output);
        Assert.Equal(0, exeProcess.ExitCode);
        Assert.Equal("", error);
    }

    public static string? RunCommand(string testBuildPath, string executableName, string arguments)
    {

        ProcessStartInfo startInfo = new()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var fileName = Path.Combine(testBuildPath, executableName);

        startInfo.FileName = $"{fileName}{IfOsIsWindows(".exe", "")}";
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
        exeProcess?.WaitForExit(30000);

        return exeProcess;
    }

    public static string IfOsIsWindows(string windowsString, string unixString)
         => Environment.OSVersion.Platform == PlatformID.Unix
             ? unixString
             : windowsString;

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

    public static void CheckCompilation(Compilation compilation,
                                         IEnumerable<Diagnostic> diagnostics,
                                         Func<Diagnostic, bool>? diagnosticFilter = null)
    {
        Assert.NotNull(compilation);
        var filteredDiagnostics = diagnosticFilter is null
            ? WarningAndErrors(diagnostics)
            : WarningAndErrors(diagnostics).Where(diagnosticFilter);
        Assert.Empty(filteredDiagnostics);
    }

    public static SyntaxTree[] GetSyntaxTrees(string testInputPath, string generatedSubDirectoryName)
    {
        var direcotryOptions = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            MatchCasing = MatchCasing.PlatformDefault,
            MatchType = MatchType.Simple,
        };

        var directories = Directory
            .GetDirectories(testInputPath, "*", direcotryOptions)
            .Where(x => !x.Contains(generatedSubDirectoryName));
        var files = directories.SelectMany(x => Directory.GetFiles(x, "*.cs"));

        return files
            .Select(fileName => TreeFromFileInInputPath(NameWithExtension(fileName))).ToArray();

        string NameWithExtension(string fileName)
            => fileName.EndsWith(".cs")
                ? Path.Combine(testInputPath, fileName)
                : Path.Combine(testInputPath, fileName + ".cs");

        SyntaxTree TreeFromFileInInputPath(string fileName)
            => CSharpSyntaxTree.ParseText(File.ReadAllText(fileName));
    }

    public static IEnumerable<Diagnostic> WarningAndErrors(IEnumerable<Diagnostic> diagnostics)
        => diagnostics.Where(
          x => x.Severity == DiagnosticSeverity.Error ||
               x.Severity == DiagnosticSeverity.Warning);


}
