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

Building on the example from [Design input data](design-input-data.md#example), the generator needs to request nodes attributed with the `CommandAttribute` and transform that into a domain model that has value equaity. First, the design for the domain model: