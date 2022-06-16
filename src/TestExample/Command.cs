using IncrementalGeneratorSamples.Runtime;

namespace TestExample;

[Command]
public partial class Command
{
    /// <summary>
    /// The file to read and display on the console.
    /// </summary>
    public FileInfo? File { get;  }

    /// <summary>
    /// Delay between lines, specified as milliseconds per character in a line.
    /// </summary>
    public int Delay { get;  }

    public int DoWork() 
    {
        // do work, such as displaying the file here
        return 0;
    }
}
