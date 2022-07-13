using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {

        IncrementalValuesProvider<Models.CommandModel?> commandModelValues = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: ModelBuilder.IsSyntaxInteresting,
                transform: ModelBuilder.GetModel)
            .Where(static m => m is not null)!;

        IncrementalValueProvider<System.Collections.Immutable.ImmutableArray<Models.CommandModel?>> rootCommandValue = commandModelValues.Collect();

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

}

