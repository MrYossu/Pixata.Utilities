using System;
using System.Collections.Generic;
using System.Linq;

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
}
