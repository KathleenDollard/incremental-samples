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
        public int ExpectedSyntaxTreeCount { get; set; }

        public string TestSetName { get; }
    }
    
    public class IntegrationTestFromSourceConfiguration : IntegrationTestConfigurationBase
    {
        public IntegrationTestFromSourceConfiguration(string testSetName)
            : base (testSetName)
        {
            InputData = Enumerable.Empty<TestData>();
        }

        private List<string> ExtraSources = new();

        public IEnumerable<TestData> InputData { get; set; }

        public string[] InputSourceCode => InputData    
                                            .Select(x=>x.InputSourceCode).ToArray()
                                            .Union(ExtraSources) 
                                            .ToArray();
        public void AddSource(string extraSource)
            => ExtraSources.Add(extraSource);   

    }

    public class IntegrationTestFromPathConfiguration : IntegrationTestConfigurationBase
    {
        public IntegrationTestFromPathConfiguration(string testSetName)
            : base(testSetName)
        {
            ExecutableName = testSetName;
            TestInputPath = Path.Combine(currentPath, @$"../../../../{TestSetName}");
            GeneratedSubDirectoryName = "GeneratedViaTest";
            TestBuildPath = Path.Combine(TestInputPath, "bin", "Debug", DotnetVersion);
            ProgramFilePath = Path.Combine(TestInputPath, "Program.cs");
        }

        public string ExecutableName { get; set; }
        public string TestInputPath { get; set; }
        public string GeneratedSubDirectoryName { get; set; }
        public string TestBuildPath { get; set; }
        public string ProgramFilePath { get; set; }
        public string TestGeneratedCodePath => Path.Combine(TestInputPath, GeneratedSubDirectoryName);
    }
}
