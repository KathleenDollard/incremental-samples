using IncrementalGeneratorSamples.Test;
using Microsoft.CodeAnalysis;
using System.Reflection.Emit;

namespace IncrementalGeneratorSamples.IntegrationTests
{
    public class GenerateAndRunTests
    {
        internal static string examplePath = Path.Combine(Environment.CurrentDirectory, @$"../../../../TestExample");

        private Compilation outputCompilation;
        
        public GenerateAndRunTests()
        { 
            var (inputCompilation, inputDiagnostics) = TestHelpers.GetInputCompilation<Generator>(
                OutputKind.ConsoleApplication,
                File.ReadAllText(Path.Combine(examplePath, "Commands.cs")));
            Assert.NotNull(inputCompilation);
            Assert.Empty(inputDiagnostics);

            var (outputCompilation, outputTrees, outputDiagnostics) = TestHelpers.GenerateTrees<Generator>(inputCompilation);
            Assert.NotNull(outputCompilation);
            Assert.Empty(outputDiagnostics);
            Assert.Equal(3, outputTrees.Count());

            this.outputCompilation = outputCompilation;
        }

        [Fact]
    }
}