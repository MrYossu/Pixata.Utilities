using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Components;

internal static partial class PixataChartLayout {
  /// <summary>
  /// The thinnest a bar gets, in pixels. A bar chart with more categories than fit at this size grows taller
  /// </summary>
  public const double MinBarSize = 4;

  /// <summary>
  /// The most of the chart's width the category labels of a bar chart may take. Longer ones are shortened
  /// </summary>
  public const double MaxBarCategoryShare = .3;

  #region Bar charts

  /// <summary>
  /// Draws the bar series: the categories go down the left, the first at the top, and the values along the bottom. When
  /// there isn't the height for every category, at the thinnest bars and with room for each label, the chart grows taller
  /// </summary>
  private static void BuildBar(PixataChartScene scene, PixataChartLayoutInput input) {
    scene.IsBar = true;
    PixataChartTextMeasurer measurer = input.Measurer;
    PixataChartCategoryAxis? categoryAxis = input.CategoryAxis;
    List<PixataChartValueAxis> axes = input.ValueAxes.Count > 0 ? [.. input.ValueAxes] : [new()];
    CategoryData data = ReadCategoryData(input, axes, s => s.SeriesType == PixataChartSeriesType.Bar);
    List<object?> categories = data.Categories;
    int count = data.Count;
    List<SeriesInfo> bars = data.Visible;
    scene.Categories.AddRange(categories);

    // A value axis goes along the bottom, or along the top when the category axis crosses it after its last category
    bool[] onTop = new bool[axes.Count];
    for (int i = 0; i < axes.Count; i++) {
      object? crossing = categoryAxis?.AxisCrossingValue is { } crossings && i < crossings.Length ? crossings[i] : null;
      onTop[i] = PixataChartFormatter.ToDouble(crossing) is double at && at >= Math.Max(count, 1);
    }

    // The category axis, down the left
    double width = scene.Width;
    bool categoryAxisVisible = categoryAxis?.IsVisible ?? true;
    PixataChartAxisLabelsBase? categoryLabels = categoryAxis?.Labels;
    bool categoryLabelsVisible = categoryAxisVisible && (categoryLabels?.Visible ?? true);
    string? categoryFont = categoryLabels?.Font;
    double categoryLabelLine = measurer.LineHeight(AxisLabel, categoryFont);
    double categoryLabelSize = measurer.FontSize(AxisLabel, categoryFont);
    string[] categoryTexts = categories.Select((c, i) => AxisLabelText(categoryLabels, c, FirstItem(data.All, i), null)).ToArray();
    double[] categoryWidths = categoryLabelsVisible ? categoryTexts.Select(t => measurer.Width(t, AxisLabel, categoryFont)).ToArray() : [];
    int categoryStep = categoryLabels?.Step is double st && st >= 1 ? (int)st : 1;
    int categorySkip = categoryLabels?.Skip is double sk && sk >= 0 ? (int)sk : 0;
    PixataChartAxisTitleBase? categoryTitle = categoryAxis?.Title;
    bool categoryTitleVisible = categoryAxisVisible && (categoryTitle?.Visible ?? true) && !string.IsNullOrEmpty(categoryTitle?.Text);
    double categoryTitleWidth = categoryTitleVisible ? measurer.LineHeight(AxisTitle, categoryTitle!.Font) + TitleGap : 0;
    double maxCategoryWidth = Math.Min(categoryWidths.DefaultIfEmpty(0).Max(), Math.Max(categoryLabelSize * 3, width * MaxBarCategoryShare));
    double categoryAxisWidth = categoryAxisVisible ? Math.Ceiling(TickLength + (categoryLabelsVisible ? LabelGap + maxCategoryWidth : 0)) : 0;

    // How the bars share each category, and the least height a category can have: room for its bars at their thinnest,
    // and for its label (or for the labels that are shown, when only every nth one is)
    List<string> slots = bars.Select(SlotKey).Distinct().ToList();
    PixataChartSeries? firstBar = data.All.FirstOrDefault()?.Series;
    double gap = firstBar?.Gap ?? DefaultGap;
    double spacing = firstBar?.Spacing ?? DefaultSpacing;
    int slotCount = Math.Max(slots.Count, 1);
    double minBand = Math.Max(MinBarSize * Math.Max(1, slotCount + (slotCount - 1) * spacing + gap), categoryLabelsVisible ? (categoryLabelLine + 2) / categoryStep : 0);

    // Room after the bars for labels outside their ends. As with columns, none is kept for those of negative bars, which
    // have the room the value axis leaves below zero
    double labelsRight = 0;
    foreach (SeriesInfo info in bars) {
      PixataChartSeriesLabels? labels = info.Series.Labels;
      if (labels?.Visible != true || (labels.Position ?? PixataChartSeriesLabelsPosition.OutsideEnd) != PixataChartSeriesLabelsPosition.OutsideEnd) {
        continue;
      }
      for (int c = 0; c < count; c++) {
        if (info.Values[c] is not double value || info.Stacked[c] is not { } stacked) {
          continue;
        }
        PixataChartScenePoint context = BarPoint(info, c, value, stacked, categories[c], categoryTexts[c], 0, 0, 0, 0);
        double labelWidth = measurer.Width(PixataChartFormatter.Lines(LabelText(labels, context)), Label, labels.Font) + LabelGap;
        if (stacked.To >= stacked.From) {
          labelsRight = Math.Max(labelsRight, labelWidth);
        }
      }
    }

    // The value axes, along the bottom and top. Their labels sit centred under their ticks, so the scale needs room
    // between the ticks for the widest of them, and the last one hangs over the end of the plot area
    double[] axisHeights = axes.Select(a => ValueAxisHeight(a, measurer)).ToArray();
    double top = Padding + Enumerable.Range(0, axes.Count).Where(i => onTop[i]).Sum(i => axisHeights[i]);
    double bottomSpace = Padding + Enumerable.Range(0, axes.Count).Where(i => !onTop[i]).Sum(i => axisHeights[i]);
    double left = Padding + categoryTitleWidth + categoryAxisWidth;
    double right = Padding + labelsRight;
    double plotWidth = 0;
    double[] minSpacing = axes.Select(a => measurer.FontSize(AxisLabel, a.Labels?.Font) * 4).ToArray();
    PixataChartValueScale[] scales = [];
    string[][] tickTexts = new string[axes.Count][];
    for (int pass = 0; pass < 4; pass++) {
      plotWidth = Math.Max(10, width - left - right);
      scales = new PixataChartValueScale[axes.Count];
      bool settled = true;
      double overhangLeft = 0;
      double overhangRight = 0;
      for (int i = 0; i < axes.Count; i++) {
        PixataChartValueAxis axis = axes[i];
        scales[i] = PixataChartValueScale.Create(data.DataMin[i], data.DataMax[i], axis.Min, axis.Max, axis.MajorUnit, axis.MinorUnit, axis.NarrowRange == true, data.Percent[i], plotWidth, minSpacing[i]);
        string? format = axis.Labels?.Format ?? (data.Percent[i] ? "P0" : null);
        tickTexts[i] = scales[i].MajorTicks().Select(v => AxisLabelText(axis.Labels, v, null, format)).ToArray();
        if (!axis.IsVisible || !(axis.Labels?.Visible ?? true) || tickTexts[i].Length == 0) {
          continue;
        }
        double[] widths = tickTexts[i].Select(t => measurer.Width(t, AxisLabel, axis.Labels?.Font)).ToArray();
        double needed = widths.Max() + measurer.FontSize(AxisLabel, axis.Labels?.Font) * 1.5;
        if (axis.MajorUnit is null && needed > minSpacing[i] + .5) {
          minSpacing[i] = needed;
          settled = false;
        }
        overhangLeft = Math.Max(overhangLeft, widths[0] / 2);
        overhangRight = Math.Max(overhangRight, widths[^1] / 2);
      }
      double newLeft = Math.Max(left, Padding + overhangLeft);
      double newRight = Math.Max(Padding + labelsRight, Padding + overhangRight);
      if (Math.Abs(newLeft - left) > .5 || Math.Abs(newRight - right) > .5) {
        left = newLeft;
        right = newRight;
        settled = false;
      }
      if (settled) {
        break;
      }
    }

    // The plot area takes the height it is given, or more when the categories need it, in which case the chart grows
    double available = Math.Max(10, scene.Height - top - bottomSpace);
    double plotHeight = Math.Max(available, count * minBand);
    scene.Height += plotHeight - available;
    PixataChartBox plot = new(left, top, plotWidth, plotHeight);
    scene.Plot = plot;
    PixataChartCategoryScale categoryScale = new(count, plot.Y, plot.Height, false);
    scene.CategoryScale = categoryScale;
    foreach (PixataChartValueScale scale in scales) {
      // A value scale runs up the screen from its start, so a negative length runs it rightwards
      scale.Start = plot.X;
      scale.Length = -plot.Width;
    }

    // Grid lines go underneath everything else
    for (int i = 0; i < axes.Count; i++) {
      AddValueGridLines(scene, axes[i], scales[i], plot, i == 0, true);
    }
    if (count > 0) {
      AddCategoryGridLines(scene, categoryAxis, categoryScale, plot, true);
    }

    // The value axes, working outwards from the plot area above and below it
    double topEdge = plot.Y;
    double bottomEdge = plot.Bottom;
    for (int i = 0; i < axes.Count; i++) {
      PixataChartValueAxis axis = axes[i];
      if (!axis.IsVisible) {
        continue;
      }
      bool isTop = onTop[i];
      double y = isTop ? topEdge : bottomEdge;
      double direction = isTop ? -1 : 1;
      string? colour = axis.Color;
      scene.AxisLines.Add(new(plot.X, y, plot.Right, y, "pixata-blazor-chart-axis-line", colour));
      IReadOnlyList<double> ticks = scales[i].MajorTicks();
      bool labelsVisible = axis.Labels?.Visible ?? true;
      int step = axis.Labels?.Step is double ast && ast >= 1 ? (int)ast : 1;
      int skip = axis.Labels?.Skip is double ask && ask >= 0 ? (int)ask : 0;
      double? rotation = PixataChartLabelFitter.Angle(axis.Labels?.Rotation?.Angle);
      string? labelFont = axis.Labels?.Font;
      double fontSize = measurer.FontSize(AxisLabel, labelFont);
      double lineHeight = measurer.LineHeight(AxisLabel, labelFont);
      for (int t = 0; t < ticks.Count; t++) {
        double x = scales[i].ToPixel(ticks[t]);
        scene.AxisLines.Add(new(x, y, x, y + direction * TickLength, "pixata-blazor-chart-axis-line pixata-blazor-chart-tick", colour));
        if (labelsVisible && PixataChartLabelFitter.Shows(t, step, skip)) {
          double labelY = isTop ? y - TickLength - LabelGap - fontSize * .15 : y + TickLength + LabelGap + fontSize * .85;
          scene.AxisTexts.Add(new([tickTexts[i][t]], x, labelY, "middle", AxisLabel, lineHeight, rotation ?? 0, axis.Labels?.Color ?? colour, labelFont, FontSize: fontSize));
        }
      }
      PixataChartAxisTitleBase? title = axis.Title;
      if ((title?.Visible ?? true) && !string.IsNullOrEmpty(title?.Text)) {
        double size = measurer.FontSize(AxisTitle, title.Font);
        double titleY = isTop ? y - axisHeights[i] + size * .85 : y + axisHeights[i] - size * .25;
        scene.AxisTexts.Add(new([title.Text], plot.X + plot.Width / 2, titleY, "middle", AxisTitle, measurer.LineHeight(AxisTitle, title.Font), 0, title.Color ?? colour, title.Font, FontSize: size));
      }
      if (isTop) {
        topEdge -= axisHeights[i];
      } else {
        bottomEdge += axisHeights[i];
      }
    }

    // The category axis
    if (categoryAxisVisible) {
      string? colour = categoryAxis?.Color;
      scene.AxisLines.Add(new(plot.X, plot.Y, plot.X, plot.Bottom, "pixata-blazor-chart-axis-line", colour));
      if (count > 0) {
        foreach (double y in CategoryLinePositions(categoryScale, true)) {
          scene.AxisLines.Add(new(plot.X, y, plot.X - TickLength, y, "pixata-blazor-chart-axis-line pixata-blazor-chart-tick", colour));
        }
      }
      if (categoryLabelsVisible) {
        double labelX = plot.X - TickLength - LabelGap;
        string? labelColour = categoryLabels?.Color ?? colour;
        for (int i = 0; i < count; i++) {
          if (!PixataChartLabelFitter.Shows(i, categoryStep, categorySkip) || categoryTexts[i] == "") {
            continue;
          }
          string text = PixataChartTextMeasurer.Truncate(categoryTexts[i], categoryWidths[i], maxCategoryWidth + .5);
          string? full = text == categoryTexts[i] ? null : categoryTexts[i];
          scene.AxisTexts.Add(new([text], labelX, categoryScale.Centre(i) + categoryLabelSize * .35, "end", AxisLabel, categoryLabelLine, 0, labelColour, categoryFont, FontSize: categoryLabelSize, Title: full));
        }
      }
      if (categoryTitleVisible) {
        double size = measurer.FontSize(AxisTitle, categoryTitle!.Font);
        double titleX = plot.X - categoryAxisWidth - categoryTitleWidth + size * .85;
        scene.AxisTexts.Add(new([categoryTitle.Text!], titleX, plot.Y + plot.Height / 2, "middle", AxisTitle, measurer.LineHeight(AxisTitle, categoryTitle.Font), -90, categoryTitle.Color ?? colour, categoryTitle.Font, FontSize: size));
      }
    }

    // The bars, each series in its slot down its category
    foreach (SeriesInfo info in bars) {
      (double offset, double thickness) = PixataChartCategoryScale.ColumnSlot(categoryScale.BandWidth, slots.Count, slots.IndexOf(SlotKey(info)), gap, spacing);
      PixataChartValueScale scale = scales[info.AxisIndex];
      for (int c = 0; c < count; c++) {
        if (info.Values[c] is not double value || info.Stacked[c] is not { } stacked) {
          continue;
        }
        double xFrom = scale.ToPixel(stacked.From);
        double xTo = scale.ToPixel(stacked.To);
        double y = categoryScale.BandStart(c) + offset;
        bool positive = stacked.To >= stacked.From;
        PixataChartScenePoint point = BarPoint(info, c, value, stacked, categories[c], categoryTexts[c], Math.Min(xFrom, xTo), y, Math.Abs(xTo - xFrom), thickness);
        scene.Points.Add(point);
        AddBarLabel(scene, info, point, positive, measurer);
      }
    }
    scene.Points.Sort((a, b) => a.SeriesIndex != b.SeriesIndex ? a.SeriesIndex.CompareTo(b.SeriesIndex) : a.PointIndex.CompareTo(b.PointIndex));

    // The legend lists every bar series with a name, including hidden ones (greyed out) so they can be shown again
    foreach (SeriesInfo info in data.All) {
      PixataChartSeries series = info.Series;
      if (!(series.VisibleInLegend ?? true) || string.IsNullOrEmpty(series.Name)) {
        continue;
      }
      (string colour, _) = PointColour(series, info.Index, null);
      scene.LegendEntries.Add(new(info.Index, null, series.Name, colour, series.SeriesType, series.IsVisible, null));
    }
  }

