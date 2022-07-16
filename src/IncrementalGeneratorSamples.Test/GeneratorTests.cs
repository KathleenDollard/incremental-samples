using Microsoft.CodeAnalysis;

namespace IncrementalGeneratorSamples.Test
{
    public class GeneratorTests
    {
        private const string SimpleClass = @"
using IncrementalGeneratorSamples.Runtime;

[Command]
public partial class ReadFile
{
    public int Delay { get;  }
}
";
        private string expected1 = @"
using System.CommandLine;

namespace TestExample
{
    internal partial class Cli
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private static System.CommandLine.RootCommand? rootCommand = null;
#pragma warning restore IDE0044 // Add readonly modifier

        public static void Invoke(string[] args)
        {
            SetRootCommand();
            if (rootCommand is null)
            { throw new InvalidOperationException(""No classes were mared with the [Command] attribute""); }
            rootCommand.Invoke(args);
        }

        static partial void SetRootCommand();
    }
}
";
        private string expected2 = @"
namespace TestExample
{
    internal partial class Cli
    {
        static partial void SetRootCommand()
        {
            var rootHandler = RootCommand.CommandHandler.GetHandler();
            rootCommand = rootHandler.SystemCommandLineRoot;
        }
    }
}
";

        private string expected3 = @"
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class ReadFile
{
    internal class CommandHandler : CommandHandler<CommandHandler>
    {
        Option<int> delayOption = new Option<int>(""--delay"", """");

        public CommandHandler()
            : base(""read-file"", """")
        {
            SystemCommandLineCommand.AddOption(delayOption);
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new ReadFile(GetValueForSymbol(delayOption, commandResult));
            return command.DoWork();
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }
    }
}
";
        private string expected4 = @"
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class RootCommand
{
    public static void Invoke(string[] args)
        => CommandHandler.Invoke(args);

    internal class CommandHandler : RootCommandHandler<CommandHandler>
    {
        public CommandHandler() : base(string.Empty)
        {
            SystemCommandLineCommand.Add(ReadFile.CommandHandler.GetHandler().SystemCommandLineCommand);
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {
            Console.WriteLine(""Enter one of the commands. Use '-h' to get a list of available commands"");
            return 1;
        }

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }
    }
}
";


        [Fact]
        public void Can_compile_input()
        {
            var inputCompilation = TestHelpers.GetInputCompilation<Generator>(OutputKind.DynamicallyLinkedLibrary, SimpleClass);
            Assert.NotNull(inputCompilation);
            Assert.Empty(TestHelpers.ErrorAndWarnings(inputCompilation));
        }

        [Fact]
        public void Can_generate_test()
        {
            var inputCompilation = TestHelpers.GetInputCompilation<Generator>(
                OutputKind.DynamicallyLinkedLibrary, SimpleClass);
            Assert.NotNull(inputCompilation);
            Assert.Empty(TestHelpers.ErrorAndWarnings(inputCompilation));

            var (outputCompilation, runResult) = TestHelpers.GenerateTrees<Generator>(
                inputCompilation);
            var trees = runResult.GeneratedTrees;
            Assert.NotNull(outputCompilation);
            Assert.Empty(TestHelpers.ErrorAndWarnings(runResult.Diagnostics));

            Assert.Equal(4,trees.Count());
            Assert.Equal(expected1, trees[0].ToString());
            Assert.Equal(expected2, trees[1].ToString());
            Assert.Equal(expected3, trees[2].ToString());
            Assert.Equal(expected4, trees[3].ToString());
        }
    }
}
