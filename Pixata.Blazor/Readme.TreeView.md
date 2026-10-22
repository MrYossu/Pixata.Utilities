# Tree view

>Part of [Pixata.Blazor](Readme.md)

`PixataTreeView` shows flat or hierarchical data as a tree. Items can be expanded and collapsed, selected, checked, dragged and dropped (within a tree, between trees, or on to a `PixataGrid`), and used as links, and children can be loaded when an item is first expanded. It works with the keyboard, mouse and touch, and is styled entirely through CSS custom properties, so you can fit it to your own theme.

See the [live demo](https://test.pixata.co.uk/TreeViewSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/TreeViewSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<PixataTreeView Data="_items" @bind-ExpandedItems="_expanded" AriaLabel="Files" />

@code {
  private List<FileItem> _items = [
    new() { Id = 1, Text = "Documents" },
    new() { Id = 2, ParentId = 1, Text = "Report.pdf" },
    new() { Id = 3, Text = "Notes.txt" }
  ];

  private IEnumerable<object> _expanded = [];

  public class FileItem {
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Text { get; set; } = "";
  }
}
```

`Data` is an `IEnumerable<object>`, so a `List<FileItem>` can be passed as it is. The tree view isn't generic, because each level of a tree can hold a different type. That means the items you get back (in `ExpandedItems`, `SelectedItems`, `CheckedItems` and the events) are `object`s, which you cast to your own type.

## Data

### Flat data

Each item has an id, and the id of its parent, which is null for a root item. Every item, at every level, goes in `Data`. The example above is flat data.

### Hierarchical data

Each item holds its children in a collection. `Data` holds the root items only...

```xml
<PixataTreeView Data="_folders" />

@code {
  private List<Folder> _folders = [
    new() { Text = "Fruit", Items = [new() { Text = "Apple" }, new() { Text = "Pear" }] }
  ];

  public class Folder {
    public string Text { get; set; } = "";
    public List<Folder>? Items { get; set; }
  }
}
```

The tree view decides which kind it has been given by looking at the first root item. If it has the parent id property (`ParentId` by default), the data is treated as flat, so if hierarchical items happen to have a `ParentId` property, set `ParentIdField` on the binding to a name they don't have, eg `ParentIdField="None"`.

### Property names

By default, the tree view reads these properties. A property the items don't have is simply ignored, so you only need the ones you use.

| Property | Default name | What it's for |
| --- | --- | --- |
| Id | `Id` | Identifies the item. Needed for flat data |
| Parent id | `ParentId` | The id of the item's parent, or null for a root item (flat data) |
| Text | `Text` | The text shown for the item |
| Children | `Items` | The item's children (hierarchical data) |
| Has children | `HasChildren` | Whether the item has children, whether or not they've been loaded. If the items have this, it decides whether the expand arrow is shown. Otherwise the arrow is shown when the item has children in the data |
| Icon | `Icon` | A `PixataSvgIcon` (see `PixataSvgIcons`), or the CSS class(es) of an icon font, such as `"fas fa-folder"` |
| URL | `Url` | Makes the item a link (see [Links](#links)) |
| Disabled | `Disabled` | When true, the item is greyed out, and can't be selected, checked, clicked or dragged. It can still be expanded |

To use other names, add a `PixataTreeViewBinding` inside `TreeViewBindings`...

```xml
<PixataTreeView Data="_people">
  <TreeViewBindings>
    <PixataTreeViewBinding IdField="PersonId" ParentIdField="ManagerId" TextField="FullName" />
  </TreeViewBindings>
</PixataTreeView>
```

Nested properties are separated by dots (`TextField="Address.City"`), and dictionaries (including `ExpandoObject`) are read by key. Setting a field to a name the items don't have turns that feature off, eg `IconField="None"` for no icons even though the items have an `Icon` property.

### Different types at each level

A binding with a `Level` applies to the items at that depth, where the root items are level 0. A binding without a `Level` applies to every level that doesn't have one of its own, and with no bindings at all the default names are used everywhere. With hierarchical data, each level can be a different type...

```xml
<PixataTreeView Data="_departments">
  <TreeViewBindings>
    <PixataTreeViewBinding Level="0" TextField="Name" ItemsField="Staff" />
    <PixataTreeViewBinding Level="1" TextField="FullName" />
  </TreeViewBindings>
</PixataTreeView>
```

For flat data, the parent id field of a child's level is compared with the id field of its parent's level. If you can, use the same parent id field for every level.

### Changing the data

The tree is rebuilt from `Data` whenever the tree view's parent redraws, so if you change the data in an event handler on the same page, the tree shows the change. If `Data` is an `ObservableCollection<T>`, the tree also redraws when items are added to it or removed from it. If you change the data some other way (from another component, say), call `Rebind()` on the tree view...

```xml
<PixataTreeView @ref="_tree" Data="_items" />

@code {
  private PixataTreeView _tree = null!;

  private void ItemsChangedElsewhere() =>
    _tree.Rebind();
}
```

## Templates

Give a binding an `ItemTemplate` to draw the items yourself, in place of the icon and text. The context is the item...

```xml
<PixataTreeViewBinding Level="1">
  <ItemTemplate>
    @{
      Employee employee = (Employee)context;
    }
    <strong>@employee.FullName</strong> (@employee.Role)
  </ItemTemplate>
</PixataTreeViewBinding>
```

The arrow, checkbox, hover, selection and focus are still drawn by the tree view. Use `OnItemRender` to add CSS classes to particular items...

```xml
<PixataTreeView Data="_items" OnItemRender="ItemRender" />

@code {
  private void ItemRender(PixataTreeViewItemRenderEventArgs args) =>
    args.Class = ((FileItem)args.Item).Text.EndsWith(".pdf") ? "pdf-file" : null;
}
```

The class goes on the element that holds the item's arrow, checkbox and text (`pixata-blazor-treeview-content`), not on its children.

## Expanding and collapsing

The user expands and collapses an item by clicking its arrow, or with the right and left arrow keys. `ExpandedItems` holds the expanded items. Bind it with `@bind-ExpandedItems` to know which they are, or set it to expand items from code...

```csharp
_expanded = [.. _items.Where(i => i.ParentId is null)];
```

Assign a new collection rather than adding to the one the tree view has, as changes to that aren't noticed.

If you set `ExpandedItems` one way and handle `ExpandedItemsChanged` yourself, an item the user expands or collapses only stays that way if you set `ExpandedItems` to the collection you are given. If you only set `ExpandedItems` (without handling the event), it says what is expanded to start with, and the tree view keeps track of it after that. `SelectedItems` and `CheckedItems` work the same way.

### Loading children on demand

Give the items a `HasChildren` property, so the tree view knows which have children before they're loaded, and add the children to the data in `OnExpand`...

```xml
<PixataTreeView Data="_servers" OnExpand="LoadChildren" />

@code {
  private List<Server> _servers = [new() { Id = 1, Text = "London", HasChildren = true }];

  private async Task LoadChildren(PixataTreeViewExpandEventArgs args) {
    Server server = (Server)args.Item;
    if (args.Expanded && !_servers.Any(s => s.ParentId == server.Id)) {
      _servers.AddRange(await ServerService.GetChildren(server.Id));
    }
  }
}
```

`OnExpand` is raised when the user expands or collapses an item, and `args.Expanded` says which. While the handler is running, the item's arrow is replaced with a spinner, and its children are shown once it has finished. With hierarchical data, add the children to the item's own collection instead.

## Selection

Set `SelectionMode` to `PixataTreeViewSelectionMode.Single` or `Multiple`, and bind `SelectedItems`...

```xml
<PixataTreeView Data="_items" SelectionMode="PixataTreeViewSelectionMode.Multiple" @bind-SelectedItems="_selected" />

@code {
  private IEnumerable<object> _selected = [];
}
```

In multiple mode, Ctrl+click (Cmd+click on a Mac) adds an item to the selection or removes it, and Shift+click selects the items from the last one the user picked to this one, among those showing. The same goes for Enter with Ctrl and Shift on the keyboard.

## Checkboxes

Set `CheckBoxMode` to `PixataTreeViewCheckBoxMode.Single` (checking one item unchecks the others) or `Multiple`, and bind `CheckedItems`...

```xml
<PixataTreeView Data="_items"
                CheckBoxMode="PixataTreeViewCheckBoxMode.Multiple"
                CheckChildren="true"
                CheckParents="true"
                @bind-CheckedItems="_checked" />
```

In multiple mode...

- `CheckChildren` makes checking or unchecking an item do the same to everything under it, whether or not it is showing (disabled items are left alone)
- `CheckParents` checks an item when all its children are checked, and unchecks it when any isn't. An unchecked item with some checked items under it shows a dash, and `aria-checked="mixed"`, but isn't in `CheckedItems`

`CheckOnClick` makes clicking the item itself check it, rather than only its checkbox. Space checks the item the keyboard is on.

Checkboxes and selection are separate, so a tree can have both.

## Links

An item with a URL is drawn as a link (an `<a>` element), so the browser's own link behaviour works, such as Ctrl+click to open it in a new tab. Pressing Enter on it goes to the URL as well. Links to other sites need the whole URL, including `https://`.

If an `ItemTemplate` draws its own links, or an `OnItemClick` handler navigates, turn the tree view's links off with `UrlField="None"`, or the user will be sent somewhere twice.

## Drag and drop

Set `Draggable` and handle `OnDrop`. The tree view shows where the item would go as it is dragged, but doesn't move it, as only you know how your data is stored...

```xml
<PixataTreeView Data="_items" Draggable="true" OnDrop="Drop" />

@code {
  private void Drop(PixataTreeViewDropEventArgs args) {
    FileItem item = (FileItem)args.Item;
    FileItem? target = (FileItem?)args.DestinationItem;
    if (args.DestinationTreeView is null) {
      return;
    }
    _items.Remove(item);
    if (target is null) {
      // Dropped on the tree, but not on an item
      item.ParentId = null;
      _items.Add(item);
    } else if (args.DropPosition == PixataTreeViewDropPosition.Over) {
      item.ParentId = target.Id;
      _items.Add(item);
    } else {
      item.ParentId = target.ParentId;
      _items.Insert(_items.IndexOf(target) + (args.DropPosition == PixataTreeViewDropPosition.After ? 1 : 0), item);
    }
  }
}
```

With flat data, remember to move the item's children too if they are in another list. The [sample page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/TreeViewSample.razor) has a full example that moves items within a tree and between two trees.

The user drags an item by its text, and drops it...

- **On another item**, to put it inside it (`DropPosition` is `Over`)
- **Near the top or bottom edge of an item**, to put it before or after it (`Before` or `After`), which is shown by a line
- **On a tree view but not on an item**, such as the space below the last item (`DestinationItem` is null)
- **On another tree view**. `DestinationTreeView` is the tree it was dropped on, which may be this one. The other tree doesn't have to be `Draggable` itself
- **On a `PixataGrid`**. `DestinationComponentId` is the grid's `Id`, and `DestinationIndex` is the position of the row among the rows the grid is showing, or null if it was dropped below them. `DestinationTreeView` and `DestinationItem` are null

An item can't be dropped on itself or one of its own children. Escape cancels a drag. On a touch screen, the user holds an item for a moment before dragging it, so that swiping across the tree still scrolls the page.

`DestinationIndex` for a tree view is the item's position, as the index at each level separated by underscores (`"0_2"` is the third child of the first root item). `GetItemFromDropIndex()` turns it back into the item.

`OnDragStart` lets you stop an item being dragged (set `args.IsCancelled`), `OnDrag` is raised as it moves (no more often than every `DragThrottleInterval` milliseconds), and `OnDragEnd` is raised after `OnDrop`, wherever it was dropped, including when it wasn't dropped on a tree view or grid, or the drag was cancelled.

## Using it

| Input | What it does |
| --- | --- |
| Click the arrow | Expands or collapses the item |
| Click or tap the item | Raises `OnItemClick`, and selects it (with `SelectionMode`) or checks it (with `CheckOnClick`) |
| Ctrl+click, Shift+click | Adds to or removes from the selection, or selects a range, in multiple selection mode |
| Click the checkbox | Checks or unchecks the item |
| Double-click | Raises `OnItemDoubleClick` |
| Right-click, or the context menu key | Raises `OnItemContextMenu` |
| Tab | Moves focus to the tree (to the item the user was last on) and away from it again. The tree is a single tab stop |
| Up and Down arrows | Move to the previous or next item that is showing |
| Right arrow | Expands the item, or if it's already expanded, moves to its first child |
| Left arrow | Collapses the item, or if it's already collapsed, moves to its parent |
| Home and End | Move to the first or last item that is showing |
| Enter | Raises `OnItemClick`, selects the item (Ctrl+Enter and Shift+Enter work as Ctrl+click and Shift+click) and goes to its URL if it has one |
| Space | Checks or unchecks the item if there are checkboxes, otherwise selects it |

In a right-to-left page, the left and right arrow keys are swapped, and the arrows point the other way.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Data` | `IEnumerable<object>` | | The items: every item for flat data, or the root items for hierarchical data |
| `TreeViewBindings` | `RenderFragment` | | The `PixataTreeViewBinding`s that say which properties to read |
| `ExpandedItems` | `IEnumerable<object>` | | The expanded items. Use `@bind-ExpandedItems` for two-way binding |
| `SelectionMode` | `PixataTreeViewSelectionMode` | `None` | `None`, `Single` or `Multiple` |
| `SelectedItems` | `IEnumerable<object>` | | The selected items. Use `@bind-SelectedItems` for two-way binding |
| `CheckBoxMode` | `PixataTreeViewCheckBoxMode` | `None` | `None`, `Single` or `Multiple` |
| `CheckedItems` | `IEnumerable<object>` | | The checked items. Use `@bind-CheckedItems` for two-way binding |
| `CheckChildren` | `bool` | `false` | Checking an item checks everything under it |
| `CheckParents` | `bool` | `false` | An item is checked when all its children are, and shows a dash when some are |
| `CheckOnClick` | `bool` | `false` | Clicking an item checks it |
| `Draggable` | `bool` | `false` | Lets the user drag items |
| `DragThrottleInterval` | `int` | `0` | The least time, in milliseconds, between one `OnDrag` and the next |
| `Size` | `string` | `"md"` | `"sm"`, `"md"` or `"lg"`. The values are in `PixataThemeConstants.Size` |
| `Id` | `string` | | The `id` of the outer element. One is made up if you don't set it |
| `Class` | `string` | | Extra CSS classes for the outer element |
| `AriaLabel` | `string` | | The accessible name of the tree |
| `AriaLabelledBy` | `string` | | The `id` of the element that labels the tree |

Any other attributes you add (`style`, `data-*` and so on) are rendered on the outer element. The tree has no height of its own; give it one (eg `style="height: 300px"`) to make it scroll.

### PixataTreeViewBinding

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Level` | `int?` | | The depth this binding applies to (0 for the root items), or null for every level without a binding of its own |
| `IdField` | `string` | `"Id"` | The property that identifies the item |
| `ParentIdField` | `string` | `"ParentId"` | The property holding the parent's id (flat data) |
| `TextField` | `string` | `"Text"` | The property holding the text |
| `ItemsField` | `string` | `"Items"` | The property holding the children (hierarchical data) |
| `HasChildrenField` | `string` | `"HasChildren"` | The property saying whether the item has children |
| `IconField` | `string` | `"Icon"` | The property holding the icon |
| `UrlField` | `string` | `"Url"` | The property holding the URL |
| `DisabledField` | `string` | `"Disabled"` | The property saying whether the item is disabled |
| `ItemTemplate` | `RenderFragment<object>` | | Draws the items at this level. The context is the item |

## Events

| Event | Argument | Description |
| --- | --- | --- |
| `ExpandedItemsChanged` | `IEnumerable<object>` | The user expanded or collapsed an item. Set for you by `@bind-ExpandedItems` |
| `SelectedItemsChanged` | `IEnumerable<object>` | The user selected or deselected items. Set for you by `@bind-SelectedItems` |
| `CheckedItemsChanged` | `IEnumerable<object>` | The user checked or unchecked items. Set for you by `@bind-CheckedItems` |
| `OnExpand` | `PixataTreeViewExpandEventArgs` | The user expanded or collapsed an item. `Item` is the item, and `Expanded` says which. Raised before `ExpandedItemsChanged`, so you can load the children here |
| `OnItemClick` | `PixataTreeViewItemClickEventArgs` | The user clicked or tapped an item, or pressed Enter on it. `EventArgs` is the `MouseEventArgs` or `KeyboardEventArgs` |
| `OnItemDoubleClick` | `PixataTreeViewItemDoubleClickEventArgs` | The user double-clicked an item |
| `OnItemContextMenu` | `PixataTreeViewItemContextMenuEventArgs` | The user right-clicked an item, or pressed the context menu key on it. `EventArgs` is the `MouseEventArgs`, whose `ClientX` and `ClientY` say where to show a menu. When this is handled, the browser's own menu isn't shown |
| `OnItemRender` | `Action<PixataTreeViewItemRenderEventArgs>` | Called as each item is drawn. Set `Class` to add CSS classes to it |
| `OnDragStart` | `PixataTreeViewDragStartEventArgs` | The user started dragging an item. Set `IsCancelled` to stop it |
| `OnDrag` | `PixataTreeViewDragEventArgs` | The item is being dragged. Has the same destination properties as `OnDrop`, plus `PageX` and `PageY` |
| `OnDrop` | `PixataTreeViewDropEventArgs` | The item was dropped on a tree view or a grid. `Item`, `DestinationItem`, `DestinationTreeView`, `DestinationIndex`, `DestinationComponentId` and `DropPosition` say what went where |
| `OnDragEnd` | `PixataTreeViewDragEndEventArgs` | The drag ended, wherever the item was dropped, or was cancelled. The destination properties are null if it wasn't dropped on a tree view or grid |

The event arguments also have a `ShouldRender` property, for code written for other tree views. The tree view always redraws after its events, so you don't need to set it.

## Methods

| Method | Description |
| --- | --- |
| `Rebind()` | Rebuilds the tree from `Data` and redraws it |
| `GetItemFromDropIndex(string index)` | The item at a `DestinationIndex`, or null if there isn't one |

## Accessibility

The tree follows the WAI-ARIA tree pattern. The list has `role="tree"` (and `aria-multiselectable` when more than one item can be selected or checked), and each item has `role="treeitem"`, with `aria-level`, `aria-posinset`, `aria-setsize`, `aria-expanded` for items with children, `aria-selected` when selection is on, `aria-checked` when there are checkboxes, `aria-disabled`, and `aria-busy` while its children are loading. Children are in a `role="group"` list inside their parent. The tree is one tab stop, and the arrow keys move the focus between items. Give it an `AriaLabel` or `AriaLabelledBy` so screen readers can name it.

## Styling

The tree's colours and spacing come from these custom properties, which you can set on `:root` or on a particular tree's `Class`...

| Property | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-treeview-color` | `--pixata-blazor-text-color` | The text colour |
| `--pixata-blazor-treeview-indent` | `16px` | How far each level is indented, which is also the width of the arrows |
| `--pixata-blazor-treeview-item-padding-y` | `4px` (`2px` small, `6px` large) | The space above and below each item's text |
| `--pixata-blazor-treeview-item-padding-x` | `8px` | The space either side of each item's text |
| `--pixata-blazor-treeview-item-radius` | `--pixata-blazor-border-radius` | The roundness of the item's highlight |
| `--pixata-blazor-treeview-hover-bg` | `--pixata-blazor-hover-bg` | The background of an item under the mouse |
| `--pixata-blazor-treeview-selected-bg` | `--pixata-blazor-selected-bg` | The background of a selected item |
| `--pixata-blazor-treeview-selected-hover-bg` | `--pixata-blazor-selected-hover-bg` | The background of a selected item under the mouse |
| `--pixata-blazor-treeview-selected-color` | `--pixata-blazor-selected-color` | The text colour of a selected item |
| `--pixata-blazor-treeview-link-color` | `inherit` | The text colour of items that are links |
| `--pixata-blazor-treeview-focus-ring` | `--pixata-blazor-focus-ring` | The `box-shadow` showing the item the keyboard is on |
| `--pixata-blazor-treeview-drop-color` | `--pixata-blazor-primary` | The line and outline showing where a dragged item will go |
| `--pixata-blazor-treeview-clue-bg` | `--pixata-blazor-surface-bg` | The background of the dragged item that follows the pointer |
| `--pixata-blazor-treeview-clue-shadow` | `0 4px 10px rgba(0, 0, 0, .12)` | Its shadow |

The classes all start with `pixata-blazor-treeview-`: the outer element is `pixata-blazor-treeview` (plus `pixata-blazor-treeview-sm`, `-md` or `-lg`), each item is a `pixata-blazor-treeview-item`, holding a `pixata-blazor-treeview-content` (with `pixata-blazor-treeview-top`, `-mid` or `-bot` for its place among its siblings) and, when expanded, a `pixata-blazor-treeview-group` of its children. The content holds the `pixata-blazor-treeview-toggle` arrow, the checkbox, and the `pixata-blazor-treeview-leaf` with the icon and `pixata-blazor-treeview-leaf-text`. A selected leaf also has `pixata-blazor-selected`, and a disabled one `pixata-blazor-disabled`.
