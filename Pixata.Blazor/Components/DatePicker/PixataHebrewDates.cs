using System;
using System.Collections.Generic;
using System.Globalization;
using Pixata.Extensions;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataHebrewDatePicker{TValue}"/> shows in its text box: the Hebrew date, the Gregorian date, or both
/// </summary>
public enum PixataDateInputDisplay {
  Hebrew,
  Gregorian,
  Both
}

/// <summary>
/// Which shading <see cref="PixataHebrewDatePicker{TValue}"/> uses for a day that is both a bank holiday and a Jewish one
/// (and which comes first in its tooltip)
/// </summary>
public enum PixataBankHolidayClashPriority {
  HebrewFirst,
  BankHolidayFirst
}

/// <summary>
/// The kinds of day <see cref="PixataHebrewDatePicker{TValue}"/> can shade
/// </summary>
public enum PixataHebrewDayType {
  None,
  ShabbosOrYomTov,
  OtherNonWorkDay,
  BankHoliday
}

/// <summary>
/// Hebrew dates, and the days that <see cref="PixataHebrewDatePicker{TValue}"/> shades: Shabbos and Yom Tov (as kept outside
/// Israel), the other days that usually aren't worked (Chol Hamoed, Tisha B'Av and Purim), and the bank holidays of England
/// and Wales. Use them in a <see cref="PixataCalendar"/>'s or <see cref="PixataDatePicker{TValue}"/>'s <c>DayInfo</c> for a
/// picker of your own. Dates outside the range the Hebrew calendar supports (1583 to 2239) have no Hebrew date or holidays
/// </summary>
public static class PixataHebrewDates {
  private const char LeftToRightMark = '\u200E';
  private static readonly HebrewCalendar Calendar = new();

  /// <summary>
  /// The first date that has a Hebrew date
  /// </summary>
  public static DateTime MinSupportedDate =>
    Calendar.MinSupportedDateTime.Date;

  /// <summary>
  /// The last date that has a Hebrew date
  /// </summary>
  public static DateTime MaxSupportedDate =>
    Calendar.MaxSupportedDateTime.Date;

  /// <summary>
  /// Whether the date has a Hebrew date
  /// </summary>
  public static bool IsSupported(DateTime date) =>
    date >= Calendar.MinSupportedDateTime && date <= Calendar.MaxSupportedDateTime;

  /// <summary>
  /// The Hebrew year, month (counted from Tishrei, so Nissan is 7, or 8 in a leap year) and day of a date
  /// </summary>
  public static (int Year, int Month, int Day) ToHebrewDate(DateTime date) =>
    (Calendar.GetYear(date), Calendar.GetMonth(date), Calendar.GetDayOfMonth(date));

  /// <summary>
  /// The name of a Hebrew month, in Hebrew, such as תשרי. In a leap year, months 6 and 7 are אדר א and אדר ב
  /// </summary>
  public static string MonthName(int year, int month) {
    if (Calendar.GetMonthsInYear(year) == 13) {
      switch (month) {
        case 6:
          return "אדר א";
        case 7:
          return "אדר ב";
        case > 7:
          month--;
          break;
      }
    }
    return month switch {
      1 => "תשרי",
      2 => "חשון",
      3 => "כסלו",
      4 => "טבת",
      5 => "שבט",
      6 => "אדר",
      7 => "ניסן",
      8 => "אייר",
      9 => "סיון",
      10 => "תמוז",
      11 => "אב",
      12 => "אלול",
      _ => $"חודש {month}"
    };
  }

  /// <summary>
  /// The Hebrew date, such as <c>12 כסלו 5787</c>, with marks that keep the numbers in order in right-to-left text. Empty
  /// for a date the Hebrew calendar doesn't support
  /// </summary>
  public static string Format(DateTime date) {
    if (!IsSupported(date)) {
      return "";
    }
    (int year, int month, int day) = ToHebrewDate(date);
    return $"{LeftToRightMark}{day}{LeftToRightMark} {MonthName(year, month)} {LeftToRightMark}{year}{LeftToRightMark}";
  }

  /// <summary>
  /// The Hebrew day of the month in Hebrew letters, such as י״ב, as shown under the day in the picker's calendar
  /// </summary>
  public static string DayInLetters(DateTime date) =>
    IsSupported(date) ? Calendar.GetDayOfMonth(date).ToHebrew() : "";

