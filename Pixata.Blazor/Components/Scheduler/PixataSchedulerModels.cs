using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// An appointment as the scheduler shows it: the fields read from the data item, and, for an occurrence of a recurring
/// appointment, which series and occurrence it is
/// </summary>
internal sealed class PixataSchedulerItem<TItem> {
  /// <summary>
  /// Identifies the appointment (and, for an occurrence, which one) across renders
  /// </summary>
  public string Key { get; init; } = "";

  /// <summary>
  /// What templates and click events are given: the data item, or for an occurrence a copy of the series with the
  /// occurrence's start and end
  /// </summary>
  public TItem Item { get; init; } = default!;

  /// <summary>
  /// The item in the scheduler's data (the series, for an occurrence)
  /// </summary>
  public TItem DataItem { get; init; } = default!;

  public object? Id { get; init; }
  public string Title { get; init; } = "";
  public string Description { get; init; } = "";
  public DateTime Start { get; init; }
  public DateTime End { get; init; }
  public bool IsAllDay { get; init; }
  public bool ReadOnly { get; init; }

  /// <summary>
  /// True for an occurrence of a recurring appointment
  /// </summary>
  public bool IsOccurrence { get; init; }

  /// <summary>
  /// True for an appointment that replaces one occurrence of a recurring appointment
  /// </summary>
  public bool IsException { get; init; }

  /// <summary>
  /// For an occurrence, its start before any changes (what goes in the series' exceptions)
  /// </summary>
  public DateTime OccurrenceStart { get; init; }

  /// <summary>
  /// The colour of the first resource the appointment matches, if any
  /// </summary>
  public string? Colour { get; init; }

  /// <summary>
  /// The appointment's resource values, by field
  /// </summary>
  public IReadOnlyDictionary<string, object?> Resources { get; init; } = new Dictionary<string, object?>();

  /// <summary>
  /// True if the appointment goes in the all-day row (it is all day, or lasts at least a day)
  /// </summary>
  public bool IsLong => PixataSchedulerDates.IsLong(Start, End, IsAllDay);
}

/// <summary>
/// One level of a resource group, eg the "Big room" in "Big room / Alice"
/// </summary>
internal sealed record PixataSchedulerGroupLevel(string Field, object? Value, string Text, string? Colour);

/// <summary>
/// A group of slots when the scheduler is grouped by resource: one combination of resource values. When the scheduler isn't
/// grouped there is a single group with no resources, which every appointment belongs to
/// </summary>
internal sealed class PixataSchedulerGroup {
  public int Index { get; init; }
  public List<PixataSchedulerGroupLevel> Levels { get; init; } = [];

  public List<KeyValuePair<string, object?>> Resources =>
    Levels.Select(l => new KeyValuePair<string, object?>(l.Field, l.Value)).ToList();

  public string Text =>
    string.Join(" / ", Levels.Select(l => l.Text));

  public bool Contains<TItem>(PixataSchedulerItem<TItem> item) =>
    Levels.All(l => PixataSchedulerItemFields.ValuesMatch(item.Resources.TryGetValue(l.Field, out object? value) ? value : null, l.Value));
}

/// <summary>
/// A day column of a view, in a group
/// </summary>
internal readonly record struct PixataSchedulerColumn(DateTime Day, PixataSchedulerGroup Group);

/// <summary>
/// An appointment placed in a time column. <see cref="Top"/> and <see cref="Height"/> are fractions of the column's height
/// </summary>
internal sealed record PixataSchedulerBox<TItem>(PixataSchedulerItem<TItem> Item, double Top, double Height, int Lane, int Lanes, bool ContinuesBefore, bool ContinuesAfter);

/// <summary>
/// An appointment placed as a horizontal bar. <see cref="Start"/> and <see cref="Width"/> are fractions of the row's width,
/// <see cref="FirstColumn"/> and <see cref="LastColumn"/> the (inclusive) columns it covers, and <see cref="Row"/> which row
/// of bars it is in
/// </summary>
internal sealed record PixataSchedulerBar<TItem>(PixataSchedulerItem<TItem> Item, double Start, double Width, int FirstColumn, int LastColumn, int Row, bool ContinuesBefore, bool ContinuesAfter);

