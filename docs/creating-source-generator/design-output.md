# Design source code output

The first step of a generator is determining what you want to output. Failing to get this initially get this correct will result in inefficient rework of your generator, so it is strongly recommended that you create at least one fully working test project. Place the code you intend to generate in a subdirectory with an obvious name, like `ToBeGenerated`. You can leverage this process later for end-to-end testing.

The first step is determining what you want users of your generator to create.

The next step is determining what supporting classes you need and probably creating a runtime project. If the user's code references types such as attributes and base types, they must be available when the user's application runs. They must appear either in the user's source code, or in a separate library referenced by both the generator and user's code. The separate runtime is preferred to avoid duplicating types and to have better behavior if the generator fails or is removed.

The next step is creating the code you will later generate.

The final step is testing your sample project, at least with manual tests. 

Namespaces and partial organziaton.

## Example

The example creates a simplified [System.CommandLine ]() CLI using a class to define commands and properties to define options. The example is simplified and does not support arguments, subcommands, or all System.CommandLine features. The goal is to allow the simplest possible definition of a CLI and to avoid the user needing to know anything about System.CommandLine itself.

The code of the generated application comes from three sources: the code the user enters, the generated code, and supporting runtime code. Depending on your style and the problem you are working on, you may start by creating the generated code or a sample of the code the user supplies. these three sets of code are interdependent and you are likely update each as you better understand the others.

Much of this section involves how System.CommandLine works. It also illustrates how to use partial classes, partial methods, and base classes to support generation, as well as an example of how to design user code that supports generation.

### The code the user writes

The user creates an entry point of the application either as an explicit `Main` method or When using top-level commands. Using top-level commands this is:

```csharp
namespace IncrementalGeneratorSamples;
Cli.Invoke(args);
```

Or without top-level commands:

```csharp
namespace IncrementalGeneratorSamples;

internal class Program
{
    private static void Main(string[] args)
    {
        Cli.Invoke(args);
    }
}
```

The entry point is the same regardless of what CLI is being created. The [example CLI used here is from the System.CommandLine help]() and is a simple CLI to read a file and add lines to it. The CLI is defined in two classes that are marked with the `[Command]` attribute:

```csharp
using IncrementalGeneratorSamples.Runtime;

namespace TestExample;

[Command]
/// <summary>
/// Output the contents of a file to the console.
/// </summary>
public partial class ReadFile
{
    internal ReadFile(FileInfo? file)
    {
        File = file;
    }

    /// <summary>
    /// The file to read and display on the console.
    /// </summary>
    public FileInfo? File { get; }

    public int Execute()
    {
        if (File is null)
        {
            Console.WriteLine("No file name was specified.");
            return 1;
        }
        List<string> lines = System.IO.File.ReadLines(File.FullName).ToList();
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        };
        return 0;
    }
}

[Command]
/// <summary>
/// Add a new line to a file.
/// </summary>
public partial class AddLine
{
    internal AddLine(FileInfo? file, string line)
    {
        File = file;
        Line = line;
    }

    /// <summary>
    /// The file to read and display on the console.
    /// </summary>
    public FileInfo? File { get; }

    /// <summary>
    /// Delay between lines, specified as milliseconds per character in a line.
    /// </summary>
    public string Line { get; }

    public int Execute()
    {
        if (Line is null || File is null)
        {
            Console.WriteLine("No file name was specified.");
            return 1;
        }
        using StreamWriter? writer = File.AppendText();
        writer.WriteLine(Line);
        writer.Flush();
        return 0;
    }
}
```

Note that these are partial classes because the generator will add more code to the.

This is all the user needs to write. The rest is handled by the generator.

### Runtime support for attributes

The user code references two attributes: the `CommandAttribute` and the `AliasAttribute`. Both of these will be included in a separate runtime project. The `CommandAttribute` is a marker and has not properties:

```csharp
using System;

namespace IncrementalGeneratorSamples.Runtime
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    { }
}
```

The `AliasAttribute` can appear multiple times on a command class or property, and specifies aliases:

```c#
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
```

Managing project and package references during development can be challenging and you can find some solutions in the article [Managing NuGet]().

### The generated code

This generated code of this example needs two different things: 

- A `Cli` class that allows the user to interact, and especially to `Invoke` the Cli to run the application.
- Extensions to the command class to support System.CommandLine.

The `Cli` class needs to be present when the user starts the application, it is the user's main access point. It also needs to support generated code that performs the actual invocation. This is most easily provided with two partial classes. One is created always created and contains the common portions of the `Cli` class and a partial method call:

```c#
// generated file: cli.g.cs
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
                Console.WriteLine("No classes were marked with the [Command] attribute");
                return 1;
            }
            return rootCommand.Invoke(args);
        }

        static partial void SetRootCommand();
    }
}
```

`#nullable enable` is required to support nullable in the application. The assumption is that the 

The `#pragma warning` is needed because the `rootCommand` may be set in the `SetRootCommand` method, and thus should not be `readonly`. Generated code is compiled in the context of the application your user's are creating. Assume they have stringent warnings and errors to avoid creating a problem for them that they cannot fix.

`SetRootCommand` is a partial method. It and the `RootCommand` class it specifies are generated:

```csharp
// generated file: cli.partial.g.cs
namespace IncrementalGeneratorSamples
{
    internal partial class Cli
    {
        static partial void SetRootCommand()
        {
            var rootHandler = TestExample.RootCommand.CommandHandler.Instance;
            rootCommand = rootHandler.RootCommand;
        }
    }
}
```

