using System;
using System.Collections.Generic;

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
        public string Name { get;  }
        public string XmlComments { get;  }
        public IEnumerable<AttributeValue> Attributes { get;  }

        public override bool Equals(object obj)
            => Equals(obj as InitialSymbolModel);

        public bool Equals(InitialSymbolModel other) 
            => !(other is null) &&
                   Name == other.Name &&
                   XmlComments == other.XmlComments &&
                   // Is this default a value equality
                   EqualityComparer<IEnumerable<AttributeValue>>.Default.Equals(Attributes, other.Attributes);

        public override int GetHashCode()
        {
            int hashCode = 1546976210;
            hashCode = hashCode * -1521134295 + (Name, XmlComments).GetHashCode();
            // Does this default correspond to a value equality
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<AttributeValue>>.Default.GetHashCode(Attributes);
            return hashCode;
        }
    }
}
