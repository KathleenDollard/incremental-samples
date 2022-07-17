using IncrementalGeneratorSamples.InternalModels;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
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
                // since we do not know how big this list is, so we will check cancellation token
                cancellationToken.ThrowIfCancellationRequested();
                properties.Add(new InitialPropertyModel(property.Name,
                                                     property.GetDocumentationCommentXml(),
                                                     property.Type.ToString(),
                                                     property.AttributenamesAndValues()));
            }
            return new InitialClassModel(typeSymbol.Name,
                                         typeSymbol.ContainingNamespace.Name,
                                         typeSymbol.GetDocumentationCommentXml(),
                                         typeSymbol.AttributenamesAndValues(),
                                         properties);
        }

        public static CommandModel GetModel(InitialClassModel classModel,
                                            CancellationToken cancellationToken)
        {
            if (classModel is null) { return null; }

            var options = new List<OptionModel>();
            var aliases = GetAliases(classModel.Attributes);
            foreach (var property in classModel.Properties)
            {
                // since we do not know how big this list is, so we will check cancellation token
                cancellationToken.ThrowIfCancellationRequested();
                var optionAliases = GetAliases(property.Attributes);
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

        private static IEnumerable<string> GetAliases(IEnumerable<AttributeValue> attributes)
        {
            var aliasAttributes = attributes.Where(x => x.AttributeName == "AliasAttribute");
            if (!aliasAttributes.Any())
            { return Enumerable.Empty<string>(); }
            var aliases = new List<string>();
            foreach (var attribute in aliasAttributes)
            { aliases.Add(attribute.Value.ToString()); }
            return aliases;
        }
    }
}