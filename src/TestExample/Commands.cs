using IncrementalGeneratorSamples.Runtime;
using System.Drawing;
using System.IO;

namespace TestExample;

[Command]
/// <summary>
/// Output the contens of a file to the console.
/// </summary>
public partial class ReadFile
{
    /// <summary>
    /// The file to read and display on the console.
    /// </summary>
    public FileInfo? File { get;  }

    public int DoWork() 
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
    /// <summary>
    /// The file to read and display on the console.
    /// </summary>
    public FileInfo? File { get; }

    /// <summary>
    /// Delay between lines, specified as milliseconds per character in a line.
    /// </summary>
    public string Line { get; }

    public int DoWork()
    {
        if (Line is null || File is null)
        { return 1; }
        using StreamWriter? writer = File.AppendText();
        writer.WriteLine(Line);
        writer.Flush();
        return 0;
    }
}
