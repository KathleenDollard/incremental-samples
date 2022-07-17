//using IncrementalGeneratorSamples.InternalModels;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;

//namespace IncrementalGeneratorSamples
//{

//    public class CodeOutput
//    {
//        public const string AlwaysOnCli = @"
//using System.CommandLine;

//namespace TestExample
//{
//    internal partial class Cli
//    {
//#pragma warning disable IDE0044 // Add readonly modifier
//        private static System.CommandLine.RootCommand? rootCommand = null;
//#pragma warning restore IDE0044 // Add readonly modifier

//        public static void Invoke(string[] args)
//        {
//            SetRootCommand();
//            if (rootCommand is null)
//            { throw new InvalidOperationException(""No classes were mared with the [Command] attribute""); }
//            rootCommand.Invoke(args);
//        }

//        static partial void SetRootCommand();
//    }
//}
//";

//        public static string PartialCli(IEnumerable<CommandModel> modelData, CancellationToken cancellationToken)
//        {
//            if (modelData is null)
//            { return ""; }

//            return $@"
//namespace TestExample
//{{
//    internal partial class Cli
//    {{
//        static partial void SetRootCommand()
//        {{
//            var rootHandler = RootCommand.CommandHandler.GetHandler();
//            rootCommand = rootHandler.SystemCommandLineRoot;
//        }}
//    }}
//}}
//";
//        }


//        public static string FileName(CommandModel modelData)
//            => $"{modelData?.Name}.g.cs";

//        public static string GenerateCommandCode(CommandModel modelData, CancellationToken cancellationToken)
//        {
//            if (modelData is null)
//            { return ""; }

//            return $@"
//using System.CommandLine;
//using System.CommandLine.Invocation;
//using IncrementalGeneratorSamples.Runtime;

//#nullable enable

//namespace TestExample;

//public partial class {modelData.Name}
//{{
//    internal class CommandHandler : CommandHandler<CommandHandler>
//    {{
//        {OptionFields(modelData.Options)}

//        public CommandHandler()
//            : base(""{modelData.Name.AsKebabCase()}"", ""{modelData.Description}"")
//        {{
//            {OptionAssign(modelData.Options)}
//        }}

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override int Invoke(InvocationContext invocationContext)
//        {{
//            var commandResult = invocationContext.ParseResult.CommandResult;
//            var command = new {modelData.Name}({CommandParams(modelData.Options)});
//            return command.DoWork();
//        }}

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override Task<int> InvokeAsync(InvocationContext invocationContext)
//        {{
//            // Since this method is not implemented in the user source, we do not implement it here.
//            throw new NotImplementedException();
//        }}
//    }}
//}}
//";
//            string Parameters(IEnumerable<OptionModel> options)
//                => string.Join(", ", options.Select(o => $"{o.Type} {o.Name.AsField()}"));

//            string CtorAssignments(IEnumerable<OptionModel> options)
//                => string.Join("\n        ", options.Select(o => $"{o.Name.AsProperty()} = {o.Name.AsField()};"));

//            string OptionFields(IEnumerable<OptionModel> options)
//                => string.Join("\n        ", options.Select(o => $"Option<{o.Type}> {o.Name.AsField()}Option = new Option<{o.Type}>({OptionAlias(o)}, {o.Description.InQuotes()});"));

//            string OptionAlias(OptionModel option)
//                => $@"""--{option.Name.AsAlias()}""";

//            string OptionAssign(IEnumerable<OptionModel> options)
//                => string.Join("\n            ", options.Select(o => $"SystemCommandLineCommand.AddOption({o.Name.AsField()}Option);"));

//            string CommandParams(IEnumerable<OptionModel> options)
//                => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.Name.AsField()}Option, commandResult)"));
//        }

//        public static string GenerateRootCommandCode(IEnumerable<CommandModel> modelData, CancellationToken cancellationToken)
//        {
//            if (modelData is null)
//            { return ""; }

//            return $@"
//using System.CommandLine.Invocation;
//using IncrementalGeneratorSamples.Runtime;

//#nullable enable

//namespace TestExample;

//public partial class RootCommand
//{{
//    public static void Invoke(string[] args)
//        => CommandHandler.Invoke(args);

//    internal class CommandHandler : RootCommandHandler<CommandHandler>
//    {{
//        public CommandHandler() : base(string.Empty)
//        {{
//            {CtorAssignments(modelData)}
//        }}

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override int Invoke(InvocationContext invocationContext)
//        {{
//            Console.WriteLine(""Enter one of the commands. Use '-h' to get a list of available commands"");
//            return 1;
//        }}

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override Task<int> InvokeAsync(InvocationContext invocationContext)
//        {{
//            // Since this method is not implemented in the user source, we do not implement it here.
//            throw new NotImplementedException();
//        }}
//    }}
//}}
//";

//            string CtorAssignments(IEnumerable<CommandModel> options)
//                => string.Join("\n            ", options.Select(c => $"SystemCommandLineCommand.Add({c.Name}.CommandHandler.GetHandler().SystemCommandLineCommand);"));
//        }


//    }
//}