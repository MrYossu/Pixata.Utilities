namespace Pixata.Blazor.Components;

/// <summary>
/// How <see cref="PixataCalendar"/> shows one day, as returned by its <c>DayInfo</c> function: a background colour or a class
/// to shade it (such as for a holiday), a tooltip, and a second line of text under the day's number (such as the date in
/// another calendar). The day can still be picked; use <c>DisabledDates</c> for days that can't
/// </summary>
public class PixataCalendarDayInfo {
  /// <summary>
  /// A CSS class added to the day's cell
  /// </summary>
  public string? Class { get; init; }

  /// <summary>
  /// A CSS colour to shade the day with. A selected day shows as selected instead
  /// </summary>
  public string? BackgroundColour { get; init; }

  /// <summary>
  /// The day's tooltip, in place of the date in full. Use a new line (<c>\n</c>) for more than one line
  /// </summary>
  public string? Title { get; init; }

  /// <summary>
  /// A second, smaller line of text under the day's number
  /// </summary>
  public string? SecondaryText { get; init; }
}
