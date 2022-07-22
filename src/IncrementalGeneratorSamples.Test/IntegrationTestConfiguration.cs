using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples.Test
{
    public abstract class IntegrationTestConfigurationBase
    {
        internal static string currentPath = Environment.CurrentDirectory;

        protected IntegrationTestConfigurationBase(string testSetName)
        {
            TestSetName = testSetName;
            DotnetVersion = "net6.0";
            OutputKind = OutputKind.DynamicallyLinkedLibrary;
        }

        public string DotnetVersion { get; set; }
        public OutputKind OutputKind { get; set; }
        public int SyntaxTreeCount { get; set; }

        public string TestSetName { get; }
    }
    
    public class IntegrationTestFromSourceConfiguration : IntegrationTestConfigurationBase
    {
        public IntegrationTestFromSourceConfiguration(string testSetName)
            : base (testSetName)
        {
            InputData = Enumerable.Empty<TestData>();
        }

        public IEnumerable<TestData> InputData { get; set; }
    }

    public class IntegrationTestFromPathConfiguration : IntegrationTestConfigurationBase
    {
        public IntegrationTestFromPathConfiguration(string testSetName)
            : base(testSetName)
        {
            TestInputPath = Path.Combine(currentPath, @$"../../../../{TestSetName}");
            TestGeneratedCodePath = Path.Combine(TestInputPath, "GeneratedViaTest");
            TestBuildPath = Path.Combine(TestInputPath, "bin", "Debug", DotnetVersion);
            ProgramFilePath = Path.Combine(TestInputPath, "Program.cs");
        }

        public string TestInputPath { get; set; }
        public string TestGeneratedCodePath { get; set; }
        public string TestBuildPath { get; set; }
        public string ProgramFilePath { get; set; }
    }
}
