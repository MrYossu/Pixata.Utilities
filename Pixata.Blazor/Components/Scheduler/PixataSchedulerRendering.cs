using System;
using System.Globalization;

namespace Pixata.Blazor.Components;

/// <summary>
/// Helpers for writing the scheduler's inline styles and data attributes, which must use invariant formats whatever the
/// scheduler's culture is
/// </summary>
internal static class PixataSchedulerRendering {
  /// <summary>
  /// A number for CSS, eg <c>12.5</c>
  /// </summary>
  public static string Number(double value) =>
    value.ToString("0.####", CultureInfo.InvariantCulture);

  /// <summary>
  /// A fraction as a CSS percentage, eg <c>0.125</c> as <c>12.5%</c>
  /// </summary>
  public static string Percent(double fraction) =>
    $"{Number(fraction * 100)}%";

  /// <summary>
  /// A date and time for a data attribute, in the sortable ISO format the browser script passes back
  /// </summary>
  public static string Iso(DateTime value) =>
    value.ToString("s", CultureInfo.InvariantCulture);

  /// <summary>
  /// The inline style that places a box (an appointment in a time column)
  /// </summary>
  public static string BoxStyle(double top, double height, int lane, int lanes) =>
    $"--pixata-blazor-scheduler-event-top:{Percent(top)};--pixata-blazor-scheduler-event-height:{Percent(height)};--pixata-blazor-scheduler-event-lane:{lane};--pixata-blazor-scheduler-event-lanes:{Math.Max(1, lanes)};";

  /// <summary>
  /// The inline style that places a bar (an all-day appointment, or one in the month or timeline view)
  /// </summary>
  public static string BarStyle(double start, double width, int row) =>
    $"--pixata-blazor-scheduler-event-start:{Percent(start)};--pixata-blazor-scheduler-event-width:{Percent(width)};--pixata-blazor-scheduler-event-row:{row};";
}

/// <summary>
/// How an appointment is drawn: <see cref="Orientation"/> is <c>vertical</c> (a box in a time column), <c>horizontal</c>
/// (a bar) or <c>agenda</c> (a row of the agenda), and <see cref="Style"/> places it
/// </summary>
internal sealed record PixataSchedulerEventPlacement(string Orientation, string? Style, bool InAllDayRow = false, bool ContinuesBefore = false,
  bool ContinuesAfter = false, bool Draggable = true) {
  public const string Vertical = "vertical";
  public const string Horizontal = "horizontal";
  public const string Agenda = "agenda";
}
