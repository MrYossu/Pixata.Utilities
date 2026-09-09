# String extension methods

>Part of [Pixata.Extensions](Readme.md)

Extension methods for working with strings, found in `StringExtensionMethods`.

## Joining collections into strings

`JoinStr()` - Does the same as `string.Join`, but as an extension method, so it can be chained. Sample usage...

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

## Splitting camel case

`SplitCamelCase()` - Splits a camel case string into separate words, eg "ThisIsMyString" gets converted into "This Is My String". Very useful for working with enums (although see below for variations of this method that work directly with enums). Takes an optional `bool` parameter that specifies whether the second and subsequent words in the returned string should be lower case. Default is true.

`SplitEnumCamelCase<T>()` - Splits an enum member name using camel case as the rule. For example, if you had an `enum` named `Animals`, and it had a member named `JimSpriggs`, then `Animals.JimSpriggs.SplitEnumCamelCase()` would return "Jim Spriggs". Takes an optional `bool` parameter as above.

`SplitEnumValueCamelCase<T>()` - Splits an enum member value (assumed to be an int) using camel case as the rule. Using the same `enum` as in the previous comment, if `JimSpriggs` had a value of 2, then `2.SplitEnumValueCamelCase<Animals>()` would return "Jim Spriggs". Takes an optional `bool` parameter as above.

See also [Enum helpers](Readme.Enums.md), which lists the members of an `enum` with their names already split.

## Working with multi-line strings

`FirstLine()` - Returns the first line of a multi-line string. Useful for getting the first line of someone's address.

`LastLine()` - Returns the last line of a multi-line string. Useful for getting the last line of someone's address.

`OtherLines()` - Returns all but the first line of a multi-line string. Useful for getting the second and subsequent line(s) of someone's address.

`ToHtml()` - Converts a string to a format suitable for display in HTML by adding `<p></p>` tags around paragraphs. Paragraphs are defined as blocks of text separated by one or more blank lines.

## Cleaning up strings

`RemoveDiacritics()` - Removes diacritics (such as é, ü and ñ) from letters, replacing them with their (hopefully) nearest Latin equivalents. Note that for boring technical reasons, the returned string was lowercase in earlier versions of this package. Starting with version 1.27.0, case is preserved.

`ToTitleCase()` - Replaces the first character of each word with upper case, eg "This is an example" becomes "This Is An Example". If the parameter is null, an empty string is returned.

`Sanitise()` - Sanitises a string to be safe for use as a file name. Invalid characters are replaced, and sequences of invalid characters are condensed.

`ToUrlString()` - Converts a string into a URL-friendly slug. Diacritics are removed, the string is lower-cased, spaces become hyphens, and anything that isn't a letter, digit or hyphen is dropped. For example, "This is an example!" becomes "this-is-an-example".

>Note that reserved URL characters (`?`, `&` and friends) are **removed** rather than encoded, so this is for building readable slugs, not for escaping a value you need to read back.

`TruncateAtWordBoundary()` - Truncates a string to a specified maximum length, ensuring that it does not cut off in the middle of a word. If truncation occurs, an optional suffix (eg "...") can be appended to indicate that the string has been shortened. By default, ellipses ("...") are added, but this can be suppressed by supplying `false` as a parameter.

## UK postcodes

`UkPostcodeValid()` - Checks if the string has the format of a valid UK postcode. Note that this only checks the format, it does not check that the postcode actually exists.

`FormatUkPostcode()` - Formats a string as a UK postcode, ie upper case, and with a space between the major and minor parts. If the string is not in a valid postcode format, it is returned unchanged.
