# Design models

Once you understand the code you plan to generate, you can explore the data you need. This data will be expressed by programmers using your generator via source code or additional files, or one of the other providers discussed in [Pipelines](../pipeline.md#providers). Understanding what data you need lets you design input that makes sense to users of your generator.

Review the generated code in your sample project and mark all of the things that will vary based on what users of your generator are doing. That will include individual symbols or words, in source code and comments. It will also include blocks of code that are repeated, where you have a block per item in your current output. It probably includes conditional blocks of code. You may want to copy your example output into a tool that allows additional formatting, such as word, and decorate the code, such as using colors or adding opening and closing markers. You may want to remove code coloration prior to doing this.

The output of this evaluation will be a domain class model for generation. Each item you need for generation needs to appear in this model. Where there are multiples and loops will be involved during generation, include a collection. If conditions will result in significantly different output, use similar classes unified via a base class or interface, or simple allow values in the model to be empty if they are unused. 

During this step, you may be inspired to create additional output samples. 

## Example

Next Step: [Initial extraction](initial-extraction.md).