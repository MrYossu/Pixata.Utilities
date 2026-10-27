using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// What the layout works from: the chart's settings, its size and a way of measuring text
/// </summary>
internal sealed class PixataChartLayoutInput {
  public IReadOnlyList<PixataChartSeries> Series { get; init; } = [];
  public PixataChartCategoryAxis? CategoryAxis { get; init; }
  public IReadOnlyList<PixataChartValueAxis> ValueAxes { get; init; } = [];
  public double Width { get; init; } = 600;
  public double Height { get; init; } = 300;
  public PixataChartTextMeasurer Measurer { get; init; } = new();
}

/// <summary>
/// Works out where everything on the chart goes, in plain C# so it can be tested without rendering anything
/// </summary>
internal static class PixataChartLayout {
  public const double Padding = 6;
  public const double TickLength = 4;
  public const double LabelGap = 4;
  public const double TitleGap = 6;
  public const double DefaultGap = 1.5;
  public const double DefaultSpacing = .4;
  public const double DefaultMarkerSize = 7;
  public const double DefaultLineWidth = 2;
  public const double DefaultPieLabelDistance = 35;
  public const double DefaultStartAngle = 90;
  public const double DefaultHoleSize = 50;
  public const double RingGap = 2;

  private const string AxisLabel = PixataChartTextMeasurer.AxisLabel;
  private const string AxisTitle = PixataChartTextMeasurer.AxisTitle;
  private const string Label = PixataChartTextMeasurer.Label;

  public static PixataChartScene Build(PixataChartLayoutInput input) {
    PixataChartScene scene = new() {
      Width = Math.Max(input.Width, 1),
      Height = Math.Max(input.Height, 1)
    };
    scene.HasData = input.Series.Any(s => s.Data?.Any() == true);
    // The first pie or donut series decides what sort of chart this is: a pie shows just that series, and a donut has a ring
    // for every donut series
    int round = -1;
    for (int i = 0; i < input.Series.Count; i++) {
      if (input.Series[i].SeriesType is PixataChartSeriesType.Pie or PixataChartSeriesType.Donut) {
        round = i;
        break;
      }
    }
    if (round >= 0 && input.Series[round].SeriesType == PixataChartSeriesType.Pie) {
      BuildPie(scene, input, [round], false);
    } else if (round >= 0) {
      BuildPie(scene, input, Enumerable.Range(0, input.Series.Count).Where(i => input.Series[i].SeriesType == PixataChartSeriesType.Donut).ToList(), true);
    } else {
      BuildCartesian(scene, input);
    }
    return scene;
  }

  #region Column and line charts

  private sealed class SeriesInfo {
    public required PixataChartSeries Series { get; init; }
    public int Index { get; init; }
    public int AxisIndex { get; init; }
    public required double?[] Values { get; init; }
    public required object?[] Items { get; init; }
    public string? StackKey { get; set; }
    public bool Percent { get; set; }
    public PixataChartStackedValue?[] Stacked { get; set; } = [];
    public bool Visible => Series.IsVisible;
    public bool IsColumn => Series.SeriesType == PixataChartSeriesType.Column;
  }

