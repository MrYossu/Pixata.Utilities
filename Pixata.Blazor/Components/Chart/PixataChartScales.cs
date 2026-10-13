using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// A value axis's range and divisions, and the mapping from values to pixels. The axis runs up the screen, from
/// <see cref="Start"/> (the bottom of the plot area) for <see cref="Length"/> pixels
/// </summary>
internal sealed class PixataChartValueScale {
  public double Min { get; private init; }
  public double Max { get; private init; }
  public double MajorUnit { get; private init; }
  public double MinorUnit { get; private init; }
  public double Start { get; set; }
  public double Length { get; set; }

  public double ToPixel(double value) =>
    Max <= Min ? Start : Start - (value - Min) / (Max - Min) * Length;

  /// <summary>
  /// The values of the major divisions, from <see cref="Min"/> to <see cref="Max"/>
  /// </summary>
  public IReadOnlyList<double> MajorTicks() =>
    Ticks(MajorUnit);

  /// <summary>
  /// The values of the minor divisions, leaving out those that fall on a major division
  /// </summary>
  public IReadOnlyList<double> MinorTicks() =>
    Ticks(MinorUnit).Where(v => !IsMultiple(v - Min, MajorUnit)).ToList();

  private List<double> Ticks(double unit) {
    List<double> ticks = [];
    if (unit <= 0 || Max <= Min) {
      return ticks;
    }
    int count = (int)Math.Floor((Max - Min) / unit + 1e-9);
    for (int i = 0; i <= Math.Min(count, 1000); i++) {
      ticks.Add(Clean(Min + i * unit, unit));
    }
    return ticks;
  }

  private static bool IsMultiple(double value, double unit) {
    double ratio = value / unit;
    return Math.Abs(ratio - Math.Round(ratio)) < 1e-6;
  }

  /// <summary>
  /// Rounds away the floating point noise in a tick value (0.30000000000000004 and so on)
  /// </summary>
  public static double Clean(double value, double unit) {
    int decimals = Math.Clamp(-(int)Math.Floor(Math.Log10(Math.Abs(unit))) + 6, 0, 15);
    return Math.Round(value, decimals);
  }

  /// <summary>
  /// A round interval that splits the range into roughly five to ten divisions, leaving a little headroom above the data
  /// </summary>
  public static double AutoMajorUnit(double min, double max) {
    double range = Math.Abs(max - min);
    if (range == 0) {
      range = Math.Abs(min) > 0 ? Math.Abs(min) : 1;
    }
    double scale = Math.Pow(10, Math.Floor(Math.Log10(range)));
    double relative = range / scale;
    double multiplier = relative < 1.904762 ? .2 : relative < 4.761904 ? .5 : relative < 9.523809 ? 1 : 2;
    return Clean(scale * multiplier, scale * multiplier);
  }

  /// <summary>
  /// The next larger round interval: 1, 2, 5, 10, 20, 50...
  /// </summary>
  public static double NextUnit(double unit) {
    double scale = Math.Pow(10, Math.Floor(Math.Log10(unit)));
    double mantissa = unit / scale;
    double next = mantissa < 1.5 ? 2 : mantissa < 3.5 ? 5 : 10;
    return Clean(next * scale, next * scale);
  }

  /// <summary>
  /// Works out the axis range and divisions
  /// </summary>
  /// <param name="dataMin">The lowest value plotted against the axis, or <see cref="double.NaN"/> if there's no data</param>
  /// <param name="dataMax">The highest value plotted against the axis, or <see cref="double.NaN"/> if there's no data</param>
  /// <param name="percent">The axis shows 100% stacked series, so runs from 0 to 1 (or -1 to 1)</param>
  /// <param name="length">The length of the axis in pixels</param>
  /// <param name="minSpacing">The fewest pixels between major divisions. When the automatic divisions are closer than this
  /// (on a phone, say) a larger interval is used</param>
  public static PixataChartValueScale Create(double dataMin, double dataMax, double? userMin, double? userMax, double? userMajorUnit, double? userMinorUnit, bool narrowRange, bool percent, double length, double minSpacing) {
    bool hasData = !double.IsNaN(dataMin) && !double.IsNaN(dataMax);
    if (!hasData) {
      dataMin = 0;
      dataMax = 1;
    }
    double low;
    double high;
    if (percent) {
      low = dataMin < 0 ? -1 : 0;
      high = dataMax > 0 || dataMin >= 0 ? 1 : 0;
    } else if (narrowRange) {
      low = dataMin;
      high = dataMax;
    } else {
      low = Math.Min(0, dataMin);
      high = Math.Max(0, dataMax);
    }
    low = userMin ?? low;
    high = userMax ?? high;
    if (high <= low) {
      if (userMax is not null && userMin is null) {
        low = high - 1;
      } else {
        high = low + (low == 0 ? 1 : Math.Abs(low));
      }
    }
    double unit = userMajorUnit is > 0 ? userMajorUnit.Value : AutoMajorUnit(low, high);
    (double min, double max) = Round(low, high, unit, userMin, userMax, percent || !hasData);
    if (userMajorUnit is null && length > 0 && minSpacing > 0) {
      for (int i = 0; i < 20 && (max - min) / unit > 1 && length / ((max - min) / unit) < minSpacing; i++) {
        unit = NextUnit(unit);
        (min, max) = Round(low, high, unit, userMin, userMax, percent || !hasData);
      }
    }
    double minor = userMinorUnit is > 0 ? userMinorUnit.Value : unit / 5;
    return new() {
      Min = min,
      Max = max,
      MajorUnit = unit,
      MinorUnit = minor
    };
  }

