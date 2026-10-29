using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column as it will be exported. Change, remove or reorder these in <c>OnBeforeExport</c>
/// </summary>
public class PixataGridExportColumn {
  /// <summary>
  /// The field the values come from
  /// </summary>
  public string Field { get; set; } = "";

  /// <summary>
  /// The column's heading
  /// </summary>
  public string Title { get; set; } = "";

  /// <summary>
  /// The column's width, as a CSS width. Excel uses it if it's in pixels
  /// </summary>
  public string? Width { get; set; }

  /// <summary>
  /// The .NET format string for the values, such as "{0:C0}"
  /// </summary>
  public string? DisplayFormat { get; set; }
}

/// <summary>
/// What <c>OnBeforeExport</c> is given. Change the columns or the data to change what's exported, or cancel the export
/// </summary>
public class PixataGridBeforeExportEventArgs<TItem> {
  public List<PixataGridExportColumn> Columns { get; set; } = [];
  public List<TItem> Data { get; set; } = [];

  /// <summary>
  /// Set to true to stop the export
  /// </summary>
  public bool IsCancelled { get; set; }
}

/// <summary>
/// What <c>OnAfterExport</c> is given
/// </summary>
public class PixataGridAfterExportEventArgs(MemoryStream stream) {
  /// <summary>
  /// The exported file. Read it (from the start) to save it somewhere else as well
  /// </summary>
  public MemoryStream Stream { get; } = stream;
}

/// <summary>
/// The base for the grid's export settings, such as <see cref="PixataGridCsvExport{TItem}"/>. Goes in the grid's
/// <c>GridExport</c>. Other packages (such as Pixata.Blazor.Grid.Excel) derive from it to add formats
/// </summary>
public abstract class PixataGridExportBase<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  internal PixataGrid<TItem>? Grid { get; set; }

  /// <summary>
  /// The name of the file, without its extension
  /// </summary>
  [Parameter]
  public string FileName { get; set; } = "Grid";

  /// <summary>
  /// Whether to export all the rows (filtered and sorted as in the grid), rather than just the page shown
  /// </summary>
  [Parameter]
  public bool AllPages { get; set; }

  /// <summary>
  /// Fires before the export, with the columns and data, which you can change
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridBeforeExportEventArgs<TItem>> OnBeforeExport { get; set; }

  /// <summary>
  /// Fires after the export, with the file
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridAfterExportEventArgs> OnAfterExport { get; set; }

  /// <summary>
  /// The toolbar command that exports in this format, such as "CsvExport"
  /// </summary>
  public abstract string Command { get; }

  /// <summary>
  /// The file extension, including the dot
  /// </summary>
  public abstract string FileExtension { get; }

  public abstract string ContentType { get; }

  /// <summary>
  /// Writes the file
  /// </summary>
  public abstract Task<MemoryStream> Write(List<PixataGridExportColumn> columns, List<TItem> data, CultureInfo culture);

  protected override void OnInitialized() {
    if (Grid is null) {
      throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} must be inside the GridExport of a PixataGrid");
    }
    Grid.AddExport(this);
  }

  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    Grid?.RemoveExport(this);
    GC.SuppressFinalize(this);
  }

  // A value as text, formatted with the column's format, if it has one
  protected static string FormatValue(object? value, PixataGridExportColumn column, CultureInfo culture) =>
    value is null
      ? ""
      : string.IsNullOrWhiteSpace(column.DisplayFormat)
        ? Convert.ToString(value, culture) ?? ""
        : string.Format(culture, column.DisplayFormat, value);
}

/// <summary>
/// Settings for exporting to CSV, with the <c>CsvExport</c> toolbar command or <c>SaveAsCsvFile()</c>
/// </summary>
public class PixataGridCsvExport<TItem> : PixataGridExportBase<TItem> {
  public override string Command =>
    "CsvExport";

  public override string FileExtension =>
    ".csv";

  public override string ContentType =>
    "text/csv";

  // Values are written as they are, not formatted, so the file can be read back accurately: numbers with a full stop for the
  // decimal point, TRUE and FALSE, and dates as the current culture writes them. They are quoted if they contain a comma, a
  // quote or a line break
  public override Task<MemoryStream> Write(List<PixataGridExportColumn> columns, List<TItem> data, CultureInfo culture) {
    List<Func<TItem, object?>> getters = [.. columns.Select(c => FieldAccessor.Getter<TItem>(c.Field))];
    StringBuilder csv = new();
    csv.Append(string.Join(",", columns.Select(c => Quote(c.Title)))).Append("\r\n");
    foreach (TItem item in data) {
      csv.Append(string.Join(",", columns.Select((_, i) => Quote(RawValue(getters[i](item), culture))))).Append("\r\n");
    }
    MemoryStream stream = new();
    using (StreamWriter writer = new(stream, new UTF8Encoding(true), leaveOpen: true)) {
      writer.Write(csv.ToString());
    }
    stream.Position = 0;
    return Task.FromResult(stream);
  }