  private static void BuildCartesian(PixataChartScene scene, PixataChartLayoutInput input) {
    PixataChartTextMeasurer measurer = input.Measurer;
    PixataChartCategoryAxis? categoryAxis = input.CategoryAxis;
    List<PixataChartValueAxis> axes = input.ValueAxes.Count > 0 ? [.. input.ValueAxes] : [new()];

    // Categories: the axis's own, then any new ones from series with a CategoryField
    List<object?> categories = [.. categoryAxis?.Categories ?? []];
    Dictionary<object, int> categoryIndex = [];
    for (int i = 0; i < categories.Count; i++) {
      if (categories[i] is { } c) {
        categoryIndex.TryAdd(c, i);
      }
    }
    foreach (PixataChartSeries series in input.Series) {
      if (string.IsNullOrEmpty(series.CategoryField) || series.Data is null) {
        continue;
      }
      foreach (object item in series.Data) {
        if (PixataFieldReader.GetValue(item, series.CategoryField) is { } c && categoryIndex.TryAdd(c, categories.Count)) {
          categories.Add(c);
        }
      }
    }
    int count = categories.Count;
    foreach (PixataChartSeries series in input.Series) {
      if (string.IsNullOrEmpty(series.CategoryField) && series.Data is not null) {
        count = Math.Max(count, series.Data.Count());
      }
    }
    while (categories.Count < count) {
      categories.Add(null);
    }
    scene.Categories.AddRange(categories);

    // Each series' values, by category
    List<SeriesInfo> all = [];
    for (int i = 0; i < input.Series.Count; i++) {
      PixataChartSeries series = input.Series[i];
      double?[] values = new double?[count];
      object?[] items = new object?[count];
      if (series.Data is not null) {
        int position = 0;
        foreach (object item in series.Data) {
          int index = position++;
          if (!string.IsNullOrEmpty(series.CategoryField)) {
            if (PixataFieldReader.GetValue(item, series.CategoryField) is not { } c || !categoryIndex.TryGetValue(c, out index)) {
              continue;
            }
          }
          if (index < count) {
            values[index] = PixataChartFormatter.ToDouble(PixataFieldReader.GetValue(item, series.Field));
            items[index] = item;
          }
        }
      }
      int axisIndex = string.IsNullOrEmpty(series.Axis) ? 0 : Math.Max(0, axes.FindIndex(a => a.Name == series.Axis));
      all.Add(new() { Series = series, Index = i, AxisIndex = axisIndex, Values = values, Items = items });
    }

    // Stacking: a series' own stack settings, or those of the first series of its type
    foreach (PixataChartSeriesType type in new[] { PixataChartSeriesType.Column, PixataChartSeriesType.Line }) {
      PixataChartSeriesStack? first = all.FirstOrDefault(s => s.Series.SeriesType == type)?.Series.Stack;
      bool firstEnabled = first is not null && (first.Enabled ?? true);
      foreach (SeriesInfo info in all.Where(s => s.Series.SeriesType == type)) {
        PixataChartSeriesStack? own = info.Series.Stack;
        bool stacked = own is not null ? own.Enabled ?? true : firstEnabled;
        if (!stacked) {
          continue;
        }
        PixataChartSeriesStackType stackType = own?.Type ?? first?.Type ?? PixataChartSeriesStackType.Normal;
        info.Percent = stackType == PixataChartSeriesStackType.Stack100;
        info.StackKey = $"{type}|{info.AxisIndex}|{own?.Group ?? first?.Group ?? ""}|{info.Percent}";
      }
    }
    foreach (IGrouping<string, SeriesInfo> stack in all.Where(s => s.Visible && s.StackKey is not null).GroupBy(s => s.StackKey!)) {
      List<SeriesInfo> members = [.. stack];
      PixataChartStackedValue?[][] stacked = PixataChartStacking.Stack(members.Select(StackableValues).ToList(), members[0].Percent);
      for (int i = 0; i < members.Count; i++) {
        members[i].Stacked = stacked[i];
      }
    }
    foreach (SeriesInfo info in all.Where(s => s.StackKey is null || !s.Visible)) {
      info.Stacked = StackableValues(info).Select(v => v is double d ? new PixataChartStackedValue(0, d, 0, 0) : (PixataChartStackedValue?)null).ToArray();
    }
    List<SeriesInfo> visible = all.Where(s => s.Visible && s.Series.SeriesType != PixataChartSeriesType.Pie).ToList();

    // The data range of each value axis
    double[] dataMin = axes.Select(_ => double.NaN).ToArray();
    double[] dataMax = axes.Select(_ => double.NaN).ToArray();
    bool[] percent = new bool[axes.Count];
    foreach (SeriesInfo info in visible) {
      percent[info.AxisIndex] |= info.Percent;
      foreach (PixataChartStackedValue? value in info.Stacked) {
        if (value is not { } v) {
          continue;
        }
        Include(info.AxisIndex, v.To);
        if (info.IsColumn || info.StackKey is not null) {
          Include(info.AxisIndex, v.From);
        }
      }
    }

    void Include(int axis, double value) {
      dataMin[axis] = double.IsNaN(dataMin[axis]) ? value : Math.Min(dataMin[axis], value);
      dataMax[axis] = double.IsNaN(dataMax[axis]) ? value : Math.Max(dataMax[axis], value);
    }

    // Which side each value axis goes on
    bool[] onRight = new bool[axes.Count];
    for (int i = 0; i < axes.Count; i++) {
      object? crossing = categoryAxis?.AxisCrossingValue is { } crossings && i < crossings.Length ? crossings[i] : null;
      onRight[i] = PixataChartFormatter.ToDouble(crossing) is double at && at >= Math.Max(count, 1);
    }

    // Sizes of the text around the plot area
    double labelSize = measurer.FontSize(AxisLabel);
    double labelLine = measurer.LineHeight(AxisLabel);
    double titleSize = measurer.FontSize(AxisTitle);
    double titleLine = measurer.LineHeight(AxisTitle);
    bool categoryAxisVisible = categoryAxis?.IsVisible ?? true;
    PixataChartAxisLabelsBase? categoryLabels = categoryAxis?.Labels;
    bool categoryLabelsVisible = categoryAxisVisible && (categoryLabels?.Visible ?? true);
    string? categoryFont = categoryLabels?.Font;
    double categoryLabelLine = measurer.LineHeight(AxisLabel, categoryFont);
    double categoryLabelSize = measurer.FontSize(AxisLabel, categoryFont);
    string[] categoryTexts = categories.Select((c, i) => AxisLabelText(categoryLabels, c, FirstItem(all, i), null)).ToArray();
    double[] categoryWidths = categoryLabelsVisible ? categoryTexts.Select(t => measurer.Width(t, AxisLabel, categoryFont)).ToArray() : [];
    PixataChartAxisTitleBase? categoryTitle = categoryAxis?.Title;
    bool categoryTitleVisible = categoryAxisVisible && (categoryTitle?.Visible ?? true) && !string.IsNullOrEmpty(categoryTitle?.Text);
    double categoryTitleHeight = categoryTitleVisible ? measurer.LineHeight(AxisTitle, categoryTitle!.Font) + TitleGap : 0;
    bool hasLabels = visible.Any(s => s.Series.Labels?.Visible == true);
    bool justified = categoryAxis?.Justified == true && !visible.Any(s => s.IsColumn);

    double top = Padding + Math.Max(labelLine / 2, hasLabels ? measurer.LineHeight(Label) : 0);
    double width = scene.Width;
    double height = scene.Height;
    double categoryArea = categoryLabelsVisible ? categoryLabelLine : 0;
    PixataChartLabelFit fit = new(0, 1, categoryArea, 0);
    PixataChartValueScale[] scales = [];
    double[] axisWidths = new double[axes.Count];
    string[][] tickTexts = new string[axes.Count][];
    double left = Padding;
    double right = Padding;
    double plotWidth = 0;
    double plotHeight = 0;
    for (int pass = 0; pass < 3; pass++) {
      double bottomSpace = Padding + (categoryAxisVisible ? TickLength + (categoryLabelsVisible ? LabelGap + categoryArea : 0) : 0) + categoryTitleHeight;
      plotHeight = Math.Max(10, height - top - bottomSpace);
      scales = new PixataChartValueScale[axes.Count];
      left = Padding;
      right = Padding;
      for (int i = 0; i < axes.Count; i++) {
        PixataChartValueAxis axis = axes[i];
        scales[i] = PixataChartValueScale.Create(dataMin[i], dataMax[i], axis.Min, axis.Max, axis.MajorUnit, axis.MinorUnit, axis.NarrowRange == true, percent[i], plotHeight, labelLine * 1.75);
        string? format = axis.Labels?.Format ?? (percent[i] ? "P0" : null);
        tickTexts[i] = scales[i].MajorTicks().Select(v => AxisLabelText(axis.Labels, v, null, format)).ToArray();
        axisWidths[i] = AxisWidth(axis, tickTexts[i], measurer, titleLine);
        if (onRight[i]) {
          right += axisWidths[i];
        } else {
          left += axisWidths[i];
        }
      }
      plotWidth = Math.Max(10, width - left - right);
      double band = count <= 0 ? plotWidth : justified && count > 1 ? plotWidth / (count - 1) : plotWidth / count;
      double? angle = PixataChartLabelFitter.Angle(categoryLabels?.Rotation?.Angle);
      int? step = categoryLabels?.Step is double s && s >= 1 ? (int)s : null;
      fit = PixataChartLabelFitter.Fit(categoryWidths, categoryLabelLine, band, angle, step, Math.Max(categoryLabelLine * 2, height * .35));
      double newArea = categoryLabelsVisible ? fit.AreaHeight : 0;
      if (Math.Abs(newArea - categoryArea) < .5 && pass > 0) {
        break;
      }
      categoryArea = newArea;
    }
    PixataChartBox plot = new(left, top, plotWidth, plotHeight);
    scene.Plot = plot;
    PixataChartCategoryScale categoryScale = new(count, plot.X, plot.Width, justified);
    scene.CategoryScale = categoryScale;
    foreach (PixataChartValueScale scale in scales) {
      scale.Start = plot.Bottom;
      scale.Length = plot.Height;
    }

    // Grid lines go underneath everything else
    for (int i = 0; i < axes.Count; i++) {
      AddValueGridLines(scene, axes[i], scales[i], plot, i == 0);
    }
    if (count > 0) {
      AddCategoryGridLines(scene, categoryAxis, categoryScale, plot);
    }

    // The value axes, working outwards from the plot area on each side
    double leftEdge = plot.X;
    double rightEdge = plot.Right;
    for (int i = 0; i < axes.Count; i++) {
      PixataChartValueAxis axis = axes[i];
      if (!axis.IsVisible) {
        continue;
      }
      bool isRight = onRight[i];
      double x = isRight ? rightEdge : leftEdge;
      double direction = isRight ? 1 : -1;
      string? colour = axis.Color;
      scene.AxisLines.Add(new(x, plot.Y, x, plot.Bottom, "pixata-blazor-chart-axis-line", colour));
      IReadOnlyList<double> ticks = scales[i].MajorTicks();
      bool labelsVisible = axis.Labels?.Visible ?? true;
      int step = axis.Labels?.Step is double st && st >= 1 ? (int)st : 1;
      int skip = axis.Labels?.Skip is double sk && sk >= 0 ? (int)sk : 0;
      double? rotation = PixataChartLabelFitter.Angle(axis.Labels?.Rotation?.Angle);
      string? labelFont = axis.Labels?.Font;
      double fontSize = measurer.FontSize(AxisLabel, labelFont);
      for (int t = 0; t < ticks.Count; t++) {
        double y = scales[i].ToPixel(ticks[t]);
        scene.AxisLines.Add(new(x, y, x + direction * TickLength, y, "pixata-blazor-chart-axis-line pixata-blazor-chart-tick", colour));
        if (labelsVisible && PixataChartLabelFitter.Shows(t, step, skip)) {
          double labelX = x + direction * (TickLength + LabelGap);
          scene.AxisTexts.Add(new([tickTexts[i][t]], labelX, y + fontSize * .35, isRight ? "start" : "end", AxisLabel, labelLine, rotation ?? 0, axis.Labels?.Color ?? colour, labelFont, FontSize: fontSize));
        }
      }
      PixataChartAxisTitleBase? title = axis.Title;
      if ((title?.Visible ?? true) && !string.IsNullOrEmpty(title?.Text)) {
        double size = measurer.FontSize(AxisTitle, title.Font);
        double titleX = isRight ? x + axisWidths[i] - size * .25 : x - axisWidths[i] + size * .85;
        scene.AxisTexts.Add(new([title.Text], titleX, plot.Y + plot.Height / 2, "middle", AxisTitle, titleLine, -90, title.Color ?? colour, title.Font, FontSize: size));
      }
      if (isRight) {
        rightEdge += axisWidths[i];
      } else {
        leftEdge -= axisWidths[i];
      }
    }

    // The category axis
    if (categoryAxisVisible) {
      string? colour = categoryAxis?.Color;
      scene.AxisLines.Add(new(plot.X, plot.Bottom, plot.Right, plot.Bottom, "pixata-blazor-chart-axis-line", colour));
      if (count > 0) {
        foreach (double x in CategoryLinePositions(categoryScale, true)) {
          scene.AxisLines.Add(new(x, plot.Bottom, x, plot.Bottom + TickLength, "pixata-blazor-chart-axis-line pixata-blazor-chart-tick", colour));
        }
      }
      double labelTop = plot.Bottom + TickLength + LabelGap;
      if (categoryLabelsVisible) {
        int skip = categoryLabels?.Skip is double sk && sk >= 0 ? (int)sk : 0;
        double radians = fit.Angle * Math.PI / 180;
        for (int i = 0; i < count; i++) {
          if (!PixataChartLabelFitter.Shows(i, fit.Step, skip) || categoryTexts[i] == "") {
            continue;
          }
          string text = PixataChartTextMeasurer.Truncate(categoryTexts[i], categoryWidths[i], fit.MaxWidth + .5);
          double x = categoryScale.Centre(i);
          string? labelColour = categoryLabels?.Color ?? colour;
          string? full = text == categoryTexts[i] ? null : categoryTexts[i];
          if (fit.Angle == 0) {
            scene.AxisTexts.Add(new([text], x, labelTop + categoryLabelSize * .85, "middle", AxisLabel, categoryLabelLine, 0, labelColour, categoryFont, FontSize: categoryLabelSize, Title: full));
          } else {
            // Turned labels hang from their tick by their end (or start, when turned clockwise), nudged so the middle of the
            // text, rather than its baseline, lines up with the tick
            double nudge = categoryLabelSize * .35;
            double nudgeX = nudge * Math.Sin(Math.Abs(radians)) * (fit.Angle < 0 ? 1 : -1);
            double nudgeY = nudge * Math.Cos(radians);
            scene.AxisTexts.Add(new([text], x + nudgeX, labelTop + nudgeY, fit.Angle < 0 ? "end" : "start", AxisLabel, categoryLabelLine, fit.Angle, labelColour, categoryFont, FontSize: categoryLabelSize, Title: full));
          }
        }
      }
      if (categoryTitleVisible) {
        double size = measurer.FontSize(AxisTitle, categoryTitle!.Font);
        double titleY = labelTop + (categoryLabelsVisible ? categoryArea : 0) + TitleGap + size * .85;
        scene.AxisTexts.Add(new([categoryTitle.Text!], plot.X + plot.Width / 2, titleY, "middle", AxisTitle, titleLine, 0, categoryTitle.Color ?? colour, categoryTitle.Font, FontSize: size));
      }
    }

    // Columns
    List<SeriesInfo> columns = visible.Where(s => s.IsColumn).ToList();
    List<string> slots = columns.Select(SlotKey).Distinct().ToList();
    PixataChartSeries? firstColumn = all.FirstOrDefault(s => s.IsColumn)?.Series;
    double gap = firstColumn?.Gap ?? DefaultGap;
    double spacing = firstColumn?.Spacing ?? DefaultSpacing;
    foreach (SeriesInfo info in columns) {
      (double offset, double slotWidth) = PixataChartCategoryScale.ColumnSlot(categoryScale.BandWidth, slots.Count, slots.IndexOf(SlotKey(info)), gap, spacing);
      PixataChartValueScale scale = scales[info.AxisIndex];
      for (int c = 0; c < count; c++) {
        if (info.Values[c] is not double value || info.Stacked[c] is not { } stacked) {
          continue;
        }
        double yFrom = scale.ToPixel(stacked.From);
        double yTo = scale.ToPixel(stacked.To);
        double x = categoryScale.BandStart(c) + offset;
        bool positive = stacked.To >= stacked.From;
        (string colour, string reported) = PointColour(info.Series, info.Index, info.Items[c]);
        PixataChartScenePoint point = new() {
          Series = info.Series,
          SeriesIndex = info.Index,
          PointIndex = c,
          Kind = PixataChartPointKind.Column,
          Category = categories[c],
          Value = value,
          DataItem = info.Items[c],
          Percentage = info.Percent ? stacked.Percentage : 0,
          Total = info.Percent ? stacked.Total : 0,
          Colour = colour,
          ReportedColour = reported,
          X = x,
          Y = Math.Min(yFrom, yTo),
          Width = slotWidth,
          Height = Math.Abs(yFrom - yTo),
          Opacity = info.Series.Opacity,
          AnchorX = x + slotWidth / 2,
          AnchorY = positive ? Math.Min(yFrom, yTo) : Math.Max(yFrom, yTo),
          Description = Describe(info.Series, categoryTexts[c], value, info.Series.Labels?.Format)
        };
        scene.Points.Add(point);
        AddColumnLabel(scene, info, point, positive, measurer);
      }
    }

    // Lines
    foreach (SeriesInfo info in visible.Where(s => s.Series.SeriesType == PixataChartSeriesType.Line)) {
      PixataChartSeries series = info.Series;
      PixataChartValueScale scale = scales[info.AxisIndex];
      PixataChartSeriesMissingValues missing = Missing(info);
      List<(double X, double Y)?> path = [];
      List<(double Start, double End)> spans = [];
      PixataChartSeriesMarkers? markers = series.Markers;
      double markerSize = markers?.Size ?? DefaultMarkerSize;
      PixataChartSeriesMarkersType markerType = markers?.Type ?? PixataChartSeriesMarkersType.Circle;
      (string lineColour, _) = PointColour(series, info.Index, null);
      for (int c = 0; c < count; c++) {
        double? value = info.Values[c];
        if (value is null && missing == PixataChartSeriesMissingValues.Interpolate) {
          continue;
        }
        double start = Math.Max(plot.X, categoryScale.BandStart(c));
        double end = Math.Min(plot.Right, categoryScale.BandStart(c) + categoryScale.BandWidth);
        if (value is null && missing == PixataChartSeriesMissingValues.Gap) {
          path.Add(null);
          spans.Add((start, end));
          continue;
        }
        double plotted = info.Stacked[c]?.To ?? value ?? 0;
        double x = categoryScale.Centre(c);
        double y = scale.ToPixel(plotted);
        path.Add((x, y));
        spans.Add((start, end));
        (string colour, string reported) = PointColour(series, info.Index, info.Items[c]);
        PixataChartScenePoint point = new() {
          Series = series,
          SeriesIndex = info.Index,
          PointIndex = c,
          Kind = PixataChartPointKind.Marker,
          Category = categories[c],
          Value = value ?? 0,
          DataItem = info.Items[c],
          Percentage = info.Percent ? info.Stacked[c]?.Percentage ?? 0 : 0,
          Total = info.Percent ? info.Stacked[c]?.Total ?? 0 : 0,
          Colour = colour,
          ReportedColour = reported,
          X = x,
          Y = y,
          Width = markerSize,
          Path = MarkerPath(markerType, x, y, markerSize),
          MarkerType = markerType,
          MarkerVisible = markers?.Visible ?? true,
          MarkerBackground = markers?.Background,
          Opacity = series.Opacity,
          AnchorX = x,
          AnchorY = y - markerSize / 2,
          Description = Describe(series, categoryTexts[c], value ?? 0, series.Labels?.Format)
        };
        scene.Points.Add(point);
        AddLineLabel(scene, info, point, measurer);
      }
      string d = (series.Style ?? PixataChartSeriesStyle.Normal) switch {
        PixataChartSeriesStyle.Smooth => PixataChartPaths.Smooth(path),
        PixataChartSeriesStyle.Step => PixataChartPaths.Step(path, spans),
        _ => PixataChartPaths.Linear(path)
      };
      if (d != "") {
        scene.Paths.Add(new(d, "pixata-blazor-chart-line", info.Index, lineColour, series.Width ?? DefaultLineWidth, DashArray(series.DashType, series.Width ?? DefaultLineWidth), series.Opacity));
      }
    }

    // Columns are drawn before lines, so lines (and their markers) sit on top
    scene.Points.Sort((a, b) => a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.SeriesIndex != b.SeriesIndex ? a.SeriesIndex.CompareTo(b.SeriesIndex) : a.PointIndex.CompareTo(b.PointIndex));

    // The legend lists every series with a name, including hidden ones (greyed out) so they can be shown again
    foreach (SeriesInfo info in all) {
      PixataChartSeries series = info.Series;
      if (series.SeriesType == PixataChartSeriesType.Pie || !(series.VisibleInLegend ?? true) || string.IsNullOrEmpty(series.Name)) {
        continue;
      }
      (string colour, _) = PointColour(series, info.Index, null);
      scene.LegendEntries.Add(new(info.Index, null, series.Name, colour, series.SeriesType, series.IsVisible, series.SeriesType == PixataChartSeriesType.Line ? DashArray(series.DashType, 2) : null));
    }
  }