  /// <summary>
  /// A bar: its rectangle (left, top, length and thickness), and what it shows and reports
  /// </summary>
  private static PixataChartScenePoint BarPoint(SeriesInfo info, int category, double value, PixataChartStackedValue stacked, object? categoryValue, string categoryText, double x, double y, double length, double thickness) {
    (string colour, string reported) = PointColour(info.Series, info.Index, info.Items[category]);
    bool positive = stacked.To >= stacked.From;
    return new() {
      Series = info.Series,
      SeriesIndex = info.Index,
      PointIndex = category,
      Kind = PixataChartPointKind.Bar,
      Category = categoryValue,
      Value = value,
      DataItem = info.Items[category],
      Percentage = info.Percent ? stacked.Percentage : 0,
      Total = info.Percent ? stacked.Total : 0,
      Colour = colour,
      ReportedColour = reported,
      X = x,
      Y = y,
      Width = length,
      Height = thickness,
      Opacity = info.Series.Opacity,
      AnchorX = positive ? x + length : x,
      AnchorY = y + thickness / 2,
      Description = Describe(info.Series, categoryText, value, info.Series.Labels?.Format)
    };
  }

  /// <summary>
  /// The height a value axis along the bottom (or top) of a bar chart takes, with its ticks, labels and title
  /// </summary>
  private static double ValueAxisHeight(PixataChartValueAxis axis, PixataChartTextMeasurer measurer) {
    if (!axis.IsVisible) {
      return 0;
    }
    double height = TickLength;
    if (axis.Labels?.Visible ?? true) {
      height += LabelGap + measurer.LineHeight(AxisLabel, axis.Labels?.Font);
    }
    if ((axis.Title?.Visible ?? true) && !string.IsNullOrEmpty(axis.Title?.Text)) {
      height += TitleGap + measurer.LineHeight(AxisTitle, axis.Title.Font);
    }
    return Math.Ceiling(height);
  }

