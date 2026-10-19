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

```csharp
(new DateTime(2026, 11, 2), new DateTime(2026, 11, 30)).DateRangeToString();       // 2nd-30th Nov 26
(new DateTime(2026, 11, 2), new DateTime(2027, 1, 10)).DateRangeToString();        // 2nd Nov 26 - 10th Jan 27
```

As of v2.22.0, it also works on a tuple of `DateOnly` values (nullable or not), and there's a long format, with full month names and "to" between the dates, which suits text that will be read as a sentence, such as an email...

```csharp
(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 30)).DateRangeToString();                        // 2nd-30th Nov 26
(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 30)).DateRangeToString(DateRangeFormat.Long);    // 2 to 30 November 2026
(new DateOnly(2026, 11, 2), new DateOnly(2027, 1, 10)).DateRangeToString(DateRangeFormat.Long);     // 2 November 2026 to 10 January 2027
(start, (DateOnly?)null).DateRangeToString(DateRangeFormat.Long);                                   // From 2 November 2026
```

The `DateTime` overloads take the format too, before `showTime`, eg `(from, to).DateRangeToString(DateRangeFormat.Long, true)` gives "2 November 2026 09:30 to 16:00" for two times on the same day. A `DateOnly` has no time, so its overloads don't have `showTime`. The month names come from the current culture.
