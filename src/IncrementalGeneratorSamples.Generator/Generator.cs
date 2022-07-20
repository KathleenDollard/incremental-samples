using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace IncrementalGeneratorSamples
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            var classModelValues = initContext.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: "IncrementalGeneratorSamples.Runtime.CommandAttribute",
                    predicate: (_, _1) => true,
                    transform: GetModelFromAttribute);

            classModelValues = classModelValues.Where(classModel => !(classModel is null));

            var commandModelValues = classModelValues.Select((classModel, cancellationToken) 
                => ModelBuilder.GetCommandModel(classModel, cancellationToken));

            //initContext.RegisterPostInitializationOutput((postinitContext) =>
            //    postinitContext.AddSource("Cli.g.cs", CodeOutput.AlwaysOnCli));

            //initContext.RegisterSourceOutput(
            //   rootCommandValue,
            //   (outputContext, modelData) =>
            //           outputContext.AddSource("Cli.Partial.g.cs",
            //                             CodeOutput.PartialCli(modelData, outputContext.CancellationToken)));

            //initContext.RegisterSourceOutput(
            //    commandModelValues,
            //    (outputContext, modelData) =>
            //            outputContext.AddSource(CodeOutput.FileName(modelData),
            //                              CodeOutput.GenerateCommandCode(modelData, outputContext.CancellationToken)));

            //initContext.RegisterSourceOutput(
            //    rootCommandValue,
            //    (outputContext, modelData) =>
            //            outputContext.AddSource("Root.g.cs",
            //                              CodeOutput.GenerateRootCommandCode(modelData, outputContext.CancellationToken)));

        }

        private static InitialClassModel GetModelFromAttribute(GeneratorAttributeSyntaxContext generatorContext,
                                      CancellationToken cancellationToken)
         => ModelBuilder.GetInitialModel( generatorContext.TargetSymbol,  cancellationToken);


    }
}