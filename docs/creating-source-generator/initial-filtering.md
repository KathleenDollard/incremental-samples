# Initial filtering

Generators may run on every keystroke, so they should be very, very fast. Because an individual generator works with only a small amount of information, the first step of an efficient generator is to filter out what your generator does not need. Identifying the code of interest with an attributes facilitate fast generators because attributes are indexed.

Since the use of attributes to identify source code used for generation is strongly recommended, it will be used in this example. The `SyntaxProvider.CreateSyntaxProvider` method uses the predicate and transform in the same way, but for that case it is essential that the predicate be extremely fast to exclude unneeded syntax nodes. 

> [!WARNING]
> In the near future, generators that slow down Visual Studio will run only on the full build. If your generator changes code, such as altering a type or method declaration, this will probably result in a poor user experience.

Other [available providers are discussed in the article on pipelines](../pipeline.md).

## ForAttributeWithMetadataName

The basic syntax is a method on the `SyntaxProvider` property of the `IncrementalGeneratorContext`:

```csharp
public IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
    string fullyQualifiedMetadataName,
    Func<SyntaxNode, CancellationToken, bool> predicate,
    Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
```

This method finds the syntax nodes that are decorated with the attribute. This comparison is by the full metadata name. An example of a full metadata name is:  `<full name of system.ComponentModel.description>`. 

It passes the decorated syntax nodes to the predicate, and passes each node that passes the predicate to the transform. 

## Predicate

The predicate allows further filtering of nodes that are decorated with the specified attribute. For example, the predicate is useful if your generated only uses syntax that has the attribute and also is of a particular name, an additional attribute, an aspect of a descendant node, or a particular argument to an attribute. The predicate only has access to the `SyntaxNode`, so additional filtering may be done in the transform.

When using `ForAttributeWithMetadataName` the predicate may not be needed because the attribute filtering is sufficient. In this case, simply return true.

When using `CreateSyntaxProvider` the predicate should never just return true. It will be passed every syntax node in the project - and that might be millions of syntax nodes. This is why this approach cannot be made performant in large projects. If you are using this approach, the first step should always be to filter syntax nodes on [[REVIEW: pattern match to type or use kind, or pattern match if you have work you need a typed value for, and otherwise kind??]]

If the predicate is non-trivial, use a method you access via a method group to keep your generator readable.

## Transform

The goal of the transform is to return a domain model which is an instance of a type that contains all of the information you need to retrieve from the syntax node or the semantic model. 

In all cases, the transform should return an instance of a type with value equality that is not related to the syntax node, semantic model, `IOperation`, `ISymbol`, compilation or similar artifacts of the generator. The reason this is necessary is that the incremental generator caches this result and will skip steps in the generation pipeline that have no changes in their input.

The transform receives a context which contains a `GeneratorSyntaxContext` which contains the `SyntaxNode` and the corresponding semantic model of the compilation. Examples of you you can call `GetDeclaredSymbol` passing a `ClassDeclarationSyntax` to retrieve the `ITypeSymbol` and use it to retrieve the class members. Writing predicates and transforms will require an understanding of Roslyn [[ Bill: Can you supply a link?]]

Sometimes the additional information determines that you do not need the node for genera  case you should return a marker value such as null that can be filtered later.

[[ Review, is this slow and is anything else slow? ]]
You should not do expensive operations against the semantic model such as finding all references [[ find the actual name]]

When required multiple syntax providers may return different domain models which can be combined later in the pipeline.

## Example

