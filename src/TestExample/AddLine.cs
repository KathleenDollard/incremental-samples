using IncrementalGeneratorSamples.Runtime;

namespace TestExample;

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
