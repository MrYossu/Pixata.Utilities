using System;
using System.Collections.Concurrent;
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
