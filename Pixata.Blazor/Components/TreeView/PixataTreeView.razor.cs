using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Shows flat or hierarchical data as a tree, whose items can be expanded and collapsed, selected, checked, dragged and
/// dropped, and used as links. Say which properties of the items to read with <see cref="PixataTreeViewBinding"/>s in
/// <see cref="TreeViewBindings"/>
/// </summary>
public partial class PixataTreeView {
  private static readonly string ModulePath =
    $"./_content/Pixata.Blazor/Components/TreeView/PixataTreeView.razor.js?v={typeof(PixataTreeView).Assembly.ManifestModule.ModuleVersionId:N}";

  /// <summary>
  /// The items. For flat data, every item is in this collection, and each says which is its parent through its
  /// <see cref="PixataTreeViewBinding.ParentIdField"/> (null for a root item). For hierarchical data, this holds the root
  /// items, and each holds its children in its <see cref="PixataTreeViewBinding.ItemsField"/>. The tree is rebuilt whenever
  /// the tree view's parent redraws, and when an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/> passed
  /// here changes. If you change the data any other way, call <see cref="Rebind"/>
  /// </summary>
  [Parameter]
  public IEnumerable<object>? Data { get; set; }

  /// <summary>
  /// The <see cref="PixataTreeViewBinding"/>s that say which properties of the items to read, and how to draw them
  /// </summary>
  [Parameter]
  public RenderFragment? TreeViewBindings { get; set; }

  /// <summary>
  /// The items that are expanded. Use <c>@bind-ExpandedItems</c> for two-way binding. To change it from code, assign a new
  /// collection, as changes to the one the tree view already has aren't noticed
  /// </summary>
  [Parameter]
  public IEnumerable<object>? ExpandedItems { get; set; }

  /// <summary>
  /// Raised when the user expands or collapses an item, with the new set of expanded items. If you handle this without
  /// binding <see cref="ExpandedItems"/> to the same field, the change only sticks if you set <see cref="ExpandedItems"/>
  /// </summary>
  [Parameter]
  public EventCallback<IEnumerable<object>> ExpandedItemsChanged { get; set; }

  /// <summary>
  /// Whether the user can select items by clicking them. <see cref="PixataTreeViewSelectionMode.None"/> by default
  /// </summary>
  [Parameter]
  public PixataTreeViewSelectionMode SelectionMode { get; set; } = PixataTreeViewSelectionMode.None;

  /// <summary>
  /// The selected items. Use <c>@bind-SelectedItems</c> for two-way binding, and assign a new collection to change it from
  /// code
  /// </summary>
  [Parameter]
  public IEnumerable<object>? SelectedItems { get; set; }

  /// <summary>
  /// Raised when the user selects or deselects items, with the new set of selected items. If you handle this without binding
  /// <see cref="SelectedItems"/> to the same field, the change only sticks if you set <see cref="SelectedItems"/>
  /// </summary>
  [Parameter]
  public EventCallback<IEnumerable<object>> SelectedItemsChanged { get; set; }

  /// <summary>
  /// Whether the items have checkboxes. <see cref="PixataTreeViewCheckBoxMode.None"/> by default
  /// </summary>
  [Parameter]
  public PixataTreeViewCheckBoxMode CheckBoxMode { get; set; } = PixataTreeViewCheckBoxMode.None;

  /// <summary>
  /// The checked items. Use <c>@bind-CheckedItems</c> for two-way binding, and assign a new collection to change it from
  /// code
  /// </summary>
  [Parameter]
  public IEnumerable<object>? CheckedItems { get; set; }

  /// <summary>
  /// Raised when the user checks or unchecks items, with the new set of checked items. If you handle this without binding
  /// <see cref="CheckedItems"/> to the same field, the change only sticks if you set <see cref="CheckedItems"/>
  /// </summary>
  [Parameter]
  public EventCallback<IEnumerable<object>> CheckedItemsChanged { get; set; }

  /// <summary>
  /// With <see cref="PixataTreeViewCheckBoxMode.Multiple"/>, checking or unchecking an item does the same to all the items
  /// under it (those in the data, whether or not they are showing)
  /// </summary>
  [Parameter]
  public bool CheckChildren { get; set; }

  /// <summary>
  /// With <see cref="PixataTreeViewCheckBoxMode.Multiple"/>, an item is checked when all its children are, and its checkbox
  /// shows a dash when only some of the items under it are checked
  /// </summary>
  [Parameter]
  public bool CheckParents { get; set; }

