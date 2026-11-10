using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// One cell that pasted text lands on, in <c>OnPaste</c>
/// </summary>
public class PixataGridPasteCell<TItem>(TItem item, string field, string text) {
  /// <summary>
  /// The row's item, as it is before the paste
  /// </summary>
  public TItem Item { get; } = item;

  /// <summary>
  /// The column's field
  /// </summary>
  public string Field { get; } = field;

  /// <summary>
  /// The text pasted into the cell
  /// </summary>
  public string Text { get; } = text;

  /// <summary>
  /// The text, converted to the field's type. Change it to put a different value in the cell
  /// </summary>
  public object? Value { get; set; }

  /// <summary>
  /// Why the text can't go in the cell, such as a word pasted into a number column, or a value that breaks the field's
  /// validation attributes. Null if it can. A cell with an error is left as it was. Set it to leave a cell as it was, or
  /// clear it to put the <see cref="Value"/> in after all
  /// </summary>
  public string? Error { get; set; }
}

/// <summary>
/// What <c>OnPaste</c> is given: the cells the pasted text lands on, before they're changed
/// </summary>
public class PixataGridPasteEventArgs<TItem> {
  /// <summary>
  /// The text that was pasted, split into rows and cells
  /// </summary>
  public List<List<string>> Rows { get; init; } = [];

  /// <summary>
  /// The cells the text lands on, row by row, leaving out the ones that can't be edited, and any past the last row or
  /// column. Change their values, set errors to leave some as they are, or remove them
  /// </summary>
  public List<PixataGridPasteCell<TItem>> Cells { get; init; } = [];

  /// <summary>
  /// Set to true to stop the grid changing the cells, such as when you change the data yourself
  /// </summary>
  public bool IsCancelled { get; set; }
}

// Pasting a block of cells copied from a spreadsheet into a navigable in-cell grid. The JavaScript catches the paste, and if
// it's into a cell (rather than an editor), or the text has tabs or line breaks, it sends the text here rather than letting
// it go into the editor
public partial class PixataGrid<TItem> {
  /// <summary>
  /// Whether pasting cells copied from a spreadsheet, such as Excel, into a grid with <c>EditMode="InCell"</c> and
  /// <c>Navigable="true"</c> fills the cells to the right and below, as it would in the spreadsheet. Defaults to true
  /// </summary>
  [Parameter]
  public bool PasteFromSpreadsheets { get; set; } = true;

  /// <summary>
  /// Fires when cells are pasted into the grid, before they're changed. See <see cref="PasteFromSpreadsheets"/>
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridPasteEventArgs<TItem>> OnPaste { get; set; }

  private bool CanPaste =>
    PasteFromSpreadsheets && Navigable && EditMode == PixataGridEditMode.InCell;

