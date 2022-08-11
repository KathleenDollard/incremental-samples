using BenchmarkDotNet.Attributes;
using IncrementalGeneratorSamples;
using IncrementalGeneratorSamples.InternalModels;
using IncrementalGeneratorSamples.Test;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Benchmarks
{
    public class ExtractData
    {
        private readonly string InputCode;
        private readonly string XmlDocs;
        private readonly ISymbol Symbol;
        private readonly CancellationToken CancellationToken = new CancellationTokenSource().Token;

        public ExtractData()
        {
            InputCode = new WithMultipleProperties().InputSourceCode;
            XmlDocs = new WithXmlDescriptions().InitialClassModel?.XmlComments ?? "";
            var (symbol, cancellationToken, inputDiagnostics) = 
                    TestHelpers.GetSymbolForSynatax<ClassDeclarationSyntax>(InputCode, x => x.Identifier.ToString() == "MyClass");
            Symbol = symbol is null
                    ? throw new InvalidOperationException("symbol not found")
                    : symbol;
        }

        [Benchmark]
        public InitialClassModel GetInitialClassModel() => ModelBuilder.GetInitialModel(Symbol, CancellationToken);

        [Benchmark]
        public string DescriptionFromXmlDocs() => Helpers.GetXmlDescription(XmlDocs);
    }
}
