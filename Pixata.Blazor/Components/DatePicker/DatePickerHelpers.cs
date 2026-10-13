using System.Linq;

namespace Pixata.Blazor.Components;

internal static class DatePickerCss {
  public static string Join(params string?[] classes) =>
    string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)));
}

/// <summary>
/// The views a <see cref="PixataCalendar"/> can show, from a month of days up to a century of decades
/// </summary>
public enum PixataCalendarView {
  Month,
  Year,
  Decade,
  Century
}
