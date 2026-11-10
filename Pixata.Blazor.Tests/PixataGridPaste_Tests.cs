using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridPaste_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  public enum Status {
    Draft,
    OnHold,
    Done
  }

  public class Line {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public DateTime? Due { get; set; }
    public string Code { get; set; } = "";

    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
    public int Discount { get; set; }

    public bool Paid { get; set; }
    public Status Status { get; set; }
  }

  private List<Line> _lines = null!;
  private readonly List<PixataGridCommandEventArgs<Line>> _updates = [];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", false);
    _lines = [.. Enumerable.Range(1, 3).Select(n => new Line { Id = n, Name = $"Line {n}", Amount = n, Quantity = n, Code = $"C{n}" })];
    _updates.Clear();
  }

  [TestCleanup]
  public void Cleanup() {
    CultureInfo.CurrentCulture = _culture;
    _ctx.Dispose();
  }

  private static void Column(RenderTreeBuilder b, string field, bool editable = true) {
    b.OpenComponent<PixataGridColumn<Line>>(0);
    b.AddAttribute(1, "Field", field);
    b.AddAttribute(2, "Editable", editable);
    b.CloseComponent();
  }

  private IRenderedComponent<PixataGrid<Line>> Render(bool navigable = true, bool paste = true, Action<PixataGridPasteEventArgs<Line>>? onPaste = null) =>
    _ctx.Render<PixataGrid<Line>>(p => {
      p.Add(g => g.Data, _lines)
        .Add(g => g.EditMode, PixataGridEditMode.InCell)
        .Add(g => g.Navigable, navigable)
        .Add(g => g.PasteFromSpreadsheets, paste)
        .Add(g => g.OnUpdate, args => {
          _updates.Add(args);
          _lines[_lines.FindIndex(l => l.Id == args.Item.Id)] = args.Item;
        })
        .Add(g => g.GridColumns, b => {
          Column(b, nameof(Line.Name));
          Column(b, nameof(Line.Amount));
          Column(b, nameof(Line.Quantity));
          Column(b, nameof(Line.Due));
          Column(b, nameof(Line.Code), false);
          Column(b, nameof(Line.Discount));
          Column(b, nameof(Line.Paid));
          Column(b, nameof(Line.Status));
        });
      if (onPaste is not null) {
        p.Add(g => g.OnPaste, onPaste);
      }
    });

  // The key the JavaScript sends for a column, from the first row's cells
  private static string Key(IRenderedComponent<PixataGrid<Line>> grid, int column) =>
    grid.FindAll("tbody tr")[0].QuerySelectorAll("td")[column].GetAttribute("data-pixata-column-key")!;

  private static void Paste(IRenderedComponent<PixataGrid<Line>> grid, int row, int column, string text) =>
    grid.InvokeAsync(() => grid.Instance.Paste(row, Key(grid, column), text)).GetAwaiter().GetResult();

  [TestMethod]
  public void ABlock_FillsTheCellsToTheRightAndBelow_WithAnUpdateForEachRow() {
    IRenderedComponent<PixataGrid<Line>> grid = Render();

    // As Excel copies two rows of three cells
    Paste(grid, 0, 1, "1,234.50\t5\t31/12/2026\r\n£99\t6\t01/02/2027\r\n");

    Assert.AreEqual(2, _updates.Count);
    // More than one cell changed, so it's reported as an edit of the row
    Assert.IsNull(_updates[0].Field);
    Assert.AreEqual(1234.50m, _lines[0].Amount);
    Assert.AreEqual(5, _lines[0].Quantity);
    Assert.AreEqual(new DateTime(2026, 12, 31), _lines[0].Due);
    Assert.AreEqual(99m, _lines[1].Amount);
    Assert.AreEqual(new DateTime(2027, 2, 1), _lines[1].Due);
    // The rest is as it was
    Assert.AreEqual("Line 1", _lines[0].Name);
    Assert.AreEqual(3m, _lines[2].Amount);
    // The grid shows the new values
    Assert.AreEqual("1234.50", grid.FindAll("tbody tr")[0].QuerySelectorAll("td")[1].TextContent.Trim());
  }

  [TestMethod]
  public void Values_AreConvertedAsTheSpreadsheetWritesThem() {
    IRenderedComponent<PixataGrid<Line>> grid = Render();

    Paste(grid, 0, 5, "15%\tyes\tonhold\r\n");
    Paste(grid, 1, 1, "(12.5)\t2E1\r\n");
    Paste(grid, 2, 6, "FALSE\tDone");

    // The discount isn't shown as a percentage, so 15% is 15
    Assert.AreEqual(15, _lines[0].Discount);
    Assert.IsTrue(_lines[0].Paid);
    Assert.AreEqual(Status.OnHold, _lines[0].Status);
    Assert.AreEqual(-12.5m, _lines[1].Amount);
    Assert.AreEqual(20, _lines[1].Quantity);
    Assert.IsFalse(_lines[2].Paid);
    Assert.AreEqual(Status.Done, _lines[2].Status);
  }

  [TestMethod]
  public void CellsThatCantBeEdited_AreSkipped_AndTheRestStayInTheirColumns() {
    IRenderedComponent<PixataGrid<Line>> grid = Render();

    // From Due across Code (not editable) to Discount
    Paste(grid, 0, 3, "01/01/2027\tX99\t10");

    Assert.AreEqual(new DateTime(2027, 1, 1), _lines[0].Due);
    Assert.AreEqual("C1", _lines[0].Code);
    Assert.AreEqual(10, _lines[0].Discount);
  }

  [TestMethod]
  public void TextPastTheLastRowOrColumn_IsLeftOut() {
    IRenderedComponent<PixataGrid<Line>> grid = Render();

    Paste(grid, 2, 7, "Done\tmore\r\nDraft\tmore\r\n");

    Assert.AreEqual(1, _updates.Count);
    Assert.AreEqual(Status.Done, _lines[2].Status);
    // One cell changed, so it's reported as an in-cell edit of it
    Assert.AreEqual(nameof(Line.Status), _updates[0].Field);
    Assert.AreEqual(Status.Done, _updates[0].Value);
  }

  [TestMethod]
  public void ValuesThatCantGoIn_LeaveTheirCellsAsTheyWere_AndAreGivenToOnPaste() {
    PixataGridPasteEventArgs<Line>? given = null;
    IRenderedComponent<PixataGrid<Line>> grid = Render(onPaste: args => given = args);

    Paste(grid, 0, 1, "lots\t\t\t\t150\r\n7\t8\r\n");

    Assert.AreEqual("\"lots\" isn't a valid value for Amount", given!.Cells.Single(c => c.Item.Id == 1 && c.Field == nameof(Line.Amount)).Error);
    Assert.AreEqual("Quantity can't be empty", given.Cells.Single(c => c.Item.Id == 1 && c.Field == nameof(Line.Quantity)).Error);
    Assert.AreEqual("Discount must be between 0 and 100", given.Cells.Single(c => c.Field == nameof(Line.Discount)).Error);
    // An empty cell clears a field that can be empty
    Assert.IsNull(given.Cells.Single(c => c.Item.Id == 1 && c.Field == nameof(Line.Due)).Error);
    Assert.AreEqual(2, given.Rows.Count);
    CollectionAssert.AreEqual(new[] { "lots", "", "", "", "150" }, given.Rows[0]);
    Assert.AreEqual(1m, _lines[0].Amount);
    Assert.AreEqual(0, _lines[0].Discount);
    Assert.AreEqual(7m, _lines[1].Amount);
    Assert.AreEqual(8, _lines[1].Quantity);
  }

  [TestMethod]
  public void OnPaste_CanChangeTheValues_OrCancel() {
    IRenderedComponent<PixataGrid<Line>> grid = Render(onPaste: args => args.Cells[0].Value = 42m);

    Paste(grid, 0, 1, "1\t2");

    Assert.AreEqual(42m, _lines[0].Amount);
    Assert.AreEqual(2, _lines[0].Quantity);

    IRenderedComponent<PixataGrid<Line>> cancelled = Render(onPaste: args => args.IsCancelled = true);
    _updates.Clear();

    Paste(cancelled, 1, 1, "1\t2");

    Assert.AreEqual(0, _updates.Count);
  }

  [TestMethod]
  public void PastingWhileEditing_CancelsTheEditFirst() {
    IRenderedComponent<PixataGrid<Line>> grid = Render();
    grid.FindAll("tbody tr")[0].QuerySelectorAll("td")[0].Click();
    string key = grid.Find(".pixata-blazor-grid-edit-cell").GetAttribute("data-pixata-column-key")!;

    grid.InvokeAsync(() => grid.Instance.Paste(0, key, "Alpha\r\nBeta\r\n")).GetAwaiter().GetResult();

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-edit-cell").Count);
    Assert.AreEqual("Alpha", _lines[0].Name);
    Assert.AreEqual("Beta", _lines[1].Name);
  }

  [TestMethod]
  public void OnlyANavigableInCellGrid_TakesPastes() {
    Assert.AreEqual("true", Render().Find(".pixata-blazor-grid").GetAttribute("data-pixata-paste"));

    IRenderedComponent<PixataGrid<Line>> off = Render(paste: false);
    Assert.IsNull(off.Find(".pixata-blazor-grid").GetAttribute("data-pixata-paste"));
    Paste(off, 0, 1, "1\t2");
    Assert.AreEqual(0, _updates.Count);

    Assert.IsNull(Render(navigable: false).Find(".pixata-blazor-grid").GetAttribute("data-pixata-paste"));
  }

  [TestMethod]
  public void SpreadsheetText_SplitsRowsAndCells_AndUnquotesCellsWithBreaksInThem() {
    List<List<string>> rows = SpreadsheetText.Parse("a\t\"two\r\nlines\"\t\"say \"\"hi\"\"\"\r\n\"tab\there\"\t\tc\n\"just starts with a quote\" here\r\n");

    Assert.AreEqual(3, rows.Count);
    CollectionAssert.AreEqual(new[] { "a", "two\r\nlines", "say \"hi\"" }, rows[0]);
    CollectionAssert.AreEqual(new[] { "tab\there", "", "c" }, rows[1]);
    CollectionAssert.AreEqual(new[] { "\"just starts with a quote\" here" }, rows[2]);
    CollectionAssert.AreEqual(new[] { "one" }, SpreadsheetText.Parse("one").Single());
    Assert.AreEqual(0, SpreadsheetText.Parse("").Count);
  }

  [TestMethod]
  [DataRow("1,234.5", typeof(decimal), 1234.5)]
  [DataRow("£1,234.50", typeof(double), 1234.5)]
  [DataRow("€ 99", typeof(decimal), 99)]
  [DataRow("-$5.25", typeof(decimal), -5.25)]
  [DataRow("(5)", typeof(int), -5)]
  [DataRow("12.5%", typeof(double), 0.125)]
  [DataRow("12.5%", typeof(decimal), 0.125)]
  [DataRow("3.5", typeof(int), null)]
  [DataRow("300", typeof(byte), null)]
  [DataRow("Y", typeof(bool), true)]
  [DataRow("0", typeof(bool), false)]
  [DataRow("maybe", typeof(bool), null)]
  public void SpreadsheetText_ConvertsNumbersAndBooleans(string text, Type type, object? expected) {
    (bool success, object? value) = SpreadsheetText.Convert(text, type, new CultureInfo("en-GB", false));

    Assert.AreEqual(expected is not null, success);
    if (expected is not null) {
      Assert.AreEqual(System.Convert.ChangeType(expected, type, CultureInfo.InvariantCulture), value);
    }
  }
}