  private static string RawValue(object? value, CultureInfo culture) =>
    value switch {
      null => "",
      bool flag => flag ? "TRUE" : "FALSE",
      DateTime or DateOnly or DateTimeOffset => Convert.ToString(value, culture) ?? "",
      IFormattable number when value.GetType().IsPrimitive || value is decimal => number.ToString(null, CultureInfo.InvariantCulture),
      _ => Convert.ToString(value, culture) ?? ""
    };

  private static string Quote(string text) =>
    text.IndexOfAny([',', '"', '\r', '\n']) < 0 ? text : $"\"{text.Replace("\"", "\"\"")}\"";
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// The export settings, such as <see cref="PixataGridCsvExport{TItem}"/>
  /// </summary>
  [Parameter]
  public RenderFragment? GridExport { get; set; }

  private readonly List<PixataGridExportBase<TItem>> _exports = [];

  internal void AddExport(PixataGridExportBase<TItem> export) =>
    _exports.Add(export);

  internal void RemoveExport(PixataGridExportBase<TItem> export) =>
    _exports.Remove(export);

  // The settings for a format, if the page has given any
  internal T? ExportSettings<T>() where T : PixataGridExportBase<TItem> =>
    _exports.OfType<T>().FirstOrDefault();

  /// <summary>
  /// Exports the grid to CSV, and has the browser save it
  /// </summary>
  public Task SaveAsCsvFile() =>
    SaveAsFile(ExportSettings<PixataGridCsvExport<TItem>>() ?? new());

  /// <summary>
  /// Exports the grid to CSV. Returns null if <c>OnBeforeExport</c> cancelled it
  /// </summary>
  public Task<MemoryStream?> ExportToCsv() =>
    ExportWith(ExportSettings<PixataGridCsvExport<TItem>>() ?? new());

  /// <summary>
  /// Exports the grid with the given settings, and has the browser save the file. Used by other export packages
  /// </summary>
  public async Task SaveAsFile(PixataGridExportBase<TItem> export) {
    MemoryStream? stream = await ExportWith(export);
    if (stream is null) {
      return;
    }
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      using DotNetStreamReference reference = new(stream);
      await _module.InvokeVoidAsync("downloadFile", $"{export.FileName}{export.FileExtension}", export.ContentType, reference);
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
      // The page has gone
    }
  }

  /// <summary>
  /// Exports the grid with the given settings. Returns null if <c>OnBeforeExport</c> cancelled it. Used by other export
  /// packages
  /// </summary>
  public async Task<MemoryStream?> ExportWith(PixataGridExportBase<TItem> export) {
    PixataGridBeforeExportEventArgs<TItem> before = new() {
      Columns = [.. RenderedColumns()
        .Where(c => !string.IsNullOrWhiteSpace(c.Field) && c is not PixataGridCommandColumn<TItem> and not PixataGridCheckboxColumn<TItem> and not PixataGridDragColumn<TItem>)
        .Select(c => new PixataGridExportColumn { Field = c.Field!, Title = c.HeaderText, Width = c.CurrentWidth, DisplayFormat = c.Format })],
      Data = export.AllPages ? await AllData() : [.. _items]
    };
    await export.OnBeforeExport.InvokeAsync(before);
    if (before.IsCancelled) {
      return null;
    }
    MemoryStream stream = await export.Write(before.Columns, before.Data, CultureInfo.CurrentCulture);
    await export.OnAfterExport.InvokeAsync(new(stream));
    stream.Position = 0;
    return stream;
  }

  // Every row, filtered and sorted as the grid shows them. With OnRead, the handler is asked for them all
  private async Task<List<TItem>> AllData() {
    if (!UsesOnRead) {
      return (_boundData ?? []).ToGridResult(AllItemsRequest()).Data;
    }
    GridRequest request = AllItemsRequest();
    PixataGridReadEventArgs<TItem> args = new(request, CancellationToken.None);
    await OnRead.InvokeAsync(args);
    return [.. args.Data ?? []];
  }

  private async Task RunExportCommand(string command) {
    PixataGridExportBase<TItem>? export = _exports.FirstOrDefault(e => e.Command == command);
    if (export is null && command == "CsvExport") {
      export = new PixataGridCsvExport<TItem>();
    }
    if (export is null) {
      throw new InvalidOperationException($"The {command} command needs export settings in the grid's GridExport. For Excel, add the Pixata.Blazor.Grid.Excel package and a PixataGridExcelExport");
    }
    await SaveAsFile(export);
  }
}
