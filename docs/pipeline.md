---
title: Roslyn incremental source generator pipeline
description: Roslyn incremental generators use a pipeline approach. Learn how pipelines work and the specific steps available to the Roslyn incremental generator.
author: KathleenDollard
ms.author: kdollard
ms.date: 6/11/2022 
ms.topic: tutorial
---
# Incremental source generator pipeline

[[ Review: The spec shows RegisterExecutionPipeline. Is this an old form of pipeline registration? ]]

Roslyn incremental source generators are based on a pipeline of operations. A pipeline is a set of functions defined as delegates. When the pipeline executes, the output of each function is the input to the next function. The pipeline itself performs operations between steps, and can determine whether steps are run. In the case of incremental generators, the pipeline only runs steps when their results are used and their input has changed from the cached values of the input. 

It is important to understand that pipelines area a series of steps that are defined once and used many times. You define a Roslyn incremental generator pipeline in the generator's `Initialize` method, which runs only once. In most pipelines, including the Roslyn incremental source generator, the output of each step is a generic container allowing the infrastructure of the pipeline to be unaware of the contents within the container.

LINQ is an example of this type of pipeline. The container is an `IEnumerable<T1>` collection. The pipeline is created as a series of delegates passed to methods like `Where`. This LINQ pipeline is executed at a later time, and possibly executed multiple times, by iterating with `foreach` or calling a method like `ToList`. As you call methods of the LINQ pipeline like `Select` or `OfType`, a new `IEnumerable<T2>` container is returned. `T1` and `T2` may be of the same or different types.

There is a great deal of theoretical work around these types of containers within pipelines. You can explore this by searching for terms like "lambda calculus" or exploring functional programming books, but you will not need a deep understanding to create Roslyn incremental generators. You will only need to understand a few things:

