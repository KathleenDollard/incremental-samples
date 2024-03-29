//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;

//namespace IncrementalGeneratorSamples.Test
//{
//    [UsesVerify]
//    public class ModelBuildingTests
//    {
//        private const string SimpleClass = @"
//using IncrementalGeneratorSamples.Runtime;

//[Command]
//public partial class ReadFile
//{
//    public int Delay { get;  }
//}
//";

//        private const string ClassWithXmlComments = @"
//using IncrementalGeneratorSamples.Runtime;

///// <summary>
///// The file to output to the console.
///// </summary>
//[CommandAttribute]
//public partial class ReadFile
//{
//    /// <summary>
//    /// Delay between lines, specified as milliseconds per character in a line.
//    /// </summary>
//    public int Delay { get;  }
//}
//";

//        private const string CompleteClass = @"
//using IncrementalGeneratorSamples.Runtime;

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

//        [Theory]
//        [InlineData(1, SimpleClass)]
//        [InlineData(1, ClassWithXmlComments)]
//        [InlineData(1, CompleteClass)]
//        public void Should_select_attributed_syntax_nodes(int expectedCount, string sourceCode)
//        {
//            var cancellationToken = new CancellationTokenSource().Token;
//            var tree = CSharpSyntaxTree.ParseText(sourceCode);
//            var matches = tree.GetRoot()
//                .DescendantNodes()
//                .Where(node => TestHelpers.IsSyntaxInteresting(node, cancellationToken));
//            Assert.Equal(expectedCount, matches.Count());
//        }



//        [Fact]
//        public void Should_build_model_from_SimpleClass()
//        {
//            var model = TestHelpers.GetModelForTesting(SimpleClass);
//            Assert.NotNull(model);
//            // REVIEW: Is there a better way to quiet the warning left after the NotNull assertion?
//            if (model is null) return; // to appease NRT
//            Assert.Equal("ReadFile", model.Name);
//            Assert.Single(model.Options);
//            Assert.Equal("Delay", model.Options.First().Name);
//            Assert.Equal("int", model.Options.First().Type);
//        }

//        [Fact]
//        public void Should_build_model_from_CompleteClass()
//        {
//            var model = TestHelpers.GetModelForTesting(CompleteClass);
//            Assert.NotNull(model);
//            if (model is null) return; // to appease 
//            Assert.Equal("CompleteCommand", model.Name);
//            Assert.Equal(2, model.Options.Count());
//            Assert.Equal("File", model.Options.First().Name);
//            Assert.Equal("System.IO.FileInfo?", model.Options.First().Type);
//            Assert.Equal("Delay", model.Options.Skip(1).First().Name);
//            Assert.Equal("int", model.Options.Skip(1).First().Type);
//        }

//        [Fact]
//        public void Should_include_Xml_description_in_model()
//        {
//            var model = TestHelpers.GetModelForTesting(ClassWithXmlComments);
//            Assert.NotNull(model);
//            if (model is null) return; // to appease 
//            Assert.Equal("ReadFile", model.Name);
//            Assert.Equal("The file to output to the console.", model.Description);
//            Assert.Single(model.Options);
//            Assert.Equal("Delay between lines, specified as milliseconds per character in a line.", model.Options.First().Description);
//        }



//        //[Theory]
//        //[InlineData("SimpleClass", SimpleClass)]
//        //[InlineData("ClassWithXmlComments", ClassWithXmlComment)]
//        //[InlineData("CompleteClass", CompleteClass)]
//        //public Task Should_build_correct_model(string fileName, string sourceCode)
//        //{
//        //    var (inputCompilation, inputDiagnostics ) = TestHelpers.GetInputCompilation<ModelTestGenerator>(OutputKind.DynamicallyLinkedLibrary, sourceCode);
//        //    // TODO: You may need  to exclude specific diagnostics here, such as CS0103 is you use symbols that are created during generation. 
//        //    Assert.Empty(inputDiagnostics);

//        //    var (output, outputDiagnostics) = TestHelpers.Generate<ModelTestGenerator>(inputCompilation);
//        //    Assert.Empty(outputDiagnostics);

//        //    return Verifier.Verify(output).UseDirectory("ModelSnapshots").UseTextForParameters(fileName);
//        //}
//    }
//}