using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Pixata.Blazor.Components.Grid;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Grid.Excel;

/// <summary>
/// Settings for exporting a <see cref="PixataGrid{TItem}"/> to Excel, with the <c>ExcelExport</c> toolbar command or
/// <c>SaveAsExcelFile()</c>. Goes in the grid's <c>GridExport</c>
/// </summary>
public class PixataGridExcelExport<TItem> : PixataGridExportBase<TItem> {
  /// <summary>
  /// The name of the worksheet. Defaults to "Sheet1"
  /// </summary>
  [Parameter]
  public string SheetName { get; set; } = "Sheet1";

  /// <summary>
  /// Whether to write totals under the columns that have aggregates in the grid's <c>GridAggregates</c>, as Excel formulas
  /// (such as <c>=SUM(C2:C40)</c>), so they stay right if rows are changed or deleted. A column with more than one aggregate
  /// gets a row for each. With <see cref="GroupRows"/>, this includes each group's totals. Defaults to true
  /// </summary>
  [Parameter]
  public bool Totals { get; set; } = true;

  /// <summary>
  /// Whether, when the grid is grouped, to write a heading row before each group and its totals after it, and to group its
  /// rows in Excel's outline, so they can be collapsed. Defaults to false, which leaves the sheet a plain table that can be
  /// sorted and filtered in Excel
  /// </summary>
  [Parameter]
  public bool GroupRows { get; set; }

  /// <summary>
  /// The text shown before a total, such as "Average: ". It's part of the cell's number format, so the cell is still a
  /// number. Defaults to "Count: ", "Average: ", "Min: " and "Max: ", with nothing before a sum. Return "" for no text
  /// </summary>
  [Parameter]
  public Func<GridAggregateType, string>? AggregateLabel { get; set; }

  public override string Command =>
    "ExcelExport";

  public override string FileExtension =>
    ".xlsx";

  public override string ContentType =>
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  public override Task<MemoryStream> Write(List<PixataGridExportColumn> columns, List<TItem> data, CultureInfo culture) =>
    Write(new PixataGridBeforeExportEventArgs<TItem> { Columns = columns, Data = data }, culture);

  // Numbers, dates and true/false values are stored as such, with the column's format turned into Excel's, so they can be
  // worked with in Excel. Everything else is stored as the text the grid shows. Totals are formulas, so they stay right if
  // the rows are changed. In a grouped sheet they're SUBTOTALs, which leave out the subtotals within their range
  public override Task<MemoryStream> Write(PixataGridBeforeExportEventArgs<TItem> export, CultureInfo culture) {
    List<PixataGridExportColumn> columns = export.Columns;
    using XLWorkbook workbook = new();
    IXLWorksheet sheet = workbook.AddWorksheet(SheetName);
    for (int c = 0; c < columns.Count; c++) {
      IXLCell header = sheet.Cell(1, c + 1);
      header.Value = columns[c].Title;
      if (PixelWidth(columns[c].Width) is double pixels) {
        // Excel measures widths in characters of its default font, which are about 7 pixels wide, plus some padding
        sheet.Column(c + 1).Width = Math.Max((pixels - 5) / 7, 1);
      }
    }
    SheetWriter writer = new(sheet, columns, export.Data, culture, Totals, AggregateLabel ?? DefaultLabel);
    bool grouped = GroupRows && export.Groups.Count > 0 && export.Data.Count > 0;
    int headings = 0;
    if (grouped) {
      headings = writer.Groups(export.Groups, export.Data, 0);
    } else {
      writer.Items(export.Data);
    }
    if (export.Data.Count > 0 && writer.FooterRows > 0) {
      int last = writer.Row - 1;
      sheet.Range(writer.Row, 1, writer.Row, Math.Max(columns.Count, 1)).Style.Border.TopBorder = XLBorderStyleValues.Thin;
      writer.Footers(2, last, grouped, headings);
    }
    for (int c = 0; c < columns.Count; c++) {
      if (PixelWidth(columns[c].Width) is null) {
        sheet.Column(c + 1).AdjustToContents();
      }
    }
    // The group headings go in after the columns are sized, so they don't make the first column as wide as they are. They
    // spill over into the empty cells beside them instead
    writer.WriteHeadings();
    MemoryStream stream = new();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return Task.FromResult(stream);
  }

