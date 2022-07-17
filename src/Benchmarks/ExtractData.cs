using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using IncrementalGeneratorSamples;
using IncrementalGeneratorSamples.InternalModels;
using IncrementalGeneratorSamples.Test;
using Microsoft.CodeAnalysis;

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
            InputCode = new WithMulitipeProperties().InputSourceCode;
            XmlDocs = new WithXmlDescripions().InitialClassModel.XmlComments;
            var (_, symbol, _, cancellationToken, inputDiagnostics) = TestHelpers.GetTransformInfo(InputCode, x => x.Identifier.ToString() == "MyClass");
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
