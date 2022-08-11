//namespace IncrementalGeneratorSamples.Test
//{
//    internal class SampleCode
//    {
//        internal const string SimpleClass = @"
//using IncrementalGeneratorSamples.Runtime;

//[Command]
//public partial class Command
//{
//    public int Delay { get;  }
//}
//";

//        internal const string ClassWithXmlComment = @"
//using IncrementalGeneratorSamples.Runtime;

//[CommandAttribute]
//public partial class Command
//{
//    /// <summary>
//    /// Delay between lines, specified as milliseconds per character in a line.
//    /// </summary>
//    public int Delay { get;  }
//}
//";

//        internal const string CompleteClass = @"
//using IncrementalGeneratorSamples.Runtime;
//using System.IO;

//#nullable enable

//[Command]
//public partial class CompleteCommand
//{
//    /// <summary>
//    /// The file to read and display on the console.
//    /// </summary>
//    public FileInfo? File { get;  }

//    /// <summary>
//    /// Delay between lines, specified as milliseconds per character in a line.
//    /// </summary>
//    public int Delay { get;  }

//    public int DoWork() 
//    {
//        // do work, such as displaying the file here
//        return 0;
//    }
//}
//";
//    }
//}
