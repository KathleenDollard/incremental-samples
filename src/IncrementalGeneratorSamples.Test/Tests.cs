using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class Tests
    {
        private static T GetInputSource<T>(Type inputDataType, Func<TestData, T> getPart)
            where T : class?
        {
            return Activator.CreateInstance(inputDataType) is TestData testData
                            ? getPart(testData) as T
                            : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));
        }

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
            var inputSource = GetInputSource(inputDataType, x => x.InputSourceCode);
            var (_, symbol, _, cancellationToken, inputDiagnostics) = TestHelpers.GetTransformInfoForClass(inputSource, x => true);
            Assert.Empty(TestHelpers.ErrorAndWarnings(inputDiagnostics));
            Assert.NotNull(symbol);

            var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);

            return Verifier.Verify(classModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }



        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescriptions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]

        public Task Command_model(string fileNamePart, Type inputDataType)
        {
            var initialModel = GetInputSource(inputDataType, x => x.InitialClassModel);

            var commandModel = ModelBuilder.GetCommandModel(initialModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(commandModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }


        [Fact]
        // this is a constant, but for consistency is checked for regression in tests
        public Task Generated_cli_code()
        {

            var outputCode = CodeOutput.ConsistentCli;

            return Verifier.Verify(outputCode).UseDirectory("InitialModelSnapshots").UseTextForParameters("Cli");
        }


        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithThreeCommands", typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task Generated_cli_partial_code(string fileNamePart, params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                .Select(t => GetInputSource(t, x => x.CommandModel));

            var outputCode = CodeOutput.PartialCli(commandModels, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }


        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithThreeCommands", typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task Generated_root_command_code(string fileNamePart, params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                .Select(t => GetInputSource(t, x => x.CommandModel));

            var outputCode = CodeOutput.GenerateRootCommandCode(commandModels, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }

        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescriptions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]
        public Task Generated_command_code(string fileNamePart, Type inputDataType)
        {
            var commandModel = Activator.CreateInstance(inputDataType) is TestData testData
                ? testData.CommandModel
                : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));

            var outputCode = CodeOutput.GenerateCommandCode(commandModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
    }
}
