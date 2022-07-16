namespace IncrementalGeneratorSamples.InternalModels
{
    public class OptionModel : ModelCommon
    {
        public OptionModel(string name,
                           string originalName,
                           string publicSymbolName,
                           string privateSymbolName,
                           string description,
                           string type)
        : base(name, originalName, publicSymbolName, privateSymbolName, description)
        {
            Type = type;
        }

        public string Type { get; }

        // Default equality and hash is fine here
    }
}