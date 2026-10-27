using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// A grid of cells, each coloured between a low and a high colour by its value, with a label for each row and column. Useful
/// for showing when something is busy, with the days of the week as the rows and the hours of the day as the columns
/// </summary>
public partial class PixataHeatmap {
  // A cell this far or more up the scale gets the strong text colour, so its text shows on the darker background
  private const double StrongLevel = 0.6;

  private readonly string _titleId = $"pixata-blazor-heatmap-title-{Guid.NewGuid():N}";
  private double _min;
  private double _max;
  private bool _hasValues;

  /// <summary>
  /// The label of each row, from the top
  /// </summary>
  [Parameter, EditorRequired]
  public IReadOnlyList<string> RowLabels { get; set; } = [];

  /// <summary>
  /// The label of each column, from the left
  /// </summary>
  [Parameter, EditorRequired]
  public IReadOnlyList<string> ColumnLabels { get; set; } = [];

  /// <summary>
  /// The value of each cell, indexed by row then column, so it must be <c>RowLabels.Count</c> by <c>ColumnLabels.Count</c>.
  /// A null value is a cell with no data, which isn't coloured
  /// </summary>
  [Parameter, EditorRequired]
  public double?[,] Values { get; set; } = new double?[0, 0];

  /// <summary>
  /// The value shown in the low colour. Values below it are shown in the low colour too. By default it's the smallest value
  /// </summary>
  [Parameter]
  public double? Min { get; set; }

  /// <summary>
  /// The value shown in the high colour. Values above it are shown in the high colour too. By default it's the largest value
  /// </summary>
  [Parameter]
  public double? Max { get; set; }

  /// <summary>
  /// Whether the values are shown in the cells. If not, they are only in the tooltips, and in text that screen readers read
  /// </summary>
  [Parameter]
  public bool ShowValues { get; set; }

  /// <summary>
  /// The composite format for the values, in the cells, tooltips and legend. <c>{0:N0}</c> by default
  /// </summary>
  [Parameter]
  public string Format { get; set; } = "{0:N0}";

  /// <summary>
  /// The text for a cell with no value, in its tooltip and the text that screen readers read
  /// </summary>
  [Parameter]
  public string EmptyText { get; set; } = "No data";

  /// <summary>
  /// The tooltip for each cell. By default it's the row and column labels and the value, eg "Monday, 09:00: 12"
  /// </summary>
  [Parameter]
  public Func<PixataHeatmapCell, string>? CellTooltip { get; set; }

  /// <summary>
  /// Whether to show a bar under the grid, with the low and high values at its ends. True by default
  /// </summary>
  [Parameter]
  public bool ShowLegend { get; set; } = true;

  /// <summary>
  /// The title, shown above the grid. It also names the table for screen readers
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  /// <summary>
  /// Names the table for screen readers when there's no <see cref="Title"/>
  /// </summary>
  [Parameter]
  public string? AriaLabel { get; set; }

  /// <summary>
  /// The text in the top left corner, above the row labels
  /// </summary>
  [Parameter]
  public string? CornerText { get; set; }

  [Parameter]
  public string? Id { get; set; }

  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public string? Style { get; set; }

  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  protected override void OnParametersSet() {
    if (Values.GetLength(0) != RowLabels.Count || Values.GetLength(1) != ColumnLabels.Count) {
      throw new InvalidOperationException($"{nameof(PixataHeatmap)}'s {nameof(Values)} is {Values.GetLength(0)} by {Values.GetLength(1)}, but there are {RowLabels.Count} row labels and {ColumnLabels.Count} column labels");
    }
    double min = double.MaxValue;
    double max = double.MinValue;
    _hasValues = false;
    foreach (double? value in Values) {
      if (value is { } v) {
        min = Math.Min(min, v);
        max = Math.Max(max, v);
        _hasValues = true;
      }
    }
    _min = Min ?? (_hasValues ? min : 0);
    _max = Max ?? (_hasValues ? max : 0);
  }

  private PixataHeatmapCell GetCell(int row, int column) {
    double? value = Values[row, column];
    return new(row, column, RowLabels[row], ColumnLabels[column], value, Level(value));
  }

  // How far up the scale (0 to 1) the value is. If every value is the same, they are all shown in the low colour
  private double Level(double? value) =>
    value is not { } v || _max <= _min
      ? 0
      : Math.Clamp((v - _min) / (_max - _min), 0, 1);

  private static string CellClass(PixataHeatmapCell cell) =>
    PixataCss.Classes("pixata-blazor-heatmap-cell",
      cell.Value is null ? "pixata-blazor-heatmap-cell-empty" : null,
      cell.Value is not null && cell.Level >= StrongLevel ? "pixata-blazor-heatmap-cell-strong" : null);

  private static string? CellStyle(PixataHeatmapCell cell) =>
    cell.Value is null
      ? null
      : $"--pixata-blazor-heatmap-level: {(cell.Level * 100).ToString("0.##", CultureInfo.InvariantCulture)}%";

  private string CellText(PixataHeatmapCell cell) =>
    cell.Value is { } value ? FormatValue(value) : EmptyText;

  private string Tooltip(PixataHeatmapCell cell) =>
    CellTooltip?.Invoke(cell) ?? $"{cell.RowLabel}, {cell.ColumnLabel}: {CellText(cell)}";

  private string FormatValue(double value) =>
    string.Format(CultureInfo.CurrentCulture, Format, value);
}

/// <summary>
/// A cell in a <see cref="PixataHeatmap"/>, passed to its <see cref="PixataHeatmap.CellTooltip"/>
/// </summary>
/// <param name="Row">The row's index, from 0</param>
/// <param name="Column">The column's index, from 0</param>
/// <param name="RowLabel">The row's label</param>
/// <param name="ColumnLabel">The column's label</param>
/// <param name="Value">The cell's value, or null if it has none</param>
/// <param name="Level">How far up the scale the value is, from 0 (the low colour) to 1 (the high colour)</param>
public record PixataHeatmapCell(int Row, int Column, string RowLabel, string ColumnLabel, double? Value, double Level);
