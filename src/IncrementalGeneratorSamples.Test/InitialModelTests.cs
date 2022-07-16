using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class InitialModelTests
    {
        public (IEnumerable<Diagnostic> inputDiagostics, InitialClassModel classModel)
            GetInitialClassModel(string sourceCode)
        {

        }

        private const string SimplestPractical = @"
";

        private const string Empty = @"
";

        private const string WithOneProperty = @"
";

        private const string WithMulitipeProperties= @"
";

        private const string WithXmlDescripions = @"
";

        private const string WithAttributeNamedValue = @"
";

        private const string WithAttributeContructorValue = @"
";

        private const string WithAttributeNamedArray= @"
";

        private const string WithAttributeContructorArray= @"
";

        [Theory]
        [InlineData("SimplestPractical", SimplestPractical)]
        [InlineData("Empty", Empty)]
        [InlineData("WithOneProperty", WithOneProperty)]
        [InlineData("WithMulitipeProperties", WithMulitipeProperties)]
        [InlineData("WithXmlDescripions", WithXmlDescripions)]
        [InlineData("WithAttributeNamedValue", WithAttributeNamedValue)]
        [InlineData("WithAttributeContructorValue", WithAttributeContructorValue)]
        [InlineData("WithAttributeNamedArray", WithAttributeNamedArray)]
        [InlineData("WithAttributeContructorArray", WithAttributeContructorArray)]

        public Task Can_create_commandDef(string fileNamePart, string input)
        {
            var (inputDiagnostics, output) = GetInitialClassModel(input);

            Assert.Single(TestHelpers.ErrorAndWarnings(inputDiagnostics));
            Assert.Empty(inputDiagnostics);
            return Verifier.Verify(output).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
    }
}