[[ Bill: Can we suggest books: Buonanno's is quite good. ]]

* The pipeline contains functions - delegates or lambda expressions.
* The pipeline does not execute until requested.
* The pipeline operates on a generic container.
* The pipeline does not need to know the contents of the containers, although the functions that make up the pipeline do.
* The pipeline can intervene after any step to provide features like cancellation and skipping further steps if the input is unchanged based on caching.
* The pipeline will not do unnecessary work - if a value is not used, it's value is not calculated.

[[ Review:  The spec refers to IValueProvider<T>, but I cannot find this interface. Is this now just a logical grouping of the ..Value.. and ..Values.. providers? (That is how I wrote this article) ]]

The incremental generator pipeline supports two container types: `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>`. Notice that the first has a singular and the second plural for `Value`, because one contains a single value and the other a collection of. The operations to create these containers are [providers](#providers). The available [pipeline operations](#pipeline-operations) transform the containers, and the pipeline ends by [outputting source code](#output) using one or more `RegisterSourceOutput` or `RegisterImplementationSourceOutput` methods. The central elements of a pipeline are the generic containers - for incremental generators, this is the incremental value providers.

> [!TIP]
> `IncrementalValuesProvider` (plural) manage the cache separately for each item and if an item is changed, further work is done based only on the change in that item. Also, if used to output code `...Values...` providers produce one file per item in the collection. `IncrementalValueProvider` (singular) may contain a collection of items after using the `Collect` transform. Further work is done based on any item in the collection changing and if used to output code, one file is created for the collection. Both are important to generation.

## Providers

The incremental generator pipeline is created in the generator's `Initialize` method. This is the only member of the `IIncrementalGenerator` interface. This method is passed a single parameter which is an `IncrementalGeneratorInitializationContext`.

You get `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>` containers from the `IncrementalGeneratorInitializationContext`. `SyntaxValueProvider` provides two methods for the  creation of a specific syntax provider, and the others  are static properties on the context that are lazily created if you request them.

This table abbreviates `IncrementalValueProvider` and `IncrementalValuesProvider` to make it easier to read:

| Provider                        | Type                                                       |
|---------------------------------|------------------------------------------------------------|
| `SyntaxValueProvider`           | *[See text that follows this table](#SyntaxValueProvider)* |
| `CompilationProvider`           | `...Value...<Compilation>`                    |
| `AdditionalTextsProvider`       | `...Values...<AdditionalText>`                |
| `AnalyzerConfigOptionsProvider` | `...Value...<AnalyzerConfigOptionsProvider>`  |
| `MetadataReferencesProvider`    | `...Values...<MetadataReference>`              |
| `ParseOptionsProvider`          | `...Value...<ParseOptions>`                   |

[[ Review: confirm the following with team: do we/can we cache non-syntax providers? ]]

The incremental value providers for the properties (all of the above except the `SyntaxValueProvider`) contain instances of classes that cannot be cached. The next step for all of these should be to call [`Select`](#pipeline-operations) to extract the data you need into a model.

### SyntaxValueProvider
 
The `SyntaxValueProvider` contains a two methods that allows your to create an `IncrementalValuesProvider` instances. The first is `ForAttributeWithMetadataName` which creates a provider based on the qualified name of an attribute.  The `SyntaxNode` passed to the predicate and contained in the `GeneratorAttributeSyntaxContext` is the node on which the attribute is placed (not the attribute itself):

```csharp
public IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
    string fullyQualifiedMetadataName,
    Func<SyntaxNode, CancellationToken, bool> predicate,
    Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
```

The second is `CreateSyntaxProvider` whose signature is;

```csharp
public IncrementalValuesProvider<T> CreateSyntaxProvider<TModel>(
    Func<SyntaxNode, CancellationToken, bool> predicate, 
    Func<GeneratorSyntaxContext, CancellationToken, TModel> transform)
```

The primary difference between these approaches is that the provider created with `ForAttributeWithMetadataName` is only called for syntax nodes that are decorated with the specific attribute, and the provider created with `CreateSyntaxProvider` is called for every node in the user's compilation. If at all possible, design your generator so the input is based on attributed nodes to improve performance. This method was added in Visual Studio 17.3, and should be used in generators created for Visual Studio 17.4 and up.

In both cases, the first delegate is a predicate `true` if the syntax node should be considered further. For providers created with `ForAttributeWithMetadataName`, the attribute may be sufficient to indicate a valid `SyntaxNode`, so this predicate may always return `true`. For providers created with `CreateSyntaxProvider` it is essential that the predicate be extremely fast and exclude as many nodes as possible. The predicate should use pattern matching to immediately return `false` for any syntax nodes of the wrong type and should then exclude as many additional syntax based on things like naming. Note that it is tricky to correctly evaluate attributes due to variations in how the user may lay them out, so using `ForAttributeWithMetadataName` is also easier.

The transform step of your generator should extract the values you need from the `SyntaxNode` and semantic model into a model that has value equality. Ensuring this model is created and has value equality is essential to the incremental generator working correctly.

The transform delegate of `ForAttributeWithMetadataName` receives a `GeneratorAttributeSyntaxContext` which contains the `SyntaxNode`, the corresponding `ISymbol`, the semantic model, and `AttributeData` for any other attributes that appear on the `SyntaxNode`. 

The transform delegate of `CreateSyntaxNode` receives a `GeneratorSyntaxContext` which contains the `SyntaxNode` and the semantic model you can use to retrieve additional information such as the `ISymbol` or `IOperation`.

The transform delegate is called for every `SyntaxNode` that matches the predicate. Use this delegate to extract the information that you will need later in generation, rather than returning any portion of the Roslyn syntactic tree or semantic model. This result of this delegate must be a simple value or a user defined model that uses value equality.

This step can also do prepare for further filtering by returning a recognizable value, such as `null`, when the transform determines that the item is not needed.

### Other providers

[[ questions out to  Chris. What  is cacheable, and what is just inherently slow and when do we look anew at the underlying data for these as there have been questions in the past that the additional texts  ]]

## Further transformations

Transformations allow you to reshape the data for easier code outputting, and doing this in a separate is especially important when the reshaping may be slow. Transformations also allow you to combine a collection into a single item if it should output a single file, or split a collection into many items if they should create many files. And they allow you to combine the results from multiple providers. Sometimes the model created from the syntax value provider is ready for outputting code and further pipeline operations are not needed.

When providers do not return an item with value equality, the first step of transformations should be `Select` to extract the data you need into a simple value or model that uses value equality. See [Other providers](#other-providers) for more information.

As an example of transformation, you could combine information from a syntax provider with analyzer config options. To facilitate caching, separately create each model and then use `Combine` on the models.

As a more complicated example, you may want to create a single partial class that contains code for one or more attributed methods in the user's source, with a partial class for each of the user's classes that contains an attributed method. In this example, the classes are not also attributed. Extracting models from the syntax nodes of the attributed methods using `SyntaxProvider.ForAttributeWithMetadataName` can be quite fast. As you process each node in the transform delegate, you can add class information for each method. While this results in some redundant work, it is fast and quickly gives you a cacheable model, so any further work is done only when required. But, now you have a model instance per method, where you need an model instance per class to create output. Using the `Collect` method you could create a single model with a collection containing all of the methods. You could generate from this combined model, resulting in a single file that contains all of the generated partial classes, but if you or users will inspect the generated code this may result in an overly large unwieldy file. To avoid this, you could split the single model into individual class models using `SelectMany`, and then generate separate files per class.

The pipeline methods for incremental generators are extension methods on one or both of `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>`. This table abbreviates `IncrementalValueProvider` and `IncrementalValuesProvider` to make it easier to read:


| Method           | Input type                                 | Return type  | Comments                                                                                                                     |
|------------------|--------------------------------------------|--------------|------------------------------------------------------------------------------------------------------------------------------|
| Select           | `..Value..`                                | `..Value..`  |                                                                                                                              |
| Select           | `..Values..`                               | `..Values..` |                                                                                                                              |
| SelectMany       | `..Value..`                                | `..Values..` |                                                                                                                              |
| SelectMany       | `..Values..`                               | `..Values..` | IEnumerable and immutable array overloads   @chsienki Why IEnumerable and  immutable array overloads here and not everywhere |
| Where            | `..Values..`                               | `..Values..` |                                                                                                                              |
| Collect          | `..Values..`                               | `..Value..`  |                                                                                                                              |
| Combine          | `..Value..`, `..Value..`                   | `..Value..`  |                                                                                                                              |
| Combine          | `..Values..`, `..Value..                   | `..Values..` |                                                                                                                              |
| WithComparer     | `..Value..`, `IEqualityComparer<TSource>`  | `..Value..`  |                                                                                                                              |
| WithComparer     | `..Values..`, `IEqualityComparer<TSource>` | `..Values..` | Allows per transformation equality for caching                                                                               |
| WithTrackingName | `..Values..`, `string`                     | `..Values..` |                                                                                                                              |
| WithTrackingName | `..Value..`, `string`                      | `..Value..`  |                                                                                                                              |

You can find more details on these methods in the [Incremental Generators specification](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md). Each of the transforms fit a specific need:

* Use `Where` to abandon data when you understand it is not of value. This might if the transform of `SyntaxValueProvider.CreateSyntax` or `SyntaxValueProvider.ForAttributedSyntaxProvider` returns null for syntax nodes that require no further generation.
  
* Use `Select` to transform from the initial domain model to a domain model that is friendlier for code output. The initial domain model should be as simple as possible to capture details from the compilation to allow rapid checking of the cache. This is often in the language of source code, rather than in the domain language of your problem, and outputting non-trivial code is much easier if the model is in the final domain and pre-calculates common values.

* Use `SelectMany` to split up a single domain model into multiple domain models. For example, use this if `SyntaxValueProvider.ForAttributedSyntaxProvider` returns attributed classes, and you want to output a separate file, or do other work, based on each property or method.

> [!TIP]
> Whenever `Collect` is used, caching is no longer granular and further work is done when any item changes. If this is followed by a `SelectMany`, caching and further work becomes granular again, based on the value equality.

* Use `Combine` to combine two `...Value...` providers to create a `...Value...`  provider that is a tuple of the two input values.
  
* Use `Combine` with one `...Values...` and one `...Value...` provider to add information to each item in `...Values...`. The result will be a tuple of each item in `...Values...` and the item in `...Value...`. You can then use `Select` to create a friendlier model for outputting code.

* Use `Collect` and `SelectMany` to change the grouping within a `...Values...` provider, such as shifting items that are per method to those that are per class. 
  
* Use `Collect`, `Combine`, `Select` and `SelectMany` to combine two `...Values...` providers. `Collect` can create two `...Value...` providers, each containing a collection. `Combine` creates a tuple of the two collections. The work to join the elements is done in the `Select` transformation, and should be fast because it will happen whenever any item in input collection changes. Finally, `SelectMany` can create a `...Values...` provider of the new individual items.

You can see examples of some these in the [Creating an incremental source generator section](creating-a-source-generator/further-tranformations.md).

## Output

There are three methods to output code: `RegisterImplementationSourceOutput`, `RegisterSourceOutput`,and `RegisterPostInitializationOutput`. `RegisterImplementationSourceOutput` outputs code before your generator runs. `RegisterSourceOutput` and `RegisterPostInitializationOutput` are part of your generation pipeline.

### RegisterPostInitializationOutput

`RegisterPostInitializationOutput` is special because it allows you to output code that is added to the input compilation. It is run once and has no input. This is useful for adding code that should always be in the user's compilation. This is especially useful for partial classes that provide initial access to your generator to provide features such as IntelliSense as soon as your generator is referenced. Partial classes must all be in the same assembly and namespace.

This feature can also be used for attributes and base classes. However, unless they need to be `internal`, it may be preferable to place them in a separate runtime library. See the [tip on when to use a runtime library for more information](tips.md#attributes-in-registerpostinitializationoutput-or-a-separate-package)

### RegisterSourceOutput and RegisterPostInitializationOutput

`RegisterSourceOutput` and `RegisterPostInitializationOutput` are the step of your generation pipeline that outputs code.

`RegisterImplementationSourceOutput` is almost identical to `RegisterImplementationSourceOutput`, but also indicates that the source has no semantic impact. You can use this if the code you generate does not impact the IDE, such as providing IntelliSense entries or diagnostics like errors or warnings.

> [!IMPORTANT]
> `RegisterImplementationSourceOutput` was added to the initial incremental generator API to support future use. As of Visual Studio 17.3, it is not yet implemented and all generators run during design time builds and should follow all of the performance guidelines.

There are two overloads of each of these methods - one takes an `IncrementalValueProvider` (singular) and the other takes an `IncrementalValuesProvider` (plural). The `...Value...` overload outputs a single file. The `...Values...` overload outputs a separate file for each item in the collection. Each overload also takes a delegate that will create the output.

The delegate includes parameters for the `SourceProductionContext` and a single item - either the  `...Value...` provider item or the current item in the `...Values...` collection. The work in the delegate will first create a string that is the code to output and then call the `AddSource` method of the `SourceProductionContext` to output the code. The `AddSource` method has two parameters: the *hintname* and the new source code as a string. The *hintname* is the output filename.

If your output is complicated or includes an unbounded collection of data, check the `CancellationToken` on each iteration or before after slow operations to ensure you respond to cancellation.

`SourceProductionContext` also includes the `ReportDiagnostic` method to add diagnostics to the compilation. See [Let compiler and separate analyzers report diagnostics](tips.md#let-compiler-and-separate-analyzers-report-diagnostics) for suggestions on when to report diagnostics through your generator.

> [!CAUTION]
> Incremental generators only do work that is required and this means they only do work to create incremental value providers that are used in `RegisterSourceOutput` and `RegisterPostInitializationOutput`. If a step in your pipeline does not contribute to output, it will never run. This can be confusing early in development as it is natural to focus on creating data before output.
>
> To avoid this, the [Creating a source generator walkthrough](creating-a-source-generator/index.md) creates and tests each step of the pipeline, prior to pulling it all together in the generator.