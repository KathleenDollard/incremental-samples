# Output code

Before outputting code you need a clear understanding of [the code you want to output](design-output.md) and [the domain model](create-models.md) containing the data that drives generation.

The easiest way to output code in recent versions of C# is interpolated strings. The complexity of outputting code depends on the complexity of what you are outputting and you can manage this complexity by calling methods within the expressions of interpolated strings. In normal interpolated strings, you double any curly brackets and double quotes that should appear in your code. If you are [using C# 11 or later](), consider [raw interpolated strings](https://docs.microsoft.com/dotnet/csharp/programming-guide/strings/).

To begin generating code, copy your sample code for a file into an interpolated string. In newer versions of Visual Studio, if you copy C# code into a verbatim interpolated string, the curly brackets and double quotes will be doubled, which is required to escape them so they appear correctly in output: 

```csharp
    var x = $@"
    // Copy code here to automate doubling curly brackets
"
```

Break complex code into methods that return fragment of the code you want to create. This is particularly helpful when you have code that in conditionally included or when you need a block of code for every item in a collection.

## Example

This example builds on the design of the [Further transformations article](further-transformations.md#example). You can see how this code is used in [Putting it all together](putting-it-all-together.md#example).

This example generates code for three different file patterns:

- A `cli.g.cs` file with a partial class that is always present and allows the user to interact with the CLI and get started with the generator. 
- A `cli.partial.g.cs` file with a partial class that is generated only when there are commands and defines the commands and allows it to be run.
- A separate file for each command that defines command details and allows it to be run.

You can see the desired output of generation in the [Design output example](design-output.md/#example).

### Generating `cli.g.cs`

The `cli.g.cs` file is output as a constant, so the code to generate can be a method or property that returns the string. Generally, no interpolation is needed because there is no variable text:

```c#
        public const string ConsistentCli = @"
using System.CommandLine;

#nullable enable

namespace IncrementalGeneratorSamples
{
    internal partial class Cli
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private static System.CommandLine.RootCommand? rootCommand = null;
#pragma warning restore IDE0044 // Add readonly modifier

        public static int Invoke(string[] args)
        {
            SetRootCommand();
            if (rootCommand is null)
            {
                Console.WriteLine(""No classes were marked with the [Command] attribute"");
                return 1;
            }
            return rootCommand.Invoke(args);
        }

        static partial void SetRootCommand();
    }
}
";
```

### Generating `cli.partial.g.cs`

The `cli.partial.g.cs` file includes a statement for each method decorated with the `Command` attribute which adds the command specific generated class created in the next section to the `Cli` class. This method uses a method that loops through the commands to output these statements:

```csharp
   public static string PartialCli(RootCommandModel rootCommandModel, CancellationToken _)
    {
        return rootCommandModel is null
                    ? ""
                    : $@"
using IncrementalGeneratorSamples.Runtime;
using System.CommandLine;

#nullable enable

namespace IncrementalGeneratorSamples
{{
    internal partial class Cli
    {{
        static partial void SetRootCommand()
        {{
            var rootHandler = CommandHandler.Instance;
            rootCommand = rootHandler.RootCommand;
        }}

        internal class CommandHandler : RootCommandHandler<CommandHandler>
        {{
            // Manage singleton instance
            public static CommandHandler Instance = new CommandHandler();

            public CommandHandler() : base(string.Empty)
            {{
                {CtorAssignments(rootModel.Namespace, rootModel.CommandSymbolNames)}
            }}
        }}
    }}
}}

";
        string CtorAssignments(string nspace, IEnumerable<string> commandNames)
            => string.Join("\n            ", 
                commandNames.Select(c => 
                    $"Command.Add({nspace}.{c}.CommandHandler.Instance.Command);"));

    }
```

A check for `null` ensures that code output doesn't crash if something unexpected occurs.

The only portion of this file that is changed from copying in the sample code of the [Design output example](design-output.md#cli-classes) is the call to the `CtorAssignments` local function. The local function uses the LINQ `Select` method that uses interpolated strings to create a string for each command, and then concatenates them with a new line character using `string.Join`.

### Command specific methods

A file containing a partial class is created for each class decorated with the `Command` attribute. The interpolated string for this method was also copied from an example. Generating complex files with many loops and conditional can be very difficult to read without thoughtful use of methods within interpolated string expressions:

```csharp
        public static string CommandCode(CommandModel commandModel, CancellationToken cancellationToken)
        {
            return commandModel is null
                ? ""
                : $@"
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace {commandModel.Namespace};

public partial class {commandModel.Name.AsSymbol()}
{{
    internal class CommandHandler : CommandHandlerBase
    {{
        // Manage singleton instance
        public static {commandModel.Name.AsSymbol()}.CommandHandler Instance {{ get; }} = new {commandModel.Name.AsSymbol()}.CommandHandler();

        // Create System.CommandLine options
        {OptionFields(commandModel.Options)}

        // Base constructor creates System.CommandLine and options are added here
        private CommandHandler()
            : base({commandModel.DisplayName.InQuotes()}, {commandModel.Description.InQuotes()})
        {{
            {CommandAliases(commandModel)}
            {OptionAssignments(commandModel.Options)}
        }}

        // The code invoked when the user runs the command
        protected override int Invoke(InvocationContext invocationContext)
        {{
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new {commandModel.Name.AsSymbol()}({CommandParams(commandModel.Options)});
            return command.Execute();
        }}
    }}
}}
";
```

After copying the sample code into the interpolated string, the namespace and class names were replaced with the corresponding values of the models. Creating source code almost always includes adjusting the casing of model names to C# standards. The `AsSymbol()` and `AsLocalSymbol` do these modifications. If you prefix field names with an underscore, you will also need a method for that. It is much easier to read code where field names appear with an extension method than keeping up with underscores.

```csharp
private string {model.Name.AsField()};
```

Another place extension methods can be easer to read is the wrapping of strings in quotations, such as in the call to the `CommandHandler` base constructor. When you are reading the code of templates this clarifies the intent, and it ensures that the double quotes are always matched correctly.

Statements in the template can be long if there are several interpolated expressions, such as when the singleton is created here. While this can be annoying, if you wrap this code for easier reading in the template, you will have unneeded wrapping in the generated code.

This code uses local functions as helper methods that follow the pattern of the LINQ `Select` method returning an `IEnumerable<string>` which is concatenated with a newline and spaces using the `string.Join` method:


```csharp
    string CommandAliases(CommandModel model) 
        => model.Aliases is null || !model.Aliases.Any()
            ? ""
            : string.Join("\n            ", model.Aliases.Select(a => $"Command.AddAlias({a});"));

    string OptionFields(IEnumerable<OptionModel> options)
        => string.Join("\n        ", options.Select(o =>
            $"private Option<{o.Type}> {o.Name.AsLocalSymbol()}Option = new Option<{o.Type}>({OptionAliases(o)}, {o.Description.InQuotes()});"));

    string OptionAliases(OptionModel option)
    {
        var aliases = new List<string>() { option.DisplayName.InQuotes() };
        aliases.AddRange(option.Aliases);
        return string.Join(", ", aliases);
    }

    string OptionAssignments(IEnumerable<OptionModel> options)
        => string.Join("\n            ", options.Select(o => $"Command.AddOption({o.Name.AsLocalSymbol()}Option);"));

    string CommandParams(IEnumerable<OptionModel> options)
        => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.Name.AsLocalSymbol()}Option, commandResult)"));

        }```

In more complex scenarios the helper methods may themselves call additional helper methods, particularly when there are conditional and looping blocks of code that are themselves complex. 

Next Step: [Putting it all together](putting-it-all-together.md)