  /// <summary>
  /// When true, clicking an item (not just its checkbox) checks or unchecks it
  /// </summary>
  [Parameter]
  public bool CheckOnClick { get; set; }

  /// <summary>
  /// Raised when the user expands or collapses an item, before its children are shown. To load the children on demand, give
  /// the items a <see cref="PixataTreeViewBinding.HasChildrenField"/>, and add the children to the data here. While an async
  /// handler is running, the item's arrow shows that it is loading
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewExpandEventArgs> OnExpand { get; set; }

  /// <summary>
  /// Raised when the user clicks or taps an item, or presses Enter on it
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewItemClickEventArgs> OnItemClick { get; set; }

  /// <summary>
  /// Raised when the user double-clicks or double-taps an item
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewItemDoubleClickEventArgs> OnItemDoubleClick { get; set; }

  /// <summary>
  /// Raised when the user right-clicks an item, presses the context menu key on it, or taps and holds it. When this is
  /// handled, the browser's own context menu isn't shown
  /// </summary>
  [Parameter]
  public EventCallback<PixataTreeViewItemContextMenuEventArgs> OnItemContextMenu { get; set; }

  /// <summary>
  /// Called as each item is drawn, so you can give it CSS classes
  /// </summary>
  [Parameter]
  public Action<PixataTreeViewItemRenderEventArgs>? OnItemRender { get; set; }

  /// <summary>
  /// The size of the items: <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// The <c>id</c> of the tree view's outer element. One is made up if you don't set it
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Extra CSS classes for the tree view's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// The accessible name of the tree, for when there is no visible label
  /// </summary>
  [Parameter]
  public string? AriaLabel { get; set; }

  /// <summary>
  /// The <c>id</c> of the element that labels the tree
  /// </summary>
  [Parameter]
  public string? AriaLabelledBy { get; set; }

  /// <summary>
  /// Any other attributes are rendered on the tree view's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  private readonly List<PixataTreeViewBinding> _bindings = [];
  private readonly PixataTreeViewItemSet _expanded = new();
  private readonly PixataTreeViewItemSet _selected = new();
  private readonly PixataTreeViewItemSet _checked = new();
  private readonly HashSet<object> _loading = [];
  private readonly string _generatedId = $"pixata-blazor-treeview-{Guid.NewGuid():N}";
  private PixataTreeViewModel? _model;
  private IEnumerable<object>? _data;
  private INotifyCollectionChanged? _observed;
  private IEnumerable<object>? _expandedParameter;
  private IEnumerable<object>? _selectedParameter;
  private IEnumerable<object>? _checkedParameter;
  private bool _firstParametersSet = true;
  private ElementReference _root;
  private ElementReference _list;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataTreeView>? _dotNetReference;
  private string? _attachedOptions;
  private bool _rtl;
  private object? _focused;
  private bool _focusPending;
  private object? _selectionAnchor;
  private PixataTreeViewNode? _tabStop;
  private readonly Dictionary<PixataTreeViewNode, bool> _hasCheckedDescendants = [];
  private bool _disposed;

  private string TreeId =>
    string.IsNullOrWhiteSpace(Id) ? _generatedId : Id;

  private string RootClass =>
    PixataCss.Classes("pixata-blazor-treeview",
      $"pixata-blazor-treeview-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}",
      Draggable ? "pixata-blazor-treeview-draggable" : null,
      Class);

  /// <summary>
  /// The tree, rebuilt from the data the first time it's needed after something has changed
  /// </summary>
  internal PixataTreeViewModel Model =>
    _model ??= new(Data, BindingFor);

  protected override void OnParametersSet() {
    if (!ReferenceEquals(Data, _data)) {
      Observe(Data);
    }
    // Rebuilt every time, so changes the page made to the data in place show when it redraws
    _model = null;
    Sync(ExpandedItems, ref _expandedParameter, _expanded);
    Sync(SelectedItems, ref _selectedParameter, _selected);
    Sync(CheckedItems, ref _checkedParameter, _checked);
    _firstParametersSet = false;
  }

  private void Sync(IEnumerable<object>? parameter, ref IEnumerable<object>? previous, PixataTreeViewItemSet set) {
    if (_firstParametersSet || !ReferenceEquals(parameter, previous)) {
      previous = parameter;
      set.Reset(parameter ?? []);
    }
  }

