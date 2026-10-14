using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pixata.Blazor.Components;

/// <summary>
/// The scheduler's date maths: which days a view shows, where the previous and next buttons go, the time slots, and the
/// text for the toolbar. Kept apart from the component so it can be unit tested
/// </summary>
internal static class PixataSchedulerDates {
  /// <summary>
  /// The number of weeks the month view shows, so that its height doesn't change from month to month
  /// </summary>
  public const int MonthWeeks = 6;

  /// <summary>
  /// The first day of the week containing <paramref name="date"/>
  /// </summary>
  public static DateTime StartOfWeek(DateTime date, DayOfWeek firstDay) =>
    date.Date.AddDays(-(((int)date.DayOfWeek - (int)firstDay + 7) % 7));

  /// <summary>
  /// The days a view shows when its date is <paramref name="date"/>, as a start date and an exclusive end date
  /// </summary>
  public static (DateTime Start, DateTime End) VisibleRange(PixataSchedulerView view, DateTime date, int numberOfDays, DayOfWeek firstDay) {
    DateTime day = date.Date;
    switch (view) {
      case PixataSchedulerView.Week: {
        DateTime start = StartOfWeek(day, firstDay);
        return (start, start.AddDays(7));
      }
      case PixataSchedulerView.Month: {
        DateTime start = StartOfWeek(new(day.Year, day.Month, 1), firstDay);
        return (start, start.AddDays(MonthWeeks * 7));
      }
      case PixataSchedulerView.MultiDay:
      case PixataSchedulerView.Timeline:
      case PixataSchedulerView.Agenda:
        return (day, day.AddDays(Math.Max(1, numberOfDays)));
      default:
        return (day, day.AddDays(1));
    }
  }

  /// <summary>
  /// The date the previous (<paramref name="step"/> = -1) or next (+1) button moves to
  /// </summary>
  public static DateTime Navigate(PixataSchedulerView view, DateTime date, int numberOfDays, int step) =>
    view switch {
      PixataSchedulerView.Week => date.Date.AddDays(7 * step),
      PixataSchedulerView.Month => date.Date.AddMonths(step),
      PixataSchedulerView.MultiDay or PixataSchedulerView.Timeline or PixataSchedulerView.Agenda => date.Date.AddDays(Math.Max(1, numberOfDays) * step),
      _ => date.Date.AddDays(step)
    };

  /// <summary>
  /// The time of day a view's <c>StartTime</c> or <c>EndTime</c> means. Only the time part matters, and an end time of
  /// midnight (or one not after the start) means the end of the day
  /// </summary>
  public static (TimeSpan Start, TimeSpan End) DayWindow(DateTime start, DateTime end) {
    TimeSpan from = start.TimeOfDay;
    TimeSpan to = end.TimeOfDay;
    if (to <= from) {
      to = TimeSpan.FromDays(1);
    }
    return (from, to);
  }

  /// <summary>
  /// The start times of the slot divisions between <paramref name="start"/> and <paramref name="end"/>. A slot is
  /// <paramref name="slotDuration"/> minutes long and split into <paramref name="slotDivisions"/> divisions
  /// </summary>
  public static List<TimeSpan> Divisions(TimeSpan start, TimeSpan end, int slotDuration, int slotDivisions) {
    TimeSpan step = DivisionLength(slotDuration, slotDivisions);
    List<TimeSpan> result = [];
    for (TimeSpan time = start; time < end; time += step) {
      result.Add(time);
    }
    return result;
  }

  /// <summary>
  /// The length of one slot division
  /// </summary>
  public static TimeSpan DivisionLength(int slotDuration, int slotDivisions) =>
    TimeSpan.FromMinutes(Math.Max(1d, Math.Max(1, slotDuration) / (double)Math.Max(1, slotDivisions)));

  /// <summary>
  /// The days an all-day appointment covers, as a first day and an exclusive last day. An all-day appointment covers every
  /// day from the date it starts to the date it ends, so one from the 13th to the 16th covers the 13th, 14th, 15th and 16th,
  /// and one that starts and ends on the 14th covers just the 14th
  /// </summary>
  public static (DateTime First, DateTime End) DaySpan(DateTime start, DateTime end) {
    DateTime first = start.Date;
    DateTime last = end.Date < first ? first : end.Date;
    return (first, last.AddDays(1));
  }

  /// <summary>
  /// The days a timed appointment touches, as a first day and an exclusive last day. One that ends exactly at midnight
  /// doesn't touch the day it ends on
  /// </summary>
  public static (DateTime First, DateTime End) TimedDaySpan(DateTime start, DateTime end) {
    DateTime first = start.Date;
    DateTime last = end > start && end.TimeOfDay == TimeSpan.Zero ? end.Date : end.Date.AddDays(1);
    return (first, last <= first ? first.AddDays(1) : last);
  }

  /// <summary>
  /// True if the appointment goes in a view's all-day row rather than its time slots, ie it is marked as all day, or lasts
  /// at least a day
  /// </summary>
  public static bool IsLong(DateTime start, DateTime end, bool isAllDay) =>
    isAllDay || end - start >= TimeSpan.FromDays(1);

  /// <summary>
  /// The text for the toolbar's date range button, eg "October 14", "October 11 - October 17" or "October 2026". The years
  /// are only given for the month view, and when the range crosses into a new year
  /// </summary>
  public static string RangeText(PixataSchedulerView view, DateTime date, DateTime start, DateTime end, CultureInfo culture) {
    DateTimeFormatInfo format = culture.DateTimeFormat;
    DateTime last = end.AddDays(-1);
    if (view == PixataSchedulerView.Month) {
      // The invariant culture's pattern puts the year first ("2026 October"), which reads oddly
      string pattern = culture.Name == "" ? "MMMM yyyy" : format.YearMonthPattern;
      return date.ToString(pattern, culture);
    }
    string from = start.ToString(format.MonthDayPattern, culture);
    if (last <= start) {
      return from;
    }
    string to = last.ToString(format.MonthDayPattern, culture);
    return start.Year == last.Year
      ? $"{from} - {to}"
      : $"{from} {start.ToString("yyyy", culture)} - {to} {last.ToString("yyyy", culture)}";
  }

  /// <summary>
  /// A time formatted with the culture's short time pattern
  /// </summary>
  public static string Time(DateTime time, CultureInfo culture) =>
    time.ToString(culture.DateTimeFormat.ShortTimePattern, culture);

  /// <summary>
  /// The text for an appointment's times, eg "10:00 - 11:30"
  /// </summary>
  public static string TimeRange(DateTime start, DateTime end, CultureInfo culture) =>
    $"{Time(start, culture)} - {Time(end, culture)}";
}
