using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pixata.Blazor.Components;

/// <summary>
/// Everything the chart draws, worked out by <see cref="PixataChartLayout"/> in plain numbers, so <c>PixataChart.razor</c>
/// only has to turn it into SVG
/// </summary>
internal sealed class PixataChartScene {
  public double Width { get; init; }
  public double Height { get; init; }
  public bool IsPie { get; set; }

  /// <summary>
  /// Whether the pie is a donut (in which case <see cref="IsPie"/> is true as well)
  /// </summary>
  public bool IsDonut { get; set; }

  /// <summary>
  /// The centre and radius of a donut's hole
  /// </summary>
  public (double X, double Y, double Radius) Hole { get; set; }

  /// <summary>
  /// Whether any series has any data, as without it the chart shows its no-data overlay
  /// </summary>
  public bool HasData { get; set; }
  public PixataChartBox Plot { get; set; }
  public List<object?> Categories { get; } = [];
  public PixataChartCategoryScale? CategoryScale { get; set; }
  public List<PixataChartSceneLine> GridLines { get; } = [];
  public List<PixataChartSceneLine> AxisLines { get; } = [];
  public List<PixataChartSceneText> AxisTexts { get; } = [];
  public List<PixataChartScenePath> Paths { get; } = [];
  public List<PixataChartScenePoint> Points { get; } = [];
  public List<PixataChartSceneText> Labels { get; } = [];
  public List<PixataChartScenePath> Connectors { get; } = [];
  public List<PixataChartLegendEntry> LegendEntries { get; } = [];

  /// <summary>
  /// The points of each category, for the shared tooltip
  /// </summary>
  public IEnumerable<PixataChartScenePoint> PointsInCategory(int category) {
    foreach (PixataChartScenePoint point in Points) {
      if (point.PointIndex == category) {
        yield return point;
      }
    }
  }
}

internal readonly record struct PixataChartBox(double X, double Y, double Width, double Height) {
  public double Right => X + Width;
  public double Bottom => Y + Height;
}

/// <summary>
/// A grid line, axis line or tick
/// </summary>
internal sealed record PixataChartSceneLine(double X1, double Y1, double X2, double Y2, string Class, string? Colour = null, double? Width = null, string? DashArray = null);

/// <summary>
/// A piece of text. <see cref="Y"/> is the baseline of the first line, and <see cref="Anchor"/> is the SVG text-anchor
/// </summary>
internal sealed record PixataChartSceneText(string[] Lines, double X, double Y, string Anchor, string Class, double LineHeight, double Rotation = 0, string? Colour = null, string? Font = null, string? Background = null, double Width = 0, double FontSize = 12, string? Title = null);

/// <summary>
/// A line series' line, or a pie or donut label's connector
/// </summary>
internal sealed record PixataChartScenePath(string D, string Class, int SeriesIndex, string? Colour = null, double? Width = null, string? DashArray = null, double? Opacity = null);

internal enum PixataChartPointKind {
  Column,
  Marker,
  Segment
}

/// <summary>
/// A data point the user can hover over, tap, click and move the keyboard focus to: a column, a line point (drawn as a
/// marker) or a pie or donut segment
/// </summary>
internal sealed class PixataChartScenePoint {
  public required PixataChartSeries Series { get; init; }
  public int SeriesIndex { get; init; }

  /// <summary>
  /// The category index, or for a pie the index of the data item
  /// </summary>
  public int PointIndex { get; init; }

  public PixataChartPointKind Kind { get; init; }
  public object? Category { get; init; }
  public double? Value { get; init; }
  public object? DataItem { get; init; }
  public double Percentage { get; init; }
  public double Total { get; init; }

  /// <summary>
  /// The CSS colour the point is drawn in
  /// </summary>
  public string Colour { get; init; } = "";

  /// <summary>
  /// The colour reported to event handlers and templates (the palette's default colour when the CSS one is a variable)
  /// </summary>
  public string ReportedColour { get; init; } = "";

  // Columns: the rectangle. Markers: the centre in X and Y, and the size in Width
  public double X { get; init; }
  public double Y { get; init; }
  public double Width { get; init; }
  public double Height { get; init; }

  /// <summary>
  /// The outline of a pie or donut segment, or of a marker that isn't a circle or square
  /// </summary>
  public string? Path { get; init; }