  /// <summary>
  /// Rebuilds the tree from <see cref="Data"/> and redraws it, after you have changed the data in a way the tree view can't
  /// see, such as adding children to an item's collection from outside the tree view's parent
  /// </summary>
  public void Rebind() {
    _model = null;
    StateHasChanged();
  }

  /// <summary>
  /// The item at a <see cref="PixataTreeViewDragEventArgsBase.DestinationIndex"/>, or null if there isn't one there
  /// </summary>
  public object? GetItemFromDropIndex(string? index) =>
    Model.FindByIndex(index)?.Item;

  // ---------- Bindings ----------

  internal void AddBinding(PixataTreeViewBinding binding) {
    _bindings.Add(binding);
    _model = null;
  }

  internal void RemoveBinding(PixataTreeViewBinding binding) {
    _bindings.Remove(binding);
    _model = null;
    if (!_disposed) {
      StateHasChanged();
    }
  }

  internal void BindingChanged() =>
    _model = null;

  private PixataTreeViewBinding BindingFor(int level) =>
    _bindings.FirstOrDefault(b => b.Level == level)
    ?? _bindings.FirstOrDefault(b => b.Level is null)
    ?? PixataTreeViewBinding.Default;

  // ---------- Observable data ----------

  private void Observe(IEnumerable<object>? data) {
    if (_observed is not null) {
      _observed.CollectionChanged -= DataCollectionChanged;
    }
    _data = data;
    _observed = data as INotifyCollectionChanged;
    if (_observed is not null) {
      _observed.CollectionChanged += DataCollectionChanged;
    }
  }

  private void DataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
    _ = InvokeAsync(() => {
      if (!_disposed) {
        Rebind();
      }
    });

  // ---------- Drawing ----------

  private bool ShowsChildren(PixataTreeViewNode node) =>
    _expanded.Contains(node.Item) && !_loading.Contains(node.Item) && node.HasChildren;

  private List<PixataTreeViewNode> Visible() =>
    Model.Visible(ShowsChildren);

  private void PrepareRender() {
    _hasCheckedDescendants.Clear();
    List<PixataTreeViewNode> visible = Visible();
    // The item that gets the focus when the user tabs into the tree: the one they were last on, or the first
    _tabStop = visible.FirstOrDefault(n => Equals(n.Item, _focused))
               ?? visible.FirstOrDefault(n => _selected.Contains(n.Item))
               ?? visible.FirstOrDefault();
  }

  private string? RenderClass(PixataTreeViewNode node) {
    if (OnItemRender is null) {
      return null;
    }
    PixataTreeViewItemRenderEventArgs args = new() { Item = node.Item };
    OnItemRender(args);
    return args.Class;
  }

  private static string PositionClass(PixataTreeViewNode node) =>
    node.SetSize == 1 ? "pixata-blazor-treeview-top pixata-blazor-treeview-bot"
    : node.Position == 0 ? "pixata-blazor-treeview-top"
    : node.Position == node.SetSize - 1 ? "pixata-blazor-treeview-bot"
    : "pixata-blazor-treeview-mid";

  // ---------- Expanding ----------

  private Task ToggleExpanded(PixataTreeViewNode node) =>
    SetExpanded(node, !_expanded.Contains(node.Item));

  private async Task SetExpanded(PixataTreeViewNode node, bool expand) {
    if (expand == _expanded.Contains(node.Item) || _loading.Contains(node.Item) || (expand && !node.HasChildren)) {
      return;
    }
    PixataTreeViewExpandEventArgs args = new() { Item = node.Item, Expanded = expand };
    Task handler = OnExpand.InvokeAsync(args);
    if (expand && !handler.IsCompleted) {
      // Loading the children, so show that until they arrive
      _loading.Add(node.Item);
      StateHasChanged();
      try {
        await handler;
      } finally {
        _loading.Remove(node.Item);
      }
    } else {
      await handler;
    }
    if (expand) {
      _expanded.Add(node.Item);
    } else {
      _expanded.Remove(node.Item);
      if (_focused is not null && node.Descendants().Any(d => Equals(d.Item, _focused))) {
        // The item the user was on is going out of sight, so move to the one that hid it
        _focused = node.Item;
        _focusPending = true;
      }
    }
    _model = null;
    List<object> items = _expanded.ToList();
    if (ExpandedItemsChanged.HasDelegate) {
      _expandedParameter = items;
      await ExpandedItemsChanged.InvokeAsync(items);
    }
  }

  // ---------- Clicking and selecting ----------

