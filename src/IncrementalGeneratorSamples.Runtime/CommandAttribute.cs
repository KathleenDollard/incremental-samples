using System;

namespace IncrementalGeneratorSamples.Runtime
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    { }
}