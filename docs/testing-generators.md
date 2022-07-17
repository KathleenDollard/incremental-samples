# Testing generators

Testing generators require some techniques you may not be familiar with. To see how to use these techniques, check out the testing docs referenced in [Creating source generators](creating-source-generators/../creating-source-generator/creating-a-source-generator.md).

## Creating syntax nodes for testing

The easiest way to create syntax nodes is to create code that is a string, either by pasting code into a string, or by opening a file that contains code. You'll rely on this code to be syntactically correct, or to contain only the syntax errors you expect. 