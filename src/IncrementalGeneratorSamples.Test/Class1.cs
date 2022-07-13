
//using System.CommandLine;
//using System.CommandLine.Invocation;
//using IncrementalGeneratorSamples.Runtime;

//#nullable enable

//namespace TestExample;

//public partial class ReadFile
//{
//    internal class CommandHandler : CommandHandler<CommandHandler>
//    {
//        Option<int> delayOption = new Option<int>("--delay", "");

//        public CommandHandler()
//            : base("read-file", "")
//        {
//            SystemCommandLineCommand.AddOption(delayOption);
//        }

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override int Invoke(InvocationContext invocationContext)
//        {
//            var commandResult = invocationContext.ParseResult.CommandResult;
//            var command = new ReadFile(GetValueForSymbol(delayOption, commandResult));
//            return command.DoWork();
//        }

//        /// <summary>
//        /// The handler invoked by System.CommandLine. This will not be public when generated is more sophisticated.
//        /// </summary>
//        /// <param name="invocationContext">The System.CommandLine Invocation context used to retrieve values.</param>
//        public override Task<int> InvokeAsync(InvocationContext invocationContext)
//        {
//            // Since this method is not implemented in the user source, we do not implement it here.
//            throw new NotImplementedException();
//        }
//    }
//}
