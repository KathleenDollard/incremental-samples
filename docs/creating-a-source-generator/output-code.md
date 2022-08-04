# Output code

The ease of outputting code depends on the complexity of the code and how well the domain model aligns with what you are generating. Consider using `Select` to transform your initial domain model to an output friendly one. These transformations can be as simple as adjusting naming and cases, and is particularly helpful when decisions on whether to output a block of code is complicated and a `Select` step can pre-calculate a Boolean value for outputting code.

A number of generation languages have been used through the years, and one one well suited for Roslyn generation is interpolated strings and raw interpolated strings. When outputting code, you will generally want to use a variation of verbatim strings so your strings can extend across multiple lines. For simple code, you can paste the code you want to output into a verbatim interpolated string and adding string interpolation placeholders. If you are using normal interpolated strings, you'll need to double up all of the curly brackets and double quotes. Starting with C# 11 you can use raw interpolated strings to avoid doubling up curly brackets and double quotes in your code, although you'll need to double the curly brackets you use to mark the interpolated string holes. You can find out more about [string variation in strings in this article](https://docs.microsoft.com/dotnet/csharp/programming-guide/strings/).

Break complex code into methods that return fragment of the code you want to create. This is particularly helpful when you have code that in conditionally included or when you need a block of code for every item in a collection.

## Example

This example builds on the design of the [Further transformations article](further-transformations.md#example). You can see how this code is used in [Putting it all together](putting-it-all-together.md#example).

Next Step: [Putting it all together](putting-it-all-together.md)