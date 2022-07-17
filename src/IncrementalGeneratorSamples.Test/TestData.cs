using IncrementalGeneratorSamples.InternalModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Test
{
    public class TestData
    {
        protected TestData(string inputSourceCode, InitialClassModel initialClassModel, string outptSourceCode)
        {
            InputSourceCode = inputSourceCode;
            InitialClassModel = initialClassModel;
            OutptSourceCode = outptSourceCode;
        }

        public string InputSourceCode { get; }
        public InitialClassModel InitialClassModel { get; }
        public string OutptSourceCode { get; }
    }

    public class SimplestPractical : TestData
    {
        public SimplestPractical()
            : base(@"
namespace MyNamespace
{
    public class MyClass{}
}",
                   new("MyClass",
                       "MyNamespace",
                       "",
                       Enumerable.Empty<AttributeValue>(),
                       Enumerable.Empty<InitialPropertyModel>()),
                   "")
        { }
    }

    public class WithOneProperty : TestData
    {
        public WithOneProperty()
            : base(@"
namespace MyNamespace
{
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
    }
}",
                   new("MyClass",
                        "MyNamespace",
                        "",
                        Enumerable.Empty<AttributeValue>(),
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>())}),
                   "")
        { }
    }



    public class WithMulitipeProperties : TestData
    {
        public WithMulitipeProperties()
            : base(@"
namespace MyNamespace
{
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
        public int PropertyTwo{ get; set; }
        public FileInfo? PropertyThree{ get; set; }
    }
}",
                   new("MyClass",
                        "MyNamespace",
                        "",
                        Enumerable.Empty<AttributeValue>(),
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()),
                                new("PropertyTwo","","int",Enumerable.Empty<AttributeValue>()),
                                new("PropertyThree","","System.IO.FileInfo",Enumerable.Empty<AttributeValue>()),
                        }),
                   "")
        { }
    }

    public class WithXmlDescripions : TestData
    {
        public WithXmlDescripions()
           : base(@"
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
                  new("MyClass",
                        "MyNamespace",
                        @"
<member name=""T:MyNamespace.MyClass"">
    <summary>
    This is MyClass
    </summary>
</member>",
                        Enumerable.Empty<AttributeValue>(),
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
                  "")
        { }
    }

    public class WithAliasAttributes : TestData
    {
        public WithAliasAttributes()
            : base(@"
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
                   new("MyClass",
                        "MyNamespace",
                        "",
                        new List<AttributeValue>
                        {
                            new("AliasAttribute","Alias","string","command-alias"),
                        },
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",new List<AttributeValue>
                            {
                                new("AliasAttribute","Alias","string","--p"),
                                new("AliasAttribute","Alias","string","-prop1"),
                            })
                        }),
                   "")
        { }
    }


    public class WithAttributeNamedValues : TestData
    {
        public WithAttributeNamedValues()
           : base(@"
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
                  new("MyClass",
                        "MyNamespace",
                        "",
                        new List<AttributeValue>
                        {
                            new("NamedValueAttribute","NamedInt","int",42),
                            new("NamedValueAttribute","NamedStrings","string[]",new string[]{ "A","B","C"})
                        },
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",new List<AttributeValue>
                            {
                                new("NamedValueAttribute","NamedInt","int",43),
                                new("NamedValueAttribute","NamedStrings","string[]",new string[]{ "D"})
                            })
                        }),
                  "")
        { }
    }

    public class WithAttributeConstructorValues : TestData
    {
        public WithAttributeConstructorValues()
            : base(@"
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
                   new("MyClass",
                        "MyNamespace",
                        "",
                        new List<AttributeValue>
                        {
                            new("CtorValueAttribute","positionalString","string","Okay"),
                            new("CtorValueAttribute","ints","int[]",new int[]{ 1,3,5,7})
                        },
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",new List<AttributeValue>
                            {
                                new("CtorValueAttribute","positionalString","string","Still Okay"),
                                new("CtorValueAttribute","ints","int[]",new int[]{ 11,13})
                            })
                        }),
                   "")
        { }
    }


    public class WithAttributeNestedNamedValues : TestData
    {
        public WithAttributeNestedNamedValues()
            : base( @"
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
                   new("MyClass",
                        "MyNamespace",
                        "",
                        new List<AttributeValue>
                        {
                            new("NamedValueAttribute","NamedInt","int",42),
                            new("NamedValueAttribute","NamedStrings","object[]",new object[]{ "A","B",new object[] { "C" } })
                        },
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()) }),
                   "")
        { }
    }



    public class WithAttributeNestedConstructorValues : TestData
    {
        public WithAttributeNestedConstructorValues()
            : base(@"
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
                   new("MyClass",
                        "MyNamespace",
                        "",
                        new List<AttributeValue>
                        {
                            new("CtorValueAttribute","positionalString","string","Okay"),
                            new("CtorValueAttribute","ints","object[]",new object[]{ 1,3,5,new object[] { 7 } })
                        },
                        new List<InitialPropertyModel>
                        { new("PropertyOne","","string?",Enumerable.Empty<AttributeValue>()) }),
                   "")
        { }
    }
}
