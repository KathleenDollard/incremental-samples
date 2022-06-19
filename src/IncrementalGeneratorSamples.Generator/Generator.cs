using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using IncrementalGeneratorSamples.Models;

namespace IncrementalGeneratorSamples;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var commandModelValues = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: ModelBuilder.IsSyntaxInteresting,
                transform: ModelBuilder.GetModel)
            .Where(static m => m is not null)!;

        var rootCommandValue = commandModelValues.Collect();

        initContext.RegisterSourceOutput(
            commandModelValues,
            static (context, modelData) =>
                    context.AddSource(CodeOutput.FileName(modelData),
                                      CodeOutput.GenerateCommandCode(modelData)));

        initContext.RegisterSourceOutput(
            rootCommandValue,
            static (context, modelData) =>
                    context.AddSource("Root.g.cs",
                                      CodeOutput.GenerateRootCommandCode(modelData)));

    }

}

