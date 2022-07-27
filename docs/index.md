---
title: Roslyn Incremental Source Generators
description: Source generators add C# or VB code to the compilation. Roslyn incremental source generators are defined as a series of steps to allow caching and cancellation.
author: KathleenDollard
ms.author: kdollard
ms.date: 6/11/2022 
ms.topic: overview
---
# Roslyn incremental source generators

Roslyn incremental generators are run by the compiler to add source code to the compilation. They are built on the concept of a [pipeline](pipeline.md) of actions to be performed later. The pipeline allows caching, which avoids doing any unnecessary work. Since source generators are run quite often, the previous output can be reused in the vast majority cases. The major difference between Roslyn V1 source generators and Roslyn incremental source generators is that incremental source generators use this pipeline and caching to avoid unnecessary work. Incremental generators must be written correctly for this caching to work.

> [!IMPORTANT]
> Roslyn V1 generators are often slow; its why we build Roslyn incremental generators. All new Roslyn source generators should be incremental generators and existing generators should be converted as time allows.

The development of any source generation is complicated because the resulting code is effectively an abstraction that does not exist until generation is run. Developing Roslyn source generator has unique complexity;

* Roslyn itself can be intimidating if you are not familiar with it - although recommended incremental generators makes this a little easier.
* Because Roslyn source generators run during compilation, it is essential that they are very fast.
* Designing your generation output may require unfamiliar features, especially partial classes and methods.
* Designing your generation input requires understanding/evaluating what other coders will find most natural.
* When the input is source code, the input your generator receives may be incomplete or incorrect.
* When the input is incomplete or incorrect, it is subtle to find the best way to report the error - the underlying compiler error or an adjacent analyzer may be the best choice.
* End to end testing of the generator requires mimicking the design time environment, which can be complicated to set up to fit your test demands (unit test of supporting methods as much as possible).
* The important behavior is not your generator but the behavior of the generated code, so you also need to test your output in the context where it will be used.
* The normal deployment mechanism is NuGet which is awkward during development.

In addition to these challenges, incremental generators have additional challenges for people that were familiar with V1 generators:

* Incremental generators are a radical shift from Roslyn V1 source generators.
* Pipelines are a powerful technique that is common in functional programming, but many C# programmers are not familiar with it.

These articles explain how Roslyn incremental source generators work and how to setup an inner loop three kinds of test:

* [Overview](overview.md)
* [Pipeline](pipeline.md)
* [Creating an incremental source generator](creating-a-source-generator/index.md)
* [Testing generators via the compilation](testing-generators-compilation.md)
* [Performance Guidelines](performance-guidelines.md)
* [Tutorial](tutorial.md)
* Converting V1 Roslyn source generators (future article)
* [Tips](tips.md)

The most important article is the [performance Guidelines](performance-guidelines.md).

> [!WARNING]
> As the compiler team, we have a responsibility to all C# and VB developers to maintain the performance of the compiler. If your generator is affecting that performance, we will remove it from the design time compilation. If that happens, your uses will provably see errors in their code and missing IntelliSense until they explicitly build. This area will evolve, both in how we mark generators and in the tools available for you to understand the performance of your code.