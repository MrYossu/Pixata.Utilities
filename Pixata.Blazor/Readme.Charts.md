# Charts

>Part of [Pixata.Blazor](Readme.md)

`PixataChart` draws column, line and pie charts. Column and line series can be mixed on one chart, stacked, and plotted against more than one value axis. The chart is plain SVG, drawn by the component, with the title, legend and tooltip as ordinary HTML, so it can be styled with CSS and needs no charting library.

You describe the chart with child components, one for each part...

```xml
<PixataChart Height="350px">
  <PixataChartTitle Text="Sales and margin" />
  <PixataChartLegend Position="PixataChartLegendPosition.Bottom" />
  <PixataChartTooltip Visible="true" />

  <PixataChartSeriesItems>
    <PixataChartSeries Type="PixataChartSeriesType.Column" Name="Hardware" Data="@_sales" Field="@nameof(MonthlySales.Hardware)" CategoryField="@nameof(MonthlySales.Month)" />
    <PixataChartSeries Type="PixataChartSeriesType.Column" Name="Software" Data="@_sales" Field="@nameof(MonthlySales.Software)" CategoryField="@nameof(MonthlySales.Month)" />
    <PixataChartSeries Type="PixataChartSeriesType.Line" Name="Margin" Data="@_sales" Field="@nameof(MonthlySales.Margin)" CategoryField="@nameof(MonthlySales.Month)" Axis="margin" />
  </PixataChartSeriesItems>

  <PixataChartCategoryAxes>
    <PixataChartCategoryAxis AxisCrossingValue="@(new object[] { 0, 100 })">
      <PixataChartCategoryAxisTitle Text="Month" />
    </PixataChartCategoryAxis>
  </PixataChartCategoryAxes>

  <PixataChartValueAxes>
    <PixataChartValueAxis>
      <PixataChartValueAxisTitle Text="Sales" />
      <PixataChartValueAxisLabels Format="{0:C0}" />
    </PixataChartValueAxis>
    <PixataChartValueAxis Name="margin" Min="0" Max="0.4" Color="var(--pixata-blazor-chart-series-3)">
      <PixataChartValueAxisLabels Format="{0:P0}" />
    </PixataChartValueAxis>
  </PixataChartValueAxes>
</PixataChart>
```

