using IncrementalGeneratorSamples.Models;

namespace IncrementalGeneratorSamples;


public class CodeOutput
{
    public static string FileName(CommandModel? modelData)
        => $"{modelData?.CommandName}.g.cs";

    public static string GenerateCommandCode(CommandModel? modelData)
    {
        if (modelData is null)
        { return ""; }

        return $@"
using System.CommandLine;
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class {modelData.CommandName}
{{
    public {modelData.CommandName}({Parameters(modelData.Options)})
    {{
        {CtorAssignments(modelData.Options)}
    }}

    internal class CommandHandler : CommandHandler<CommandHandler>
    {{
        {OptionFields(modelData.Options)}

        public CommandHandler()
            : base(""--{modelData.CommandName.KebabCase()}"", ""{modelData.Description}"")
        {{
            {OptionAssign(modelData.Options)}
        }}

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {{
            var commandResult = invocationContext.ParseResult.CommandResult;
            var command = new Command({CommandParams(modelData.Options)});
            return command.DoWork();
        }}

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {{
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }}
    }}
}}
";
        static string Parameters(IEnumerable<OptionModel> options)
            => string.Join(", ", options.Select(o => $"{o.Type} {o.Name.AsField()}"));

        static string CtorAssignments(IEnumerable<OptionModel> options)
            => string.Join("\n        ", options.Select(o => $"{o.Name.AsProperty()} = {o.Name.AsField()};"));

        static string OptionFields(IEnumerable<OptionModel> options)
            => string.Join("\n        ", options.Select(o => $"Option<{o.Type}> {o.Name.AsField()}Option = new Option<{o.Type}>({o.Name.AsAlias().InQuotes()}, {o.Description.InQuotes()});"));

        static string OptionAssign(IEnumerable<OptionModel> options)
            => string.Join("\n            ", options.Select(o => $"SystemCommandLineCommand.AddOption({o.Name.AsField()}Option);"));

        static string CommandParams(IEnumerable<OptionModel> options)
            => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.Name.AsField()}Option, commandResult)"));
    }

    public static string GenerateRootCommandCode(IEnumerable<CommandModel> modelData)
    {
        if (modelData is null)
        { return ""; }

        return $@"
using System.CommandLine.Invocation;
using IncrementalGeneratorSamples.Runtime;

#nullable enable

namespace TestExample;

public partial class RootCommand
{{
    public static void Invoke(string[] args)
        => CommandHandler.Invoke(args);

    internal class CommandHandler : CommandHandler<CommandHandler>
    {{
        public CommandHandler() : base(string.Empty)
        {{
            {CtorAssignments(modelData)}
        }}

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override int Invoke(InvocationContext invocationContext)
        {{
            Console.WriteLine(""Enter one of the commands. Use '-h' to get a list of available commands"");
            return 1;
        }}

        /// <summary>
        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
        /// </summary>
        /// <param name=""invocationContext"">The System.CommandLine Invocation context used to retrieve values.</param>
        public override Task<int> InvokeAsync(InvocationContext invocationContext)
        {{
            // Since this method is not implemented in the user source, we do not implement it here.
            throw new NotImplementedException();
        }}
    }}
}}
";

        static string CtorAssignments(IEnumerable<CommandModel> options)
            => string.Join("\n            ", options.Select(c => $"SystemCommandLineCommand.Add({c.CommandName}.CommandHandler.GetHandler().SystemCommandLineCommand);"));
    }


}
