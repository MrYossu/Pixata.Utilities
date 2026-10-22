using System;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base of the arguments the <see cref="PixataTreeView"/> events are given
/// </summary>
public abstract class PixataTreeViewEventArgsBase {
  /// <summary>
  /// The data item. Cast it to your model type (which may differ between levels of the tree)
  /// </summary>
  public object Item { get; init; } = null!;

  /// <summary>
  /// Kept for code written for other tree views. The tree view always redraws after its events, so there is no need to set it
  /// </summary>
  public bool ShouldRender { get; set; }
}

/// <summary>
/// What <see cref="PixataTreeView.OnExpand"/> is given
/// </summary>
public class PixataTreeViewExpandEventArgs : PixataTreeViewEventArgsBase {
  /// <summary>
  /// True if the item is being expanded, false if it is being collapsed
  /// </summary>
  public bool Expanded { get; init; }
}

/// <summary>
/// What <see cref="PixataTreeView.OnItemClick"/> is given
/// </summary>
public class PixataTreeViewItemClickEventArgs : PixataTreeViewEventArgsBase {
  /// <summary>
  /// The browser event: a <see cref="Microsoft.AspNetCore.Components.Web.MouseEventArgs"/> for a click or tap, or a
  /// <see cref="Microsoft.AspNetCore.Components.Web.KeyboardEventArgs"/> when the user pressed Enter
  /// </summary>
  public EventArgs EventArgs { get; init; } = EventArgs.Empty;
}

/// <summary>
/// What <see cref="PixataTreeView.OnItemDoubleClick"/> is given
/// </summary>
public class PixataTreeViewItemDoubleClickEventArgs : PixataTreeViewEventArgsBase {
  /// <summary>
  /// The browser event, a <see cref="Microsoft.AspNetCore.Components.Web.MouseEventArgs"/>
  /// </summary>
  public EventArgs EventArgs { get; init; } = EventArgs.Empty;
}

/// <summary>
/// What <see cref="PixataTreeView.OnItemContextMenu"/> is given
/// </summary>
public class PixataTreeViewItemContextMenuEventArgs : PixataTreeViewEventArgsBase {
  /// <summary>
  /// The browser event, a <see cref="Microsoft.AspNetCore.Components.Web.MouseEventArgs"/>. Its <c>ClientX</c> and
  /// <c>ClientY</c> say where to show a context menu
  /// </summary>
  public EventArgs EventArgs { get; init; } = EventArgs.Empty;
}

/// <summary>
/// What <see cref="PixataTreeView.OnItemRender"/> is given
/// </summary>
public class PixataTreeViewItemRenderEventArgs {
  /// <summary>
  /// The data item being drawn
  /// </summary>
  public object Item { get; init; } = null!;

  /// <summary>
  /// Set this to add CSS classes to the item (to the element that holds its arrow, checkbox and text, not its children)
  /// </summary>
  public string? Class { get; set; }
}

/// <summary>
/// What <see cref="PixataTreeView.OnDragStart"/> is given
/// </summary>
public class PixataTreeViewDragStartEventArgs : PixataTreeViewEventArgsBase {
  /// <summary>
  /// Set this to true to stop the item being dragged
  /// </summary>
  public bool IsCancelled { get; set; }
}

/// <summary>
/// Where a dragged item is (or was) over. The base of the arguments of the drag events
/// </summary>
public abstract class PixataTreeViewDragEventArgsBase {
  /// <summary>
  /// The tree view item the dragged item is over, or null if it isn't over one
  /// </summary>
  public object? DestinationItem { get; init; }

  /// <summary>
  /// The tree view the dragged item is over, which may be this one, or null if it isn't over a tree view
  /// </summary>
  public PixataTreeView? DestinationTreeView { get; init; }

  /// <summary>
  /// Where the dragged item is in the destination. For a tree view, it is the item's position in the tree, as the index of
  /// each level separated by underscores (eg <c>"0_2"</c> for the third child of the first root item), which
  /// <see cref="PixataTreeView.GetItemFromDropIndex"/> turns back into the item. For a <see cref="PixataGrid{TItem}"/>, it
  /// is the position of the row among the rows the grid is showing. Null when it isn't over an item or a row
  /// </summary>
  public string? DestinationIndex { get; init; }

  /// <summary>
  /// The <c>id</c> of the tree view or grid the dragged item is over, or null if it isn't over one
  /// </summary>
  public string? DestinationComponentId { get; init; }

  /// <summary>
  /// Whether the dragged item is before, over or after <see cref="DestinationItem"/> (or the grid row)
  /// </summary>
  public PixataTreeViewDropPosition DropPosition { get; init; }
}

/// <summary>
/// What <see cref="PixataTreeView.OnDrag"/> is given
/// </summary>
public class PixataTreeViewDragEventArgs : PixataTreeViewDragEventArgsBase {
  /// <summary>
  /// The item being dragged
  /// </summary>
  public object Item { get; init; } = null!;

  /// <summary>
  /// The horizontal position of the pointer, relative to the page
  /// </summary>
  public double PageX { get; init; }

  /// <summary>
  /// The vertical position of the pointer, relative to the page
  /// </summary>
  public double PageY { get; init; }
}

/// <summary>
/// What <see cref="PixataTreeView.OnDrop"/> is given
/// </summary>
public class PixataTreeViewDropEventArgs : PixataTreeViewDragEventArgsBase {
  /// <summary>
  /// The item that was dragged
  /// </summary>
  public object Item { get; init; } = null!;
}

/// <summary>
/// What <see cref="PixataTreeView.OnDragEnd"/> is given
/// </summary>
public class PixataTreeViewDragEndEventArgs : PixataTreeViewDragEventArgsBase {
  /// <summary>
  /// The item that was dragged
  /// </summary>
  public object Item { get; init; } = null!;
}
