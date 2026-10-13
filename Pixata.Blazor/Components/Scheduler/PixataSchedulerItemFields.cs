using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Pixata.Blazor.Components;

/// <summary>
/// Reads and writes the appointment fields named by the scheduler's <c>TitleField</c>, <c>StartField</c> (and so on)
/// parameters. Optional fields (description, all day, read-only and the recurrence fields) can be missing from the data type
/// when their names are the defaults, in which case they read as empty and aren't written
/// </summary>
internal sealed class PixataSchedulerItemFields {
  public const string DefaultId = "Id";
  public const string DefaultTitle = "Title";
  public const string DefaultStart = "Start";
  public const string DefaultEnd = "End";
  public const string DefaultIsAllDay = "IsAllDay";
  public const string DefaultDescription = "Description";
  public const string DefaultReadOnly = "ReadOnly";
  public const string DefaultRecurrenceRule = "RecurrenceRule";
  public const string DefaultRecurrenceExceptions = "RecurrenceExceptions";
  public const string DefaultRecurrenceId = "RecurrenceId";

  private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Properties = new();
  private static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

  public string Id { get; init; } = DefaultId;
  public string Title { get; init; } = DefaultTitle;
  public string Start { get; init; } = DefaultStart;
  public string End { get; init; } = DefaultEnd;
  public string IsAllDay { get; init; } = DefaultIsAllDay;
  public string Description { get; init; } = DefaultDescription;
  public string ReadOnly { get; init; } = DefaultReadOnly;
  public string RecurrenceRule { get; init; } = DefaultRecurrenceRule;
  public string RecurrenceExceptions { get; init; } = DefaultRecurrenceExceptions;
  public string RecurrenceId { get; init; } = DefaultRecurrenceId;

  /// <summary>
  /// Checks that the fields exist on a data item, throwing if a required field (title, start or end) is missing, or a field
  /// whose name was set explicitly is missing
  /// </summary>
  public void Validate(object item) {
    if (item is IDictionary<string, object?>) {
      return;
    }
    foreach ((string field, string parameter) in new[] { (Title, "TitleField"), (Start, "StartField"), (End, "EndField") }) {
      if (!Has(item, field)) {
        throw new InvalidOperationException($"The appointment type {item.GetType().Name} has no {field} field. Set the scheduler's {parameter} parameter to the name of the field to use");
      }
    }
    foreach ((string field, string defaultName, string parameter) in new[] {
      (Id, DefaultId, "IdField"), (IsAllDay, DefaultIsAllDay, "IsAllDayField"), (Description, DefaultDescription, "DescriptionField"),
      (ReadOnly, DefaultReadOnly, "ReadOnlyField"), (RecurrenceRule, DefaultRecurrenceRule, "RecurrenceRuleField"),
      (RecurrenceExceptions, DefaultRecurrenceExceptions, "RecurrenceExceptionsField"), (RecurrenceId, DefaultRecurrenceId, "RecurrenceIdField")
    }) {
      if (field != defaultName && !Has(item, field)) {
        throw new InvalidOperationException($"The appointment type {item.GetType().Name} has no {field} field, which was given as the scheduler's {parameter}");
      }
    }
  }

  public object? GetId(object item) => Read(item, Id);
  public string GetTitle(object item) => Read(item, Title)?.ToString() ?? "";
  public string GetDescription(object item) => Read(item, Description)?.ToString() ?? "";
  public bool GetIsAllDay(object item) => ToBool(Read(item, IsAllDay));
  public bool GetReadOnly(object item) => ToBool(Read(item, ReadOnly));
  public string? GetRecurrenceRule(object item) => Read(item, RecurrenceRule)?.ToString();
  public object? GetRecurrenceId(object item) => Read(item, RecurrenceId);
  public DateTime GetStart(object item) => ToDateTime(Read(item, Start)) ?? DateTime.MinValue;

  public DateTime GetEnd(object item) {
    DateTime start = GetStart(item);
    DateTime end = ToDateTime(Read(item, End)) ?? start;
    return end < start ? start : end;
  }

  public List<DateTime> GetRecurrenceExceptions(object item) =>
    Read(item, RecurrenceExceptions) switch {
      null => [],
      string text => text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(t => ToDateTime(t)).Where(d => d is not null).Select(d => d!.Value).ToList(),
      IEnumerable values => values.Cast<object?>().Select(ToDateTime).Where(d => d is not null).Select(d => d!.Value).ToList(),
      object value => ToDateTime(value) is { } date ? [date] : []
    };

