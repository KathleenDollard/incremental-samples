using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IncrementalGeneratorSamples
{
    internal static class Helpers
    {
        public static string AsPublicSymbol(this string val)
           => char.IsUpper(val[0])
            ? val
            : char.ToUpperInvariant(val[0]) + val.Substring(1);

        public static string AsPrivateSymbol(this string val)
            => char.IsLower(val[0])
            ? val
            : char.ToLowerInvariant(val[0]) + val.Substring(1);

        public static string InQuotes(this string val)
            => $@"""{val}""";

        public static string AsKebabCase(this string val)
        {
            // this is not particularly performant or correct
            return char.ToLower(val[0]).ToString() +
                string.Join("",
                            val.Skip(1).Select(c => char.IsUpper(c) ? $"-{char.ToLower(c)}" : c.ToString()));
        }

        public static IEnumerable<AttributeValue>
            AttributenamesAndValues(this ISymbol symbol)
        {
            var attributes = symbol.GetAttributes();
            var list = new List<AttributeValue>();
            foreach (var attribute in attributes)
            {
                var attributeName = attribute.AttributeClass.Name.ToString();
                foreach (var pair in attribute.NamedArguments)
                {
                    var value = ValuesFromTypedConstant(pair.Value);
                    list.Add(new AttributeValue(attributeName, pair.Key, pair.Value.Type.ToString(), value));

                }
                if (!(attribute.AttributeConstructor is null))
                {
                    var parameters = attribute.AttributeConstructor.Parameters;
                    var args = attribute.ConstructorArguments;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters.Length >= i && args.Length >= i)
                        {
                            var value = ValuesFromTypedConstant(args[i]);
                            list.Add(new AttributeValue(attributeName, parameters[i].Name, parameters[i].Type.ToString(), value));
                        }
                    }
                }
            }
            return list;
        }

        private static object ValuesFromTypedConstant(TypedConstant val, int recursionDepth = 1)
        {
            recursionDepth += 1;
            return val.Kind == TypedConstantKind.Array
                                            ? ValuesFromTypedConstantArray(val.Values, recursionDepth)
                                            : val.Value;

            object ValuesFromTypedConstantArray(IEnumerable<TypedConstant> typedConstants, int innerRecursionDepth)
            {
                var list = new List<object>();
                foreach (var typedConstant in typedConstants)
                {
                    list.Add(ValuesFromTypedConstant(typedConstant, innerRecursionDepth + 1));
                }
                return list.ToArray();
            }
        }



        public static string GetXmlDescription(string doc)
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
