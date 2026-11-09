using System;

namespace Pixata.Extensions.Grid;

/// <summary>
/// Options for applying a <see cref="GridRequest"/> to a query, which you can pass to <c>ToGridResult()</c> and
/// <c>ToGridResultAsync()</c>
/// </summary>
public record GridQueryOptions {
  /// <summary>
  /// Converts each <see cref="DateTime"/> in the filters before it goes into the query, such as from the user's time zone to
  /// UTC. The dates picked in a grid's filters have a <see cref="DateTime.Kind"/> of <see cref="DateTimeKind.Unspecified"/>,
  /// which SQL Server doesn't mind, but Npgsql (PostgreSQL) refuses to compare with a <c>timestamp with time zone</c>
  /// column. For a filter with <see cref="GridFilterDescriptor.IgnoreTime"/>, it's called for the start of the day and the
  /// start of the next one. See <see cref="Utc"/> and <see cref="FromTimeZone"/> for the usual conversions
  /// </summary>
  public Func<DateTime, DateTime>? ConvertDateTime { get; init; }

  /// <summary>
  /// Takes the filter dates to be in UTC already, and marks them as such without changing them
  /// </summary>
  public static GridQueryOptions Utc =>
    new() { ConvertDateTime = date => DateTime.SpecifyKind(date, DateTimeKind.Utc) };

  /// <summary>
  /// Takes the filter dates to be in the given time zone (normally the user's), and converts them to UTC, so a filter for
  /// "on 21/10/2026" in London finds the items from 23:00 UTC on the 20th. A date that's already marked as UTC is left as it
  /// is, and a time that doesn't exist in the zone, as the clocks went forward over it, is moved on to when they did
  /// </summary>
  public static GridQueryOptions FromTimeZone(TimeZoneInfo zone) =>
    new() { ConvertDateTime = date => ToUtc(date, zone) };

  private static DateTime ToUtc(DateTime date, TimeZoneInfo zone) {
    if (date.Kind == DateTimeKind.Utc) {
      return date;
    }
    if (date.Kind == DateTimeKind.Local) {
      return date.ToUniversalTime();
    }
    // Clocks change on a quarter of an hour in every zone, so this lands on the moment they changed
    while (zone.IsInvalidTime(date)) {
      date = date.AddMinutes(15);
    }
    return TimeZoneInfo.ConvertTimeToUtc(date, zone);
  }
}
