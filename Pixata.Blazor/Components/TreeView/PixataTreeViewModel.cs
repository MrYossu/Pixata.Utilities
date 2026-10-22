using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// The tree a <see cref="PixataTreeView"/> draws, built from its data and bindings. The data is flat if the root items have
/// the level 0 binding's <see cref="PixataTreeViewBinding.ParentIdField"/>, and hierarchical otherwise. Children are only
/// worked out when they are asked for, so a large tree costs little until it is expanded
/// </summary>
internal sealed class PixataTreeViewModel {
  private readonly Func<int, PixataTreeViewBinding> _bindingFor;
  private readonly List<object> _items;
  // For flat data, the items grouped by the value of each parent id field the bindings use (usually just the one)
  private readonly Dictionary<string, ILookup<object, object>> _childrenByParent = [];

  public PixataTreeViewModel(IEnumerable<object>? data, Func<int, PixataTreeViewBinding> bindingFor) {
    _bindingFor = bindingFor;
    _items = data?.Where(item => item is not null).ToList() ?? [];
    PixataTreeViewBinding rootBinding = bindingFor(0);
    IsFlat = _items.Count > 0 && PixataFieldReader.TryGetValue(_items[0], rootBinding.ParentIdField, out _);
    IEnumerable<object> roots = IsFlat
      ? _items.Where(item => PixataFieldReader.TryGetValue(item, rootBinding.ParentIdField, out object? parentId) && parentId is null)
      : _items;
    Roots = Nodes(roots, null);
  }

  /// <summary>
  /// True if the data is flat (each item says which is its parent), false if it is hierarchical (each item holds its children)
  /// </summary>
  public bool IsFlat { get; }

  public IReadOnlyList<PixataTreeViewNode> Roots { get; }

  public PixataTreeViewBinding BindingFor(int level) =>
    _bindingFor(level);

  /// <summary>
  /// The items under <paramref name="node"/> in the data, whether or not it is expanded
  /// </summary>
  internal IReadOnlyList<PixataTreeViewNode> ChildrenOf(PixataTreeViewNode node) {
    if (IsFlat) {
      if (node.Id is null) {
        return [];
      }
      string parentIdField = BindingFor(node.Level + 1).ParentIdField;
      if (!_childrenByParent.TryGetValue(parentIdField, out ILookup<object, object>? lookup)) {
        lookup = _items
          .Select(item => (Item: item, Found: PixataFieldReader.TryGetValue(item, parentIdField, out object? parentId), ParentId: parentId))
          .Where(entry => entry.Found && entry.ParentId is not null)
          .ToLookup(entry => entry.ParentId!, entry => entry.Item);
        _childrenByParent[parentIdField] = lookup;
      }
      return Nodes(lookup[node.Id], node);
    }
    return PixataFieldReader.TryGetValue(node.Item, node.Binding.ItemsField, out object? children) && children is IEnumerable enumerable and not string
      ? Nodes(enumerable.OfType<object>(), node)
      : [];
  }

  /// <summary>
  /// The items that are drawn: the roots, and the children of each drawn item for which <paramref name="showChildren"/> is true
  /// </summary>
  public List<PixataTreeViewNode> Visible(Func<PixataTreeViewNode, bool> showChildren) {
    List<PixataTreeViewNode> visible = [];
    AddVisible(Roots, showChildren, visible);
    return visible;
  }

  private static void AddVisible(IReadOnlyList<PixataTreeViewNode> nodes, Func<PixataTreeViewNode, bool> showChildren, List<PixataTreeViewNode> visible) {
    foreach (PixataTreeViewNode node in nodes) {
      visible.Add(node);
      if (showChildren(node)) {
        AddVisible(node.Children, showChildren, visible);
      }
    }
  }

  /// <summary>
  /// The item at a position in the tree, given as the index of each level separated by underscores (eg <c>"0_2"</c>), or
  /// null if there isn't one there
  /// </summary>
  public PixataTreeViewNode? FindByIndex(string? index) {
    if (string.IsNullOrWhiteSpace(index)) {
      return null;
    }
    IReadOnlyList<PixataTreeViewNode> level = Roots;
    PixataTreeViewNode? node = null;
    foreach (string part in index.Split('_')) {
      if (!int.TryParse(part, out int position) || position < 0 || position >= level.Count) {
        return null;
      }
      node = level[position];
      level = node.Children;
    }
    return node;
  }

