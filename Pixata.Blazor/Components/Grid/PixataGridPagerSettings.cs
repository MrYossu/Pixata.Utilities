using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Where the pager goes
/// </summary>
public enum PixataGridPagerPosition {
  Top,
  Bottom
}

/// <summary>
/// How the pager lets the user pick a page
/// </summary>
public enum PixataGridPagerInputType {
  /// <summary>
  /// A button for each page, in groups of <see cref="PixataGridPagerSettings{TItem}.ButtonCount"/>
  /// </summary>
  Buttons,

  /// <summary>
  /// A box where the user types the page number
  /// </summary>
  Input
}

/// <summary>
/// Settings for the grid's pager. Goes inside the grid's <c>GridSettings</c>, and only has an effect if the grid is
/// <c>Pageable</c>
/// </summary>
public class PixataGridPagerSettings<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  internal PixataGrid<TItem>? Grid { get; set; }

  /// <summary>
  /// The number of page buttons shown at once. Defaults to 10
  /// </summary>
  [Parameter]
  public int ButtonCount { get; set; } = 10;

  /// <summary>
  /// The page sizes the user can pick from. Include <c>null</c> to offer "All". If not set, there's no page size dropdown
  /// </summary>
  [Parameter]
  public List<int?>? PageSizes { get; set; }

  /// <summary>
  /// Whether the user picks a page with buttons (the default) or by typing its number
  /// </summary>
  [Parameter]
  public PixataGridPagerInputType InputType { get; set; } = PixataGridPagerInputType.Buttons;

  /// <summary>
  /// Whether the pager goes below the rows (the default) or above them
  /// </summary>
  [Parameter]
  public PixataGridPagerPosition Position { get; set; } = PixataGridPagerPosition.Bottom;

  /// <summary>
  /// Whether the pager adapts to narrow widths, by hiding the less important parts and swapping the page buttons for a
  /// dropdown. Defaults to true
  /// </summary>
  [Parameter]
  public bool Adaptive { get; set; } = true;

  protected override void OnInitialized() {
    if (Grid is null) {
      throw new InvalidOperationException("PixataGridPagerSettings must be inside the GridSettings of a PixataGrid");
    }
    Grid.PagerSettings = this;
  }

  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    if (Grid?.PagerSettings == this) {
      Grid.PagerSettings = null;
    }
    GC.SuppressFinalize(this);
  }
}
