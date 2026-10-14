using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Pixata.Blazor.Components;

/// <summary>
/// Reads (and writes) the field of a data item named by a component parameter such as <c>TextField</c>, <c>ValueField</c> or
/// <c>StartField</c>. Nested properties are separated with dots (<c>Address.City</c>), and dictionaries (including
/// <c>ExpandoObject</c>) are read by key. When no field is given, the item itself is the value, which is how primitive data
/// (a list of strings, say) is handled
/// </summary>
public static class PixataFieldReader {
  private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> Properties = new();

  /// <summary>
  /// The value of <paramref name="field"/> on <paramref name="item"/>, the item itself if <paramref name="field"/> is null or
  /// empty, or null if <paramref name="item"/> (or a property along a nested path) is null
  /// </summary>
  /// <exception cref="InvalidOperationException">The item's type has no readable property with that name</exception>
  public static object? GetValue(object? item, string? field) {
    if (item is null || string.IsNullOrEmpty(field)) {
      return item;
    }
    object? current = item;
    foreach (string part in field.Split('.')) {
      if (current is null) {
        return null;
      }
      current = current switch {
        IDictionary<string, object?> dictionary => dictionary.TryGetValue(part, out object? value) ? value : null,
        IReadOnlyDictionary<string, object?> dictionary => dictionary.TryGetValue(part, out object? value) ? value : null,
        _ => Property(current.GetType(), part).GetValue(current)
      };
    }
    return current;
  }

  /// <summary>
  /// The value of <paramref name="field"/> as text, formatted with the current culture, or <c>""</c> if it is null
  /// </summary>
  public static string GetText(object? item, string? field, string? format = null) =>
    GetValue(item, field) switch {
      null => "",
      IFormattable formattable when !string.IsNullOrEmpty(format) => formattable.ToString(format, CultureInfo.CurrentCulture),
      IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
      object value => value.ToString() ?? ""
    };

  /// <summary>
  /// The value of <paramref name="field"/>, converted to <typeparamref name="T"/> if it isn't one already (eg an int field
  /// read as a long, or a string as an enum)
  /// </summary>
  public static T? GetValue<T>(object? item, string? field) =>
    ConvertTo<T>(GetValue(item, field));

  /// <summary>
  /// Sets <paramref name="field"/> on <paramref name="item"/>, converting the value to the property's type if need be
  /// </summary>
  /// <exception cref="InvalidOperationException">The item's type has no writable property with that name</exception>
  public static void SetValue(object item, string field, object? value) {
    string[] parts = field.Split('.');
    object? target = item;
    for (int i = 0; i < parts.Length - 1; i++) {
      target = GetValue(target, parts[i]) ?? throw new InvalidOperationException($"Cannot set {field} on {item.GetType().Name}, as {parts[i]} is null");
    }
    string last = parts[^1];
    if (target is IDictionary<string, object?> dictionary) {
      dictionary[last] = value;
      return;
    }
    PropertyInfo property = Property(target!.GetType(), last);
    if (!property.CanWrite) {
      throw new InvalidOperationException($"The {property.Name} property of {target.GetType().Name} is read-only");
    }
    property.SetValue(target, ConvertTo(value, property.PropertyType));
  }

  /// <summary>
  /// Converts a value to <typeparamref name="T"/>, handling nullable types, enums and the usual numeric conversions
  /// </summary>
  public static T? ConvertTo<T>(object? value) =>
    (T?)ConvertTo(value, typeof(T));

  /// <summary>
  /// Converts a value to <paramref name="type"/>, handling nullable types, enums and the usual numeric conversions
  /// </summary>
  public static object? ConvertTo(object? value, Type type) {
    if (value is null) {
      return null;
    }
    if (type.IsInstanceOfType(value)) {
      return value;
    }
    Type target = Nullable.GetUnderlyingType(type) ?? type;
    if (target.IsEnum) {
      return value is string text ? Enum.Parse(target, text, true) : Enum.ToObject(target, value);
    }
    if (target == typeof(Guid)) {
      return Guid.Parse(value.ToString()!);
    }
    if (target == typeof(DateOnly) && value is DateTime dateTime) {
      return DateOnly.FromDateTime(dateTime);
    }
    if (target == typeof(DateTime) && value is DateOnly dateOnly) {
      return dateOnly.ToDateTime(TimeOnly.MinValue);
    }
    return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
  }

  private static PropertyInfo Property(Type type, string name) =>
    Properties.GetOrAdd((type, name), key =>
      key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance)
      ?? throw new InvalidOperationException($"The type {key.Item1.Name} has no public property called {key.Item2}"));
}