  private static (double Min, double Max) Round(double low, double high, double unit, double? userMin, double? userMax, bool exact) {
    if (exact) {
      return (userMin ?? low, userMax ?? high);
    }
    double headroom = (high - low) * .05;
    double min = userMin ?? (low < 0 ? Math.Floor((low - headroom) / unit + 1e-9) * unit : Math.Floor(low / unit + 1e-9) * unit);
    double max = userMax ?? (high > 0 ? Math.Ceiling((high + headroom) / unit - 1e-9) * unit : Math.Ceiling(high / unit - 1e-9) * unit);
    if (max <= min) {
      max = min + unit;
    }
    return (Clean(min, unit), Clean(max, unit));
  }
}

/// <summary>
/// The category axis: splits its length into equal bands, one per category. When <see cref="Justified"/>, the first and
/// last categories sit on the ends of the axis instead
/// </summary>
internal sealed class PixataChartCategoryScale(int count, double start, double length, bool justified) {
  public int Count => count;
  public double Start => start;
  public double Length => length;
  public bool Justified => justified && count > 1;

  /// <summary>
  /// The distance from one category to the next
  /// </summary>
  public double BandWidth =>
    count <= 0 ? length : Justified ? length / (count - 1) : length / count;

  public double Centre(int index) =>
    Justified ? start + index * BandWidth : start + (index + .5) * BandWidth;

  public double BandStart(int index) =>
    Centre(index) - BandWidth / 2;

  /// <summary>
  /// The category under the x coordinate, or -1 if there isn't one
  /// </summary>
  public int IndexAt(double x) {
    if (count <= 0) {
      return -1;
    }
    int index = (int)Math.Floor((x - start + (Justified ? BandWidth / 2 : 0)) / BandWidth);
    return index >= 0 && index < count ? index : -1;
  }

  /// <summary>
  /// Where a column goes within its category band, given the number of columns side by side, the gap between categories and
  /// the spacing between columns (both as a multiple of the column width)
  /// </summary>
  public static (double Offset, double Width) ColumnSlot(double bandWidth, int slots, int slot, double gap, double spacing) {
    slots = Math.Max(slots, 1);
    double width = bandWidth / (slots + (slots - 1) * spacing + gap);
    return (gap * width / 2 + slot * width * (1 + spacing), width);
  }
}

/// <summary>
/// How the category labels fit: their angle, how many to step over so they don't collide, the height they need, and the
/// width beyond which they are shortened
/// </summary>
internal readonly record struct PixataChartLabelFit(double Angle, int Step, double AreaHeight, double MaxWidth);

internal static class PixataChartLabelFitter {
  /// <summary>
  /// Decides how to show the category labels in the space available. When they don't fit side by side they are turned to
  /// -45 degrees (unless <paramref name="angle"/> says otherwise), and if they still collide only every nth one is shown
  /// </summary>
  /// <param name="widths">The width of each label</param>
  /// <param name="lineHeight">The height of a line of label text</param>
  /// <param name="bandWidth">The distance between categories</param>
  /// <param name="angle">The angle the labels must be at, or null to choose one</param>
  /// <param name="userStep">The step the labels must use, or null to choose one</param>
  /// <param name="maxAreaHeight">The most height the labels may take. Longer rotated labels are shortened</param>
  public static PixataChartLabelFit Fit(IReadOnlyList<double> widths, double lineHeight, double bandWidth, double? angle, int? userStep, double maxAreaHeight) {
    double widest = widths.Count == 0 ? 0 : widths.Max();
    double padding = Math.Max(4, lineHeight * .4);
    double chosen = angle ?? (widest + padding <= bandWidth ? 0 : -45);
    double radians = Math.Abs(chosen) * Math.PI / 180;
    double sin = Math.Sin(radians);
    double cos = Math.Cos(radians);
    double needed = chosen == 0 ? widest + padding : (lineHeight + 2) / Math.Max(sin, .2);
    if (chosen != 0 && cos > .01) {
      // Steep labels never need more room than flat ones would
      needed = Math.Min(needed, widest + padding);
    }
    int step = userStep is > 0 ? userStep.Value : bandWidth <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(needed / bandWidth - 1e-9));
    double maxWidth = widest;
    if (chosen != 0 && sin > .01) {
      double allowed = Math.Max(lineHeight * 2, (maxAreaHeight - lineHeight * cos) / sin);
      maxWidth = Math.Min(widest, allowed);
    }
    double height = chosen == 0 ? lineHeight : maxWidth * sin + lineHeight * cos;
    return new(chosen, step, Math.Ceiling(height), maxWidth);
  }

  /// <summary>
  /// Whether label <paramref name="index"/> is shown, given the step and the number skipped at the start
  /// </summary>
  public static bool Shows(int index, int step, int skip) =>
    index >= skip && (index - skip) % Math.Max(step, 1) == 0;

  /// <summary>
  /// The angle from a rotation component's <c>Angle</c> (a number or "auto"), or null for automatic
  /// </summary>
  public static double? Angle(object? value) =>
    value switch {
      null => null,
      string text when text.Equals("auto", StringComparison.OrdinalIgnoreCase) => null,
      _ => PixataChartFormatter.ToDouble(value)
    };
}
