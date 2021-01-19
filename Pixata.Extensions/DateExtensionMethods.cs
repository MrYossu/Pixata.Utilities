﻿using System;

namespace Pixata.Extensions {
  public static class DateExtensionMethods {
    /// <summary>
    /// Formats a DateTime in a more readable way than the built-in methods, eg 31st December 2021
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static string ToPrettyString(this DateTime date) =>
      date.Day.OrdinalSuffix() + date.ToString(" MMMM yyyy");

    /// <summary>
    /// Gets the last second of the month, ie 23:59:59 on th elast day
    /// </summary>
    /// <param name="d">A date in the desired month</param>
    /// <returns>A DateTime that represents the last second of the month</returns>
    public static DateTime EndOfMonth(this DateTime d) =>
      new(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 23, 59, 59);
  }
}