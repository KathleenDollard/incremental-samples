namespace IncrementalGeneratorSamples.Models;

public record OptionModel
{
    public OptionModel(string name, string type, string description)
    {
        Name = name;
        Type = type;
        Description = description;
    }

    public string Name { get; }
    public string Type { get; }
    public string Description { get; }
}