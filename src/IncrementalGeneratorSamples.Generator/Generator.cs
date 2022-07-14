using IncrementalGeneratorSamples.Models;
using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var commandModelValues = initContext.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "IncrementalGeneratorSamples.Runtime.CommandAttribute",
                predicate: (_,_) => true,
                transform: GetModelFromAttribute);

        var rootCommandValue = commandModelValues.Collect();

        initContext.RegisterPostInitializationOutput((postinitContext) =>
            postinitContext.AddSource("Cli.g.cs", CodeOutput.AlwaysOnCli));

        initContext.RegisterSourceOutput(
           rootCommandValue,
           static (outputContext, modelData) =>
                   outputContext.AddSource("Cli.Partial.g.cs",
                                     CodeOutput.PartialCli(modelData, outputContext.CancellationToken)));

        initContext.RegisterSourceOutput(
            commandModelValues,
            static (outputContext, modelData) =>
                    outputContext.AddSource(CodeOutput.FileName(modelData),
                                      CodeOutput.GenerateCommandCode(modelData, outputContext.CancellationToken)));

        initContext.RegisterSourceOutput(
            rootCommandValue,
            static (outputContext, modelData) =>
                    outputContext.AddSource("Root.g.cs",
                                      CodeOutput.GenerateRootCommandCode(modelData, outputContext.CancellationToken)));

    }

    private static CommandModel? GetModelFromAttribute(GeneratorAttributeSyntaxContext generatorContext,
                                  CancellationToken cancellationToken)
     => ModelBuilder.GetModel(generatorContext.TargetNode, generatorContext.TargetSymbol, generatorContext.SemanticModel, cancellationToken);


}

