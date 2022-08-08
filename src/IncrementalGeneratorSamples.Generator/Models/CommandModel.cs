using System.Collections;
using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class CommandModel
    {
        public CommandModel(string name,
                            string displayName,
                            IEnumerable<string> aliases,
                            string description,
                            string nspace,
                            IEnumerable<OptionModel> options)
        {
            Name = name;
            DisplayName = displayName;
            Aliases = aliases;
            Description = description;
            Namespace = nspace;
            Options = options;
        }

        public string Namespace { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public IEnumerable<string> Aliases { get; }
        public string Description { get; }
        public IEnumerable<OptionModel> Options { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as InitialClassModel);
        }

        public bool Equals(InitialClassModel other)
        {   // REVIEW: Does this box individual elements? Do we care if things are strings?
            return StructuralComparisons.StructuralEqualityComparer.Equals(this, other);
        }

        public override int GetHashCode()
        {
            // REVIEW: Does this box individual elements? Do we care if things are strings?
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(this);
        }
    }
}