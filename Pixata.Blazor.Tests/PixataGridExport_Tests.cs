using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using Pixata.Blazor.Grid.Excel;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridExport_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  public record Person(int Id, string Name, decimal Salary, DateTime Born, bool Manager);

  private static readonly List<Person> People = [.. Enumerable.Range(1, 12).Select(n => new Person(n, n == 2 ? "Smith, \"Bob\"" : $"Person {n}", n * 1000.5m, new(1980, 1, n), n % 2 == 0))];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", false);
  }

  [TestCleanup]
  public void Cleanup() {
    CultureInfo.CurrentCulture = _culture;
    _ctx.Dispose();
  }

  private static void Column(RenderTreeBuilder b, string field, string? format = null, string? width = null) {
    b.OpenComponent<PixataGridColumn<Person>>(0);
    b.AddAttribute(1, "Field", field);
    if (format is not null) {
      b.AddAttribute(2, "DisplayFormat", format);
    }
    if (width is not null) {
      b.AddAttribute(3, "Width", width);
    }
    b.CloseComponent();
  }

  private IRenderedComponent<PixataGrid<Person>> Render(RenderFragment? export = null, params (string Field, GridAggregateType Aggregate)[] aggregates) =>
    _ctx.Render<PixataGrid<Person>>(p => {
      if (aggregates.Length > 0) {
        p.Add(g => g.GridAggregates, b => {
          foreach ((string field, GridAggregateType aggregate) in aggregates) {
            b.OpenComponent<PixataGridAggregate<Person>>(0);
            b.AddAttribute(1, "Field", field);
            b.AddAttribute(2, "Aggregate", aggregate);
            b.CloseComponent();
          }
        });
      }
      p.Add(g => g.Data, People)
        .Add(g => g.Pageable, true)
        .Add(g => g.PageSize, 5)
        .Add(g => g.GridColumns, b => {
          Column(b, nameof(Person.Id), width: "80px");
          Column(b, nameof(Person.Name));
          Column(b, nameof(Person.Salary), "{0:C0}");
          Column(b, nameof(Person.Born), "{0:dd MMM yyyy}");
          Column(b, nameof(Person.Manager));
          b.OpenComponent<PixataGridCommandColumn<Person>>(10);
          b.CloseComponent();
        });
      if (export is not null) {
        p.Add(g => g.GridExport, export);
      }
    });

  private static RenderFragment Export<TExport>(bool allPages = false, Action<PixataGridBeforeExportEventArgs<Person>>? before = null) where TExport : PixataGridExportBase<Person> =>
    b => {
      b.OpenComponent<TExport>(0);
      b.AddAttribute(1, "AllPages", allPages);
      if (before is not null) {
        b.AddAttribute(2, "OnBeforeExport", EventCallback.Factory.Create(new object(), before));
      }
      b.CloseComponent();
    };

  private static string Text(MemoryStream stream) =>
    Encoding.UTF8.GetString(stream.ToArray()).TrimStart('﻿');

  [TestMethod]
  public void Csv_HasAHeaderAndARowForEachItemOnThePage() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    MemoryStream stream = grid.InvokeAsync(() => grid.Instance.ExportToCsv()).Result!;
    string[] lines = Text(stream).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    // The command column has no field, so it isn't exported
    Assert.AreEqual("Id,Name,Salary,Born,Manager", lines[0]);
    Assert.AreEqual(6, lines.Length);
    // Values aren't formatted, and a value with a comma or quote is quoted
    Assert.AreEqual("1,Person 1,1000.5,01/01/1980 00:00:00,FALSE", lines[1]);
    Assert.AreEqual("2,\"Smith, \"\"Bob\"\"\",2001.0,02/01/1980 00:00:00,TRUE", lines[2]);
    // It starts with a byte order mark, so Excel reads it as UTF-8
    CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, stream.ToArray().Take(3).ToArray());
  }

  [TestMethod]
  public void AllPages_ExportsEveryRow() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Export<PixataGridCsvExport<Person>>(true));

    string csv = Text(grid.InvokeAsync(() => grid.Instance.ExportToCsv()).Result!);

    Assert.AreEqual(13, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
  }

  [TestMethod]
  public void OnBeforeExport_CanChangeTheColumnsAndData_OrCancel() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Export<PixataGridCsvExport<Person>>(before: args => {
      args.Columns.RemoveAll(c => c.Field != nameof(Person.Name));
      args.Columns[0].Title = "Who";
      args.Data = [.. args.Data.Take(1)];
    }));

    Assert.AreEqual("Who\r\nPerson 1\r\n", Text(grid.InvokeAsync(() => grid.Instance.ExportToCsv()).Result!));

    IRenderedComponent<PixataGrid<Person>> cancelled = Render(Export<PixataGridCsvExport<Person>>(before: args => args.IsCancelled = true));

    Assert.IsNull(cancelled.InvokeAsync(() => cancelled.Instance.ExportToCsv()).Result);
  }

  [TestMethod]
  public void Excel_KeepsNumbersAndDatesWithTheirFormats() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Export<PixataGridExcelExport<Person>>());

    MemoryStream stream = grid.InvokeAsync(() => grid.Instance.ExportToExcel()).Result!;
    using XLWorkbook workbook = new(stream);
    IXLWorksheet sheet = workbook.Worksheets.First();

    Assert.AreEqual("Name", sheet.Cell(1, 2).GetString());
    Assert.AreEqual(6, sheet.RangeUsed()!.RowCount());
    Assert.AreEqual(1000.5, sheet.Cell(2, 3).GetDouble());
    Assert.AreEqual("\"£\"#,##0", sheet.Cell(2, 3).Style.NumberFormat.Format);
    Assert.AreEqual(new DateTime(1980, 1, 1), sheet.Cell(2, 4).GetDateTime());
    Assert.AreEqual("dd mmm yyyy", sheet.Cell(2, 4).Style.DateFormat.Format);
    Assert.IsTrue(sheet.Cell(3, 5).GetBoolean());
    // 80px is about 10.7 characters
    Assert.AreEqual(10.7, Math.Round(sheet.Column(1).Width, 1));
  }

  private static RenderFragment Excel(bool totals = true, bool groupRows = false, Action<PixataGridBeforeExportEventArgs<Person>>? before = null) =>
    b => {
      b.OpenComponent<PixataGridExcelExport<Person>>(0);
      b.AddAttribute(1, "AllPages", true);
      b.AddAttribute(2, "Totals", totals);
      b.AddAttribute(3, "GroupRows", groupRows);
      if (before is not null) {
        b.AddAttribute(4, "OnBeforeExport", EventCallback.Factory.Create(new object(), before));
      }
      b.CloseComponent();
    };

  private static IXLWorksheet Sheet(IRenderedComponent<PixataGrid<Person>> grid) {
    XLWorkbook workbook = new(grid.InvokeAsync(() => grid.Instance.ExportToExcel()).Result!);
    return workbook.Worksheets.First();
  }

  // The salaries are 1000.5 times 1 to 12
  private const double AllSalaries = 1000.5 * 78;

  [TestMethod]
  public void Excel_WritesTheGridsAggregatesAsFormulas_UnderTheRows() {
    PixataGridBeforeExportEventArgs<Person>? given = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render(Excel(before: args => given = args),
      (nameof(Person.Salary), GridAggregateType.Sum), (nameof(Person.Name), GridAggregateType.Count),
      (nameof(Person.Salary), GridAggregateType.Average), (nameof(Person.Born), GridAggregateType.Max));

    IXLWorksheet sheet = Sheet(grid);

    // OnBeforeExport is told which aggregates each column has, so it can change them
    CollectionAssert.AreEqual(new[] { GridAggregateType.Sum, GridAggregateType.Average }, given!.Columns.Single(c => c.Field == nameof(Person.Salary)).Aggregates);
    Assert.AreEqual(0, given.Columns.Single(c => c.Field == nameof(Person.Id)).Aggregates.Count);
    // 12 rows from row 2, then a row for each column's first aggregate, and another for the salary's second
    Assert.AreEqual(15, sheet.LastRowUsed()!.RowNumber());
    Assert.AreEqual("SUM(C2:C13)", sheet.Cell(14, 3).FormulaA1);
    Assert.AreEqual(AllSalaries, sheet.Cell(14, 3).Value.GetNumber(), 0.001);
    // Formatted like the column. A sum has no label, as the row speaks for itself
    Assert.AreEqual("\"£\"#,##0", sheet.Cell(14, 3).Style.NumberFormat.Format);
    Assert.AreEqual("COUNTA(B2:B13)", sheet.Cell(14, 2).FormulaA1);
    Assert.AreEqual(12, sheet.Cell(14, 2).Value.GetNumber());
    Assert.AreEqual("\\C\\o\\u\\n\\t\\:\\ 0", sheet.Cell(14, 2).Style.NumberFormat.Format);
    Assert.AreEqual("MAX(D2:D13)", sheet.Cell(14, 4).FormulaA1);
    Assert.AreEqual("\\M\\a\\x\\:\\ dd mmm yyyy", sheet.Cell(14, 4).Style.NumberFormat.Format);
    Assert.AreEqual("AVERAGE(C2:C13)", sheet.Cell(15, 3).FormulaA1);
    Assert.AreEqual(AllSalaries / 12, sheet.Cell(15, 3).Value.GetNumber(), 0.001);
    Assert.IsTrue(sheet.Cell(15, 4).IsEmpty());
    Assert.IsTrue(sheet.Cell(14, 3).Style.Font.Bold);
    Assert.AreEqual(XLBorderStyleValues.Thin, sheet.Cell(14, 1).Style.Border.TopBorder);
  }

  [TestMethod]
  public void Excel_WithoutTotals_OrAggregates_HasJustTheRows() {
    Assert.AreEqual(13, Sheet(Render(Excel(totals: false), (nameof(Person.Salary), GridAggregateType.Sum))).LastRowUsed()!.RowNumber());
    Assert.AreEqual(13, Sheet(Render(Excel())).LastRowUsed()!.RowNumber());
  }

  [TestMethod]
  public void Excel_GroupRows_WritesEachGroupWithAHeadingAndSubtotals_InAnOutline() {
    PixataGridBeforeExportEventArgs<Person>? given = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render(Excel(groupRows: true, before: args => given = args),
      (nameof(Person.Id), GridAggregateType.Count), (nameof(Person.Salary), GridAggregateType.Sum));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Manager))).GetAwaiter().GetResult();

    IXLWorksheet sheet = Sheet(grid);

    Assert.AreEqual(nameof(Person.Manager), given!.Groups.Single().Title);
    // The odd numbered people aren't managers, and come first
    Assert.AreEqual("Manager: False", sheet.Cell(2, 1).GetString());
    Assert.IsTrue(sheet.Cell(2, 1).Style.Font.Bold);
    Assert.AreEqual("Person 1", sheet.Cell(3, 2).GetString());
    Assert.AreEqual("SUBTOTAL(3,A3:A8)", sheet.Cell(9, 1).FormulaA1);
    Assert.AreEqual(6, sheet.Cell(9, 1).Value.GetNumber());
    Assert.AreEqual("SUBTOTAL(9,C3:C8)", sheet.Cell(9, 3).FormulaA1);
    Assert.AreEqual(1000.5 * 36, sheet.Cell(9, 3).Value.GetNumber(), 0.001);
    Assert.AreEqual("Manager: True", sheet.Cell(10, 1).GetString());
    // The totals leave out the subtotals, and the count leaves out the headings, which are text in its column
    Assert.AreEqual("SUBTOTAL(9,C2:C17)", sheet.Cell(18, 3).FormulaA1);
    Assert.AreEqual(AllSalaries, sheet.Cell(18, 3).Value.GetNumber(), 0.001);
    Assert.AreEqual("SUBTOTAL(3,A2:A17)-2", sheet.Cell(18, 1).FormulaA1);
    Assert.AreEqual(12, sheet.Cell(18, 1).Value.GetNumber());
    // Each group's rows can be collapsed, leaving its heading and totals
    Assert.AreEqual(0, sheet.Row(2).OutlineLevel);
    Assert.AreEqual(1, sheet.Row(3).OutlineLevel);
    Assert.AreEqual(1, sheet.Row(8).OutlineLevel);
    Assert.AreEqual(0, sheet.Row(9).OutlineLevel);
  }

  [TestMethod]
  public void Excel_GroupRows_NestsGroupsWithinGroups() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Excel(groupRows: true), (nameof(Person.Id), GridAggregateType.Count));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Manager))).GetAwaiter().GetResult();
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Name))).GetAwaiter().GetResult();

    IXLWorksheet sheet = Sheet(grid);

    // Each person is a group of their own: a heading, their row, and its count
    Assert.AreEqual("Name: Person 1", sheet.Cell(3, 1).GetString());
    Assert.AreEqual(1, sheet.Cell(3, 1).Style.Alignment.Indent);
    Assert.AreEqual(1, sheet.Cell(4, 1).GetDouble());
    Assert.AreEqual(2, sheet.Row(4).OutlineLevel);
    Assert.AreEqual(1, sheet.Cell(5, 1).Value.GetNumber());
    Assert.AreEqual("SUBTOTAL(3,A3:A20)-6", sheet.Cell(21, 1).FormulaA1);
    Assert.AreEqual(6, sheet.Cell(21, 1).Value.GetNumber());
    Assert.AreEqual("SUBTOTAL(3,A2:A41)-14", sheet.Cell(42, 1).FormulaA1);
    Assert.AreEqual(12, sheet.Cell(42, 1).Value.GetNumber());
  }

  [TestMethod]
  public void Excel_GroupRows_WithoutTotals_HasJustTheHeadings() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Excel(totals: false, groupRows: true), (nameof(Person.Salary), GridAggregateType.Sum));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Manager))).GetAwaiter().GetResult();

    IXLWorksheet sheet = Sheet(grid);

    // Two headings and twelve rows
    Assert.AreEqual(15, sheet.LastRowUsed()!.RowNumber());
    Assert.AreEqual("Manager: True", sheet.Cell(9, 1).GetString());
  }

  [TestMethod]
  public void Excel_WithoutGroupRows_AGroupedGridIsAPlainTable() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Excel(), (nameof(Person.Salary), GridAggregateType.Sum));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Manager))).GetAwaiter().GetResult();

    IXLWorksheet sheet = Sheet(grid);

    Assert.AreEqual("Person 1", sheet.Cell(2, 2).GetString());
    Assert.AreEqual("SUM(C2:C13)", sheet.Cell(14, 3).FormulaA1);
  }

  [TestMethod]
  [DataRow("Count: ", "0", "\\C\\o\\u\\n\\t\\:\\ 0")]
  [DataRow("Ø ", null, "\\Ø\\ General")]
  [DataRow("", "#,##0", "#,##0")]
  public void ExcelFormats_Labelled_EscapesTheText(string text, string? format, string expected) =>
    Assert.AreEqual(expected, ExcelFormats.Labelled(text, format));

  [TestMethod]
  public void ExcelExportCommand_WithoutTheSettings_SaysWhatIsMissing() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    InvalidOperationException ex = Assert.ThrowsException<InvalidOperationException>(() => grid.InvokeAsync(() => grid.Instance.Command("ExcelExport", default)).GetAwaiter().GetResult());

    StringAssert.Contains(ex.Message, "Pixata.Blazor.Grid.Excel");
  }

  [TestMethod]
  public void ExcelExportCommand_WithTheSettings_SavesTheFile() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Export<PixataGridExcelExport<Person>>());

    grid.InvokeAsync(() => grid.Instance.Command("ExcelExport", default)).GetAwaiter().GetResult();

    JSRuntimeInvocation download = _ctx.JSInterop.Invocations["downloadFile"].Single();
    Assert.AreEqual("Grid.xlsx", download.Arguments[0]);
  }

  [TestMethod]
  [DataRow("{0:C2}", "\"£\"#,##0.00")]
  [DataRow("{0:N0}", "#,##0")]
  [DataRow("{0:P1}", "0.0%")]
  [DataRow("{0:F3}", "0.000")]
  [DataRow("{0:dd/MM/yyyy HH:mm}", "dd/mm/yyyy hh:mm")]
  [DataRow("{0:#,##0.00}", "#,##0.00")]
  [DataRow("Total: {0}", null)]
  public void ExcelFormats_TranslatesDotNetFormats(string format, string? expected) =>
    Assert.AreEqual(expected, ExcelFormats.For(format, new CultureInfo("en-GB", false)));
}
