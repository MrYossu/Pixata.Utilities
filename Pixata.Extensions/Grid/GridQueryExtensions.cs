using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Pixata.Extensions.Grid;

public static class GridQueryExtensions {
  /// <summary>
  /// Applies a <see cref="GridRequest"/> to a query. This only uses standard LINQ operators, so it works on an EF Core query
  /// (where it is translated to SQL) as well as on in-memory data
  /// </summary>
  /// <param name="query">The data</param>
  /// <param name="request">What the grid wants</param>
  /// <returns>The requested page of data, and the total number of items before paging</returns>
  public static GridResult<T> ToGridResult<T>(this IQueryable<T> query, GridRequest request) {
    IQueryable<T> filtered = query.ApplyFilters(request.Search is null ? request.Filters : [.. request.Filters, request.Search]);
    IQueryable<T> sorted = filtered.ApplySorts(SortsWithGroups(request));
    int total = sorted.Count();
    if (request.PageSize > 0) {
      sorted = sorted.Skip(request.ItemsToSkip).Take(request.PageSize);
    }
    return WithGroupsAndAggregates(filtered, [.. sorted], total, request);
  }

  /// <summary>
  /// The sorts to apply for a request: by the group fields first, so the items in each group are together, then by the
  /// other sorts
  /// </summary>
  public static List<GridSortDescriptor> SortsWithGroups(GridRequest request) =>
    [.. request.Groups.Select(g => new GridSortDescriptor(g.Member, g.SortDirection)), .. request.Sorts.Where(s => request.Groups.All(g => g.Member != s.Member))];

  /// <summary>
  /// Adds the groups and aggregates a request asked for to a page of data
  /// </summary>
  /// <param name="filtered">The data, filtered but not paged, for the counts and aggregates</param>
  /// <param name="page">The page of items</param>
  /// <param name="total">The number of items before paging</param>
  /// <param name="request">The request</param>
  public static GridResult<T> WithGroupsAndAggregates<T>(IQueryable<T> filtered, List<T> page, int total, GridRequest request) =>
    new(page, total) {
      Groups = request.Groups.Count == 0 ? null : GridGroupingExtensions.GroupPage(filtered, page, request.Groups, request.Aggregates),
      Aggregates = request.Aggregates.Count == 0 ? null : filtered.Aggregate(request.Aggregates)
    };

  /// <summary>
  /// Applies a <see cref="GridRequest"/> to in-memory data
  /// </summary>
  /// <param name="data">The data</param>
  /// <param name="request">What the grid wants</param>
  /// <returns>The requested page of data, and the total number of items before paging</returns>
  public static GridResult<T> ToGridResult<T>(this IEnumerable<T> data, GridRequest request) =>
    data.AsQueryable().ToGridResult(request);

  /// <summary>
  /// Filters a query, keeping the items that pass every composite filter. String comparisons ignore case, as Telerik's do
  /// </summary>
  /// <param name="query">The data</param>
  /// <param name="filters">The filters to apply</param>
  /// <returns>The filtered query</returns>
  public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<GridCompositeFilterDescriptor> filters) {
    Expression<Func<T, bool>>? predicate = GridFilterExpressions.Predicate<T>(filters);
    return predicate is null ? query : query.Where(predicate);
  }

  /// <summary>
  /// Sorts a query by each of the descriptors in turn, so the first has the highest priority. A nested field is sorted
  /// safely when part of its path is null, with nulls coming first in ascending order, as they do in LINQ and SQL Server
  /// </summary>
  /// <param name="query">The data</param>
  /// <param name="sorts">The sorts to apply</param>
  /// <returns>The sorted query</returns>
  public static IQueryable<T> ApplySorts<T>(this IQueryable<T> query, IEnumerable<GridSortDescriptor> sorts) {
    bool first = true;
    foreach (GridSortDescriptor sort in sorts) {
      LambdaExpression key = FieldAccessor.Lambda(typeof(T), sort.Member);
      string method = (first, sort.SortDirection) switch {
        (true, ListSortDirection.Ascending) => nameof(Queryable.OrderBy),
        (true, ListSortDirection.Descending) => nameof(Queryable.OrderByDescending),
        (false, ListSortDirection.Ascending) => nameof(Queryable.ThenBy),
        _ => nameof(Queryable.ThenByDescending)
      };
      query = query.Provider.CreateQuery<T>(Expression.Call(typeof(Queryable), method, [typeof(T), key.ReturnType], query.Expression, Expression.Quote(key)));
      first = false;
    }
    return query;
  }
}
