# Further transformations

Once you have your initial model, you may need to transform it prior to outputting code. Several things may lead to these transformations:

* The shape of the initial model does not align with code outputting. For example the initial extraction step results is one item per syntax node. The output step needs one item per file.
* There is expensive work that can be delayed and avoided when the output of the initial extraction is unchanged. 
* Domain models from the extraction step of multiple providers need to be combined. [[ need example ]] or information from a different syntax node.
* Pre-calculating values to reduce work during output or renaming that makes the code of the output step easier to read. 

Work done as part of further transformations should not require any part of the the syntax tree, semantic model, or compilation.

The further transformations that are available are discussed in the [pipelines article](..\pipeline.md). While it may not intuitive, it is generally faster to do discrete steps than to put too much into one step because caching occurs after each step. This means that you should have at least as many steps as logical places to cache, which is any location that when unchanged state can save later work.

Some generators will not have any transformations beyond the initial filtering and extraction into a domain model.

## Example

This example builds on the design of the [Initial filtering article](initial-filtering.md#example). You can see how this code is used in [Putting it all together](putting-it-all-together.md#example).

Two transformations are done: a `Where` transformation to remove nulls and `Select` transform the initial `ClassModel` into an output friendly `CommandModel`. The initial extraction is performed each time generation runs, which can be quite often. Further transformations run only when needed because the output of the extraction step is unchanged. Because the underlying syntax nodes are generally unchanged, further transformations and code output can often be skipped.

Creating an output friendly `CommandModel` from the initial `ClasModel` uses a `Select` which calls the `GetCommandModel` method to isolate the transformation for readability and testing. This method transforms `ClassModel` to `CommandModel` and `OptionModel`  to`PropertyModel`.

The compelling reason for separating this step is that retrieving the XmlDescription is relatively slow. Performing this work as part of transformations, rather than in the initial extraction, this slower work occurs only when the underlying data is changed. There is also pre-calculation of the casing of symbol names which makes the outputting code easier to read and some renaming to make the code of the output step easier to understand:

```csharp
    public static CommandModel GetCommandModel(InitialClassModel classModel,
                                        CancellationToken cancellationToken)
    {
        // null should have been filtered out, but finding one is not a reason to crash
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

The `foreach` loop is used instead of a LINQ `Select` to support cancellation. Cancellation commonly occurs when a generation cycle is not complete and the next generation starts. In the normal case of a small number of properties and reasonably sized XML comments, cancellation support is not needed. But in a pathological case, or something wrong that results in a large number of properties or large XML comments cancellation is helpful.

This method uses a number of helper methods. `GetAttributeValues` avoids repeating a LINQ `Select`, and will support additional attributes if this example is extended to cover more command and property attributes:

```csharp
        public static IEnumerable<string> GetAttributeValues(IEnumerable<AttributeValue> attributes, string attributeName)
          => attributes
              .Where(x => x.AttributeName == attributeName)
              .Select(x => x.Value.ToString());
```

The summary description of the XML comment is used as the description for the command. Extracting this requires loading the XML as `XDocument` and retrieving the value of the summary:

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
        // this is not particularly performant or correct version of kebab case
        return char.ToLower(val[0]).ToString() +
            string.Join("",
                        val.Skip(1).Select(c => char.IsUpper(c) ? $"-{char.ToLower(c)}" : c.ToString()));
    }
```

Note that this is not a full implementation of converting strings to kebab case and will not handle situations like two adjacent upper case letters.

## Testing the example

Further transformations can be tested in isolation. This version uses a handcrafted model for each scenario. This can be tedious so avoid creating more scenarios than necessary. If you are extremely comfortable with your verification strategy, you may experiment with deserializing the output of testing the initial extraction step. However, this is risky because if bad models are approved, tests will not fail at the step where the problem occurred.

The test is very similar to the test of the initial transformation and uses [XUnit's theory feature]() and [Verify to confirm output]() which were introduced in [initial extractions](initial-extractions.md):

```csharp
    [Theory]
    [InlineData( typeof(SimplestPractical))]
    [InlineData( typeof(WithOneProperty))]
    [InlineData( typeof(WithMultipleProperties))]
    [InlineData( typeof(WithXmlDescriptions))]
    [InlineData( typeof(WithAliasAttributes))]

    public Task Command_model(Type inputDataType)
    {
        var initialModel = GetInputSource(inputDataType, x => x.InitialClassModel);
        var className = inputDataType.Name;

        var commandModel = ModelBuilder.GetCommandModel(initialModel, TestHelpersCommon.CancellationTokenForTesting);

        return Verifier.Verify(commandModel).UseDirectory("Snapshots").UseTextForParameters(className);
    }
```

The `TestData` class was introduced in the [previous article on Initial filtering](design-input-data.md#testing-the-example). Testing further transformations requires a `ClassModel` that mimics the output of the initial extraction step. The full version of the class is:

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

The `CommandModel` is used to test the code output step.

The test data for each scenario is a separate class. An example of a scenario is:

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

The input for the further transformation test is the `initialClassModel`. The initial class model definitions for each scenario are grouped in a static class:

```csharp
internal static class TestDataInitialModels
{
    public static InitialClassModel SimplestPractical
        => new("SimplestPractical",
                    "",
                    Enumerable.Empty<AttributeValue>(),
                    "MyNamespace",
                    Enumerable.Empty<InitialPropertyModel>());
```

Next Step: [Outputting code](output-code.md).
