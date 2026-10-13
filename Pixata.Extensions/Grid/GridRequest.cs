using System.Collections.Generic;

namespace Pixata.Extensions.Grid;

/// <summary>
/// Describes the data a grid wants: which page, how to sort, filter and group it, and which aggregates to calculate. It
/// contains nothing but data, so it serialises cleanly, and can be sent from a WASM client to an API, whose server applies it
/// to a query with <see cref="GridQueryExtensions.ToGridResult{T}(System.Linq.IQueryable{T}, GridRequest)"/>
/// </summary>
public record GridRequest {
  /// <summary>
  /// The 1-based page number. Ignored when <see cref="PageSize"/> is zero
  /// </summary>
  public int Page { get; init; } = 1;

  /// <summary>
  /// The number of items on a page. Zero means no paging, so every item is returned
  /// </summary>
  public int PageSize { get; init; }

  /// <summary>
  /// The sorts to apply, in order of priority
  /// </summary>
  public List<GridSortDescriptor> Sorts { get; init; } = [];

  /// <summary>
  /// The column filters. An item must pass all of them
  /// </summary>
  public List<GridCompositeFilterDescriptor> Filters { get; init; } = [];

  /// <summary>
  /// The search box's filter, if the user has typed anything in it. It is kept apart from <see cref="Filters"/>, as it is
  /// in Telerik's grid, but an item must pass it as well
  /// </summary>
  public GridCompositeFilterDescriptor? Search { get; init; }

  /// <summary>
  /// The fields to group by, outermost first
  /// </summary>
  public List<GridGroupDescriptor> Groups { get; init; } = [];

  /// <summary>
  /// The aggregates to calculate, for the whole data and for each group
  /// </summary>
  public List<GridAggregateDescriptor> Aggregates { get; init; } = [];
}
