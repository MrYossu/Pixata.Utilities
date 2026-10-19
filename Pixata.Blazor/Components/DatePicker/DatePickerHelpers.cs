using System;
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

/// <summary>
/// Whether a <see cref="PixataCalendar"/> picks a single date or a range
/// </summary>
public enum PixataCalendarSelectionMode {
  Single,
  Range
}

/// <summary>
/// The range picked in a <see cref="PixataCalendar"/>. Either end can be null while the user is picking it
/// </summary>
public record PixataCalendarRangeChangeEventArgs(DateTime? Start, DateTime? End);

// Converts between the value types the date components work with (DateTime, DateOnly and their nullable versions) and DateTime
internal static class DateValue<TValue> {
  private static readonly Type UnderlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

  public static bool IsSupported =>
    UnderlyingType == typeof(DateTime) || UnderlyingType == typeof(DateOnly);

  public static bool IsNullable =>
    Nullable.GetUnderlyingType(typeof(TValue)) is not null;

  public static DateTime? ToDateTime(TValue? value) => value switch {
    DateTime dateTime => dateTime,
    DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
    _ => null
  };

  public static TValue? FromDateTime(DateTime? date) {
    if (date is not DateTime value) {
      return default;
    }
    object result = UnderlyingType == typeof(DateOnly) ? DateOnly.FromDateTime(value) : value;
    return (TValue)result;
  }
}
