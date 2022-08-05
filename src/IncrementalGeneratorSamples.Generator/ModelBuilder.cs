using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace IncrementalGeneratorSamples
{
    public class ModelBuilder
    {
        public static InitialClassModel GetInitialModel(
                                      ISymbol symbol,
                                      CancellationToken cancellationToken)
        {
            if (!(symbol is ITypeSymbol typeSymbol))
            { return null; }

            var properties = new List<InitialPropertyModel>();
            foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                // since we do not know how big this list is, check cancellation token
                cancellationToken.ThrowIfCancellationRequested();
                properties.Add(new InitialPropertyModel(property.Name,
                                                     property.GetDocumentationCommentXml(),
                                                     property.Type.ToString(),
                                                     property.AttributeNamesAndValues()));
            }
            return new InitialClassModel(typeSymbol.Name,
                                         typeSymbol.GetDocumentationCommentXml(),
                                         typeSymbol.AttributeNamesAndValues(),
                                         typeSymbol.ContainingNamespace.Name,
                                         properties);
        }

        public static CommandModel GetCommandModel(InitialClassModel classModel,
                                            CancellationToken cancellationToken)
        {
            // null should have been filtered out, but finding one is not a reason to crash
            if (classModel is null) { return null; }

            var aliases = Helpers.GetAttributeValues(classModel.Attributes, "AliasAttribute");
            var options = new List<OptionModel>();
            foreach (var property in classModel.Properties)
            {
                // since we do not know how big this list is, check cancellation token
                cancellationToken.ThrowIfCancellationRequested();
                var optionAliases = Helpers.GetAttributeValues(property.Attributes, "AliasAttribute");
                options.Add(new OptionModel(
                    $"--{property.Name.AsKebabCase()}",
                    property.Name,
                    property.Name.AsPublicSymbol(),
                    property.Name.AsPrivateSymbol(),
                    optionAliases,
                    Helpers.GetXmlDescription(property.XmlComments),
                    property.Type.ToString()));
            }
            return new CommandModel(
                    name: classModel.Name.AsKebabCase(),
                    originalName: classModel.Name,
                    symbolName: classModel.Name.AsPublicSymbol(),
                    localSymbolName: classModel.Name.AsPrivateSymbol(),
                    aliases,
                    Helpers.GetXmlDescription(classModel.XmlComments),
                    classModel.Namespace,
                    options: options);
        }

        //public static RootCommandModel GetRootCommandModel(ImmutableArray<CommandModel> classModels, CancellationToken _)
        //    => GetRootCommandModel(classModels, CancellationToken.None);

        public static RootCommandModel GetRootCommandModel(ImmutableArray<CommandModel> classModels, CancellationToken _)
            => classModels.Any()
                ? new RootCommandModel(classModels.First().Namespace, classModels.Select(m => m.SymbolName))
                : null;
    }
}