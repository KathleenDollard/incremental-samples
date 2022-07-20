using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    public class TestData
    {
        protected TestData(string inputSourceCode)
        {
            InputSourceCode = inputSourceCode;
        }

        protected TestData(string inputSourceCode, InitialClassModel initialClassModel, CommandModel commandModel)
        {
            InputSourceCode = inputSourceCode;
            InitialClassModel = initialClassModel;
            CommandModel = commandModel;
        }

        public string InputSourceCode { get; }
        public InitialClassModel? InitialClassModel { get; }
        public CommandModel? CommandModel { get; }

    }

    public class SimplestPractical : TestData
    {
        public SimplestPractical()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class SimplestPractical{}
}",
                   initialClassModel: TestDataInitialModels.SimplestPractical,
                   commandModel: TestDataCommandModels.SimplestPractical)
        { }
    }

    public class WithOneProperty : TestData
    {
        public WithOneProperty()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class WithOneProperty
    {
        public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: TestDataInitialModels.WithOneProperty,
                   commandModel: TestDataCommandModels.WithOneProperty)
        { }
    }

    public class WithMultipleProperties : TestData
    {
        public WithMultipleProperties()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class WithMultipleProperties
    {
        public string? PropertyOne{ get; set; }
        public int PropertyTwo{ get; set; }
        public FileInfo? PropertyThree{ get; set; }
    }
}",
                   initialClassModel: TestDataInitialModels.WithMultipleProperties,
                   commandModel: TestDataCommandModels.WithMultipleProperties)
        { }
    }

    public class WithXmlDescriptions : TestData
    {
        public WithXmlDescriptions()
           : base(inputSourceCode: @"
namespace MyNamespace
{
    /// <summary>
    /// This class is named WithXmlDescriptions
    /// </summary>
    public class WithXmlDescriptions
    {
           /// <summary>
           /// This is the first property
           /// </summary>
           public string? PropertyOne{ get; set; }
    }
}",
                  initialClassModel: TestDataInitialModels.WithXmlDescriptions,
                  commandModel: TestDataCommandModels.WithXmlDescriptions)
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
    public class WithAliasAttributes
    {
       [Alias(""--p"")]
       [Alias(""-prop1"")]
       public string? PropertyOne{ get; set; }
    }
}",
                   initialClassModel: TestDataInitialModels.WithAliasAttributes,
                   commandModel: TestDataCommandModels.WithAliasAttributes)
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
}")
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
}")

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
}")
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
}")
        { }
    }
}