  private void ItemFocused(PixataTreeViewNode node) =>
    _focused = node.Item;

  private async Task ItemClick(PixataTreeViewNode node, MouseEventArgs e) {
    if (node.Disabled) {
      return;
    }
    _focused = node.Item;
    await OnItemClick.InvokeAsync(new() { Item = node.Item, EventArgs = e });
    await Select(node, e.CtrlKey || e.MetaKey, e.ShiftKey);
    if (CheckOnClick) {
      await ToggleChecked(node);
    }
  }

  private async Task ItemDoubleClick(PixataTreeViewNode node, MouseEventArgs e) {
    if (!node.Disabled) {
      await OnItemDoubleClick.InvokeAsync(new() { Item = node.Item, EventArgs = e });
    }
  }

  private async Task ItemContextMenu(PixataTreeViewNode node, MouseEventArgs e) {
    if (!node.Disabled) {
      await OnItemContextMenu.InvokeAsync(new() { Item = node.Item, EventArgs = e });
    }
  }

  /// <summary>
  /// Selects an item the user clicked or pressed Enter on. In multiple mode, <paramref name="toggle"/> (Ctrl) adds or removes
  /// it, and <paramref name="range"/> (Shift) selects the items from the last one the user picked to this one
  /// </summary>
  private async Task Select(PixataTreeViewNode node, bool toggle, bool range) {
    if (SelectionMode == PixataTreeViewSelectionMode.None || node.Disabled) {
      return;
    }
    if (SelectionMode == PixataTreeViewSelectionMode.Single) {
      if (_selected.Count == 1 && _selected.Contains(node.Item)) {
        return;
      }
      _selected.Reset([node.Item]);
    } else if (range && _selectionAnchor is not null) {
      List<PixataTreeViewNode> visible = Visible();
      int from = visible.FindIndex(n => Equals(n.Item, _selectionAnchor));
      int to = visible.FindIndex(n => Equals(n.Item, node.Item));
      if (to < 0) {
        return;
      }
      if (from < 0) {
        from = to;
      }
      if (!toggle) {
        _selected.Clear();
      }
      foreach (PixataTreeViewNode item in visible.Skip(Math.Min(from, to)).Take(Math.Abs(to - from) + 1).Where(n => !n.Disabled)) {
        _selected.Add(item.Item);
      }
    } else if (toggle) {
      _selected.Set(node.Item, !_selected.Contains(node.Item));
      _selectionAnchor = node.Item;
    } else {
      _selected.Reset([node.Item]);
      _selectionAnchor = node.Item;
    }
    List<object> items = _selected.ToList();
    if (SelectedItemsChanged.HasDelegate) {
      _selectedParameter = items;
      await SelectedItemsChanged.InvokeAsync(items);
    }
  }

  // ---------- Checking ----------

  /// <summary>
  /// True if the item is checked, false if it isn't, and null if it isn't but some of the items under it are, which is only
  /// shown with <see cref="CheckParents"/>
  /// </summary>
  private bool? CheckState(PixataTreeViewNode node) {
    if (CheckBoxMode == PixataTreeViewCheckBoxMode.None || _checked.Contains(node.Item)) {
      return CheckBoxMode != PixataTreeViewCheckBoxMode.None;
    }
    return CheckBoxMode == PixataTreeViewCheckBoxMode.Multiple && CheckParents && HasCheckedDescendants(node) ? null : false;
  }

  private bool HasCheckedDescendants(PixataTreeViewNode node) {
    if (!_hasCheckedDescendants.TryGetValue(node, out bool result)) {
      result = node.Children.Any(child => _checked.Contains(child.Item) || HasCheckedDescendants(child));
      _hasCheckedDescendants[node] = result;
    }
    return result;
  }

  private async Task ToggleChecked(PixataTreeViewNode node) {
    if (CheckBoxMode == PixataTreeViewCheckBoxMode.None || node.Disabled) {
      return;
    }
    bool check = !_checked.Contains(node.Item);
    if (CheckBoxMode == PixataTreeViewCheckBoxMode.Single) {
      _checked.Clear();
      _checked.Set(node.Item, check);
    } else {
      _checked.Set(node.Item, check);
      if (CheckChildren) {
        foreach (PixataTreeViewNode descendant in node.Descendants().Where(d => !d.Disabled)) {
          _checked.Set(descendant.Item, check);
        }
      }
      if (CheckParents) {
        for (PixataTreeViewNode? parent = node.Parent; parent is not null; parent = parent.Parent) {
          List<PixataTreeViewNode> children = [.. parent.Children.Where(c => !c.Disabled)];
          _checked.Set(parent.Item, children.Count > 0 && children.All(c => _checked.Contains(c.Item)));
        }
      }
    }
    List<object> items = _checked.ToList();
    if (CheckedItemsChanged.HasDelegate) {
      _checkedParameter = items;
      await CheckedItemsChanged.InvokeAsync(items);
    }
  }

