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

  public override string Command =>
    "ExcelExport";

  public override string FileExtension =>
    ".xlsx";

  public override string ContentType =>
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  // Numbers, dates and true/false values are stored as such, with the column's format turned into Excel's, so they can be
  // worked with in Excel. Everything else is stored as the text the grid shows
  public override Task<MemoryStream> Write(List<PixataGridExportColumn> columns, List<TItem> data, CultureInfo culture) {
    using XLWorkbook workbook = new();
    IXLWorksheet sheet = workbook.AddWorksheet(SheetName);
    List<Func<TItem, object?>> getters = [.. columns.Select(c => FieldAccessor.Getter<TItem>(c.Field))];
    for (int c = 0; c < columns.Count; c++) {
      IXLCell header = sheet.Cell(1, c + 1);
      header.Value = columns[c].Title;
      if (PixelWidth(columns[c].Width) is double pixels) {
        // Excel measures widths in characters of its default font, which are about 7 pixels wide, plus some padding
        sheet.Column(c + 1).Width = Math.Max((pixels - 5) / 7, 1);
      }
    }
    for (int r = 0; r < data.Count; r++) {
      for (int c = 0; c < columns.Count; c++) {
        SetCell(sheet.Cell(r + 2, c + 1), getters[c](data[r]), columns[c], culture);
      }
    }
    for (int c = 0; c < columns.Count; c++) {
      if (PixelWidth(columns[c].Width) is null) {
        sheet.Column(c + 1).AdjustToContents();
      }
    }
    MemoryStream stream = new();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return Task.FromResult(stream);
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
