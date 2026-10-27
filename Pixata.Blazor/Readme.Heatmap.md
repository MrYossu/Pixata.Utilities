# Heatmap

>Part of [Pixata.Blazor](Readme.md)

`PixataHeatmap` shows a grid of values as coloured cells, from a low colour for the smallest value to a high colour for the largest, with a label for each row and column. A typical use is the days of the week against the hours of the day, to show when something is busy.

See the [live demo](https://test.pixata.co.uk/HeatmapSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/HeatmapSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

Give it the row labels, the column labels, and a value for each cell in a two-dimensional array, indexed by row then column...

```xml
<PixataHeatmap Title="Visitors per hour"
               RowLabels="@_days"
               ColumnLabels="@_hours"
               Values="@_visitors" />

@code {
  private readonly string[] _days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
  private readonly string[] _hours = Enumerable.Range(0, 24).Select(h => $"{h:00}").ToArray();
  private double?[,] _visitors = new double?[7, 24];
}
```

The array must have a row for each row label and a column for each column label, or the heatmap throws an `InvalidOperationException`. A null value is a cell with no data, which is shown with an outline rather than a colour.

The grid is a table, with the labels as row and column headers, so screen readers can read it. Each cell's value is in its text, hidden unless `ShowValues` is set, as well as in its tooltip. On a narrow screen, the grid scrolls sideways rather than squashing the cells.

## The scale

By default, the smallest value is shown in the low colour and the largest in the high colour, with the others in between. Set `Min` and `Max` to fix the ends of the scale instead, such as to compare two heatmaps, or to start the scale at zero. Values outside them are shown in the low or high colour. If every value is the same, they are all shown in the low colour.

A cell 60% or more of the way up the scale gets the `pixata-blazor-heatmap-cell-strong` class, which gives its text the strong colour, so that it shows on the darker background.

The legend under the grid is a bar from the low colour to the high colour, with the values at its ends. Set `ShowLegend="false"` to leave it out. It's left out anyway if there are no values.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `RowLabels` | `IReadOnlyList<string>` | | The label of each row, from the top |
| `ColumnLabels` | `IReadOnlyList<string>` | | The label of each column, from the left |
| `Values` | `double?[,]` | | The value of each cell, indexed by row then column. Null for a cell with no data |
| `Min` | `double?` | The smallest value | The value shown in the low colour |
| `Max` | `double?` | The largest value | The value shown in the high colour |
| `ShowValues` | `bool` | `false` | Whether the values are shown in the cells |
| `Format` | `string` | `"{0:N0}"` | The composite format for the values, in the cells, tooltips and legend |
| `EmptyText` | `string` | `"No data"` | The text for a cell with no value, in its tooltip and for screen readers |
| `CellTooltip` | `Func<PixataHeatmapCell, string>` | | The tooltip for each cell. By default it's the row and column labels and the value, eg "Mon, 09: 12" |
| `ShowLegend` | `bool` | `true` | Whether to show the scale under the grid |
| `Title` | `string` | | The title above the grid, which also names the table for screen readers |
| `AriaLabel` | `string` | | Names the table for screen readers when there's no `Title` |
| `CornerText` | `string` | | The text in the top left corner, above the row labels |
| `Id`, `Class`, `Style` | `string` | | Added to the outer `div`, as are any other attributes |

`PixataHeatmapCell`, passed to `CellTooltip`, has the cell's `Row` and `Column` indexes, its `RowLabel` and `ColumnLabel`, its `Value`, and its `Level`, which is how far up the scale it is, from 0 to 1...

```xml
<PixataHeatmap RowLabels="@_days"
               ColumnLabels="@_hours"
               Values="@_visitors"
               Format="{0:N1}"
               CellTooltip="@(cell => $"{cell.RowLabel} {cell.ColumnLabel}:00, {cell.Value:N1} visitors on average")" />
```

## Styling

The colours and sizes come from these CSS variables, which you can set on `:root` or on any container, such as for a dark theme...

| Variable | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-heatmap-low` | `--pixata-blazor-surface-alt-bg` | The colour for the bottom of the scale |
| `--pixata-blazor-heatmap-high` | `--pixata-blazor-primary` | The colour for the top of the scale |
| `--pixata-blazor-heatmap-empty-bg` | `transparent` | The background of a cell with no value |
| `--pixata-blazor-heatmap-empty-border` | `--pixata-blazor-border-color` | The outline of a cell with no value |
| `--pixata-blazor-heatmap-color` | `--pixata-blazor-text-color` | The text colour |
| `--pixata-blazor-heatmap-strong-color` | `--pixata-blazor-primary-color` | The text colour for cells near the top of the scale |
| `--pixata-blazor-heatmap-label-color` | `--pixata-blazor-subtle-color` | The colour of the row and column labels, and the legend's text |
| `--pixata-blazor-heatmap-font-size` | `--pixata-blazor-font-size-sm` | The size of the text |
| `--pixata-blazor-heatmap-title-font-size` | `1rem` | The size of the title |
| `--pixata-blazor-heatmap-cell-width` | `2.25rem` | The cells' minimum width |
| `--pixata-blazor-heatmap-cell-height` | `1.75rem` | The cells' height |
| `--pixata-blazor-heatmap-gap` | `2px` | The space between the cells |
| `--pixata-blazor-heatmap-cell-radius` | `--pixata-blazor-border-radius-sm` | How rounded the cells' corners are |
| `--pixata-blazor-heatmap-legend-width` | `12rem` | The width of the legend's bar |

The cells are mixed from the low and high colours with `color-mix()`, which every current browser supports.
