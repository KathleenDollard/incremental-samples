using IncrementalGeneratorSamples.InternalModels;

namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class UnitTests
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
        [InlineData("WithMultipleProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescriptions", typeof(WithXmlDescriptions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]
        [InlineData("WithAttributeNamedValue", typeof(WithAttributeNamedValues))]
        [InlineData("WithAttributeConstructorValues", typeof(WithAttributeConstructorValues))]
        [InlineData("WithAttributeNestedNamedValues", typeof(WithAttributeNestedNamedValues))]
        [InlineData("WithAttributeNestedConstructorValues", typeof(WithAttributeNestedConstructorValues))]
        public Task Initial_class_model(string className, Type inputDataType)
        {
            var inputSource = GetInputSource(inputDataType, x => x.InputSourceCode);
            var (_, symbol, _, cancellationToken, inputDiagnostics) = 
                TestHelpers.GetTransformInfoForClass(inputSource, x => x.Identifier.ToString()==className);
            Assert.Empty(TestHelpersCommon.WarningAndErrors(inputDiagnostics));
            Assert.NotNull(symbol);

            var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);

            return Verifier.Verify(classModel).UseDirectory("Snapshots").UseTextForParameters(className);
        }



        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescriptions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]

        public Task Command_model(string className, Type inputDataType)
        {
            var initialModel = GetInputSource(inputDataType, x => x.InitialClassModel);

            var commandModel = ModelBuilder.GetCommandModel(initialModel, TestHelpersCommon.CancellationTokenForTesting);

            return Verifier.Verify(commandModel).UseDirectory("Snapshots").UseTextForParameters(className);
        }

        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithThreeCommands", typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task Root_command_model(string className, params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                                .Select(t => GetInputSource(t, x => x.CommandModel));

            var rootCommandModel = ModelBuilder.GetRootCommandModel(commandModels, TestHelpersCommon.CancellationTokenForTesting);

            return Verifier.Verify(rootCommandModel).UseDirectory("Snapshots").UseTextForParameters(className);
        }


        [Fact]
        // this is a constant, but for consistency is checked for regression in tests
        public Task Generated_cli_code()
        {

            var outputCode = CodeOutput.ConsistentCli;

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters("Cli");
        }


        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithThreeCommands", typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task Generated_cli_partial_code(string className, params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                .Select(t => GetInputSource(t, x => x.CommandModel));
            var rootCommandModel = ModelBuilder.GetRootCommandModel(commandModels, TestHelpersCommon.CancellationTokenForTesting);

            var outputCode = CodeOutput.PartialCli(rootCommandModel, TestHelpersCommon.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters(className);
        }


        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithThreeCommands", typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task Generated_root_command_code(string className, params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                .Select(t => GetInputSource(t, x => x.CommandModel));
            var rootCommandModel = ModelBuilder.GetRootCommandModel(commandModels, TestHelpersCommon.CancellationTokenForTesting);

            var outputCode = CodeOutput.GenerateRootCommandCode(rootCommandModel, TestHelpersCommon.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters(className);
        }

        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescriptions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]
        public Task Generated_command_code(string className, Type inputDataType)
        {
            var commandModel = Activator.CreateInstance(inputDataType) is TestData testData
                ? testData.CommandModel
                : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));

            var outputCode = CodeOutput.GenerateCommandCode(commandModel, TestHelpersCommon.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters(className);
        }
    }
}
