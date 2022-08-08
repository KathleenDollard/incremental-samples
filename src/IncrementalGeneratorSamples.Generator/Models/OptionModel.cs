using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class OptionModel 
    {
        public OptionModel(string name,
                           string displayName,
                           IEnumerable<string> aliases,
                           string description,
                           string type)
        {
            Name = name;
            DisplayName = displayName;
            Aliases = aliases;
            Description = description;
            Type = type;
        }

        public string Type { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public IEnumerable<string> Aliases { get; }
        public string Description { get; }

        // Default equality and hash is fine here
    }
}