  private static double?[] StackableValues(SeriesInfo info) =>
    info.Series.SeriesType == PixataChartSeriesType.Line && Missing(info) == PixataChartSeriesMissingValues.Zero
      ? info.Values.Select(v => v ?? 0).Select(v => (double?)v).ToArray()
      : info.Values;

  /// <summary>
  /// What a line series does with missing values: interpolate by default, or treat them as zero when stacked
  /// </summary>
  private static PixataChartSeriesMissingValues Missing(SeriesInfo info) =>
    info.Series.MissingValues ?? (info.StackKey is null ? PixataChartSeriesMissingValues.Interpolate : PixataChartSeriesMissingValues.Zero);

  private static string SlotKey(SeriesInfo info) =>
    info.StackKey is null ? $"series|{info.Index}" : $"stack|{info.StackKey}";

  private static object? FirstItem(List<SeriesInfo> series, int category) =>
    series.Select(s => category < s.Items.Length ? s.Items[category] : null).FirstOrDefault(i => i is not null);

  private static double AxisWidth(PixataChartValueAxis axis, string[] texts, PixataChartTextMeasurer measurer, double titleLine) {
    if (!axis.IsVisible) {
      return 0;
    }
    double width = TickLength;
    if (axis.Labels?.Visible ?? true) {
      width += LabelGap + texts.Select(t => measurer.Width(t, AxisLabel, axis.Labels?.Font)).DefaultIfEmpty(0).Max();
    }
    if ((axis.Title?.Visible ?? true) && !string.IsNullOrEmpty(axis.Title?.Text)) {
      width += TitleGap + measurer.LineHeight(AxisTitle, axis.Title.Font);
    }
    return Math.Ceiling(width);
  }

