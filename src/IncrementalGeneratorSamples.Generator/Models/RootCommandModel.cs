using IncrementalGeneratorSamples.InternalModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class RootCommandModel
    {
        public RootCommandModel(string nspace,
                                IEnumerable<string> commandSymbolNames)
        {
            CommandSymbolNames = commandSymbolNames;
            Namespace = nspace;
        }

        public string Namespace { get; }
        public IEnumerable<string> CommandSymbolNames { get; }
    }
}
