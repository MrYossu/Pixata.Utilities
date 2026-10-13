namespace Pixata.Extensions.Grid;

/// <summary>
/// Describes the data a grid wants: which page, and (in later versions) the sorting, filtering and grouping to apply. It
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
}
