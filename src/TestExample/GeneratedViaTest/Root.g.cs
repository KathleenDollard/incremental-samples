using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class Root
{
    internal class CommandHandler : RootCommandHandler<CommandHandler>
    {
        public CommandHandler()
        {
            SystemCommandLineCommand.Add(AddLine.CommandHandler.GetHandler().SystemCommandLineCommand);
            SystemCommandLineCommand.Add(ReadFile.CommandHandler.GetHandler().SystemCommandLineCommand);
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {
            Console.WriteLine("Enter one of the commands. Use '-h' to get a list of available commands");
            return 1;
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
