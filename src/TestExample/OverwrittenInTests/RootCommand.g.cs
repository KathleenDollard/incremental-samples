using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public class RootCommand
{
    internal class CommandHandler : RootCommandHandler<CommandHandler>
    {
        // Manage singleton instance
        public static RootCommand.CommandHandler Instance = new RootCommand.CommandHandler();

        public CommandHandler() : base(string.Empty)
        {
            Command.Add(ReadFile.CommandHandler.Instance.Command);
            Command.Add(AddLine.CommandHandler.Instance.Command);
        }
    }
}