There are live examples of everything on this page on the [sample site](https://test.pixata.co.uk/ChartSample), and the source is in [ChartSample.razor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/ChartSample.razor).

## Setting up

There is nothing to register. The chart's styles are in the package's stylesheet (`_content/Pixata.Blazor/pixata.css`), which you will already have if you use any of the other components, and its small script (which measures the chart and its text) is loaded by the component when it first renders.

The chart works in Server, WebAssembly and Auto render modes. When it is prerendered it is drawn at a guessed width, and redrawn at the right size as soon as it becomes interactive.

## How the components fit together

Everything goes inside a `PixataChart`. The series and axes each have a container component around them, which does nothing but group them...

| Container | What goes inside it |
| --- | --- |
| `PixataChartSeriesItems` | One or more `PixataChartSeries` |
| `PixataChartCategoryAxes` | One `PixataChartCategoryAxis` (any more are ignored) |
| `PixataChartValueAxes` | One or more `PixataChartValueAxis` |

The other settings go straight inside the chart (`PixataChartTitle`, `PixataChartLegend` and `PixataChartTooltip`), inside a series (`PixataChartSeriesStack`, `PixataChartSeriesLabels`, `PixataChartSeriesMarkers` and `PixataChartSeriesTooltip`), or inside an axis (its title, labels and grid lines).

None of these components render anything themselves. They pass their settings up to the chart, which redraws whenever one of them changes. Arrays (such as `Categories` and `AxisCrossingValue`) are compared by their contents, so it is fine to create them inline in the markup.

Every parameter is optional. A setting you leave out gets the default described below.

## PixataChart

| Parameter | Type | Description |
| --- | --- | --- |
| `Width` | `string` | The width, as a CSS length. By default the chart fills the width of its container, and redraws when the container changes size |
| `Height` | `string` | The height, as a CSS length. 400px by default (from the `--pixata-blazor-chart-height` variable) |
| `Class` | `string` | Extra CSS classes for the chart's outer `div` |
| `Id` | `string` | The `id` of the outer `div` |
| `Transitions` | `bool` | Whether the columns grow and the lines and segments fade in when the chart first appears. `true` by default, and always off for users who have asked for reduced motion |
| `OnSeriesClick` | `EventCallback<PixataChartSeriesClickEventArgs>` | Raised when the user clicks or taps a column, line point or pie segment, or presses Enter or Space on one |
| `OnLegendItemClick` | `EventCallback<PixataChartLegendItemClickEventArgs>` | Raised when the user clicks a legend item, after its series (or pie segment) has been shown or hidden |

Any other attributes are added to the outer `div`.

The chart redraws when its parameters change. If you change the data in place (adding an item to the list you passed as `Data`, say), the chart can't know, so call its `Refresh()` method...

```xml
<PixataChart @ref="_chart">
  ...
</PixataChart>
```

```csharp
private PixataChart _chart = null!;

private void AddMonth() {
  _sales.Add(new() { Month = "Sep", Hardware = 51000, Software = 44000 });
  _chart.Refresh();
}
```

Passing a new list (`_sales = [.. _sales, newMonth]`) redraws the chart without needing `Refresh()`.

## Series

`PixataChartSeries` has the following parameters...

| Parameter | Type | Description |
| --- | --- | --- |
| `Type` | `PixataChartSeriesType` | `Column` (the default), `Line` or `Pie` |
| `Data` | `IEnumerable<object>` | The data. Either a list of objects, with `Field` naming the property that holds the value, or a list of numbers |
| `Field` | `string` | The property holding each item's value. Nested properties are separated with dots (`Totals.Net`) |
| `CategoryField` | `string` | The property holding each item's category. Without it, items are matched to the category axis's `Categories` by position |
| `Name` | `string` | The name shown in the legend and tooltips. A series without a name isn't shown in the legend |
| `Color` | `string` | The series colour, as any CSS colour. By default the series take the palette colours in turn |
| `ColorField` | `string` | The property holding each item's colour, which overrides `Color`. As these colours can come from your data, anything that isn't a plain colour value (text containing `;`, `:`, braces, quotes or `url(`) is ignored, and the point gets the colour it would have had without it |
| `Axis` | `string` | The `Name` of the value axis to plot the series against. The first value axis by default |
| `Gap` | `double` | The space between categories, as a multiple of a column's width. 1.5 by default |
| `Spacing` | `double` | The space between the columns in a category, as a multiple of a column's width. 0.4 by default |
| `Style` | `PixataChartSeriesStyle` | How a line joins its points: `Normal` (straight lines), `Smooth` (a curve that doesn't overshoot the points) or `Step` |
| `MissingValues` | `PixataChartSeriesMissingValues` | What a line does with a null value: `Gap`, `Interpolate` (the default) or `Zero` (the default for stacked series) |
| `DashType` | `PixataChartDashType` | The pattern of a line: `Solid`, `Dot`, `Dash`, `LongDash`, `DashDot`, `LongDashDot` or `LongDashDotDot` |
| `Width` | `double` | The width of a line, in pixels. 2 by default |
| `Opacity` | `double` | The opacity of the series, from 0 to 1 |
| `Visible` | `bool` | Whether the series is shown. A hidden series keeps its legend item, so the user can show it again |
| `VisibleInLegend` | `bool` | Whether the series has a legend item |
| `VisibleInLegendField` | `string` | For a pie, the property saying whether each segment has a legend item |
| `ExplodeField` | `string` | For a pie, the property saying whether each segment is pulled out from the centre |
| `StartAngle` | `double` | For a pie, the angle at which the first segment starts, in degrees. 0 is three o'clock and angles go anticlockwise, so the default of 90 starts at twelve o'clock. The segments go clockwise from there |
| `Size` | `double` | For a pie, the radius in pixels. By default the pie is as big as will fit |

Null values are left out: a column chart has no column for them, a pie has no segment, and a line follows its `MissingValues` setting.

`Gap` and `Spacing` apply to all the column series on the chart, and are taken from the first column series.

### Mixing series

Column and line series share the category axis, so you can put any number of each on one chart, and their points line up. The columns are drawn first, so lines and their markers are always on top.

A chart with a pie series shows only that pie. Each data item is a segment, and has its own legend item.

### Stacking

Put a `PixataChartSeriesStack` inside a series to stack it on the series before it...

```xml
<PixataChartSeries Name="North" Data="@_north">
  <PixataChartSeriesStack Type="PixataChartSeriesStackType.Stack100" />
</PixataChartSeries>
<PixataChartSeries Name="South" Data="@_south" />
<PixataChartSeries Name="East" Data="@_east" />
```

The stack settings of the first column series apply to all the column series after it, unless they have their own `PixataChartSeriesStack`, and the same goes for line series. So in the example above, all three series are stacked.

| Parameter | Type | Description |
| --- | --- | --- |
| `Enabled` | `bool` | Whether the series is stacked. `true` by default, so `Enabled="false"` lets one series opt out |
| `Type` | `PixataChartSeriesStackType` | `Normal` (the default) adds the values up. `Stack100` scales each category to 100%, and the value axis then runs from 0% to 100% |
| `Group` | `string` | Series with the same group are stacked together, and different groups stand side by side in each category |

Positive values stack upwards from zero and negative ones downwards. Line series stack in the same way, and treat missing values as zero unless they say otherwise.

### Labels

`PixataChartSeriesLabels` shows a label on each point...

| Parameter | Type | Description |
| --- | --- | --- |
| `Visible` | `bool` | Whether the labels are shown |
| `Format` | `string` | The format of the value, either a composite format such as `{0:C}` or a plain one such as `N2` |
| `Template` | `string` | The text of each label, with placeholders (see below) |
| `TextTemplate` | `Func<PixataChartSeriesLabelsTemplateContext, string>` | Works out each label's text in C#. Takes precedence over `Template` |
| `Position` | `PixataChartSeriesLabelsPosition` | Where the labels go (see below) |
| `Color` | `string` | The text colour |
| `Background` | `string` | A background colour behind each label |
| `Distance` | `double` | How far a pie's outside labels are from the pie, in pixels. 35 by default, and less on a narrow chart |
| `Font` | `string` | The font, in CSS `font` shorthand (`bold 13px sans-serif`) |

The placeholders in `Template` are `#= value #`, `#= category #`, `#= percentage #` (a fraction, such as 0.25), `#= series.name #`, `#= dataItem #` and `#= dataItem.Property #`. Any of them can be formatted with `#= format('{0:P0}', percentage) #`. `\n` starts a new line...

```xml
<PixataChartSeriesLabels Visible="true" Template="#= category #: #= format('{0:P0}', percentage) #" />
```

`TextTemplate` gets a `PixataChartSeriesLabelsTemplateContext`, which has the same properties as the click event's arguments (see below)...

```xml
<PixataChartSeriesLabels Visible="true" TextTemplate="@(c => $"{c.Value:N0} ({c.Percentage:P0})")" />
```

The positions are...

| Series | Positions |
| --- | --- |
| Column | `OutsideEnd` (the default, just past the end of the column), `InsideEnd`, `Center` and `InsideBase` |
| Line | `Above` (the default), `Below`, `Left`, `Right` and `Center` |
| Pie | `OutsideEnd` (the default, outside the pie with a line to the segment), `InsideEnd` and `Center` |

Labels outside a pie are moved up or down so they don't overlap, and shortened (with an ellipsis) when there isn't room for them. Labels on the points have a light halo so they can be read over grid lines and other series.

### Markers

`PixataChartSeriesMarkers` sets the markers on a line's points...

| Parameter | Type | Description |
| --- | --- | --- |
| `Visible` | `bool` | Whether the markers are shown. `true` by default |
| `Type` | `PixataChartSeriesMarkersType` | `Circle` (the default), `Square`, `Triangle`, `Cross`, `Rect` or `RoundedRect` |
| `Size` | `double` | The size in pixels. 7 by default |
| `Background` | `string` | The fill colour. By default the markers are hollow, with a border in the series colour |

Hidden markers can still be hovered, tapped and focused.

## Axes

Column and line charts have one category axis along the bottom, and one or more value axes up the sides. Without any axis components, you get a category axis showing the categories in the data, and one value axis on the left.

### Category axis

| Parameter | Type | Description |
| --- | --- | --- |
| `Categories` | `object[]` | The categories, for series without a `CategoryField`. Series with a `CategoryField` add any categories of their own after these |
| `Justified` | `bool` | Puts the first and last line points on the edges of the chart, rather than in the middle of their categories. Ignored when there are column series |
| `AxisCrossingValue` | `object[]` | Where each value axis crosses the category axis, in the order of the value axes. A category index at or past the number of categories puts that value axis on the right, so `new object[] { 0, 100 }` puts the first on the left and the second on the right |
| `Color` | `string` | The colour of the axis line and labels |
| `Visible` | `bool` | Whether the axis is shown |

### Value axis

| Parameter | Type | Description |
| --- | --- | --- |
| `Name` | `string` | The name series use in their `Axis` parameter |
| `Min` | `double` | The lowest value. By default it is worked out from the data, and includes zero |
| `Max` | `double` | The highest value. By default it is worked out from the data, with a little room above the highest value |
| `MajorUnit` | `double` | The interval between the labels and major grid lines. By default it is a round number that gives five to ten divisions, and a larger one when the chart is too short for those to fit |
| `MinorUnit` | `double` | The interval between the minor grid lines. A fifth of `MajorUnit` by default |
| `NarrowRange` | `bool` | Lets the automatic range start above zero when all the values are well above it |
| `Color` | `string` | The colour of the axis line and labels. Useful for showing which series go with a second axis |
| `Visible` | `bool` | Whether the axis is shown |

Only the first value axis has grid lines by default, so a second axis doesn't add a confusing second set.

### Axis titles, labels and grid lines

Each axis can contain a title (`PixataChartCategoryAxisTitle` or `PixataChartValueAxisTitle`), with `Text`, `Color`, `Font` and `Visible` parameters.

The labels (`PixataChartCategoryAxisLabels` or `PixataChartValueAxisLabels`) have these parameters...

| Parameter | Type | Description |
| --- | --- | --- |
| `Format` | `string` | The format, either composite (`{0:C0}`) or plain (`N0`) |
| `Template` | `string` | The text of each label, with the placeholder `#= value #`, and for categories from the data, `#= dataItem.Property #` |
| `Step` | `double` | Shows every nth label |
| `Skip` | `double` | The number of labels to leave out at the start |
| `Visible`, `Color`, `Font` | | As for the other components |

The labels can contain a `PixataChartCategoryAxisLabelsRotation` (or `PixataChartValueAxisLabelsRotation`) whose `Angle` is a number of degrees (negative is anticlockwise) or `"auto"`.

Category labels look after themselves by default. When they don't fit side by side they are turned to -45 degrees, and if they still don't fit, only every second (or third, and so on) label is shown. Very long turned labels are shortened with an ellipsis, and the full text is shown when you hover over them.

The grid lines are `PixataChartCategoryAxisMajorGridLines` (the vertical lines between categories, shown by default), `PixataChartCategoryAxisMinorGridLines` (through the middle of each category, hidden by default), `PixataChartValueAxisMajorGridLines` (shown by default) and `PixataChartValueAxisMinorGridLines` (hidden by default). They all have `Visible`, `Color`, `Width`, `DashType`, `Step` and `Skip` parameters.

## Title and legend

`PixataChartTitle` has `Text`, `Position` (`Top` or `Bottom`), `Align` (`Left`, `Center` or `Right`), `Color`, `Background`, `Font` and `Visible` parameters. Its `Description` is not shown, but is read out by screen readers as the chart's description.

`PixataChartLegend` has these parameters...

| Parameter | Type | Description |
| --- | --- | --- |
| `Position` | `PixataChartLegendPosition` | `Right` (the default), `Left`, `Top` or `Bottom` |
| `Align` | `PixataChartLegendAlign` | How the items line up along the legend: `Center` (the default), `Start` or `End` |
| `Orientation` | `PixataChartLegendOrientation` | Whether the items go across or down. By default they go down beside the chart and across above or below it |
| `Background` | `string` | The background colour |
| `Reverse` | `bool` | Lists the items in reverse order, which is handy for stacked series, as the legend then matches the stack |
| `Visible` | `bool` | Whether the legend is shown |

Clicking a legend item hides its series (or, for a pie, its segment), and clicking it again shows it. The value axes are worked out again from the series that are left. Legend items are buttons, so they can be reached with the Tab key and toggled with Enter or Space, and they report whether they are pressed to screen readers.

## Tooltips

Tooltips are off unless you turn them on, with a `PixataChartTooltip` inside the chart...

| Parameter | Type | Description |
| --- | --- | --- |
| `Visible` | `bool` | Whether tooltips are shown |
| `Shared` | `bool` | Shows one tooltip for the whole category, listing every series, rather than one for the point under the pointer. Ignored by pies |
| `Format` | `string` | The format of the value |
| `Template` | `RenderFragment<PixataChartSeriesTooltipTemplateContext>` | The content of the tooltip |
| `SharedTemplate` | `RenderFragment<PixataChartSeriesTooltipSharedTemplateContext>` | The content of a shared tooltip |
| `Background`, `Color`, `Font` | `string` | The look of the tooltip. By default it takes the colour of the point, with dark or light text to suit |

A series can override these with its own `PixataChartSeriesTooltip` (which has the same parameters, other than `Shared` and `SharedTemplate`), so you can, for example, turn tooltips on for just one series.

The template's context has the point's details (see the click event below), and `FormattedValue`, the value with the tooltip's `Format` applied...

```xml
<PixataChartTooltip Visible="true" Format="{0:C0}">
  <Template>
    <strong>@context.SeriesName</strong><br />
    @context.Category: @context.FormattedValue
  </Template>
</PixataChartTooltip>
```

The shared template's context has the `Category`, and `Points`, a list with one entry for each series that has a value in that category.

## Events

`OnSeriesClick` gets a `PixataChartSeriesClickEventArgs` with these properties...

| Property | Description |
| --- | --- |
| `Category` | The category (or for a pie, the segment's category) |
| `CategoryIndex` | The position of the category |
| `SeriesIndex` | The position of the series in the chart |
| `SeriesName` | The series' `Name` |
| `SeriesColor` | The point's colour. For palette colours this is the default hex value, even if you have changed the CSS variable |
| `DataItem` | The item from `Data`, or the number for a series of numbers |
| `Value` | The value |
| `Percentage` | For a pie, the segment's share of the whole. For a stacked series, the point's share of its category |
| `Total` | The total that `Percentage` is a share of |

`OnLegendItemClick` gets a `PixataChartLegendItemClickEventArgs`, with the `SeriesIndex`, the `PointIndex` (for a pie segment), the item's `Text`, and `Visible`, whether the series or segment is now shown.

## Phones, touch and the keyboard

The chart redraws whenever its container changes size, and adapts to the space it has...

- When the chart is narrower than 400 pixels, a legend on the left or right moves below the chart, and its items wrap across it.
- Category labels turn and thin out (see above), and the value axis uses fewer divisions when the chart is short.
- Pie labels move closer to the pie, and are shortened if need be, leaving as much room for the pie as possible.

On a touch screen, tapping a column, point or segment shows its tooltip (and raises `OnSeriesClick`), and tapping elsewhere on the chart hides it. Nothing depends on hovering, and the touch targets for line points and legend items are made larger on touch screens.

The chart can be reached with the Tab key. Once it has the focus...

| Key | What it does |
| --- | --- |
| Left and Right arrows | Move to the previous or next point in the series (or segment of a pie) |
| Up and Down arrows | Move to the same category in the previous or next series |
| Home and End | Move to the first or last point in the series |
| Enter or Space | Raises `OnSeriesClick` for the point |
| Escape | Hides the tooltip |

The focused point is outlined, its tooltip is shown (if tooltips are on), and its details are announced to screen readers. The chart is marked up as a graphics document, with each point described by its series, category and value.

## Styling

The chart uses the stylesheet's general variables (font, text colour and so on), and has its own, which you can override in your own CSS...

| Variable | Default | Used for |
| --- | --- | --- |
| `--pixata-blazor-chart-series-1` to `-6` | `#ff6358`, `#ffd246`, `#78d237`, `#28b4c8`, `#2d73f5`, `#aa46be` | The series palette. The seventh series uses the first colour again |
| `--pixata-blazor-chart-height` | `400px` | The height of a chart without a `Height` |
| `--pixata-blazor-chart-bg` | `transparent` | The chart background |
| `--pixata-blazor-chart-text-color` | The general text colour | Titles and labels |
| `--pixata-blazor-chart-font-size` | The general font size | The chart as a whole, which the legend uses |
| `--pixata-blazor-chart-title-font-size` | `1rem` | The chart title |
| `--pixata-blazor-chart-axis-title-font-size` | `.875rem` | Axis titles |
| `--pixata-blazor-chart-label-font-size` | `.75rem` | Axis and point labels |
| `--pixata-blazor-chart-major-lines`, `--pixata-blazor-chart-minor-lines` | Faint greys | Grid lines |
| `--pixata-blazor-chart-axis-color` | A light grey | Axis lines and ticks |
| `--pixata-blazor-chart-inactive` | A mid grey | Hidden series in the legend |
| `--pixata-blazor-chart-column-border` | `20%` | How much darker a column's border is than its fill |
| `--pixata-blazor-chart-marker-bg` | The surface colour | The fill of hollow markers |
| `--pixata-blazor-chart-hover-opacity` | `.75` | The opacity of a hovered column or segment |
| `--pixata-blazor-chart-label-halo` | Translucent white | The halo around point labels |
| `--pixata-blazor-chart-connector-color` | A mid grey | The lines from pie labels to their segments |
| `--pixata-blazor-chart-focus-color` | The text colour | The keyboard focus outline |
| `--pixata-blazor-chart-legend-marker-size` | `.875rem` | Legend markers |
| `--pixata-blazor-chart-tooltip-color`, `--pixata-blazor-chart-tooltip-dark-color` | White, the text colour | Tooltip text on dark and light point colours |
| `--pixata-blazor-chart-tooltip-font-size` | `.8125rem` | Tooltips |
| `--pixata-blazor-chart-shared-tooltip-bg` | The base background | Shared tooltips |
| `--pixata-blazor-chart-tooltip-z-index` | `3` | Tooltips |
| `--pixata-blazor-chart-transition` | `.5s ease-out` | The opening animation |

So to use your brand colours for every chart...

```css
:root {
  --pixata-blazor-chart-series-1: #0b5394;
  --pixata-blazor-chart-series-2: #e69138;
}
```

The parts of the chart have classes starting `pixata-blazor-chart-` (`pixata-blazor-chart-title`, `pixata-blazor-chart-legend`, `pixata-blazor-chart-column`, `pixata-blazor-chart-line`, `pixata-blazor-chart-segment`, `pixata-blazor-chart-tooltip` and so on), and each point's colour is passed to it in the `--pixata-blazor-chart-point-color` property, so you can restyle anything the variables don't cover.

## Limitations

- Only one category axis is used, and it is always along the bottom of the chart.
- A chart shows only one pie. Any other series on a pie chart are ignored.
- Label templates support the placeholders listed above, but not general expressions. Use `TextTemplate` for anything else.
