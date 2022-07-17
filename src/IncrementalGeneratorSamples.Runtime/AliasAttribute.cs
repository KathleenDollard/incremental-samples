using System;

namespace IncrementalGeneratorSamples.Runtime
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Class| AttributeTargets.Struct,
        Inherited = false, AllowMultiple = true)]
    public sealed class AliasAttribute : Attribute
    {
        public AliasAttribute(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; }
    }
}