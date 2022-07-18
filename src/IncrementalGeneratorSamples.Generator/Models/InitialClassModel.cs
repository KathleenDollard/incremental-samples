using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class InitialClassModel : InitialSymbolModel, IEquatable<InitialClassModel>
    {
        public InitialClassModel(string name,
                                 string xmlComments,
                                 IEnumerable<AttributeValue> attributes,
                                 string nspace,
                                 IEnumerable<InitialPropertyModel> properties)
            : base(name, xmlComments, attributes)
        {
            Namespace = nspace;
            Properties = properties;
        }
        public string Namespace { get; set; }

        public IEnumerable<InitialPropertyModel> Properties { get; set; }

        public override bool Equals(object obj)
            => Equals(obj as InitialClassModel);

        public bool Equals(InitialClassModel other)
            => !(other is null) &&
                base.Equals(other) &&
                Namespace == other.Namespace &&
                EqualityComparer<IEnumerable<InitialPropertyModel>>.Default.Equals(Properties, other.Properties);

        public override int GetHashCode()
        {
            int hashCode = 1383515346;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namespace);
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<InitialPropertyModel>>.Default.GetHashCode(Properties);
            return hashCode;
        }
    }

 
}
