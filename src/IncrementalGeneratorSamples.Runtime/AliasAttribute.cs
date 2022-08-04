using System;

namespace IncrementalGeneratorSamples.Runtime
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AliasAttribute : Attribute
    {
        public AliasAttribute(string alias)
        { Alias = alias; }

        public string Alias { get; }
    }
}