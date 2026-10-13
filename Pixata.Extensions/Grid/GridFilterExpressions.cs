using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Pixata.Extensions.Grid;

/// <summary>
/// Turns grid filters into expression trees, so they can be applied to in-memory data or translated to SQL by EF Core
/// </summary>
public static class GridFilterExpressions {
  private static readonly MethodInfo ToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
  private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
  private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
  private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
  private static readonly MethodInfo CompareMethod = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

  /// <summary>
  /// Builds a predicate that is true for items that pass every composite filter. Within each composite, its filters are
  /// combined with its own AND or OR. Returns null if there's nothing to filter on
  /// </summary>
  public static Expression<Func<T, bool>>? Predicate<T>(IEnumerable<GridCompositeFilterDescriptor> composites) {
    ParameterExpression item = Expression.Parameter(typeof(T), "item");
    Expression? body = null;
    foreach (GridCompositeFilterDescriptor composite in composites) {
      Expression? part = Composite(item, composite);
      if (part is not null) {
        body = body is null ? part : Expression.AndAlso(body, part);
      }
    }
    return body is null ? null : Expression.Lambda<Func<T, bool>>(body, item);
  }

  private static Expression? Composite(ParameterExpression item, GridCompositeFilterDescriptor composite) {
    Expression? body = null;
    foreach (GridFilterDescriptor filter in composite.Filters) {
      Expression part = Filter(item, filter);
      body = body is null ? part : composite.Logic == GridFilterLogic.And ? Expression.AndAlso(body, part) : Expression.OrElse(body, part);
    }
    return body;
  }

  private static Expression Filter(ParameterExpression item, GridFilterDescriptor filter) {
    // The member access is null-safe for nested fields, as it is for sorting
    LambdaExpression lambda = FieldAccessor.Lambda(item.Type, filter.Member);
    Expression member = new ReplaceParameter(lambda.Parameters[0], item).Visit(lambda.Body);
    return member.Type == typeof(string) ? StringFilter(member, filter) : ValueFilter(member, filter);
  }

  // String comparisons ignore case, as Telerik's do. ToLower() is used rather than a StringComparison, because EF Core can
  // translate it to SQL
  private static Expression StringFilter(Expression member, GridFilterDescriptor filter) {
    ConstantExpression nullString = Expression.Constant(null, typeof(string));
    Expression isNull = Expression.Equal(member, nullString);
    Expression notNull = Expression.NotEqual(member, nullString);
    ConstantExpression empty = Expression.Constant("");
    if (filter.Operator == GridFilterOperator.IsContainedIn) {
      return Contained(member, filter.Value);
    }
    string text = (string?)ConvertValue(filter.Value, typeof(string)) ?? "";
    Expression lower = Expression.Call(member, ToLowerMethod);
    ConstantExpression value = Expression.Constant(text.ToLower());
    Expression Call(MethodInfo method) =>
      Expression.AndAlso(notNull, Expression.Call(lower, method, value));
    Expression Compare(Func<Expression, Expression, BinaryExpression> comparison) =>
      Expression.AndAlso(notNull, comparison(Expression.Call(CompareMethod, member, Expression.Constant(text)), Expression.Constant(0)));
    return filter.Operator switch {
      GridFilterOperator.Contains => Call(ContainsMethod),
      GridFilterOperator.DoesNotContain => Expression.OrElse(isNull, Expression.Not(Expression.Call(lower, ContainsMethod, value))),
      GridFilterOperator.StartsWith => Call(StartsWithMethod),
      GridFilterOperator.EndsWith => Call(EndsWithMethod),
      GridFilterOperator.IsEqualTo => Expression.AndAlso(notNull, Expression.Equal(lower, value)),
      GridFilterOperator.IsNotEqualTo => Expression.OrElse(isNull, Expression.NotEqual(lower, value)),
      GridFilterOperator.IsNull => isNull,
      GridFilterOperator.IsNotNull => notNull,
      GridFilterOperator.IsEmpty => Expression.Equal(member, empty),
      GridFilterOperator.IsNotEmpty => Expression.NotEqual(member, empty),
      GridFilterOperator.IsNullOrEmpty => Expression.OrElse(isNull, Expression.Equal(member, empty)),
      GridFilterOperator.IsNotNullOrEmpty => Expression.AndAlso(notNull, Expression.NotEqual(member, empty)),
      GridFilterOperator.IsLessThan => Compare(Expression.LessThan),
      GridFilterOperator.IsLessThanOrEqualTo => Compare(Expression.LessThanOrEqual),
      GridFilterOperator.IsGreaterThan => Compare(Expression.GreaterThan),
      GridFilterOperator.IsGreaterThanOrEqualTo => Compare(Expression.GreaterThanOrEqual),
      _ => throw new ArgumentOutOfRangeException(nameof(filter), filter.Operator, null)
    };
  }

