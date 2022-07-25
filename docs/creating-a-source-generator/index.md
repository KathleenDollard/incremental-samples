# Creating a source generator

- [Design output](design-output.md)
- [Design input data](design-input-data.md)
- [Initial filtering](initial-filtering.md)
- [Supply further transformations (optional)](further-tranformations.md)
- [Output code](output-code.md)
- [Putting it all together](putting-it-all-together.md)
- [Testing generation]()
- [End to end testing]()
- [Reporting diagnostics]()
  
It's logical to think about the work you are doing in your generator. But from the generator perspective, avoiding work your generator *does not need to do* is as or more important. Work does not done in these cases:

- The syntax nodes are not relevant to generation,
- The relevant portions of the application have not changed since the last time the generator ran, so the previous generation artifacts can be used,
- The underlying code is invalid and will fail prior to generation having meaning (for example, an important symbol lookup fails), or
- The editor starts another generation cycle and the generator should immediately cease work.
- Incremental generators are designed around these exclusions, not around the actual work, which is one of the reasons they can be tricky to understand.

This is accomplished by splitting generation into discrete steps. The purpose of each step is:

- *Initial filtering* provides "entry points" for generation allowing the exclusion of all other syntax nodes. When the input data is source code, the details of this step are declared in one of the two SyntaxProvider methods `ForAttributeWithMetadataName` or `CreateSyntaxProvider`. `ForAttributeWithMetadataName` ensures a fast generator, attributed nodes are indexed to allow filtering nodes by attribute name to be exceedingly fast.

- *SyntaxProvider transform* which is passed to `ForAttributeWithMetadataName` or `CreateSyntaxProvider` should return an instance of a domain type that has *value equality* and is the simplest possible way to define the data extracted from the code. This should not return [[long list  no syntax or semantic model artifacts]]. This is critical to fast generators, and also critical to allow you to unit test this part, which is often the most challenging part of your generator to write and maintain. This initial model will often be in the language of source code (such as `ClassModel` and `PropertyModel`).

 After this step, the generator will look at your domain instance and check it against the cache. In the vast, vast majority of cases, no more work will be done. Much of your optimization effort should be supply

- *Further transforms* can be used to change the shape of the domain model prior to generation. It is generally easier to output code from a model that closely mimics what you are building and pre-calculates values. See the [pipeline article](../pipeline.md) for how to perform common tasks.
  
  [[ move this to pipeline.md
  - Remove values that were deemed unimportant during the transform.
  - Change the shape of the model - 
  - Collect the 

  The shape of the simplest version of the extracted data will often be awkward for emitting code, you may need a summary, or a breakdown into individual pieces. Performance should still matter, but it is not as critical because this code only runs when the user has made changes that alter this generation. [[link to logical things to do which is in here somewhere.]]]]

- *Code output* uses the final domain model to output code.

The fun part 😉. I highly recommend a domain type as input that is friendly to generation. Doing calculations while generating means adds a layer of complexity. You cannot avoid the layers of your output code and the C# code that ruhns to layout the code you are outputting. This is plenty to hurt your head without including calculations to supply values you are outputting.

SyntaxProvider predicate (via `)
If needed, further filtering to discard nodes where the attribute is incorrect. This is a critical step with CreateSyntaxProvider and should return non relevant nodes as quickly as possible (like an initial type or kind comparison). Many generators using ForAttributeWithMetadataName will have fully filtered on the attribute and can just return true  - they should not repeat the attribute check.
