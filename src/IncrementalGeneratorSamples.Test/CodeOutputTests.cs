using IncrementalGeneratorSamples.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Test
{
    public class CodeOutputTests
    {
        private string expectedOutput = @"
using System.CommandLine;
using System.CommandLine.Invocation;

#nullable enable

namespace TestExample;

public partial class Command
{
    public Command(FileInfo? file, int delay)
    {
        File = file;
        Delay = delay;
    }

    public static void Invoke(string[] args)
        => CommandHandler.Invoke(args);

    internal class CommandHandler : IncrementalGeneratorSamples.Runtime.CommandHandler<CommandHandler>
    {
        Option<FileInfo?> fileOption;
        Option<int> delayOption;

        public CommandHandler()
        {
            fileOption = new Option<FileInfo?>(""file"", ""The file to read and display on the console"");
            delayOption = new Option<int>(""delay"", ""Delay between lines, specified as milliseconds per character in a line."");
            RootCommand.AddOption(fileOption);
            RootCommand.AddOption(delayOption);
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new Command(GetValueForSymbol(fileOption, commandResult), GetValueForSymbol(delayOption, commandResult));
            return command.DoWork();
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }
    }

}
";

        [Fact]
        public void Should_output_code()
        {
            var properties = new List<OptionModel>
            {
                new OptionModel("File","FileInfo?","The file to read and display on the console"),
                new OptionModel("Delay","int","Delay between lines, specified as milliseconds per character in a line."),
            };
            var model = new GenerationModel("Command", properties);
            var output = CodeOutput.GeneratedCode(model);

            Assert.Equal( expectedOutput.Replace("\r\n","\n"), output.Replace("\r\n", "\n"));

        }

        [Fact]
        public void Should_have_name()
        {
            var properties = new List<OptionModel>
            {
                new OptionModel("File","FileInfo?","The file to read and display on the console"),
                new OptionModel("Delay","int","Delay between lines, specified as milliseconds per character in a line."),
            };
            var model = new GenerationModel("Command", Enumerable.Empty<OptionModel>());
            var output = CodeOutput.FileName(model);

            Assert.Equal("Command.g.cs", output);

        }
    }
}
