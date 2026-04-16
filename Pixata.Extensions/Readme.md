# Pixata.Extensions [![Pixata.Extensions Nuget package](https://img.shields.io/nuget/v/Pixata.Extensions)](https://www.nuget.org/packages/Pixata.Extensions/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Extensions/VroumVroum.png "Pixata") 

Some useful utility classes and methods I've developed over the past few years. I have only put in a few so far, and the test project is woefully empty, but hopefully that will change over time.

A [Nuget package](https://www.nuget.org/packages/Pixata.Extensions/) is available for this project.

Here is a brief description of the methods in the classes so far (alphabetically by class name):

## CollectionExtensionMethods
`ToObservableCollection<T>()` - Converts any collection that implements IEnumerable<T> into an ObservableCollection<T>. Provides a neater syntax than passing the collection to the `ObservableCollection`'s constructor.

`ToObservableCollectionAsync<T>()` - An async version of `ToObservableCollection()`.

`RemoveWhere<T>()` - Removes items from an ObservableCollection based on a predicate. Mimics the `List<T>.RemoveAll()` method that doesn't exist for ObservableCollections

`ForEach<T>()` - Allows you to use `ForEach` on any collection that implements IEnumerable<T>, as opposed to the similarly named method built in to the .NET framework that requires you to cast the collection to a `List<T>` first.

`Flatten<T>()` - Enables you to flatten a hierarchical collection.

## DateExtensionMethods
`ToPrettyString()` - Formats a date as "12th January 2021". This relies on the `OrdinalSuffix()` method in `NumberExtensionMethods`. Works for both non-nullable and nullable `DateTime` variables, returning an empty string if the value is null.

`StartOfWeek()` - Returns a `DateTime` (date part only, no time) representing the start of the week for the date passed as a parameter. Takes a `DayOfWeek` parameter to specify which day you consider the week to start.

`StartOfMonth()` - Gives you a `DateTime` that represents the first day of the month at 00:00:00 for the month containing the date you pass in.

`EndOfMonth()` - Gives you a `DateTime` that represents the last millisecond of the month containing the date you pass in.

`EndOfDay()` - Gives you a `DateTime` that represents the last millisecond of the day containing the date you pass in (23:59:59.999).

`IsWithin()` - True if the date is within the range supplied.

`DateRangeToString()` - Formats a date/time range in a human-friendly way, omitting redundant information. Overloads accept nullable and non-nullable from/to dates and an optional `showTime` boolean to include times. The formatting omits repeated parts when possible (same day, same month, same year) and produces concise strings.

## EnumHelper
`GetValues<T>()` - Enumerates the values of an `enum` and returns a list of enum entries with their integer Ids and value names as string. Names are split by camel case, eg "MyEnumValue" becomes "My enum value".

## ExceptionExtensions
`MessageStack()` - Returns the exception messages all the way down the InnerException stack. There is an overload that accepts a `separator` string; the default uses `Environment.NewLine`. `MessageStack` includes stack traces in the returned text.

`Messages()` - Similar to `MessageStack` but returns only the messages (no stack traces). An overload accepts a `separator` string; the default is `Environment.NewLine`.

`InnerType()` - Returns the type name of the innermost exception.

## MemoryCacheExtensions
If you aren't caching your data, then I strongly recommend you watch [this short video](https://www.youtube.com/watch?v=Q49SZlxskjs) before reading on! It's by no means the only video out there, but it's short, clear and shows the benefits of caching in a very compelling way.

For people developing big sites, who are probably using a distributed cache, then the .NET `HybridCache` is ideal. However, for most of us, who develop single-instance apps, this is not necessary, and can be a suboptimal choice due to the extra implicit behaviour you must understand, less obvious performance characteristics and a framework-level dependency. For most of us, the `IMemoryCache` is a better choice.

The one major advantage that the hybrid cache has even for smaller apps is that it has a built-in mechanism to avoid cache stampede protection, meaning that if multiple requests come in for the same uncached item, only one of them will trigger the expensive data retrieval operation, and the others will wait for the result to be cached. If you use a memory cache, you need to implemnent this yourself, which is not hard, but adds extra noise to your code.

The class here adds a `GetOrCreateSafe()` extension method to `IMemoryCache` to provide a simple way to add cache stampede protection, with no more code than you would have anyway. It is intended to replace the regular `GetOrCreate()` method. Basic usage is as follows...

```csharp
Product? product = await cache.GetOrCreateSafe($"product-{id}",
                                               async _ => await dbContext.Products.SingleOrDefault(p => p.Id == id),
                                               TimeSpan.FromMinutes(10));
```

In reality, you should store your cache keys in a centralised place, and not hard-code them like this, but this was kept simple to show the syntax.

The lambda includes a cancellation token (which is ignored in the above example), which can be passed through if needed...

```csharp
Product? product = await cache.GetOrCreateSafe($"product-{id}",
                                               async ct => await repository.GetByIdAsync(id, ct),
                                               TimeSpan.FromMinutes(5));
```

Instead of caching database entities (which can be large, and are not always ideal candidates for caching), you can convert to DTos and cache those...

```csharp
ProductDto? dto = await cache.GetOrCreateSafe($"product-{id}",
                                              async ct => {
                                                Product entity = await repository.GetByIdAsync(id, ct);
                                                return new ProductDto {
                                                  Id = entity.Id,
                                                  Name = entity.Name,
                                                  Price = entity.Price
                                                };
                                              },
                                              TimeSpan.FromMinutes(10)
```

## NumberExtensionMethods
`OrdinalSuffix()` - Returns the ordinal suffix, eg "st" for 1, 21, 31, etc, "nd" for 2, 22, etc, "rd" for 3, 23, etc and "th" for most other numbers. Has an optional parameter that controls whether the returned string includes the number itself (e.g. "1st") or only the suffix (e.g. "st").

`DoubleToFraction()` - Converts a double to a 2-tuple of its improper fractional representation, eg 3.5 is converted to (7, 2) meaning 7/2. Slightly modified from https://stackoverflow.com/a/32903747/706346.

`DoubleToProperFraction()` - Returns a 3-tuple representing a proper fraction, eg 3.5 is converted to (3, 1, 2) meaning 3 1/2. Note: for whole numbers the tuple may look like (5, 0, 1).

`DoubleToProperFractionString()` - Returns a cleaned string representation of `DoubleToProperFraction()`, e.g. "2/3" or "3 2/7".

`ToPercentageString()` - Converts a number into the string representation of it as a percentage of a maximum. There are overloads for combinations of `int` and `double` parameters; the method returns a rounded percentage string and accepts an optional `digits` parameter to control decimal places.

`ToDurationString()` - Converts a number of seconds to a human-readable duration string. For example, 125 will be converted to "2 minutes 5 seconds".

`S()` - Returns an empty string when the input is 1, otherwise returns "s". Useful for simple pluralisation in formatted strings (e.g. `"{n} item{n.S()}"`).

`ToFileSizeString()` - Converts a byte count to a human-readable file size string (bytes, Kb, Mb, Gb, etc.) with configurable precision.

## StringExtensionMethods
`JoinStr()` - Does the same as string.Join, but as an extension method, so it can be chained. Sample usage...
```csharp
List<int> nums = [1, 2, 3];
string result = nums.JoinStr(); // result is "1, 2, 3"
string result = nums.JoinStr(", ", n => $"Number {n}"); // result is "Number 1, Number 2, Number 3"
```

Takes an optional separator string (default is ", ").

`JoinStrAnd()` - Joins the elements of a sequence into a single string, with "and" before the last element. Takes an optional function to convert each element to a string. Defaults to the element's `ToString()` method. Sample usage...
```csharp
List<int> nums = [1, 2, 3];
string result = nums.JoinStrAnd(); // result is "1, 2 and 3"
string result = nums.JoinStrAnd(", ", n => $"Number {n}"); // result is "Number 1, Number 2 and Number 3"
```

`SplitCamelCase()` - Splits a camel case string into separate words, eg "ThisIsMyString" gets converted into "This Is My String". Very useful for working with enums (although see below for variations of this method that work directly with enums). Takes an option `bool` parameter that specifies whether the second and subsequent words in the returned string should be lower case. Default is true.

`SplitEnumCamelCase<T>()` - Splits an enum member name using camel case as the rule. For example, if you had an `enum` named `Aminals`, and it had a member named `JimSpriggs`, then `Animals.JimSpriggs.SplitEnumCamelCase()` would return "Jim Spriggs". Takes an option `bool` parameter  as above.

`SplitEnumValueCamelCase<T>()` - Splits an enum member value (assumed to be an int) using camel case as the rule. Using the same `enum` as in the previous comment, if `JimSpriggs` had a value of 2, then `2.SplitEnumValueCamelCase<Animals>()` would return "Jim Spriggs". Takes an option `bool` parameter  as above.

`FirstLine()` - Returns the first line of a multi-line string. Useful for getting the first line of someone's address

`LastLine()` - Returns the last line of a multi-line string. Useful for getting the last line of someone's address

`OtherLines()` - Returns all but the first line of a multi-line string. Useful for getting the second and subsequent line(s) of someone's address

`RemoveDiacritics()` - Removes diacritics (such as ð, â and ý) from letters, replacing them with their (hopefully) nearest Latin equivalents. Note that for boring technical reasons, the returned string was lowercase in earlier versions of this package. Starting with version 1.27.0, case is preserved.

`ToTitleCase` - Replaces the first character of each word with upper case e.g. This is an example - This Is An Example. If the parameter is null, an empty string is returned

`Sanitise` - Sanitises a string to be safe for use as a file name. Invalid characters are replaced, and sequences of invalid characters are condensed.