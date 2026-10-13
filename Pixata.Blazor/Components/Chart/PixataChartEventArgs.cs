using System.Collections.Generic;

namespace Pixata.Blazor.Components;

/// <summary>
/// A data point of a chart series: a column, a point on a line or a pie segment
/// </summary>
public class PixataChartSeriesPoint {
  /// <summary>
  /// The point's category (the category axis value for a column or line point, or the segment's category for a pie)
  /// </summary>
  public object? Category { get; init; }

  /// <summary>
  /// The index of the point's category, or of the segment in a pie series
  /// </summary>
  public int? CategoryIndex { get; init; }

  /// <summary>
  /// The index of the series in the chart, counting every series, including hidden ones
  /// </summary>
  public int SeriesIndex { get; init; }

  public string SeriesName { get; init; } = "";

  /// <summary>
  /// The colour the point is drawn in. For a series that uses the default palette, this is the palette's default colour
  /// </summary>
  public string SeriesColor { get; init; } = "";

  /// <summary>
  /// The data item the point was read from, or the value itself when the series data is a list of numbers
  /// </summary>
  public object? DataItem { get; init; }

  /// <summary>
  /// The point's value
  /// </summary>
  public double? Value { get; init; }

  /// <summary>
  /// The point's share of its whole, from 0 to 1. Set for pie segments and 100% stacked series
  /// </summary>
  public double Percentage { get; init; }

  /// <summary>
  /// The sum of the values that <see cref="Percentage"/> is a share of
  /// </summary>
  public double Total { get; init; }
}

/// <summary>
/// The details passed to <see cref="PixataChart.OnSeriesClick"/>
/// </summary>
public class PixataChartSeriesClickEventArgs : PixataChartSeriesPoint {
}

/// <summary>
/// The details passed to <see cref="PixataChart.OnLegendItemClick"/>
/// </summary>
public class PixataChartLegendItemClickEventArgs {
  /// <summary>
  /// The index of the series the legend item belongs to
  /// </summary>
  public int SeriesIndex { get; init; }

  /// <summary>
  /// For a pie series, the index of the segment the legend item belongs to
  /// </summary>
  public int? PointIndex { get; init; }

  /// <summary>
  /// The text of the legend item
  /// </summary>
  public string Text { get; init; } = "";

  /// <summary>
  /// Whether the series (or pie segment) is visible now that the click has toggled it
  /// </summary>
  public bool Visible { get; init; }
}

/// <summary>
/// A point shown in a tooltip
/// </summary>
public class PixataChartSeriesTooltipTemplatePoint : PixataChartSeriesPoint {
  /// <summary>
  /// The value, formatted with the tooltip's <c>Format</c>
  /// </summary>
  public string FormattedValue { get; internal set; } = "";
}

/// <summary>
/// The context of a tooltip <c>Template</c>
/// </summary>
public class PixataChartSeriesTooltipTemplateContext : PixataChartSeriesTooltipTemplatePoint {
}

/// <summary>
/// The context of a shared tooltip's <c>SharedTemplate</c>: a category and the points of every visible series in it
/// </summary>
public class PixataChartSeriesTooltipSharedTemplateContext {
  public object? Category { get; init; }

  public List<PixataChartSeriesTooltipTemplatePoint> Points { get; init; } = [];
}

/// <summary>
/// The context of a series label's <c>TextTemplate</c>
/// </summary>
public class PixataChartSeriesLabelsTemplateContext : PixataChartSeriesPoint {
}
