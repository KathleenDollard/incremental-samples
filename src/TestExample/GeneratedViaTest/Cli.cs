using System.CommandLine;

namespace TestExample
{
    internal partial class Cli
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private static System.CommandLine.RootCommand? rootCommand = null;
#pragma warning restore IDE0044 // Add readonly modifier

        public static void Invoke(string[] args)
        {
            SetRootCommand();
            if (rootCommand is null)
            { throw new InvalidOperationException("No classes were mared with the [Command] attribute"); }
            rootCommand.Invoke(args);
        }

        static partial void SetRootCommand();
    }
}
