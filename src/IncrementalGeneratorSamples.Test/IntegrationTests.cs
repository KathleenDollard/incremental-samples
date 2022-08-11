using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples.Test
{
    public class IntegrationTests
    {
        private readonly string mainMethod = @"
namespace MyNamespace;

internal class Program
{
    private static void Main(string[] args)
    {
        Cli.Invoke(args);
    }
}";

        private static string DotnetVersion = "";
        private static string TestInputPath(string testSetName, string currentPath) 
            => Path.Combine(currentPath, @$"../../../../{testSetName}");
        private static string GeneratedSubDirectoryName = "GeneratedViaTest";
        private static string TestBuildPath(string testInputPath, string dotnetVersion) 
            => Path.Combine(testInputPath, "bin", "Debug", dotnetVersion);
        private static string ProgramFilePath(string testInputPath) 
            => Path.Combine(testInputPath, "Program.cs");


        [Fact]
#pragma warning disable IDE1006 // the weird naming is because I really want this to appear first in the set 
        public void _GeneratorHealthCheck()
#pragma warning restore IDE1006
        {
            // Get input compilation and ensure it has not errors
            var inputCompilation = TestHelpers.GetInputCompilation<Generator>(
                    outputKind: OutputKind.ConsoleApplication,
                    explicitUsings: null,
                    TestData.GetData<SimplestPractical>().InputSourceCode,
                         mainMethod);
            Assert.Empty(TestHelpers.WarningAndErrors(inputCompilation.GetDiagnostics())
                        .Where(x => x.Id != "CS0103")); // expected because no Cli class 

            // Generate and ensure there are no generation errors
            var (outputCompilation, driverResults) = TestHelpers.Generate<Generator>(inputCompilation);
            Assert.Empty(TestHelpers.WarningAndErrors(driverResults.Diagnostics));

            // Check output
            var outputSyntaxTree = driverResults
                                    .GeneratedTrees
                                    .FirstOrDefault(x => x.FilePath.EndsWith("Cli.g.cs"));
            Assert.NotNull(outputSyntaxTree);
            Assert.Equal(CodeOutput.ConsistentCli, outputSyntaxTree!.ToString());
        }


        [Theory]
        [InlineData("SimpleFileRead")]

#pragma warning disable IDE1006 // the weird naming is because I really want this to appear first in the set 
        public Task SimpleFileRead(string testSetName)
#pragma warning restore IDE1006
        {
            var testInputPath = TestInputPath(testSetName, Environment.CurrentDirectory);
            var exeCompile = TestHelpers.CompileOutput(testInputPath);
            Assert.NotNull(exeCompile);
            Assert.True(exeCompile!.HasExited);
            Assert.Equal(0, exeCompile.ExitCode);

            var exeOutput = TestHelpers.RunCommand(TestBuildPath(testInputPath, DotnetVersion),testSetName, "-h");
            return Verifier.Verify(exeOutput).UseDirectory("Snapshots").UseTextForParameters("Run SimpleFileRead");

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
