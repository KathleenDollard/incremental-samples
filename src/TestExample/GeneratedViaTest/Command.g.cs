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
        private Option<FileInfo?> fileOption;
        private Option<int> delayOption;

        public CommandHandler()
        {
            fileOption = new Option<FileInfo?>("--file", "The file to read and display on the console.");
            RootCommand.AddOption(fileOption);
            delayOption = new Option<int>("--delay", "Delay between lines, specified as milliseconds per character in a line.");
            RootCommand.AddOption(delayOption);
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new Command(GetValueForSymbol(fileOption, commandResult), GetValueForSymbol(delayOption, commandResult));
            return command.DoWork();
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }
    }

}
