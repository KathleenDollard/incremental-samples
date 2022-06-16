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
        var modelDataValues = initContext.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: ModelBuilder.IsSyntaxInteresting,
                transform: ModelBuilder.GetModel)
            .Where(static m => m is not null)!;

        initContext.RegisterSourceOutput(
            modelDataValues,
            static (context, modelData) =>
                    context.AddSource(CodeOutput.FileName(modelData),
                                      CodeOutput.GeneratedCode(modelData)));

    }

}

