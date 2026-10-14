namespace Pixata.Blazor.Components;

/// <summary>
/// The views a <see cref="PixataScheduler{TItem}"/> can show
/// </summary>
public enum PixataSchedulerView {
  /// <summary>A single day, with its hours down the side</summary>
  Day,

  /// <summary>Several days side by side (set by the view's <c>NumberOfDays</c>), with the hours down the side</summary>
  MultiDay,

  /// <summary>A week, starting on the culture's (or the <c>FirstDayOfWeek</c> parameter's) first day of the week</summary>
  Week,

  /// <summary>A month, as six weeks of day cells</summary>
  Month,

  /// <summary>One or more days laid out horizontally, with a row for each resource when grouped</summary>
  Timeline,

  /// <summary>A list of the appointments, day by day</summary>
  Agenda
}

/// <summary>
/// How the scheduler lays out the groups when appointments are grouped by resource
/// </summary>
public enum PixataSchedulerGroupOrientation {
  /// <summary>The groups sit side by side, each with its own set of columns</summary>
  Horizontal,

  /// <summary>The groups sit one above the other, each with its own set of rows</summary>
  Vertical
}

/// <summary>
/// Whether the user chose to change one occurrence of a recurring appointment, or the whole series
/// </summary>
public enum PixataSchedulerRecurrenceEditMode {
  /// <summary>Just the occurrence the user picked</summary>
  Occurrence,

  /// <summary>The whole series (also used for appointments that don't recur)</summary>
  Series
}

/// <summary>
/// How often a <see cref="PixataRecurrenceRule"/> repeats
/// </summary>
public enum PixataRecurrenceFrequency {
  Daily,
  Weekly,
  Monthly,
  Yearly
}
