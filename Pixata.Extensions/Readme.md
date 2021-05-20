# Pixata.Extensions [![Pixata.Extensions Nuget package](https://img.shields.io/nuget/v/Pixata.Extensions)](https://www.nuget.org/packages/Pixata.Extensions/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Extensions/Icon/Vroum%20Vroum.png "Pixata") 

Some useful utility classes and methods I've developed over the past few years. I have only put in a few so far, and the test project is woefully empty, but hopefully that will change over time.

A [Nuget package](https://www.nuget.org/packages/Pixata.Extensions/) is available for this project.

Here is a brief description of the methods in the classes so far...

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

## NumberExtensionMethods
`OrdinalSuffix()` - Returns the ordinal suffix, eg "st" for 1, 21, 31, etc, "nd" for 2, 22, etc, "rd" for 3, 23, etc and "th" for pretty much everything else. Has an optional parameter that allows you to specify if you want 1 to return "1st" (default) or just "st".

`DoubleToFraction()` - Converts a double to a 2-tuples of its improper fractional representation, eg 3.5 is converted to (7, 2), ie 7/2. Slightly modified from https://stackoverflow.com/a/32903747/706346

`DoubleToProperFraction()` - Similar to `RealToFraction()`, but returns a 3-tuple representing a proper fraction, eg 3.5 is converted to (3, 2, 1), meaning 3 1/2. Note that this method has a slight quirk, in that if you pass in a whole number, say 5, you will get (5, 0, 1) returned. technically this is correct, but it looks odd. If you are going to display this result, you would probably want to check for this and neaten the display. At some point I hope to add a `DoubleToProperFractionString()` method to handle this.

## StringExtensionMethods
`SplitCamelCase()` - Splits a camcel case string into separate words, eg "ThisIsMyString" gets converted into "This Is My String". Very useful for working with enums.

`RemoveDiacritics()` - Removes diacritics (such as ð, â and ý) from letters, replacing them with their (hopefully) nearest Latin equivalents. Note that for boring technical reasons, the returned string is lowercase.