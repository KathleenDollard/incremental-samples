using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples
{
    internal static class Extensions
    {
        public static string AsProperty(this string val)
           => char.ToUpperInvariant(val[0]) + val.Substring(1);
        public static string AsField(this string val)
            => char.ToLowerInvariant(val[0]) + val.Substring(1);
        public static string AsAlias(this string val)
            => char.ToLowerInvariant(val[0]) + val.Substring(1);
        public static string InQuotes(this string val)
            => @$"""{val}""";
        public static string KebabCase(this string val)
        {
            // this is not particularly performant or correct
            return char.ToLower(val[0]).ToString() + string.Join("", val.Skip(1).Select(c => char.IsUpper(c) ? $"-{char.ToLower(c)}" : c.ToString()));
        }
    }
}
