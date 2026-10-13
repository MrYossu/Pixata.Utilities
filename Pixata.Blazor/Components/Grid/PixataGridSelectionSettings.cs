using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Settings for selection. Goes inside the grid's <c>GridSettings</c>, and only has an effect if the grid has a
/// <c>SelectionMode</c>
/// </summary>
public class PixataGridSelectionSettings<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  internal PixataGrid<TItem>? Grid { get; set; }

  /// <summary>
  /// Whether the user selects rows (the default) or cells
  /// </summary>
  [Parameter]
  public PixataGridSelectionType SelectionType { get; set; } = PixataGridSelectionType.Row;

  /// <summary>
  /// Whether the user can press on a row (or cell) and drag to select a range. Only for multiple selection. Defaults to true
  /// </summary>
  [Parameter]
  public bool DragToSelect { get; set; } = true;

  protected override void OnInitialized() {
    if (Grid is null) {
      throw new InvalidOperationException("PixataGridSelectionSettings must be inside the GridSettings of a PixataGrid");
    }
    Grid.SelectionSettings = this;
  }

  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    if (Grid?.SelectionSettings == this) {
      Grid.SelectionSettings = null;
    }
    GC.SuppressFinalize(this);
  }
}
