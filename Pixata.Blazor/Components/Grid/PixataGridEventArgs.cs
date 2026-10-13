using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// What <c>OnRowClick</c>, <c>OnRowDoubleClick</c> and <c>OnRowContextMenu</c> are given
/// </summary>
public class PixataGridRowClickEventArgs<TItem>(TItem item, MouseEventArgs eventArgs, string? field) {
  /// <summary>
  /// The row's item
  /// </summary>
  public TItem Item { get; } = item;

  /// <summary>
  /// The mouse event, with the position and the keys that were held down, for showing a context menu and so on
  /// </summary>
  public MouseEventArgs EventArgs { get; } = eventArgs;

  /// <summary>
  /// The field of the column that was clicked, or null if it was a column without a field, or a row template
  /// </summary>
  public string? Field { get; } = field;
}

/// <summary>
/// What <c>OnRowRender</c> is given. Set <see cref="Class"/> to add a CSS class to the row
/// </summary>
public class PixataGridRowRenderEventArgs<TItem>(TItem item) {
  public TItem Item { get; } = item;

  public string? Class { get; set; }
}

/// <summary>
/// What a column's <c>OnCellRender</c> is given. Set <see cref="Class"/> to add a CSS class to the cell
/// </summary>
public class PixataGridCellRenderEventArgs<TItem>(TItem item, object? value) {
  public TItem Item { get; } = item;

  /// <summary>
  /// The cell's value, before it's formatted
  /// </summary>
  public object? Value { get; } = value;

  public string? Class { get; set; }
}
