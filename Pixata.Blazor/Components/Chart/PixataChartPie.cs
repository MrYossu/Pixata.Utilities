using System;
using System.Collections.Generic;
using System.Linq;
using static Pixata.Blazor.Components.PixataChartFormatter;

namespace Pixata.Blazor.Components;

/// <summary>
/// A pie or donut segment. Angles are in degrees, measured anticlockwise from three o'clock; segments go clockwise, so
/// <see cref="EndAngle"/> is less than <see cref="StartAngle"/>
/// </summary>
internal readonly record struct PixataChartPieSlice(int Index, double StartAngle, double EndAngle, double Percentage, double Total) {
  public double MidAngle => (StartAngle + EndAngle) / 2;
  public double Sweep => StartAngle - EndAngle;
}

/// <summary>
/// An outside pie label, which is moved up or down so it doesn't overlap its neighbours
/// </summary>
internal sealed class PixataChartPieLabel {
  public int Index { get; init; }
  public bool Right { get; init; }
  public double Y { get; set; }
}

internal static class PixataChartPie {
  /// <summary>
  /// Divides the circle between the values that are positive and not hidden, starting at <paramref name="startAngle"/> and
  /// going clockwise
  /// </summary>
  public static List<PixataChartPieSlice> Slices(IReadOnlyList<double?> values, ICollection<int> hidden, double startAngle) {
    double total = 0;
    for (int i = 0; i < values.Count; i++) {
      if (values[i] is > 0 && !hidden.Contains(i)) {
        total += values[i]!.Value;
      }
    }
    List<PixataChartPieSlice> slices = [];
    if (total <= 0) {
      return slices;
    }
    double angle = startAngle;
    for (int i = 0; i < values.Count; i++) {
      if (values[i] is not > 0 || hidden.Contains(i)) {
        continue;
      }
      double share = values[i]!.Value / total;
      double end = angle - share * 360;
      slices.Add(new(i, angle, end, share, total));
      angle = end;
    }
    return slices;
  }

  /// <summary>
  /// The point at <paramref name="radius"/> from the centre, at <paramref name="degrees"/> anticlockwise from three o'clock
  /// </summary>
  public static (double X, double Y) PointAt(double cx, double cy, double radius, double degrees) {
    double radians = degrees * Math.PI / 180;
    return (cx + radius * Math.Cos(radians), cy - radius * Math.Sin(radians));
  }

  /// <summary>
  /// The SVG path of a segment
  /// </summary>
  public static string SegmentPath(double cx, double cy, double radius, double startAngle, double endAngle) {
    double sweep = startAngle - endAngle;
    if (sweep >= 359.99) {
      // A whole circle can't be drawn as one arc, so draw two halves
      return $"M {Svg(cx - radius)} {Svg(cy)} A {Svg(radius)} {Svg(radius)} 0 1 1 {Svg(cx + radius)} {Svg(cy)} A {Svg(radius)} {Svg(radius)} 0 1 1 {Svg(cx - radius)} {Svg(cy)} Z";
    }
    (double x1, double y1) = PointAt(cx, cy, radius, startAngle);
    (double x2, double y2) = PointAt(cx, cy, radius, endAngle);
    int large = sweep > 180 ? 1 : 0;
    return $"M {Svg(cx)} {Svg(cy)} L {Svg(x1)} {Svg(y1)} A {Svg(radius)} {Svg(radius)} 0 {large} 1 {Svg(x2)} {Svg(y2)} Z";
  }

  /// <summary>
  /// The SVG path of a donut segment, between <paramref name="inner"/> and <paramref name="outer"/>. With no inner radius it
  /// is a pie segment
  /// </summary>
  public static string RingSegmentPath(double cx, double cy, double inner, double outer, double startAngle, double endAngle) {
    if (inner <= 0) {
      return SegmentPath(cx, cy, outer, startAngle, endAngle);
    }
    double sweep = startAngle - endAngle;
    if (sweep >= 359.99) {
      // The outer circle goes clockwise and the inner one anticlockwise, so the inner one is a hole under the nonzero fill rule
      return $"M {Svg(cx - outer)} {Svg(cy)} A {Svg(outer)} {Svg(outer)} 0 1 1 {Svg(cx + outer)} {Svg(cy)} A {Svg(outer)} {Svg(outer)} 0 1 1 {Svg(cx - outer)} {Svg(cy)} Z "
        + $"M {Svg(cx - inner)} {Svg(cy)} A {Svg(inner)} {Svg(inner)} 0 1 0 {Svg(cx + inner)} {Svg(cy)} A {Svg(inner)} {Svg(inner)} 0 1 0 {Svg(cx - inner)} {Svg(cy)} Z";
    }
    (double ox1, double oy1) = PointAt(cx, cy, outer, startAngle);
    (double ox2, double oy2) = PointAt(cx, cy, outer, endAngle);
    (double ix1, double iy1) = PointAt(cx, cy, inner, endAngle);
    (double ix2, double iy2) = PointAt(cx, cy, inner, startAngle);
    int large = sweep > 180 ? 1 : 0;
    return $"M {Svg(ox1)} {Svg(oy1)} A {Svg(outer)} {Svg(outer)} 0 {large} 1 {Svg(ox2)} {Svg(oy2)} L {Svg(ix1)} {Svg(iy1)} A {Svg(inner)} {Svg(inner)} 0 {large} 0 {Svg(ix2)} {Svg(iy2)} Z";
  }

  /// <summary>
  /// Moves the labels on each side of the pie apart so they don't overlap, keeping them between <paramref name="top"/> and
  /// <paramref name="bottom"/> (which are the centres of the highest and lowest allowed labels)
  /// </summary>
  public static void Spread(IEnumerable<PixataChartPieLabel> labels, double lineHeight, double top, double bottom) {
    foreach (bool right in new[] { true, false }) {
      List<PixataChartPieLabel> side = labels.Where(l => l.Right == right).OrderBy(l => l.Y).ToList();
      if (side.Count == 0) {
        continue;
      }
      for (int i = 0; i < side.Count; i++) {
        double lowest = i == 0 ? top : side[i - 1].Y + lineHeight;
        side[i].Y = Math.Max(side[i].Y, lowest);
      }
      for (int i = side.Count - 1; i >= 0; i--) {
        double highest = i == side.Count - 1 ? bottom : side[i + 1].Y - lineHeight;
        side[i].Y = Math.Min(side[i].Y, highest);
      }
    }
  }
}
