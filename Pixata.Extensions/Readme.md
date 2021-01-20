# Pixata.Extensions

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Extensions/Icon/Vroum%20Vroum.png "Pixata") 

Some useful utility classes and methods I've developed over the past few years. I have only put in a few so far, and the test project is woefully empty, but hopefully that will change over time. Here is a brief description of the methods in the classes so far...

## CollectionExtensionMethods
`ToObservableCollection<T>()` - Converts any collection that implements IEnumerable<T> into an ObservableCollection<T>. Provides a neater syntax than passing the collection to the `ObservableCollection`'s constructor.

`ForEach<T>()` - Allows you to use `ForEach` on any collection that implements IEnumerable<T>, as opposed to the similarly named method built in to the .NET framework that requires you to cast the collection to a `List<T>` first.

`Flatten<T>()` - Enables you to flatten a hierarchical collection.

## DateExtensionMethods
`ToPrettyString()` - Formats a date as "12th January 2021". This relies on the `OrdinalSuffix()` method in `IntExtensionMethods`.

`EndOfMonth()` - Gives you a `DateTime` that represents the last second of the month containing the date you pass in.

## EnumHelper
`GetValues<T>()` - Enumerate the values of an `enum`.

## ExceptionExtensions
`MessageStack()` - Returns the exception messages all the way down the InnerException stack.

`InnerType()` - Returns the type of the innermost exception.

## IntExtensionMethods
`OrdinalSuffix()` - Returns the ordinal suffix, eg "st" for 1, 21, 31, etc, "nd" for 2, 22, etc, "rd" for 3, 23, etc and "th" for pretty much everything else.

## StringExtensionMethods
`SplitCamelCase()` - Splits a camcel case string into separate words, eg "ThisIsMyString" gets converted into "This Is My String". Very useful for working with enums.