/// <summary>
/// The day, multi-day and week views: an all-day row and time columns, in one block (or one per group when grouped
/// vertically)
/// </summary>
internal sealed class PixataSchedulerTimeGridModel<TItem> {
  public List<DateTime> Days { get; init; } = [];
  public List<TimeSpan> Divisions { get; init; } = [];
  public TimeSpan DayStart { get; init; }
  public TimeSpan DayEnd { get; init; }
  public TimeSpan Division { get; init; }
  public int DivisionsPerSlot { get; init; } = 1;
  public bool ShowAllDayRow { get; init; }
  public List<PixataSchedulerGroup> Groups { get; init; } = [];
  public bool Vertical { get; init; }
  public List<PixataSchedulerTimeBlock<TItem>> Blocks { get; init; } = [];
}

internal sealed class PixataSchedulerTimeBlock<TItem> {
  /// <summary>
  /// The block's group when grouped vertically
  /// </summary>
  public PixataSchedulerGroup? Group { get; init; }

  public List<PixataSchedulerColumn> Columns { get; init; } = [];
  public List<PixataSchedulerBar<TItem>> AllDayBars { get; init; } = [];
  public int AllDayRows { get; init; }

  /// <summary>
  /// The appointments in each column
  /// </summary>
  public List<List<PixataSchedulerBox<TItem>>> Boxes { get; init; } = [];
}

/// <summary>
/// The month view: weeks of day cells, in one block (or one per group when grouped vertically)
/// </summary>
internal sealed class PixataSchedulerMonthModel<TItem> {
  public List<DateTime> Days { get; init; } = [];
  public int ItemsPerSlot { get; init; }
  public List<PixataSchedulerGroup> Groups { get; init; } = [];
  public bool Vertical { get; init; }
  public List<PixataSchedulerMonthBlock<TItem>> Blocks { get; init; } = [];
}

internal sealed class PixataSchedulerMonthBlock<TItem> {
  public PixataSchedulerGroup? Group { get; init; }
  public List<PixataSchedulerMonthWeek<TItem>> Weeks { get; init; } = [];
}

internal sealed class PixataSchedulerMonthWeek<TItem> {
  public List<PixataSchedulerColumn> Columns { get; init; } = [];

  /// <summary>
  /// The bars shown (those in the first <c>ItemsPerSlot</c> rows)
  /// </summary>
  public List<PixataSchedulerBar<TItem>> Bars { get; init; } = [];

  /// <summary>
  /// For each column, the number of appointments that didn't fit
  /// </summary>
  public int[] More { get; init; } = [];
}

/// <summary>
/// The timeline view: lanes of horizontal bars, one lane (or one per group when grouped vertically)
/// </summary>
internal sealed class PixataSchedulerTimelineModel<TItem> {
  public List<DateTime> Days { get; init; } = [];
  public List<TimeSpan> Divisions { get; init; } = [];
  public TimeSpan DayStart { get; init; }
  public TimeSpan DayEnd { get; init; }
  public TimeSpan Division { get; init; }
  public int DivisionsPerSlot { get; init; } = 1;

  /// <summary>
  /// The groups across the view (all of them when grouped horizontally, otherwise just the one group with no resources)
  /// </summary>
  public List<PixataSchedulerGroup> ColumnGroups { get; init; } = [];

  public List<PixataSchedulerTimelineLane<TItem>> Lanes { get; init; } = [];
}

internal sealed class PixataSchedulerTimelineLane<TItem> {
  /// <summary>
  /// The lane's group when grouped vertically, or null for a lane holding every group
  /// </summary>
  public PixataSchedulerGroup? Group { get; init; }

  public List<PixataSchedulerBar<TItem>> Bars { get; init; } = [];
  public int Rows { get; init; }
}

