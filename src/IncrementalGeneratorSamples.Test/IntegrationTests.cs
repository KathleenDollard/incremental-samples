using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Test
{
    public class IntegrationTests
    {
        private readonly IntegrationTestConfiguration testOutputExampleConfiguration =
            new("TestOutputSimpleFileRead")
            {
                OutputKind = OutputKind.ConsoleApplication
            };

        [Fact]
        public void _PipelineCheck() // the weird naming is because I really want this to appear first in the set 
        {
            var (inputCompilation, inputDiagnostics) = TestHelpers.GetInputCompilation(
                TestHelpers.Generate<Generator >()
        }

        [Fact]
        public void GenerateProjectFiles()
        {
            TestHelpers.GenerateIntoProject<Generator>(testOutputExampleConfiguration);
            var output = TestHelpers.RunCommand<Generator>(testOutputExampleConfiguration,
                                                                  "star-trek --uhura");
            Assert.Equal($"Hello, Nyota Uhura{Environment.NewLine}", output);
        }
    }
}
