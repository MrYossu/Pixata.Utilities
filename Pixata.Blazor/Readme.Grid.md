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
| `GridColumns` | `RenderFragment` | The column definitions |
| `AutoGenerateColumns` | `bool` | Adds a column for each simple property of `TItem`. See [auto-generated columns](#auto-generated-columns) |
| `Height` | `string` | A CSS height, such as "400px". If set, the rows scroll under a fixed header. If not, the grid is as tall as its rows |
| `Width` | `string` | A CSS width. If not set, the grid fills its container |
| `Class` | `string` | A CSS class for the grid's outer element |
| `Size` | `PixataGridSize` | `Medium` (the default) or `Small`, which sets the padding around the cells. Like Telerik's grid, there's no large size |
| `NoDataTemplate` | `RenderFragment` | What to show when there are no items. Defaults to "No records available." |

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

The grid's elements all have classes starting with `pixata-grid-`, so you can target them in your own CSS. The grid doesn't use Telerik's `k-` classes, so it isn't affected by a Telerik theme on the same page.

## Using the request types on the server

When the grid gets its data, it describes what it wants with a `GridRequest`, and gets back a `GridResult<T>`. These are in Pixata.Extensions (namespace `Pixata.Extensions.Grid`), so that a WASM client and a server can both use them. For now the request only holds the page, but sorting, filtering and grouping will be added as the grid gets them, along with a way for you to supply the data yourself. See [Pixata.Extensions](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.SharedModels.md#grid-requests) for details.
