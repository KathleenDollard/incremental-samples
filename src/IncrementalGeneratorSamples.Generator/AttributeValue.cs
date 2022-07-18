using System;
using System.Collections.Generic;

namespace IncrementalGeneratorSamples
{
    public class AttributeValue : IEquatable<AttributeValue>
    {

        public AttributeValue(string attributeName,
                              string valueName,
                              string valueType,
                              object value)
        {
            AttributeName = attributeName;
            ValueName = valueName;
            ValueType = valueType;
            this.Value = value;
        }

        public string AttributeName { get;  }
        public string ValueName { get;  }
        public string ValueType { get;  }
        public object Value { get;  }

        public override bool Equals(object obj) 
            => Equals(obj as AttributeValue);

        public bool Equals(AttributeValue other)
            => !(other is null) &&
                   AttributeName == other.AttributeName &&
                   ValueName == other.ValueName &&
                   ValueType == other.ValueType &&
                   EqualityComparer<object>.Default.Equals(Value, other.Value);

        public override int GetHashCode()
        {
            int hashCode = -960430703;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AttributeName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ValueName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ValueType);
            hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Value);
            return hashCode;
        }
    }
}
