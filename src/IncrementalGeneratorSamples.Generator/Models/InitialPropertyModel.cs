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
        {
            return Equals(obj as InitialPropertyModel);
        }

        public bool Equals(InitialPropertyModel other)
        {           // REVIEW: Does this box individual elements? Do we care if things are strings?
            return StructuralComparisons.StructuralEqualityComparer.Equals(this, other);
            //return obj is InitialSymbolModel model &&
            //       Name == model.Name &&
            //       XmlComments == model.XmlComments &&
            //       Attributes.SequenceEqual(model.Attributes);           
        }

        public override int GetHashCode()
        {
            // REVIEW: Does this box individual elements? Do we care if things are strings?
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(this);
        }
    }
}
