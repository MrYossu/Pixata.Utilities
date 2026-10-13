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
    query = query.ApplySorts(request.Sorts);
    int total = query.Count();
    if (request.PageSize > 0) {
      query = query.Skip((Math.Max(request.Page, 1) - 1) * request.PageSize).Take(request.PageSize);
    }
    return new([.. query], total);
  }

  /// <summary>
  /// Applies a <see cref="GridRequest"/> to in-memory data
  /// </summary>
  /// <param name="data">The data</param>
  /// <param name="request">What the grid wants</param>
  /// <returns>The requested page of data, and the total number of items before paging</returns>
  public static GridResult<T> ToGridResult<T>(this IEnumerable<T> data, GridRequest request) =>
    data.AsQueryable().ToGridResult(request);

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
