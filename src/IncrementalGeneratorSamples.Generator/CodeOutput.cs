using IncrementalGeneratorSamples.Models;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Data;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace IncrementalGeneratorSamples;


public class CodeOutput
{
    public static string FileName(GenerationModel? modelData)
        => $"{modelData?.CommandName}.g.cs";

    public static string GeneratedCode(GenerationModel? modelData)
    {
        if (modelData is null)
        { return ""; }

        return $@"
using System.CommandLine;
using System.CommandLine.Invocation;

#nullable enable

namespace TestExample;

public partial class {modelData.CommandName}
{{
    public {modelData.CommandName}({Parameters(modelData.Options)})
    {{
        {CtorAssignments(modelData.Options)}
    }}

    public static void Invoke(string[] args)
        => CommandHandler.Invoke(args);

    internal class CommandHandler : IncrementalGeneratorSamples.Runtime.CommandHandler<CommandHandler>
    {{
        {OptionFields(modelData.Options)}

        public CommandHandler()
        {{
            {OptionCreate(modelData.Options)}
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
            => string.Join("\n        ", options.Select(o => $"Option<{o.Type}> {o.Name.AsField()}Option;"));

        static string OptionCreate(IEnumerable<OptionModel> options)
            => string.Join("\n            ", options.Select(o => $"{o.Name.AsField()}Option = new Option<{o.Type}>({o.Name.AsAlias().InQuotes()}, {o.Description.InQuotes()});"));

        static string OptionAssign(IEnumerable<OptionModel> options)
            => string.Join("\n            ", options.Select(o => $"RootCommand.AddOption({o.Name.AsField()}Option);"));

        static string CommandParams(IEnumerable<OptionModel> options)
            => string.Join(", ", options.Select(o => $"GetValueForSymbol({o.Name.AsField()}Option, commandResult)"));
    }

}