  private static void AddValueGridLines(PixataChartScene scene, PixataChartValueAxis axis, PixataChartValueScale scale, PixataChartBox plot, bool primary) {
    PixataChartGridLinesBase? minor = axis.MinorGridLines;
    if (minor?.Visible == true) {
      AddGridLines(scene, minor, scale.MinorTicks().Select(scale.ToPixel), y => new(plot.X, y, plot.Right, y, "pixata-blazor-chart-gridline pixata-blazor-chart-gridline-minor"));
    }
    PixataChartGridLinesBase? major = axis.MajorGridLines;
    if (major?.Visible ?? primary) {
      AddGridLines(scene, major, scale.MajorTicks().Select(scale.ToPixel), y => new(plot.X, y, plot.Right, y, "pixata-blazor-chart-gridline"));
    }
  }

  private static void AddCategoryGridLines(PixataChartScene scene, PixataChartCategoryAxis? axis, PixataChartCategoryScale scale, PixataChartBox plot) {
    PixataChartGridLinesBase? minor = axis?.MinorGridLines;
    if (minor?.Visible == true) {
      AddGridLines(scene, minor, CategoryLinePositions(scale, false), x => new(x, plot.Y, x, plot.Bottom, "pixata-blazor-chart-gridline pixata-blazor-chart-gridline-minor"));
    }
    PixataChartGridLinesBase? major = axis?.MajorGridLines;
    if (major?.Visible ?? true) {
      AddGridLines(scene, major, CategoryLinePositions(scale, true), x => new(x, plot.Y, x, plot.Bottom, "pixata-blazor-chart-gridline"));
    }
  }