  private static Expression ValueFilter(Expression member, GridFilterDescriptor filter) {
    Type type = member.Type;
    bool nullable = !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    switch (filter.Operator) {
      case GridFilterOperator.IsNull or GridFilterOperator.IsNullOrEmpty:
        return nullable ? Expression.Equal(member, Expression.Constant(null, type)) : Expression.Constant(false);
      case GridFilterOperator.IsNotNull or GridFilterOperator.IsNotNullOrEmpty:
        return nullable ? Expression.NotEqual(member, Expression.Constant(null, type)) : Expression.Constant(true);
      case GridFilterOperator.IsEmpty:
        return Expression.Constant(false);
      case GridFilterOperator.IsNotEmpty:
        return Expression.Constant(true);
      case GridFilterOperator.IsContainedIn:
        return Contained(member, filter.Value);
    }
    ConstantExpression value = Expression.Constant(ConvertValue(filter.Value, type), type);
    // Enums can only be compared for equality in an expression, so ordering comparisons use their underlying number
    Expression left = member;
    Expression right = value;
    Type underlying = Nullable.GetUnderlyingType(type) ?? type;
    if (underlying.IsEnum && filter.Operator is not (GridFilterOperator.IsEqualTo or GridFilterOperator.IsNotEqualTo)) {
      Type number = Enum.GetUnderlyingType(underlying);
      Type target = type == underlying ? number : typeof(Nullable<>).MakeGenericType(number);
      left = Expression.Convert(member, target);
      right = Expression.Convert(value, target);
    }
    return filter.Operator switch {
      GridFilterOperator.IsEqualTo => Expression.Equal(left, right),
      GridFilterOperator.IsNotEqualTo => Expression.NotEqual(left, right),
      GridFilterOperator.IsLessThan => Expression.LessThan(left, right),
      GridFilterOperator.IsLessThanOrEqualTo => Expression.LessThanOrEqual(left, right),
      GridFilterOperator.IsGreaterThan => Expression.GreaterThan(left, right),
      GridFilterOperator.IsGreaterThanOrEqualTo => Expression.GreaterThanOrEqual(left, right),
      _ => throw new ArgumentException($"The {filter.Operator} filter can only be used on text, not on {underlying.Name} ({filter.Member})", nameof(filter))
    };
  }

  // values.Contains(member), with the values converted to the member's type
  private static Expression Contained(Expression member, object? values) {
    Type listType = typeof(List<>).MakeGenericType(member.Type);
    IList list = (IList)Activator.CreateInstance(listType)!;
    IEnumerable source = values switch {
      JsonElement { ValueKind: JsonValueKind.Array } json => json.EnumerateArray().Cast<object>(),
      string s => new[] { s },
      IEnumerable enumerable => enumerable,
      null => Array.Empty<object>(),
      _ => new[] { values }
    };
    foreach (object? value in source) {
      list.Add(ConvertValue(value, member.Type));
    }
    MethodInfo contains = typeof(Enumerable).GetMethods().First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2).MakeGenericMethod(member.Type);
    return Expression.Call(contains, Expression.Constant(list), member);
  }

  /// <summary>
  /// Converts a filter value to the field's type. Handles values that have been through JSON (which arrive as
  /// <see cref="JsonElement"/>), enums given as names or numbers, and dates given as <see cref="DateTime"/> for a
  /// <see cref="DateOnly"/> field (and the other way round)
  /// </summary>
  public static object? ConvertValue(object? value, Type type) {
    if (value is null) {
      return null;
    }
    Type target = Nullable.GetUnderlyingType(type) ?? type;
    if (value is JsonElement json) {
      return json.ValueKind == JsonValueKind.Null ? null : json.Deserialize(target);
    }
    if (target.IsInstanceOfType(value)) {
      return value;
    }
    return target switch {
      _ when target == typeof(string) => Convert.ToString(value, CultureInfo.InvariantCulture),
      _ when target.IsEnum => value is string name ? Enum.Parse(target, name, true) : Enum.ToObject(target, value),
      _ when target == typeof(DateOnly) && value is DateTime dateTime => DateOnly.FromDateTime(dateTime),
      _ when target == typeof(DateTime) && value is DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
      _ when target == typeof(DateTime) && value is DateTimeOffset offset => offset.DateTime,
      _ when target == typeof(Guid) => Guid.Parse(value.ToString()!),
      _ when target == typeof(DateOnly) => DateOnly.Parse(value.ToString()!, CultureInfo.InvariantCulture),
      _ => Convert.ChangeType(value, target, CultureInfo.InvariantCulture)
    };
  }

  private sealed class ReplaceParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor {
    protected override Expression VisitParameter(ParameterExpression node) =>
      node == from ? to : base.VisitParameter(node);
  }
}
