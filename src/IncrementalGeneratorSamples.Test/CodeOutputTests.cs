//using IncrementalGeneratorSamples.InternalModels;
//using Xunit;

//namespace IncrementalGeneratorSamples.Test
//{
//    public class CodeOutputTests
//    {
//        private CancellationToken cancellationToken = new CancellationTokenSource().Token;

//        private string expectedCommandOutput = @"
//using System.CommandLine;
//using System.CommandLine.Invocation;
//using IncrementalGeneratorSamples.Runtime;

//#nullable enable

//namespace TestExample;

//public partial class ReadFile
//{
//    internal class CommandHandler : CommandHandler<CommandHandler>
//    {
//        Option<FileInfo?> fileOption = new Option<FileInfo?>(""--file"", ""The file to read and display on the console"");
//        Option<int> delayOption = new Option<int>(""--delay"", ""Delay between lines, specified as milliseconds per character in a line."");

//        public CommandHandler()
//            : base(""read-file"", """")
//        {
//            SystemCommandLineCommand.AddOption(fileOption);
//            SystemCommandLineCommand.AddOption(delayOption);
//        }

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override int Invoke(InvocationContext invocationContext)
//        {
//            var commandResult = invocationContext.ParseResult.CommandResult;
//            var command = new ReadFile(GetValueForSymbol(fileOption, commandResult), GetValueForSymbol(delayOption, commandResult));
//            return command.DoWork();
//        }

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override Task<int> InvokeAsync(InvocationContext invocationContext)
//        {
//            // Since this method is not implemented in the user source, we do not implement it here.
//            throw new NotImplementedException();
//        }
//    }
//}
//";

//        private string expectedRootCommandOutput = @"
//using System.CommandLine.Invocation;
//using IncrementalGeneratorSamples.Runtime;

//#nullable enable

//namespace TestExample;

//public partial class RootCommand
//{
//    public static void Invoke(string[] args)
//        => CommandHandler.Invoke(args);

//    internal class CommandHandler : RootCommandHandler<CommandHandler>
//    {
//        public CommandHandler() : base(string.Empty)
//        {
//            SystemCommandLineCommand.Add(ReadFile.CommandHandler.GetHandler().SystemCommandLineCommand);
//            SystemCommandLineCommand.Add(AddLine.CommandHandler.GetHandler().SystemCommandLineCommand);
//        }

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override int Invoke(InvocationContext invocationContext)
//        {
//            Console.WriteLine(""Enter one of the commands. Use '-h' to get a list of available commands"");
//            return 1;
//        }

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override Task<int> InvokeAsync(InvocationContext invocationContext)
//        {
//            // Since this method is not implemented in the user source, we do not implement it here.
//            throw new NotImplementedException();
//        }
//    }
//}
//";

//        [Fact]
//        public void Should_have_name_and_description()
//        {
//            var model = new CommandModel("ReadFile", description: "This is a description", options: Enumerable.Empty<OptionModel>());
//            var output = CodeOutput.FileName(model);

//            Assert.Equal("ReadFile.g.cs", output);

//        }

//        [Fact]
//        public void Should_output_command_code()
//        {
//            var properties = new List<OptionModel>
//            {
//                new OptionModel("File","FileInfo?","The file to read and display on the console"),
//                new OptionModel("Delay","int","Delay between lines, specified as milliseconds per character in a line."),
//            };
//            var model = new CommandModel("ReadFile", description: "", options: properties);
//            var output = CodeOutput.GenerateCommandCode(model, cancellationToken);

//            Assert.Equal(expectedCommandOutput.Replace("\r\n", "\n"), output.Replace("\r\n", "\n"));

//        }

//        [Fact]
//        public void Should_output_rootcommand_code()
//        {
//            var commands = new List<CommandModel>
//            {
//                new CommandModel("ReadFile",description: "Output dile to the console.", options: Enumerable.Empty<OptionModel>()),
//                new CommandModel("AddLine",description: "Add a line to a file.", options: Enumerable.Empty<OptionModel>())
//            };
//            var output = CodeOutput.GenerateRootCommandCode(commands, cancellationToken);

//            Assert.Equal(expectedRootCommandOutput.Replace("\r\n", "\n"), output.Replace("\r\n", "\n"));

//        }

//    }
//}