  /// <summary>
  /// Where the category grid lines and ticks go: the band boundaries (major) or middles (minor), swapped round on a
  /// justified axis, whose categories sit on the major lines
  /// </summary>
  private static IEnumerable<double> CategoryLinePositions(PixataChartCategoryScale scale, bool major) {
    if (scale.Justified == major) {
      for (int i = 0; i < scale.Count; i++) {
        yield return scale.Centre(i);
      }
    } else {
      int boundaries = scale.Justified ? scale.Count - 1 : scale.Count + 1;
      for (int i = 0; i < boundaries; i++) {
        yield return scale.Justified ? scale.Centre(i) + scale.BandWidth / 2 : scale.Start + i * scale.BandWidth;
      }
    }
  }

  private static void AddGridLines(PixataChartScene scene, PixataChartGridLinesBase? settings, IEnumerable<double> positions, Func<double, PixataChartSceneLine> line) {
    int step = settings?.Step is double st && st >= 1 ? (int)st : 1;
    int skip = settings?.Skip is double sk && sk >= 0 ? (int)sk : 0;
    int index = 0;
    foreach (double position in positions) {
      if (PixataChartLabelFitter.Shows(index++, step, skip)) {
        PixataChartSceneLine basic = line(position);
        scene.GridLines.Add(basic with { Colour = settings?.Color, Width = settings?.Width, DashArray = DashArray(settings?.DashType, settings?.Width ?? 1) });
      }
    }
  }

  private static string AxisLabelText(PixataChartAxisLabelsBase? labels, object? value, object? dataItem, string? defaultFormat) {
    if (!string.IsNullOrEmpty(labels?.Template)) {
      return PixataChartFormatter.ApplyTemplate(labels.Template, name => name.ToLowerInvariant() switch {
        "value" => value,
        "dataitem" => dataItem,
        _ when name.StartsWith("dataItem.", StringComparison.OrdinalIgnoreCase) => SafeField(dataItem, name[9..]),
        _ => null
      });
    }
    return PixataChartFormatter.Format(value, labels?.Format ?? defaultFormat);
  }

  private static void AddColumnLabel(PixataChartScene scene, SeriesInfo info, PixataChartScenePoint point, bool positive, PixataChartTextMeasurer measurer) {
    PixataChartSeriesLabels? labels = info.Series.Labels;
    if (labels?.Visible != true) {
      return;
    }
    string[] lines = PixataChartFormatter.Lines(LabelText(labels, point));
    double size = measurer.FontSize(Label, labels.Font);
    double line = measurer.LineHeight(Label, labels.Font);
    double extra = (lines.Length - 1) * line;
    double top = point.Y;
    double bottom = point.Y + point.Height;
    double x = point.X + point.Width / 2;
    double y = (labels.Position ?? PixataChartSeriesLabelsPosition.OutsideEnd) switch {
      PixataChartSeriesLabelsPosition.InsideEnd => positive ? top + LabelGap + size * .85 : bottom - LabelGap - extra - size * .15,
      PixataChartSeriesLabelsPosition.Center => (top + bottom) / 2 + size * .35 - extra / 2,
      PixataChartSeriesLabelsPosition.InsideBase => positive ? bottom - LabelGap - extra - size * .15 : top + LabelGap + size * .85,
      _ => positive ? top - LabelGap - extra - size * .15 : bottom + LabelGap + size * .85
    };
    scene.Labels.Add(new(lines, x, y, "middle", Label, line, 0, labels.Color, labels.Font, labels.Background, measurer.Width(lines, Label, labels.Font), size));
  }

