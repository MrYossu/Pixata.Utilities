namespace Pixata.Blazor.Components;

/// <summary>
/// How a <see cref="PixataChartSeries"/> draws its data
/// </summary>
public enum PixataChartSeriesType {
  /// <summary>
  /// Vertical bars, one per category. This is the default
  /// </summary>
  Column,

  /// <summary>
  /// Points joined by a line, one per category. Can be mixed with column series on the same chart
  /// </summary>
  Line,

  /// <summary>
  /// A circle divided into segments, one per data item. A chart with a pie series shows only that series
  /// </summary>
  Pie,

  /// <summary>
  /// A pie with a hole in the middle. Each donut series is a ring, the first declared on the inside, and a chart with donut
  /// series shows only those
  /// </summary>
  Donut
}

/// <summary>
/// How stacked series add up
/// </summary>
public enum PixataChartSeriesStackType {
  /// <summary>
  /// Each value sits on top of the one before it
  /// </summary>
  Normal,

  /// <summary>
  /// The values in each category are scaled to add up to 100%
  /// </summary>
  Stack100
}

/// <summary>
/// How a line series joins its points
/// </summary>
public enum PixataChartSeriesStyle {
  /// <summary>
  /// Straight lines from point to point
  /// </summary>
  Normal,

  /// <summary>
  /// A smooth curve through the points
  /// </summary>
  Smooth,

  /// <summary>
  /// Horizontal lines across each category, joined by vertical steps
  /// </summary>
  Step
}

/// <summary>
/// What a line series does with a data item whose value is null
/// </summary>
public enum PixataChartSeriesMissingValues {
  /// <summary>
  /// Leaves a gap in the line
  /// </summary>
  Gap,

  /// <summary>
  /// Joins the points either side of the missing one. This is the default
  /// </summary>
  Interpolate,

  /// <summary>
  /// Treats the missing value as zero. This is the default for stacked series
  /// </summary>
  Zero
}

/// <summary>
/// The shape of the markers on a line series
/// </summary>
public enum PixataChartSeriesMarkersType {
  Circle,
  Cross,
  Square,
  Triangle,
  Rect,
  RoundedRect
}

/// <summary>
/// Where a data label goes, relative to its column, point or pie segment. Columns use <see cref="OutsideEnd"/> (the default),
/// <see cref="InsideEnd"/>, <see cref="Center"/> and <see cref="InsideBase"/>; line points use <see cref="Above"/> (the
/// default), <see cref="Below"/>, <see cref="Left"/>, <see cref="Right"/> and <see cref="Center"/>; pie segments use
/// <see cref="OutsideEnd"/> (the default), <see cref="InsideEnd"/> and <see cref="Center"/>
/// </summary>
public enum PixataChartSeriesLabelsPosition {
  Above,
  Below,
  Bottom,
  Center,
  InsideBase,
  InsideEnd,
  Left,
  OutsideEnd,
  Right,
  Top
}

/// <summary>
/// Where the legend goes
/// </summary>
public enum PixataChartLegendPosition {
  Top,
  Bottom,
  Left,

  /// <summary>
  /// The default. On a narrow chart (a phone, say) a legend on the left or right moves below the chart
  /// </summary>
  Right
}

/// <summary>
/// How the legend items line up along the legend
/// </summary>
public enum PixataChartLegendAlign {
  Center,
  End,
  Start
}

/// <summary>
/// Whether the legend items go across or down
/// </summary>
public enum PixataChartLegendOrientation {
  Horizontal,
  Vertical
}

/// <summary>
/// Whether the chart title goes above or below the chart
/// </summary>
public enum PixataChartTitlePosition {
  Top,
  Bottom
}

/// <summary>
/// How the chart title lines up
/// </summary>
public enum PixataChartTitleAlign {
  Left,
  Center,
  Right
}

/// <summary>
/// The pattern of a line or grid line
/// </summary>
public enum PixataChartDashType {
  Solid,
  Dot,
  Dash,
  LongDash,
  DashDot,
  LongDashDot,
  LongDashDotDot
}
