namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class ModelTests
    {



        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMulitipeProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescripions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]
        //[InlineData("WithAttributeNamedValue", typeof(WithAttributeNamedValues))]
        //[InlineData("WithAttributeConstructorValues", typeof(WithAttributeConstructorValues))]
        //[InlineData("WithAttributeNestedNamedValues", typeof(WithAttributeNestedNamedValues))]
        //[InlineData("WithAttributeNestedConstructorValues", typeof(WithAttributeNestedConstructorValues))]
        public Task Initial_class_model(string fileNamePart, Type inputDataType)
        {
            var initialModel = Activator.CreateInstance(inputDataType) is TestData testData
                ? testData.InitialClassModel
                : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));

            var commandModel = ModelBuilder.GetModel(initialModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(commandModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
    }
}
