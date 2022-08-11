﻿using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace IncrementalGeneratorSamples.Test
{
    [UsesVerify]
    public class UnitTests
    {
        private static string GetClassName(Type[] inputDataTypes)
            => inputDataTypes.Length == 1
                ? inputDataTypes.First().Name
                : $"{inputDataTypes.First().Name}_Plus_{inputDataTypes.Length - 1}";

        [Theory]
        [InlineData(typeof(SimplestPractical))]
        [InlineData(typeof(WithOneProperty))]
        [InlineData(typeof(WithMultipleProperties))]
        [InlineData(typeof(WithXmlDescriptions))]
        [InlineData(typeof(WithAliasAttributes))]
        [InlineData(typeof(WithAttributeNamedValues))]
        [InlineData(typeof(WithAttributeConstructorValues))]
        [InlineData(typeof(WithAttributeNestedNamedValues))]
        [InlineData(typeof(WithAttributeNestedConstructorValues))]
        public Task Initial_class_model(Type inputDataType)
        {
            var inputSource = TestData.GetData(inputDataType).InputSourceCode;
            var className = inputDataType.Name;
            var (symbol, cancellationToken, inputDiagnostics) =
                TestHelpers.GetSymbolForSynatax<ClassDeclarationSyntax>(inputSource, x => x.Identifier.ToString() == className);
            Assert.Empty(TestHelpers.WarningAndErrors(inputDiagnostics));
            Assert.NotNull(symbol);

            var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);

            return Verifier.Verify(classModel).UseDirectory("Snapshots").UseTextForParameters(className);
        }

        [Theory]
        [InlineData(typeof(SimplestPractical))]
        [InlineData(typeof(WithOneProperty))]
        [InlineData(typeof(WithMultipleProperties))]
        [InlineData(typeof(WithXmlDescriptions))]
        [InlineData(typeof(WithAliasAttributes))]

        public Task Command_model(Type inputDataType)
        {
            var initialModel = TestData.GetData(inputDataType).InitialClassModel;
            var className = inputDataType.Name;

            var commandModel = ModelBuilder.GetCommandModel(initialModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(commandModel).UseDirectory("Snapshots").UseTextForParameters(className);
        }

        [Theory]
        [InlineData(typeof(SimplestPractical))]
        [InlineData(typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task Root_command_model(params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                                .Select(t => TestData.GetData(t).CommandModel)
                                .ToImmutableArray();
            var className = GetClassName(inputDataTypes);

            var rootCommandModel = ModelBuilder.GetRootCommandModel(commandModels, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(rootCommandModel).UseDirectory("Snapshots").UseTextForParameters(className);
        }

        [Fact]
        // this is a constant, but for consistency is checked for regression in tests
        public Task CodeOutput_cli_code()
        {

            var outputCode = CodeOutput.ConsistentCli;

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters("Cli");
        }


        [Theory]
        [InlineData(typeof(SimplestPractical))]
        [InlineData(typeof(SimplestPractical), typeof(WithOneProperty), typeof(WithMultipleProperties))]
        public Task CodeOutput_cli_partial_code(params Type[] inputDataTypes)
        {
            var commandModels = inputDataTypes
                .Select(t => TestData.GetData(t).CommandModel)
                .ToImmutableArray();
            var className = GetClassName(inputDataTypes);

            var rootCommandModel = ModelBuilder.GetRootCommandModel(commandModels, TestHelpers.CancellationTokenForTesting);

            var outputCode = CodeOutput.PartialCli(rootCommandModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters(className);
        }

        [Theory]
        [InlineData(typeof(SimplestPractical))]
        [InlineData(typeof(WithOneProperty))]
        [InlineData(typeof(WithMultipleProperties))]
        [InlineData(typeof(WithXmlDescriptions))]
        [InlineData(typeof(WithAliasAttributes))]
        public Task CodeOutput_command_code(Type inputDataType)
        {
            var commandModel = Activator.CreateInstance(inputDataType) is TestData testData
                ? testData.CommandModel
                : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));
            var className = inputDataType.Name;

            var outputCode = CodeOutput.CommandCode(commandModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(outputCode).UseDirectory("Snapshots").UseTextForParameters(className);
        }
    }
}