/// <summary>
/// The agenda view: the appointments of each day, in one list (or one per group)
/// </summary>
internal sealed class PixataSchedulerAgendaModel<TItem> {
  public List<PixataSchedulerAgendaGroup<TItem>> Groups { get; init; } = [];
}

internal sealed class PixataSchedulerAgendaGroup<TItem> {
  public PixataSchedulerGroup? Group { get; init; }
  public List<(DateTime Day, List<PixataSchedulerItem<TItem>> Items)> Days { get; init; } = [];
}

/// <summary>
/// Works out where every appointment goes in each view. Plain C#, so it can be unit tested
/// </summary>
internal static class PixataSchedulerModelBuilder {
  /// <summary>
  /// The groups for the resources being grouped by: every combination of their values, in order. With no resources there is
  /// a single group that every appointment belongs to
  /// </summary>
  public static List<PixataSchedulerGroup> Groups(IReadOnlyList<(string Field, IReadOnlyList<(object? Value, string Text, string? Colour)> Items)> resources) {
    List<List<PixataSchedulerGroupLevel>> combinations = [[]];
    foreach ((string field, IReadOnlyList<(object? Value, string Text, string? Colour)> items) in resources) {
      combinations = combinations
        .SelectMany(levels => items.Select(item => (List<PixataSchedulerGroupLevel>)[.. levels, new(field, item.Value, item.Text, item.Colour)]))
        .ToList();
    }
    return combinations.Select((levels, index) => new PixataSchedulerGroup { Index = index, Levels = levels }).ToList();
  }

  /// <summary>
  /// The header rows for groups laid out side by side: for each level of grouping, each header and the number of groups it
  /// spans
  /// </summary>
  public static List<List<(PixataSchedulerGroupLevel Level, int Span)>> GroupHeaders(IReadOnlyList<PixataSchedulerGroup> groups) {
    List<List<(PixataSchedulerGroupLevel, int)>> rows = [];
    int depth = groups.Count == 0 ? 0 : groups[0].Levels.Count;
    for (int level = 0; level < depth; level++) {
      List<(PixataSchedulerGroupLevel Level, int Span)> row = [];
      for (int i = 0; i < groups.Count; i++) {
        if (row.Count > 0 && i > 0 && SamePath(groups[i - 1], groups[i], level)) {
          row[^1] = (row[^1].Level, row[^1].Span + 1);
        } else {
          row.Add((groups[i].Levels[level], 1));
        }
      }
      rows.Add(row);
    }
    return rows;
  }