  private static void AddLineLabel(PixataChartScene scene, SeriesInfo info, PixataChartScenePoint point, PixataChartTextMeasurer measurer) {
    PixataChartSeriesLabels? labels = info.Series.Labels;
    if (labels?.Visible != true) {
      return;
    }
    string[] lines = PixataChartFormatter.Lines(LabelText(labels, point));
    double size = measurer.FontSize(Label, labels.Font);
    double line = measurer.LineHeight(Label, labels.Font);
    double extra = (lines.Length - 1) * line;
    double offset = point.Width / 2 + LabelGap;
    (double x, double y, string anchor) = (labels.Position ?? PixataChartSeriesLabelsPosition.Above) switch {
      PixataChartSeriesLabelsPosition.Below or PixataChartSeriesLabelsPosition.Bottom => (point.X, point.Y + offset + size * .85, "middle"),
      PixataChartSeriesLabelsPosition.Left => (point.X - offset, point.Y + size * .35 - extra / 2, "end"),
      PixataChartSeriesLabelsPosition.Right => (point.X + offset, point.Y + size * .35 - extra / 2, "start"),
      PixataChartSeriesLabelsPosition.Center => (point.X, point.Y + size * .35 - extra / 2, "middle"),
      _ => (point.X, point.Y - offset - extra - size * .15, "middle")
    };
    scene.Labels.Add(new(lines, x, y, anchor, Label, line, 0, labels.Color, labels.Font, labels.Background, measurer.Width(lines, Label, labels.Font), size));
  }

  #endregion

  #region Pie and donut charts

  /// <summary>
  /// A pie, or one ring of a donut, with its data read out of the series
  /// </summary>
  private sealed class Ring {
    public required PixataChartSeries Series { get; init; }
    public int Index { get; init; }
    public List<object> Items { get; } = [];
    public List<double?> Values { get; } = [];
    public List<object?> Categories { get; } = [];
    public List<string> CategoryTexts { get; } = [];
    public List<(string Css, string Reported)> Colours { get; } = [];
    public List<bool> Explode { get; } = [];
    public List<PixataChartPieSlice> Slices { get; set; } = [];
    public Dictionary<int, string[]> Texts { get; } = [];
    public Dictionary<int, double> Widths { get; } = [];
    public double Inner { get; set; }
    public double Outer { get; set; }
  }

  /// <summary>
  /// Draws a pie (one series) or a donut (one ring for each series, the first on the inside)
  /// </summary>
  private static void BuildPie(PixataChartScene scene, PixataChartLayoutInput input, IReadOnlyList<int> seriesIndexes, bool donut) {
    scene.IsPie = true;
    scene.IsDonut = donut;
    PixataChartTextMeasurer measurer = input.Measurer;
    List<Ring> rings = seriesIndexes.Select(i => ReadRing(input.Series[i], i)).ToList();
    AddPieLegend(scene, rings);
    scene.Categories.AddRange(rings[0].Categories);
    PixataChartBox area = new(Padding, Padding, Math.Max(10, scene.Width - Padding * 2), Math.Max(10, scene.Height - Padding * 2));
    scene.Plot = area;
    double cx = area.X + area.Width / 2;
    double cy = area.Y + area.Height / 2;
    scene.Hole = (cx, cy, 0);
    List<Ring> visible = rings.Where(r => r.Series.IsVisible).ToList();
    if (visible.Count == 0) {
      return;
    }

    // Outside labels only fit round the outer ring, so the labels of the rings inside it go in the middle of their segments
    Ring outer = visible[^1];
    PixataChartSeriesLabels? outerLabels = outer.Series.Labels;
    bool outside = outerLabels?.Visible == true && IsOutside(outerLabels);
    double distance = outerLabels?.Distance ?? DefaultPieLabelDistance;
    double labelSize = measurer.FontSize(Label, outerLabels?.Font);
    double labelLine = measurer.LineHeight(Label, outerLabels?.Font);
    foreach (Ring ring in visible) {
      ring.Slices = PixataChartPie.Slices(ring.Values, ring.Series.HiddenPoints, ring.Series.StartAngle ?? DefaultStartAngle);
      PixataChartSeriesLabels? labels = ring.Series.Labels;
      foreach (PixataChartPieSlice slice in ring.Slices) {
        // The label text of each slice, before the size of the pie is known
        PixataChartScenePoint context = PiePoint(ring, slice, false, 0, 0, null);
        ring.Texts[slice.Index] = labels?.Visible == true ? PixataChartFormatter.Lines(LabelText(labels, context)) : [];
        ring.Widths[slice.Index] = labels?.Visible == true ? measurer.Width(ring.Texts[slice.Index], Label, labels.Font) : 0;
      }
    }
    double widest = outer.Widths.Values.DefaultIfEmpty(0).Max();
    double tallest = outer.Texts.Values.Select(t => t.Length).DefaultIfEmpty(1).Max() * labelLine;
    bool anyExploded = visible.Any(r => r.Slices.Any(s => r.Explode[s.Index]));

    double radius;
    if (outer.Series.Size is > 0) {
      radius = outer.Series.Size.Value;
    } else if (outside) {
      double smallest = Math.Min(area.Width, area.Height);
      double byWidth = area.Width / 2 - widest - distance - LabelGap;
      if (byWidth < smallest * .3 && outerLabels?.Distance is null) {
        // On a narrow chart (a phone, say), bring the labels in closer to leave more room for the pie
        distance = Math.Min(distance, 16);
        byWidth = area.Width / 2 - widest - distance - LabelGap;
      }
      double byHeight = area.Height / 2 - distance / 2 - tallest;
      radius = Math.Max(Math.Min(byWidth, byHeight), smallest * .25);
    } else {
      radius = Math.Min(area.Width, area.Height) / 2;
    }
    double explodeOffset = anyExploded ? Math.Max(4, radius * .1) : 0;
    if (outer.Series.Size is not > 0) {
      radius = Math.Max(4, radius - explodeOffset);
      explodeOffset = anyExploded ? Math.Max(4, radius * .1) : 0;
    }
    double maxLabelWidth = Math.Max(labelSize * 3, area.Width / 2 - radius - explodeOffset - distance - LabelGap);

    // The rings share what is left after the hole, with a small gap between them
    double hole = donut ? radius * Math.Clamp(visible[0].Series.HoleSize ?? DefaultHoleSize, 0, 100) / 100 : 0;
    scene.Hole = (cx, cy, hole);
    double ringGap = visible.Count > 1 ? Math.Min(RingGap, (radius - hole) / visible.Count / 4) : 0;
    double thickness = Math.Max(0, (radius - hole - ringGap * (visible.Count - 1)) / visible.Count);
    for (int r = 0; r < visible.Count; r++) {
      visible[r].Inner = hole + r * (thickness + ringGap);
      visible[r].Outer = visible[r].Inner + thickness;
    }

    List<PixataChartPieLabel> outsideLabels = [];
    foreach (Ring ring in visible) {
      PixataChartSeriesLabels? labels = ring.Series.Labels;
      foreach (PixataChartPieSlice slice in ring.Slices) {
        double offset = ring.Explode[slice.Index] ? explodeOffset : 0;
        (double sx, double sy) = PixataChartPie.PointAt(cx, cy, offset, slice.MidAngle);
        (double ax, double ay) = PixataChartPie.PointAt(sx, sy, ring.Inner + (ring.Outer - ring.Inner) * (donut ? .5 : .6), slice.MidAngle);
        string path = PixataChartPie.RingSegmentPath(sx, sy, ring.Inner, ring.Outer, slice.StartAngle, slice.EndAngle);
        scene.Points.Add(PiePoint(ring, slice, visible.Count > 1, ax, ay, path));
        if (labels?.Visible != true) {
          continue;
        }
        if (ring == outer && outside) {
          (double _, double ey) = PixataChartPie.PointAt(cx, cy, radius + explodeOffset + distance / 2, slice.MidAngle);
          outsideLabels.Add(new() { Index = slice.Index, Right = Math.Cos(slice.MidAngle * Math.PI / 180) >= -1e-9, Y = ey });
        } else {
          // A pie's centred labels go half way out and the others three quarters; a donut's go in the middle of the ring
          // unless they are asked to go at its outer end
          double share = labels.Position == PixataChartSeriesLabelsPosition.InsideEnd ? .75 : donut || labels.Position == PixataChartSeriesLabelsPosition.Center ? .5 : .75;
          (double lx, double ly) = PixataChartPie.PointAt(sx, sy, ring.Inner + (ring.Outer - ring.Inner) * share, slice.MidAngle);
          string[] lines = ring.Texts[slice.Index];
          double size = measurer.FontSize(Label, labels.Font);
          double line = measurer.LineHeight(Label, labels.Font);
          scene.Labels.Add(new(lines, lx, ly + size * .35 - (lines.Length - 1) * line / 2, "middle", Label, line, 0, labels.Color, labels.Font, labels.Background, ring.Widths[slice.Index], size));
        }
      }
    }
    if (outsideLabels.Count == 0) {
      return;
    }
    PixataChartPie.Spread(outsideLabels, labelLine, area.Y + labelLine / 2, area.Bottom - labelLine / 2);
    foreach (PixataChartPieLabel label in outsideLabels) {
      PixataChartPieSlice slice = outer.Slices.First(s => s.Index == label.Index);
      double offset = outer.Explode[slice.Index] ? explodeOffset : 0;
      (double ex, double ey) = PixataChartPie.PointAt(cx, cy, radius + offset + 1, slice.MidAngle);
      (double bx, double by) = PixataChartPie.PointAt(cx, cy, radius + offset + Math.Min(distance / 2, 12), slice.MidAngle);
      double labelX = cx + (label.Right ? 1 : -1) * (radius + explodeOffset + distance);
      double endX = labelX + (label.Right ? -LabelGap : LabelGap);
      if (label.Right ? bx > endX : bx < endX) {
        bx = endX;
      }
      scene.Connectors.Add(new($"M {PixataChartFormatter.Svg(ex)} {PixataChartFormatter.Svg(ey)} L {PixataChartFormatter.Svg(bx)} {PixataChartFormatter.Svg(by)} L {PixataChartFormatter.Svg(endX)} {PixataChartFormatter.Svg(label.Y)}", "pixata-blazor-chart-label-connector", outer.Index, outerLabels!.Color));
      string[] lines = outer.Texts[slice.Index].Select(t => PixataChartTextMeasurer.Truncate(t, measurer.Width(t, Label, outerLabels.Font), maxLabelWidth)).ToArray();
      double lineWidth = Math.Min(outer.Widths[slice.Index], maxLabelWidth);
      scene.Labels.Add(new(lines, labelX, label.Y + labelSize * .35 - (lines.Length - 1) * labelLine / 2, label.Right ? "start" : "end", Label, labelLine, 0, outerLabels.Color, outerLabels.Font, outerLabels.Background, lineWidth, labelSize));
    }
  }

