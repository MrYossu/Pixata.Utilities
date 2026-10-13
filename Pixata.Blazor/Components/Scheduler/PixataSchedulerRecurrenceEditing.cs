using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// A day in a monthly or yearly rule such as "the second Tuesday" or "the last weekday". <see cref="Ordinal"/> is 1 to 4,
/// or -1 for the last, and <see cref="Kind"/> is a day code (<c>MO</c>, <c>TU</c>...) or <see cref="PixataSchedulerRecurrenceEditing.AnyDay"/>,
/// <see cref="PixataSchedulerRecurrenceEditing.Weekday"/> or <see cref="PixataSchedulerRecurrenceEditing.WeekendDay"/>
/// </summary>
internal readonly record struct PixataRecurrenceOrdinalDay(int Ordinal, string Kind);

/// <summary>
/// The changes the recurrence editor makes to a rule, and the plain-language description of a rule. Kept apart from the
/// component so it can be unit tested
/// </summary>
internal static class PixataSchedulerRecurrenceEditing {
  public const string AnyDay = "day";
  public const string Weekday = "weekday";
  public const string WeekendDay = "weekend";

  private static readonly DayOfWeek[] AllDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
  private static readonly DayOfWeek[] Weekdays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
  private static readonly DayOfWeek[] WeekendDays = [DayOfWeek.Saturday, DayOfWeek.Sunday];

  /// <summary>
  /// A new rule with the given frequency, set up to repeat on the day the series starts, and keeping the interval and end
  /// of the previous rule (if any)
  /// </summary>
  public static PixataRecurrenceRule Create(PixataRecurrenceFrequency frequency, DateTime start, PixataRecurrenceRule? previous) {
    PixataRecurrenceRule rule = new() {
      Frequency = frequency,
      Interval = previous?.Interval ?? 1,
      Count = previous?.Count,
      Until = previous?.Until,
      WeekStart = previous?.WeekStart
    };
    switch (frequency) {
      case PixataRecurrenceFrequency.Weekly:
        rule.ByDay = [new(start.DayOfWeek)];
        break;
      case PixataRecurrenceFrequency.Monthly:
        rule.ByMonthDay = [start.Day];
        break;
      case PixataRecurrenceFrequency.Yearly:
        rule.ByMonth = [start.Month];
        rule.ByMonthDay = [start.Day];
        break;
    }
    return rule;
  }

  /// <summary>
  /// True if a monthly or yearly rule repeats on a day of the month (eg the 14th) rather than an ordinal day (eg the second
  /// Tuesday)
  /// </summary>
  public static bool UsesMonthDay(PixataRecurrenceRule rule) =>
    OrdinalDay(rule) is null;

  /// <summary>
  /// The ordinal day a monthly or yearly rule repeats on, or null if it repeats on a day of the month
  /// </summary>
  public static PixataRecurrenceOrdinalDay? OrdinalDay(PixataRecurrenceRule rule) {
    if (rule.ByMonthDay.Count > 0 || rule.ByDay.Count == 0) {
      return null;
    }
    if (rule.ByDay.Count == 1 && rule.ByDay[0].Offset != 0) {
      return new(rule.ByDay[0].Offset, PixataRecurrenceDay.Code(rule.ByDay[0].Day));
    }
    if (rule.BySetPosition.Count > 0 && rule.ByDay.All(d => d.Offset == 0)) {
      HashSet<DayOfWeek> days = rule.ByDay.Select(d => d.Day).ToHashSet();
      string kind = days.SetEquals(AllDays) ? AnyDay
        : days.SetEquals(Weekdays) ? Weekday
        : days.SetEquals(WeekendDays) ? WeekendDay
        : days.Count == 1 ? PixataRecurrenceDay.Code(days.First())
        : AnyDay;
      return new(rule.BySetPosition[0], kind);
    }
    return null;
  }

  /// <summary>
  /// Makes a monthly or yearly rule repeat on a day of the month
  /// </summary>
  public static void SetMonthDay(PixataRecurrenceRule rule, int day) {
    rule.ByMonthDay = [Math.Clamp(day, 1, 31)];
    rule.ByDay = [];
    rule.BySetPosition = [];
  }

  /// <summary>
  /// Makes a monthly or yearly rule repeat on an ordinal day, eg the second Tuesday (<c>BYDAY=2TU</c>) or the last weekday
  /// (<c>BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1</c>)
  /// </summary>
  public static void SetOrdinalDay(PixataRecurrenceRule rule, PixataRecurrenceOrdinalDay day) {
    rule.ByMonthDay = [];
    DayOfWeek[]? days = day.Kind switch {
      AnyDay => AllDays,
      Weekday => Weekdays,
      WeekendDay => WeekendDays,
      _ => null
    };
    if (days is null) {
      rule.ByDay = [new(PixataRecurrenceDay.Parse(day.Kind).Day, day.Ordinal)];
      rule.BySetPosition = [];
    } else {
      rule.ByDay = days.Select(d => new PixataRecurrenceDay(d)).ToList();
      rule.BySetPosition = [day.Ordinal];
    }
  }

