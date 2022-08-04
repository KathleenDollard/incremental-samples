using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples.Test
{
    public class IntegrationTests
    {
        private readonly IntegrationTestFromSourceConfiguration withMultipleProperties;

        public IntegrationTests()
        {
            withMultipleProperties =
            new("TestOutputSimpleFileRead")
            {
                OutputKind = OutputKind.ConsoleApplication,
            };
            var commandClass = @"
using IncrementalGeneratorSamples.Runtime;

namespace MyNamespace
{
    [Command]
    public class WithMultipleProperties
    {
        public string? PropertyOne{ get; set; }
        public int PropertyTwo{ get; set; }
        public FileInfo? PropertyThree{ get; set; }
    }
}";
           
            var mainMethod = @"
namespace MyNamespace;

internal class Program
{
    private static void Main(string[] args)
    {
        Cli.Invoke(args);
    }
}";

            withMultipleProperties.AddSource(commandClass);
            withMultipleProperties.AddSource(mainMethod);
        }

        [Fact] // (Skip ="froze VS")]
#pragma warning disable IDE1006 // the weird naming is because I really want this to appear first in the set 
        public void _GeneratorHealthCheck() 
#pragma warning restore IDE1006
        {
            var inputCompilation = TestHelpersCommon.GetInputCompilation<Generator>(
                    withMultipleProperties.OutputKind,
                    withMultipleProperties.InputSourceCode);
            Assert.Empty(TestHelpersCommon.WarningAndErrors(inputCompilation.GetDiagnostics())
                        .Where(x=>x.Id != "CS0103")); // expected because no Cli class 
            var (outputCompilation, driverResults) = TestHelpersCommon.Generate<Generator>(inputCompilation);
            Assert.Empty(TestHelpersCommon.WarningAndErrors(driverResults.Diagnostics));
            var outputSyntaxTree = driverResults
                                    .GeneratedTrees
                                    .Single(x=> !(new string[] { "Cli.g.cs", "Cli.Partial.g.cs","Root.g.cs" }.Contains(Path.GetFileName(x.FilePath))));
            var output = outputSyntaxTree.ToString();
            Assert.True( output.Length > 200);
        }

//        var expected = $@"
//using System.CommandLine;
//using System.CommandLine.Invocation;
//using IncrementalGeneratorSamples.Runtime;

//#nullable enable

//namespace MyNamespace;

//public partial class WithMultipleProperties
//{{
//    internal class CommandHandler : CommandHandlerBase
//    {{
//        // Manage singleton instance
//        public static WithMultipleProperties.CommandHandler Instance {{ get; }} = new WithMultipleProperties.CommandHandler();

//        // Create System.CommandLine options
//        private Option<string?> propertyOneOption = new Option<string?>(""--property-one"", """");
//        private Option<int> propertyTwoOption = new Option<int>(""--property - two"", """");
//        private Option<System.IO.FileInfo?> propertyThreeOption = new Option<System.IO.FileInfo?>(""--property - three"", """");

//        // Base constructor creates System.CommandLine and optins are added here
//        private CommandHandler()
//            : base(""with - multiple - properties"", """")
//        {{
//            {{

//                Command.AddOption(propertyOneOption);
//                Command.AddOption(propertyTwoOption);
//                Command.AddOption(propertyThreeOption);
//            }}
//        }}

//        // The code invoked when the user runs the command
//        protected override int Invoke(InvocationContext invocationContext)
//        {{
//            {{
//                var commandResult = invocationContext.ParseResult.CommandResult;
//                var command = new WithMultipleProperties(GetValueForSymbol(propertyOneOption, commandResult), GetValueForSymbol(propertyTwoOption, commandResult), GetValueForSymbol(propertyThreeOption, commandResult));
//                return command.Execute();
//            }}
//        }}
//    }}
//}}
//";


        //[Fact]
        //public void GenerateProjectFiles()
        //{
        //    TestHelpers.GenerateIntoProject<Generator>(testOutputExampleConfiguration);
        //    var output = TestHelpers.RunCommand<Generator>(testOutputExampleConfiguration,
        //                                                          "star-trek --uhura");
        //    Assert.Equal($"Hello, Nyota Uhura{Environment.NewLine}", output);
        //}
    }
}