  /// <summary>
  /// Called by the JavaScript when text is pasted into a cell. Spreads it over the cells to the right and below, converting
  /// each value to its field's type, and raises <c>OnUpdate</c> once for each row that changes
  /// </summary>
  /// <param name="rowIndex">The row's position among the rows shown</param>
  /// <param name="columnKey">The column's key</param>
  /// <param name="text">The text pasted: rows separated by line breaks, and cells by tabs</param>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task Paste(int rowIndex, string columnKey, string text) {
    if (!CanPaste || rowIndex < 0 || rowIndex >= _items.Count || (_editing && _isNew)) {
      return;
    }
    List<PixataGridColumn<TItem>> columns = [.. RenderedColumns().Where(c => !string.IsNullOrWhiteSpace(c.Field)
      && c is not PixataGridCommandColumn<TItem> and not PixataGridCheckboxColumn<TItem> and not PixataGridDragColumn<TItem>)];
    int startColumn = columns.FindIndex(c => c.Key == columnKey);
    List<List<string>> rows = SpreadsheetText.Parse(text);
    if (startColumn < 0 || rows.Count == 0) {
      return;
    }
    // The cell being edited is being pasted over, so its edit is cancelled
    if (_editing) {
      await CancelCellEdit();
      if (_editing) {
        return;
      }
    }
    PixataGridPasteEventArgs<TItem> args = new() { Rows = rows };
    for (int r = 0; r < rows.Count && rowIndex + r < _items.Count; r++) {
      TItem item = _items[rowIndex + r];
      for (int c = 0; c < rows[r].Count && startColumn + c < columns.Count; c++) {
        PixataGridColumn<TItem> column = columns[startColumn + c];
        if (!CanEdit(column)) {
          continue;
        }
        PixataGridPasteCell<TItem> cell = new(item, column.Field!, rows[r][c]);
        (cell.Value, cell.Error) = PastedValue(cell.Text, column);
        cell.Error ??= ValidationError(item, column, cell.Value);
        args.Cells.Add(cell);
      }
    }
    await OnPaste.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    // The rows are worked out before any are updated, as an update can re-render the grid, and so change its rows (if they're
    // sorted by a pasted column, say)
    List<List<PixataGridPasteCell<TItem>>> changed = [];
    foreach (PixataGridPasteCell<TItem> cell in args.Cells.Where(c => c.Error is null)) {
      if (changed.FirstOrDefault(r => SameItem(r[0].Item, cell.Item)) is List<PixataGridPasteCell<TItem>> row) {
        row.Add(cell);
      } else {
        changed.Add([cell]);
      }
    }
    foreach (List<PixataGridPasteCell<TItem>> cells in changed) {
      TItem copy = Copy(cells[0].Item);
      foreach (PixataGridPasteCell<TItem> cell in cells) {
        FieldAccessor.SetValue(copy, cell.Field, cell.Value);
      }
      // A row with one cell changed is reported as an in-cell edit of that cell, and one with more as an edit of the row
      await OnUpdate.InvokeAsync(cells.Count == 1 ? new(copy) { Field = cells[0].Field, Value = cells[0].Value } : new(copy));
    }
    RefreshAfterEdit();
    StateHasChanged();
  }

  // The text converted to the column's type, or why it can't be. An empty cell clears a field that can be empty
  private (object? Value, string? Error) PastedValue(string text, PixataGridColumn<TItem> column) {
    Type type = FieldAccessor.FieldType(typeof(TItem), column.Field!);
    Type underlying = Nullable.GetUnderlyingType(type) ?? type;
    if (underlying == typeof(string)) {
      return (text, null);
    }
    string value = text.Trim();
    if (value == "") {
      return !type.IsValueType || underlying != type ? (null, null) : (null, Text.PasteEmptyValue(column.HeaderText));
    }
    // "15%" is 0.15 in a column shown as a percentage, and 15 in any other
    bool percentages = column.Format is string format && (format.Contains(":P", StringComparison.OrdinalIgnoreCase) || format.Contains('%'));
    return SpreadsheetText.Convert(value, underlying, CultureInfo.CurrentCulture, e => column.FormatValue(e), percentages) is (true, object converted)
      ? (converted, null)
      : (null, Text.PasteInvalidValue(text, column.HeaderText));
  }

  // The first validation attribute on the field that the value breaks, if there's no validator template of the app's own
  private string? ValidationError(TItem item, PixataGridColumn<TItem> column, object? value) {
    if (!(ValidationSettings?.Enabled ?? true) || ValidationSettings?.ValidatorTemplate is not null) {
      return null;
    }
    string[] parts = column.Field!.Split('.');
    object? owner = parts.Length == 1 ? item : FieldAccessor.Getter<TItem>(string.Join('.', parts[..^1]))(item);
    if (owner is null || owner.GetType().GetProperty(parts[^1]) is null) {
      return null;
    }
    List<ValidationResult> results = [];
    return System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(value, new(owner) { MemberName = parts[^1] }, results) ? null : results.FirstOrDefault()?.ErrorMessage;
  }
}

/// <summary>
/// Reads text copied from a spreadsheet, such as Excel or Google Sheets
/// </summary>
public static class SpreadsheetText {
  /// <summary>
  /// Splits the text into rows (at line breaks) and cells (at tabs). A cell in quotes can have tabs, line breaks and quotes
  /// (doubled) in it, as spreadsheets write them. The line break at the end, which spreadsheets add, is ignored
  /// </summary>
  public static List<List<string>> Parse(string text) {
    List<List<string>> rows = [];
    if (string.IsNullOrEmpty(text)) {
      return rows;
    }
    if (text.EndsWith("\r\n")) {
      text = text[..^2];
    } else if (text.EndsWith('\n') || text.EndsWith('\r')) {
      text = text[..^1];
    }
    List<string> row = [];
    int i = 0;
    while (true) {
      row.Add(Cell(text, ref i));
      if (i >= text.Length) {
        break;
      }
      if (text[i] == '\t') {
        i++;
        continue;
      }
      // A line break, of whichever kind
      i += text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
      rows.Add(row);
      row = [];
    }
    rows.Add(row);
    return rows;
  }