  private static string DefaultLabel(GridAggregateType aggregate) =>
    aggregate switch {
      GridAggregateType.Count => "Count: ",
      GridAggregateType.Average => "Average: ",
      GridAggregateType.Min => "Min: ",
      GridAggregateType.Max => "Max: ",
      _ => ""
    };

  // Writes the rows, keeping track of where it's got to
  private sealed class SheetWriter(IXLWorksheet sheet, List<PixataGridExportColumn> columns, List<TItem> data, CultureInfo culture, bool totals, Func<GridAggregateType, string> label) {
    private readonly List<Func<TItem, object?>> _getters = [.. columns.Select(c => FieldAccessor.Getter<TItem>(c.Field))];
    private readonly List<(IXLCell Cell, string Text)> _headings = [];

    // The format for each column's totals: the column's own, or, for dates without one, the culture's short date
    private readonly List<string?> _formats = [.. columns.Select(c => ExcelFormats.For(c.DisplayFormat, culture)
      ?? (FieldAccessor.Getter<TItem>(c.Field) is Func<TItem, object?> getter && data.Select(getter).FirstOrDefault(v => v is not null) is DateTime or DateOnly or DateTimeOffset
        ? ExcelFormats.Date(culture.DateTimeFormat.ShortDatePattern)
        : null))];

    /// <summary>
    /// The row to write next
    /// </summary>
    public int Row { get; private set; } = 2;

    /// <summary>
    /// The number of rows of totals: one for each aggregate of the column with the most, or none if there are to be no totals
    /// </summary>
    public int FooterRows { get; } = !totals || columns.Count == 0 ? 0 : columns.Max(c => c.Aggregates.Count);

    public void Items(IEnumerable<TItem> items) {
      foreach (TItem item in items) {
        for (int c = 0; c < columns.Count; c++) {
          SetCell(sheet.Cell(Row, c + 1), _getters[c](item), columns[c], culture);
        }
        Row++;
      }
    }

    // Writes the groups at one level, each with its heading, its rows (or the groups within it) and its totals. The items are
    // in group order, so a group is a run of items with the same value. Returns the number of headings written, including
    // the ones within
    public int Groups(List<PixataGridExportColumn> groups, List<TItem> items, int level) {
      Func<TItem, object?> key = FieldAccessor.Getter<TItem>(groups[level].Field);
      int headings = 0;
      for (int start = 0; start < items.Count;) {
        object? value = key(items[start]);
        int end = start + 1;
        while (end < items.Count && Equals(key(items[end]), value)) {
          end++;
        }
        List<TItem> group = items.GetRange(start, end - start);
        IXLCell heading = sheet.Cell(Row, 1);
        _headings.Add((heading, $"{groups[level].Title}: {FormatValue(value, groups[level], culture)}"));
        heading.Style.Font.Bold = true;
        heading.Style.Alignment.Indent = level;
        Row++;
        int first = Row;
        int within = 0;
        if (level + 1 < groups.Count) {
          within = Groups(groups, group, level + 1);
        } else {
          Items(group);
        }
        sheet.Rows(first, Row - 1).Group();
        Footers(first, Row - 1, true, within);
        headings += 1 + within;
        start = end;
      }
      return headings;
    }

    public void WriteHeadings() {
      foreach ((IXLCell cell, string text) in _headings) {
        cell.Value = text;
      }
    }

