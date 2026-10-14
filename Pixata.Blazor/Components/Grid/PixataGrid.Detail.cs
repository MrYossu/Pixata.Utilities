using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// What <c>OnRowExpand</c> is given
/// </summary>
public class PixataGridRowExpandEventArgs<TItem>(TItem item) {
  /// <summary>
  /// The item whose details are being shown
  /// </summary>
  public TItem Item { get; } = item;
}

/// <summary>
/// What <c>OnRowCollapse</c> is given
/// </summary>
public class PixataGridRowCollapseEventArgs<TItem>(TItem item) {
  /// <summary>
  /// The item whose details are being hidden
  /// </summary>
  public TItem Item { get; } = item;
}

/// <summary>
/// Where a dragged row was dropped, relative to the row it was dropped on
/// </summary>
public enum PixataGridDropPosition {
  Before,
  Over,
  After
}

/// <summary>
/// What <c>OnRowDrop</c> is given
/// </summary>
public class PixataGridRowDropEventArgs<TItem> {
  /// <summary>
  /// The row that was dragged
  /// </summary>
  public TItem Item { get; init; } = default!;

  /// <summary>
  /// The rows that were dragged: the selected rows if the dragged row was one of them, otherwise just that row
  /// </summary>
  public List<TItem> Items { get; init; } = [];

  /// <summary>
  /// The grid the rows were dropped on, which may be this one, or null if they were dropped somewhere else
  /// </summary>
  public PixataGrid<TItem>? DestinationGrid { get; init; }

  /// <summary>
  /// The row they were dropped on, or null if they were dropped on the grid but not on a row (such as an empty grid)
  /// </summary>
  public TItem? DestinationItem { get; init; }

  /// <summary>
  /// The position of <see cref="DestinationItem"/> among the rows the destination grid is showing
  /// </summary>
  public int? DestinationIndex { get; init; }

  /// <summary>
  /// Whether they were dropped before, over or after <see cref="DestinationItem"/>
  /// </summary>
  public PixataGridDropPosition DropPosition { get; init; }
}

/// <summary>
/// A narrow column with a handle for dragging each row, when the grid is <c>RowDraggable</c>
/// </summary>
public class PixataGridDragColumn<TItem> : PixataGridColumn<TItem> {
  protected override void OnParametersSet() {
#pragma warning disable BL0005 // These are the column's own parameters
    Sortable = false;
    Filterable = false;
    Editable = false;
    Groupable = false;
    Width ??= "36px";
#pragma warning restore BL0005
    base.OnParametersSet();
  }
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Details for each row, shown in a row of their own when the user expands the row with the arrow at its start. The context
  /// is the row's item. It can hold anything, including another grid
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? DetailTemplate { get; set; }

  /// <summary>
  /// Fires when the user expands a row to show its details, such as to load them
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridRowExpandEventArgs<TItem>> OnRowExpand { get; set; }

  /// <summary>
  /// Fires when the user collapses a row to hide its details
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridRowCollapseEventArgs<TItem>> OnRowCollapse { get; set; }

  /// <summary>
  /// Lets the user drag rows by the handle in a <see cref="PixataGridDragColumn{TItem}"/>, to move them within the grid or to
  /// another grid. Handle <see cref="OnRowDrop"/> to move them
  /// </summary>
  [Parameter]
  public bool RowDraggable { get; set; }

  /// <summary>
  /// Fires when the user drops dragged rows. The grid doesn't move them itself, as only you know how the data is stored
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridRowDropEventArgs<TItem>> OnRowDrop { get; set; }

  private List<TItem> _expanded = [];
  private bool _rowDragReady;

  // The grids on the page, so a row dragged from one can find the one it was dropped on
  private static readonly ConcurrentDictionary<string, WeakReference<PixataGrid<TItem>>> Grids = new();

  private bool HasDetail =>
    DetailTemplate is not null;

  // The narrow cells at the start of each row: one for each level of grouping, and one for the expand arrows
  private int LeadingColumns =>
    GroupLevels + (HasDetail ? 1 : 0);

  private bool IsExpanded(TItem item) =>
    _expanded.Any(e => SameItem(e, item));

  private async Task ToggleDetail(TItem item) {
    if (IsExpanded(item)) {
      _expanded.RemoveAll(e => SameItem(e, item));
      await OnRowCollapse.InvokeAsync(new(item));
    } else {
      _expanded.Add(item);
      await OnRowExpand.InvokeAsync(new(item));
    }
  }

  private async Task DetailKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e, TItem item) {
    if (e.Key is "Enter" or " ") {
      await ToggleDetail(item);
    }
  }

  // ---------- Dragging rows ----------

  private async Task InitRowDrag() {
    Grids[_gridId] = new(this);
    if (!RowDraggable || _rowDragReady || _disposed) {
      return;
    }
    _rowDragReady = true;
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      _self ??= DotNetObjectReference.Create(this);
      await _module.InvokeVoidAsync("initRowDrag", _gridId, _self);
    } catch (JSDisconnectedException) {
      // The page has gone
    }
  }

  private void ForgetGrid() =>
    Grids.TryRemove(_gridId, out _);

  /// <summary>
  /// Called by the JavaScript when the user drops a dragged row
  /// </summary>
  /// <param name="index">The dragged row's position among the rows this grid is showing</param>
  /// <param name="gridId">The id of the grid it was dropped on, if any</param>
  /// <param name="destinationIndex">The position of the row it was dropped on, or -1 if it wasn't dropped on a row</param>
  /// <param name="position">Before, Over or After</param>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task RowDrop(int index, string? gridId, int destinationIndex, string position) {
    if (index < 0 || index >= _items.Count) {
      return;
    }
    TItem item = _items[index];
    PixataGrid<TItem>? destination = gridId is not null && Grids.TryGetValue(gridId, out WeakReference<PixataGrid<TItem>>? reference) && reference.TryGetTarget(out PixataGrid<TItem>? grid) ? grid : null;
    bool hasDestinationItem = destination is not null && destinationIndex >= 0 && destinationIndex < destination._items.Count;
    PixataGridRowDropEventArgs<TItem> args = new() {
      Item = item,
      Items = SelectionMode == PixataGridSelectionMode.Multiple && IsSelected(item) ? [.. _items.Where(IsSelected)] : [item],
      DestinationGrid = destination,
      DestinationItem = hasDestinationItem ? destination!._items[destinationIndex] : default,
      DestinationIndex = hasDestinationItem ? destinationIndex : null,
      DropPosition = Enum.TryParse(position, out PixataGridDropPosition dropPosition) ? dropPosition : PixataGridDropPosition.After
    };
    await OnRowDrop.InvokeAsync(args);
    StateHasChanged();
  }
}
