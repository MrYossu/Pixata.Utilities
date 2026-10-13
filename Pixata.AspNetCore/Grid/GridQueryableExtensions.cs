using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Pixata.Extensions.Grid;

namespace Pixata.AspNetCore.Grid;

public static class GridQueryableExtensions {
  /// <summary>
  /// Applies a <see cref="GridRequest"/> to an EF Core query, using EF's async methods for the count and the page, so the
  /// database calls don't block a thread. On any other kind of query, it does the same thing synchronously, as
  /// <see cref="GridQueryExtensions.ToGridResult{T}(IQueryable{T}, GridRequest)"/> does
  /// </summary>
  /// <param name="query">The data, such as <c>db.People</c></param>
  /// <param name="request">What the grid wants, such as <c>args.Request</c> in the grid's <c>OnRead</c></param>
  /// <param name="cancellationToken">Cancels the database calls</param>
  /// <returns>The requested page of data, and the total number of items before paging</returns>
  public static async Task<GridResult<T>> ToGridResultAsync<T>(this IQueryable<T> query, GridRequest request, CancellationToken cancellationToken = default) {
    if (query.Provider is not IAsyncQueryProvider) {
      return query.ToGridResult(request);
    }
    IQueryable<T> filtered = query.ApplyFilters(request.Search is null ? request.Filters : [.. request.Filters, request.Search]);
    IQueryable<T> sorted = filtered.ApplySorts(GridQueryExtensions.SortsWithGroups(request));
    int total = await sorted.CountAsync(cancellationToken);
    if (request.PageSize > 0) {
      sorted = sorted.Skip((Math.Max(request.Page, 1) - 1) * request.PageSize).Take(request.PageSize);
    }
    List<T> data = await sorted.ToListAsync(cancellationToken);
    // The group counts and aggregates are a handful of small queries, which are run synchronously
    return GridQueryExtensions.WithGroupsAndAggregates(filtered, data, total, request);
  }
}