    // Writes the totals of the rows from first to last. The headings within them are text in the first column, so a count
    // there takes them off
    public void Footers(int first, int last, bool subtotals, int headings) {
      for (int r = 0; r < FooterRows; r++) {
        for (int c = 0; c < columns.Count; c++) {
          if (r >= columns[c].Aggregates.Count) {
            continue;
          }
          GridAggregateType aggregate = columns[c].Aggregates[r];
          IXLCell cell = sheet.Cell(Row, c + 1);
          string range = $"{sheet.Cell(first, c + 1).Address.ToStringRelative()}:{sheet.Cell(last, c + 1).Address.ToStringRelative()}";
          string formula = subtotals
            ? $"SUBTOTAL({aggregate switch { GridAggregateType.Count => 3, GridAggregateType.Sum => 9, GridAggregateType.Average => 1, GridAggregateType.Min => 5, _ => 4 }},{range})"
            : $"{aggregate switch { GridAggregateType.Count => "COUNTA", GridAggregateType.Sum => "SUM", GridAggregateType.Average => "AVERAGE", GridAggregateType.Min => "MIN", _ => "MAX" }}({range})";
          cell.FormulaA1 = aggregate == GridAggregateType.Count && c == 0 && headings > 0 ? $"{formula}-{headings}" : formula;
          if (ExcelFormats.Labelled(label(aggregate), aggregate == GridAggregateType.Count ? "0" : _formats[c]) is string format) {
            cell.Style.NumberFormat.Format = format;
          }
          cell.Style.Font.Bold = true;
        }
        Row++;
      }
    }
  }

  private static void SetCell(IXLCell cell, object? value, PixataGridExportColumn column, CultureInfo culture) {
    string? format = ExcelFormats.For(column.DisplayFormat, culture);
    switch (value) {
      case null:
        return;
      case bool flag:
        cell.Value = flag;
        return;
      case DateTime date:
        cell.Value = date;
        cell.Style.DateFormat.Format = format ?? ExcelFormats.Date(culture.DateTimeFormat.ShortDatePattern);
        return;
      case DateOnly date:
        cell.Value = date.ToDateTime(TimeOnly.MinValue);
        cell.Style.DateFormat.Format = format ?? ExcelFormats.Date(culture.DateTimeFormat.ShortDatePattern);
        return;
      case DateTimeOffset offset:
        cell.Value = offset.DateTime;
        cell.Style.DateFormat.Format = format ?? ExcelFormats.Date(culture.DateTimeFormat.ShortDatePattern);
        return;
      case Enum:
        cell.Value = value.ToString();
        return;
      case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
        cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (format is not null) {
          cell.Style.NumberFormat.Format = format;
        }
        return;
      default:
        cell.Value = FormatValue(value, column, culture);
        return;
    }
  }

  private static double? PixelWidth(string? width) {
    if (string.IsNullOrWhiteSpace(width)) {
      return null;
    }
    string number = width.Trim().EndsWith("px", StringComparison.OrdinalIgnoreCase) ? width.Trim()[..^2] : width.Trim();
    return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double pixels) ? pixels : null;
  }
}

