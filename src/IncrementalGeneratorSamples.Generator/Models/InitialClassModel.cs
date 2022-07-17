using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class InitialClassModel : InitialSymbolModel, IEquatable<InitialClassModel>
    {
        public InitialClassModel(string name,
                                 string nspace,
                                 string xmlComments,
                                 IEnumerable<AttributeValue> attributes,
                                 IEnumerable<InitialPropertyModel> properties) 
            : base(name, xmlComments, attributes)
        {
            Namespace = nspace;
            Properties = properties;
        }
        public string Namespace { get; set; }

        public IEnumerable<InitialPropertyModel> Properties { get; set; }

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
