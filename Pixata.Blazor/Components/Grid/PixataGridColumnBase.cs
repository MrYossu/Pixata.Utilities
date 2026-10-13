using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// The base for everything that can go inside a grid's <c>GridColumns</c>. These components render nothing themselves. They
/// register with the grid when they are created, and the grid reads their parameters when it renders
/// </summary>
public abstract class PixataGridColumnBase<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  internal PixataGrid<TItem>? Grid { get; set; }

  protected override void OnInitialized() {
    if (Grid is null) {
      throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} must be inside the GridColumns of a PixataGrid");
    }
    Grid.AddColumn(this);
  }

  // The grid re-renders whenever its GridColumns do, and it reads the column parameters then, so the columns never need to
  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    Grid?.RemoveColumn(this);
    GC.SuppressFinalize(this);
  }
}
