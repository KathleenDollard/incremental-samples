using IncrementalGeneratorSamples.Runtime;
using System.CommandLine;

namespace IncrementalGeneratorSamples
{
    internal partial class Cli
    {
        static partial void SetRootCommand()
        {
            var rootHandler = CommandHandler.Instance;
            rootCommand = rootHandler.RootCommand;
        }

        internal class CommandHandler : RootCommandHandler<CommandHandler>
        {
            // Manage singleton instance
            public static CommandHandler Instance = new CommandHandler();

            public CommandHandler() : base(string.Empty)
            {
                Command.Add(TestExample.ReadFile.CommandHandler.Instance.Command);
                Command.Add(TestExample.AddLine.CommandHandler.Instance.Command);
            }
        }
    }
}
