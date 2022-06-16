namespace IncrementalGeneratorSamples.Models;

public record GenerationModel
{

    public GenerationModel(string commandName, IEnumerable<OptionModel> options)
    {
        CommandName = commandName;
        Options = options;
    }

    public string CommandName { get; }


    public IEnumerable<OptionModel> Options { get; }

    public virtual bool Equals(GenerationModel model)
        => model is not null && 
            model.CommandName == CommandName &&
            model.Options.SequenceEqual(this.Options);

    public override int GetHashCode()
    {
        var hash = CommandName.GetHashCode();
        foreach(var prop in Options)
        {
            hash ^= prop.GetHashCode();
        }
        return hash;
    }
}
