using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class ReadFile
{
    internal class CommandHandler : CommandHandlerBase
    {
        // Manage singleton instance
        public static ReadFile.CommandHandler Instance { get; } = new ReadFile.CommandHandler();

        // Create System.CommandLine options
        private Option<System.IO.FileInfo?> fileOption = new Option<System.IO.FileInfo?>("--file", "The file to read and display on the console.");

        // Base constructor creates System.CommandLine and optins are added here
        private CommandHandler()
            : base("read-file", "")
        {
            Command.AddOption(fileOption);
        }

        // The code invoked when the user runs the command
        protected override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new ReadFile(GetValueForSymbol(fileOption, commandResult));
            return command.Execute();
        }
    }
}
