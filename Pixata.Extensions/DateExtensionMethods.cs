using System;

namespace Pixata.Extensions {
  public static class DateExtensionMethods {
    /// <summary>
    /// Formats a DateTime in a more readable way than the built-in methods, eg 31st December 2021
    /// </summary>
    /// <param name="date">The DateTime to be formatted</param>
    /// <param name="includeTime">Optional. Specifies whether or not the time is included</param>
    /// <returns>A string representation of the date in a pleasant form</returns>
    public static string ToPrettyString(this DateTime date, bool includeTime = false) =>
      date.Day.OrdinalSuffix() + date.ToString(" MMMM yyyy") + (includeTime ? " " + date.ToString("HH:mm") : "");

    /// <summary>
    /// Overload of ToPrettyString for nullable DateTime
    /// </summary>
    /// <param name="date">The DateTime to be formatted</param>
    /// <param name="includeTime">Optional. Specifies whether or not the time is included</param>
    /// <returns>A string representation of the date in a pleasant form, or the empty string if the date were null</returns>
    public static string ToPrettyString(this DateTime? date, bool includeTime = false) =>
      date == null
        ? ""
        : date.Value.ToPrettyString(includeTime);

    /// <summary>
    /// Returns true of the specified date is within the range given
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <param name="start">The start of the range</param>
    /// <param name="end">The end of the range</param>
    /// <returns>true is date is within the range, false otherwise</returns>
    public static bool IsWithin(this DateTime date, DateTime start, DateTime end) =>
      date >= start && date <= end;

    /// <summary>
    /// gets the last second of the day. Useful when you want to set a date range, and want the time of the end date to be 23:59:59.000
    /// </summary>
    /// <param name="d">A date in the desired month</param>
    /// <returns>A DateTime that represents the last millisecond of the day</returns>
    public static DateTime EndOfDay(this DateTime d) =>
      new DateTime(d.Year, d.Month, d.Day, 23, 59, 59).AddMilliseconds(999);

    /// <summary>
    /// Returns the first day of the given month
    /// </summary>
    /// <param name="d">A date in the desired month</param>
    /// <returns>the first day of the month</returns>
    public static DateTime StartOfMonth(this DateTime d) =>
      new(d.Year, d.Month, 1);

    /// <summary>
    /// Gets the last second of the month, ie 23:59:59 on the last day
    /// </summary>
    /// <param name="d">A date in the desired month</param>
    /// <returns>A DateTime that represents the last millisecond of the month</returns>
    public static DateTime EndOfMonth(this DateTime d) =>
      new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 23, 59, 59).AddMilliseconds(999);
  }
}