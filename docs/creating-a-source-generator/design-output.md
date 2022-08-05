---
title: Designing generator output
description: The first step in creating a generator is understanding what you want to output.
author: KathleenDollard
ms.author: kdollard
ms.date: 6/11/2022 
ms.topic: conceptual
---
# Design generator output

The first step of a generator is determining what you want to output. Failing to this correct initially will result in painful rework of your generator. It is strongly recommended that you create at least one fully working sample project. Place the code you intend to generate in a subdirectory with an obvious name, like `ToBeGenerated`, and consider placing any supporting code in a separate library. You can leverage your sample project later for end-to-end testing.

Identify the code that makes up your working application or library in one of these categories;

- _User written code_ that the user will write as part the their project.
- _Supporting code_, probably in a separate project/library.
- _Generated code_.

Your generator will extract information from a user written code, and possibly other inputs, transform that data to a format friendly to generation, and output generated code that may use supporting code during execution.

[The overview covers limitations and rules for generation](../overview.md#limitations-of-generators).

Test the application or library with unit tests or with manual tests to ensure it works correctly.

## Partial classes and methods

Sometimes code you will generate is logically part of a class that also contains code the user writes. This is common because the job of Roslyn generators is to extend existing code. You can split the code of a class between two files using the partial keyword:

```csharp
partial class A
{
    int B = 5;
}

partial class A
{
    int C = 7;
}
```

This results in a single class `A` that has a `B` and a `C` property. The compiler literally creates a single class in the semantic model and merges features such as XML comments and attributes, as well as class members. Modifiers cannot conflict on the two portions of the partial class.

Partial classes may contain partial methods. There are two types of partial methods that are distinguished by whether there is a scope, such as `public` or `private`. In both types of partial methods, not more than one of the partial methods can have an implementation. The two types imply whether the user or the generator supplies the implementation.

All of the portions of a partial class must be in the same assembly (project) and namespace. This makes them very useful for interactions between user 

### Partial methods without a specified scope

Partial method declarations that do not have a scope must have a return type of `void`, cannot include `out` parameters and cannot include the modifiers `virtual`, `override`, `sealed`, `new`, or `extern`. 

These methods generally appear without an implementation in generated code and allow the user to create an implementation in the file of the partial class they control. You might use this to allow the user to add extra runtime configuration, for example.

If the user does not implement the partial class, it is erased from the emitted code, meaning it is completely removed. The restrictions on what you cannot do with a partial method that does not have a scope are to allow it to be erased. The user can do things like set property or field values and call code additional code.

There is no restriction on this style of empty partial methods being used only in generated code, but that is the common use.
 
### Partial methods with implementation

Partial methods that do have a scope can have a return type, out parameters and modifiers valid on methods. The restriction on these partial methods is that they much have an implementation in one of the partial classes.

These methods generally appear without an implementation in user code. This is used to signal to a generator that it needs to create an implementation. You might use this to create a method implementation based on information in an attribute. This is done in the RegEx generator introduced in .NET 6.

There is no restriction on this style of empty partial methods being used only in user code, but that is the common use.

> [!IMPORTANT]
> Partial methods with scope were introduced in C# 9 and are not available in C# 7.3. C# 7.3 is the only C# version supported on .NET Standard 2.0, and generators must target .NET Standard 2.0.

## Example

This walk-through example creates a simplified [System.CommandLine ]() CLI using a class to define commands and properties to define options. The example does not support many features of System.CommandLine such as arguments or subcommands. The goal is to allow the simplest possible definition of a CLI and to avoid the user needing to know anything about System.CommandLine itself.

This section shows the full example code, and how it is split across code the user will write, runtime support and generated code. Much of this section involves how System.CommandLine works. It also illustrates how to use partial classes and base classes to support generation. This section does not explain the System.CommandLine code itself which is a variation of theThe [example CLI from the System.CommandLine help]() and is a simple CLI to read a file and add lines to it.

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

The `Cli` class is discussed below in [The generated code], along with its design challenges.

The entry point is the same regardless of what CLI is being created. The CLI is defined in two classes that are marked with the `[Command]` attribute. The XML comments in these classes are important because the generator uses them to create help for the command and option:

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
        // output the file to the console
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
        // Write a new line to the file
        return 0;
    }
}
```

Note that these are partial classes because the generator will add more code to each of them.

This is all the user needs to write. The rest is handled by the generator.

### Runtime support for attributes

The user code references two attributes: the `CommandAttribute` and the `AliasAttribute`. Both of these will be included in a separate runtime project. The `CommandAttribute` is a marker and has no properties:

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
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AliasAttribute : Attribute
    {
        public AliasAttribute(string alias)
        { Alias = alias; }

        public string Alias { get; }
    }
}
```

