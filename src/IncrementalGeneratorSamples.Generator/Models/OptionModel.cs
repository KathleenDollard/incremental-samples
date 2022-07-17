using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class OptionModel : ModelCommon
    {
        public OptionModel(string name,
                           string originalName,
                           string symbolName,
                           string localSymbolName,
                           IEnumerable<string> aliases,
                           string description,
                           string type)
        : base(name, originalName, symbolName, localSymbolName, aliases, description)
        {
            Type = type;
        }

        public string Type { get; }

        // Default equality and hash is fine here
    }
}