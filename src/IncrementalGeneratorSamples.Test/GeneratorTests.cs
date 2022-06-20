using Microsoft.CodeAnalysis;


namespace IncrementalGeneratorSamples.Test
{
    public class GeneratorTests
    {
        private const string SimpleClass = @"
using IncrementalGeneratorSamples.Runtime;

[Command]
public partial class ReadFile
{
    public int Delay { get;  }
}
";

        [Fact]
        public void Can_generate_test()
        {
            var (inputCompilation, inputDiagnostics) = TestHelpers.GetInputCompilation<Generator>(OutputKind.DynamicallyLinkedLibrary, SimpleClass);
            Assert.NotNull(inputCompilation);
            Assert.Empty(inputDiagnostics);
            var (outputCompilation, trees, outputDiagnostics) = TestHelpers.GenerateTrees<Generator>(inputCompilation);
            Assert.NotNull(outputCompilation);
            Assert.Empty(outputDiagnostics);
            Assert.Equal(4,trees.Count());
        }
    }
}
