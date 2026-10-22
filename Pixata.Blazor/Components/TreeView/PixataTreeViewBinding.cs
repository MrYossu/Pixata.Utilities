using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// Says which properties of the data items a <see cref="PixataTreeView"/> reads, and optionally how the items are drawn.
/// Declare these inside the tree view's <see cref="PixataTreeView.TreeViewBindings"/>. A binding with a <see cref="Level"/>
/// applies to the items at that depth (0 for the root items), and one without applies to every level that doesn't have its
/// own. With no bindings, the default property names are used at every level. A field that names a property the items don't
/// have turns that feature off, eg <c>IconField="None"</c> for no icons. The binding renders nothing itself
/// </summary>
public class PixataTreeViewBinding : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataTreeView? TreeView { get; set; }

  /// <summary>
  /// The depth this binding applies to, where the root items are level 0. Leave it out for a binding that applies to every
  /// level that doesn't have its own
  /// </summary>
  [Parameter]
  public int? Level { get; set; }

  /// <summary>
  /// The property that uniquely identifies an item. Needed for flat data, where <see cref="ParentIdField"/> refers to it.
  /// <c>"Id"</c> by default
  /// </summary>
  [Parameter]
  public string IdField { get; set; } = "Id";

  /// <summary>
  /// For flat data, the property holding the <see cref="IdField"/> of the item's parent, or null for a root item. If the
  /// items have this property, the data is treated as flat. <c>"ParentId"</c> by default
  /// </summary>
  [Parameter]
  public string ParentIdField { get; set; } = "ParentId";

  /// <summary>
  /// The property holding the text shown for the item. <c>"Text"</c> by default
  /// </summary>
  [Parameter]
  public string TextField { get; set; } = "Text";

  /// <summary>
  /// The property saying whether the item has children, whether or not they have been loaded yet. When the items have it,
  /// it decides whether the expand arrow is shown, which is how you load children on demand. Without it, the arrow is shown
  /// when the item has children in the data. <c>"HasChildren"</c> by default
  /// </summary>
  [Parameter]
  public string HasChildrenField { get; set; } = "HasChildren";

  /// <summary>
  /// For hierarchical data, the property holding the item's children, a collection of items (which may be of another type).
  /// <c>"Items"</c> by default
  /// </summary>
  [Parameter]
  public string ItemsField { get; set; } = "Items";

  /// <summary>
  /// The property holding the item's icon: a <see cref="PixataSvgIcon"/>, or the CSS class(es) of an icon font.
  /// <c>"Icon"</c> by default
  /// </summary>
  [Parameter]
  public string IconField { get; set; } = "Icon";

  /// <summary>
  /// The property holding a URL. An item with one is drawn as a link, which goes to the URL when the item is clicked or the
  /// user presses Enter on it. <c>"Url"</c> by default
  /// </summary>
  [Parameter]
  public string UrlField { get; set; } = "Url";

  /// <summary>
  /// The property saying whether the item is disabled. A disabled item is greyed out, and can't be selected, checked, clicked
  /// or dragged, but can still be expanded. <c>"Disabled"</c> by default
  /// </summary>
  [Parameter]
  public string DisabledField { get; set; } = "Disabled";

  /// <summary>
  /// What to show for each item at this level, in place of its icon and text. The context is the data item
  /// </summary>
  [Parameter]
  public RenderFragment<object>? ItemTemplate { get; set; }

  /// <summary>
  /// The binding used when there are none, or none for a level
  /// </summary>
  internal static PixataTreeViewBinding Default { get; } = new();

  protected override void OnInitialized() {
    if (TreeView is null) {
      throw new InvalidOperationException($"{nameof(PixataTreeViewBinding)} must be declared inside the {nameof(PixataTreeView.TreeViewBindings)} of a {nameof(PixataTreeView)}");
    }
    TreeView.AddBinding(this);
  }

  protected override void OnParametersSet() =>
    TreeView?.BindingChanged();

  // A binding draws nothing: the tree view reads its parameters
  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    TreeView?.RemoveBinding(this);
    GC.SuppressFinalize(this);
  }
}
