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
