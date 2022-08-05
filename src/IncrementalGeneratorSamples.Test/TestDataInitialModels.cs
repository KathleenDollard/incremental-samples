using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    internal static class TestDataInitialModels
    {
        public static InitialClassModel SimplestPractical
            => new("SimplestPractical",
                       "",
                       Enumerable.Empty<AttributeValue>(),
                       "MyNamespace",
                       Enumerable.Empty<InitialPropertyModel>());

        public static InitialClassModel WithOneProperty
            => new("WithOneProperty",

                        "",
                        Enumerable.Empty<AttributeValue>(),
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>())});

        public static InitialClassModel WithMultipleProperties
            => new("WithMultipleProperties",

                        "",
                        Enumerable.Empty<AttributeValue>(),
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()),
                                new("PropertyTwo","","int",Enumerable.Empty<AttributeValue>()),
                                new("PropertyThree","","System.IO.FileInfo",Enumerable.Empty<AttributeValue>()),
                        });

        public static InitialClassModel WithXmlDescriptions
            => new("WithXmlDescriptions",

                        @"
<member name=""T:MyNamespace.MyClass"">
    <summary>
    This class is named WithXmlDescriptions
    </summary>
</member>",
                        Enumerable.Empty<AttributeValue>(),
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne",
                                @"
<member name=""P:MyNamespace.MyClass.PropertyOne"">
    <summary>
    This is the first property
    </summary>
</member>",
                                "string?",
                                Enumerable.Empty<AttributeValue>())});

        public static InitialClassModel WithAliasAttributes
            => new("WithAliasAttributes",

                        "",
                        new List<AttributeValue>
                        {
                            new("AliasAttribute","Alias","string","command-alias"),
                        },
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",new List<AttributeValue>
                            {
                                new("AliasAttribute","Alias","string","--p"),
                                new("AliasAttribute","Alias","string","-prop1")
                            })
                        });

    }
}
