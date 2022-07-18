using System;
using System.Collections;
using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class InitialPropertyModel : InitialSymbolModel, IEquatable<InitialPropertyModel>
    {
        public InitialPropertyModel(string name,
                                    string xmlComments,
                                    string type,
                                    IEnumerable<AttributeValue> attributes)
            : base(name, xmlComments, attributes)
        {
            Type = type;
        }

        public string Type { get; set; }

        public override bool Equals(object obj)
            => Equals(obj as InitialPropertyModel);

        public bool Equals(InitialPropertyModel other)
            => !(other is null) &&
                base.Equals(other) &&
                Type == other.Type;

        public override int GetHashCode()
        {
            int hashCode = 1522494069;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            return hashCode;
        }
    }
}
