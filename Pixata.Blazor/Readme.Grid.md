# PixataGrid

>Part of [Pixata.Blazor](Readme.md)

`PixataGrid` is a data grid with paging, sorting, filtering, searching, selection, editing, grouping and aggregates, resizable, movable and frozen columns, virtual scrolling, detail rows, row dragging, export, keyboard navigation, translation and right-to-left layout. It renders on the client or the server, so it can be used in WASM as well as Blazor Server apps.

The [sample page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/PixataGridSample.razor) shows each feature.

## Getting started

Add the namespace, either to the page or to your `_Imports.razor`...

```razor
@using Pixata.Blazor.Components.Grid
```

Make sure you have [added the stylesheet](Readme.Styling.md), which contains the grid's styles. Then...

```razor
<PixataGrid Data="_people" Height="400px">
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.FirstName)" Width="150px" />
    <PixataGridColumn Field="@nameof(Person.Surname)" />
    <PixataGridColumn Field="@nameof(Person.DateOfBirth)" DisplayFormat="{0:dd MMM yyyy}" />
    <PixataGridColumn Field="Address.City" Title="City" />
  </GridColumns>
</PixataGrid>
```

You don't need to specify `TItem`, as the grid works it out from `Data`, and the columns get it from the grid.

### Why the components aren't called `GridColumn` and so on

Names such as `GridColumn` are common in component libraries, and an app that uses two libraries with the same names gets ambiguous reference errors wherever both namespaces are imported. So all the grid's components and types start with `PixataGrid`.

## Grid parameters

