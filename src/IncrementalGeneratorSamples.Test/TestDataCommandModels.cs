using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    internal static class TestDataCommandModels
    {
        public static CommandModel SimplestPractical
          => new(name: "simplest-practical",
                 originalName: "SimplestPractical",
                 symbolName: "SimplestPractical",
                 localSymbolName: "simplestPractical",
                 aliases: Array.Empty<string>(),
                 description: "",
                 nspace: "MyNamespace",
                 options: Enumerable.Empty<OptionModel>());

        public static CommandModel WithOneProperty
          => new(name: "with-one-property",
                 originalName: "WithOneProperty",
                 symbolName: "WithOneProperty",
                 localSymbolName: "withOneProperty",
                 aliases: Array.Empty<string>(),
                 description: "",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (name: "--property-one",
                           originalName: "PropertyOne",
                           symbolName: "PropertyOne",
                           localSymbolName: "propertyOne",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "string?")
              });

        public static CommandModel WithMultipleProperties
          => new(name: "with-multiple-properties",
                 originalName: "WithMultipleProperties",
                 symbolName: "WithMultipleProperties",
                 localSymbolName: "withMultipleProperties",
                 aliases: Array.Empty<string>(),
                 description: "",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (name: "--property-one",
                           originalName: "PropertyOne",
                           symbolName: "PropertyOne",
                           localSymbolName: "propertyOne",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "string?"),
                      new (name: "--property-two",
                           originalName: "PropertyTwo",
                           symbolName: "PropertyTwo",
                           localSymbolName: "PropertyTwo",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "int"),
                      new (name: "--property-three",
                           originalName: "PropertyThree",
                           symbolName: "PropertyThree",
                           localSymbolName: "PropertyThree",
                           aliases: Array.Empty<string>(),
                           description: "",
                           type: "System.IO.FileInfo")
              });

        public static CommandModel WithXmlDescriptions
          => new(name: "with-xml-descriptions",
                 originalName: "WithXmlDescriptions",
                 symbolName: "WithXmlDescriptions",
                 localSymbolName: "withXmlDescriptions",
                 aliases: Array.Empty<string>(),
                 description: "This class is named WithXmlDescriptions",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (name: "--property-one",
                           originalName: "PropertyOne",
                           symbolName: "PropertyOne",
                           localSymbolName: "propertyOne",
                           aliases: Array.Empty<string>(),
                           description: "This is the first property",
                           type: "string?")
              });

        public static CommandModel WithAliasAttributes
          => new(name: "with-alias-attributes",
                 originalName: "WithAliasAttributes",
                 symbolName: "WithAliasAttributes",
                 localSymbolName: "withAliasAttributes",
                 aliases: new string[] {"command-alias"},
                 description: "",
                 nspace: "MyNamespace",
                 options: new OptionModel[]{
                      new (name: "--property-one",
                           originalName: "PropertyOne",
                           symbolName: "PropertyOne",
                           localSymbolName: "propertyOne",
                           aliases:  new string[] {"--p","-prop1"},
                           description: "",
                           type: "string?")
              });
    }
}
