using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    internal static class TestDataCommandModels
    {
        public static CommandModel SimplestPractical
          => new(name: "SimplestPractical",
                 displayName: "simplest-practical",
                 aliases: Array.Empty<string>(),
                 description: "",
                 nspace: "MyNamespace",
                 options: Enumerable.Empty<OptionModel>());

        public static CommandModel WithOneProperty
          => new(                 name: "WithOneProperty",
                 displayName: "with-one-property",
                 aliases: Array.Empty<string>(),
                 description: "",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (displayName: "--property-one",
                           name: "PropertyOne",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "string?")
              });

        public static CommandModel WithMultipleProperties
          => new(name: "WithMultipleProperties",
                 displayName: "with-multiple-properties",
                 aliases: Array.Empty<string>(),
                 description: "",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (displayName: "--property-one",
                           name: "PropertyOne",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "string?"),
                      new (displayName: "--property-two",
                           name: "PropertyTwo",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "int"),
                      new (displayName: "--property-three",
                           name: "PropertyThree",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "System.IO.FileInfo")
              });

        public static CommandModel WithXmlDescriptions
          => new(name: "WithXmlDescriptions",
                 displayName: "with-xml-descriptions",
                 aliases: Array.Empty<string>(),
                 description: "This class is named WithXmlDescriptions",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (displayName: "--property-one",
                           name: "PropertyOne",
                           aliases: Array.Empty<string>(),
                           description: "This is the first property",
                           type: "string?")
              });

        public static CommandModel WithAliasAttributes
          => new(name: "WithAliasAttributes",
                 displayName: "with-alias-attributes",
                 aliases: new string[] {"command-alias"},
                 description: "",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (displayName: "--property-one",
                           name: "PropertyOne",
                           aliases:  new string[] {"--p","-prop1"},
                           description: "",
                           type: "string?")
              });
    }
}
