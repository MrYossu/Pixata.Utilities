using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// A day of the week in a recurrence rule's <c>BYDAY</c> part, optionally with an offset: <c>MO</c> is every Monday,
/// <c>2TU</c> the second Tuesday (of the month, or of the year for a yearly rule without <c>BYMONTH</c>) and <c>-1FR</c> the
/// last Friday
/// </summary>
/// <param name="Day">The day of the week</param>
/// <param name="Offset">0 for every such day, 1 for the first, 2 for the second, -1 for the last, -2 for the last but one, and so on</param>
public readonly record struct PixataRecurrenceDay(DayOfWeek Day, int Offset = 0) {
  private static readonly string[] Codes = ["SU", "MO", "TU", "WE", "TH", "FR", "SA"];

  /// <summary>
  /// The two-letter code for a day, eg <c>MO</c>
  /// </summary>
  public static string Code(DayOfWeek day) =>
    Codes[(int)day];

  /// <summary>
  /// Parses a <c>BYDAY</c> value such as <c>MO</c>, <c>2TU</c> or <c>-1FR</c>
  /// </summary>
  /// <exception cref="FormatException">The value isn't a valid day</exception>
  public static PixataRecurrenceDay Parse(string value) {
    string text = value.Trim().ToUpperInvariant();
    if (text.Length < 2) {
      throw new FormatException($"'{value}' is not a valid BYDAY value");
    }
    int index = Array.IndexOf(Codes, text[^2..]);
    if (index < 0) {
      throw new FormatException($"'{value}' is not a valid BYDAY value");
    }
    string offsetText = text[..^2];
    int offset = 0;
    if (offsetText != "" && (!int.TryParse(offsetText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out offset) || offset == 0 || Math.Abs(offset) > 53)) {
      throw new FormatException($"'{value}' is not a valid BYDAY value");
    }
    return new((DayOfWeek)index, offset);
  }

  public override string ToString() =>
    $"{(Offset == 0 ? "" : Offset.ToString(CultureInfo.InvariantCulture))}{Code(Day)}";
}

/// <summary>
/// A recurrence rule in the iCalendar (RFC 5545) <c>RRULE</c> format, eg <c>FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE;COUNT=10</c>.
/// Supports <c>FREQ</c> (<c>DAILY</c>, <c>WEEKLY</c>, <c>MONTHLY</c> and <c>YEARLY</c>), <c>INTERVAL</c>, <c>COUNT</c>,
/// <c>UNTIL</c>, <c>BYDAY</c> (including offsets such as <c>2TU</c> and <c>-1FR</c>), <c>BYMONTHDAY</c>, <c>BYMONTH</c>,
/// <c>BYSETPOS</c> and <c>WKST</c>. Every occurrence starts at the time of day of the series' first appointment
/// </summary>
public sealed class PixataRecurrenceRule {
  /// <summary>
  /// How often the rule repeats
  /// </summary>
  public PixataRecurrenceFrequency Frequency { get; set; } = PixataRecurrenceFrequency.Daily;

  /// <summary>
  /// Repeat every this many days, weeks, months or years. 1 by default
  /// </summary>
  public int Interval { get; set; } = 1;

  /// <summary>
  /// The number of occurrences, including the first, or null for no limit
  /// </summary>
  public int? Count { get; set; }

  /// <summary>
  /// The last moment an occurrence can start, or null for no limit
  /// </summary>
  public DateTime? Until {
    get => _until;
    set {
      // A value set in code is local time; only one read from a rule can be marked as UTC
      _until = value;
      _untilIsUtc = false;
    }
  }

  private DateTime? _until;

  /// <summary>
  /// The days of the week (with optional offsets) the rule repeats on
  /// </summary>
  public List<PixataRecurrenceDay> ByDay { get; set; } = [];

  /// <summary>
  /// The days of the month the rule repeats on: 1 to 31, or -1 (the last day) to -31
  /// </summary>
  public List<int> ByMonthDay { get; set; } = [];

  /// <summary>
  /// The months (1 to 12) the rule repeats in
  /// </summary>
  public List<int> ByMonth { get; set; } = [];

  /// <summary>
  /// Picks occurrences from each period's set, eg <c>BYSETPOS=-1</c> with <c>BYDAY=MO,TU,WE,TH,FR</c> on a monthly rule is
  /// the last weekday of the month
  /// </summary>
  public List<int> BySetPosition { get; set; } = [];

