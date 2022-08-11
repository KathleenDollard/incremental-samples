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
            // Initial extraction - creates models that have value equality
            var classModelValues = initContext.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: "IncrementalGeneratorSamples.Runtime.CommandAttribute",
                    predicate: (_, _1) => true,
                    transform: GetModelFromAttribute);

            // Further transformations
            classModelValues = classModelValues.Where(classModel => !(classModel is null));

            var commandModelValues = classModelValues
                .Select( ModelBuilder.GetCommandModel);

            var rootCommandValue = commandModelValues
                .Collect()
                .Select(ModelBuilder.GetRootCommandModel);

            // Output code that does not depend on input and is added prior to compilation
            initContext.RegisterPostInitializationOutput((postinitContext) =>
                postinitContext.AddSource("Cli.g.cs", CodeOutput.ConsistentCli));

            // Output code that depends on input
            initContext.RegisterSourceOutput(
                rootCommandValue,
                (outputContext, rootModel) =>
                    outputContext.AddSource("Cli.Partial.g.cs",
                            CodeOutput.PartialCli(rootModel, outputContext.CancellationToken)));

            initContext.RegisterSourceOutput(
                commandModelValues,
                (outputContext, model) =>
                        outputContext.AddSource(CodeOutput.FileName(model),
                             CodeOutput.CommandCode(model, outputContext.CancellationToken)));

        }

        private static InitialClassModel GetModelFromAttribute(GeneratorAttributeSyntaxContext generatorContext,
                                      CancellationToken cancellationToken)
         => ModelBuilder.GetInitialModel(generatorContext.TargetSymbol, cancellationToken);


    }
}