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
                File.ReadAllText(Path.Combine(examplePath, "Commands.cs")),
                File.ReadAllText(Path.Combine(examplePath, "Program.cs")));
            // We need to ignore the diagnostic that points to code we will always generate (PostIniti...)
            Assert.NotNull(inputCompilation);
            Assert.Single(inputDiagnostics.Where(diagnostic => diagnostic.Id == "CS0103"));
            Assert.Empty(inputDiagnostics.Where(diagnostic => diagnostic.Id != "CS0103"));

            var (outputCompilation, outputTrees, outputDiagnostics) = TestHelpers.GenerateTrees<Generator>(inputCompilation);
            Assert.NotNull(outputCompilation);
            Assert.Empty(outputDiagnostics);
            Assert.Equal(3, outputTrees.Count());

            this.outputCompilation = outputCompilation;
        }

        [Fact]
        public void Can_compile_generated_code()
        {
            var process = IntegrationHelpers.TestOutputCompiles(examplePath);
            var output = process?.StandardOutput.ReadToEnd();
            // dotnet puts errors in standard out, so use that to debug.
        }
    }
}