  // ---------- Keyboard ----------

  private async Task KeyDown(KeyboardEventArgs e) {
    List<PixataTreeViewNode> visible = Visible();
    if (visible.Count == 0) {
      return;
    }
    PixataTreeViewNode current = visible.FirstOrDefault(n => Equals(n.Item, _focused)) ?? _tabStop ?? visible[0];
    int index = visible.IndexOf(current);
    string key = _rtl ? e.Key switch { "ArrowLeft" => "ArrowRight", "ArrowRight" => "ArrowLeft", _ => e.Key } : e.Key;
    switch (key) {
      case "ArrowDown" when index < visible.Count - 1:
        MoveTo(visible[index + 1]);
        break;
      case "ArrowUp" when index > 0:
        MoveTo(visible[index - 1]);
        break;
      case "Home":
        MoveTo(visible[0]);
        break;
      case "End":
        MoveTo(visible[^1]);
        break;
      case "ArrowRight":
        if (current.HasChildren && !_expanded.Contains(current.Item)) {
          await SetExpanded(current, true);
        } else if (ShowsChildren(current) && current.Children.Count > 0) {
          MoveTo(current.Children[0]);
        }
        break;
      case "ArrowLeft":
        if (ShowsChildren(current)) {
          await SetExpanded(current, false);
        } else if (current.Parent is not null) {
          MoveTo(current.Parent);
        }
        break;
      case "Enter" when !current.Disabled:
        _focused = current.Item;
        await OnItemClick.InvokeAsync(new() { Item = current.Item, EventArgs = e });
        await Select(current, e.CtrlKey || e.MetaKey, e.ShiftKey);
        if (current.Url is not null) {
          Navigation.NavigateTo(current.Url);
        }
        break;
      case " " when !current.Disabled:
        if (CheckBoxMode != PixataTreeViewCheckBoxMode.None) {
          await ToggleChecked(current);
        } else {
          await Select(current, SelectionMode == PixataTreeViewSelectionMode.Multiple, e.ShiftKey);
        }
        break;
    }
  }

  private void MoveTo(PixataTreeViewNode node) {
    _focused = node.Item;
    _focusPending = true;
  }

  // ---------- Browser ----------

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_disposed) {
      return;
    }
    string options = DragOptions;
    if (_attachedOptions != options) {
      bool attaching = _attachedOptions is null;
      _attachedOptions = options;
      _dotNetReference ??= DotNetObjectReference.Create(this);
      if (attaching) {
        _rtl = await InvokeModule("attach", false, _root, _list, _dotNetReference, DragSettings);
      } else {
        await InvokeModuleVoid("configure", _root, DragSettings);
      }
    }
    if (_focusPending) {
      _focusPending = false;
      PixataTreeViewNode? node = Visible().FirstOrDefault(n => Equals(n.Item, _focused));
      if (node is not null) {
        await InvokeModuleVoid("focusItem", _list, node.Index);
      }
    }
  }

  private async Task<IJSObjectReference?> Module() {
    if (_module is null && !_disposed) {
      try {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", ModulePath);
      } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
      }
    }
    return _module;
  }

  private async Task InvokeModuleVoid(string identifier, params object?[] args) {
    try {
      IJSObjectReference? module = await Module();
      if (module is not null) {
        await module.InvokeVoidAsync(identifier, args);
      }
    } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException or ObjectDisposedException) {
    }
  }

  private async Task<T> InvokeModule<T>(string identifier, T fallback, params object?[] args) {
    try {
      IJSObjectReference? module = await Module();
      if (module is not null) {
        return await module.InvokeAsync<T>(identifier, args);
      }
    } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException or ObjectDisposedException) {
    }
    return fallback;
  }

  public async ValueTask DisposeAsync() {
    _disposed = true;
    Observe(null);
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("detach", _root);
        await _module.DisposeAsync();
      } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
      }
    }
    _dotNetReference?.Dispose();
    GC.SuppressFinalize(this);
  }
}
