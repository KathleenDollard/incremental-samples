using IncrementalGeneratorSamples.InternalModels;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;

namespace IncrementalGeneratorSamples
{

    public class CodeOutput
    {
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

        public static string PartialCli(IEnumerable<CommandModel> commandModels, CancellationToken cancellationToken)
        {
            if (commandModels is null || !commandModels.Any())
            { return ""; }

            return $@"
namespace IncrementalGeneratorSamples
{{
    internal partial class Cli
    {{
        static partial void SetRootCommand()
        {{
            var rootHandler = {commandModels.First().Namespace}.RootCommand.CommandHandler.Instance;
            rootCommand = rootHandler.RootCommand;
        }}
    }}
}}
";
        }

        public static string GenerateRootCommandCode(IEnumerable<CommandModel> commandModels, CancellationToken cancellationToken)
        {
            if (commandModels is null || !commandModels.Any())
            { return ""; }

            return $@"
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace {commandModels.First().Namespace};

public class RootCommand
{{
    internal class CommandHandler : RootCommandHandler<CommandHandler>
    {{
        // Manage singleton instance
        public static RootCommand.CommandHandler Instance = new RootCommand.CommandHandler();

        public CommandHandler() : base(string.Empty)
        {{
            {CtorAssignments(commandModels)}
        }}
    }}
}}
";
            string CtorAssignments(IEnumerable<CommandModel> options)
                => string.Join("\n            ", options.Select(c => $"Command.Add({c.SymbolName}.CommandHandler.Instance.Command);"));
        }


        //public static string FileName(CommandModel modelData)
        //    => $"{modelData?.Name}.g.cs";

        public static string GenerateCommandCode(CommandModel commandModel, CancellationToken cancellationToken)
        {
            if (commandModel is null)
            { return ""; }

            return $@"
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace {commandModel.Namespace};

public partial class {commandModel.SymbolName}
{{
    internal class CommandHandler : CommandHandlerBase
    {{
        // Manage singleton instance
        public static {commandModel.SymbolName}.CommandHandler Instance {{ get; }} = new {commandModel.SymbolName}.CommandHandler();

        // Create System.CommandLine options
        {OptionFields(commandModel.Options)}

        // Base constructor creates System.CommandLine and optins are added here
        private CommandHandler()
            : base({commandModel.Name.InQuotes()}, {commandModel.Description.InQuotes()})
        {{
            {CommandAliases(commandModel)}
            {OptionAssign(commandModel.Options)}
        }}

        // The code invoked when the user runs the command
        protected override int Invoke(InvocationContext invocationContext)
        {{
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new {commandModel.SymbolName}({CommandParams(commandModel.Options)});
            return command.Execute();
        }}
    }}
}}
";

            string OptionFields(IEnumerable<OptionModel> options)
                => string.Join("\n        ", options.Select(o =>
                    $"private Option<{o.Type}> {o.LocalSymbolName}Option = new Option<{o.Type}>({OptionAlias(o)}, {o.Description.InQuotes()});"));

            string OptionAlias(OptionModel option)
            {
                var aliases = new List<string>() { option.Name.InQuotes() };
                aliases.AddRange(option.Aliases);
                return string.Join(", ", aliases);
            }

            object CommandAliases(CommandModel model)
            {
                if (model.Aliases is null || !model.Aliases.Any())
                    { return ""; }
                return string.Join("\n            ", model.Aliases.Select(a => $"Command.AddAlias({a});"));
            }

            string OptionAssign(IEnumerable<OptionModel> options)
                => string.Join("\n            ", options.Select(o => $"Command.AddOption({o.LocalSymbolName}Option);"));

            string CommandParams(IEnumerable<OptionModel> options)
                => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.LocalSymbolName}Option, commandResult)"));

        }


    }
}