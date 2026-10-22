using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

public partial class PixataTreeView {
  /// <summary>
  /// Lets the user drag items, within the tree view, on to another tree view, or on to a <see cref="PixataGrid{TItem}"/>.
  /// The tree view doesn't move them itself, as only you know how the data is stored, so handle <see cref="OnDrop"/>
  /// </summary>
  [Parameter]
  public bool Draggable { get; set; }

  /// <summary>
  /// The least time, in milliseconds, between one <see cref="OnDrag"/> and the next. 0 (the default) raises it whenever the
  /// pointer moves
  /// </summary>
  [Parameter]
  public int DragThrottleInterval { get; set; }

  /// <summary>
  /// Raised when the user starts dragging an item. Set <see cref="PixataTreeViewDragStartEventArgs.IsCancelled"/> to stop it
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewDragStartEventArgs> OnDragStart { get; set; }

  /// <summary>
  /// Raised as the user drags an item about, as often as <see cref="DragThrottleInterval"/> allows
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewDragEventArgs> OnDrag { get; set; }

  /// <summary>
  /// Raised when the user drops an item on a tree view (this one or another) or a grid. Move the item in your data here.
  /// It isn't raised when an item is dropped on itself or one of its own children
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewDropEventArgs> OnDrop { get; set; }

  /// <summary>
  /// Raised when a drag ends, after <see cref="OnDrop"/>, wherever the item was dropped, and when the user presses Escape to
  /// cancel it. Where it wasn't dropped on a tree view or a grid, the destination properties are null
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewDragEndEventArgs> OnDragEnd { get; set; }

  private object DragSettings =>
    new {
      draggable = Draggable,
      dragStart = OnDragStart.HasDelegate,
      drag = OnDrag.HasDelegate,
      throttle = Math.Max(DragThrottleInterval, 0)
    };

  // Changes when the settings the script needs change, so it is only told when they do
  private string DragOptions =>
    $"{Draggable}|{OnDragStart.HasDelegate}|{OnDrag.HasDelegate}|{DragThrottleInterval}";

  /// <summary>
  /// Called by the script when the user starts dragging an item. Returns false if the drag was cancelled
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task<bool> DragStart(string index) {
    PixataTreeViewNode? node = Model.FindByIndex(index);
    if (node is null || node.Disabled || !Draggable) {
      return false;
    }
    PixataTreeViewDragStartEventArgs args = new() { Item = node.Item };
    await OnDragStart.InvokeAsync(args);
    return !args.IsCancelled;
  }

  /// <summary>
  /// Called by the script as the user drags an item
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task Drag(string index, DotNetObjectReference<PixataTreeView>? destination, string? destinationIndex, string? componentId, string position, double pageX, double pageY) {
    PixataTreeViewNode? node = Model.FindByIndex(index);
    if (node is null) {
      return;
    }
    PixataTreeView? tree = destination?.Value;
    await OnDrag.InvokeAsync(new() {
      Item = node.Item,
      DestinationTreeView = tree,
      DestinationItem = tree?.GetItemFromDropIndex(destinationIndex),
      DestinationIndex = destinationIndex,
      DestinationComponentId = componentId,
      DropPosition = Position(position),
      PageX = pageX,
      PageY = pageY
    });
  }

  /// <summary>
  /// Called by the script when the user drops an item, or cancels the drag
  /// </summary>
  /// <param name="index">The position of the dragged item in this tree</param>
  /// <param name="destination">The tree view it was dropped on, if any</param>
  /// <param name="destinationIndex">The position of the item or grid row it was dropped on, if any</param>
  /// <param name="componentId">The id of the tree view or grid it was dropped on, if any</param>
  /// <param name="position">Before, Over or After</param>
  /// <param name="dropped">False if it was dropped somewhere it can't go, or the drag was cancelled</param>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task Drop(string index, DotNetObjectReference<PixataTreeView>? destination, string? destinationIndex, string? componentId, string position, bool dropped) {
    PixataTreeViewNode? node = Model.FindByIndex(index);
    if (node is null) {
      return;
    }
    PixataTreeView? tree = dropped ? destination?.Value : null;
    object? destinationItem = tree?.GetItemFromDropIndex(destinationIndex);
    PixataTreeViewDropPosition dropPosition = Position(position);
    if (dropped) {
      await OnDrop.InvokeAsync(new() {
        Item = node.Item,
        DestinationTreeView = tree,
        DestinationItem = destinationItem,
        DestinationIndex = destinationIndex,
        DestinationComponentId = componentId,
        DropPosition = dropPosition
      });
    }
    await OnDragEnd.InvokeAsync(new() {
      Item = node.Item,
      DestinationTreeView = tree,
      DestinationItem = destinationItem,
      DestinationIndex = dropped ? destinationIndex : null,
      DestinationComponentId = dropped ? componentId : null,
      DropPosition = dropPosition
    });
    // The page has probably moved the item, so draw the tree afresh
    Rebind();
    if (tree is not null && tree != this) {
      tree.Rebind();
    }
  }

  private static PixataTreeViewDropPosition Position(string? position) =>
    Enum.TryParse(position, out PixataTreeViewDropPosition dropPosition) ? dropPosition : PixataTreeViewDropPosition.Over;
}
