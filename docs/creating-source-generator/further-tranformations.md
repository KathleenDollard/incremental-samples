# Further transformations

As discussed in the [pipelines article](..\pipeline.md), there are a number of transformations that can be applied to the domain models that are returned as part of the initial filtering.

A few transformations are particularly interesting:

* Use `Where` to filter domain model instances that are marked as uninteresting. This might be done by the transform returning null for syntax nodes that require no further generation.
* Use `Select` to transform from the initial domain model to a domain model that is friendlier for code output. This can be quite helpful since the initial domain model should be as simple as possible to capture details from the compilation to allow rapid checking of the cache. Outputting non-trivial code can become quite difficult to understand if there are also data translations.
* Split up a single domain model into multiple domain models using `SelectMany`. One scenario where this could occur is if a class is attributed, and you want the resulting generation to result in a separate file for each property or method.
* Summarize the domain models of a set of attributed syntax nodes using `Collect`. In some cases you might generate a file for each of a set and also a file for the total. The initial transformation probably results in either the individual or the combined domain model, and `SelectMany` and `Collect` allows you to get the other.
* Adding a consistent value to each of a set of domain models using `Combine` and `Select` [[ continue example ]]
* Providing *join* behavior using `Combine` and `Select` [[ warning and then continue example ]]

Some generators will not have any transformations beyond the initial filtering and extraction into a domain model. 