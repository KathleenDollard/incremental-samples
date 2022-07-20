# Further transformations

Once you have your initial model, you may need to transform it prior to outputting code. Several things may lead to these transformations:

* The shape of the initial model does not align with code outputting. The initial extraction step results is one item per syntax node. The output step needs one item per file.
* There is expensive work that can be delayed. This work cannot require any part of the the syntax tree or semantic model.
* Information from multiple sources needs to be combined, such as adding the TFM or information from a different syntax node.
* Naming and pre-calculating values makes the code of the later step to output source code easier to read.

The transformations that are available are discussed in the [pipelines article](..\pipeline.md). While it is not intuitive, it is generally faster to do discrete steps than to put too much into one step, because caching occurs after each step. This means that you should have at least as many steps as logical places to cache, which is any location that when unchanged can save later work.

Some generators will not have any transformations beyond the initial filtering and extraction into a domain model.

## Example

This example builds on the design of the [Initial filtering article](initial-filtering.md#example). You can see how this code is used in [Putting it all together](putting-it-all-together.md#example).

Two transformations are done: a `Where` transformation to remove nulls and `Select` transform the initial `ClassModel` into an output friendly `CommandModel`. The initial transformation is performed each time generation runs, which can be quite often. The transformations run only when needed due to changes in the code, which will be comparatively rare.

Creating an output friendly model from the initial model uses a `Select` which calls the `GetCommandModel` method to isolate the transformation for readability and testing. This method transforms `ClassModel` and `OptionModel` to `CommandModel` and `PropertyModel`, respectively.

The compelling reason for separating this step is that retrieving the XmlDescription is relatively slow. By placing doing this work in a follow-on transformation, rather than in the initial extraction, it occurs only when the underlying data is changed. There is also pre-calculation of the casing of symbol names which makes the outputting code easier to read:

```csharp
    public static CommandModel GetCommandModel(InitialClassModel classModel,
                                        CancellationToken cancellationToken)
    {
        // null is not expected, but may happen with invalid code
        if (classModel is null) { return null; }

        var aliases = Helpers.GetAttributeValues(classModel.Attributes, "AliasAttribute");
        var options = new List<OptionModel>();
        foreach (var property in classModel.Properties)
        {
            // since we do not know how big this list is, check cancellation token
            cancellationToken.ThrowIfCancellationRequested();
            var optionAliases = Helpers.GetAttributeValues(property.Attributes, "AliasAttribute");
            options.Add(new OptionModel(
                $"--{property.Name.AsKebabCase()}",
                property.Name,
                property.Name.AsPublicSymbol(),
                property.Name.AsPrivateSymbol(),
                optionAliases,
                Helpers.GetXmlDescription(property.XmlComments),
                property.Type.ToString()));
        }
        return new CommandModel(
                name: classModel.Name.AsKebabCase(),
                originalName: classModel.Name,
                symbolName: classModel.Name.AsPublicSymbol(),
                localSymbolName: classModel.Name.AsPrivateSymbol(),
                aliases,
                Helpers.GetXmlDescription(classModel.XmlComments),
                classModel.Namespace,
                options: options);
    }
```

The initial extraction returns `null` if the type symbol is not found. Generators will often run on invalid code and must behave gracefully, such as managing `null`. This check occurs even though we know the previous step filtered out nulls, because generators should be as defensive as practical because a crashing generator can be hard to debug.

The `foreach` loop is used instead of a LINQ `Select` to support cancellation. Cancellation commonly occurs when a generation cycle is not complete and the next generation starts. In the normal case of a small number of properties and reasonably sized XML comments, cancellation support is not needed. But in a pathological case, or something wrong that results in a large number of properties or XML comments cancellation is helpful.

This method uses a number of helper methods. `GetAttributeValues` avoids repeating a LINQ `Select`, and will support additional attributes if this example is extended to cover more command and property attributes:

```csharp
        public static IEnumerable<string> GetAttributeValues(IEnumerable<AttributeValue> attributes, string attributeName)
          => attributes
              .Where(x => x.AttributeName == attributeName)
              .Select(x => x.Value.ToString());
```

The summary of the XML comment is used as the description. Extracting this requires loading the XML as `XDocument` and retrieving the value of the summary:

```csharp
        public static string GetXmlDescription(string doc)
        {
            if (string.IsNullOrEmpty(doc))
            { return ""; }
            var xDoc = XDocument.Parse(doc);
            var desc = xDoc.DescendantNodes()
                .OfType<XElement>()
                .FirstOrDefault(x => x.Name == "summary")
                ?.Value;
            return desc is null
                ? ""
                : desc.Replace("\n", "").Replace("\r", "").Trim();
        }
```

A series of string helper methods provide standard casing for outputting source code from the command and option models:

```csharp
    public static string AsPublicSymbol(this string val)
    => char.IsUpper(val[0])
        ? val
        : char.ToUpperInvariant(val[0]) + val.Substring(1);

    public static string AsPrivateSymbol(this string val)
        => char.IsLower(val[0])
        ? val
        : char.ToLowerInvariant(val[0]) + val.Substring(1);

    public static string AsKebabCase(this string val)
    {
        // this is not particularly performant or correct
        return char.ToLower(val[0]).ToString() +
            string.Join("",
                        val.Skip(1).Select(c => char.IsUpper(c) ? $"-{char.ToLower(c)}" : c.ToString()));
    }
```

Note that a full implementation of converting to kebab case is more involved to handle situations like two adjacent upper case letters.

## Testing the example

Testing transformations in isolation uses handcrafted versions of the input. This makes isolation easy, although maintaining a handcrafted model for each scenario can be tedious so avoid creating more scenarios than necessary. The test is very similar to the test of the initial transformation and uses [XUnit's theory feature]() and [Verify to confirm output]():

```csharp
        [Theory]
        [InlineData("SimplestPractical", typeof(SimplestPractical))]
        [InlineData("WithOneProperty", typeof(WithOneProperty))]
        [InlineData("WithMulitipeProperties", typeof(WithMultipleProperties))]
        [InlineData("WithXmlDescripions", typeof(WithXmlDescriptions))]
        [InlineData("WithAliasAttributes", typeof(WithAliasAttributes))]

        public Task Initial_class_model(string fileNamePart, Type inputDataType)
        {
            var initialModel = Activator.CreateInstance(inputDataType) is TestData testData
                ? testData.InitialClassModel
                : throw new ArgumentException("Unexpected test input type", nameof(inputDataType));

            var commandModel = ModelBuilder.GetCommandModel(initialModel, TestHelpers.CancellationTokenForTesting);

            return Verifier.Verify(commandModel).UseDirectory("InitialModelSnapshots").UseTextForParameters(fileNamePart);
        }
```



The `TestData` class and using `Verify` was introduced in the [previous article on Initial filtering](design-input-data.md#testing-the-example). The full version of the class is:

```csharp
    public class TestData
    {
        protected TestData(string inputSourceCode)
        {
            InputSourceCode = inputSourceCode;
        }

        protected TestData(string inputSourceCode, InitialClassModel initialClassModel, CommandModel commandModel)
        {
            InputSourceCode = inputSourceCode;
            InitialClassModel = initialClassModel;
            CommandModel = commandModel;
        }

        public string InputSourceCode { get; }
        public InitialClassModel? InitialClassModel { get; }
        public CommandModel? CommandModel { get; }

    }
```

A separate constructor is used to create data for the initial filtering and later steps in the incremental generator pipeline. Extraction tends to be the most complex step and may need extra testing - in the [full example extra testing is done for attributes]().

The test data is created in a separate static class for each step, allowing easier side by side comparisons and making the scenario definitions easier to read. An example of a scenario is:

```csharp
    public class SimplestPractical : TestData
    {
        public SimplestPractical()
            : base(inputSourceCode: @"
namespace MyNamespace
{
    public class MyClass{}
}",
                   initialClassModel: TestDataInitialModels.SimplestPractical,
                   commandModel: TestDataCommandModels.SimplestPractical)
        { }
    }
```

The input for the transformation test is the `initialClassModel`. An example of manually creating the initial class model is:

```csharp
    public static InitialClassModel SimplestPractical
        => new("MyClass",
               "",
               Enumerable.Empty<AttributeValue>(),
               "MyNamespace",
               Enumerable.Empty<InitialPropertyModel>());
```

You can [read more about using Verify for testing in the article on the initial transformation]().