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
            // Initial extraction - there may be multiple for some generators
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

            // Output code on einput could produce several outputs, and the reverse
            initContext.RegisterSourceOutput(
                rootCommandValue,
                (outputContext, rootModel) =>
                    outputContext.AddSource("Cli.Partial.g.cs",
                            CodeOutput.PartialCli(rootModel, outputContext.CancellationToken)));

            initContext.RegisterSourceOutput(
                commandModelValues,
                (outputContext, model) =>
                        outputContext.AddSource(CodeOutput.FileName(model),
                             CodeOutput.GenerateCommandCode(model, outputContext.CancellationToken)));

            initContext.RegisterSourceOutput(
                rootCommandValue,
                (outputContext, rootModel) =>
                        outputContext.AddSource("Root.g.cs",
                            CodeOutput.GenerateRootCommandCode(rootModel, outputContext.CancellationToken)));

        }

        private static InitialClassModel GetModelFromAttribute(GeneratorAttributeSyntaxContext generatorContext,
                                      CancellationToken cancellationToken)
         => ModelBuilder.GetInitialModel(generatorContext.TargetSymbol, cancellationToken);


    }
}