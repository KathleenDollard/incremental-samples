using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class InitialSymbolModel
    {
        public InitialSymbolModel(string name,
                                  string xmlComments,
                                  IEnumerable<AttributeValue> attributes)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            XmlComments = xmlComments;
            Attributes = attributes;
        }
        public string Name { get; set; }
        public string XmlComments { get; set; }
        public IEnumerable<AttributeValue> Attributes { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as InitialSymbolModel);
        }

        public bool Equals(InitialSymbolModel other)
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
