using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column of checkboxes for selecting rows, with an optional "select all" box in the header. The grid needs a
/// <c>SelectionMode</c>
/// </summary>
public class PixataGridCheckboxColumn<TItem> : PixataGridColumn<TItem> {
  /// <summary>
  /// Whether the header has a box that selects (or deselects) all the rows. Only for multiple selection. Defaults to true
  /// </summary>
  [Parameter]
  public bool SelectAll { get; set; } = true;

  /// <summary>
  /// Whether "select all" selects the rows on the current page (the default) or every row that passes the filters
  /// </summary>
  [Parameter]
  public PixataGridSelectAllMode SelectAllMode { get; set; } = PixataGridSelectAllMode.Current;

  /// <summary>
  /// When true, rows can only be selected with their checkboxes, not by clicking the row
  /// </summary>
  [Parameter]
  public bool CheckBoxOnlySelection { get; set; }

  protected override void OnParametersSet() {
    // A checkbox column is 65px wide unless you say otherwise, as in Telerik's grid, and can't be sorted or filtered
#pragma warning disable BL0005 // These are the column's own parameters
    Width ??= "65px";
    Sortable = false;
    Filterable = false;
#pragma warning restore BL0005
    base.OnParametersSet();
  }
}