  /// <summary>
  /// The day a week starts on (<c>WKST</c>), which matters for weekly rules with an interval of more than one, or null when the
  /// rule doesn't say, in which case <see cref="GetOccurrences"/> uses the first day of the week it is given (the scheduler
  /// gives its own), or Monday
  /// </summary>
  public DayOfWeek? WeekStart { get; set; }

  private bool _untilIsUtc;

  /// <summary>
  /// Parses a rule, with or without a leading <c>RRULE:</c>
  /// </summary>
  /// <exception cref="FormatException">The rule is malformed, or uses a part that isn't supported</exception>
  public static PixataRecurrenceRule Parse(string rule) {
    if (string.IsNullOrWhiteSpace(rule)) {
      throw new FormatException("The recurrence rule is empty");
    }
    string text = rule.Trim();
    if (text.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase)) {
      text = text[6..];
    }
    PixataRecurrenceRule result = new();
    bool hasFrequency = false;
    foreach (string part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
      int equals = part.IndexOf('=');
      if (equals <= 0) {
        throw new FormatException($"'{part}' is not a valid part of a recurrence rule");
      }
      string name = part[..equals].Trim().ToUpperInvariant();
      string value = part[(equals + 1)..].Trim();
      switch (name) {
        case "FREQ":
          result.Frequency = value.ToUpperInvariant() switch {
            "DAILY" => PixataRecurrenceFrequency.Daily,
            "WEEKLY" => PixataRecurrenceFrequency.Weekly,
            "MONTHLY" => PixataRecurrenceFrequency.Monthly,
            "YEARLY" => PixataRecurrenceFrequency.Yearly,
            _ => throw new FormatException($"The frequency '{value}' is not supported. Use DAILY, WEEKLY, MONTHLY or YEARLY")
          };
          hasFrequency = true;
          break;
        case "INTERVAL":
          result.Interval = PositiveInt(value, name);
          break;
        case "COUNT":
          result.Count = PositiveInt(value, name);
          break;
        case "UNTIL":
          (result.Until, result._untilIsUtc) = ParseUntil(value);
          break;
        case "BYDAY":
          result.ByDay = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(PixataRecurrenceDay.Parse).ToList();
          break;
        case "BYMONTHDAY":
          result.ByMonthDay = IntList(value, name, 1, 31);
          break;
        case "BYMONTH":
          result.ByMonth = IntList(value, name, 1, 12);
          if (result.ByMonth.Any(m => m < 0)) {
            throw new FormatException("BYMONTH values must be between 1 and 12");
          }
          break;
        case "BYSETPOS":
          result.BySetPosition = IntList(value, name, 1, 366);
          break;
        case "WKST":
          result.WeekStart = PixataRecurrenceDay.Parse(value) is { Offset: 0 } day ? day.Day : throw new FormatException($"'{value}' is not a valid WKST value");
          break;
        default:
          if (!name.StartsWith("X-", StringComparison.Ordinal)) {
            throw new FormatException($"The recurrence rule part {name} is not supported");
          }
          break;
      }
    }
    if (!hasFrequency) {
      throw new FormatException("A recurrence rule must have a FREQ part");
    }
    if (result.Count is not null && result.Until is not null) {
      throw new FormatException("A recurrence rule can't have both COUNT and UNTIL");
    }
    return result;
  }

  /// <summary>
  /// Parses a rule, returning false (rather than throwing) if it is empty, malformed or uses a part that isn't supported
  /// </summary>
  public static bool TryParse(string? rule, out PixataRecurrenceRule? result) {
    result = null;
    if (string.IsNullOrWhiteSpace(rule)) {
      return false;
    }
    try {
      result = Parse(rule);
      return true;
    }
    catch (FormatException) {
      return false;
    }
  }

  /// <summary>
  /// A copy of the rule, so it can be changed without affecting this one
  /// </summary>
  public PixataRecurrenceRule Clone() =>
    new() {
      Frequency = Frequency,
      Interval = Interval,
      Count = Count,
      Until = Until,
      ByDay = [.. ByDay],
      ByMonthDay = [.. ByMonthDay],
      ByMonth = [.. ByMonth],
      BySetPosition = [.. BySetPosition],
      WeekStart = WeekStart,
      _untilIsUtc = _untilIsUtc
    };

  /// <summary>
  /// The rule in RFC 5545 format, eg <c>FREQ=MONTHLY;BYDAY=-1FR;COUNT=6</c>
  /// </summary>
  public override string ToString() {
    List<string> parts = [$"FREQ={Frequency.ToString().ToUpperInvariant()}"];
    if (Interval > 1) {
      parts.Add($"INTERVAL={Interval.ToString(CultureInfo.InvariantCulture)}");
    }
    if (ByDay.Count > 0) {
      parts.Add($"BYDAY={string.Join(",", ByDay)}");
    }
    if (ByMonthDay.Count > 0) {
      parts.Add($"BYMONTHDAY={string.Join(",", ByMonthDay.Select(d => d.ToString(CultureInfo.InvariantCulture)))}");
    }
    if (ByMonth.Count > 0) {
      parts.Add($"BYMONTH={string.Join(",", ByMonth.Select(m => m.ToString(CultureInfo.InvariantCulture)))}");
    }
    if (BySetPosition.Count > 0) {
      parts.Add($"BYSETPOS={string.Join(",", BySetPosition.Select(p => p.ToString(CultureInfo.InvariantCulture)))}");
    }
    if (Count is { } count) {
      parts.Add($"COUNT={count.ToString(CultureInfo.InvariantCulture)}");
    }
    if (Until is { } until) {
      parts.Add($"UNTIL={until.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture)}{(_untilIsUtc ? "Z" : "")}");
    }
    if (WeekStart is { } weekStart) {
      parts.Add($"WKST={PixataRecurrenceDay.Code(weekStart)}");
    }
    return string.Join(";", parts);
  }

  /// <summary>
  /// The start times of the occurrences of a series whose first appointment starts at <paramref name="seriesStart"/>, from
  /// <paramref name="from"/> (inclusive) to <paramref name="to"/> (exclusive). The first appointment is always an occurrence,
  /// and counts towards <see cref="Count"/>, even if it doesn't match the rule. Exceptions aren't taken into account here.
  /// A UTC <see cref="Until"/> (one that ends in <c>Z</c>) is converted to local time, unless <paramref name="seriesStart"/>
  /// is itself in UTC
  /// </summary>
  /// <param name="seriesStart">The start of the series' first appointment</param>
  /// <param name="from">The start of the range</param>
  /// <param name="to">The end of the range</param>
  /// <param name="firstDayOfWeek">The day weeks start on when the rule has no <c>WKST</c>. Monday when null</param>
  public List<DateTime> GetOccurrences(DateTime seriesStart, DateTime from, DateTime to, DayOfWeek? firstDayOfWeek = null) =>
    GetOccurrences(seriesStart, from, to, firstDayOfWeek, TimeZoneInfo.Local);

  internal List<DateTime> GetOccurrences(DateTime seriesStart, DateTime from, DateTime to, DayOfWeek? firstDayOfWeek, TimeZoneInfo localZone) {
    List<DateTime> result = [];
    if (to <= from || to <= seriesStart) {
      return result;
    }
    int interval = Math.Max(1, Interval);
    DayOfWeek weekStart = WeekStart ?? firstDayOfWeek ?? DayOfWeek.Monday;
    DateTime? until = UntilIn(seriesStart.Kind, localZone);
    TimeSpan time = seriesStart.TimeOfDay;
    DateTime period = PeriodStart(seriesStart.Date, weekStart);
    bool firstDone = false;
    int produced = 0;
    if (Count is null && from > seriesStart) {
      // Nothing is counted, so jump straight to (a whole number of intervals before) the period containing the range's start
      int periods = PeriodsBetween(period, PeriodStart(from.Date, weekStart));
      int skip = periods / interval * interval;
      if (skip > 0) {
        period = Advance(period, skip);
        firstDone = true;
      }
    }

    // Adds an occurrence, returning false once the series (or the range) has ended
    bool Add(DateTime occurrence) {
      if (until is { } last && occurrence > last) {
        return false;
      }
      if (Count is { } count && produced >= count) {
        return false;
      }
      if (occurrence >= to) {
        return false;
      }
      produced++;
      if (occurrence >= from) {
        result.Add(occurrence);
      }
      return true;
    }

    while (period < to) {
      foreach (DateTime date in Candidates(period, seriesStart)) {
        DateTime occurrence = date + time;
        if (occurrence < seriesStart) {
          continue;
        }
        if (!firstDone) {
          firstDone = true;
          if (occurrence != seriesStart && !Add(seriesStart)) {
            return result;
          }
        }
        if (!Add(occurrence)) {
          return result;
        }
      }
      if (period.Year >= 9999) {
        break;
      }
      period = Advance(period, interval);
    }
    if (!firstDone) {
      Add(seriesStart);
    }
    return result;
  }

  // The end of the series in the same terms as its start: a UTC UNTIL is converted to local time for a series in local time
  private DateTime? UntilIn(DateTimeKind seriesKind, TimeZoneInfo localZone) {
    if (Until is not { } until || !_untilIsUtc || seriesKind == DateTimeKind.Utc) {
      return Until;
    }
    try {
      return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(until, DateTimeKind.Utc), localZone);
    }
    catch (ArgumentException) {
      return until;
    }
  }

  private DateTime PeriodStart(DateTime date, DayOfWeek weekStart) =>
    Frequency switch {
      // (A week that would start before the first day of the calendar starts on that day instead)
      PixataRecurrenceFrequency.Weekly => date.AddDays(-Math.Min(((int)date.DayOfWeek - (int)weekStart + 7) % 7, (date - DateTime.MinValue).Days)),
      PixataRecurrenceFrequency.Monthly => new(date.Year, date.Month, 1),
      PixataRecurrenceFrequency.Yearly => new(date.Year, 1, 1),
      _ => date
    };

  private int PeriodsBetween(DateTime first, DateTime second) =>
    Frequency switch {
      PixataRecurrenceFrequency.Weekly => (int)((second - first).TotalDays / 7),
      PixataRecurrenceFrequency.Monthly => (second.Year - first.Year) * 12 + second.Month - first.Month,
      PixataRecurrenceFrequency.Yearly => second.Year - first.Year,
      _ => (int)(second - first).TotalDays
    };

  private DateTime Advance(DateTime period, int periods) {
    try {
      return Frequency switch {
        PixataRecurrenceFrequency.Weekly => period.AddDays(7 * periods),
        PixataRecurrenceFrequency.Monthly => period.AddMonths(periods),
        PixataRecurrenceFrequency.Yearly => period.AddYears(periods),
        _ => period.AddDays(periods)
      };
    }
    catch (ArgumentOutOfRangeException) {
      return DateTime.MaxValue.Date;
    }
  }

  // The dates in one period (a day, a week, a month or a year) that match the rule, in order
  private List<DateTime> Candidates(DateTime period, DateTime seriesStart) {
    List<DateTime> dates = Frequency switch {
      PixataRecurrenceFrequency.Daily => DailyCandidates(period),
      PixataRecurrenceFrequency.Weekly => WeeklyCandidates(period, seriesStart),
      PixataRecurrenceFrequency.Monthly => ByMonth.Count > 0 && !ByMonth.Contains(period.Month) ? [] : MonthCandidates(period.Year, period.Month, seriesStart),
      _ => YearlyCandidates(period.Year, seriesStart)
    };
    dates = dates.Distinct().OrderBy(d => d).ToList();
    if (BySetPosition.Count == 0 || dates.Count == 0) {
      return dates;
    }
    List<DateTime> picked = [];
    foreach (int position in BySetPosition) {
      int index = position > 0 ? position - 1 : dates.Count + position;
      if (index >= 0 && index < dates.Count) {
        picked.Add(dates[index]);
      }
    }
    return picked.Distinct().OrderBy(d => d).ToList();
  }

  private List<DateTime> DailyCandidates(DateTime day) {
    if (ByMonth.Count > 0 && !ByMonth.Contains(day.Month)) {
      return [];
    }
    if (ByMonthDay.Count > 0 && !ByMonthDay.Any(d => MonthDay(day.Year, day.Month, d) == day.Day)) {
      return [];
    }
    if (ByDay.Count > 0 && ByDay.All(d => d.Day != day.DayOfWeek)) {
      return [];
    }
    return [day];
  }

  private List<DateTime> WeeklyCandidates(DateTime weekStart, DateTime seriesStart) {
    List<DateTime> dates = [];
    for (int i = 0; i < 7; i++) {
      DateTime day = weekStart.AddDays(i);
      bool dayMatches = ByDay.Count > 0 ? ByDay.Any(d => d.Day == day.DayOfWeek) : day.DayOfWeek == seriesStart.DayOfWeek;
      if (dayMatches && (ByMonth.Count == 0 || ByMonth.Contains(day.Month))) {
        dates.Add(day);
      }
    }
    return dates;
  }

  // The matching dates in one month, with BYDAY offsets counted within the month
  private List<DateTime> MonthCandidates(int year, int month, DateTime seriesStart) {
    int daysInMonth = DateTime.DaysInMonth(year, month);
    List<DateTime> byMonthDay = ByMonthDay
      .Select(d => MonthDay(year, month, d))
      .Where(d => d > 0)
      .Select(d => new DateTime(year, month, d))
      .ToList();
    if (ByDay.Count == 0) {
      if (ByMonthDay.Count > 0) {
        return byMonthDay;
      }
      return seriesStart.Day <= daysInMonth ? [new(year, month, seriesStart.Day)] : [];
    }
    List<DateTime> month1 = Enumerable.Range(1, daysInMonth).Select(d => new DateTime(year, month, d)).ToList();
    List<DateTime> byDay = DaysMatching(month1);
    return ByMonthDay.Count > 0 ? byDay.Intersect(byMonthDay).ToList() : byDay;
  }

  private List<DateTime> YearlyCandidates(int year, DateTime seriesStart) {
    if (ByMonth.Count > 0) {
      return ByMonth.SelectMany(m => MonthCandidates(year, m, seriesStart)).ToList();
    }
    if (ByMonthDay.Count > 0) {
      List<DateTime> dates = Enumerable.Range(1, 12)
        .SelectMany(m => ByMonthDay.Select(d => (Month: m, Day: MonthDay(year, m, d))))
        .Where(md => md.Day > 0)
        .Select(md => new DateTime(year, md.Month, md.Day))
        .ToList();
      return ByDay.Count > 0 ? dates.Where(d => ByDay.Any(b => b.Day == d.DayOfWeek)).ToList() : dates;
    }
    if (ByDay.Count > 0) {
      int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
      return DaysMatching(Enumerable.Range(0, daysInYear).Select(d => new DateTime(year, 1, 1).AddDays(d)).ToList());
    }
    if (seriesStart.Month == 2 && seriesStart.Day == 29 && !DateTime.IsLeapYear(year)) {
      return [];
    }
    return [new(year, seriesStart.Month, seriesStart.Day)];
  }

  // The days in a period (a month or a year) that match BYDAY, where an offset counts within that period
  private List<DateTime> DaysMatching(List<DateTime> days) {
    List<DateTime> result = [];
    foreach (PixataRecurrenceDay byDay in ByDay) {
      List<DateTime> matching = days.Where(d => d.DayOfWeek == byDay.Day).ToList();
      if (byDay.Offset == 0) {
        result.AddRange(matching);
      } else {
        int index = byDay.Offset > 0 ? byDay.Offset - 1 : matching.Count + byDay.Offset;
        if (index >= 0 && index < matching.Count) {
          result.Add(matching[index]);
        }
      }
    }
    return result;
  }

  // The day of the month a BYMONTHDAY value means (negative values count back from the end), or 0 if there isn't one
  private static int MonthDay(int year, int month, int value) {
    int days = DateTime.DaysInMonth(year, month);
    int day = value > 0 ? value : days + value + 1;
    return day >= 1 && day <= days ? day : 0;
  }

  private static int PositiveInt(string value, string name) =>
    int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) && result > 0
      ? result
      : throw new FormatException($"{name} must be a positive whole number");

  private static List<int> IntList(string value, string name, int min, int max) {
    List<int> result = [];
    foreach (string item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
      if (!int.TryParse(item, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number) || number == 0 || Math.Abs(number) < min || Math.Abs(number) > max) {
        throw new FormatException($"'{item}' is not a valid {name} value");
      }
      result.Add(number);
    }
    return result;
  }

  private static (DateTime Until, bool IsUtc) ParseUntil(string value) {
    string text = value.Trim();
    bool utc = text.EndsWith('Z') || text.EndsWith('z');
    if (utc) {
      text = text[..^1];
    }
    string[] formats = ["yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm", "yyyyMMdd", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd"];
    if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime until)) {
      // A date without a time means the whole of that day
      return (text.Contains('T') ? until : until.Date.AddTicks(TimeSpan.TicksPerDay - 1), utc);
    }
    throw new FormatException($"'{value}' is not a valid UNTIL value");
  }
}
