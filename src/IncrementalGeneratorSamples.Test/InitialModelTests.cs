using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

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
        public (IEnumerable<Diagnostic> inputDiagostics, InitialClassModel? classModel)
            GetInitialClassModel(string sourceCode, Func<ClassDeclarationSyntax, bool>? filter = null, bool continueOnInputErrors = false)
        {
            var cancellationToken = new CancellationTokenSource().Token;
            var compilation = TestHelpers.GetInputCompilation<Generator>(
                    OutputKind.DynamicallyLinkedLibrary, sourceCode);
            var inputDiagnostics = compilation.GetDiagnostics();
            if (!continueOnInputErrors && TestHelpers.ErrorAndWarnings(inputDiagnostics).Any())
            { return (inputDiagnostics, null); }
            var tree = compilation.SyntaxTrees.Single();
            var matchQuery = tree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>();
            if (filter is not null)
            { matchQuery = matchQuery.Where(x => filter(x)); }
            var matches = matchQuery.ToList();
            Assert.Single(matches);
            var syntaxNode = matches.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode);
            Assert.NotNull(symbol);
            var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);
            return (inputDiagnostics, classModel);

        }

        private const string SimplestPractical = @"
namespace MyNamespace
{
    public class MyClass{}
}";

        private const string WithOneProperty = @"
namespace MyNamespace
{
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
    }
}";

        private const string WithMulitipeProperties = @"
namespace MyNamespace
{
    public class MyClass
    {
        public string? PropertyOne{ get; set; }
        public int PropertyTwo{ get; set; }
        public FileInfo? PropertyThree{ get; set; }
    }
}";

        /// <summary>
        /// 
        /// </summary>
        private const string WithXmlDescripions = @"
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
}";

        private const string WithAttributeNamedValues = @"
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
}";

        private const string WithAttributeConstructorValues = @"
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
}";

        private const string WithAttributeNestedNamedValues = @"
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
}";

        private const string WithAttributeNestedConstructorValues = @"
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
}";


        [Theory]
        [InlineData("SimplestPractical", SimplestPractical)]
        [InlineData("WithOneProperty", WithOneProperty)]
        [InlineData("WithMulitipeProperties", WithMulitipeProperties)]
        [InlineData("WithXmlDescripions", WithXmlDescripions)]
        [InlineData("WithAttributeNamedValue", WithAttributeNamedValues)]
        [InlineData("WithAttributeConstructorValues", WithAttributeConstructorValues)]
        [InlineData("WithAttributeNestedNamedValues", WithAttributeNestedNamedValues)]
        [InlineData("WithAttributeNestedConstructorValues", WithAttributeNestedConstructorValues)]
        public Task Initial_class_model(string fileNamePart, string input)
        {
            var (inputDiagnostics, output) = GetInitialClassModel(input, x => x.Identifier.ToString() == "MyClass");

            Assert.Empty(TestHelpers.ErrorAndWarnings(inputDiagnostics));
            return Verifier.Verify(output).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
    }
}
