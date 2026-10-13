using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Pixata.Extensions.Grid;

public static class GridGroupingExtensions {
  /// <summary>
  /// Calculates the requested aggregates over a query, keyed by field. Each one is a separate query, so on EF Core they run in
  /// the database. Sum and Average only work on numbers; an aggregate that can't be calculated (such as the Min of no items)
  /// is left null
  /// </summary>
  public static Dictionary<string, GridFieldAggregates> Aggregate<T>(this IQueryable<T> query, IEnumerable<GridAggregateDescriptor> aggregates) {
    Dictionary<string, GridFieldAggregates> results = [];
    foreach (IGrouping<string, GridAggregateDescriptor> field in aggregates.GroupBy(a => a.Member)) {
      GridFieldAggregates result = new();
      LambdaExpression selector = FieldAccessor.Lambda(typeof(T), field.Key);
      foreach (GridAggregateType type in field.Select(a => a.Aggregate).Distinct()) {
        object? value = type == GridAggregateType.Count ? query.Count() : Calculate(query, selector, type);
        switch (type) {
          case GridAggregateType.Count:
            result.Count = value;
            break;
          case GridAggregateType.Sum:
            result.Sum = value;
            break;
          case GridAggregateType.Average:
            result.Average = value;
            break;
          case GridAggregateType.Min:
            result.Min = value;
            break;
          case GridAggregateType.Max:
            result.Max = value;
            break;
        }
      }
      results[field.Key] = result;
    }
    return results;
  }

  // Calls Queryable.Sum, Average, Min or Max with the field's selector
  private static object? Calculate<T>(IQueryable<T> query, LambdaExpression selector, GridAggregateType type) {
    Type resultType = selector.ReturnType;
    MethodInfo? method = type switch {
      GridAggregateType.Sum or GridAggregateType.Average => typeof(Queryable).GetMethods()
        .Where(m => m.Name == type.ToString() && m.GetParameters().Length == 2)
        .Select(m => m.MakeGenericMethod(typeof(T)))
        .FirstOrDefault(m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments()[1] == resultType),
      _ => typeof(Queryable).GetMethods()
        .First(m => m.Name == type.ToString() && m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 2)
        .MakeGenericMethod(typeof(T), resultType)
    };
    if (method is null) {
      return null;
    }
    try {
      return method.Invoke(null, [query, selector]);
    } catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) {
      // Min, Max and Average of no items
      return null;
    }
  }

  /// <summary>
  /// Groups a page of items. The counts and aggregates are for each whole group, so they're calculated from
  /// <paramref name="all"/>, which should be the filtered query, rather than from the page
  /// </summary>
  /// <param name="all">The data, filtered but not paged</param>
  /// <param name="page">The items on the page, already in group order</param>
  /// <param name="groups">The fields to group by</param>
  /// <param name="aggregates">The aggregates wanted for each group</param>
  public static List<GridGroup<T>> GroupPage<T>(IQueryable<T> all, List<T> page, IReadOnlyList<GridGroupDescriptor> groups, IReadOnlyList<GridAggregateDescriptor> aggregates) =>
    Group(all, page, groups, 0, aggregates);

  private static List<GridGroup<T>> Group<T>(IQueryable<T> all, List<T> items, IReadOnlyList<GridGroupDescriptor> groups, int level, IReadOnlyList<GridAggregateDescriptor> aggregates) {
    if (level >= groups.Count) {
      return [];
    }
    string member = groups[level].Member;
    Func<T, object?> getter = FieldAccessor.Getter<T>(member);
    List<GridGroup<T>> result = [];
    // The items are already sorted by the group fields, so each group's items are together, in the right order
    foreach (IGrouping<object?, T> group in items.GroupBy(getter)) {
      IQueryable<T> inGroup = all.Where(Equals<T>(member, group.Key));
      List<T> groupItems = [.. group];
      result.Add(new() {
        Member = member,
        Key = group.Key,
        Count = inGroup.Count(),
        Items = level == groups.Count - 1 ? groupItems : [],
        Subgroups = Group(inGroup, groupItems, groups, level + 1, aggregates),
        Aggregates = aggregates.Count == 0 ? [] : inGroup.Aggregate(aggregates)
      });
    }
    return result;
  }

  // item => item.Member == key, null-safe for nested fields, as the filters are
  private static Expression<Func<T, bool>> Equals<T>(string member, object? key) {
    ParameterExpression item = Expression.Parameter(typeof(T), "item");
    LambdaExpression selector = FieldAccessor.Lambda(typeof(T), member);
    Expression body = new ReplaceParameter(selector.Parameters[0], item).Visit(selector.Body);
    return Expression.Lambda<Func<T, bool>>(Expression.Equal(body, Expression.Constant(GridFilterExpressions.ConvertValue(key, body.Type), body.Type)), item);
  }

  private sealed class ReplaceParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor {
    protected override Expression VisitParameter(ParameterExpression node) =>
      node == from ? to : base.VisitParameter(node);
  }
}