  public PixataChartSeriesMarkersType MarkerType { get; init; }
  public bool MarkerVisible { get; init; }
  public string? MarkerBackground { get; init; }
  public double? Opacity { get; init; }

  /// <summary>
  /// Where the tooltip points to
  /// </summary>
  public double AnchorX { get; init; }

  public double AnchorY { get; init; }

  /// <summary>
  /// What a screen reader says for the point
  /// </summary>
  public string Description { get; init; } = "";

  /// <summary>
  /// The point as event handlers and templates see it
  /// </summary>
  public T ToSeriesPoint<T>(string formattedValue = "") where T : PixataChartSeriesPoint, new() {
    T point = new() {
      Category = Category,
      CategoryIndex = PointIndex,
      SeriesIndex = SeriesIndex,
      SeriesName = Series.Name ?? "",
      SeriesColor = ReportedColour,
      DataItem = DataItem,
      Value = Value,
      Percentage = Percentage,
      Total = Total
    };
    if (point is PixataChartSeriesTooltipTemplatePoint tooltipPoint) {
      tooltipPoint.FormattedValue = formattedValue;
    }
    return point;
  }
}

/// <summary>
/// An item in the legend. <see cref="Linked"/> holds the segments (series and point indexes) of a donut's other rings that
/// have the same category, which the item shows and hides along with its own
/// </summary>
internal sealed record PixataChartLegendEntry(int SeriesIndex, int? PointIndex, string Text, string Colour, PixataChartSeriesType Type, bool Visible, string? DashArray, IReadOnlyList<(int Series, int Point)>? Linked = null);

/// <summary>
/// The default series colours, and a little colour arithmetic for tooltip text
/// </summary>
internal static class PixataChartPalette {
  public static readonly string[] Defaults = ["#ff6358", "#ffd246", "#78d237", "#28b4c8", "#2d73f5", "#aa46be"];

  private static readonly char[] UnsafeCharacters = [';', ':', '{', '}', '<', '>', '"', '\'', '\\', '@'];

  /// <summary>
  /// The CSS colour of the nth series (or pie segment), which hosts can change with the --pixata-blazor-chart-series-*
  /// custom properties
  /// </summary>
  public static string Css(int index) =>
    $"var(--pixata-blazor-chart-series-{index % Defaults.Length + 1}, {Defaults[index % Defaults.Length]})";

  public static string Hex(int index) =>
    Defaults[index % Defaults.Length];

  /// <summary>
  /// The colour if it is safe to put in a style attribute, or null. Colours can come from the data (a series'
  /// <c>ColorField</c>), so anything that could end the declaration and add others, or load something, is refused
  /// </summary>
  public static string? Safe(string? colour) =>
    string.IsNullOrWhiteSpace(colour) || colour.IndexOfAny(UnsafeCharacters) >= 0 || colour.Contains("url(", StringComparison.OrdinalIgnoreCase)
      ? null
      : colour.Trim();

  /// <summary>
  /// Whether dark text reads better than white on <paramref name="colour"/>. Colours that can't be parsed (named colours,
  /// variables) count as dark
  /// </summary>
  public static bool IsLight(string? colour) {
    if (colour is null || !TryParse(colour.Trim(), out double r, out double g, out double b)) {
      return false;
    }
    double luminance = .2126 * Linear(r) + .7152 * Linear(g) + .0722 * Linear(b);
    return luminance > .45;
  }

  private static double Linear(double channel) {
    double c = channel / 255;
    return c <= .03928 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);
  }

  private static bool TryParse(string colour, out double r, out double g, out double b) {
    r = g = b = 0;
    if (colour.StartsWith('#')) {
      string hex = colour[1..];
      if (hex.Length is 3 or 4) {
        hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
      }
      if (hex.Length < 6 || !int.TryParse(hex[..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)) {
        return false;
      }
      r = (value >> 16) & 255;
      g = (value >> 8) & 255;
      b = value & 255;
      return true;
    }
    if (colour.StartsWith("rgb", StringComparison.OrdinalIgnoreCase)) {
      int open = colour.IndexOf('(');
      int close = colour.IndexOf(')');
      if (open < 0 || close < open) {
        return false;
      }
      string[] parts = colour[(open + 1)..close].Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 3) {
        return false;
      }
      return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out r)
        && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out g)
        && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b);
    }
    return false;
  }
}