/// <summary>
/// Turns .NET format strings into Excel number formats
/// </summary>
public static partial class ExcelFormats {
  /// <summary>
  /// The Excel format for a .NET format string such as "{0:C2}" or "{0:dd MMM yyyy}", or null if there isn't one
  /// </summary>
  public static string? For(string? displayFormat, CultureInfo culture) {
    if (string.IsNullOrWhiteSpace(displayFormat)) {
      return null;
    }
    Match match = FormatItem().Match(displayFormat);
    if (!match.Success) {
      return null;
    }
    string format = match.Groups[1].Value;
    Match standard = StandardNumber().Match(format);
    if (standard.Success) {
      int? digits = standard.Groups[2].Success ? int.Parse(standard.Groups[2].Value, CultureInfo.InvariantCulture) : null;
      string decimals(int fallback) =>
        (digits ?? fallback) > 0 ? $".{new string('0', digits ?? fallback)}" : "";
      return char.ToUpperInvariant(standard.Groups[1].Value[0]) switch {
        'C' => $"\"{culture.NumberFormat.CurrencySymbol}\"#,##0{decimals(culture.NumberFormat.CurrencyDecimalDigits)}",
        'N' => $"#,##0{decimals(culture.NumberFormat.NumberDecimalDigits)}",
        'F' => $"0{decimals(culture.NumberFormat.NumberDecimalDigits)}",
        'P' => $"0{decimals(culture.NumberFormat.PercentDecimalDigits)}%",
        'D' => digits is int width ? new string('0', width) : "0",
        _ => null
      };
    }
    return format switch {
      "d" => Date(culture.DateTimeFormat.ShortDatePattern),
      "D" => Date(culture.DateTimeFormat.LongDatePattern),
      "g" => Date($"{culture.DateTimeFormat.ShortDatePattern} {culture.DateTimeFormat.ShortTimePattern}"),
      "G" => Date($"{culture.DateTimeFormat.ShortDatePattern} {culture.DateTimeFormat.LongTimePattern}"),
      "t" => Date(culture.DateTimeFormat.ShortTimePattern),
      "T" => Date(culture.DateTimeFormat.LongTimePattern),
      _ when format.Length > 1 && format.IndexOfAny(['d', 'M', 'y', 'H', 'h', 'm', 's']) >= 0 && format.All(c => !char.IsDigit(c) && c != '#') => Date(format),
      _ when format.Length > 1 && format.All(c => "#0.,%‰ -".Contains(c)) => format,
      _ => null
    };
  }

  /// <summary>
  /// An Excel format that shows the text before the value, such as "Average: " before "#,##0.00", so the cell is still a
  /// number. Each character of the text is escaped, so it's shown as it is. Returns the format as it is if there's no text
  /// </summary>
  public static string? Labelled(string? text, string? format) =>
    string.IsNullOrEmpty(text) ? format : $"{string.Concat(text.Select(c => $"\\{c}"))}{format ?? "General"}";

  /// <summary>
  /// The Excel format for a .NET custom date format, such as "dd MMM yyyy"
  /// </summary>
  public static string Date(string format) {
    StringBuilder excel = new();
    for (int i = 0; i < format.Length;) {
      char c = format[i];
      int run = 1;
      while (i + run < format.Length && format[i + run] == c) {
        run++;
      }
      string part = format.Substring(i, run);
      excel.Append(c switch {
        'M' => new string('m', run),
        'H' => new string('h', run),
        't' => "AM/PM",
        'f' or 'F' => new string('0', run),
        '\'' or '"' => "",
        'K' or 'z' => "",
        _ => part
      });
      i += run;
    }
    return excel.ToString();
  }

  [GeneratedRegex(@"\{0:([^}]+)\}")]
  private static partial Regex FormatItem();

  [GeneratedRegex(@"^([CcNnFfPpDd])(\d{1,2})?$")]
  private static partial Regex StandardNumber();
}

/// <summary>
/// Excel export methods for <see cref="PixataGrid{TItem}"/>
/// </summary>
public static class PixataGridExcelExtensions {
  /// <summary>
  /// Exports the grid to Excel, using the grid's <see cref="PixataGridExcelExport{TItem}"/> settings if it has any, and has
  /// the browser save the file
  /// </summary>
  public static Task SaveAsExcelFile<TItem>(this PixataGrid<TItem> grid) =>
    grid.SaveAsFile(grid.ExportSettings<PixataGridExcelExport<TItem>>() ?? new());

  /// <summary>
  /// Exports the grid to Excel. Returns null if <c>OnBeforeExport</c> cancelled it
  /// </summary>
  public static Task<MemoryStream?> ExportToExcel<TItem>(this PixataGrid<TItem> grid) =>
    grid.ExportWith(grid.ExportSettings<PixataGridExcelExport<TItem>>() ?? new());
}
