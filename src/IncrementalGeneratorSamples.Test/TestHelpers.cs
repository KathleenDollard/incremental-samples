﻿
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace IncrementalGeneratorSamples.Test;

public class TestHelpers
{

    public static (Compilation inputCompilation, IEnumerable<Diagnostic> inputDiagnostics) GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
    {
        var inputCompilation = CreateInputCompilation<TGenerator>(outputKind, code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray());
        var inputDiagnostics = inputCompilation.GetDiagnostics()
            .Where(x => x.Severity == DiagnosticSeverity.Error || x.Severity == DiagnosticSeverity.Warning);
        return (inputCompilation, inputDiagnostics);
    }

    public static (Compilation compilation, IEnumerable<SyntaxTree> outputTrees, IEnumerable<Diagnostic> outputDiagnostics) GenerateTrees<TGenerator>(Compilation inputCompilation)
        where TGenerator : IIncrementalGenerator, new()
    {
        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var compilation, out var _);

        // REVIEW: Is there a reason to use the out results over the run results?
        var runResult = driver.GetRunResult();
        var outputDiagnostics = runResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error || x.Severity == DiagnosticSeverity.Warning);
        return (compilation, runResult.GeneratedTrees, outputDiagnostics);
    }

    public static (Compilation compilation, IEnumerable<string> output, IEnumerable<Diagnostic> outputDiagnostics) Generate<TGenerator>(Compilation inputCompilation)
        where TGenerator : IIncrementalGenerator, new()
    {
        var (compilation, trees, diagnostics) = GenerateTrees<TGenerator>(inputCompilation);
        return (compilation, trees.Select(x => x.ToString()), diagnostics);
    }

    private static Compilation CreateInputCompilation<TGenerator>(OutputKind outputKind, SyntaxTree[] syntaxTrees)
    {
        // REVIEW: Is there a better way to get the references
        System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var references = assemblies
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(TGenerator).Assembly.Location),
                //MetadataReference.CreateFromFile(typeof(EnumExtensionsAttribute).Assembly.Location)
            });
        return CSharpCompilation.Create("compilation",
                    syntaxTrees,
                    references,
                    new CSharpCompilationOptions(outputKind));
    }
}

