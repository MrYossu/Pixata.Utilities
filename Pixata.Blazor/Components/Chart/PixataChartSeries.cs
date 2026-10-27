using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// Holds the chart's series. Put it inside a <see cref="PixataChart"/>, and put <see cref="PixataChartSeries"/> inside it
/// </summary>
public class PixataChartSeriesItems : ComponentBase {
  [CascadingParameter]
  private PixataChart? Chart { get; set; }

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    // The series are given their parameters in the order they are declared, straight after this
    Chart?.BeginDeclaringSeries();
    builder.AddContent(0, ChildContent);
  }
}

/// <summary>
/// A series of data drawn on the chart, as columns, a line, a pie or a donut ring. Put it inside <see cref="PixataChartSeriesItems"/>
/// </summary>
public class PixataChartSeries : PixataChartSettingsBase {
  /// <summary>
  /// How the series is drawn. <see cref="PixataChartSeriesType.Column"/> by default
  /// </summary>
  [Parameter]
  public PixataChartSeriesType? Type { get; set; }

  /// <summary>
  /// The data. Either a list of objects, with <see cref="Field"/> naming the property that holds the value, or a list of
  /// numbers (with no <see cref="Field"/>)
  /// </summary>
  [Parameter]
  public IEnumerable<object>? Data { get; set; }

  /// <summary>
  /// The property of the data items that holds the value. Nested properties are separated with dots
  /// </summary>
  [Parameter]
  public string? Field { get; set; }

  /// <summary>
  /// The property of the data items that holds the category. Without it, the data items are matched to the category
  /// axis's <c>Categories</c> by position
  /// </summary>
  [Parameter]
  public string? CategoryField { get; set; }

  /// <summary>
  /// The name shown in the legend and tooltips
  /// </summary>
  [Parameter]
  public string? Name { get; set; }

  /// <summary>
  /// The colour of the series, as any CSS colour. By default the series take the colours of the palette in turn
  /// </summary>
  [Parameter]
  public string? Color { get; set; }

  /// <summary>
  /// The property of the data items that holds each point's colour
  /// </summary>
  [Parameter]
  public string? ColorField { get; set; }

  /// <summary>
  /// The <c>Name</c> of the value axis the series is plotted against. The first value axis by default
  /// </summary>
  [Parameter]
  public string? Axis { get; set; }

  /// <summary>
  /// The space between the categories of a column chart, as a multiple of a column's width. 1.5 by default. The value of
  /// the chart's first column series is used for all of them
  /// </summary>
  [Parameter]
  public double? Gap { get; set; }

  /// <summary>
  /// The space between the columns in a category, as a multiple of a column's width. 0.4 by default. The value of the
  /// chart's first column series is used for all of them
  /// </summary>
  [Parameter]
  public double? Spacing { get; set; }

  /// <summary>
  /// How a line series joins its points
  /// </summary>
  [Parameter]
  public PixataChartSeriesStyle? Style { get; set; }

  /// <summary>
  /// What a line series does with null values. <see cref="PixataChartSeriesMissingValues.Interpolate"/> by default, or
  /// <see cref="PixataChartSeriesMissingValues.Zero"/> for a stacked series
  /// </summary>
  [Parameter]
  public PixataChartSeriesMissingValues? MissingValues { get; set; }

  /// <summary>
  /// The pattern of a line series' line
  /// </summary>
  [Parameter]
  public PixataChartDashType? DashType { get; set; }

  /// <summary>
  /// The width of a line series' line, in pixels. 2 by default
  /// </summary>
  [Parameter]
  public double? Width { get; set; }

  /// <summary>
  /// The opacity of the series, from 0 to 1
  /// </summary>
  [Parameter]
  public double? Opacity { get; set; }

  /// <summary>
  /// Whether the series is shown. Clicking its legend item toggles this until the parameter next changes
  /// </summary>
  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// Whether the series has an item in the legend
  /// </summary>
  [Parameter]
  public bool? VisibleInLegend { get; set; }

