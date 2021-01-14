# Pixata.Utilities

I seem to keep adding the same utility classes to every project I work on, resulting in (almost) identical code spread across different projects.

I used to get around this by maintaining the utilities in a separate project, and referencing the DLL. However, apart from being soooo 1990s, it also has problems like VS locking the DLL, preventing you from building the utility project and so on.

So, I decided to take a leap into the mdoern world, and try publishing a Nuget package. This is the first time I've done this, so goodness knows what's going to happen.

I stated off with some Blazor components that I've been developing recently. These are offered as-is, with no guarantee that they will work for you. I'm still working on them, and cannot offer much in the way of support, although I'd be very happy to receive any feedback you have.

## Documentation
I know, I really ought to document them. I probably will at some point, but haven't had time yet. I've spent most of this afternoon trying to work out how to publish a Nuget package.

## Pixata.Extensions
Some useful utility classes and methods I've developed over the past few years. I have only put in a few so far, and the test project is woefully empty, but hopefully that will change over time. Here is a brief description of the methods in the classes so far...

### CollectionExtensionMethods
`ToObservableCollection<T>()` - Converts any collection that implements IEnumerable<T> into an ObservableCollection<T>. Provides a neater syntax than passing the collection to the `ObservableCollection`'s constructor.

`ForEach<T>()` - Allows you to use `ForEach` on any collection that implements IEnumerable<T>, as opposed to the similarly named method built in to the .NET framework that requires you to cast the collection to a `List<T>` first.

`Flatten<T>()` - Enables you to flatten a hierarchical collection.

### DateExtensionMethods
`ToPrettyString()` - Formats a date as "12th January 2021". This relies on the `OrdinalSuffix()` method in `IntExtensionMethods`.

`EndOfMonth()` - Gives you a `DateTime` that represents the last second of the month containing the date you pass in.

### EnumHelper
`GetValues<T>()` - Enumerate the values of an `enum`.

### ExceptionExtensions
`MessageStack()` - Returns the exception messages all the way down the InnerException stack.

`InnerType()` - Returns the type of the innermost exception.

### IntExtensionMethods
`OrdinalSuffix()` - Returns the ordinal suffix, eg "st" for 1, 21, 31, etc, "nd" for 2, 22, etc, "rd" for 3, 23, etc and "th" for pretty much everything else.

### StringExtensionMethods
`SplitCamelCase()` - Splits a camcel case string into separate words, eg "ThisIsMyString" gets converted into "This Is My String". Very useful for working with enums.

## Pixata.Blazor
I have [blogged about some Blazor components I've been writing](https://www.pixata.co.uk/tag/blazor/). This project contains the source for those components.

### Warning
The package relies on the Telerik.Blazor Nuget package. If you don't have a subscription with Telerik, you can a 30-day trial version from them. I make no apologies for relying on this package, I have been using Telerik components for many years, and wouldn't develop without them.

### Sample project
I have added a Blazor web project to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project.

## Blog
Random thoughts and notes that are sometimes useful, and sometimes related to these components can be found on [my blog](https://www.pixata.co.uk/).

