---
title: Designing generator input
description: Before coding your generator, understand where you will get the necessary input.
author: KathleenDollard
ms.author: kdollard
ms.date: 6/11/2022 
ms.topic: conceptual
---
# Design generator input

Input data can come from several sources as discussed in [pipelines subsection]

When source generators use C# or VB code as input, they often create special rules that are more strict than C# syntax. This code is not executed during generation, and in many cases is never executed. You must extract information from the code itself, and that information must be available to the compiler. There is minimal ability to understand calculations and control flow.

Attributes are useful in designing input, in addition to being very fast during generation. The RegEx generator in .NET 7 is a good example. This is an optimizing generator, and the code prior to generation looks like:

```csharp
private static readonly Regex s_myCoolRegex = new Regex("abc|def", RegexOptions.Compiled | RegexOptions.IgnoreCase);
```

Using the RegexGenerator attribute, this becomes:

```csharp
[RegexGenerator("abc|def", RegexOptions.IgnoreCase)]
private static partial Regex MyCoolRegex();
```

Because the information for generation are compile time constants, the programmer using the generator won't be be able to supply a calculation.

An example of of a potentially problematic design would be assigning values to properties or fields, or function arguments. The user is accustomed to being able to use any C# code, and you cannot evaluate their intent. For example, if you allow the first of these, the user will not immediately understand why you do not allow the second:

```csharp
var procCount = 4;
var procCount = System.Environment.ProcessorCount;
```

Since you are not running code, there is no way to evaluate the code that requests the processor count. If that was important to your generator, you could look for that specific call, but you would then fail if the user created their own method that returned the processor count.

Whenever possible, extract the input data you need from attribute values and from the natural structure of code. the down 

## Example

The example source generator will use System.CommandLine to create a simple CLI. The goal of the generator is to let you create a CLI without having to learn System.CommandLine. You want the user to be able to invoke their CLI as simply as possible, which in a program with top level statements would look like:

```csharp
Cli.Invoke(args);
```

Defining the CLI to allow this simple invocation requires defining the options and arguments, as well as what code to run. This shape of the CLI is expressed as the shape of the class. Using an example from the [System.CommandLine documentation]():

```csharp
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
        { return 1; }
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
        { return 1; }
        using StreamWriter? writer = File.AppendText();
        writer.WriteLine(Line);
        writer.Flush();
        return 0;
    }
}
```

This CLI has two commands, each of which is identified with the `[Command]` attribute: `ReadFile` and `AddLine`. `ReadFile` has a single option named `File` and `AddLine` has options for `File` and `Line`. They each have a `Execute` method that runs to perform the work the user requests. Descriptions for the commands and options will be retrieved from the XML comments.

It's helpful to review the code you intend to create and record the information your generator will require, along with its source:

|Data|Source|
|-|-|
|Command name| Class name|
|Command description|Class XML comment|
|Command return type |Execute method return type. System.CommandLine supports only `int`, `Task<int>`, `void` and `Task`|
|Option names| Property name|
|Option type| Property type. Array or IEnumerable would indicate that multiple values are allowed|
|Option description|Option XML comment|

There are restrictions on some of the values the user can enter, and it is good to identify the analyzers that you probably want to write to accompany your generator. In this case, there are a small number of valid return types from the execute method and the only generics that are allowed in option types is `IEnumerable<T>`.

This is a greatly simplified approach to `System.CommandLine` and if you would like to further explore generation, you might want to add features, such as adding arguments or subcommands.

Next Step: [Create models](create-models.md).
