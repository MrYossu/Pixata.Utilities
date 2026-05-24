using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Pixata.Extensions;

public static class ObjectExtensionMethods {
  /// <summary>
  /// Returns a shallow clone of an object
  /// </summary>
  /// <typeparam name="T">The type of the object to be cloned</typeparam>
  /// <param name="source">The object to be cloned</param>
  /// <returns>A shallow clone of the object. Uses reflection, but despite all the myths about this being slow, doing 10 million clones only took about 300ms longer than a reflection-free (and way more complex) version, so I went for simplicity.</returns>
  public static T Clone<T>(this T source) where T : class =>
    (T)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(source, null)!;

  /// <summary>
  /// Dumps the names and values of all simple properties of an object to a string for debugging purposes. Simple properties are enums, string, decimal, DateTime, DateOnly, TimeOnly, TimeSpan, and Guid. Properties that cannot be read or are in the ignoredProperties list are skipped.
  /// </summary>
  /// <param name="obj">The object whose properties are to be dumped.</param>
  /// <param name="ignoredProperties">An optional array of property names to ignore.</param>
  /// <returns>A string representation of the object's simple properties.</returns>
  public static string DumpProperties(this object? obj, string[]? ignoredProperties = null) {
    if (obj is null) {
      return "(null)";
    }
    HashSet<string> ignored = ignoredProperties is null ? [] : [..ignoredProperties];
    Type type = obj.GetType();
    StringBuilder sb = new();
    sb.AppendLine($"Type: {type.Name}");
    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (PropertyInfo property in properties) {
      if (!property.CanRead) {
        continue;
      }
      if (ignored.Contains(property.Name)) {
        continue;
      }
      if (!IsSimpleType(property.PropertyType)) {
        continue;
      }
      object? value = property.GetValue(obj);
      sb.AppendLine($"{property.Name}: {FormatValue(value)}");
    }
    return sb.ToString();
  }

  private static bool IsSimpleType(Type type) {
    Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
    return underlyingType.IsPrimitive || underlyingType.IsEnum || underlyingType == typeof(string) || underlyingType == typeof(decimal) || underlyingType == typeof(DateTime) || underlyingType == typeof(DateOnly) || underlyingType == typeof(TimeOnly) || underlyingType == typeof(TimeSpan) || underlyingType == typeof(Guid);
  }

  private static string FormatValue(object? value) {
    if (value is null) {
      return "(null)";
    }
    return value switch {
      string s => $"\"{s}\"",
      DateTime d => $"\"{d:dd MMM yyyy hh:mm:ss.fff}\"",
      DateOnly d => $"\"{d:dd MMM yyyy}\"",
      _ => value.ToString() ?? "(null)"
    };
  }
}