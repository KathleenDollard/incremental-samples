using IncrementalGeneratorSamples.Models;
using Microsoft.CodeAnalysis;
using System.Xml.Linq;

namespace IncrementalGeneratorSamples;

public class ModelBuilder
{
 
    public static CommandModel? GetModel(SyntaxNode syntaxNode,
                                         ISymbol? symbol,
                                         SemanticModel semanticModel,
                                         CancellationToken cancellationToken)
    {
        if (symbol is not ITypeSymbol typeSymbol)
        { return null; }

        var attribute = symbol.GetAttributes().First();
        var x = attribute.AttributeClass?.Name;

        var description = GetXmlDescription(symbol.GetDocumentationCommentXml());

        var properties = typeSymbol.GetMembers().OfType<IPropertySymbol>();
        var options = new List<OptionModel>();
        foreach (var property in properties)
        {
            // since we do not know how big this list is, so we will check cancellation token
            cancellationToken.ThrowIfCancellationRequested();
            var propDescription = GetXmlDescription(property.GetDocumentationCommentXml());
            options.Add(new OptionModel(property.Name, property.Type.ToString(), propDescription));
        }
        return new CommandModel(typeSymbol.Name, description, options);

        static string GetXmlDescription(string? doc)
        {
            if (string.IsNullOrEmpty(doc))
            { return ""; }
            var xDoc = XDocument.Parse(doc);
            var desc = xDoc.DescendantNodes()
                .OfType<XElement>()
                .FirstOrDefault(x => x.Name == "summary")
                ?.Value;
            return desc is null
                ? ""
                : desc.Replace("\n", "").Replace("\r", "").Trim();
        }

    }
}
