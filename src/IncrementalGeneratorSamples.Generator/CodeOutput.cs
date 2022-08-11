using IncrementalGeneratorSamples.InternalModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IncrementalGeneratorSamples
{

    public class CodeOutput
    {
        public static string FileName(CommandModel modelData)
            => $"{modelData?.Name.AsSymbol()}.g.cs";

        public const string ConsistentCli = @"
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
                Console.WriteLine(""No classes were marked with the [Command] attribute"");
                return 1;
            }
            return rootCommand.Invoke(args);
        }

        static partial void SetRootCommand();
    }
}
";

        public static string PartialCli(RootCommandModel rootModel, CancellationToken _)
        {
            return rootModel is null
                        ? ""
                        : $@"
using IncrementalGeneratorSamples.Runtime;
using System.CommandLine;

#nullable enable

namespace IncrementalGeneratorSamples
{{
    internal partial class Cli
    {{
        static partial void SetRootCommand()
        {{
            var rootHandler = CommandHandler.Instance;
            rootCommand = rootHandler.RootCommand;
        }}

        internal class CommandHandler : RootCommandHandler<CommandHandler>
        {{
            // Manage singleton instance
            public static CommandHandler Instance = new CommandHandler();

            public CommandHandler() : base(string.Empty)
            {{
                {CtorAssignments(rootModel.Namespace, rootModel.CommandSymbolNames)}
            }}
        }}
    }}
}}

";
            string CtorAssignments(string nspace, IEnumerable<string> commandNames)
                => string.Join("\n            ", commandNames.Select(c => $"Command.Add({nspace}.{c}.CommandHandler.Instance.Command);"));

        }

        public static string CommandCode(CommandModel commandModel, CancellationToken cancellationToken)
        {
            return commandModel is null
                ? ""
                : $@"
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace {commandModel.Namespace};

public partial class {commandModel.Name.AsSymbol()}
{{
    internal class CommandHandler : CommandHandlerBase
    {{
        // Manage singleton instance
        public static {commandModel.Name.AsSymbol()}.CommandHandler Instance {{ get; }} = new {commandModel.Name.AsSymbol()}.CommandHandler();

        // Create System.CommandLine options
        {OptionFields(commandModel.Options)}

        // Base constructor creates System.CommandLine and options are added here
        private CommandHandler()
            : base({commandModel.DisplayName.InQuotes()}, {commandModel.Description.InQuotes()})
        {{
            {CommandAliases(commandModel)}
            {OptionAssignments(commandModel.Options)}
        }}

        // The code invoked when the user runs the command
        protected override int Invoke(InvocationContext invocationContext)
        {{
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new {commandModel.Name.AsSymbol()}({CommandParams(commandModel.Options)});
            return command.Execute();
        }}
    }}
}}
";
            string CommandAliases(CommandModel model) 
                => model.Aliases is null || !model.Aliases.Any()
                    ? ""
                    : string.Join("\n            ", model.Aliases.Select(a => $"Command.AddAlias({a});"));

            string OptionFields(IEnumerable<OptionModel> options)
                => string.Join("\n        ", options.Select(o =>
                    $"private Option<{o.Type}> {o.Name.AsLocalSymbol()}Option = new Option<{o.Type}>({OptionAliases(o)}, {o.Description.InQuotes()});"));

            string OptionAliases(OptionModel option)
            {
                var aliases = new List<string>() { option.DisplayName.InQuotes() };
                aliases.AddRange(option.Aliases);
                return string.Join(", ", aliases);
            }

            string OptionAssignments(IEnumerable<OptionModel> options)
                => string.Join("\n            ", options.Select(o => $"Command.AddOption({o.Name.AsLocalSymbol()}Option);"));

            string CommandParams(IEnumerable<OptionModel> options)
                => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.Name.AsLocalSymbol()}Option, commandResult)"));

        }


    }
}