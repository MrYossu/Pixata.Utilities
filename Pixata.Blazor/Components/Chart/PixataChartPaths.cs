using System;
using System.Collections.Generic;
using System.Text;
using static Pixata.Blazor.Components.PixataChartFormatter;

namespace Pixata.Blazor.Components;

/// <summary>
/// Builds the SVG paths of line series. A null point is a gap, which breaks the line
/// </summary>
internal static class PixataChartPaths {
  /// <summary>
  /// Straight lines from point to point
  /// </summary>
  public static string Linear(IReadOnlyList<(double X, double Y)?> points) {
    StringBuilder path = new();
    foreach (List<(double X, double Y)> run in Runs(points)) {
      for (int i = 0; i < run.Count; i++) {
        path.Append(i == 0 ? "M " : " L ").Append(Svg(run[i].X)).Append(' ').Append(Svg(run[i].Y));
      }
      path.Append(' ');
    }
    return path.ToString().Trim();
  }

  /// <summary>
  /// A smooth curve through the points, which doesn't overshoot them (a monotone cubic spline)
  /// </summary>
  public static string Smooth(IReadOnlyList<(double X, double Y)?> points) {
    StringBuilder path = new();
    foreach (List<(double X, double Y)> run in Runs(points)) {
      path.Append("M ").Append(Svg(run[0].X)).Append(' ').Append(Svg(run[0].Y));
      if (run.Count == 2) {
        path.Append(" L ").Append(Svg(run[1].X)).Append(' ').Append(Svg(run[1].Y));
      } else if (run.Count > 2) {
        double[] tangents = Tangents(run);
        for (int i = 0; i < run.Count - 1; i++) {
          double dx = (run[i + 1].X - run[i].X) / 3;
          path.Append(" C ")
            .Append(Svg(run[i].X + dx)).Append(' ').Append(Svg(run[i].Y + tangents[i] * dx)).Append(", ")
            .Append(Svg(run[i + 1].X - dx)).Append(' ').Append(Svg(run[i + 1].Y - tangents[i + 1] * dx)).Append(", ")
            .Append(Svg(run[i + 1].X)).Append(' ').Append(Svg(run[i + 1].Y));
        }
      }
      path.Append(' ');
    }
    return path.ToString().Trim();
  }

  /// <summary>
  /// A horizontal line across each point's span, joined to the next by a vertical step
  /// </summary>
  /// <param name="spans">The horizontal extent of each point (its category band)</param>
  public static string Step(IReadOnlyList<(double X, double Y)?> points, IReadOnlyList<(double Start, double End)> spans) {
    StringBuilder path = new();
    bool drawing = false;
    for (int i = 0; i < points.Count; i++) {
      if (points[i] is not (double X, double Y) point) {
        drawing = false;
        continue;
      }
      (double start, double end) = spans[i];
      path.Append(drawing ? " V " : " M ");
      if (drawing) {
        path.Append(Svg(point.Y));
      } else {
        path.Append(Svg(start)).Append(' ').Append(Svg(point.Y));
      }
      path.Append(" H ").Append(Svg(end));
      drawing = true;
    }
    return path.ToString().Trim();
  }

  private static IEnumerable<List<(double X, double Y)>> Runs(IReadOnlyList<(double X, double Y)?> points) {
    List<(double X, double Y)> run = [];
    foreach ((double X, double Y)? point in points) {
      if (point is { } p) {
        run.Add(p);
      } else if (run.Count > 0) {
        yield return run;
        run = [];
      }
    }
    if (run.Count > 0) {
      yield return run;
    }
  }

  // Fritsch-Carlson tangents, which keep the curve monotone between points
  private static double[] Tangents(List<(double X, double Y)> run) {
    int n = run.Count;
    double[] secants = new double[n - 1];
    for (int i = 0; i < n - 1; i++) {
      double dx = run[i + 1].X - run[i].X;
      secants[i] = dx == 0 ? 0 : (run[i + 1].Y - run[i].Y) / dx;
    }
    double[] tangents = new double[n];
    tangents[0] = secants[0];
    tangents[n - 1] = secants[n - 2];
    for (int i = 1; i < n - 1; i++) {
      tangents[i] = secants[i - 1] * secants[i] <= 0 ? 0 : (secants[i - 1] + secants[i]) / 2;
    }
    for (int i = 0; i < n - 1; i++) {
      if (secants[i] == 0) {
        tangents[i] = 0;
        tangents[i + 1] = 0;
        continue;
      }
      double a = tangents[i] / secants[i];
      double b = tangents[i + 1] / secants[i];
      double sum = a * a + b * b;
      if (sum > 9) {
        double t = 3 / Math.Sqrt(sum);
        tangents[i] = t * a * secants[i];
        tangents[i + 1] = t * b * secants[i];
      }
    }
    return tangents;
  }
}