  /// <summary>
  /// The node for a data item, searching the whole tree (including items that aren't drawn), or null if it isn't in it
  /// </summary>
  public PixataTreeViewNode? Find(object? item) =>
    item is null ? null : All(Roots).FirstOrDefault(node => Equals(node.Item, item));

  public static IEnumerable<PixataTreeViewNode> All(IEnumerable<PixataTreeViewNode> nodes) {
    foreach (PixataTreeViewNode node in nodes) {
      yield return node;
      foreach (PixataTreeViewNode descendant in All(node.Children)) {
        yield return descendant;
      }
    }
  }

  private List<PixataTreeViewNode> Nodes(IEnumerable<object> items, PixataTreeViewNode? parent) {
    int level = parent is null ? 0 : parent.Level + 1;
    PixataTreeViewBinding binding = BindingFor(level);
    List<object> list = items.ToList();
    return [.. list.Select((item, position) => new PixataTreeViewNode(this, item, parent, level, position, list.Count, binding))];
  }
}

/// <summary>
/// One item of a <see cref="PixataTreeViewModel"/>, with the values its binding reads from it
/// </summary>
internal sealed class PixataTreeViewNode {
  private readonly PixataTreeViewModel _model;
  private IReadOnlyList<PixataTreeViewNode>? _children;

  public PixataTreeViewNode(PixataTreeViewModel model, object item, PixataTreeViewNode? parent, int level, int position, int setSize, PixataTreeViewBinding binding) {
    _model = model;
    Item = item;
    Parent = parent;
    Level = level;
    Position = position;
    SetSize = setSize;
    Binding = binding;
    Index = parent is null ? $"{position}" : $"{parent.Index}_{position}";
    Id = Read(binding.IdField);
    Text = Read(binding.TextField) is { } text ? Convert.ToString(text) ?? "" : "";
    Icon = Read(binding.IconField);
    Url = Read(binding.UrlField) is { } url && Convert.ToString(url) is { Length: > 0 } urlText ? urlText : null;
    Disabled = Read(binding.DisabledField) is true;
  }

  public object Item { get; }
  public PixataTreeViewNode? Parent { get; }
  public int Level { get; }
  public int Position { get; }
  public int SetSize { get; }
  public PixataTreeViewBinding Binding { get; }

  /// <summary>
  /// The item's position in the tree, as the index of each level separated by underscores, eg <c>"0_2"</c>
  /// </summary>
  public string Index { get; }

  public object? Id { get; }
  public string Text { get; }
  public object? Icon { get; }
  public string? Url { get; }
  public bool Disabled { get; }

  public IReadOnlyList<PixataTreeViewNode> Children =>
    _children ??= _model.ChildrenOf(this);

  /// <summary>
  /// Whether the item has an expand arrow: what its HasChildren field says if it has one (so children can be loaded on
  /// demand), and otherwise whether it has children in the data
  /// </summary>
  public bool HasChildren =>
    Read(Binding.HasChildrenField) is bool hasChildren ? hasChildren : Children.Count > 0;

  public IEnumerable<PixataTreeViewNode> Descendants() =>
    PixataTreeViewModel.All(Children);

  public bool IsDescendantOf(PixataTreeViewNode ancestor) {
    for (PixataTreeViewNode? node = Parent; node is not null; node = node.Parent) {
      if (node == ancestor) {
        return true;
      }
    }
    return false;
  }

  private object? Read(string field) =>
    PixataFieldReader.TryGetValue(Item, field, out object? value) ? value : null;
}

/// <summary>
/// The expanded, selected or checked items of a <see cref="PixataTreeView"/>, in the order they were added
/// </summary>
internal sealed class PixataTreeViewItemSet {
  private readonly List<object> _items = [];
  private readonly HashSet<object> _lookup = [];

  public int Count =>
    _items.Count;

  public bool Contains(object item) =>
    _lookup.Contains(item);

  public void Add(object item) {
    if (_lookup.Add(item)) {
      _items.Add(item);
    }
  }

  public void Remove(object item) {
    if (_lookup.Remove(item)) {
      _items.Remove(item);
    }
  }

  public void Set(object item, bool include) {
    if (include) {
      Add(item);
    } else {
      Remove(item);
    }
  }

  public void Clear() {
    _items.Clear();
    _lookup.Clear();
  }

  public void Reset(IEnumerable<object> items) {
    Clear();
    foreach (object item in items.Where(item => item is not null)) {
      Add(item);
    }
  }

  public List<object> ToList() =>
    [.. _items];
}
