using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
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

        protected CommandHandler(Command command)
        {
            command.Handler = this;
            SystemCommandLineCommand = command;
        }

        public Command SystemCommandLineCommand { get; private set; }

        public static int Invoke(string[] args)
            => new TCommandHandler().SystemCommandLineCommand.Invoke(args);

        public static async Task<int> InvokeAsync(string[] args)
            => await new TCommandHandler().SystemCommandLineCommand.InvokeAsync(args);

        protected static TSymbol GetValueForSymbol<TSymbol>(IValueDescriptor<TSymbol> symbol, CommandResult result)
        {
            if (symbol is Argument<TSymbol> argument)
            { return result.GetValueForArgument(argument); }
            if (symbol is Option<TSymbol> option)
            { return result.GetValueForOption(option); }
            throw new ArgumentOutOfRangeException();
        }

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

    public abstract class RootCommandHandler<TCommandHandler> : CommandHandler<TCommandHandler>
        where TCommandHandler : RootCommandHandler<TCommandHandler>, new()
    {

        public new static RootCommandHandler<TCommandHandler> GetHandler()
            => new TCommandHandler();

        protected RootCommandHandler(string description = "")
            : base(new RootCommand(description))
        { }

        public RootCommand SystemCommandLineRoot
            => SystemCommandLineCommand is RootCommand root
                ? root
                : throw new InvalidOperationException("The root command may have been reset.");
    }
}
