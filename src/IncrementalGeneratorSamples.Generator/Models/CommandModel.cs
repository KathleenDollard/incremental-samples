using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class CommandModel : ModelCommon
    {
        private List<CommandModel> commands;
        public CommandModel(string name,
                            string originalName,
                            string publicSymbolName,
                            string privateSymbolName,
                            string description,
                            IEnumerable<OptionModel> options)
        :base(name, originalName,publicSymbolName,privateSymbolName,description)
        {
            commands = new List<CommandModel>();
            Options = options;
        }

        public IEnumerable<OptionModel> Options { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as InitialClassModel);
        }

        public bool Equals(InitialClassModel other)
        {           // REVIEW: Does this box individual elements? Do we care if things are strings?
            return StructuralComparisons.StructuralEqualityComparer.Equals(this, other);
        }

        public override int GetHashCode()
        {
            // REVIEW: Does this box individual elements? Do we care if things are strings?
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(this);
        }
    }
}