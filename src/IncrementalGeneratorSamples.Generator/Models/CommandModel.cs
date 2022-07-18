using System.Collections;
using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class CommandModel : ModelCommon
    {
        private List<CommandModel> commands;
        public CommandModel(string name,
                            string originalName,
                            string symbolName,
                            string localSymbolName,
                            IEnumerable<string> aliases,
                            string description,
                            string nspace,
                            IEnumerable<OptionModel> options)
        : base(name, originalName, symbolName, localSymbolName, aliases, description)
        {
            commands = new List<CommandModel>();
            Options = options;
            Namespace = nspace;
        }

        public string Namespace { get; }
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