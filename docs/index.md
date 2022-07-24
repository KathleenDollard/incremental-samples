# Incremental source generators

Roslyn incremental generators are run by the compiler to add source code to the compilation. They are built on the concept of a [pipeline](pipeline.md) of actions that are performed later and is used to avoid doing any unnecessary work. Since source generators run quite often, the previous output can be reused in the vast majority cases. The major difference between Roslyn V1 source generators and Roslyn incremental source generators is that use the pipeline and caching to avoid unnecessary work. Incremental generators must be written correctly for this caching to work.

> [!IMPORTANT]
> Roslyn V1 generators are often slow; its why we build Roslyn incremental generators. All new Roslyn source generators should be incremental generators and existing generators should be converted as time allows.

The development of any source generation is complicated because the resulting code is effectively an abstraction that does not exist until generation is run. Developing Roslyn source generator has unique complexity;

* The normal deployment mechanism is NuGet which is awkward during development.
* When the input is source code, the input may be incomplete or incorrect.
* When the input is incomplete or incorrect, it is often but not always correct for the compiler to report the error.
* While unit testing of individual parts of the compiler in isolation is essential, you also have to mimic the compiler for testing.
* The important behavior is not your generator but the behavior of the generated code, so you also need to test this.
* Because Roslyn source generators run during compilation, it is essential that they are very fast.
* Incremental generators are a radical shift from Roslyn V1 source generators.

These articles explain how Roslyn incremental source generators work and how to setup an inner loop three kinds of test:

* [Overview](overview.md)
* [Pipeline](pipeline.md)
* [Development inner loop](development.md)
* [Testing generators via the compilation](testing-generators-compilation.md)
* [Performance Guidelines](performance-guid elines.md)
* [Tutorial](tutorial.md)
* [Converting V1 Roslyn source generators](converting-v1-generators.md)
* [Tips](tips.md)

The most important article is the [performance Guidelines](performance-guidelines.md).

> [!WARNING]
> As the compiler team, we have a responsibility to all C# and VB developers to maintain the performance of the compiler. If your generator is affecting that performance, we will remove it from the design time compilation. If that happens, your uses will provably see errors in their code and missing IntelliSense until they explicitly build. This area will evolve, both in how we mark generators and in the tools available for you to understand the performance of your code.

> [!IMPORTANT]
> Roslyn V1 generators are often slow; its why we build Roslyn incremental generators. All new Roslyn source generators should be incremental generators and existing generators should be converted as time allows.