  private static bool IsOutside(PixataChartSeriesLabels labels) =>
    (labels.Position ?? PixataChartSeriesLabelsPosition.OutsideEnd) is not (PixataChartSeriesLabelsPosition.InsideEnd or PixataChartSeriesLabelsPosition.Center or PixataChartSeriesLabelsPosition.InsideBase);

  private static Ring ReadRing(PixataChartSeries series, int seriesIndex) {
    Ring ring = new() { Series = series, Index = seriesIndex };
    ring.Items.AddRange(series.Data ?? []);
    for (int i = 0; i < ring.Items.Count; i++) {
      object item = ring.Items[i];
      ring.Values.Add(PixataChartFormatter.ToDouble(PixataFieldReader.GetValue(item, series.Field)));
      object? category = string.IsNullOrEmpty(series.CategoryField) ? null : PixataFieldReader.GetValue(item, series.CategoryField);
      ring.Categories.Add(category);
      ring.CategoryTexts.Add(PixataChartFormatter.Format(category, null));
      string? colour = string.IsNullOrEmpty(series.ColorField) ? null : PixataChartPalette.Safe(PixataFieldReader.GetValue(item, series.ColorField)?.ToString());
      ring.Colours.Add(colour is null ? (PixataChartPalette.Css(i), PixataChartPalette.Hex(i)) : (colour, colour));
      ring.Explode.Add(!string.IsNullOrEmpty(series.ExplodeField) && PixataChartFormatter.ToBool(PixataFieldReader.GetValue(item, series.ExplodeField)) == true);
    }
    return ring;
  }

