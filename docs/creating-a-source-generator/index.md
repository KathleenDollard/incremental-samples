---
title: Creating a source generator
description: Walk-through the series of steps to create a Roslyn incremental source generator
author: KathleenDollard
ms.author: kdollard
ms.date: 6/11/2022 
ms.topic: overview
---
# Creating a source generator

- [Design output](design-output.md)
- [Design input data](design-input-data.md)
- [Create models](create-models.md)
- [Initial filtering](initial-filtering.md)
- [Supply further transformations (optional)](further-tranformations.md)
- [Output code](output-code.md)
- [Putting it all together](putting-it-all-together.md)
- [Testing generation]()
- [End to end testing]()
- [Reporting diagnostics]()
  
Creating a generators is a multi-step process. You need to determine what you are going to build, what input you need, create initial filters, possibly supply further transformations and create code to output. This walk-through goes through these steps in order. If you want a peek at how they fit together before your get started, you can read  [Putting it all together](putting-it-all-together.md) and reread it when you understand the contributing steps.

It's logical to think about the work you are doing in your generator. But from the generator perspective, avoiding work your generator does not need to do is as or more important. Work does not done in these cases:

- The syntax node is not relevant to generation.
- The relevant portions of the application have not changed since the last time the generator ran, so the previous generation artifacts can be used.
- The underlying code is invalid and will fail prior to generation having meaning (for example, an important symbol lookup fails).
- The editor starts another generation cycle so the current output will never be used.
  
Incremental generators are designed around these exclusions, which is one of the reasons they can be tricky to understand. One of the ways they minimize work is to split generation into discrete steps. The purpose of each step is:

- *Initial filtering* of syntax provides an "entry point" for generation allowing the exclusion of all other syntax nodes. These use one of either the `ForAttributeWithMetadataName` or the `CreateSyntaxProvider` of the `SyntaxProvider`. `ForAttributeWithMetadataName` ensures a fast generator, attributed nodes are indexed to allow filtering nodes by attribute name to be exceedingly fast. When input is from a provider other than the syntax provider, the first filtering is in `Where` and `Select` that are discussed in further transformations, discussed below.

- *SyntaxProvider transform* delegate is passed to`ForAttributeWithMetadataName` or `CreateSyntaxProvider` should return an instance of a domain type that has *value equality* and is the simplest possible way to define the data needed from the code. This should not return any semantic model artifacts or elements of the syntax tree. Instead it should extract the information relevant into a domain model you design. This is critical to fast generators, and also critical to allow you to unit test this part, which can otherwise be one of the most challenging part of your generator to write and maintain.

 After this step, the generator will look at your domain instance and check it against the cache. In the majority of generation cycles, the user did not change these inputs and no more work needs to be done by the generator because it can use the previous artifacts.

- *Further transforms* can be used to change the shape of the domain model prior to generation. It is generally easier to output code from a model that closely mimics what you are building and pre-calculates values. You also may need a summary, a breakdown into individual pieces, or to combine values from multiple providers. See [pipeline article](../pipeline.md) to learn how to perform common tasks.

- *Code output* uses the domain model that results from the transformations  to output code. Using a domain model removes a level of complexity while your are outputting code by organizing and pre-calculating values.

These steps are part of the pipeline which uses the delegates you specify to manage generation. Reading [pipeline article](../pipeline.md) will make it easier to understand how to design and build your generator.

Next Step: [Design output](design-output.md)
