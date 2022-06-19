namespace IncrementalGeneratorSamples.Models;

public record CommandModel
{
    private List<CommandModel> commands = new();
    public CommandModel(string commandName, IEnumerable<OptionModel> options)
    {
        CommandName = commandName;
        Options = options;
    }

    public string CommandName { get; }


    public IEnumerable<OptionModel> Options { get; }
    public IEnumerable<CommandModel> SubCommands => commands;

    public virtual bool Equals(CommandModel model)
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
