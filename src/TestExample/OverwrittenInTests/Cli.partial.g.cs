namespace IncrementalGeneratorSamples
{
    internal partial class Cli
    {
        static partial void SetRootCommand()
        {
            var rootHandler = TestExample.RootCommand.CommandHandler.Instance;
            rootCommand = rootHandler.RootCommand;
        }
    }
}
