using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using System.Threading;

namespace IncrementalGeneratorSamples.Test
{
    sealed class NamedValueAttribute : Attribute
    {
        public int NamedInt { get; set; }
        public object[]? NamedStrings { get; set; }
    }

    [NamedValue(NamedInt = 42, NamedStrings = new object[] { "A", "B", new object[] { "A", "B", "C" } })]
    public class MyClass
    {
        [NamedValue(NamedInt = 43, NamedStrings = new string[] { "D" })]
        public string? PropertyOne { get; set; }
    }

    [UsesVerify]
    public class InitialModelTests
    {


        //        private const string SimplestPractical = @"
        //namespace MyNamespace
        //{
        //    public class MyClass{}
        //}";

        //        private const string WithOneProperty = @"
        //namespace MyNamespace
        //{
        //    public class MyClass
        //    {
        //        public string? PropertyOne{ get; set; }
        //    }
        //}";

        //        private const string WithMulitipeProperties = @"
        //namespace MyNamespace
        //{
        //    public class MyClass
        //    {
        //        public string? PropertyOne{ get; set; }
        //        public int PropertyTwo{ get; set; }
        //        public FileInfo? PropertyThree{ get; set; }
        //    }
        //}";

        //        /// <summary>
        //        /// 
        //        /// </summary>
        //        private const string WithXmlDescripions = @"
        //namespace MyNamespace
        //{
        //    /// <summary>
        //    /// This is MyClass
        //    /// </summary>
        //    public class MyClass
        //    {
        //           /// <summary>
        //           /// This is the first property
        //           /// </summary>
        //           public string? PropertyOne{ get; set; }
        //    }
        //}";

        //        private const string WithAttributeNamedValues = @"
        //namespace MyNamespace
        //{
        //    sealed class NamedValueAttribute : Attribute
        //    {
        //        public int NamedInt { get; set; }
        //        public string[]? NamedStrings { get; set; }
        //    }

        //    [NamedValue(NamedInt = 42, NamedStrings = new string[] { ""A"", ""B"", ""C"" })]
        //    public class MyClass
        //    {
        //        [NamedValue(NamedInt = 43, NamedStrings = new string[] { ""D"" })]
        //        public string? PropertyOne{ get; set; }
        //    }
        //}";

        //        private const string WithAttributeConstructorValues = @"
        //namespace MyNamespace
        //{
        //    sealed class CtorValueAttribute : Attribute
        //    {
        //        public CtorValueAttribute(string positionalString, int[] ints)
        //        {
        //            PositionalString = positionalString;
        //            Ints = ints;
        //        }

        //        public string PositionalString { get; }
        //        public int[] Ints { get; }
        //    }

        //    [CtorValue(""Okay"", new int[] { 1, 3, 5, 7 })]
        //    public class MyClass
        //    {
        //       [CtorValue(""Still Okay"", new int[] { 11, 13 })]
        //       public string? PropertyOne{ get; set; }
        //    }
        //}";

        //        private const string WithAttributeNestedNamedValues = @"
        //namespace MyNamespace
        //{
        //    sealed class NamedValueAttribute : Attribute
        //    {
        //        public int NamedInt { get; set; }
        //        public object[]? NamedStrings { get; set; }
        //    }

        //    [NamedValue(NamedInt = 42, NamedStrings = new object[] { ""A"", ""B"", new object[] { ""C"" } })]
        //    public class MyClass
        //    {
        //        public string? PropertyOne{ get; set; }
        //    }
        //}";

        //        private const string WithAttributeNestedConstructorValues = @"
        //namespace MyNamespace
        //{
        //    sealed class CtorValueAttribute : Attribute
        //    {
        //        public CtorValueAttribute(string positionalString, object[] objs)
        //        {
        //            PositionalString = positionalString;
        //            Objs = objs;
        //        }

        //        public string PositionalString { get; }
        //        public object[] Objs { get; }
        //    }

        //    [CtorValue(""Okay"", new object[] { 1, 3, 5,  new object[] { 7 } })]
        //    public class MyClass
        //    {
        //       public string? PropertyOne{ get; set; }
        //    }
        //}";


        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMulitipeProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescripions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]
        [InlineData("WithAttributeNamedValue", typeof(WithAttributeNamedValues))]
        [InlineData("WithAttributeConstructorValues", typeof(WithAttributeConstructorValues))]
        [InlineData("WithAttributeNestedNamedValues", typeof(WithAttributeNestedNamedValues))]
        [InlineData("WithAttributeNestedConstructorValues", typeof(WithAttributeNestedConstructorValues))]
        public Task Initial_class_model(string fileNamePart, Type inputDataType)
        {
            var inputSource = Activator.CreateInstance(inputDataType) is TestData testData
                ? testData.InputSourceCode
                : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));
            var (_, symbol, _, cancellationToken, inputDiagnostics) = TestHelpers.GetTransformInfo(inputSource, x => x.Identifier.ToString() == "MyClass");

            Assert.NotNull(symbol);
            var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);

            Assert.Empty(TestHelpers.ErrorAndWarnings(inputDiagnostics));
            return Verifier.Verify(classModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
    }
}
