using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridInCell_Tests {
  private BunitContext _ctx = null!;

  public class Person {
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = "";

    public string Town { get; set; } = "";
  }

  private List<Person> _people = null!;
  private readonly List<string> _events = [];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _people = [new() { Id = 1, Name = "Alice", Town = "Leeds" }, new() { Id = 2, Name = "Bob", Town = "York" }];
    _events.Clear();
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataGrid<Person>> Render() =>
    _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, _people)
      .Add(g => g.EditMode, PixataGridEditMode.InCell)
      .Add(g => g.OnUpdate, args => {
        _events.Add($"update {args.Field}={args.Value}");
        _people[_people.FindIndex(x => x.Id == args.Item.Id)] = args.Item;
      })
      .Add(g => g.OnCreate, args => {
        _events.Add($"create {args.Item.Name}");
        _people.Add(args.Item);
      })
      .Add(g => g.OnCancel, args => _events.Add($"cancel {args.Field}"))
      .Add(g => g.GridToolBarTemplate, b => {
        b.OpenComponent<PixataGridCommandButton<Person>>(0);
        b.AddAttribute(1, "Command", "Add");
        b.AddAttribute(2, "ChildContent", (RenderFragment)(c => c.AddContent(0, "Add")));
        b.CloseComponent();
      })
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Name));
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(2);
        b.AddAttribute(3, "Field", nameof(Person.Town));
        b.CloseComponent();
      }));

  private static IElement Cell<TComponent>(IRenderedComponent<TComponent> grid, int row, int column) where TComponent : IComponent =>
    grid.FindAll("tbody tr")[row].QuerySelectorAll("td")[column];

  private static string EditingCell<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent {
    IReadOnlyList<IElement> rows = grid.FindAll("tbody tr");
    for (int r = 0; r < rows.Count; r++) {
      IHtmlCollection<IElement> cells = rows[r].QuerySelectorAll("td");
      for (int c = 0; c < cells.Length; c++) {
        if (cells[c].ClassList.Contains("pixata-blazor-grid-edit-cell")) {
          return $"{r},{c}";
        }
      }
    }
    return "none";
  }

  [TestMethod]
  public void ClickingACell_EditsJustThatCell() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Cell(grid, 1, 1).Click();

    Assert.AreEqual("1,1", EditingCell(grid));
    Assert.AreEqual("York", grid.Find(".pixata-blazor-grid-edit-cell input").GetAttribute("value"));
    Assert.AreEqual("Bob", Cell(grid, 1, 0).TextContent.Trim());
  }

  [TestMethod]
  public void Tab_SavesAndMovesToTheNextCellThenTheNextRow() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Cell(grid, 0, 0).Click();
    grid.Find(".pixata-blazor-grid-edit-cell input").Input("Alicia");
    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Tab" });
    Assert.AreEqual("0,1", EditingCell(grid));
    CollectionAssert.AreEqual(new[] { "update Name=Alicia" }, _events);

    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Tab" });
    Assert.AreEqual("1,0", EditingCell(grid));

    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Tab", ShiftKey = true });
    Assert.AreEqual("0,1", EditingCell(grid));
  }

  [TestMethod]
  public void Enter_SavesAndMovesDownARow() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Cell(grid, 0, 1).Click();
    grid.Find(".pixata-blazor-grid-edit-cell input").Input("Bath");
    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Enter" });

    Assert.AreEqual("1,1", EditingCell(grid));
    Assert.AreEqual("Bath", Cell(grid, 0, 1).TextContent.Trim());
    CollectionAssert.AreEqual(new[] { "update Town=Bath" }, _events);
  }

  [TestMethod]
  public void Escape_CancelsTheCell() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Cell(grid, 1, 0).Click();
    grid.Find(".pixata-blazor-grid-edit-cell input").Input("Robert");
    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Escape" });

    Assert.AreEqual("none", EditingCell(grid));
    Assert.AreEqual("Bob", Cell(grid, 1, 0).TextContent.Trim());
    CollectionAssert.AreEqual(new[] { "cancel Name" }, _events);
  }

  [TestMethod]
  public void InvalidValue_KeepsTheCellInEditMode() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Cell(grid, 0, 0).Click();
    grid.Find(".pixata-blazor-grid-edit-cell input").Input("");
    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Enter" });

    Assert.AreEqual("0,0", EditingCell(grid));
    Assert.AreEqual(0, _events.Count);
    Assert.IsTrue(grid.Find(".pixata-blazor-grid-edit-cell .pixata-blazor-grid-textbox").ClassList.Contains("pixata-blazor-grid-invalid"));
  }

  [TestMethod]
  public void Add_OpensTheFirstCellOfANewRowAndEnterCreatesIt() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.Find(".pixata-blazor-grid-toolbar button").Click();
    Assert.AreEqual("0,0", EditingCell(grid));
    Assert.AreEqual(3, grid.FindAll("tbody tr").Count);

    grid.Find(".pixata-blazor-grid-edit-cell input").Input("Carol");
    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Tab" });
    Assert.AreEqual("0,1", EditingCell(grid));
    Assert.AreEqual(0, _events.Count);

    grid.Find(".pixata-blazor-grid-edit-cell").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    CollectionAssert.AreEqual(new[] { "create Carol" }, _events);
    Assert.AreEqual("none", EditingCell(grid));
  }
}
