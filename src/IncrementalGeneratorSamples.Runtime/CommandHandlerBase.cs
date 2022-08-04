using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Runtime
{
    public abstract class CommandHandlerBase : ICommandHandler
    {

        // Constructor for all derived classes, except root command
        protected CommandHandlerBase(string name, string description = "")
           : this(new Command(name, description))
        { }

        // Constructor used directly by root command,and indirectly by others
        protected CommandHandlerBase(Command command)
        {
            command.Handler = this;
            Command = command;
        }

        // The underlying System.CommandLine Command
        public Command Command { get; private set; }

        // Helper method to retrieve data during invocation
        protected static T GetValueForSymbol<T>(IValueDescriptor<T> symbol, CommandResult commandResult)
            => symbol is Option<T> option
                ? commandResult.GetValueForOption(option)
                : throw new ArgumentOutOfRangeException();

        // ICommandHandler explicit interface implementation
        int ICommandHandler.Invoke(InvocationContext context) => Invoke(context);
        // Async not supported in this example
        Task<int> ICommandHandler.InvokeAsync(InvocationContext context) => throw new NotImplementedException();

        // If an ExecuteAsync method is present in the input, a generated override will be created
        protected virtual int Invoke(InvocationContext invocationContext)
            => throw new NotImplementedException();
    }
}