  /// <summary>
  /// The Hebrew month or months that a Gregorian month spans, such as <c>כסלו 5787 / טבת 5787</c>
  /// </summary>
  public static string MonthsOf(int year, int month) {
    DateTime first = new(year, month, 1);
    DateTime last = first.AddMonths(1).AddDays(-1);
    if (!IsSupported(first) || !IsSupported(last)) {
      return "";
    }
    (int firstYear, int firstMonth, _) = ToHebrewDate(first);
    (int lastYear, int lastMonth, _) = ToHebrewDate(last);
    string start = $"{MonthName(firstYear, firstMonth)} {firstYear}";
    return firstYear == lastYear && firstMonth == lastMonth ? start : $"{start} / {MonthName(lastYear, lastMonth)} {lastYear}";
  }

  /// <summary>
  /// The name of the Yom Tov (as kept outside Israel) on a date, such as "Second day Pesach", or null
  /// </summary>
  public static string? YomTovName(DateTime date) {
    if (!IsSupported(date)) {
      return null;
    }
    (int year, int month, int day) = ToHebrewDate(date);
    int nissan = NissanMonth(year);
    if (month == nissan) {
      return day switch {
        15 => "First day Pesach",
        16 => "Second day Pesach",
        21 => "Seventh day Pesach",
        22 => "Eighth day Pesach",
        _ => null
      };
    }
    if (month == nissan + 2) {
      return day switch {
        6 => "First day Shavuos",
        7 => "Second day Shavuos",
        _ => null
      };
    }
    if (month == 1) {
      return day switch {
        1 => "First day Rosh Hashana",
        2 => "Second day Rosh Hashana",
        10 => "Yom Kippur",
        15 => "First day Succos",
        16 => "Second day Succos",
        22 => "Shemini Atzeres",
        23 => "Simchas Torah",
        _ => null
      };
    }
    return null;
  }

  /// <summary>
  /// The name of the other non-working day on a date (Chol Hamoed Pesach or Succos, Tisha B'Av, or Purim), or null. Tisha
  /// B'Av that falls on Shabbos is kept on the Sunday, which is named as well as the Shabbos
  /// </summary>
  public static string? OtherNonWorkDayName(DateTime date) {
    if (!IsSupported(date)) {
      return null;
    }
    (int year, int month, int day) = ToHebrewDate(date);
    int nissan = NissanMonth(year);
    if (month == nissan && day is >= 17 and <= 20) {
      return "Chol Hamoed Pesach";
    }
    if (month == 1 && day is >= 17 and <= 21) {
      return "Chol Hamoed Succos";
    }
    if (month == nissan + 4 && (day == 9 || (day == 10 && date.AddDays(-1).DayOfWeek == DayOfWeek.Saturday))) {
      return "Tisha B'Av";
    }
    int adar = Calendar.IsLeapYear(year) ? 7 : 6;
    return month == adar && day == 14 ? "Purim" : null;
  }

  /// <summary>
  /// The name of the bank holiday in England and Wales on a date, such as "Easter Monday", or null. A holiday that falls at a
  /// weekend is moved to the next weekday, as the substitute day. Holidays moved or added by proclamation (such as for a
  /// coronation) aren't included
  /// </summary>
  public static string? BankHolidayName(DateTime date) {
    date = date.Date;
    int year = date.Year;
    if (date == MondaySubstitute(new(year, 1, 1))) {
      return "New Year's Day";
    }
    DateTime easter = EasterSunday(year);
    if (date == easter.AddDays(-2)) {
      return "Good Friday";
    }
    if (date == easter.AddDays(1)) {
      return "Easter Monday";
    }
    if (date == FirstMonday(year, 5)) {
      return "Early May Bank Holiday";
    }
    if (date == LastMonday(year, 5)) {
      return "Spring Bank Holiday";
    }
    if (date == LastMonday(year, 8)) {
      return "Summer Bank Holiday";
    }
    (DateTime christmas, DateTime boxing) = ChristmasHolidays(year);
    return date == christmas ? "Christmas Day" : date == boxing ? "Boxing Day" : null;
  }