  public bool HasRecurrence(object item) => Has(item, RecurrenceRule);
  public bool HasRecurrenceId(object item) => Has(item, RecurrenceId);
  public bool HasDescription(object item) => Has(item, Description);
  public bool HasIsAllDay(object item) => Has(item, IsAllDay);

  public void SetTitle(object item, string value) => Write(item, Title, value);
  public void SetDescription(object item, string value) => Write(item, Description, value);
  public void SetIsAllDay(object item, bool value) => Write(item, IsAllDay, value);
  public void SetStart(object item, DateTime value) => WriteDate(item, Start, value);
  public void SetEnd(object item, DateTime value) => WriteDate(item, End, value);
  public void SetRecurrenceRule(object item, string? value) => Write(item, RecurrenceRule, string.IsNullOrWhiteSpace(value) ? null : value);
  public void SetRecurrenceId(object item, object? value) => Write(item, RecurrenceId, value);

  /// <summary>
  /// Sets the item's id to the default for its type (null, 0, an empty Guid...), for an item copied from another
  /// </summary>
  public void ClearId(object item) {
    if (!Has(item, Id)) {
      return;
    }
    if (item is IDictionary<string, object?> dictionary) {
      dictionary.Remove(Id);
      return;
    }
    Type? type = FieldType(item, Id);
    if (type is not null && Writable(item, Id)) {
      PixataFieldReader.SetValue(item, Id, type.IsValueType && Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(type) : null);
    }
  }

  public void SetRecurrenceExceptions(object item, IEnumerable<DateTime>? values) {
    if (!Has(item, RecurrenceExceptions) && item is not IDictionary<string, object?>) {
      return;
    }
    List<DateTime>? list = values?.Distinct().OrderBy(d => d).ToList();
    Type? type = FieldType(item, RecurrenceExceptions);
    object? value = list is null || type is null ? list : type switch {
      _ when type == typeof(DateTime[]) => list.ToArray(),
      _ when type == typeof(string) => string.Join(",", list.Select(d => d.ToString("s", CultureInfo.InvariantCulture))),
      _ when type.IsAssignableFrom(typeof(List<DateTime>)) => list,
      _ when type == typeof(List<DateTimeOffset>) || type.IsAssignableFrom(typeof(List<DateTimeOffset>)) => list.Select(d => new DateTimeOffset(d)).ToList(),
      _ => list
    };
    Write(item, RecurrenceExceptions, value);
  }

  /// <summary>
  /// Sets a resource field, converting the value to the field's type
  /// </summary>
  public void SetField(object item, string field, object? value) => Write(item, field, value);

  /// <summary>
  /// Reads any field, or null if the item doesn't have it
  /// </summary>
  public object? GetField(object item, string field) => Read(item, field);

  /// <summary>
  /// A shallow copy of a data item, so the edit form can change it without changing the original
  /// </summary>
  public static TItem Clone<TItem>(TItem item) =>
    item switch {
      null => item,
      ExpandoObject expando => (TItem)(object)CopyExpando(expando),
      Dictionary<string, object?> dictionary => (TItem)(object)new Dictionary<string, object?>(dictionary),
      _ => (TItem)MemberwiseCloneMethod.Invoke(item, null)!
    };

  /// <summary>
  /// Copies every public read-write property (or dictionary entry) from one item to another, except those named
  /// </summary>
  public static void CopyFields(object source, object target, params string[] except) {
    if (source is IDictionary<string, object?> from && target is IDictionary<string, object?> to) {
      foreach (KeyValuePair<string, object?> pair in from.Where(p => !except.Contains(p.Key))) {
        to[pair.Key] = pair.Value;
      }
      return;
    }
    foreach (PropertyInfo property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
      if (except.Contains(property.Name) || !property.CanRead || property.GetIndexParameters().Length > 0) {
        continue;
      }
      PropertyInfo? targetProperty = target.GetType().GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
      if (targetProperty is { CanWrite: true } && targetProperty.GetSetMethod() is not null && targetProperty.PropertyType.IsAssignableFrom(property.PropertyType)) {
        targetProperty.SetValue(target, property.GetValue(source));
      }
    }
  }