  /// <summary>
  /// A legend item for each segment. The rings of a donut often share their categories, so a category that is already in
  /// the legend isn't listed again, and its item shows and hides it in every ring
  /// </summary>
  private static void AddPieLegend(PixataChartScene scene, List<Ring> rings) {
    List<(PixataChartLegendEntry Entry, List<(int Series, int Point)> Linked)> entries = [];
    Dictionary<string, int> byText = [];
    foreach (Ring ring in rings) {
      PixataChartSeries series = ring.Series;
      for (int i = 0; i < ring.Items.Count; i++) {
        bool inLegend = string.IsNullOrEmpty(series.VisibleInLegendField) || PixataChartFormatter.ToBool(PixataFieldReader.GetValue(ring.Items[i], series.VisibleInLegendField)) != false;
        if (!inLegend || !(series.VisibleInLegend ?? true)) {
          continue;
        }
        string text = ring.CategoryTexts[i];
        if (text != "" && byText.TryGetValue(text, out int existing)) {
          entries[existing].Linked.Add((ring.Index, i));
          continue;
        }
        if (text != "") {
          byText[text] = entries.Count;
        }
        entries.Add((new(ring.Index, i, text, ring.Colours[i].Css, series.SeriesType, series.IsVisible && !series.HiddenPoints.Contains(i), null), []));
      }
    }
    scene.LegendEntries.AddRange(entries.Select(e => e.Linked.Count == 0 ? e.Entry : e.Entry with { Linked = e.Linked }));
  }

  private static PixataChartScenePoint PiePoint(Ring ring, PixataChartPieSlice slice, bool nameRing, double anchorX, double anchorY, string? path) {
    PixataChartSeries series = ring.Series;
    string categoryText = ring.CategoryTexts[slice.Index];
    string what = categoryText == "" ? series.Name ?? "" : nameRing && !string.IsNullOrEmpty(series.Name) ? $"{series.Name}, {categoryText}" : categoryText;
    double? value = ring.Values[slice.Index];
    return new() {
      Series = series,
      SeriesIndex = ring.Index,
      PointIndex = slice.Index,
      Kind = PixataChartPointKind.Segment,
      Category = ring.Categories[slice.Index],
      Value = value,
      DataItem = ring.Items[slice.Index],
      Percentage = slice.Percentage,
      Total = slice.Total,
      Colour = ring.Colours[slice.Index].Css,
      ReportedColour = ring.Colours[slice.Index].Reported,
      Path = path,
      Opacity = series.Opacity,
      AnchorX = anchorX,
      AnchorY = anchorY,
      Description = $"{what}: {PixataChartFormatter.Format(value, series.Labels?.Format)} ({PixataChartFormatter.Format(slice.Percentage, "P0")})"
    };
  }

  #endregion

  #region Shared helpers

  /// <summary>
  /// The text of a data label
  /// </summary>
  public static string LabelText(PixataChartSeriesLabels labels, PixataChartScenePoint point) {
    if (labels.TextTemplate is not null) {
      return labels.TextTemplate(point.ToSeriesPoint<PixataChartSeriesLabelsTemplateContext>());
    }
    if (!string.IsNullOrEmpty(labels.Template)) {
      return PixataChartFormatter.ApplyTemplate(labels.Template, Resolver(point));
    }
    return PixataChartFormatter.Format(point.Value, labels.Format);
  }

  /// <summary>
  /// Looks up the names used in label templates
  /// </summary>
  public static Func<string, object?> Resolver(PixataChartScenePoint point) =>
    name => name.ToLowerInvariant() switch {
      "value" => point.Value,
      "category" => point.Category,
      "percentage" => point.Percentage,
      "dataitem" => point.DataItem,
      "series.name" => point.Series.Name,
      "series.color" => point.ReportedColour,
      _ when name.StartsWith("dataItem.", StringComparison.OrdinalIgnoreCase) => SafeField(point.DataItem, name[9..]),
      _ => null
    };

  private static object? SafeField(object? item, string field) {
    try {
      return PixataFieldReader.GetValue(item, field);
    }
    catch (InvalidOperationException) {
      return null;
    }
  }

  /// <summary>
  /// The CSS colour of a point (from the <c>ColorField</c>, the series' <c>Color</c> or the palette), and the colour reported
  /// to event handlers
  /// </summary>
  private static (string Css, string Reported) PointColour(PixataChartSeries series, int index, object? item) {
    string? own = !string.IsNullOrEmpty(series.ColorField) && item is not null ? PixataChartPalette.Safe(SafeField(item, series.ColorField)?.ToString()) : null;
    if (own is not null) {
      return (own, own);
    }
    if (PixataChartPalette.Safe(series.Color) is { } colour) {
      return (colour, colour);
    }
    return (PixataChartPalette.Css(index), PixataChartPalette.Hex(index));
  }

  private static string Describe(PixataChartSeries series, string category, double value, string? format) {
    string text = PixataChartFormatter.Format(value, format);
    return string.IsNullOrEmpty(series.Name) ? $"{category}: {text}" : $"{series.Name}, {category}: {text}";
  }

  /// <summary>
  /// The SVG path of a marker that isn't drawn as a circle or rectangle
  /// </summary>
  public static string? MarkerPath(PixataChartSeriesMarkersType type, double x, double y, double size) {
    double h = size / 2;
    return type switch {
      PixataChartSeriesMarkersType.Triangle => $"M {PixataChartFormatter.Svg(x)} {PixataChartFormatter.Svg(y - h)} L {PixataChartFormatter.Svg(x + h)} {PixataChartFormatter.Svg(y + h)} L {PixataChartFormatter.Svg(x - h)} {PixataChartFormatter.Svg(y + h)} Z",
      PixataChartSeriesMarkersType.Cross => $"M {PixataChartFormatter.Svg(x - h)} {PixataChartFormatter.Svg(y - h)} L {PixataChartFormatter.Svg(x + h)} {PixataChartFormatter.Svg(y + h)} M {PixataChartFormatter.Svg(x + h)} {PixataChartFormatter.Svg(y - h)} L {PixataChartFormatter.Svg(x - h)} {PixataChartFormatter.Svg(y + h)}",
      _ => null
    };
  }

  /// <summary>
  /// The stroke-dasharray of a dash type, scaled to the line width
  /// </summary>
  public static string? DashArray(PixataChartDashType? type, double width) {
    double w = Math.Max(width, 1);
    string Scale(params double[] parts) =>
      string.Join(" ", parts.Select(p => PixataChartFormatter.Svg(p * w)));
    return type switch {
      PixataChartDashType.Dot => Scale(1, 2),
      PixataChartDashType.Dash => Scale(4, 2),
      PixataChartDashType.LongDash => Scale(8, 3),
      PixataChartDashType.DashDot => Scale(4, 2, 1, 2),
      PixataChartDashType.LongDashDot => Scale(8, 3, 1, 3),
      PixataChartDashType.LongDashDotDot => Scale(8, 3, 1, 3, 1, 3),
      _ => null
    };
  }

  #endregion
}
