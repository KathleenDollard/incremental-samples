using IncrementalGeneratorSamples.Test;
using Microsoft.CodeAnalysis;
using System.Reflection.Emit;

namespace IncrementalGeneratorSamples.IntegrationTests
{
    public class GenerateAndRunTests
    {
        internal static string examplePath = Path.Combine(Environment.CurrentDirectory, @$"../../../../TestExample");

        private Compilation outputCompilation;
        private IEnumerable<SyntaxTree> generatedSyntaxTrees;
        
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
            Assert.Equal(5, outputTrees.Count());

            this.outputCompilation = outputCompilation;
            generatedSyntaxTrees = outputTrees;

            OutputFiles(generatedSyntaxTrees);
        }

        private static void OutputFiles(IEnumerable<SyntaxTree> generatedSyntaxTrees)
        {
            foreach(var tree in generatedSyntaxTrees)
            {
                var filePath = Path.Combine(examplePath,
                                            "OverwrittenInTests",
                                            Path.GetFileName(tree.FilePath));
                File.WriteAllText(filePath, tree.ToString());
            }
        }

        [Fact]
        public void Can_compile_generated_code()
        {
            var (exitCode, output, err) = IntegrationHelpers.TestOutputCompiles(examplePath);
            Assert.DoesNotContain("error", output);
            Assert.Empty(err);  // dotnet puts errors in standard out, so this is only helpful if that changes.
            Assert.Equal(0,exitCode);
        }
    }
}