  /// <summary>
  /// A bar's data label. The ends of a bar are its left and right, so a positive bar's end is on the right and a negative
  /// one's on the left
  /// </summary>
  private static void AddBarLabel(PixataChartScene scene, SeriesInfo info, PixataChartScenePoint point, bool positive, PixataChartTextMeasurer measurer) {
    PixataChartSeriesLabels? labels = info.Series.Labels;
    if (labels?.Visible != true) {
      return;
    }
    string[] lines = PixataChartFormatter.Lines(LabelText(labels, point));
    double size = measurer.FontSize(Label, labels.Font);
    double line = measurer.LineHeight(Label, labels.Font);
    double extra = (lines.Length - 1) * line;
    double left = point.X;
    double right = point.X + point.Width;
    double y = point.Y + point.Height / 2 + size * .35 - extra / 2;
    (double x, string anchor) = (labels.Position ?? PixataChartSeriesLabelsPosition.OutsideEnd) switch {
      PixataChartSeriesLabelsPosition.InsideEnd => positive ? (right - LabelGap, "end") : (left + LabelGap, "start"),
      PixataChartSeriesLabelsPosition.Center => ((left + right) / 2, "middle"),
      PixataChartSeriesLabelsPosition.InsideBase => positive ? (left + LabelGap, "start") : (right - LabelGap, "end"),
      _ => positive ? (right + LabelGap, "start") : (left - LabelGap, "end")
    };
    scene.Labels.Add(new(lines, x, y, anchor, Label, line, 0, labels.Color, labels.Font, labels.Background, measurer.Width(lines, Label, labels.Font), size));
  }

  #endregion
}
