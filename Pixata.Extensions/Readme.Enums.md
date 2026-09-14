# Enum helpers

>Part of [Pixata.Extensions](Readme.md)

Helpers for working with `enum`s, found in `EnumHelper`.

`GetValues<T>()` - Enumerates the values of an `enum` and returns a list of enum entries with their integer Ids and value names as strings. Names are split by camel case, eg "MyEnumValue" becomes "My enum value".

This is handy for binding an `enum` to a dropdown without having to write the same projection in every app.

The camel case splitting is done by the methods in [`StringExtensionMethods`](Readme.Strings.md), which also has `SplitEnumCamelCase<T>()` and `SplitEnumValueCamelCase<T>()` for formatting a single `enum` member.