  /// <summary>
  /// Adds or removes a day of a weekly rule, keeping at least one
  /// </summary>
  public static void ToggleDay(PixataRecurrenceRule rule, DayOfWeek day) {
    if (rule.ByDay.Any(d => d.Day == day)) {
      if (rule.ByDay.Count > 1) {
        rule.ByDay = rule.ByDay.Where(d => d.Day != day).ToList();
      }
    } else {
      rule.ByDay = [.. rule.ByDay, new(day)];
      rule.ByDay = rule.ByDay.OrderBy(d => ((int)d.Day + 6) % 7).ToList();
    }
  }

  /// <summary>
  /// Ends the rule after a number of occurrences
  /// </summary>
  public static void EndAfter(PixataRecurrenceRule rule, int count) {
    rule.Until = null;
    rule.Count = Math.Max(1, count);
  }

  /// <summary>
  /// Ends the rule on a date (the last occurrence can be on that date)
  /// </summary>
  public static void EndOn(PixataRecurrenceRule rule, DateTime date) {
    rule.Count = null;
    rule.Until = date.Date.AddDays(1).AddSeconds(-1);
  }

  /// <summary>
  /// Makes the rule repeat for ever
  /// </summary>
  public static void EndNever(PixataRecurrenceRule rule) {
    rule.Count = null;
    rule.Until = null;
  }

  /// <summary>
  /// The rule in words, eg "Every 2 weeks on Monday, Wednesday, until 31 December 2026"
  /// </summary>
  public static string Describe(PixataRecurrenceRule rule, CultureInfo culture, PixataSchedulerMessages text) {
    DateTimeFormatInfo format = culture.DateTimeFormat;
    List<string> parts = [
      (rule.Frequency, rule.Interval) switch {
        (PixataRecurrenceFrequency.Daily, <= 1) => text.EveryDay,
        (PixataRecurrenceFrequency.Weekly, <= 1) => text.EveryWeek,
        (PixataRecurrenceFrequency.Monthly, <= 1) => text.EveryMonth,
        (PixataRecurrenceFrequency.Yearly, <= 1) => text.EveryYear,
        (PixataRecurrenceFrequency.Daily, _) => string.Format(culture, text.EveryDaysFormat, rule.Interval),
        (PixataRecurrenceFrequency.Weekly, _) => string.Format(culture, text.EveryWeeksFormat, rule.Interval),
        (PixataRecurrenceFrequency.Monthly, _) => string.Format(culture, text.EveryMonthsFormat, rule.Interval),
        _ => string.Format(culture, text.EveryYearsFormat, rule.Interval)
      }
    ];
    if (rule.ByMonth.Count > 0) {
      parts.Add(string.Format(culture, text.InMonthsFormat, string.Join(", ", rule.ByMonth.Select(m => format.GetMonthName(m)))));
    }
    if (rule.ByMonthDay.Count > 0) {
      parts.Add(string.Format(culture, text.OnMonthDayFormat, string.Join(", ", rule.ByMonthDay.Select(d => d.ToString(culture)))));
    }
    if (rule.Frequency is PixataRecurrenceFrequency.Monthly or PixataRecurrenceFrequency.Yearly && OrdinalDay(rule) is { } ordinal) {
      string kind = ordinal.Kind switch {
        AnyDay => text.AnyDay,
        Weekday => text.Weekday,
        WeekendDay => text.WeekendDay,
        _ => format.GetDayName(PixataRecurrenceDay.Parse(ordinal.Kind).Day)
      };
      bool isDayName = ordinal.Kind is not (AnyDay or Weekday or WeekendDay);
      parts.Add(string.Format(culture, text.OnTheFormat, culture.TextInfo.ToLower(OrdinalText(ordinal.Ordinal, culture, text)),
        isDayName ? kind : culture.TextInfo.ToLower(kind)));
    } else if (rule.ByDay.Count > 0) {
      parts.Add(string.Format(culture, text.OnDaysFormat, string.Join(", ", rule.ByDay.Select(d => d.Offset == 0
        ? format.GetDayName(d.Day)
        : $"{culture.TextInfo.ToLower(OrdinalText(d.Offset, culture, text))} {format.GetDayName(d.Day)}"))));
    }
    if (rule.Count is { } count) {
      parts.Add(string.Format(culture, text.CountFormat, count));
    }
    if (rule.Until is { } until) {
      parts.Add(string.Format(culture, text.UntilFormat, until.ToString(format.LongDatePattern, culture)));
    }
    return string.Join(", ", parts);
  }

  /// <summary>
  /// The word for an ordinal: First, Second, Third, Fourth or Last (or the number for any other)
  /// </summary>
  public static string OrdinalText(int ordinal, CultureInfo culture, PixataSchedulerMessages text) =>
    ordinal switch {
      1 => text.First,
      2 => text.Second,
      3 => text.Third,
      4 => text.Fourth,
      -1 => text.Last,
      _ => ordinal.ToString(culture)
    };
}