  /// <summary>
  /// Which kind of day a date is, for shading it, given which kinds are wanted and which wins when a bank holiday falls on a
  /// Jewish one
  /// </summary>
  public static PixataHebrewDayType DayType(DateTime date, bool shabbosOrYomTov, bool otherNonWorkDays, bool bankHolidays,
                                            PixataBankHolidayClashPriority clashPriority = PixataBankHolidayClashPriority.HebrewFirst) {
    bool isShabbosOrYomTov = shabbosOrYomTov && (date.DayOfWeek == DayOfWeek.Saturday || YomTovName(date) is not null);
    bool isOtherNonWorkDay = otherNonWorkDays && OtherNonWorkDayName(date) is not null;
    bool isBankHoliday = bankHolidays && BankHolidayName(date) is not null;
    if (isBankHoliday && clashPriority == PixataBankHolidayClashPriority.BankHolidayFirst) {
      return PixataHebrewDayType.BankHoliday;
    }
    return isShabbosOrYomTov ? PixataHebrewDayType.ShabbosOrYomTov
      : isOtherNonWorkDay ? PixataHebrewDayType.OtherNonWorkDay
      : isBankHoliday ? PixataHebrewDayType.BankHoliday
      : PixataHebrewDayType.None;
  }

  /// <summary>
  /// The names of the kinds of day wanted on a date, such as "Shabbos / Chol Hamoed Succos", joined with slashes, with the
  /// bank holiday first or last as <paramref name="clashPriority"/> says. Empty if there are none
  /// </summary>
  public static string DayNames(DateTime date, bool shabbosOrYomTov, bool otherNonWorkDays, bool bankHolidays,
                                PixataBankHolidayClashPriority clashPriority = PixataBankHolidayClashPriority.HebrewFirst) {
    List<string> names = [];
    if (shabbosOrYomTov && date.DayOfWeek == DayOfWeek.Saturday) {
      names.Add("Shabbos");
    }
    if (shabbosOrYomTov && YomTovName(date) is { } yomTov) {
      names.Add(yomTov);
    }
    if (otherNonWorkDays && OtherNonWorkDayName(date) is { } other) {
      names.Add(other);
    }
    if (bankHolidays && BankHolidayName(date) is { } bankHoliday) {
      names.Insert(clashPriority == PixataBankHolidayClashPriority.BankHolidayFirst ? 0 : names.Count, bankHoliday);
    }
    return string.Join(" / ", names);
  }

  private static int NissanMonth(int year) =>
    Calendar.GetMonthsInYear(year) == 13 ? 8 : 7;

  private static DateTime MondaySubstitute(DateTime date) =>
    date.DayOfWeek switch {
      DayOfWeek.Saturday => date.AddDays(2),
      DayOfWeek.Sunday => date.AddDays(1),
      _ => date
    };

  // Christmas Day and Boxing Day, each moved to the next weekday the other isn't on if it falls at a weekend
  private static (DateTime Christmas, DateTime Boxing) ChristmasHolidays(int year) {
    DateTime christmas = new(year, 12, 25);
    return christmas.DayOfWeek switch {
      DayOfWeek.Friday => (christmas, christmas.AddDays(3)),
      DayOfWeek.Saturday => (christmas.AddDays(2), christmas.AddDays(3)),
      DayOfWeek.Sunday => (christmas.AddDays(2), christmas.AddDays(1)),
      _ => (christmas, christmas.AddDays(1))
    };
  }

  private static DateTime FirstMonday(int year, int month) {
    DateTime date = new(year, month, 1);
    while (date.DayOfWeek != DayOfWeek.Monday) {
      date = date.AddDays(1);
    }
    return date;
  }

  private static DateTime LastMonday(int year, int month) {
    DateTime date = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
    while (date.DayOfWeek != DayOfWeek.Monday) {
      date = date.AddDays(-1);
    }
    return date;
  }

  // The Gregorian date of Easter Sunday, by the anonymous Gregorian algorithm
  private static DateTime EasterSunday(int year) {
    int a = year % 19;
    int b = year / 100;
    int c = year % 100;
    int d = b / 4;
    int e = b % 4;
    int f = (b + 8) / 25;
    int g = (b - f + 1) / 3;
    int h = (19 * a + b - d - g + 15) % 30;
    int i = c / 4;
    int k = c % 4;
    int l = (32 + 2 * e + 2 * i - h - k) % 7;
    int m = (a + 11 * h + 22 * l) / 451;
    int month = (h + l - 7 * m + 114) / 31;
    int day = (h + l - 7 * m + 114) % 31 + 1;
    return new(year, month, day);
  }
}