| Parameter | Type | What it does |
| --- | --- | --- |
| `Data` | `IEnumerable<TItem>` | The items to show. See [updating the data](#updating-the-data) |
| `OnRead` | `EventCallback<PixataGridReadEventArgs<TItem>>` | Supplies the data yourself, a page at a time, instead of `Data`. See [OnRead](#supplying-the-data-with-onread) |
| `EnableLoaderContainer` | `bool` | Shows a loading indicator when `OnRead` is slow. Defaults to `true` |
| `GridColumns` | `RenderFragment` | The column definitions |
| `AutoGenerateColumns` | `bool` | Adds a column for each simple property of `TItem`. See [auto-generated columns](#auto-generated-columns) |
| `Height` | `string` | A CSS height, such as "400px". If set, the rows scroll under a fixed header. If not, the grid is as tall as its rows |
| `Width` | `string` | A CSS width. If not set, the grid fills its container |
| `Class` | `string` | A CSS class for the grid's outer element |
| `Size` | `PixataGridSize` | `Medium` (the default) or `Small`, which sets the padding around the cells |
| `NoDataTemplate` | `RenderFragment` | What to show when there are no items. Defaults to "No records available." |
| `GridSettings` | `RenderFragment` | Settings for parts of the grid, such as the [pager](#paging) |
| `Pageable` | `bool` | Splits the rows into pages, with a pager. See [paging](#paging) |
| `PageSize` | `int` | The number of rows on a page. Defaults to 10. Can be two-way bound |
| `Page` | `int` | The current page, starting from 1. Can be two-way bound |
| `Sortable` | `bool` | Lets the user sort by clicking a column header. See [sorting](#sorting) |
| `SortMode` | `PixataGridSortMode` | `Single` (the default) or `Multiple` |
| `FilterMode` | `PixataGridFilterMode` | `None` (the default), `FilterRow` or `FilterMenu`. See [filtering](#filtering) |
| `FilterRowDebounceDelay` | `int` | How long, in milliseconds, the filter row waits after the user stops typing in a text or number filter before it filters. Defaults to 150 |
| `GridToolBarTemplate` | `RenderFragment` | Content for a toolbar above the grid, such as a [search box](#search-box) |
| `RowTemplate` | `RenderFragment<TItem>` | The content of each row, in place of the grid's cells. See [templates](#templates-and-row-events) |
| `LoaderTemplate` | `RenderFragment` | What the loading indicator shows, in place of the dots |
| `OnRowRender` | `Action<PixataGridRowRenderEventArgs<TItem>>` | Lets you set a CSS class on each row |
| `OnRowClick`, `OnRowDoubleClick`, `OnRowContextMenu` | `EventCallback<PixataGridRowClickEventArgs<TItem>>` | Fire when the user clicks, double-clicks or right-clicks a row |
| `SelectionMode` | `PixataGridSelectionMode` | `None` (the default), `Single` or `Multiple`. See [selection](#selection) |
| `SelectedItems` | `IEnumerable<TItem>` | The selected rows. Can be two-way bound |
| `SelectedCells` | `IEnumerable<PixataGridSelectedCell<TItem>>` | The selected cells, when selecting cells. Can be two-way bound |
| `ItemKey` | `Func<TItem, object?>` | Gets a key (such as the id) used to match selected items, instead of `Equals` |
| `EditMode` | `PixataGridEditMode` | `None` (the default), `Inline`, `Popup` or `InCell`. See [editing](#editing) |
| `OnAdd`, `OnCreate`, `OnEdit`, `OnUpdate`, `OnDelete`, `OnCancel` | `EventCallback<PixataGridCommandEventArgs<TItem>>` | The editing events |
| `OnModelInit` | `Func<TItem>` | Creates a new item, if `TItem` has no parameterless constructor |
| `ConfirmDelete` | `bool` | Asks the user to confirm before deleting |
| `Groupable` | `bool` | Shows a panel above the grid that the user can drag column headers into, to group the rows. See [grouping](#grouping-and-aggregates) |
| `GridAggregates` | `RenderFragment` | The aggregates to calculate, as `PixataGridAggregate`s, for the footer templates |
| `Navigable` | `bool` | Makes the grid one tab stop, with the keyboard moving between cells. See [keyboard navigation](#keyboard-navigation-and-accessibility) |
| `HighlightedItems` | `IEnumerable<TItem>` | Rows to draw attention to. See [highlighting](#highlighting) |
| `HighlightedCells` | `IEnumerable<PixataGridHighlightedCell<TItem>>` | Cells to draw attention to |
| `Messages` | `PixataGridMessages` | The grid's text, to translate it. See [translation](#translation) |
| `AdaptiveMode` | `PixataGridAdaptiveMode` | `None` (the default) or `Auto`, which makes the menus and popup editor fill small screens. See [small screens](#small-screens) |
| `GridExport` | `RenderFragment` | Export settings, such as a `PixataGridCsvExport`. See [export](#export) |
| `DetailTemplate` | `RenderFragment<TItem>` | Details for each row, shown when the user expands it. See [detail rows](#detail-rows) |
| `OnRowExpand`, `OnRowCollapse` | `EventCallback<PixataGridRowExpandEventArgs<TItem>>`, `EventCallback<PixataGridRowCollapseEventArgs<TItem>>` | Fire when the user shows or hides a row's details |
| `RowDraggable` | `bool` | Lets the user drag rows. See [dragging rows](#dragging-rows) |
| `OnRowDrop` | `EventCallback<PixataGridRowDropEventArgs<TItem>>` | Fires when the user drops dragged rows |
| `ScrollMode` | `PixataGridScrollMode` | `Scrollable` (the default) or `Virtual`, which renders only the rows in view. See [virtual scrolling](#virtual-scrolling) |
| `RowHeight` | `double?` | The height of each row in pixels, for virtual scrolling |
| `ColumnVirtualization` | `bool` | Renders only the columns in view |
| `Resizable` | `bool` | Lets the user resize columns. See [columns](#resizing-moving-and-freezing-columns) |
| `Reorderable` | `bool` | Lets the user move columns by dragging their headers |
| `ShowColumnMenu` | `bool` | Gives each header a menu. See [the column menu](#the-column-menu) |
| `OnColumnResize` | `EventCallback<PixataGridColumnResizeEventArgs<TItem>>` | Fires when the user has resized a column, with the column and its new width in pixels |
| `OnStateInit`, `OnStateChanged` | `EventCallback<PixataGridStateEventArgs<TItem>>` | Set the state the grid starts with, and hear about changes to it. See [state](#state) |
| `StorageKey` | `string` | Saves the grid's state in the browser's local storage under this key, and restores it when the grid is next shown |
| `OnColumnReorder` | `EventCallback<PixataGridColumnReorderEventArgs<TItem>>` | Fires when the user has moved a column, with the column and its old and new positions among the columns it is with |

## Column parameters

| Parameter | Type | What it does |
| --- | --- | --- |
| `Field` | `string` | The property to show, such as `nameof(Person.Name)`, or a path to a nested one, such as "Address.City". This is case sensitive, and a name that doesn't match a public property or field throws an `ArgumentException` when the grid is rendered. If anything along a nested path is null, the cell is empty |
| `Title` | `string` | The header text. See below for the default |
| `Width` | `string` | A CSS width, such as "120px" or "20%". Columns without a width share the space that's left |
| `DisplayFormat` | `string` | A .NET format string, such as "{0:dd MMM yyyy}" or "{0:C2}". Values are formatted using the current culture |
| `Visible` | `bool?` | Set to `false` to hide the column. `null` is treated as `true` |
| `TextAlign` | `PixataGridTextAlign?` | `Left`, `Center` or `Right`, for the data cells. The header isn't affected, so use `HeaderClass` if you want that aligned too |
| `Class` | `string` | A CSS class for the column's data cells |
| `HeaderClass` | `string` | A CSS class for the column's header cell |
| `Sortable` | `bool` | Set to `false` to stop the user sorting by this column. Defaults to `true`, but only matters if the grid is `Sortable` |
| `Filterable` | `bool` | Set to `false` to stop the user filtering by this column. Defaults to `true` |
| `FilterMenuType` | `PixataGridFilterMenuType` | `Menu` (two conditions, the default) or `CheckBoxList` |
| `DefaultFilterOperator` | `GridFilterOperator?` | The operator the filter starts with. Defaults to Contains for text, and Is equal to for everything else |
| `IgnoreTime` | `bool` | For a `DateTime` field, makes the filter compare the date only, so "on 21/10/2026" finds the items at any time that day. True by default for a `PixataDateGridColumn`, false otherwise. See [dates with a time](#dates-with-a-time) |
| `FilterOperators` | `List<PixataGridFilterListOperator>` | The operators to offer, in place of the defaults for the field's type. See [typed columns](#typed-columns) |
| `ShowFilterCellButtons` | `bool` | Set to `false` to hide the operator button and the clear button in the filter row, leaving just the editor. Defaults to `true` |
| `FilterCellTemplate`, `FilterMenuTemplate` | `RenderFragment<PixataGridFilterTemplateContext>` | Your own filter UI. See [custom filters](#custom-filters) |
| `Template` | `RenderFragment<object>` | The content of each data cell. See [templates](#templates-and-row-events) |
| `HeaderTemplate` | `RenderFragment` | The content of the header cell, in place of the title |
| `OnCellRender` | `Action<PixataGridCellRenderEventArgs<TItem>>` | Lets you set a CSS class on each cell |
| `Editable` | `bool` | Set to `false` to stop the column being edited. Defaults to `true`, but a property with no public setter is never editable |
| `EditorType` | `PixataGridEditorType?` | The editor to use, if not the default for the field's type |
| `EditorTemplate` | `RenderFragment<TItem>` | Your own editor. The context is the copy being edited |
| `Id` | `string` | An identifier, for `AutoFitColumn` and `AutoFitColumns` |
| `Columns` | `RenderFragment` | Columns to show under this one. See [multi-column headers](#multi-column-headers) |
| `Resizable` | `bool` | Set to `false` to stop the user resizing this column. Defaults to `true`, but only matters if the grid is `Resizable` |
| `MinResizableWidth`, `MaxResizableWidth` | `double?` | The narrowest and widest the user can make the column, in pixels |
| `Reorderable` | `bool` | Set to `false` to stop the user moving this column. Defaults to `true`, but only matters if the grid is `Reorderable` |
| `Locked` | `bool` | Freezes the column, so it stays put when the grid scrolls sideways. It needs a `Width` in pixels |
| `Lockable` | `bool` | Set to `false` to stop the user freezing and unfreezing the column from the column menu |
| `ShowColumnMenu` | `bool` | Set to `false` to leave the column out when the grid's `ShowColumnMenu` is set. It then has a filter menu button instead, if the grid's `FilterMode` is `FilterMenu` |
| `VisibleInColumnChooser` | `bool` | Set to `false` to leave the column out of the column menu's list of columns |
| `Groupable` | `bool` | Set to `false` to stop the user grouping by this column. Defaults to `true`, but only matters if the grid is `Groupable` |
| `GroupHeaderTemplate` | `RenderFragment<PixataGridGroupTemplateContext>` | The content of a group's header row when grouped by this column, in place of "Title: value" |
| `GroupFooterTemplate` | `RenderFragment<PixataGridGroupTemplateContext>` | The content of this column's cell in each group's footer row |
| `FooterTemplate` | `RenderFragment<PixataGridAggregateTemplateContext>` | The content of this column's cell in the grid's footer row |

If you don't set a `Title`, the grid uses the `[Display(Name = "...")]` attribute on the property if there is one. If not, it uses the field name split into words with `SplitCamelCase()`, so `DateOfBirth` becomes "Date of birth" (for a nested field, it uses the last part of the path). So you rarely need to set a `Title` for a well-named property.

Similarly, if you don't set a `DisplayFormat`, the grid uses the `[DisplayFormat(DataFormatString = "...")]` attribute on the property if there is one.

## Auto-generated columns

Set `AutoGenerateColumns="true"` to get a column for each public property of `TItem` that holds a simple value: numbers, strings, `bool`, dates and times, `Guid`, enums, and nullable versions of these. Other properties, such as a nested `Address`, are left out, as all they could show is the type name.

- Add `[Display(AutoGenerateField = false)]` or `[ScaffoldColumn(false)]` to a property to leave it out (you can still declare a column for it yourself)
- Add `[Display(Order = n)]` to change the order. Properties without an order come after those with one, in the order they are declared

Generated columns go after any declared columns. To put them somewhere else, add a `PixataGridAutoGeneratedColumns` where you want them. Its `ColumnWidth` parameter sets the width of every generated column...

```razor
<PixataGrid Data="_people" AutoGenerateColumns="true">
  <GridColumns>
    <PixataGridAutoGeneratedColumns ColumnWidth="120px" />
    <PixataGridColumn Field="Address.City" Title="City" />
  </GridColumns>
</PixataGrid>
```

## Paging

Set `Pageable="true"` to split the rows into pages. Bind `Page` and `PageSize` if you want to know or set them, and use `GridSettings` to change the pager...

```razor
<PixataGrid Data="_people" Pageable="true" @bind-Page="_page" @bind-PageSize="_pageSize">
  <GridSettings>
    <PixataGridPagerSettings ButtonCount="5" PageSizes="@([10, 25, 50, null])" />
  </GridSettings>
  <GridColumns>...</GridColumns>
</PixataGrid>
```

| Pager setting | Type | What it does |
| --- | --- | --- |
| `ButtonCount` | `int` | The number of page buttons shown at once. Defaults to 10. Pages are shown in fixed groups (1-10, 11-20...), with "..." buttons that move to the next or previous group |
| `PageSizes` | `List<int?>` | The page sizes the user can pick from. Include `null` to offer "All". If not set, there's no page size dropdown |
| `InputType` | `PixataGridPagerInputType` | `Buttons` (the default) for a button per page, or `Input` for a box where the user types the page number |
| `Position` | `PixataGridPagerPosition` | `Bottom` (the default) or `Top` |
| `Adaptive` | `bool` | Whether the pager adapts to its width. Defaults to true. As it narrows, the "1 - 10 of 91 items" text goes first, then the page size dropdown, and finally the page buttons become a dropdown |

A few things to know...

- Picking a page size goes back to the first page
- Picking "All" sets `PageSize` to the number of items, so that's what the dropdown shows afterwards
- If the page is past the end (because the data shrank, or you set `Page` too high), the grid moves to the last page and raises `PageChanged`
- A page number typed in the box is limited to the pages that exist, and anything that isn't a number is ignored

## Sorting

Set `Sortable="true"` and the user can sort by clicking a column header (or pressing Enter on it). Each click moves the column through ascending, descending and back to unsorted. With `SortMode="PixataGridSortMode.Multiple"`, each column clicked is added to the sort, after the ones already there, and the header shows its position.

```razor
<PixataGrid Data="_people" Sortable="true" SortMode="PixataGridSortMode.Multiple">
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.Surname)" />
    <PixataGridColumn Field="@nameof(Person.FirstName)" />
    <PixataGridColumn Field="@nameof(Person.Notes)" Sortable="false" />
  </GridColumns>
</PixataGrid>
```

A few things to know...

- Sorting uses the normal .NET ordering for the field's type, so strings are compared using the current culture, and enums by their underlying value
- Sorting doesn't change the page, so you see the same page of the newly sorted data
- A nested field such as "Address.City" sorts safely when part of the path is null. Those rows count as null, which comes first in ascending order

Setting the initial sort from code will be possible through the grid's state, when that's added.

## Filtering

Set `FilterMode` to choose how the user filters...

- `FilterRow` adds a row under the headers, with an editor for each column. Text and numbers are filtered a moment after the user stops typing (150 milliseconds, unless you set `FilterRowDebounceDelay`; make it longer, say 350, when the data comes from the server through `OnRead`, so the server isn't asked on every key), and the other editors filter straight away. The button next to each editor picks the operator (such as "Starts with"), and the one beside it clears the filter
- `FilterMenu` puts a filter icon in each header. It opens a menu with two conditions joined by And or Or, which are applied when the user clicks Filter. Set a column's `FilterMenuType` to `CheckBoxList` for a list of the column's values to tick instead. The icon is coloured when the column has a filter

```razor
<PixataGrid Data="_people" FilterMode="PixataGridFilterMode.FilterMenu">
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.Name)" />
    <PixataGridColumn Field="@nameof(Person.Department)" FilterMenuType="PixataGridFilterMenuType.CheckBoxList" />
    <PixataGridColumn Field="@nameof(Person.Notes)" Filterable="false" />
  </GridColumns>
</PixataGrid>
```

The editor and the operators depend on the field's type...

| Type | Editor | Operators |
| --- | --- | --- |
| Text | A text box | Is equal to, Is not equal to, Starts with, Contains (the default), Does not contain, Ends with, Is null, Is not null, Is empty, Is not empty, Has no value, Has value |
| Numbers | A number box with up and down buttons | Is less than, Is less than or equal to, Is equal to, Is not equal to, Is greater than or equal to, Is greater than |
| `DateTime` and `DateOnly` | A [date picker](Readme.DatePicker.md) | Is equal to, Is not equal to, Is after or equal to, Is after, Is before or equal to, Is before |
| Enums | A dropdown of the values | Is equal to, Is not equal to |
| `bool` | A dropdown of (All), is true and is false | None |

Nullable numbers, dates and enums also get Is null and Is not null. Columns of other types can't be filtered unless you give them a template.

Text comparisons ignore case, and filtering (or searching) goes back to the first page.

### Typed columns

The full lists of operators are more than most users need, so there are four columns that set up the filter for a type of field. They take all the same parameters as `PixataGridColumn`...

| Column | Filter |
| --- | --- |
| `PixataTextGridColumn` | contains (the default), starts with, ends with |
| `PixataNumericGridColumn` | equals (the default), less than, more than |
| `PixataDateGridColumn` | before, on (the default), on or after |
| `PixataEnumGridColumn` | Just the drop-down of values, filtering on equality, with no operator or clear button |

```razor
<PixataGrid Data="_people" FilterMode="PixataGridFilterMode.FilterRow">
  <GridColumns>
    <PixataTextGridColumn Field="@nameof(Person.Name)" />
    <PixataNumericGridColumn Field="@nameof(Person.Age)" />
    <PixataDateGridColumn Field="@nameof(Person.DateOfBirth)" DisplayFormat="{0:dd MMM yyyy}" />
    <PixataEnumGridColumn Field="@nameof(Person.Department)" />
  </GridColumns>
</PixataGrid>
```

The lists come from `PixataGridFilterOperators.StringOperators`, `NumericOperators` and `DateOperators`. You can replace these at startup to change the operators (or their text) everywhere, or pass your own list to any column's `FilterOperators`...

```razor
<PixataGridColumn Field="@nameof(Person.Age)"
                  FilterOperators="@([new() { Operator = GridFilterOperator.IsGreaterThanOrEqualTo, Text = "at least" }])" />
```

The filter starts with the column's `DefaultFilterOperator` if you set one. Otherwise it starts with the usual default for the type if that's in the list, or the first operator in the list if not.

### Dates with a time

When a `DateTime` field holds a time as well as a date, filtering for "is equal to 21/10/2026" would normally find nothing, as the filter value is midnight and the data isn't. A column with `IgnoreTime="true"` compares the date only, so "on 21/10/2026" finds anything that day, "before" means before the start of that day and "after" means from the start of the next one. `PixataDateGridColumn` does this by default (set `IgnoreTime="false"` to compare the time as well); other columns don't.

The comparison is done with a range ("on or after the start of the day, and before the start of the next"), rather than by taking the date part of the field, so EF Core can still use an index on it. It's carried on each filter in the request as `GridFilterDescriptor.IgnoreTime`, so it works with `OnRead` and across an API as well as with `Data`.

If the columns don't set it (or the request comes from somewhere else), call `RemoveTime()` in your `OnRead` handler, naming the date fields, before you read the data. This is the equivalent of Telerik's `TelerikFilterHelper.RemoveTime()`...

```csharp
private async Task ReadPeople(PixataGridReadEventArgs<Person> args) {
  args.Request.RemoveTime(nameof(Person.DateCreated), nameof(Person.DateDeleted));
  args.SetResult(await Db.People.OrderBy(p => p.Id).ToGridResultAsync(args.Request, args.CancellationToken));
}
```

### Search box

Put a `PixataGridSearchBox` in the grid's toolbar to let the user search. It shows the rows where any of the text columns contain what they type, ignoring case, and works alongside any column filters...

```razor
<PixataGrid Data="_people">
  <GridToolBarTemplate>
    <PixataGridSearchBox />
  </GridToolBarTemplate>
  <GridColumns>...</GridColumns>
</PixataGrid>
```

| Parameter | Type | What it does |
| --- | --- | --- |
| `Fields` | `List<string>` | The fields to search. If not set, every text column in the grid is searched (including ones that can't be filtered) |
| `Placeholder` | `string` | Defaults to "Search..." |
| `DebounceDelay` | `int` | How long to wait after the last key press, in milliseconds. Defaults to 300 |
| `Width` | `string` | Defaults to 140px |

### Custom filters

A column's `FilterCellTemplate` replaces its cell in the filter row, and `FilterMenuTemplate` replaces the conditions in its filter menu (the Filter and Clear buttons are still there). Both are given a `PixataGridFilterTemplateContext`, whose `Filter` is the column's `GridCompositeFilterDescriptor`, and whose `Field` is the column's field. Add or change its descriptors, then call `FilterAsync()` to apply them, or `ClearFilterAsync()` to remove the filter...

```razor
<PixataGridColumn Field="@nameof(Person.Age)">
  <FilterCellTemplate>
    <button @onclick="async () => {
      context.Filter.Filters.Clear();
      context.Filter.Filters.Add(new(nameof(Person.Age), GridFilterOperator.IsGreaterThanOrEqualTo, 18));
      await context.FilterAsync();
    }">Adults</button>
  </FilterCellTemplate>
</PixataGridColumn>
```

### Bool filter

`PixataGridBoolFilter` is a filter for a `bool` column in the filter row, as an alternative to the drop-down. It's a three-state checkbox, which starts empty with a dash (all rows), and each click moves it on to ticked (true), unticked (false) and back to all rows. Use its `Filter()` method as the column's `FilterCellTemplate`...

```razor
<PixataGridColumn Field="@nameof(Person.Manager)" FilterCellTemplate="PixataGridBoolFilter.Filter()" />
```

Pass `true` to `Filter()` to show a clear button beside the checkbox. To set its other parameters, use it in the template yourself...

```razor
<PixataGridColumn Field="@nameof(Person.Manager)">
  <FilterCellTemplate>
    <PixataGridBoolFilter Context="context" ShowClearButton="true" AriaLabel="Filter managers" />
  </FilterCellTemplate>
</PixataGridColumn>
```

| Parameter | Type | What it does |
| --- | --- | --- |
| `Context` | `PixataGridFilterTemplateContext` | The context passed to the `FilterCellTemplate`. Required |
| `ShowClearButton` | `bool` | Shows a button that clears the filter. Defaults to `false` |
| `Class` | `string` | A CSS class for the container, which centres the checkbox in the cell |
| `AriaLabel` | `string` | The checkbox's accessible name. Defaults to "Filter" |
| `ClearText` | `string` | The clear button's tooltip and accessible name. Defaults to "Clear" |

## Updating the data

The grid re-reads `Data` whenever the parent component re-renders, so you can change the collection and let Blazor re-render as normal. If you change the collection when the parent isn't re-rendering, call `Rebind()` on the grid...

```razor
<PixataGrid @ref="_grid" Data="_people">...</PixataGrid>

@code {
  private PixataGrid<Person> _grid = null!;

  private void RemoveFirst() {
    _people.RemoveAt(0);
    _grid.Rebind();
  }
}
```

If `Data` is an `ObservableCollection<T>` (or anything else that implements `INotifyCollectionChanged`), the grid updates itself when the collection changes.

## Styling

The grid's colours, borders and spacing come from CSS custom properties. To change them, override these custom properties, either on `:root` or on any container around the grid...

| Property | Default | What it sets |
| --- | --- | --- |
| `--pixata-grid-font-size` | `.875rem` | The font size |
| `--pixata-grid-line-height` | `1.4285` | The line height (20px with the default font size) |
| `--pixata-grid-color` | `#3d3d3d` | The text colour |
| `--pixata-grid-bg` | `#fff` | The background |
| `--pixata-grid-border-color` | `rgba(0, 0, 0, .08)` | The outer border and the lines between columns |
| `--pixata-grid-header-bg` | `#fafafa` | The header background |
| `--pixata-grid-alt-row-bg` | `rgba(61, 61, 61, .05)` | The background of every other row |
| `--pixata-grid-hover-bg` | `rgba(61, 61, 61, .11)` | The background of the row under the mouse |
| `--pixata-grid-primary`, `--pixata-grid-primary-hover`, `--pixata-grid-primary-color` | `#ff6358`, `#ea5a51`, `#fff` | The accent colour: the current page, sort arrows, the active filter icon, selected items and primary buttons |
| `--pixata-grid-input-bg` | `#fff` | The background of text boxes, number boxes, popups and dropdown lists |
| `--pixata-grid-picker-bg`, `--pixata-grid-picker-hover-bg` | `#f5f5f5`, `#ebebeb` | The background of dropdowns and solid buttons |
| `--pixata-grid-chip-bg`, `--pixata-grid-chip-border-color` | `#ebebeb`, `#c2c2c2` | The chips in the group panel |
| `--pixata-grid-locked-border-color` | `rgba(0, 0, 0, .3)` | The lines either side of frozen columns |
| `--pixata-grid-skeleton-bg` | `rgba(61, 61, 61, .2)` | The placeholders shown while virtual rows load |
| `--pixata-grid-focus-color` | `rgba(61, 61, 61, .3)` | The ring round the cell the keyboard is on, in a `Navigable` grid |
| `--pixata-grid-highlight-bg` | `rgba(255, 192, 0, .25)` | Highlighted rows and cells |
| `--pixata-grid-focus-shadow` | `0 0 0 2px rgba(61, 61, 61, .08)` | The focus ring on inputs and buttons |
| `--pixata-grid-popup-shadow` | A soft two-layer shadow | The shadow under dropdown lists and filter menus |
| `--pixata-grid-border-radius` | `4px` | Rounded corners on inputs and buttons |
| `--pixata-grid-error-color` | `#ca1400` | The message shown when `OnRead` reports an error |

The grid's elements all have classes starting with `pixata-blazor-grid-`, so you can target them in your own CSS. It doesn't use any other library's classes, so it isn't affected by another library's theme on the same page.

## Templates and row events

A column's `Template` replaces the formatted value in each cell. The context is the row's item as an `object` (as in Telerik's grid), so cast it to your type. A template column doesn't need a `Field`, but if it has one, the column can be sorted and filtered by it...

```razor
<PixataGridColumn Title="Name" Field="@nameof(Person.Surname)">
  <Template>
    @{
      Person person = (Person)context;
      <a href="/people/@person.Id">@person.FirstName @person.Surname</a>
    }
  </Template>
</PixataGridColumn>
```

As the context is an `object`, templates built with the [`TemplateHelper`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Helpers.md#templatehelper) can be used as they are, and do the cast for you...

```razor
<PixataGridColumn Field="@nameof(Person.Salary)"
                  Template="@(TemplateHelper.Text<Person>(p => p.Salary.ToString("C2"), classCss: "text-end"))" />
```

`HeaderTemplate` does the same for the header (clicking it still sorts), and the grid's `RowTemplate` replaces a whole row's cells (render a `td` for each column).

`OnRowRender` and a column's `OnCellRender` let you add CSS classes based on the data. They're plain `Action`s, called as the grid renders...

```razor
<PixataGrid TItem="Person" Data="_people" OnRowRender="MarkOverdue">
  ...
</PixataGrid>

@code {
  private void MarkOverdue(PixataGridRowRenderEventArgs<Person> args) =>
    args.Class = args.Item.IsOverdue ? "overdue" : null;
}
```

`OnRowClick`, `OnRowDoubleClick` and `OnRowContextMenu` give you the item, the mouse event (for its position, to show a context menu), and the field of the column that was clicked. When `OnRowContextMenu` is handled, the browser's own menu isn't shown.

>If you pass a method rather than a lambda to one of these events, give the grid's `TItem` (and a column's, for `OnCellRender`), as Razor can't work out the type from a method.

## Selection

Set `SelectionMode` to `Single` or `Multiple`, and bind `SelectedItems`...

```razor
<PixataGrid Data="_people" SelectionMode="PixataGridSelectionMode.Multiple" @bind-SelectedItems="_selected">
  <GridColumns>
    <PixataGridCheckboxColumn />
    <PixataGridColumn Field="@nameof(Person.Name)" />
  </GridColumns>
</PixataGrid>
```

Clicking a row selects just that row, Ctrl+click adds or removes a row, and Shift+click selects the rows between it and the last row clicked. In multiple mode, the user can also press on a row and drag over others. The selection is kept as the user moves between pages, sorts and filters.

Selected items are matched with `Equals`. If your items are recreated (for example, read from a database again with `OnRead`), either override `Equals` in your class, use a record, or set `ItemKey`, such as `ItemKey="person => person.Id"`.

### Checkbox column

`PixataGridCheckboxColumn` adds a column of checkboxes, which add and remove rows without affecting the others. Its parameters are...

| Parameter | Type | What it does |
| --- | --- | --- |
| `SelectAll` | `bool` | Shows a box in the header that selects (or deselects) all the rows. Only in multiple mode. Defaults to `true`. It shows a dash when only some rows are selected |
| `SelectAllMode` | `PixataGridSelectAllMode` | `Current` (the default) selects the rows on the current page. `All` selects every row that passes the filters (with `OnRead`, the grid only has the current page) |
| `CheckBoxOnlySelection` | `bool` | When `true`, clicking a row doesn't select it, only its checkbox does |

It's 65px wide unless you give it a `Width`.

### Selecting cells

To select cells rather than rows, set the selection type in the grid's settings, and bind `SelectedCells`. Each `PixataGridSelectedCell<TItem>` has the row's `DataItem` and the `ColumnField`. Clicking, Ctrl, Shift and dragging work as for rows, with Shift and dragging selecting a rectangle...

```razor
<PixataGrid Data="_people" SelectionMode="PixataGridSelectionMode.Multiple" @bind-SelectedCells="_cells">
  <GridSettings>
    <PixataGridSelectionSettings SelectionType="PixataGridSelectionType.Cell" />
  </GridSettings>
  ...
</PixataGrid>
```

`PixataGridSelectionSettings` also has `DragToSelect` (default `true`), to turn off selecting by dragging.

## Editing

Set `EditMode` to `Inline` or `Popup`, add a `PixataGridCommandColumn` with the buttons you want (see [Using the row in a command column](#using-the-row-in-a-command-column) for buttons that depend on the row), and handle the events that change your data...

```razor
<PixataGrid TItem="Person" Data="_people" EditMode="PixataGridEditMode.Inline" ConfirmDelete="true"
            OnCreate="Create" OnUpdate="Update" OnDelete="Delete">
  <GridToolBarTemplate>
    <PixataGridCommandButton Command="Add" Icon="PixataGridIcon.Plus">Add person</PixataGridCommandButton>
  </GridToolBarTemplate>
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.Name)" />
    <PixataGridColumn Field="@nameof(Person.Email)" Editable="false" />
    <PixataGridCommandColumn Width="180px">
      <PixataGridCommandButton Command="Edit" Icon="PixataGridIcon.Pencil">Edit</PixataGridCommandButton>
      <PixataGridCommandButton Command="Delete" Icon="PixataGridIcon.Trash">Delete</PixataGridCommandButton>
      <PixataGridCommandButton Command="Save" Icon="PixataGridIcon.Save" ShowInEdit="true">Save</PixataGridCommandButton>
      <PixataGridCommandButton Command="Cancel" Icon="PixataGridIcon.Cancel" ShowInEdit="true">Cancel</PixataGridCommandButton>
    </PixataGridCommandColumn>
  </GridColumns>
</PixataGrid>

@code {
  private void Update(PixataGridCommandEventArgs<Person> args) =>
    _people[_people.FindIndex(p => p.Id == args.Item.Id)] = args.Item;
  ...
}
```

A few things to know...

- The user edits a shallow copy of the item, so cancelling leaves your data alone. In `OnUpdate`, `args.Item` is that copy, so put it in place of the original (or copy its values across). Nested objects are shared with the original, so changes to them can't be cancelled
- `Add` creates a new item (with its parameterless constructor, or `OnModelInit`), and shows it as a new row at the top (or in the popup). `OnCreate` is where you add it to your data
- In any event, set `args.IsCancelled = true` to stop what's happening. In `OnCreate` or `OnUpdate`, that keeps the row in edit mode, which is useful when saving fails
- With `OnRead`, the grid reads the data again after a change. With `Data`, it re-reads your collection
- The editors depend on the field's type: a text box for text, a number box, a date picker, a checkbox for `bool`, and a dropdown for enums. Columns that aren't editable show their value
- Edited items are validated with their data annotations before they're saved. In inline mode, an invalid editor gets a red border, with the message in its tooltip. In the popup, the message is shown under the editor. To use FluentValidation (or turn validation off), add `<PixataGridValidationSettings>` to the grid's `GridSettings`, with `<ValidatorTemplate><FluentValidationValidator /></ValidatorTemplate>` (or `Enabled="false"`)

### Editing a cell at a time

With `EditMode="PixataGridEditMode.InCell"`, clicking a cell edits just that cell, and the keyboard works as in a spreadsheet...

- Tab and Shift+Tab save the cell and move to the next or previous editable cell, carrying on into the next or previous row
- Enter saves the cell and moves to the same column in the next row
- Escape cancels the change
- Clicking anywhere else saves the cell

Each cell is saved with `OnUpdate`, whose `args.Field` and `args.Value` say which field changed. A value that fails validation keeps the cell open (with a red border) until it's fixed or cancelled. Add puts a new row at the top with its first editable cell open; the user can move around it with Tab, and it's created (with `OnCreate`) when they press Enter or click elsewhere.

### Command buttons

`PixataGridCommandButton` runs one of the built-in commands, `Add`, `Edit`, `Save`, `Cancel` or `Delete`. For anything else, leave out `Command` and handle `OnClick`, which is given the row's item. In a command column, a button shows when the row isn't being edited, or, with `ShowInEdit="true"`, when it is. `Icon` takes one of the `PixataGridIcon` values (or use `IconTemplate` for your own), and `ThemeColor="primary"` makes it stand out.

### Using the row in a command column

The content of a `PixataGridCommandColumn` is a template that's given the row's item, as an `object` (as in Telerik's grid, and as a column's `Template` is). So the buttons can depend on the row, and you can put anything else that needs the row in the column, such as a button of your own, a link or one of your components. You can mix these freely with `PixataGridCommandButton`s, which still know which row they're in. Cast the context to your type to use it...

```razor
<PixataGridCommandColumn Width="220px" Context="row">
  @{
    Invoice invoice = (Invoice)row;
  }
  <PixataGridCommandButton Command="Edit" Icon="PixataGridIcon.Pencil">Edit</PixataGridCommandButton>
  @if (invoice.IsPaid) {
    @* A button of your own, which acts on the row *@
    <button type="button" class="pixata-blazor-button pixata-blazor-button-solid" @onclick="() => ShowReceipt(invoice)">Receipt</button>
  } else {
    @* Only unpaid invoices can be deleted, with the grid's own Delete command (and ConfirmDelete, if it's set) *@
    <PixataGridCommandButton Command="Delete" Icon="PixataGridIcon.Trash">Delete</PixataGridCommandButton>
  }
  <PixataGridCommandButton Command="Save" Icon="PixataGridIcon.Save" ShowInEdit="true">Save</PixataGridCommandButton>
  <PixataGridCommandButton Command="Cancel" Icon="PixataGridIcon.Cancel" ShowInEdit="true">Cancel</PixataGridCommandButton>
</PixataGridCommandColumn>
```

A few things to know...

- Command columns that don't use the context carry on working as they are. The only time you need to do anything is when a template inside the column also uses the name `context` (for example a `PixataDropDownList` with an `ItemTemplate`), where Razor complains that it is used twice. Rename the column's with `Context="row"`, as above, as you would with Telerik
- While the row is being edited (inline, in the popup or a cell at a time), the context is the row as it was before editing started, not the copy being edited. When a new row is being added, it is the new item
- Your own controls are shown whether or not the row is being edited. To show them only for one or the other, or to tell whether the row is new, pick up the `PixataGridRowContext<TItem>` that the grid cascades to the column's content (it has `Item`, `IsEditing` and `IsNew`), in a component of your own with `[CascadingParameter] public PixataGridRowContext<Invoice>? RowContext { get; set; }`

### Popup editing

In `Popup` mode, the item is edited in a form in a window, titled "Edit" (or "Add new record"), with Update and Cancel buttons. Escape cancels. You can change it with these in the grid's `GridSettings`...

| Setting | What it does |
| --- | --- |
| `PixataGridPopupEditSettings` | `Title`, `Width`, `MaxHeight` and `Class` for the window |
| `PixataGridPopupEditFormSettings` | `Columns` (the number of columns the fields are laid out in), `ButtonsLayout` (`Start`, `Center`, `End` or `Stretch`), `FormTemplate` (your own form, given the item) and `ButtonsTemplate` (your own buttons) |

## Resizing, moving and freezing columns

With `Resizable="true"`, the user can drag the right edge of a header to resize its column, and double-click the edge to make the column as wide as its content (the header, the rows on the page and any footers, without wrapping). Once a column has been resized, every column keeps the width it had, and the table is as wide as its columns, so it may scroll sideways or leave a gap on the right. `MinResizableWidth` and `MaxResizableWidth` limit the width. You can fit columns to their content from code with `AutoFitColumn(id)`, `AutoFitColumns(ids)` and `AutoFitAllColumns()`, which use the columns' `Id`s, and work whether or not the grid is `Resizable`. Call them after the grid has rendered.

With `Reorderable="true"`, the user can drag a header onto another to move the column before or after it (a line shows where it will go). A column can only be moved among the columns it is with, so a column inside another stays inside it, and frozen columns can only be moved among frozen ones.

A column with `Locked="true"` is frozen, so it stays visible when the grid scrolls sideways. Each frozen column sticks after the frozen columns to its left and before those to its right, so frozen columns at the start stay on the left, those at the end stay on the right, and one in the middle scrolls until it reaches the frozen columns on either side. Frozen columns need a `Width` in pixels, and the grid needs to be narrower than its columns (give the grid a `Width`, or give the columns enough width) for there to be anything to scroll. If any frozen column hasn't got a width in pixels, none are frozen.

The dragging uses pointer events, so it works with touch as well as the mouse.

## Multi-column headers

A column can contain other columns, which share a header above their own...

```razor
<PixataGrid Data="People">
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.Id)" Width="80px" />
    <PixataGridColumn Title="Name">
      <Columns>
        <PixataGridColumn Field="@nameof(Person.FirstName)" />
        <PixataGridColumn Field="@nameof(Person.Surname)" />
      </Columns>
    </PixataGridColumn>
  </GridColumns>
</PixataGrid>
```

The containing column only has a header, so it doesn't need a `Field`. Columns can be nested as deeply as you like. Hiding a containing column hides the columns inside it, and if all of those are hidden, it's hidden too.

## The column menu

With `ShowColumnMenu="true"`, each header has a menu button. Its menu has...

- Sort Ascending and Sort Descending, for a sortable column. Picking the sort the column already has removes it
- Columns, a list of the columns with a tick box for each. Untick some and click Apply to hide them, or Reset to go back to how they were. The last ticked column can't be unticked
- Filter, the same form as the filter menu, for a filterable column when the grid has a `FilterMode`
- Group column (or Ungroup column), when the grid is `Groupable`
- Set column position, with Lock and Unlock (to freeze and unfreeze the column), and Move Previous and Move Next. A frozen column moves to the end of the frozen columns on the left, and an unfrozen one to just after them. A column frozen by its `Locked` parameter can't be unfrozen

Only one of the sections expands at a time. Choose what's offered with `PixataGridColumnMenuSettings` in the grid's `GridSettings`...

| Setting | Type | What it does |
| --- | --- | --- |
| `ShowColumnChooser` | `bool` | The list of columns. Defaults to `true` |
| `Sortable` | `bool` | Sorting. Defaults to `true` |
| `FilterMode` | `PixataGridColumnMenuFilterMode` | `FilterMenu` (the default) or `None` |
| `Lockable` | `bool` | Lock and Unlock. Defaults to `true` |
| `Groupable` | `bool` | Group column. Defaults to `false` |
| `Reorderable` | `bool` | Move Previous and Move Next. Defaults to `false` |

The column's own parameters (such as `Sortable="false"` or `Lockable="false"`) take precedence. A column with `ShowColumnMenu="false"` has no menu, but keeps its filter menu button if the grid's `FilterMode` is `FilterMenu`.

## Detail rows

Give the grid a `DetailTemplate`, and each row gets an arrow at its start that shows and hides a row of details under it. The context is the row's item, and the template can hold anything, including another grid...

```razor
<PixataGrid Data="Teams">
  <DetailTemplate>
    <PixataGrid Data="context.Members">...</PixataGrid>
  </DetailTemplate>
  <GridColumns>...</GridColumns>
</PixataGrid>
```

`OnRowExpand` and `OnRowCollapse` fire as the user shows and hides details, so you can load the details when they are first wanted. The expanded rows are part of the [state](#state), as `ExpandedItems`. Detail rows don't count for the striping, and the arrows go after the cells for any grouping.

## Dragging rows

Set `RowDraggable="true"`, and the user can drag rows by a handle at the start of each row (add a `PixataGridDragColumn` among the columns to put the handles somewhere else), within the grid or into another grid. A line shows where a row will go, or the row it will be dropped on is tinted. The grid doesn't move anything itself, as only you know how your data is stored, so handle `OnRowDrop`...

```razor
<PixataGrid @ref="_left" Data="_leftPeople" RowDraggable="true" OnRowDrop="@((PixataGridRowDropEventArgs<Person> args) => Dropped(args, _leftPeople))">
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.Name)" />
  </GridColumns>
</PixataGrid>

@code {
  private void Dropped(PixataGridRowDropEventArgs<Person> args, List<Person> source) {
    if (args.DestinationGrid is null) {
      return;
    }
    List<Person> destination = args.DestinationGrid == _left ? _leftPeople : _rightPeople;
    source.RemoveAll(args.Items.Contains);
    int index = args.DestinationItem is null ? destination.Count : destination.IndexOf(args.DestinationItem) + (args.DropPosition == PixataGridDropPosition.After ? 1 : 0);
    destination.InsertRange(index, args.Items);
  }
}
```

| Member of the event args | What it is |
| --- | --- |
| `Item` | The row that was dragged |
| `Items` | The rows that were dragged: all the selected rows if the dragged row was selected, otherwise just that one |
| `DestinationGrid` | The grid they were dropped on (which may be the same one), or null if they were dropped somewhere else |
| `DestinationItem`, `DestinationIndex` | The row they were dropped on and its position among the rows shown, or null if they were dropped on the grid but not on a row |
| `DropPosition` | `Before`, `Over` or `After` the destination row: the top quarter of a row is before it, the bottom quarter after it, and the rest over it |

Rows can only be dropped on the rows of a grid that is itself `RowDraggable`, with the same `TItem`. Dropped anywhere else in such a grid, they go after its rows. The dragging uses pointer events, so it works with touch as well as the mouse.

## Keyboard navigation and accessibility

With `Navigable="true"`, the grid is a single tab stop, and the keyboard moves between its cells (header cells, filter cells and data cells)...

| Key | What it does |
| --- | --- |
| Arrow keys | Move to the next cell in that direction. In a right-to-left page, left and right are swapped |
| Home, End | Move to the first or last cell in the row |
| Ctrl+Home, Ctrl+End | Move to the first cell of the grid, or the last |
| Page Up, Page Down | Go to the previous or next page, keeping the focus in the grid |
| Enter | On a header, sort by it. On a cell, edit it (in `InCell` mode) or its row (in `Inline` and `Popup` modes) |
| F2 | Edit, as Enter does on a cell |
| Space | Select the row (or the cell, when selecting cells) |
| Alt+Down | On a header, open its column menu or filter menu |
| Escape | Cancel editing |

A `Navigable` grid's table has the ARIA `grid` role, with `aria-rowcount` and `aria-colcount` for all the data (not just the rows rendered), and each data row has its `aria-rowindex`, so screen readers can say where the user is. Without `Navigable`, the grid is a normal table, which screen readers can also read, and its sortable headers are each a tab stop. Either way, sorted headers have `aria-sort`, selected rows `aria-selected`, menus are dialogs with labels, and the buttons have labels.

## Highlighting

`HighlightedItems` and `HighlightedCells` draw attention to rows and cells, such as ones that have just changed, without selecting them...

```razor
<PixataGrid Data="People" HighlightedItems="_changed" HighlightedCells="_flagged">...</PixataGrid>

@code {
  private List<Person> _changed = [];
  private List<PixataGridHighlightedCell<Person>> _flagged = [];
}
```

Items are matched with `ItemKey` if the grid has one, otherwise with `Equals`.

## Translation

Everything the grid says itself (the pager, the filter operators, the menus, the messages and the labels read by screen readers) comes from a `PixataGridMessages`, which is English by default. To translate it, create one with the text you want (anything you leave out stays English), and either register it, so every grid uses it, or give it to a grid...

```csharp
builder.Services.AddSingleton(new PixataGridMessages {
  NoRecords = "אין רשומות.",
  PagerInfoFormat = "{0} - {1} מתוך {2} פריטים",
  Contains = "מכיל"
});
```

```razor
<PixataGrid Data="People" Messages="_hebrew">...</PixataGrid>
```

The properties ending in "Format" are format strings, with {0} and so on for the values. See the [class](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Components/Grid/PixataGridMessages.cs) for the full list. A search box's placeholder comes from the messages too, unless you give it a `Placeholder`.

## Right to left

Put the grid inside an element with `dir="rtl"` (or set it on the page), and the grid is laid out from right to left: the columns, the pager, the group panel, the menus and the popups. Frozen columns stick to the right, column resize handles are on the left edge of the headers, and arrows that point along the line of text point the other way. The keyboard and the dragging work the right way round.

## Small screens

The grid works on phones as it is: it scrolls sideways if it needs to, the pager adapts to its width, and the dragging works with touch. With `AdaptiveMode="PixataGridAdaptiveMode.Auto"`, on screens 768px wide or less, the filter menu and column menu fill the screen, with a title bar and a close button, and so does the popup editor, as a small popup would be cramped.

## Export

The grid can be saved as a CSV file, with a command button in the toolbar or from code. Excel export is in a separate package, [Pixata.Blazor.Grid.Excel](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Grid.Excel/Readme.md), so apps that don't need it don't carry its weight...

```razor
<PixataGrid @ref="_grid" Data="People" Pageable="true">
  <GridToolBarTemplate>
    <PixataGridCommandButton Command="CsvExport" Icon="PixataGridIcon.FileCsv">Export to CSV</PixataGridCommandButton>
  </GridToolBarTemplate>
  <GridExport>
    <PixataGridCsvExport FileName="people" AllPages="true" />
  </GridExport>
  <GridColumns>...</GridColumns>
</PixataGrid>
```

| Setting | Type | What it does |
| --- | --- | --- |
| `FileName` | `string` | The file name, without its extension. Defaults to "Grid" |
| `AllPages` | `bool` | Exports all the rows (filtered and sorted as in the grid), rather than just the page shown. With `OnRead`, the handler is called with a request for all the rows (a `PageSize` of zero) |
| `OnBeforeExport` | `EventCallback<PixataGridBeforeExportEventArgs<TItem>>` | Fires before the export, with the `Columns` (each with a `Field`, `Title`, `Width` and `DisplayFormat`) and `Data`, which you can change. Set `IsCancelled` to stop it |
| `OnAfterExport` | `EventCallback<PixataGridAfterExportEventArgs>` | Fires after the export, with the file as a `Stream` |

Without a `PixataGridCsvExport`, the `CsvExport` command still works, with the defaults. From code, `SaveAsCsvFile()` exports and has the browser save the file, and `ExportToCsv()` returns it as a `MemoryStream` (or null if `OnBeforeExport` cancelled it).

The CSV has a heading row with the column titles, then a row for each item. Only columns that are shown and have a `Field` are exported, so command, checkbox and drag columns are left out. The values aren't formatted with the `DisplayFormat`, so the file can be read back accurately: numbers have a full stop for the decimal point and no thousands separators, true and false are written as TRUE and FALSE, and dates as the current culture writes them. Values with a comma, quote or line break are quoted. The file is UTF-8, with a byte order mark so Excel reads it correctly.

## Virtual scrolling

For a lot of rows, set `ScrollMode="PixataGridScrollMode.Virtual"`, with a `Height` and a `RowHeight` (in pixels). Instead of paging, the grid scrolls through all the rows, but only renders three pages of them (`PageSize` is the number in view at once, so set it to about the number that fit in the height), and renders others as the user scrolls...

```razor
<PixataGrid Data="People" ScrollMode="PixataGridScrollMode.Virtual" Height="400px" RowHeight="36" PageSize="20" Sortable="true">
  ...
</PixataGrid>
```

- Every row is `RowHeight` pixels high, and text in the cells doesn't wrap. Use the height of a normal row (36px for the medium size) unless your templates make rows taller
- With `OnRead`, the request has a `Skip` (the number of rows before the ones wanted) instead of a page, and a `PageSize` of three pages. `ToGridResult()` and `ToGridResultAsync()` handle it for you. While the rows load, placeholders are shown in their place
- Filtering and searching go back to the top. Sorting stays where it is
- There's no pager, so `Pageable` is ignored
- It can't be combined with grouping, and the grid throws an exception if you try, or if `Height` or `RowHeight` is missing

### Virtual columns

For a grid with a lot of columns, set `ColumnVirtualization="true"`, and each row only has cells for the columns in view (and one either side), with the rest filled by an empty cell spanning them. The headers are all there, so resizing, moving and the column menu work as normal. Every column needs a `Width` in pixels; if one hasn't, all the cells are rendered. It works with or without virtual rows.

## State

The grid's state is everything the user can change: the page and page size, sorting, filters, the search, grouping and collapsed groups, each column's position, width, visibility and freezing, the table width after a resize, the selected items, and any editing. `GetState()` returns it as a `PixataGridState<TItem>`, and `SetState(state)` puts it back (`SetState(null)` goes back to how the grid started). The state serialises to JSON as it is, so you can save it wherever you like...

```razor
<PixataGrid @ref="_grid" Data="People" Sortable="true" Pageable="true" OnStateChanged="StateChanged">...</PixataGrid>

@code {
  private PixataGrid<Person> _grid = null!;

  private async Task StateChanged(PixataGridStateEventArgs<Person> args) =>
    await Settings.Save("people-grid", JsonSerializer.Serialize(args.GridState));
}
```

- `OnStateInit` fires once, before the data is first read. Change `args.GridState` (or replace it) to set how the grid starts, such as sorted by a column or grouped
- `OnStateChanged` fires after the user changes the state, once for each part that changed, with `args.PropertyName` saying which, such as "SortDescriptors", "Page" or "ColumnStates". It isn't raised for `SetState`. Items are compared by reference, so it fires when editing starts and stops, not as the user types

Columns are matched to their saved state by `Id` if they have one, otherwise by `Field`, otherwise by title, so give columns an `Id` if two could match. Filter values come back from JSON as `JsonElement`s, and the grid turns them back into the field's type. Selected and edited items come back as copies, so they are matched to the data with `ItemKey` (or `Equals`, which works for records).

### Saving the state in local storage

Set `StorageKey`, and the grid saves its state in the browser's local storage under that key whenever it changes, and restores it when the grid is next shown (before `OnStateInit`, which can still change it). It uses Blazored.LocalStorage, which `AddPixataBlazor()` registers. Without it, a grid with a `StorageKey` throws an exception saying so.

While a page is being prerendered, local storage can't be read, so the prerendered page shows the grid as it starts, and the saved state appears when the page becomes interactive. This is handled for you, as is an `OnStateInit` handler that uses JavaScript, which gets another go once the page is interactive.

## Grouping and aggregates

With `Groupable="true"`, there's a panel above the grid. Drag a column header into it to group the rows by that column, and drag more to group within groups. Each group has a header row ("Pet: Cat"), with an arrow that collapses and expands it. Each grouping is shown as a chip in the panel. Click the chip's cross (or press Delete when it has the focus) to stop grouping by it, drag a chip onto another to change the order, or drag it out of the panel to remove it. The dragging uses pointer events, so it works with touch as well as the mouse. Call `GroupBy(field)` and `Ungroup(field)` to do the same in code.

To show totals and so on, ask for the aggregates in `GridAggregates`, and show them in the columns' templates...

```razor
<PixataGrid Data="People" Groupable="true" Pageable="true">
  <GridAggregates>
    <PixataGridAggregate Field="@nameof(Person.Salary)" Aggregate="GridAggregateType.Sum" />
    <PixataGridAggregate Field="@nameof(Person.Name)" Aggregate="GridAggregateType.Count" />
  </GridAggregates>
  <GridColumns>
    <PixataGridColumn Field="@nameof(Person.Name)">
      <FooterTemplate>Total: @context.Count</FooterTemplate>
    </PixataGridColumn>
    <PixataGridColumn Field="@nameof(Person.Salary)" DisplayFormat="{0:C0}">
      <GroupFooterTemplate>Sum: @(((decimal?)context.Sum)?.ToString("C0"))</GroupFooterTemplate>
      <FooterTemplate>Sum: @(((decimal?)context.Sum)?.ToString("C0"))</FooterTemplate>
    </PixataGridColumn>
  </GridColumns>
</PixataGrid>
```

The aggregate types are `Count`, `Sum`, `Average`, `Min` and `Max` (`GridAggregateType`, in `Pixata.Extensions.Grid`. If another library you use has a type with the same name, you'll need to say which you mean). The template contexts have a property for each, which is null if it wasn't asked for, and `AggregateResults`, which has the aggregates for every field, so one column can show another's. The group templates' context also has `Value` (the group's value), `GroupField` and `ItemsCount`.

A few things to know...

- The aggregates, and the group counts, are for all the data (or the whole group), not just the page
- Paging counts the items, not the group rows, so a group can carry on to the next page, with its header shown again
- A collapsed group still shows its footer row
- The rows are striped across groups, rather than starting again in each one
- Clicking a chip does nothing, and the groups are in ascending order
- Grouping goes back to the first page, and expands all the groups

With `OnRead`, the request includes the groups and aggregates, and `ToGridResult()` and `ToGridResultAsync()` fill in the result's `Groups` and `Aggregates`, which `SetResult()` passes on. If you set `Data` and `Total` yourself and leave out `Groups`, the grid groups the page it's given, but then the group counts and aggregates only cover that page.

## Supplying the data with OnRead

For a lot of data, or data from an API, handle `OnRead` instead of setting `Data`. The grid calls it when it first renders, and whenever the page, sorting, filtering or search changes, with a `GridRequest` saying what it wants. You set the page of data and the total number of items...

```razor
<PixataGrid TItem="Person" OnRead="ReadPeople" Pageable="true" Sortable="true" FilterMode="PixataGridFilterMode.FilterRow">
  <GridColumns>...</GridColumns>
</PixataGrid>

@code {
  private async Task ReadPeople(PixataGridReadEventArgs<Person> args) =>
    args.SetResult(await Db.People.OrderBy(p => p.Id).ToGridResultAsync(args.Request, args.CancellationToken));
}
```

You need to give `TItem`, as the grid can't work it out without `Data`. `ToGridResult()` (in Pixata.Extensions) applies the request to any query, and `ToGridResultAsync()` (in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Grid.md)) does it with EF Core's async methods. In a WASM app, send `args.Request` to your API, and have the server call one of those. `GridRequest` and `GridResult<T>` serialise with `System.Text.Json` as they are.

| Member of the event args | What it is |
| --- | --- |
| `Request` | The page, page size, sorts, filters, search, groups and aggregates the grid wants |
| `CancellationToken` | Cancelled if the grid asks for something else before this has finished (such as when the user types quickly in a filter). Pass it on to your database or API calls |
| `Data`, `Total` | Set these to the page of items and the number of items before paging. Or call `SetResult(gridResult)` |
| `Groups`, `Aggregates` | The groups the page's items are in, and the aggregates over all the data, if the request asked for them. `SetResult(gridResult)` sets these too |
| `ErrorMessage` | Set this to show a message in the grid instead of rows. `SetResult(apiResponse)` does this for you when given a failed `ApiResponse<GridResult<T>>` |

A few things to know...

- `OnRead` isn't called again when the parent component re-renders. Call `Rebind()` when you know the data has changed
- If a slow request finishes after a newer one, its result is ignored, so the grid always shows what was asked for last
- When a read takes more than 600ms, a loading indicator covers the rows (turn it off with `EnableLoaderContainer="false"`). It isn't shown for the first read, which is awaited so the data is there when the page is prerendered
- If the page is past the end of the data, the grid moves to the last page and asks again
- A checkbox-list filter can only offer the values on the current page, as the grid doesn't have the rest
- Setting both `Data` and `OnRead` throws an exception
