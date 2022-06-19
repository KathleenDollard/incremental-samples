namespace TestExample
{
    internal partial class Cli
    {
        static partial void SetRootCommand()
        {
            var rootHandler = Root.CommandHandler.GetHandler();
            rootCommand = rootHandler.SystemCommandLineRoot;
        }
    }
}