  /// <summary>
  /// The property of a pie's (or donut's) data items that says whether the segment has an item in the legend
  /// </summary>
  [Parameter]
  public string? VisibleInLegendField { get; set; }

  /// <summary>
  /// The property of a pie's (or donut's) data items that says whether the segment is pulled out from the centre
  /// </summary>
  [Parameter]
  public string? ExplodeField { get; set; }

  /// <summary>
  /// The angle, in degrees, at which a pie's (or donut ring's) first segment starts. 0 is three o'clock and angles go anticlockwise, so the
  /// default of 90 starts at twelve o'clock. The segments go clockwise from there
  /// </summary>
  [Parameter]
  public double? StartAngle { get; set; }

  /// <summary>
  /// The radius of a pie, or the outer radius of a donut, in pixels. By default it is as big as will fit. A donut takes this
  /// from its outermost ring
  /// </summary>
  [Parameter]
  public double? Size { get; set; }

  /// <summary>
  /// The size of a donut's hole, as a percentage of its diameter: 0 leaves no hole and 100 is all hole. 50 by default. A
  /// donut with several rings takes this from its innermost ring
  /// </summary>
  [Parameter]
  public double? HoleSize { get; set; }

  /// <summary>
  /// The series' labels, markers, stacking and tooltip settings
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  internal PixataChartSeriesStack? Stack { get; set; }

  internal PixataChartSeriesLabels? Labels { get; set; }

  internal PixataChartSeriesMarkers? Markers { get; set; }

  internal PixataChartSeriesTooltip? SeriesTooltip { get; set; }

  /// <summary>
  /// Set when the user toggles the series with its legend item, and cleared when the <see cref="Visible"/> parameter changes
  /// </summary>
  internal bool? VisibleOverride { get; set; }

  /// <summary>
  /// The pie or donut segments the user has hidden with their legend items
  /// </summary>
  internal HashSet<int> HiddenPoints { get; } = [];

  internal bool IsVisible => VisibleOverride ?? Visible ?? true;

  internal PixataChartSeriesType SeriesType => Type ?? PixataChartSeriesType.Column;

  internal override void Attach() =>
    Chart?.AddSeries(this);

  internal override void Detach() =>
    Chart?.RemoveSeries(this);

  private bool? _lastVisible;

  protected override void OnParametersSet() {
    if (Visible != _lastVisible) {
      VisibleOverride = null;
      _lastVisible = Visible;
    }
    Chart?.SeriesDeclared(this);
    base.OnParametersSet();
  }

  protected override void BuildRenderTree(RenderTreeBuilder builder) =>
    RenderCascading(builder, this, ChildContent);
}

/// <summary>
/// Stacks the series on top of the others. The stack settings of the first series of each type (column or line) apply to
/// the series of that type after it, unless they have their own. Put it inside a <see cref="PixataChartSeries"/>
/// </summary>
public class PixataChartSeriesStack : PixataChartSettingsBase {
  [CascadingParameter]
  private PixataChartSeries? Series { get; set; }

  /// <summary>
  /// Whether the series is stacked. True by default when this component is present
  /// </summary>
  [Parameter]
  public bool? Enabled { get; set; }

  /// <summary>
  /// Whether the values add up (<see cref="PixataChartSeriesStackType.Normal"/>, the default) or are scaled to 100%
  /// </summary>
  [Parameter]
  public PixataChartSeriesStackType? Type { get; set; }

  /// <summary>
  /// Series with the same group are stacked together, and different groups stand side by side
  /// </summary>
  [Parameter]
  public string? Group { get; set; }

  internal override void Attach() {
    if (Series is not null) {
      Series.Stack = this;
    }
  }

  internal override void Detach() {
    if (Series?.Stack == this) {
      Series.Stack = null;
    }
  }
}

/// <summary>
/// Labels showing the value (or anything else) of each data point. Put it inside a <see cref="PixataChartSeries"/>
/// </summary>
public class PixataChartSeriesLabels : PixataChartSettingsBase {
  [CascadingParameter]
  private PixataChartSeries? Series { get; set; }

  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// The format of the value, either a composite format such as <c>{0:C}</c> or a plain one such as <c>N2</c>
  /// </summary>
  [Parameter]
  public string? Format { get; set; }

