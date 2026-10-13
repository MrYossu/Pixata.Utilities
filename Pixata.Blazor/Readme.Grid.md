# PixataGrid

>Part of [Pixata.Blazor](Readme.md)

`PixataGrid` is a data grid that looks and behaves like the [Telerik Blazor grid](https://www.telerik.com/blazor-ui/documentation/components/grid/overview), but doesn't need a Telerik licence. Its parameters, events and methods have the same names as Telerik's, so moving a grid from one to the other is mostly a matter of renaming the components.

>**Work in progress.** The grid is being built up in stages, tracked in [issue #49](https://github.com/MrYossu/Pixata.Utilities/issues/49). This page describes what's there so far. The [sample page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/PixataGridSample.razor) shows each feature beside a Telerik grid with the same settings.

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

Telerik's column component is called `GridColumn`. If ours were too, any app that uses both (including our own sample) would get ambiguous reference errors wherever both namespaces are imported. So all the grid's components and types start with `PixataGrid`, while their parameters match Telerik's.

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
| `Size` | `PixataGridSize` | `Medium` (the default) or `Small`, which sets the padding around the cells. Like Telerik's grid, there's no large size |
| `NoDataTemplate` | `RenderFragment` | What to show when there are no items. Defaults to "No records available." |
| `GridSettings` | `RenderFragment` | Settings for parts of the grid, such as the [pager](#paging) |
| `Pageable` | `bool` | Splits the rows into pages, with a pager. See [paging](#paging) |
| `PageSize` | `int` | The number of rows on a page. Defaults to 10. Can be two-way bound |
| `Page` | `int` | The current page, starting from 1. Can be two-way bound |
| `Sortable` | `bool` | Lets the user sort by clicking a column header. See [sorting](#sorting) |
| `SortMode` | `PixataGridSortMode` | `Single` (the default) or `Multiple` |
| `FilterMode` | `PixataGridFilterMode` | `None` (the default), `FilterRow` or `FilterMenu`. See [filtering](#filtering) |
| `GridToolBarTemplate` | `RenderFragment` | Content for a toolbar above the grid, such as a [search box](#search-box) |
| `RowTemplate` | `RenderFragment<TItem>` | The content of each row, in place of the grid's cells. See [templates](#templates-and-row-events) |
| `LoaderTemplate` | `RenderFragment` | What the loading indicator shows, in place of the dots |
| `OnRowRender` | `Action<PixataGridRowRenderEventArgs<TItem>>` | Lets you set a CSS class on each row |
| `OnRowClick`, `OnRowDoubleClick`, `OnRowContextMenu` | `EventCallback<PixataGridRowClickEventArgs<TItem>>` | Fire when the user clicks, double-clicks or right-clicks a row |
| `SelectionMode` | `PixataGridSelectionMode` | `None` (the default), `Single` or `Multiple`. See [selection](#selection) |
| `SelectedItems` | `IEnumerable<TItem>` | The selected rows. Can be two-way bound |
| `SelectedCells` | `IEnumerable<PixataGridSelectedCell<TItem>>` | The selected cells, when selecting cells. Can be two-way bound |
| `ItemKey` | `Func<TItem, object?>` | Gets a key (such as the id) used to match selected items, instead of `Equals` |
| `EditMode` | `PixataGridEditMode` | `None` (the default), `Inline` or `Popup`. See [editing](#editing) |
| `OnAdd`, `OnCreate`, `OnEdit`, `OnUpdate`, `OnDelete`, `OnCancel` | `EventCallback<PixataGridCommandEventArgs<TItem>>` | The editing events |
| `OnModelInit` | `Func<TItem>` | Creates a new item, if `TItem` has no parameterless constructor |
| `ConfirmDelete` | `bool` | Asks the user to confirm before deleting |

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
| `FilterCellTemplate`, `FilterMenuTemplate` | `RenderFragment<PixataGridFilterTemplateContext>` | Your own filter UI. See [custom filters](#custom-filters) |
| `Template` | `RenderFragment<TItem>` | The content of each data cell. See [templates](#templates-and-row-events) |
| `HeaderTemplate` | `RenderFragment` | The content of the header cell, in place of the title |
| `OnCellRender` | `Action<PixataGridCellRenderEventArgs<TItem>>` | Lets you set a CSS class on each cell |
| `Editable` | `bool` | Set to `false` to stop the column being edited. Defaults to `true`, but a property with no public setter is never editable |
| `EditorType` | `PixataGridEditorType?` | The editor to use, if not the default for the field's type |
| `EditorTemplate` | `RenderFragment<TItem>` | Your own editor. The context is the copy being edited |

If you don't set a `Title`, the grid uses the `[Display(Name = "...")]` attribute on the property if there is one. If not, it uses the field name split into words with `SplitCamelCase()`, so `DateOfBirth` becomes "Date of birth" (for a nested field, it uses the last part of the path). **This differs from Telerik on purpose**, as Telerik shows the field name unchanged.

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
| `Position` | `PixataGridPagerPosition` | `Bottom` (the default) or `Top`. Like Telerik's grid, there's no option to have both |
| `Adaptive` | `bool` | Whether the pager adapts to its width. Defaults to true. As it narrows, the "1 - 10 of 91 items" text goes first, then the page size dropdown, and finally the page buttons become a dropdown |

As in Telerik's grid...

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

As in Telerik's grid...

- Sorting uses the normal .NET ordering for the field's type, so strings are compared using the current culture, and enums by their underlying value
- Sorting doesn't change the page, so you see the same page of the newly sorted data
- A nested field such as "Address.City" sorts safely when part of the path is null. Those rows count as null, which comes first in ascending order

Setting the initial sort from code will be possible through the grid's state, when that's added.

## Filtering

Set `FilterMode` to choose how the user filters...

- `FilterRow` adds a row under the headers, with an editor for each column. Text and numbers are filtered a moment after the user stops typing, and the other editors filter straight away. The button next to each editor picks the operator (such as "Starts with"), and the one beside it clears the filter
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

The editor and the operators depend on the field's type, as in Telerik's grid...

| Type | Editor | Operators |
| --- | --- | --- |
| Text | A text box | Is equal to, Is not equal to, Starts with, Contains (the default), Does not contain, Ends with, Is null, Is not null, Is empty, Is not empty, Has no value, Has value |
| Numbers | A number box with up and down buttons | Is less than, Is less than or equal to, Is equal to, Is not equal to, Is greater than or equal to, Is greater than |
| `DateTime` and `DateOnly` | A [date picker](Readme.DatePicker.md) | Is equal to, Is not equal to, Is after or equal to, Is after, Is before or equal to, Is before |
| Enums | A dropdown of the values | Is equal to, Is not equal to |
| `bool` | A dropdown of (All), is true and is false | None |

Nullable numbers, dates and enums also get Is null and Is not null. Columns of other types can't be filtered unless you give them a template.

As in Telerik's grid, text comparisons ignore case, and filtering (or searching) goes back to the first page.

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

A column's `FilterCellTemplate` replaces its cell in the filter row, and `FilterMenuTemplate` replaces the conditions in its filter menu (the Filter and Clear buttons are still there). Both are given a `PixataGridFilterTemplateContext`, whose `Filter` is the column's `GridCompositeFilterDescriptor`. Add or change its descriptors, then call `FilterAsync()` to apply them, or `ClearFilterAsync()` to remove the filter...

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

The grid's colours, borders and spacing match Telerik's Default theme. To change them, override these custom properties, either on `:root` or on any container around the grid...

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
| `--pixata-grid-focus-shadow` | `0 0 0 2px rgba(61, 61, 61, .08)` | The focus ring on inputs and buttons |
| `--pixata-grid-popup-shadow` | Telerik's popup shadow | The shadow under dropdown lists and filter menus |
| `--pixata-grid-border-radius` | `4px` | Rounded corners on inputs and buttons |
| `--pixata-grid-error-color` | `#ca1400` | The message shown when `OnRead` reports an error |

The grid's elements all have classes starting with `pixata-blazor-grid-`, so you can target them in your own CSS. The grid doesn't use Telerik's `k-` classes, so it isn't affected by a Telerik theme on the same page.

## Templates and row events

A column's `Template` replaces the formatted value in each cell. Unlike Telerik's, the context is the row's item itself, so there's no need to cast it (although `context as Person` still compiles, if you're moving from Telerik). A template column doesn't need a `Field`, but if it has one, the column can be sorted and filtered by it...

```razor
<PixataGridColumn Title="Name" Field="@nameof(Person.Surname)">
  <Template>
    <a href="/people/@context.Id">@context.FirstName @context.Surname</a>
  </Template>
</PixataGridColumn>
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

As in Telerik's grid, clicking a row selects just that row, Ctrl+click adds or removes a row, and Shift+click selects the rows between it and the last row clicked. In multiple mode, the user can also press on a row and drag over others. The selection is kept as the user moves between pages, sorts and filters.

Selected items are matched with `Equals`. If your items are recreated (for example, read from a database again with `OnRead`), either override `Equals` in your class, use a record, or set `ItemKey`, such as `ItemKey="person => person.Id"`.

### Checkbox column

`PixataGridCheckboxColumn` adds a column of checkboxes, which add and remove rows without affecting the others. Its parameters are...

| Parameter | Type | What it does |
| --- | --- | --- |
| `SelectAll` | `bool` | Shows a box in the header that selects (or deselects) all the rows. Only in multiple mode. Defaults to `true`. It shows a dash when only some rows are selected |
| `SelectAllMode` | `PixataGridSelectAllMode` | `Current` (the default) selects the rows on the current page. `All` selects every row that passes the filters (with `OnRead`, the grid only has the current page) |
| `CheckBoxOnlySelection` | `bool` | When `true`, clicking a row doesn't select it, only its checkbox does |

It's 65px wide unless you give it a `Width`, as in Telerik's grid.

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

Set `EditMode` to `Inline` or `Popup`, add a `PixataGridCommandColumn` with the buttons you want, and handle the events that change your data...

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

As in Telerik's grid...

- The user edits a shallow copy of the item, so cancelling leaves your data alone. In `OnUpdate`, `args.Item` is that copy, so put it in place of the original (or copy its values across). Nested objects are shared with the original, so changes to them can't be cancelled
- `Add` creates a new item (with its parameterless constructor, or `OnModelInit`), and shows it as a new row at the top (or in the popup). `OnCreate` is where you add it to your data
- In any event, set `args.IsCancelled = true` to stop what's happening. In `OnCreate` or `OnUpdate`, that keeps the row in edit mode, which is useful when saving fails
- With `OnRead`, the grid reads the data again after a change. With `Data`, it re-reads your collection
- The editors depend on the field's type: a text box for text, a number box, a date picker, a checkbox for `bool`, and a dropdown for enums. Columns that aren't editable show their value
- Edited items are validated with their data annotations before they're saved. In inline mode, an invalid editor gets a red border, with the message in its tooltip. In the popup, the message is shown under the editor. To use FluentValidation (or turn validation off), add `<PixataGridValidationSettings>` to the grid's `GridSettings`, with `<ValidatorTemplate><FluentValidationValidator /></ValidatorTemplate>` (or `Enabled="false"`)

### Command buttons

`PixataGridCommandButton` runs one of the built-in commands, `Add`, `Edit`, `Save`, `Cancel` or `Delete`. For anything else, leave out `Command` and handle `OnClick`, which is given the row's item. In a command column, a button shows when the row isn't being edited, or, with `ShowInEdit="true"`, when it is. `Icon` takes one of the `PixataGridIcon` values (or use `IconTemplate` for your own), and `ThemeColor="primary"` makes it stand out.

### Popup editing

In `Popup` mode, the item is edited in a form in a window, titled "Edit" (or "Add new record"), with Update and Cancel buttons. Escape cancels. You can change it with these in the grid's `GridSettings`...

| Setting | What it does |
| --- | --- |
| `PixataGridPopupEditSettings` | `Title`, `Width`, `MaxHeight` and `Class` for the window |
| `PixataGridPopupEditFormSettings` | `Columns` (the number of columns the fields are laid out in), `ButtonsLayout` (`Start`, `Center`, `End` or `Stretch`), `FormTemplate` (your own form, given the item) and `ButtonsTemplate` (your own buttons) |

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
| `Request` | The page, page size, sorts, filters and search the grid wants |
| `CancellationToken` | Cancelled if the grid asks for something else before this has finished (such as when the user types quickly in a filter). Pass it on to your database or API calls |
| `Data`, `Total` | Set these to the page of items and the number of items before paging. Or call `SetResult(gridResult)` |
| `ErrorMessage` | Set this to show a message in the grid instead of rows. `SetResult(apiResponse)` does this for you when given a failed `ApiResponse<GridResult<T>>` |

A few things to know...

- `OnRead` isn't called again when the parent component re-renders. Call `Rebind()` when you know the data has changed
- If a slow request finishes after a newer one, its result is ignored, so the grid always shows what was asked for last
- When a read takes more than 600ms, a loading indicator covers the rows (turn it off with `EnableLoaderContainer="false"`). As in Telerik, it isn't shown for the first read, which is awaited so the data is there when the page is prerendered
- If the page is past the end of the data, the grid moves to the last page and asks again
- A checkbox-list filter can only offer the values on the current page, as the grid doesn't have the rest
- Setting both `Data` and `OnRead` throws an exception