Managing project and package references during development can be challenging and you can find some solutions in the article [Managing packages and deploying to NuGet](deploying-nuget.md).

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

`#nullable enable` is required to support nullable in the application. Users of your generator that have nullable enabled may be frustrated if your generator does not support it.

The `#pragma warning` is needed because the `rootCommand` may be set in the `SetRootCommand` method, and thus should not be `readonly`. Generated code is compiled in the context of the application your user's are creating. Assume they have stringent warnings and errors to avoid creating a problem for them that they cannot fix.

`SetRootCommand` is a [partial method without a scope](#partial-methods-without-a-specified-scope). This means the call to it will be removed if there is no partial class that implements the method. If a project references this generator, but does not have any class marked with the `[Command]` attribute, the other portion of this partial class will not be generated, `SetRootCommand` will have a value of null and a message will be displayed to the user. 

Both parts of the partial class  must be in the user assembly, so the file above must be generated. If they were combined into one file, it would need to contain conditional code to manage their being no commands, and it would have to always be generated so the `Cli` class would appear in IntelliSense.

When the source code includes classes that are decorated with the `[Command]` attribute, additional files are generated. The `cli.partial.g.cs` contains the implementation for the partial `SetRootCommand` method. The only variable part of this file is the namespace of the `RootCommand`:

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

The file `cli.g.cs` always exists when there is a package reference to the generator and the file `cli.partial.g.cs` is present only when there is at least one class in the user's source code marked with the `Command` attribute. When that attribute is not present and the `cli.partial.g.cs` file is not present, there is no `SetRootCommand` implementation and the call to it is removed during compilation. Partial classes here support the different behavior depending on whether the user is accessing features of the generator in their code.

in addition to the `Cli` class, a partial class is created for each class the user marked with the `Command` attribute. This class is System.CommandLine details with comments explaining the actions. Here is the partial class created for the `Addline` class shown above:

```csharp
// generated file: AddLine.g.cs 
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class AddLine
{
    internal class CommandHandler : CommandHandlerBase
    {
        // Manage singleton instance
        public static AddLine.CommandHandler Instance { get; } = new AddLine.CommandHandler();

        // Create System.CommandLine options
        private Option<System.IO.FileInfo?> fileOption = new Option<System.IO.FileInfo?>("--file", "The file to read and display on the console.");
        private Option<string> lineOption = new Option<string>("--line", "Delay between lines, specified as milliseconds per character in a line.");

        // Base constructor creates System.CommandLine.Command and options are added here
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

This adds minimal members to the user's class because adding unnecessary elements to the IntelliSense displayed for their classes may be surprising to the programmer. It only adds the `CommandHandler` nested class. A nested class is helpful to keep generated implementation details tucked out of sight. The `CommandHandler` nested class includes a singleton property that ensures there is only one instance of the type. It also has fields for the options that correspond to the properties of the `AddLine` class. In the constructor, it adds these options to the internal SystemCommandLine `Command` instance that is created in the base class. The `AddLine` class had an `Execute` method, so an `Invoke` method is included in the generated class.

The `RootCommand` is generated to manage the specific commands, such as `AddLine`. The Cli has exactly one root command. Similar to the individual commands, this class has a `CommandHandler` nested class with a singleton `Instance` property:

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

The constructor of the generated `RootCommand` adds all of the generated commands to root command.

The `RootCommand` nested `CommandHandler` class inherits from the `RootCommandHandler` class of the `IncrementalSamples.Runtime` library. `IncrementalSamples.Runtime.RootCommandHandler` inherits from  `IncrementalSamples.Runtime.CommandHandler` shown below and adds special handling for the command. Code in `Cli.g.cs`  uses the `RootCommand` property:

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
                : throw new InvalidOperationException("Unexpected type");
    }
}
```

Each `CommandHandler` nested class class derives from the `CommandHandlerBase` class. This base class manages the System.CommandLine details and is part of the runtime library for the example project:

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

The explicit interface implementation along with a protected virtual property provide the Invoke method only when cast to `ICommandHandler`. This is helpful because the method is expected to be called only from the root command. Otherwise this is just the System.CommandLine details to complete the example project.

## Testing

Before creating your generator, ensure that you are creating the correct code by testing your sample project through a combination of manual and unit tests. 

Next Step: [Design models](design-models.md)