  /// <summary>
  /// The text of each label, with placeholders such as <c>#= category #</c>, <c>#= value #</c>, <c>#= percentage #</c>,
  /// <c>#= series.name #</c> and <c>#= dataItem.Property #</c>, and formatted ones such as
  /// <c>#= format('{0:P0}', percentage) #</c>. <c>\n</c> starts a new line
  /// </summary>
  [Parameter]
  public string? Template { get; set; }

  /// <summary>
  /// Works out the text of each label in C#. Takes precedence over <see cref="Template"/>
  /// </summary>
  [Parameter]
  public Func<PixataChartSeriesLabelsTemplateContext, string>? TextTemplate { get; set; }

  /// <summary>
  /// Where the labels go. See <see cref="PixataChartSeriesLabelsPosition"/> for the defaults
  /// </summary>
  [Parameter]
  public PixataChartSeriesLabelsPosition? Position { get; set; }

  /// <summary>
  /// The text colour, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Color { get; set; }

  /// <summary>
  /// A background colour for the labels, as any CSS colour
  /// </summary>
  [Parameter]
  public string? Background { get; set; }

  /// <summary>
  /// How far a pie's outside labels are from the pie, in pixels. 35 by default
  /// </summary>
  [Parameter]
  public double? Distance { get; set; }

  /// <summary>
  /// The font, in CSS <c>font</c> shorthand
  /// </summary>
  [Parameter]
  public string? Font { get; set; }

  internal override void Attach() {
    if (Series is not null) {
      Series.Labels = this;
    }
  }

  internal override void Detach() {
    if (Series?.Labels == this) {
      Series.Labels = null;
    }
  }
}

/// <summary>
/// The markers on the points of a line series. Put it inside a <see cref="PixataChartSeries"/>
/// </summary>
public class PixataChartSeriesMarkers : PixataChartSettingsBase {
  [CascadingParameter]
  private PixataChartSeries? Series { get; set; }

  /// <summary>
  /// Whether the markers are shown. True by default
  /// </summary>
  [Parameter]
  public bool? Visible { get; set; }

  [Parameter]
  public PixataChartSeriesMarkersType? Type { get; set; }

  /// <summary>
  /// The size of the markers, in pixels. 7 by default
  /// </summary>
  [Parameter]
  public double? Size { get; set; }

  /// <summary>
  /// The fill colour, as any CSS colour. White by default, with a border in the series colour
  /// </summary>
  [Parameter]
  public string? Background { get; set; }

  internal override void Attach() {
    if (Series is not null) {
      Series.Markers = this;
    }
  }

  internal override void Detach() {
    if (Series?.Markers == this) {
      Series.Markers = null;
    }
  }
}

/// <summary>
/// The tooltip settings for one series, overriding those of the chart's <see cref="PixataChartTooltip"/>. Put it inside a
/// <see cref="PixataChartSeries"/>
/// </summary>
public class PixataChartSeriesTooltip : PixataChartSettingsBase {
  [CascadingParameter]
  private PixataChartSeries? Series { get; set; }

  [Parameter]
  public bool? Visible { get; set; }

  [Parameter]
  public string? Background { get; set; }

  [Parameter]
  public string? Color { get; set; }

  [Parameter]
  public string? Font { get; set; }

  /// <summary>
  /// The format of the value, either a composite format such as <c>{0:C}</c> or a plain one such as <c>N2</c>
  /// </summary>
  [Parameter]
  public string? Format { get; set; }

  [Parameter]
  public RenderFragment<PixataChartSeriesTooltipTemplateContext>? Template { get; set; }

  internal override void Attach() {
    if (Series is not null) {
      Series.SeriesTooltip = this;
    }
  }

  internal override void Detach() {
    if (Series?.SeriesTooltip == this) {
      Series.SeriesTooltip = null;
    }
  }
}
