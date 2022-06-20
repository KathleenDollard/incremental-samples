
namespace TestExample
{
    internal partial class Cli
    {
        static partial void SetRootCommand()
        {
            var rootHandler = RootCommand.CommandHandler.GetHandler();
            rootCommand = rootHandler.SystemCommandLineRoot;
        }
    }
}
