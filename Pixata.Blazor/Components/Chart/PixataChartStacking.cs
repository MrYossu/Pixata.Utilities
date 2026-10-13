using System;
using System.Collections.Generic;

namespace Pixata.Blazor.Components;

/// <summary>
/// Where a stacked value starts and ends, and its share of its category's total
/// </summary>
internal readonly record struct PixataChartStackedValue(double From, double To, double Percentage, double Total);

internal static class PixataChartStacking {
  /// <summary>
  /// Stacks the values of several series. Positive values stack upwards from zero and negative ones downwards, each in series
  /// order. When <paramref name="percent"/> is true, each category is scaled so that its values (ignoring sign) add up to 1.
  /// Missing (null) values take no room, and come back as null
  /// </summary>
  /// <param name="values">The values of each series, indexed by category</param>
  public static PixataChartStackedValue?[][] Stack(IReadOnlyList<double?[]> values, bool percent) {
    int categories = 0;
    foreach (double?[] series in values) {
      categories = Math.Max(categories, series.Length);
    }
    PixataChartStackedValue?[][] result = new PixataChartStackedValue?[values.Count][];
    for (int s = 0; s < values.Count; s++) {
      result[s] = new PixataChartStackedValue?[categories];
    }
    for (int c = 0; c < categories; c++) {
      double total = 0;
      foreach (double?[] series in values) {
        if (c < series.Length && series[c] is double v) {
          total += Math.Abs(v);
        }
      }
      double positive = 0;
      double negative = 0;
      for (int s = 0; s < values.Count; s++) {
        if (c >= values[s].Length || values[s][c] is not double value) {
          continue;
        }
        double share = total == 0 ? 0 : Math.Abs(value) / total;
        double scaled = percent ? total == 0 ? 0 : value / total : value;
        if (scaled >= 0) {
          result[s][c] = new(positive, positive + scaled, share, total);
          positive += scaled;
        } else {
          result[s][c] = new(negative, negative + scaled, share, total);
          negative += scaled;
        }
      }
    }
    return result;
  }
}
