using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridSelection_Tests {
  private BunitContext _ctx = null!;

  public record Person(int Id, string Name);

  private static List<Person> People() => [.. Enumerable.Range(1, 6).Select(n => new Person(n, $"P{n}"))];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataGrid<Person>> Render(PixataGridSelectionMode mode, Action<IEnumerable<Person>> changed, bool checkboxColumn = false, bool checkboxOnly = false,
                                                        PixataGridSelectAllMode selectAllMode = PixataGridSelectAllMode.Current, IEnumerable<Person>? data = null, Func<Person, object?>? key = null) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, data ?? People())
        .Add(g => g.SelectionMode, mode)
        .Add(g => g.Pageable, true)
        .Add(g => g.PageSize, 4)
        .Add(g => g.SelectedItemsChanged, EventCallback.Factory.Create(this, changed))
        .Add(g => g.GridColumns, b => {
          if (checkboxColumn) {
            b.OpenComponent<PixataGridCheckboxColumn<Person>>(0);
            b.AddAttribute(1, "CheckBoxOnlySelection", checkboxOnly);
            b.AddAttribute(2, "SelectAllMode", selectAllMode);
            b.CloseComponent();
          }
          b.OpenComponent<PixataGridColumn<Person>>(10);
          b.AddAttribute(11, "Field", nameof(Person.Name));
          b.CloseComponent();
        });
      if (key is not null) {
        p.Add(g => g.ItemKey, key);
      }
    });

  private static string Selected(IEnumerable<Person> people) =>
    string.Join(",", people.Select(p => p.Name));

  private static void ClickRow(IRenderedFragment grid, int row, MouseEventArgs? e = null) =>
    grid.FindAll("tbody tr")[row].QuerySelectorAll("td").Last().Click(e ?? new MouseEventArgs());

  [TestMethod]
  public void Multiple_ClickCtrlAndShift_WorkLikeTelerik() {
    IEnumerable<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Multiple, s => selected = s);

    ClickRow(grid, 1);
    Assert.AreEqual("P2", Selected(selected));
    ClickRow(grid, 3, new() { CtrlKey = true });
    Assert.AreEqual("P2,P4", Selected(selected));
    ClickRow(grid, 0, new() { ShiftKey = true });
    Assert.AreEqual("P1,P2,P3,P4", Selected(selected));
    ClickRow(grid, 1, new() { CtrlKey = true });
    Assert.AreEqual("P1,P3,P4", Selected(selected));
    Assert.IsTrue(grid.FindAll("tbody tr")[0].ClassList.Contains("pixata-blazor-grid-selected"));
    Assert.AreEqual("false", grid.FindAll("tbody tr")[1].GetAttribute("aria-selected"));
  }

  [TestMethod]
  public void Single_SelectsOneRowAndCtrlClickDeselects() {
    IEnumerable<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Single, s => selected = s);

    ClickRow(grid, 1);
    ClickRow(grid, 2, new() { CtrlKey = true });
    Assert.AreEqual("P3", Selected(selected));
    ClickRow(grid, 2, new() { CtrlKey = true });
    Assert.AreEqual("", Selected(selected));
  }

  [TestMethod]
  public void None_DoesNothing() {
    IEnumerable<Person>? selected = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.None, s => selected = s);

    ClickRow(grid, 1);

    Assert.IsNull(selected);
    Assert.IsNull(grid.Find("tbody tr").GetAttribute("aria-selected"));
  }

  [TestMethod]
  public void CheckboxColumn_TogglesRowsAndSelectsAllOnThePage() {
    IEnumerable<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Multiple, s => selected = s, checkboxColumn: true);

    grid.FindAll("tbody input")[1].Click();
    grid.FindAll("tbody input")[2].Click();
    Assert.AreEqual("P2,P3", Selected(selected));
    Assert.IsTrue(grid.Find("th input").ClassList.Contains("pixata-blazor-checkbox-indeterminate"));

    grid.Find("th input").Click();
    Assert.AreEqual("P2,P3,P1,P4", Selected(selected));
    Assert.IsTrue(grid.Find("th input").HasAttribute("checked"));

    grid.Find("th input").Click();
    Assert.AreEqual("", Selected(selected));
    Assert.AreEqual("width: 65px", grid.Find("col").GetAttribute("style"));
  }

  [TestMethod]
  public void CheckboxColumn_SelectAllModeAll_SelectsEveryPage() {
    IEnumerable<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Multiple, s => selected = s, checkboxColumn: true, selectAllMode: PixataGridSelectAllMode.All);

    grid.Find("th input").Click();

    Assert.AreEqual("P1,P2,P3,P4,P5,P6", Selected(selected));
  }

  [TestMethod]
  public void CheckboxOnlySelection_IgnoresRowClicks() {
    IEnumerable<Person>? selected = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Multiple, s => selected = s, checkboxColumn: true, checkboxOnly: true);

    ClickRow(grid, 1);
    Assert.IsNull(selected);

    grid.FindAll("tbody input")[1].Click();
    Assert.AreEqual("P2", Selected(selected!));
  }

  [TestMethod]
  public void Selection_SurvivesPaging() {
    IEnumerable<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Multiple, s => selected = s);

    ClickRow(grid, 0);
    grid.FindAll(".pixata-blazor-grid-pager-number").Single(b => b.TextContent.Trim() == "2").Click();
    ClickRow(grid, 0, new() { CtrlKey = true });

    Assert.AreEqual("P1,P5", Selected(selected));
  }

  [TestMethod]
  public void ItemKey_MatchesItemsThatAreNotTheSameObjects() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.SelectionMode, PixataGridSelectionMode.Multiple)
      .Add(g => g.SelectedItems, [new Person(2, "Someone else")])
      .Add(g => g.ItemKey, person => person.Id)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Name));
        b.CloseComponent();
      }));

    Assert.IsTrue(grid.FindAll("tbody tr")[1].ClassList.Contains("pixata-blazor-grid-selected"));
  }

  [TestMethod]
  public void CellSelection_ClickAndShiftSelectARectangle() {
    IEnumerable<PixataGridSelectedCell<Person>> cells = [];
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.SelectionMode, PixataGridSelectionMode.Multiple)
      .Add(g => g.SelectedCellsChanged, EventCallback.Factory.Create<IEnumerable<PixataGridSelectedCell<Person>>>(this, c => cells = c))
      .Add(g => g.GridSettings, b => {
        b.OpenComponent<PixataGridSelectionSettings<Person>>(0);
        b.AddAttribute(1, "SelectionType", PixataGridSelectionType.Cell);
        b.CloseComponent();
      })
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Id));
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(2);
        b.AddAttribute(3, "Field", nameof(Person.Name));
        b.CloseComponent();
      }));

    grid.FindAll("tbody tr")[0].QuerySelectorAll("td")[0].Click();
    grid.FindAll("tbody tr")[1].QuerySelectorAll("td")[1].Click(new MouseEventArgs { ShiftKey = true });

    CollectionAssert.AreEqual(new[] { "P1 Id", "P1 Name", "P2 Id", "P2 Name" }, cells.Select(c => $"{c.DataItem.Name} {c.ColumnField}").ToArray());
    Assert.IsTrue(grid.FindAll("tbody tr")[1].QuerySelectorAll("td")[1].ClassList.Contains("pixata-blazor-grid-selected"));
    Assert.IsFalse(grid.FindAll("tbody tr")[2].QuerySelectorAll("td")[0].ClassList.Contains("pixata-blazor-grid-selected"));
  }

  [TestMethod]
  public void DragToSelect_SelectsTheRowsDraggedOver() {
    IEnumerable<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSelectionMode.Multiple, s => selected = s);

    grid.FindAll("tbody tr")[0].QuerySelector("td")!.MouseDown(new MouseEventArgs { Button = 0, Buttons = 1 });
    grid.FindAll("tbody tr")[2].QuerySelector("td")!.MouseOver(new MouseEventArgs { Buttons = 1 });
    grid.Find(".pixata-blazor-grid").MouseUp();

    Assert.AreEqual("P1,P2,P3", Selected(selected));
  }
}
