using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Text;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Runtime
{
    public abstract class CommandHandler<TCommandHandler> : ICommandHandler
        where TCommandHandler : CommandHandler<TCommandHandler>, new()
    {
        public CommandHandler()
        {
            RootCommand = new RootCommand();
            RootCommand.Handler = this;
        }

        protected RootCommand RootCommand { get; }

        public static int Invoke(string[] args)
            => new TCommandHandler().RootCommand.Invoke(args);

        public static async Task<int> InvokeAsync(string[] args)
            => await new TCommandHandler().RootCommand.InvokeAsync(args);

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
