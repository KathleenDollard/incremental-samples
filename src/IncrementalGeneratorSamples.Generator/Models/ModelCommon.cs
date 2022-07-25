using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class ModelCommon
    {
        public ModelCommon(string name,
                           string originalName,
                           string symbolName,
                           string localSymbolName,
                           IEnumerable<string> aliases,
                           string description)
        {
            Name = name;
            OriginalName = originalName;
            SymbolName = symbolName;
            LocalSymbolName = localSymbolName;
            Aliases = aliases;
            Description = description;
        }

        public string Name { get; }
        public string OriginalName { get; }
        public string SymbolName { get; }
        public string LocalSymbolName { get; }
        public IEnumerable<string> Aliases { get; }
        public string Description { get; }
    }
}
