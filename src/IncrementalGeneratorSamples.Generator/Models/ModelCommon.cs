using System;
using System.Collections.Generic;
using System.Text;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class ModelCommon
    {
        public ModelCommon(string name,
                           string originalName,
                           string publicSymbolName,
                           string privateSymbolName,
                           string description)
        {
            Name = name;
            OriginalName = originalName;
            PublicSymbolName = publicSymbolName;
            PrivateSymbolName = privateSymbolName;
            Description = description;
        }

        public string Name { get; }
        public string OriginalName { get; }
        public string PublicSymbolName { get; }
        public string PrivateSymbolName { get; }
        public string Description { get; }
    }
}
