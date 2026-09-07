using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

/// <summary>
/// Repairs a grid state that has been round-tripped through JSON, eg when restoring it from local storage.
/// <para>
/// Telerik serialises <c>FilterDescriptor.Value</c> as a bare JSON value, and doesn't serialise <c>MemberType</c> at
/// all. On the way back, the value is therefore given the narrowest CLR type that will hold it, rather than the type of
/// the member being filtered. An <c>enum</c> or an <c>int</c> comes back as a <c>short</c>, a <c>double</c> comes back
/// as a <c>decimal</c>, and a <c>Guid</c> or <c>TimeSpan</c> comes back as a <c>string</c>. As the Telerik filter
/// editors cast the value to the member's own type, rendering the filter cell for a restored filter throws (eg "Unable
/// to cast object of type 'System.Int16' to type 'System.Int32'"), which kills the circuit and leaves the page stuck on
/// its loading indicator
/// </para>
/// </summary>
public static class TelerikGridStateHelper {
  /// <summary>
  /// Converts each filter value in the state back to the type of the member it filters, and restores the
  /// <c>MemberType</c> lost in serialisation. Filters on members that no longer exist on <c>TItem</c> are dropped, as
  /// they would throw when the grid reads its data. Anything that can't be repaired is dropped rather than left to
  /// bring the page down
  /// </summary>
  /// <param name="state">The grid state to repair, modified in place</param>
  public static void RepairFilterDescriptors<TItem>(GridState<TItem> state) {
    if (state.FilterDescriptors is not null) {
      RepairCollection<TItem>(state.FilterDescriptors);
    }
    if (state.SearchFilter is not null && !Repair<TItem>(state.SearchFilter)) {
      state.SearchFilter = null;
    }
  }

  private static void RepairCollection<TItem>(ICollection<IFilterDescriptor> filters) {
    List<IFilterDescriptor> doomed = filters.Where(f => !Repair<TItem>(f)).ToList();
    foreach (IFilterDescriptor filter in doomed) {
      filters.Remove(filter);
    }
  }

  /// <summary>
  /// Repairs a single descriptor (or, recursively, the contents of a composite one), returning false if it should be
  /// dropped from the state
  /// </summary>
  private static bool Repair<TItem>(IFilterDescriptor filter) {
    switch (filter) {
      case CompositeFilterDescriptor composite:
        RepairCollection<TItem>(composite.FilterDescriptors);
        // An empty composite is how the filter row represents an unfiltered column, so it's kept
        return true;
      case FilterDescriptor descriptor:
        return RepairDescriptor<TItem>(descriptor);
      default:
        return true;
    }
  }

  private static bool RepairDescriptor<TItem>(FilterDescriptor descriptor) {
    if (string.IsNullOrWhiteSpace(descriptor.Member)) {
      return false;
    }
    Type? memberType = GetMemberType(typeof(TItem), descriptor.Member!);
    if (memberType is null) {
      // The member has been renamed or removed since the state was saved, so the filter can no longer be applied
      return false;
    }
    descriptor.MemberType = memberType;
    if (descriptor.Value is null) {
      return true;
    }
    try {
      descriptor.Value = ConvertValue(descriptor.Value, memberType);
      return true;
    }
    catch (Exception) {
      // The saved value can't be made to fit the member, eg an enum member whose values have changed since it was saved
      return false;
    }
  }

  private static object ConvertValue(object value, Type memberType) {
    Type targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;
    if (targetType.IsInstanceOfType(value)) {
      return value;
    }
    if (targetType.IsEnum) {
      return value is string enumName ? Enum.Parse(targetType, enumName, true) : Enum.ToObject(targetType, value);
    }
    if (value is string text) {
      if (targetType == typeof(Guid)) {
        return Guid.Parse(text);
      }
      if (targetType == typeof(TimeSpan)) {
        return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
      }
      if (targetType == typeof(DateOnly)) {
        return DateOnly.Parse(text, CultureInfo.InvariantCulture);
      }
      if (targetType == typeof(TimeOnly)) {
        return TimeOnly.Parse(text, CultureInfo.InvariantCulture);
      }
      if (targetType == typeof(DateTimeOffset)) {
        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
      }
    }
    if (value is DateTime dateTime) {
      if (targetType == typeof(DateOnly)) {
        return DateOnly.FromDateTime(dateTime);
      }
      if (targetType == typeof(TimeOnly)) {
        return TimeOnly.FromDateTime(dateTime);
      }
      if (targetType == typeof(DateTimeOffset)) {
        return new DateTimeOffset(dateTime);
      }
    }
    return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Returns the type of the (possibly nested, eg "Speaker.Name") member on the grid's item type, or null if there is
  /// no such member
  /// </summary>
  private static Type? GetMemberType(Type itemType, string member) {
    Type? currentType = itemType;
    foreach (string part in member.Split('.')) {
      if (currentType is null) {
        return null;
      }
      currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;
      PropertyInfo? property = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
      if (property is not null) {
        currentType = property.PropertyType;
        continue;
      }
      FieldInfo? field = currentType.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
      if (field is null) {
        return null;
      }
      currentType = field.FieldType;
    }
    return currentType;
  }
}
