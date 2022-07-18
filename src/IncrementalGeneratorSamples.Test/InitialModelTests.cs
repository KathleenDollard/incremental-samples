namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class InitialModelTests
    {
        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescriptions", typeof(WithXmlDescriptions))]
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
            var (_, symbol, _, cancellationToken, inputDiagnostics) = TestHelpers.GetTransformInfoForClass(inputSource, x => x.Identifier.ToString() == "MyClass");
            Assert.Empty(TestHelpers.ErrorAndWarnings(inputDiagnostics));
            Assert.NotNull(symbol);

            var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);

            return Verifier.Verify(classModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
    }
}
