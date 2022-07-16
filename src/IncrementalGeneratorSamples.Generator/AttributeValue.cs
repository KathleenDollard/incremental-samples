using System;
using System.Collections.Generic;
using System.Text;

namespace IncrementalGeneratorSamples
{
    public class AttributeValue
    {

        public AttributeValue(string attributeName,
                              string valueName,
                              string valueType,
                              object value)
        {
            AttributeName = attributeName;
            ValueName = valueName;
            ValueType = valueType;
            this.value = value;
        }

        public string AttributeName { get;  }
        public string ValueName { get;  }
        public string ValueType { get;  }
        public object value { get;  }
    }
}
