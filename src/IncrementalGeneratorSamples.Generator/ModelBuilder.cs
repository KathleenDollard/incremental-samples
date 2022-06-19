using IncrementalGeneratorSamples.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace IncrementalGeneratorSamples;

public class ModelBuilder
{
    public static bool IsSyntaxInteresting(SyntaxNode syntaxNode, CancellationToken _)
        // REVIEW: What's the best way to check the qualified name? 
        // REVIEW: This should be very fast. Is it ok to ignore the cancelation token in that case?
        // REVIEW: Will this catch all the ways people can use attributes
        => syntaxNode is ClassDeclarationSyntax cls &&
            cls.AttributeLists.Any(x => x.Attributes.Any(a => a.Name.ToString() == "Command" || a.Name.ToString() == "CommandAttribute"));

    public static CommandModel? GetModel(GeneratorSyntaxContext generatorContext,
                                            CancellationToken cancellationToken)
        => GetModel(generatorContext.Node, generatorContext.SemanticModel, cancellationToken);

    public static CommandModel? GetModel(SyntaxNode syntaxNode,
                                            SemanticModel semanticModel,
                                            CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
        if (symbol is not ITypeSymbol typeSymbol)
        { return null; }

        var properties = typeSymbol.GetMembers().OfType<IPropertySymbol>();
        var options = new List<OptionModel>();
        foreach (var property in properties)
        {
            // since we do not know how big this list is, so we will check cancellation token
            // REVIEW: Should this return null or throw?
            if (cancellationToken.IsCancellationRequested)
            { return null; }
            var description = GetPropertyDescription(property);
            options.Add(new OptionModel(property.Name, property.Type.ToString(), description));
        }
        return new CommandModel(typeSymbol.Name, options);

        static string GetPropertyDescription(IPropertySymbol prop)
        {
            // REVIEW: Not crazy about the repeated Parsing of small things.
            var doc = prop.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(doc))
            { return ""; }
            var xDoc = XDocument.Parse(doc);
            var desc = xDoc.DescendantNodes()
                .OfType<XElement>()
                .FirstOrDefault(x => x.Name == "summary")
                ?.Value;
            return desc is null
                ? ""
                : desc.Replace("\n","").Replace("\r", "").Trim();
        }

    }
}
