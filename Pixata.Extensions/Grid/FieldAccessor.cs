using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Pixata.Extensions.Grid;

/// <summary>
/// Reads a (possibly nested) field from an object, given its path as a string, such as "Address.City". This is how grid
/// columns get their values from a <c>Field</c> name. The getters are compiled once per type and path, and cached, so reading
/// a value is about as fast as accessing the property directly
/// </summary>
public static class FieldAccessor {
  private static readonly ConcurrentDictionary<(Type, string), Delegate> Getters = new();

  /// <summary>
  /// Gets a function that reads the field from an item. If anything along the path is null, the function returns null,
  /// rather than throwing a <see cref="NullReferenceException"/>
  /// </summary>
  /// <param name="field">The field path, such as "Name" or "Address.City". This is case sensitive</param>
  /// <exception cref="ArgumentException">Thrown if the path doesn't match public properties or fields of the type</exception>
  public static Func<T, object?> Getter<T>(string field) =>
    (Func<T, object?>)Getters.GetOrAdd((typeof(T), field), _ => BuildGetter<T>(field));

  /// <summary>
  /// Sets the field on an item, converting the value to the field's type (so a <c>decimal</c> from an editor can set an
  /// <c>int</c> property, for example). For a nested path, the objects along the way must already exist; if one is null,
  /// nothing is set
  /// </summary>
  /// <param name="item">The object to change</param>
  /// <param name="field">The field path, such as "Name" or "Address.City". This is case sensitive</param>
  /// <param name="value">The new value</param>
  /// <exception cref="ArgumentException">Thrown if the path doesn't match public properties or fields of the type</exception>
  /// <exception cref="InvalidOperationException">Thrown if the property has no public setter</exception>
  public static void SetValue<T>(T item, string field, object? value) {
    MemberInfo member = Member(typeof(T), field);
    string[] parts = Split(field);
    object? target = parts.Length == 1 ? item : Getter<T>(string.Join('.', parts[..^1]))(item);
    if (target is null) {
      return;
    }
    object? converted = GridFilterExpressions.ConvertValue(value, MemberType(member));
    switch (member) {
      case PropertyInfo property when property.SetMethod?.IsPublic == true:
        property.SetValue(target, converted);
        break;
      case FieldInfo fieldInfo when !fieldInfo.IsInitOnly:
        fieldInfo.SetValue(target, converted);
        break;
      default:
        throw new InvalidOperationException($"'{field}' on {typeof(T).Name} can't be set, as it has no public setter");
    }
  }

  /// <summary>
  /// Whether the field at the end of the path can be set, which is false for a property with no public setter, or a
  /// read-only field
  /// </summary>
  public static bool CanWrite(Type type, string field) => Member(type, field) switch {
    PropertyInfo property => property.SetMethod?.IsPublic == true,
    FieldInfo fieldInfo => !fieldInfo.IsInitOnly,
    _ => false
  };

  /// <summary>
  /// Gets the property or field at the end of the path, for reading its type or attributes
  /// </summary>
  /// <param name="type">The type that the path starts from</param>
  /// <param name="field">The field path, such as "Name" or "Address.City". This is case sensitive</param>
  /// <exception cref="ArgumentException">Thrown if the path doesn't match public properties or fields of the type</exception>
  public static MemberInfo Member(Type type, string field) {
    MemberInfo? member = null;
    Type current = type;
    foreach (string part in Split(field)) {
      member = (MemberInfo?)current.GetProperty(part, BindingFlags.Public | BindingFlags.Instance)
               ?? current.GetField(part, BindingFlags.Public | BindingFlags.Instance)
               ?? throw new ArgumentException($"'{field}' is not a valid field path for {type.Name}, as {current.Name} has no public property or field called '{part}'. Field names are case sensitive", nameof(field));
      current = MemberType(member);
    }
    return member!;
  }

  /// <summary>
  /// Gets the type of the property or field at the end of the path
  /// </summary>
  /// <param name="type">The type that the path starts from</param>
  /// <param name="field">The field path, such as "Name" or "Address.City". This is case sensitive</param>
  /// <exception cref="ArgumentException">Thrown if the path doesn't match public properties or fields of the type</exception>
  public static Type FieldType(Type type, string field) =>
    MemberType(Member(type, field));

  /// <summary>
  /// Builds a lambda that reads the field, such as <c>item =&gt; item.Address.City</c>, for use in a LINQ query (it works
  /// with EF Core). If anything along a nested path can be null, the lambda checks for it and returns null, so a value type
  /// at the end of such a path comes back as nullable
  /// </summary>
  /// <param name="type">The type that the path starts from</param>
  /// <param name="field">The field path, such as "Name" or "Address.City". This is case sensitive</param>
  /// <exception cref="ArgumentException">Thrown if the path doesn't match public properties or fields of the type</exception>
  public static LambdaExpression Lambda(Type type, string field) {
    Member(type, field);
    ParameterExpression item = Expression.Parameter(type, "item");
    string[] parts = Split(field);
    Type resultType = FieldType(type, field);
    // Only intermediate objects are checked for null. The item itself isn't, as a query never contains a null item
    bool nullable = parts.Length > 1 && Enumerable.Range(1, parts.Length - 1).Any(i => CanBeNull(FieldType(type, string.Join('.', parts.Take(i)))));
    if (nullable && !CanBeNull(resultType)) {
      resultType = typeof(Nullable<>).MakeGenericType(resultType);
    }
    return Expression.Lambda(BuildTyped(item, parts, 0, resultType), item);
  }

  private static Expression BuildTyped(Expression target, string[] parts, int index, Type resultType) {
    Expression member = Expression.MakeMemberAccess(target, Member(target.Type, parts[index]));
    Expression rest = index == parts.Length - 1
      ? (member.Type == resultType ? member : Expression.Convert(member, resultType))
      : BuildTyped(member, parts, index + 1, resultType);
    return index > 0 && CanBeNull(target.Type)
      ? Expression.Condition(Expression.Equal(target, Expression.Constant(null, target.Type)), Expression.Constant(null, resultType), rest)
      : rest;
  }

  private static Func<T, object?> BuildGetter<T>(string field) {
    // Checks the whole path first, so that a bad one gets an error message that describes the path, not just one part of it
    Member(typeof(T), field);
    ParameterExpression item = Expression.Parameter(typeof(T), "item");
    Expression body = Build(item, Split(field), 0);
    return Expression.Lambda<Func<T, object?>>(body, item).Compile();
  }

  // Builds item.A.B.C as nested conditionals, so that a null anywhere along the way gives null
  private static Expression Build(Expression target, string[] parts, int index) {
    Expression member = Expression.MakeMemberAccess(target, Member(target.Type, parts[index]));
    Expression rest = index == parts.Length - 1
      ? Expression.Convert(member, typeof(object))
      : Build(member, parts, index + 1);
    return CanBeNull(target.Type)
      ? Expression.Condition(Expression.Equal(target, Expression.Constant(null, target.Type)), Expression.Constant(null, typeof(object)), rest)
      : rest;
  }

  private static string[] Split(string field) {
    if (string.IsNullOrWhiteSpace(field)) {
      throw new ArgumentException("The field path can't be empty", nameof(field));
    }
    return field.Split('.');
  }

  private static Type MemberType(MemberInfo member) =>
    member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;

  private static bool CanBeNull(Type type) =>
    !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
