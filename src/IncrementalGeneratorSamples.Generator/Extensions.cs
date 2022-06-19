namespace IncrementalGeneratorSamples
{
    internal static class Extensions
    {
        public static IEnumerable<TItem> WhereNotNull<TItem>(this IEnumerable<TItem> enumerable)
              => enumerable is null
                    ? Enumerable.Empty<TItem>()
                    : enumerable.Where(item => item is not null);


        public static string AsProperty(this string val)
           => char.ToUpperInvariant(val[0]) + val.Substring(1);
        public static string AsField(this string val)
            => char.ToLowerInvariant(val[0]) + val.Substring(1);
        public static string AsAlias(this string val)
            => char.ToLowerInvariant(val[0]) + val.Substring(1);
        public static string InQuotes(this string val)
            => @$"""{val}""";
        public static string KebabCase(this string val)
            // this is not particularly performant or correct
            => string.Join("", val.Select(c => char.IsUpper(c) ? $"-{char.ToLower(c)}" : "c"));
}
}
