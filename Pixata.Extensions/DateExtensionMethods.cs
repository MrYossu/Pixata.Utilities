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

    // Reduced to a one-liner from https://stackoverflow.com/a/38064/706346
    /// <summary>
    /// Returns a DateTime representing the start of the week (as defined by the startOfWeek parameter) for the given DateTime
    /// </summary>
    /// <param name="dt">The DateTime that falls in the week whose start you want</param>
    /// <param name="startOfWeek">A DayOfWeek that specifies which day you consider the week to start</param>
    /// <returns>A DateTime representing the start of the week for the given DateTime</returns>
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek) =>
      dt.AddDays(-1 * ((7 + (dt.DayOfWeek - startOfWeek)) % 7)).Date;

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

    /// <summary>
    /// Formats a date/time range in a human-friendly way, omitting redundant information
    /// </summary>
    /// <param name="fromDate">The beginning of the range. May be null</param>
    /// <param name="toDate">The end of the range. May be null</param>
    /// <returns>A concise summary of the range, eg "From 29th Sept 25 00:00", "To 29th Sept 25 00:00", "28th Sept 00:00 - 29th Sept 25 12:00" or "29th Sept 25 00:00-12:00"</returns>
    public static string DateRangeToString(DateTime? fromDate, DateTime? toDate) {
      switch (fromDate) {
        case null when toDate == null:
          return "";
        case null:
          return $"To {FormatDateTime(toDate.Value)}";
      }

      if (toDate == null) {
        return $"From {FormatDateTime(fromDate.Value)}";
      }

      DateTime from = fromDate.Value;
      DateTime to = toDate.Value;

      // Same year, month, and day - only show date once
      if (from.Date == to.Date) {
        return $"{FormatDate(from)} {from:HH:mm}-{to:HH:mm}";
      }

      // Same year and month - show each date with time
      if (from.Year == to.Year && from.Month == to.Month) {
        return $"{GetDayWithSuffix(from.Day)} {from:MMM} {from:HH:mm} - {GetDayWithSuffix(to.Day)} {to:MMM yy} {to:HH:mm}";
      }

      // Same year - omit year from first date
      if (from.Year == to.Year) {
        return $"{FormatDayMonth(from)} {from:HH:mm} - {FormatDate(to)} {to:HH:mm}";
      }

      // Different years - show full dates
      return $"{FormatDateTime(from)} - {FormatDateTime(to)}";
    }

    private static string FormatDateTime(DateTime dt) =>
      $"{FormatDate(dt)} {dt:HH:mm}";

    private static string FormatDate(DateTime dt) =>
      $"{GetDayWithSuffix(dt.Day)} {dt:MMM yy}";

    private static string FormatDayMonth(DateTime dt) =>
      $"{GetDayWithSuffix(dt.Day)} {dt:MMM}";

    private static string GetDayWithSuffix(int day) =>
      day switch {
        1 or 21 or 31 => $"{day}st",
        2 or 22 => $"{day}nd",
        3 or 23 => $"{day}rd",
        _ => $"{day}th"
      };
  }