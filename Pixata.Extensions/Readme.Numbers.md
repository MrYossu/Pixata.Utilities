# Number extension methods

>Part of [Pixata.Extensions](Readme.md)

Extension methods for working with numbers, found in `NumberExtensionMethods`.

## Formatting numbers for display

`OrdinalSuffix()` - Returns the ordinal suffix, eg "st" for 1, 21, 31, etc, "nd" for 2, 22, etc, "rd" for 3, 23, etc and "th" for most other numbers. Has an optional parameter that controls whether the returned string includes the number itself (eg "1st") or only the suffix (eg "st").

`ToPercentageString()` - Converts a number into the string representation of it as a percentage of a maximum. There are overloads for combinations of `int` and `double` parameters; the method returns a rounded percentage string and accepts an optional `digits` parameter to control decimal places.

`ToDurationString()` - Converts a number of seconds to a human-readable duration string. For example, 125 will be converted to "2 minutes 5 seconds".

`ToFileSizeString()` - Converts a byte count to a human-readable file size string (bytes, Kb, Mb, Gb, etc) with configurable precision.

`S()` - Returns an empty string when the input is 1, otherwise returns "s". Useful for simple pluralisation in formatted strings, eg `$"{n} item{n.S()}"`.

`ToHebrew()` - Converts an integer to the equivalent Hebrew numeral, eg 15 becomes "טו״" and 21 becomes "כא״". The geresh and gershayim marks are added for you.

>Currently this only handles numbers from 1 to 100. Anything outside that range throws an `ArgumentOutOfRangeException`.

## Fractions

`DoubleToFraction()` - Converts a double to a 2-tuple of its improper fractional representation, eg 3.5 is converted to (7, 2), meaning 7/2. Slightly modified from https://stackoverflow.com/a/32903747/706346.

`DoubleToProperFraction()` - Returns a 3-tuple representing a proper fraction, eg 3.5 is converted to (3, 1, 2), meaning 3 1/2. Note that for whole numbers the tuple may look like (5, 0, 1).

`DoubleToProperFractionString()` - Returns a cleaned string representation of `DoubleToProperFraction()`, eg "2/3" or "3 2/7".

All three take an `accuracy` parameter, which must be greater than 0 and less than 1. Where the number has no neat fractional representation, this controls how hard the method tries to find one.

## Miscellaneous

`NewId()` - Returns a new negative integer ID that is not in the list of IDs passed in. This is useful for generating temporary IDs for new items that have not yet been saved to a database and therefore don't have a real ID yet.