  // Reads one cell, leaving i at the tab or line break after it, or the end. A cell is only taken as quoted if the closing
  // quote is followed by the end of the cell, so a cell that just starts with a quote is read as it is
  private static string Cell(string text, ref int i) {
    if (i < text.Length && text[i] == '"') {
      StringBuilder quoted = new();
      int j = i + 1;
      while (j < text.Length) {
        if (text[j] == '"') {
          if (j + 1 < text.Length && text[j + 1] == '"') {
            quoted.Append('"');
            j += 2;
            continue;
          }
          if (j + 1 == text.Length || text[j + 1] is '\t' or '\r' or '\n') {
            i = j + 1;
            return quoted.ToString();
          }
          break;
        }
        quoted.Append(text[j]);
        j++;
      }
    }
    int start = i;
    while (i < text.Length && text[i] is not ('\t' or '\r' or '\n')) {
      i++;
    }
    return text[start..i];
  }

  /// <summary>
  /// Converts a value from a spreadsheet to a type, as the culture writes it. Numbers can have thousands separators, a
  /// currency symbol (any, as the spreadsheet may be set up for another currency), brackets for a negative amount, or a
  /// percent sign. True and false can be TRUE and FALSE, yes and no, or
  /// 1 and 0. Enums can be their names, or their text in the grid, given by <paramref name="enumText"/>
  /// </summary>
  /// <param name="percentages">Whether a value with a percent sign is a fraction, so "5%" is 0.05, as it is in a column shown
  /// as a percentage. Otherwise "5%" is 5</param>
  /// <returns>Whether it could be converted, and the value</returns>
  public static (bool Success, object? Value) Convert(string text, Type type, CultureInfo culture, Func<Enum, string>? enumText = null, bool percentages = true) {
    text = text.Trim();
    if (type == typeof(string)) {
      return (true, text);
    }
    if (type == typeof(bool)) {
      return text.ToUpperInvariant() switch {
        "TRUE" or "YES" or "Y" or "1" => (true, true),
        "FALSE" or "NO" or "N" or "0" => (true, false),
        _ => bool.TryParse(text, out bool flag) ? (true, flag) : (false, null)
      };
    }
    if (type.IsEnum) {
      foreach (Enum value in Enum.GetValues(type)) {
        if (string.Equals(value.ToString(), text, StringComparison.OrdinalIgnoreCase)
          || (enumText is not null && string.Equals(enumText(value), text, StringComparison.CurrentCultureIgnoreCase))) {
          return (true, value);
        }
      }
      return (false, null);
    }
    if (type == typeof(DateTime)) {
      return DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out DateTime date) ? (true, date) : (false, null);
    }
    if (type == typeof(DateOnly)) {
      return DateOnly.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out DateOnly date) ? (true, date)
        : DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out DateTime withTime) ? (true, DateOnly.FromDateTime(withTime))
        : (false, null);
    }
    if (type == typeof(TimeOnly)) {
      return TimeOnly.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out TimeOnly time) ? (true, time) : (false, null);
    }
    if (type == typeof(DateTimeOffset)) {
      return DateTimeOffset.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset date) ? (true, date) : (false, null);
    }
    if (Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
      or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal) {
      bool percent = text.EndsWith(culture.NumberFormat.PercentSymbol, StringComparison.Ordinal);
      string number = percent ? text[..^culture.NumberFormat.PercentSymbol.Length] : text;
      number = string.Concat(number.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.CurrencySymbol)).Trim();
      if (!decimal.TryParse(number, NumberStyles.Currency | NumberStyles.AllowExponent, culture, out decimal parsed)) {
        return (false, null);
      }
      if (percent && percentages) {
        parsed /= 100;
      }
      try {
        if (Type.GetTypeCode(type) is not (TypeCode.Single or TypeCode.Double or TypeCode.Decimal) && parsed != decimal.Truncate(parsed)) {
          return (false, null);
        }
        return (true, System.Convert.ChangeType(parsed, type, culture));
      } catch (OverflowException) {
        return (false, null);
      }
    }
    try {
      TypeConverter converter = TypeDescriptor.GetConverter(type);
      return converter.CanConvertFrom(typeof(string)) ? (true, converter.ConvertFromString(null, culture, text)) : (false, null);
    } catch (Exception ex) when (ex is FormatException or NotSupportedException or ArgumentException) {
      return (false, null);
    }
  }
}
