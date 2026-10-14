using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A cell to highlight: the row's item and the column's field
/// </summary>
public record PixataGridHighlightedCell<TItem>(TItem DataItem, string ColumnField);

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Rows to draw attention to, such as ones that have just changed. They are shown like selected rows, but have nothing to
  /// do with the selection
  /// </summary>
  [Parameter]
  public IEnumerable<TItem>? HighlightedItems { get; set; }

  /// <summary>
  /// Cells to draw attention to
  /// </summary>
  [Parameter]
  public IEnumerable<PixataGridHighlightedCell<TItem>>? HighlightedCells { get; set; }

  private bool IsHighlighted(TItem item) =>
    HighlightedItems?.Any(h => SameItem(h, item)) == true;

  private bool IsCellHighlighted(TItem item, string? field) =>
    field is not null && HighlightedCells?.Any(c => c.ColumnField == field && SameItem(c.DataItem, item)) == true;
}
