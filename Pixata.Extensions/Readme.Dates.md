# Date extension methods

>Part of [Pixata.Extensions](Readme.md)

Extension methods for working with dates, found in `DateExtensionMethods`.

`ToPrettyString()` - Formats a date as "12th January 2021". This relies on the `OrdinalSuffix()` method in [`NumberExtensionMethods`](Readme.Numbers.md). Works for both non-nullable and nullable `DateTime` variables, returning an empty string if the value is null.

`StartOfWeek()` - Returns a `DateTime` (date part only, no time) representing the start of the week for the date passed as a parameter. Takes a `DayOfWeek` parameter to specify which day you consider the week to start.

`StartOfMonth()` - Gives you a `DateTime` that represents the first day of the month at 00:00:00 for the month containing the date you pass in.

`EndOfMonth()` - Gives you a `DateTime` that represents the last millisecond of the month containing the date you pass in.

`EndOfDay()` - Gives you a `DateTime` that represents the last millisecond of the day containing the date you pass in (23:59:59.999).

`IsWithin()` - True if the date is within the range supplied.

`DateRangeToString()` - Formats a date/time range in a human-friendly way, omitting redundant information. Overloads accept nullable and non-nullable from/to dates and an optional `showTime` boolean to include times. The formatting omits repeated parts when possible (same day, same month, same year) and produces concise strings.