  /// <summary>
  /// True if two field values are the same, allowing for different numeric types and for strings that hold the same value
  /// (eg a resource value of 1 and an appointment field of "1")
  /// </summary>
  public static bool ValuesMatch(object? a, object? b) {
    if (a is null || b is null) {
      return a is null && b is null;
    }
    if (a.Equals(b)) {
      return true;
    }
    return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.Ordinal);
  }

  /// <summary>
  /// True if the item has the field (or, for a nested path, the first part of it)
  /// </summary>
  public static bool Has(object item, string field) {
    if (string.IsNullOrEmpty(field)) {
      return false;
    }
    if (item is IDictionary<string, object?> dictionary) {
      return dictionary.ContainsKey(field.Split('.')[0]);
    }
    return Property(item.GetType(), field.Split('.')[0]) is not null;
  }

  private static object? Read(object item, string field) {
    if (!Has(item, field)) {
      return null;
    }
    return PixataFieldReader.GetValue(item, field);
  }

  private static void Write(object item, string field, object? value) {
    if (string.IsNullOrEmpty(field)) {
      return;
    }
    if (item is IDictionary<string, object?> dictionary && !field.Contains('.')) {
      dictionary[field] = value;
      return;
    }
    if (!Has(item, field) || !Writable(item, field)) {
      return;
    }
    Type? type = FieldType(item, field);
    if (value is not null && type is not null && Nullable.GetUnderlyingType(type) is { } underlying && underlying != value.GetType()) {
      value = PixataFieldReader.ConvertTo(value, underlying);
    }
    if (value is null && type is { IsValueType: true } && Nullable.GetUnderlyingType(type) is null) {
      value = Activator.CreateInstance(type);
    }
    PixataFieldReader.SetValue(item, field, value);
  }

  private static void WriteDate(object item, string field, DateTime value) {
    if (item is IDictionary<string, object?> dictionary && !field.Contains('.')) {
      dictionary[field] = dictionary.TryGetValue(field, out object? current) && current is DateTimeOffset offset ? new DateTimeOffset(value, offset.Offset) : value;
      return;
    }
    Type? type = FieldType(item, field);
    Type? target = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
    object converted = target switch {
      _ when target == typeof(DateTimeOffset) => Read(item, field) is DateTimeOffset current ? new DateTimeOffset(value, current.Offset) : new DateTimeOffset(value),
      _ when target == typeof(DateOnly) => DateOnly.FromDateTime(value),
      _ => value
    };
    Write(item, field, converted);
  }

  private static bool Writable(object item, string field) {
    if (item is IDictionary<string, object?>) {
      return true;
    }
    string[] parts = field.Split('.');
    Type type = item.GetType();
    for (int i = 0; i < parts.Length; i++) {
      PropertyInfo? property = Property(type, parts[i]);
      if (property is null) {
        return false;
      }
      if (i == parts.Length - 1) {
        return property.CanWrite && property.GetSetMethod() is not null;
      }
      type = property.PropertyType;
    }
    return false;
  }

  private static Type? FieldType(object item, string field) {
    if (item is IDictionary<string, object?> dictionary) {
      return dictionary.TryGetValue(field, out object? value) ? value?.GetType() : null;
    }
    Type type = item.GetType();
    foreach (string part in field.Split('.')) {
      PropertyInfo? property = Property(type, part);
      if (property is null) {
        return null;
      }
      type = property.PropertyType;
    }
    return type;
  }

  private static PropertyInfo? Property(Type type, string name) =>
    Properties.GetOrAdd((type, name), key => key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance));

  private static ExpandoObject CopyExpando(ExpandoObject source) {
    ExpandoObject copy = new();
    IDictionary<string, object?> target = copy;
    foreach (KeyValuePair<string, object?> pair in source) {
      target[pair.Key] = pair.Value;
    }
    return copy;
  }

  private static bool ToBool(object? value) =>
    value switch {
      null => false,
      bool b => b,
      string s => bool.TryParse(s, out bool parsed) && parsed,
      _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
    };

  private static DateTime? ToDateTime(object? value) =>
    value switch {
      null => null,
      DateTime dateTime => dateTime,
      DateTimeOffset offset => offset.DateTime,
      DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
      string text when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed) => parsed,
      _ => null
    };
}
