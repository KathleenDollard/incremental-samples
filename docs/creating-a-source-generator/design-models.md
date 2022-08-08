# Design models

Once you understand the code you plan to generate, you can explore the data you need. This data will be expressed by programmers using your generator via source code or additional files, or one of the other providers discussed in [Pipelines](../pipeline.md#providers). Understanding what data you need lets you design input that makes sense to users of your generator.

Review the generated code in your sample project and mark all of the things that will vary based on what users of your generator are doing. That will include individual symbols or words, in source code and comments. It will also include blocks of code that are repeated, where you have a block per item in your current output. It probably includes conditional blocks of code. You may want to copy your example output into a tool that allows additional formatting, such as word, and decorate the code, such as using colors or adding opening and closing markers. You may want to remove code coloration prior to doing this.

The output of this evaluation will be a domain class model for generation. Each item you need for generation needs to appear in this model. Where there are multiples and loops will be involved during generation, include a collection. If conditions will result in significantly different output, use similar classes unified via a base class or interface, or simple allow values in the model to be empty if they are unused.

You may find it helpful to create a table, spreadsheet or document listing the data you need prior to creating the domain model.

During this step, you may be inspired to create additional output samples.

Using this information design a domain model. In general, use the language of the problem you are solving - in the case of the example generator, the language of a command line interface and System.CommandLine. This isolates the language of your domain from the current input, which might change in the future, and supports multiple kinds of code output output, such as both classes and static methods as part of the same generator.

## Example

This example builds on the design of [Design output](design-output.md#example). You can see how this code is used in [Design input](design-input.md#example).

### Discovering the data

Discovering the input data needed by the generator requires evaluating the [code marked as generated in the example project](design-output.md#example). A file will be generated for each command, such as `ReadFile` or `AddLine`. Files will also be generated to support access to the commands. `Cli.g.cs` is generated as a constant partial class so that the programmer using the generator always has access to the expected static methods. This should contain no information. `Cli.partial.g.cs` and `RootCommand.g.cs` will be generated and also need to be reviewed. 

Evaluating the [code in the generated AddLine method of the example project](design-output.md#example) shows these values will be needed for the generator:

| Kind|Name|Type|Files|
|---------|-------------|------------------|----------------------------------------------------|
| Command | Namespace   | string           | Cli.partial.g.cs, \<command>.g.cs |
| Command | Name        | string           | \<command>.g.cs                                     |
| Command | DisplayName | string           | \<command>.g.cs                                     |
| Command | Aliases     | list of strings  | \<command>.g.cs                                     |
| Command | Description | string           | \<command>.g.cs                                     |
| Command | Options     | list of options  | \<command>.g.cs                                     |
| Command | Commands    | list of commands | Cli.partial.g.cs                                   |
| Option  | Name        | string           | \<command>.g.cs                                     |
| Option  | DisplayName | string           | \<command>.g.cs                                     |
| Option  | Aliases     | list of strings  | \<command>.g.cs                                     |
| Option  | Description | string           | \<command>.g.cs                                     |
| Option  | Type        | string           | \<command>.g.cs                                     |

Domain knowledge is also helpful. Because the sample has names that are compound words, the need for a name and a separate display name in kebab case could be discovered in the sample source code. However, System.CommandLine supports aliases and that feature was not used in the sample. There are also many more System.CommandLine features not supported in this example.

### Creating the model

Once the data is understood, creating a model is a matter of creating classes that correspond to the required data. It is essential that these models have [value equality] and special care is needed to ensure deep value equality. Deep equality means that the type and all subtypes use value equality. If you are [using C# 9 or above](), records are recommended, but by default they have shallow rather than deep equality:

[[ Review: what is the simplest fully correct way to get value equality here ??]]

These classes use constructors to ensure that if values are added to the model all instance creation is also updated:

```csharp
public class CommandModel
{
    public CommandModel(string name,
                        string displayName,
                        IEnumerable<string> aliases,
                        string description,
                        string nspace,
                        IEnumerable<OptionModel> options)
    {
        DisplayName = displayName;
        Name = name;
        Aliases = aliases;
        Description = description;
        Namespace = nspace;
        Options = options;
    }

    public string Namespace { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public IEnumerable<string> Aliases { get; }
    public string Description { get; }
    public IEnumerable<OptionModel> Options { get; }

    public override bool Equals(object obj)
    {
        return Equals(obj as InitialClassModel);
    }

    public bool Equals(InitialClassModel other)
    {   // REVIEW: Does this box individual elements? Do we care if things are strings?
        return StructuralComparisons.StructuralEqualityComparer.Equals(this, other);
    }

    public override int GetHashCode()
    {
        // REVIEW: Does this box individual elements? Do we care if things are strings?
        return StructuralComparisons.StructuralEqualityComparer.GetHashCode(this);
    }
}
```

The `OptionModel` is similar:

```csharp
using System.Collections.Generic;

namespace IncrementalGeneratorSamples.InternalModels
{
    public class OptionModel 
    {
        public OptionModel(string name,
                           string displayName,
                           IEnumerable<string> aliases,
                           string description,
                           string type)
        {
            Name = name;
            DisplayName = displayName;
            Aliases = aliases;
            Description = description;
            Type = type;
        }

        public string Type { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public IEnumerable<string> Aliases { get; }
        public string Description { get; }

        // Default equality and hash is fine here
    }
}
```

A special model is not required for the collection of commands. This will be provided during generation.

Next Step: [Initial extraction](initial-extraction.md).
