using System;

namespace Pixata.Extensions {
  public static class DateExtensionMethods {
    public static string ToPrettyString(this DateTime date) =>
      date.Day + date.Day.OrdinalSuffix() + date.ToString(" MMMM yyyy");

    public static DateTime EndOfMonth(this DateTime d) =>
      new(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 23, 59, 59);
  }
}