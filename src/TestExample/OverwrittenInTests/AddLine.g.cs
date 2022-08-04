using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class AddLine
{
    internal class CommandHandler : CommandHandlerBase
    {
        // Manage singleton instance
        public static AddLine.CommandHandler Instance { get; } = new AddLine.CommandHandler();

        // Create System.CommandLine options
        private Option<System.IO.FileInfo?> fileOption = new Option<System.IO.FileInfo?>("--file", "The file to read and display on the console.");
        private Option<string> lineOption = new Option<string>("--line", "Delay between lines, specified as milliseconds per character in a line.");

        // Base constructor creates System.CommandLine.Command and options are added here
        private CommandHandler()
            : base("add-line", "Add a new line to a file.")
        {
            Command.AddOption(fileOption);
            Command.AddOption(lineOption);
        }

        // The code invoked when the user runs the command
        protected override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new AddLine(GetValueForSymbol(fileOption, commandResult), GetValueForSymbol(lineOption, commandResult));
            return command.Execute();
        }
    }
}
