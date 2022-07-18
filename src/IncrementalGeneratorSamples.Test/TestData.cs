using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    public class TestData
    {
        protected TestData(string inputSourceCode, InitialClassModel initialClassModel, CommandModel commandModel, string outputSourceCode)
        {
            InputSourceCode = inputSourceCode;
            InitialClassModel = initialClassModel;
            CommandModel = commandModel;
            OutputSourceCode = outputSourceCode;
        }

        public string InputSourceCode { get; }
        public InitialClassModel InitialClassModel { get; }
        public CommandModel CommandModel { get; }
        public string OutputSourceCode { get; }
    }

    public class SimplestPractical : TestData
    {
        public SimplestPractical()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class MyClass{}
}",
                   initialClassModel: new("MyClass",
                       "",
                       Enumerable.Empty<AttributeValue>(),
                       "MyNamespace",
                       Enumerable.Empty<InitialPropertyModel>()),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }

    public class WithOneProperty : TestData
    {
        public WithOneProperty()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: new("MyClass",
                        "",
                        Enumerable.Empty<AttributeValue>(),
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>())}),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }



    public class WithMultipleProperties : TestData
    {
        public WithMultipleProperties()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
        public int PropertyTwo{ get; set; }
        public FileInfo? PropertyThree{ get; set; }
    }
}",
                   initialClassModel: new("MyClass",
                        "",
                        Enumerable.Empty<AttributeValue>(),
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()),
                                new("PropertyTwo","","int",Enumerable.Empty<AttributeValue>()),
                                new("PropertyThree","","System.IO.FileInfo",Enumerable.Empty<AttributeValue>()),
                        }),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }

    public class WithXmlDescriptions : TestData
    {
        public WithXmlDescriptions()
           : base(inputSourceCode: @"
namespace MyNamespace
{
    /// <summary>
    /// This is MyClass
    /// </summary>
    public class MyClass
    {
           /// <summary>
           /// This is the first property
           /// </summary>
           public string? PropertyOne{ get; set; }
    }
}",
                  initialClassModel: new("MyClass",
                        @"
<member name=""T:MyNamespace.MyClass"">
    <summary>
    This is MyClass
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
                                Enumerable.Empty<AttributeValue>())}),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }

    public class WithAliasAttributes : TestData
    {
        public WithAliasAttributes()
            : base(inputSourceCode: @"
using IncrementalGeneratorSamples.Runtime;
namespace MyNamespace
{
    [Alias(""command-alias"")]
    public class MyClass
    {
       [Alias(""--p"")]
       [Alias(""-prop1"")]
       public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: new("MyClass",
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
                                new("AliasAttribute","Alias","string","-prop1"),
                            })
                        }),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }


    public class WithAttributeNamedValues : TestData
    {
        public WithAttributeNamedValues()
           : base(inputSourceCode: @"
namespace MyNamespace
{
    sealed class NamedValueAttribute : Attribute
    {
        public int NamedInt { get; set; }
        public string[]? NamedStrings { get; set; }
    }

    [NamedValue(NamedInt = 42, NamedStrings = new string[] { ""A"", ""B"", ""C"" })]
    public class MyClass
    {
        [NamedValue(NamedInt = 43, NamedStrings = new string[] { ""D"" })]
        public string? PropertyOne{ get; set; }
    }
}",
                  initialClassModel: new("MyClass",
                        "",
                        new List<AttributeValue>
                        {
                            new("NamedValueAttribute","NamedInt","int",42),
                            new("NamedValueAttribute","NamedStrings","string[]",new string[]{ "A","B","C"})
                        },
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",new List<AttributeValue>
                            {
                                new("NamedValueAttribute","NamedInt","int",43),
                                new("NamedValueAttribute","NamedStrings","string[]",new string[]{ "D"})
                            })
                        }),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }

    public class WithAttributeConstructorValues : TestData
    {
        public WithAttributeConstructorValues()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    sealed class CtorValueAttribute : Attribute
    {
        public CtorValueAttribute(string positionalString, int[] ints)
        {
            PositionalString = positionalString;
            Ints = ints;
        }

        public string PositionalString { get; }
        public int[] Ints { get; }
    }

    [CtorValue(""Okay"", new int[] { 1, 3, 5, 7 })]
    public class MyClass
    {
       [CtorValue(""Still Okay"", new int[] { 11, 13 })]
       public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: new("MyClass",
                        "",
                        new List<AttributeValue>
                        {
                            new("CtorValueAttribute","positionalString","string","Okay"),
                            new("CtorValueAttribute","ints","int[]",new int[]{ 1,3,5,7})
                        },
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",new List<AttributeValue>
                            {
                                new("CtorValueAttribute","positionalString","string","Still Okay"),
                                new("CtorValueAttribute","ints","int[]",new int[]{ 11,13})
                            })
                        }),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }


    public class WithAttributeNestedNamedValues : TestData
    {
        public WithAttributeNestedNamedValues()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    sealed class NamedValueAttribute : Attribute
    {
        public int NamedInt { get; set; }
        public object[]? NamedStrings { get; set; }
    }

    [NamedValue(NamedInt = 42, NamedStrings = new object[] { ""A"", ""B"", new object[] { ""C"" } })]
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: new("MyClass",
                        "",
                        new List<AttributeValue>
                        {
                            new("NamedValueAttribute","NamedInt","int",42),
                            new("NamedValueAttribute","NamedStrings","object[]",new object[]{ "A","B",new object[] { "C" } })
                        },
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()) }),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }



    public class WithAttributeNestedConstructorValues : TestData
    {
        public WithAttributeNestedConstructorValues()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    sealed class CtorValueAttribute : Attribute
    {
        public CtorValueAttribute(string positionalString, object[] objs)
        {
            PositionalString = positionalString;
            Objs = objs;
        }

        public string PositionalString { get; }
        public object[] Objs { get; }
    }

    [CtorValue(""Okay"", new object[] { 1, 3, 5,  new object[] { 7 } })]
    public class MyClass
    {
       public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: new("MyClass",
                        "",
                        new List<AttributeValue>
                        {
                            new("CtorValueAttribute","positionalString","string","Okay"),
                            new("CtorValueAttribute","ints","object[]",new object[]{ 1,3,5,new object[] { 7 } })
                        },
                        "MyNamespace",
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()) }),
                   commandModel: null, 
                   outputSourceCode: "")
        { }
    }
}