The file `cli.g.cs` always exists when there is a package reference to the generator. The file `cli.partial.g.cs` is present only when there is at least one class in the user's source code marked with the `Command` attribute. When that attribute is not present and the `cli.partial.g.cs` file is not present, the SetRootCommand method is removed during compilation. This is one of two styles of partial methods and you can read more about [partial methods in Microsoft docs]().

A partial class is created for each class the user marked with the `Command` attribute. Here is the partial class created for the `Addline` class shown above:

```csharp
// generated file: AddLine.g.cs 
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class AddLine
{
    internal class CommandHandler : CommandHandler<CommandHandler>
    {
        // Manage singleton instance
        public static AddLine.CommandHandler Instance { get; } = new AddLine.CommandHandler();

        // Create System.CommandLine options
        private Option<System.IO.FileInfo?> fileOption = new Option<System.IO.FileInfo?>("--file", "The file to read and display on the console.");
        private Option<string> lineOption = new Option<string>("--line", "Delay between lines, specified as milliseconds per character in a line.");

        // Base constructor cre
        private CommandHandler()
            : base("add-line", "Add a new line to a file.")
        {
            Command.AddOption(fileOption);
            Command.AddOption(lineOption);
        }

        // The code invoked when the user runs the command
        protected override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new AddLine(GetValueForSymbol(fileOption, commandResult), GetValueForSymbol(lineOption, commandResult));
            return command.Execute();
        }
    }
}
```

This has minimal additions because adding unnecessary elements to the IntelliSense displayed for their classes may be surprising to the programmer. It just adds the `CommandHandler` nested class. A nested class is helpful to keep generated implementation details tucked out of sight. The `CommandHandler` nested class includes a singleton property that ensures there is only one instance of the type. It also has fields for the options that correspond to the properties of the `AddLine` class. In the constructor, it adds these options to the internal SystemCommandLine `Command` instance that is created in the base class. The `AddLine` class had an `Execute` method, so an `Invoke` method is included in the generated class.


The base class manages the System.CommandLine details:

```csharp
// Library file in IncrementalGeneratorSamples.Runtime: CommandHandler.cs
using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace IncrementalGeneratorSamples.Runtime
{
    public abstract class CommandHandlerBase : ICommandHandler
    {

        // Constructor for all derived classes, except root command
        protected CommandHandlerBase(string name, string description = "")
           : this(new Command(name, description))
        { }

        // Constructor used directly by root command,and indirectly by others
        protected CommandHandlerBase(Command command)
        {
            command.Handler = this;
            Command = command;
        }

        // The underlying System.CommandLine Command
        public Command Command { get; private set; }

        // Helper method to retrieve data during invocation
        protected static T GetValueForSymbol<T>(IValueDescriptor<T> symbol, CommandResult commandResult)
            => symbol is Option<T> option
                ? commandResult.GetValueForOption(option)
                : throw new ArgumentOutOfRangeException();

        // ICommandHandler explicit interface implementation
        int ICommandHandler.Invoke(InvocationContext context) => Invoke(context);
        // Async not supported in this example
        Task<int> ICommandHandler.InvokeAsync(InvocationContext context) => throw new NotImplementedException();

        // If an ExecuteAsync method is present in the input, a generated override will be created
        protected virtual int Invoke(InvocationContext invocationContext)
            => throw new NotImplementedException();
    }
}

```

Access to the handler instances are managed by a singleton which can be helpful when you will have only one instance and you need to access it from multiple places. 

The explicit interface implementation along with a protected virtual property provide the Invoke method only when cast to `ICommandHandler`. This is helpful because the method is expected to be called only from the root command.

The `RootCommand` is generated to manage the specific commands, such as `AddLine`. Each Cli has exactly one root command. By default it's name is the same as the name of the executable. `RootCommand` 
the generated command partial classes like `AddLine.g.cs` in having the `CommandHandler` nested class and a singleton `Instance` property:

```c#
// generated file: RootCommand.g.cs 
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public class RootCommand
{
    internal class CommandHandler : RootCommandHandler<CommandHandler>
    {
        // Manage singleton instance
        public static RootCommand.CommandHandler Instance = new RootCommand.CommandHandler();

        public CommandHandler() : base(string.Empty)
        {
            Command.Add(ReadFile.CommandHandler.Instance.Command);
            Command.Add(AddLine.CommandHandler.Instance.Command);
        }
    }
}

```

The constructor adds all of the generated commands to root command.

The `RootCommand` nested `CommandHandler` class inherits from the `RootCommandHandler` class of the `IncrementalSamples.Runtime` library. `IncrementalSamples.Runtime.RootCommandHandler` inherits from  `IncrementalSamples.Runtime.RootCommandHandler` shown above and adds special handling for the command. In System.CommandLine, `RootCommand` inherits from `Command` and is special because as the entry point. Because of this, the code in `Cli.g.cs`  requires a `RootCommand` and the creation and casts are here: 

```csharp
// Library file in IncrementalGeneratorSamples.Runtime: RootCommandHandler.cs
using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace IncrementalGeneratorSamples.Runtime
{
    public abstract class RootCommandHandler<TCommandHandler> : CommandHandlerBase
        where TCommandHandler : RootCommandHandler<TCommandHandler>, new()
    {
        protected RootCommandHandler(string description = "")
            : base(new RootCommand(description))
        { }

        public RootCommand RootCommand
            => Command is RootCommand root
                ? root
                : throw new InvalidOperationException("The root command may have been reset.");
    }
}
```

## Testing

Before creating your generator, ensure that you are creating the correct code by testing your sample project through a combination of manual and unit tests. 