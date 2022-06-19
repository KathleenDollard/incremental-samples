using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Runtime
{
    public abstract class CommandHandler<TCommandHandler> : ICommandHandler
        where TCommandHandler : CommandHandler<TCommandHandler>, new()
    {

        public static CommandHandler<TCommandHandler> GetHandler()
            => new TCommandHandler();

        protected CommandHandler(string name, string description = "")
        {
            SystemCommandLineCommand = new Command(name, description)
            {
                Handler = this
            };
        }

        public Command SystemCommandLineCommand { get; private set; }

        public static int Invoke(string[] args)
            => new TCommandHandler().SystemCommandLineCommand.Invoke(args);

        public static async Task<int> InvokeAsync(string[] args)
            => await new TCommandHandler().SystemCommandLineCommand.InvokeAsync(args);

        protected static TSymbol GetValueForSymbol<TSymbol>(IValueDescriptor<TSymbol> symbol, CommandResult result)
            => symbol switch
            {
                // nullable warnings are ignored because GetValueForArgument returns the default for the option
                Argument<TSymbol> argument => result.GetValueForArgument(argument)!,
                Option<TSymbol> option => result.GetValueForOption(option)!,
                _ => throw new ArgumentOutOfRangeException()
            };

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public abstract int Invoke(InvocationContext invocationContext);

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
        public abstract Task<int> InvokeAsync(InvocationContext invocationContext);


    }
}