This example builds on the design of the [Designing input article](design-input-data.md#example). You can see how this code is used in [Putting it all together](putting-it-all-together.md#example).

This extracts data in the language of source code - it has a `ClassModel` and a `PropertyModel`. A later `Select` will transform this into the language of the domain - `CommandModel` and `OptionModel`. This decision was made to delay work that was unnecessary for the value equality comparison with the cached value. In particular, it extracts the description from the XML documentation. Retrieving the XML documentation from `ISymbol` is not slow. Loading it as using `XDocument.Parse` to extract the description is slow. This step takes more time than all of the simple extraction from source code, and has significantly more allocations. You'll see both of these methods as you walk through the example:

|                 Method |     Mean |     Error |    StdDev |  Gen 0 |  Gen 1 | Allocated |
|----------------------- |---------:|----------:|----------:|-------:|-------:|----------:|
|   GetInitialClassModel | 2.889 us | 0.0577 us | 0.1468 us | 0.3967 |      - |      2 KB |
| DescriptionFromXmlDocs | 3.033 us | 0.1393 us | 0.4107 us | 2.7161 | 0.0076 |     11 KB |

It is a bit unintuitive that more steps makes an incremental generator faster, but it is often true. Doing as little work as possible in the initial data extraction is one step in writing a well performing generator.

Isolating data extraction from further transformations and from outputting code also makes generators dramatically easier to test and debug. It also helps you see quickly what you are extracting from the source code. 

Building on the example from [Design input data](design-input-data.md#example), the generator needs to request nodes attributed with the `CommandAttribute` and transform that into a domain model that has value equality. First, the design for a common model for symbols:

```csharp
public class InitialSymbolModel 
{
    public InitialSymbolModel(string name,
                                string xmlComments,
                                IEnumerable<AttributeValue> attributes)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        XmlComments = xmlComments;
        Attributes = attributes;
    }
    public string Name { get;  }
    public string XmlComments { get;  }
    public IEnumerable<AttributeValue> Attributes { get;  }

    public override bool Equals(object obj)
        => Equals(obj as InitialSymbolModel);

    public bool Equals(InitialSymbolModel other) 
        => !(other is null) &&
                Name == other.Name &&
                XmlComments == other.XmlComments &&
                // Is this default a value equality
                EqualityComparer<IEnumerable<AttributeValue>>.Default.Equals(Attributes, other.Attributes);

    public override int GetHashCode()
    {
        int hashCode = 1546976210;
        hashCode = hashCode * -1521134295 + (Name, XmlComments).GetHashCode();
        // Does this default correspond to a value equality
        hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<AttributeValue>>.Default.GetHashCode(Attributes);
        return hashCode;
    }
}
```

Note that the equality and hash codes are manually written, so this is a lot of boilerplate code for a class with three properties. Because analyzers must target `netstandard2.0` the supported C# language version is 7.3, which does not contain records.

The derived class for the initial class model adds a namespace and properties:

```csharp
public class InitialClassModel : InitialSymbolModel, IEquatable<InitialClassModel>
{
    public InitialClassModel(string name,
                                string xmlComments,
                                IEnumerable<AttributeValue> attributes,
                                string nspace,
                                IEnumerable<InitialPropertyModel> properties)
        : base(name, xmlComments, attributes)
    {
        Namespace = nspace;
        Properties = properties;
    }
    public string Namespace { get; set; }

    public IEnumerable<InitialPropertyModel> Properties { get; set; }

    public override bool Equals(object obj)
        => Equals(obj as InitialClassModel);

    public bool Equals(InitialClassModel other)
        => !(other is null) &&
            base.Equals(other) &&
            Namespace == other.Namespace &&
            EqualityComparer<IEnumerable<InitialPropertyModel>>.Default.Equals(Properties, other.Properties);

    public override int GetHashCode()
    {
        int hashCode = 1383515346;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namespace);
        hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<InitialPropertyModel>>.Default.GetHashCode(Properties);
        return hashCode;
    }
}
```

This generator does not care about methods. Classes like these will be similar for many generators, but trying to unify them would result in extra work being done to support values that weren't used, or missing values that were needed. For example, most generators will not need XML comments.

The derived class for the initial property model:

```c#
public class InitialPropertyModel : InitialSymbolModel, IEquatable<InitialPropertyModel>
{
    public InitialPropertyModel(string name,
                                string xmlComments,
                                string type,
                                IEnumerable<AttributeValue> attributes)
        : base(name, xmlComments, attributes)
    {
        Type = type;
    }

    public string Type { get; set; }

    public override bool Equals(object obj)
        => Equals(obj as InitialPropertyModel);

    public bool Equals(InitialPropertyModel other)
        => !(other is null) &&
            base.Equals(other) &&
            Type == other.Type;

    public override int GetHashCode()
    {
        int hashCode = 1522494069;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
        return hashCode;
    }
}
```

Filling these classes from the type symbol that represents the class is:

```csharp
    public static InitialClassModel GetInitialModel(
                                    ISymbol symbol,
                                    CancellationToken cancellationToken)
    {
        if (!(symbol is ITypeSymbol typeSymbol))
        { return null; }

        var properties = new List<InitialPropertyModel>();
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // since we do not know how big this list is, check cancellation token
            cancellationToken.ThrowIfCancellationRequested();
            properties.Add(new InitialPropertyModel(property.Name,
                                                    property.GetDocumentationCommentXml(),
                                                    property.Type.ToString(),
                                                    property.AttributeNamesAndValues()));
        }
        return new InitialClassModel(typeSymbol.Name,
                                        typeSymbol.GetDocumentationCommentXml(),
                                        typeSymbol.AttributeNamesAndValues(),
                                        typeSymbol.ContainingNamespace.Name,
                                        properties);
    }
```

The `GetInitialModel` will be called from a helper method, which is called as the transform delegate passed to `SyntaxProvider.ForAttributeWithMetadataName`. The helper method avoids a direct dependency between `GetIntitialModel` and the `GeneratorAttributeSyntaxContext` passed to the transform delegate, which makes testing easier. Only the symbol and the cancellation token are needed by this method.

The initial class and property models hold the XML comment text exactly as it appears in the semantic model. Because it is comparatively slow, the generator skips parsing the XML when nothing in the initial model is changed.

The values we need are straightforward to retrieve, except the attributes, which are gathered in the `AttributeNamesAndValues` extension method. Passing the attributes themselves would break value equality, so this method creates a list of items of a custom attribute type. This type also needs to implement value equality:

```csharp
public class AttributeValue : IEquatable<AttributeValue>
{

    public AttributeValue(string attributeName,
                            string valueName,
                            string valueType,
                            object value)
    {
        AttributeName = attributeName;
        ValueName = valueName;
        ValueType = valueType;
        this.Value = value;
    }

    public string AttributeName { get;  }
    public string ValueName { get;  }
    public string ValueType { get;  }
    public object Value { get;  }

    public override bool Equals(object obj) 
        => Equals(obj as AttributeValue);

    public bool Equals(AttributeValue other)
        => !(other is null) &&
                AttributeName == other.AttributeName &&
                ValueName == other.ValueName &&
                ValueType == other.ValueType &&
                EqualityComparer<object>.Default.Equals(Value, other.Value);

    public override int GetHashCode()
    {
        int hashCode = -960430703;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AttributeName);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ValueName);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ValueType);
        hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Value);
        return hashCode;
    }
}
```

`Name` is the name of the attribute class. `ValueName` and `ValueType` are the name and type of the property or the constructor parameter. The `AttributeNamesAndValues` method considers both named attribute properties and constructor parameters, and assumes that all value may be interesting to later steps:

```csharp
    public static IEnumerable<AttributeValue> AttributeNamesAndValues(this ISymbol symbol)
    {
        var attributes = symbol.GetAttributes();
        var list = new List<AttributeValue>();
        foreach (var attribute in attributes)
        {
            var attributeName = attribute.AttributeClass.Name.ToString();
            foreach (var pair in attribute.NamedArguments)
            {
                var value = ValuesFromTypedConstant(pair.Value);
                list.Add(new AttributeValue(attributeName, pair.Key, pair.Value.Type.ToString(), value));

            }
            if (!(attribute.AttributeConstructor is null))
            {
                var parameters = attribute.AttributeConstructor.Parameters;
                var args = attribute.ConstructorArguments;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters.Length >= i && args.Length >= i)
                    {
                        var value = ValuesFromTypedConstant(args[i]);
                        list.Add(new AttributeValue(attributeName, parameters[i].Name, parameters[i].Type.ToString(), value));
                    }
                }
            }
        }
        return list;
    }
```

The values passed to attributes must be constants, arrays or types. This is represented in the semantic model as a `TypedConstant`, but again, if we include a `TypedConstant` in the model returned by the transform method, it will break value equality, therefore break caching, and result in full generation on every design time compilation. Instead we evaluate the typed constant and put only return the values. This is tricky because it is legal to have nested arrays if the type is an `array` of `object`. This code solves this problem with recursion, and a depth check for runaway recursion as it is difficult to find in tests:

```csharp
    private static object ValuesFromTypedConstant(TypedConstant val, int recursionDepth = 1)
    {
        recursionDepth += 1;
        if (recursionDepth > 10)
        { throw new InvalidOperationException("Runaway recursion suspected"); }
        return val.Kind == TypedConstantKind.Array
                                        ? ValuesFromTypedConstantArray(val.Values, recursionDepth)
                                        : val.Value;

        object ValuesFromTypedConstantArray(IEnumerable<TypedConstant> typedConstants, int innerRecursionDepth)
        {
            var list = new List<object>();
            foreach (var typedConstant in typedConstants)
            {
                list.Add(ValuesFromTypedConstant(typedConstant, innerRecursionDepth + 1));
            }
            return list.ToArray();
        }
    }
```

## Testing the example

Testing every step of the generator results in a system that is easier to debug and understand, as well as making the development process much smoother. With the exception of extracting the attributes, this code is relatively straightforward. Setting up a test harness for this step requires a bit of supporting code because you need to create a semantic model. Later steps can be tested in isolation by manually creating the input types, and finally an end to end integration test allows you to test your generator wiring.

Generation requires a lot of detail and you will frequently mess up more than one thing. To manage this, use a verification tool such as [Verify]() or [ApprovalsTest](). These present a diff between the expected output and the current output. You can see exactly what differs in your diff tools. When verifying instances of types such as our output, Verify serializes the data with Newtonsoft.Json.

If the current output is correct, you simply copy it to the verified file - usually by copying the left pane to the right in your diff tool. While far more efficient than finding the problems in strings compared with `Assert.Equal`, it still becomes tedious with a large number of tests. This example tracks five test conditions through to outputting code, and has four additional scenarios to test the supported variations in managing attributes.

> [!IMPORTANT]
> If you receive an error that Newtonsoft.Json cannot serialize a type, resolve this by removing the type or altering the type to make it serializable. For example, if you try to serialize a `TypedConstant`, you will get an error. Returning a `TypedConstant` or any of almost any type of the semantic model or syntax tree is an mi9stake and will break value equality and caching.

These tests use XUnit's `Theory` feature because each scenario goes through exactly the same test:

```csharp
[UsesVerify]
public class InitialModelTests
{
    [Theory]
    [InlineData("SimplestPractical", typeof(SimplestPractical))]
    [InlineData("WithOneProperty", typeof(WithOneProperty))]
    [InlineData("WithMultipleProperties", typeof(WithMultipleProperties))]
    [InlineData("WithXmlDescriptions", typeof(WithXmlDescriptions))]
    [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]
    [InlineData("WithAttributeNamedValue", typeof(WithAttributeNamedValues))]
    [InlineData("WithAttributeConstructorValues", typeof(WithAttributeConstructorValues))]
    [InlineData("WithAttributeNestedNamedValues", typeof(WithAttributeNestedNamedValues))]
    [InlineData("WithAttributeNestedConstructorValues", typeof(WithAttributeNestedConstructorValues))]
    public Task Initial_class_model(string fileNamePart, Type inputDataType)
    {
        var inputSource = Activator.CreateInstance(inputDataType) is TestData testData
            ? testData.InputSourceCode
            : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));
        var (_, symbol, _, cancellationToken, inputDiagnostics) = TestHelpers.GetTransformInfoForClass(inputSource, x => x.Identifier.ToString() == "MyClass");
        Assert.Empty(TestHelpers.ErrorAndWarnings(inputDiagnostics));
        Assert.NotNull(symbol);

        var classModel = ModelBuilder.GetInitialModel(symbol, cancellationToken);

        return Verifier.Verify(classModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
    }
}
```

Theory tests have parameters. Here, this is an identifier used in file naming, and a type. while a string constant would work for the source code, tests in the next section will create instances of types for each scenario that are used in later testing.

Verify expects the test to return a Task.

The first step of the test is to retrieve the input source code. This is passed to a rather helper method to retrieve the symbol. The syntax node and semantic model are ignored. 

The *Act* portion of the test is the call to the `ModelBuilder.GetInitialModel` method that was discussed earlier in this article.

A subdirectory is specified for the test output so that these text files are not mixed up with the C# classes of the test project and specifying `UseTextForParameters` ensures a readable name for each test.

`TestData` is a base class which each scenario derives from. This type is:

```csharp
    public class TestData
    {
        protected TestData(string inputSourceCode)
        {
            InputSourceCode = inputSourceCode;
        }

        public string InputSourceCode { get; }
        // Additional stuff for testing other pipeline steps
     }
```

An example of a scenario class that only provides the initial source code:

```csharp
public class SimplestPractical : TestData
{
    public SimplestPractical()
        : base(inputSourceCode: @"
namespace MyNamespace
{
public class MyClass{}
}")
    { }
}
```

Testing later steps of the generator will use the techniques discussed above. Creating the `ITypeSymbol` is more complicated;

```csharp
    public static (SyntaxNode? syntaxNode, ISymbol? symbol, SemanticModel? semanticModel, CancellationToken cancellationToken, IEnumerable<Diagnostic> inputDiagnostics)
        GetTransformInfoForClass(string sourceCode, Func<ClassDeclarationSyntax, bool>? filter = null, bool continueOnInputErrors = false)
    {
        // create a dummy cancellation token. These tests do not test cancellation
        var cancellationToken = new CancellationTokenSource().Token;

        // Get the compilation and check its state
        var compilation = GetInputCompilation<Generator>(
                OutputKind.DynamicallyLinkedLibrary, sourceCode);
        var inputDiagnostics = compilation.GetDiagnostics();
        if (!continueOnInputErrors && TestHelpers.ErrorAndWarnings(inputDiagnostics).Any())
        { return (null, null, null, cancellationToken, inputDiagnostics); }

        // Get the syntax tree and filter to expected node
        var tree = compilation.SyntaxTrees.Single(); // tests are expected to have just one
        var matchQuery = tree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();
        if (filter is not null)
        { matchQuery = matchQuery.Where(x => filter(x)); }
        var matches = matchQuery.ToList();
        Assert.Single(matches);
        var syntaxNode = matches.Single();

        // Return, null values are only returned on failure
        var semanticModel = compilation.GetSemanticModel(tree);
        return (syntaxNode, semanticModel.GetDeclaredSymbol(syntaxNode), semanticModel, cancellationToken, inputDiagnostics);
    }
```

This method returns a tuple of the SyntaxNode, the symbol, the semantic model, the cancellation token, and the any diagnostics received as part of creating the compilation. 

This helper method reduces the boilerplate in tests to include a common set of using statements in the compilation. Creating the compilation requires knowing the correct assemblies to include. Here this is done by adopting the assemblies of the test project:

```csharp
    public static Compilation GetInputCompilation<TGenerator>(OutputKind outputKind, params string[] code)
    {
        // Create the initial syntax tree, add using statements, and get the updated tree
        var syntaxTrees = code.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray();
        var newUsings = new UsingDirectiveSyntax[] {
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.IO")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Collections.Generic")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System.Linq")),
        SyntaxFactory.UsingDirective(SyntaxFactory .ParseName("System")) };
        var updatedSyntaxTrees = syntaxTrees
            .Select(x => x.GetCompilationUnitRoot().AddUsings(newUsings).SyntaxTree);

        // REVIEW: Is there a better way to get the references
        // Add assemblies from the current (test) project
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var references = assemblies
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(TGenerator).Assembly.Location),
            });

        // Create the compilation options and return the new compilation
        var compilationOptions = new CSharpCompilationOptions(
            outputKind,
            nullableContextOptions: NullableContextOptions.Enable);
        return CSharpCompilation.Create("compilation",
                                        updatedSyntaxTrees,
                                        references,
                                        compilationOptions);
    }
``````

With code like this, creating a new scenario just involves creating a new scenario type and adding it to the test theory.