  private static bool SamePath(PixataSchedulerGroup a, PixataSchedulerGroup b, int level) {
    for (int i = 0; i <= level; i++) {
      if (!PixataSchedulerItemFields.ValuesMatch(a.Levels[i].Value, b.Levels[i].Value)) {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// The day, multi-day and week views
  /// </summary>
  public static PixataSchedulerTimeGridModel<TItem> TimeGrid<TItem>(IReadOnlyList<PixataSchedulerItem<TItem>> items, IReadOnlyList<DateTime> days,
    TimeSpan dayStart, TimeSpan dayEnd, TimeSpan division, int divisionsPerSlot, IReadOnlyList<PixataSchedulerGroup> groups, bool vertical, bool showAllDayRow) {
    List<PixataSchedulerItem<TItem>> sorted = Sort(items);
    List<PixataSchedulerTimeBlock<TItem>> blocks = [];
    foreach (IReadOnlyList<PixataSchedulerGroup> blockGroups in vertical ? groups.Select(g => (IReadOnlyList<PixataSchedulerGroup>)[g]) : [groups]) {
      List<PixataSchedulerColumn> columns = blockGroups.SelectMany(g => days.Select(d => new PixataSchedulerColumn(d, g))).ToList();
      List<PixataSchedulerBar<TItem>> bars = [];
      int allDayRows = 0;
      if (showAllDayRow) {
        for (int g = 0; g < blockGroups.Count; g++) {
          List<PixataSchedulerBar<TItem>> groupBars = DayBars(sorted.Where(i => i.IsLong && blockGroups[g].Contains(i)), days, g * days.Count, columns.Count);
          bars.AddRange(groupBars);
          allDayRows = Math.Max(allDayRows, PixataSchedulerLayout.RowCount(groupBars.Select(b => b.Row)));
        }
      }
      TimeSpan window = dayEnd - dayStart;
      List<List<PixataSchedulerBox<TItem>>> boxes = [];
      foreach (PixataSchedulerColumn column in columns) {
        DateTime from = column.Day + dayStart;
        DateTime to = column.Day + dayEnd;
        List<(PixataSchedulerItem<TItem> Item, DateTime Start, DateTime End)> segments = sorted
          .Where(i => (showAllDayRow ? !i.IsLong : !i.IsAllDay) && column.Group.Contains(i))
          .Where(i => i.Start < to && (i.End > from || (i.End == i.Start && i.Start >= from)))
          .Select(i => (i, i.Start < from ? from : i.Start, i.End > to ? to : i.End))
          .ToList();
        PixataSchedulerLane[] lanes = PixataSchedulerLayout.Columns(segments.Select(s => (s.Start.Ticks, Math.Max(s.End.Ticks, (s.Start + division).Ticks))).ToList());
        boxes.Add(segments.Select((s, index) => new PixataSchedulerBox<TItem>(
          s.Item,
          (s.Start - from) / window,
          (s.End - s.Start) / window,
          lanes[index].Lane,
          lanes[index].Lanes,
          s.Item.Start < from,
          s.Item.End > to)).ToList());
      }
      blocks.Add(new() {
        Group = vertical ? blockGroups[0] : null,
        Columns = columns,
        AllDayBars = bars,
        AllDayRows = allDayRows,
        Boxes = boxes
      });
    }
    return new() {
      Days = [.. days],
      Divisions = PixataSchedulerDates.Divisions(dayStart, dayEnd, (int)(division.TotalMinutes * divisionsPerSlot), divisionsPerSlot),
      DayStart = dayStart,
      DayEnd = dayEnd,
      Division = division,
      DivisionsPerSlot = divisionsPerSlot,
      ShowAllDayRow = showAllDayRow,
      Groups = [.. groups],
      Vertical = vertical,
      Blocks = blocks
    };
  }

  /// <summary>
  /// The month view
  /// </summary>
  public static PixataSchedulerMonthModel<TItem> Month<TItem>(IReadOnlyList<PixataSchedulerItem<TItem>> items, IReadOnlyList<DateTime> days,
    IReadOnlyList<PixataSchedulerGroup> groups, bool vertical, int itemsPerSlot) {
    List<PixataSchedulerItem<TItem>> sorted = Sort(items);
    List<PixataSchedulerMonthBlock<TItem>> blocks = [];
    foreach (IReadOnlyList<PixataSchedulerGroup> blockGroups in vertical ? groups.Select(g => (IReadOnlyList<PixataSchedulerGroup>)[g]) : [groups]) {
      List<PixataSchedulerMonthWeek<TItem>> weeks = [];
      for (int w = 0; w * 7 < days.Count; w++) {
        List<DateTime> weekDays = days.Skip(w * 7).Take(7).ToList();
        List<PixataSchedulerColumn> columns = blockGroups.SelectMany(g => weekDays.Select(d => new PixataSchedulerColumn(d, g))).ToList();
        List<PixataSchedulerBar<TItem>> bars = [];
        int[] more = new int[columns.Count];
        for (int g = 0; g < blockGroups.Count; g++) {
          foreach (PixataSchedulerBar<TItem> bar in DayBars(sorted.Where(i => blockGroups[g].Contains(i)), weekDays, g * weekDays.Count, columns.Count)) {
            if (bar.Row < itemsPerSlot) {
              bars.Add(bar);
            } else {
              for (int c = bar.FirstColumn; c <= bar.LastColumn; c++) {
                more[c]++;
              }
            }
          }
        }
        weeks.Add(new() { Columns = columns, Bars = bars, More = more });
      }
      blocks.Add(new() { Group = vertical ? blockGroups[0] : null, Weeks = weeks });
    }
    return new() { Days = [.. days], ItemsPerSlot = itemsPerSlot, Groups = [.. groups], Vertical = vertical, Blocks = blocks };
  }

  /// <summary>
  /// The timeline view
  /// </summary>
  public static PixataSchedulerTimelineModel<TItem> Timeline<TItem>(IReadOnlyList<PixataSchedulerItem<TItem>> items, IReadOnlyList<DateTime> days,
    TimeSpan dayStart, TimeSpan dayEnd, TimeSpan division, int divisionsPerSlot, IReadOnlyList<PixataSchedulerGroup> groups, bool vertical) {
    List<PixataSchedulerItem<TItem>> sorted = Sort(items);
    bool grouped = groups.Count > 1 || groups.Any(g => g.Levels.Count > 0);
    List<PixataSchedulerGroup> columnGroups = vertical || !grouped ? [groups.Count == 1 ? groups[0] : new PixataSchedulerGroup()] : [.. groups];
    long window = (dayEnd - dayStart).Ticks;
    long total = window * days.Count * columnGroups.Count;
    DateTime first = days.Count == 0 ? DateTime.MinValue : days[0];

    // Where a moment falls along the lane, in ticks of visible time
    long Position(DateTime value, int columnGroup) {
      int day = (int)Math.Floor((value.Date - first).TotalDays);
      long offset;
      if (day < 0) {
        offset = 0;
      } else if (day >= days.Count) {
        offset = window * days.Count;
      } else {
        TimeSpan time = value.TimeOfDay;
        offset = day * window + Math.Clamp((time - dayStart).Ticks, 0, window);
      }
      return columnGroup * window * days.Count + offset;
    }

    List<PixataSchedulerTimelineLane<TItem>> lanes = [];
    foreach (PixataSchedulerGroup? laneGroup in vertical && grouped ? groups.Select(g => (PixataSchedulerGroup?)g) : [null]) {
      List<(PixataSchedulerItem<TItem> Item, long Start, long End)> placed = [];
      for (int g = 0; g < columnGroups.Count; g++) {
        PixataSchedulerGroup group = laneGroup ?? columnGroups[g];
        foreach (PixataSchedulerItem<TItem> item in sorted.Where(group.Contains)) {
          DateTime end = item.IsAllDay ? PixataSchedulerDates.DaySpan(item.Start, item.End).End : item.End;
          long start = Position(item.Start, g);
          long finish = Position(end, g);
          if (end == item.Start) {
            finish = Math.Min(start + division.Ticks, Position(first.AddDays(days.Count), g));
          }
          if (finish > start) {
            placed.Add((item, start, finish));
          }
        }
      }
      int[] rows = PixataSchedulerLayout.Rows(placed.Select(p => (p.Start, p.End)).ToList());
      List<PixataSchedulerBar<TItem>> bars = placed.Select((p, index) => new PixataSchedulerBar<TItem>(
        p.Item,
        total == 0 ? 0 : p.Start / (double)total,
        total == 0 ? 0 : (p.End - p.Start) / (double)total,
        0,
        0,
        rows[index],
        p.Item.IsAllDay ? p.Item.Start.Date < first : p.Item.Start < first + dayStart,
        p.Item.IsAllDay
          ? PixataSchedulerDates.DaySpan(p.Item.Start, p.Item.End).End > first.AddDays(days.Count)
          : p.Item.End > first.AddDays(days.Count - 1) + dayEnd)).ToList();
      lanes.Add(new() { Group = laneGroup, Bars = bars, Rows = PixataSchedulerLayout.RowCount(rows) });
    }
    return new() {
      Days = [.. days],
      Divisions = PixataSchedulerDates.Divisions(dayStart, dayEnd, (int)(division.TotalMinutes * divisionsPerSlot), divisionsPerSlot),
      DayStart = dayStart,
      DayEnd = dayEnd,
      Division = division,
      DivisionsPerSlot = divisionsPerSlot,
      ColumnGroups = columnGroups,
      Lanes = lanes
    };
  }

  /// <summary>
  /// The agenda view. Each appointment is listed once, on the first of the days that it covers
  /// </summary>
  public static PixataSchedulerAgendaModel<TItem> Agenda<TItem>(IReadOnlyList<PixataSchedulerItem<TItem>> items, IReadOnlyList<DateTime> days,
    IReadOnlyList<PixataSchedulerGroup> groups, bool hideEmptyDays) {
    List<PixataSchedulerItem<TItem>> sorted = Sort(items);
    bool grouped = groups.Any(g => g.Levels.Count > 0);
    List<PixataSchedulerAgendaGroup<TItem>> result = [];
    foreach (PixataSchedulerGroup group in grouped ? groups : [groups.Count == 1 ? groups[0] : new PixataSchedulerGroup()]) {
      List<(DateTime, List<PixataSchedulerItem<TItem>>)> agendaDays = [];
      HashSet<PixataSchedulerItem<TItem>> listed = [];
      foreach (DateTime day in days) {
        // Each appointment is listed once, on the first day it covers
        List<PixataSchedulerItem<TItem>> dayItems = sorted
          .Where(i => group.Contains(i) && !listed.Contains(i) && Covers(i, day))
          .OrderByDescending(i => i.IsAllDay)
          .ThenBy(i => i.Start)
          .ToList();
        listed.UnionWith(dayItems);
        if (dayItems.Count > 0 || !hideEmptyDays) {
          agendaDays.Add((day, dayItems));
        }
      }
      result.Add(new() { Group = grouped ? group : null, Days = agendaDays });
    }
    return new() { Groups = result };
  }

  /// <summary>
  /// True if the appointment happens (at least partly) on the day
  /// </summary>
  public static bool Covers<TItem>(PixataSchedulerItem<TItem> item, DateTime day) {
    if (item.IsAllDay) {
      (DateTime first, DateTime end) = PixataSchedulerDates.DaySpan(item.Start, item.End);
      return first <= day && day < end;
    }
    return item.Start < day.AddDays(1) && (item.End > day || (item.End == item.Start && item.Start >= day));
  }

  // Bars for the days an appointment covers, laid out in rows
  private static List<PixataSchedulerBar<TItem>> DayBars<TItem>(IEnumerable<PixataSchedulerItem<TItem>> items, IReadOnlyList<DateTime> days, int columnOffset, int columnCount) {
    if (days.Count == 0) {
      return [];
    }
    DateTime first = days[0];
    DateTime end = days[^1].AddDays(1);
    List<(PixataSchedulerItem<TItem> Item, int First, int Last, bool Before, bool After)> spans = [];
    foreach (PixataSchedulerItem<TItem> item in items) {
      (DateTime from, DateTime to) = item.IsAllDay ? PixataSchedulerDates.DaySpan(item.Start, item.End) : PixataSchedulerDates.TimedDaySpan(item.Start, item.End);
      if (from >= end || to <= first) {
        continue;
      }
      int firstIndex = Math.Max(0, (int)(from - first).TotalDays);
      int lastIndex = Math.Min(days.Count - 1, (int)(to - first).TotalDays - 1);
      spans.Add((item, firstIndex, lastIndex, (from < first), (to > end)));
    }
    int[] rows = PixataSchedulerLayout.Rows(spans.Select(s => ((long)s.First, (long)s.Last + 1)).ToList());
    return spans.Select((s, index) => new PixataSchedulerBar<TItem>(
      s.Item,
      (columnOffset + s.First) / (double)columnCount,
      (s.Last - s.First + 1) / (double)columnCount,
      columnOffset + s.First,
      columnOffset + s.Last,
      rows[index],
      s.Before,
      s.After)).ToList();
  }

  // Earliest first; the longest first when they start together
  private static List<PixataSchedulerItem<TItem>> Sort<TItem>(IReadOnlyList<PixataSchedulerItem<TItem>> items) =>
    items.OrderBy(i => i.IsAllDay ? i.Start.Date : i.Start).ThenByDescending(i => i.End - i.Start).ToList();
}
