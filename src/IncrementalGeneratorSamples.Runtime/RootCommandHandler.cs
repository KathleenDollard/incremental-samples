using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace IncrementalGeneratorSamples.Runtime
{
    public abstract class RootCommandHandler<TCommandHandler> : CommandHandlerBase
        where TCommandHandler : RootCommandHandler<TCommandHandler>, new()
    {
        protected RootCommandHandler(string description = "")
            : base(new RootCommand(description))
        { }

        public RootCommand RootCommand
            => Command is RootCommand root
                ? root
                : throw new InvalidOperationException("Unexpected type");
    }
}
