using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridColumns_Tests {
  private BunitContext _ctx = null!;

  public record Person(int Id, string First, string Last, string Email, int Age);

  private static readonly List<Person> People = [new(1, "Ann", "Smith", "ann@example.com", 30), new(2, "Bob", "Jones", "bob@example.com", 40)];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  // A column, with any extra parameters
  private static RenderFragment Column(string? field, params (string Name, object? Value)[] parameters) =>
    b => {
      b.OpenComponent<PixataGridColumn<Person>>(0);
      if (field is not null) {
        b.AddAttribute(1, "Field", field);
      }
      foreach ((string name, object? value) in parameters) {
        b.AddAttribute(2, name, value);
      }
      b.CloseComponent();
    };

  private static RenderFragment Columns(params RenderFragment[] columns) =>
    b => {
      foreach (RenderFragment column in columns) {
        b.AddContent(0, column);
      }
    };

  private IRenderedComponent<PixataGrid<Person>> Render(RenderFragment columns, Action<ComponentParameterCollectionBuilder<PixataGrid<Person>>>? more = null) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, People).Add(g => g.GridColumns, columns);
      more?.Invoke(p);
    });

  private static List<string> Titles(IRenderedFragment grid) =>
    [.. grid.FindAll("thead tr:first-child th").Select(th => th.TextContent.Trim())];

  private static List<string> FirstRow(IRenderedFragment grid) =>
    [.. grid.FindAll("tbody tr:first-child td").Select(td => td.TextContent.Trim())];

  // ---------- Multi-column headers ----------

  [TestMethod]
  public void ColumnsInsideAColumn_ShareAHeaderSpanningTheirs() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(nameof(Person.Id)),
      Column(null, ("Title", "Name"), ("Columns", Columns(Column(nameof(Person.First)), Column(nameof(Person.Last))))),
      Column(nameof(Person.Age))));

    List<List<string>> rows = [.. grid.FindAll("thead tr").Select(tr => tr.Children.Select(th => $"{th.TextContent.Trim()}:{th.GetAttribute("colspan") ?? "1"}x{th.GetAttribute("rowspan") ?? "1"}").ToList())];

    CollectionAssert.AreEqual(new[] { "Id:1x2", "Name:2x1", "Age:1x2" }, rows[0]);
    CollectionAssert.AreEqual(new[] { "First:1x1", "Last:1x1" }, rows[1]);
    CollectionAssert.AreEqual(new[] { "1", "Ann", "Smith", "30" }, FirstRow(grid));
    Assert.AreEqual(4, grid.FindAll("col").Count);
  }

  [TestMethod]
  public void ColumnsCanBeNestedMoreThanOneLevel() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(null, ("Title", "All"), ("Columns", Columns(
        Column(nameof(Person.Id)),
        Column(null, ("Title", "Name"), ("Columns", Columns(Column(nameof(Person.First)), Column(nameof(Person.Last))))))))));

    Assert.AreEqual(3, grid.FindAll("thead tr").Count);
    Assert.AreEqual("3", grid.Find("thead tr th").GetAttribute("colspan"));
    CollectionAssert.AreEqual(new[] { "1", "Ann", "Smith" }, FirstRow(grid));
  }

  [TestMethod]
  public void AColumnWhoseColumnsAreAllHidden_IsLeftOut() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(nameof(Person.Id)),
      Column(null, ("Title", "Name"), ("Columns", Columns(Column(nameof(Person.First), ("Visible", (bool?)false)))))));

    CollectionAssert.AreEqual(new[] { "Id" }, Titles(grid));
    Assert.AreEqual(1, grid.FindAll("thead tr").Count);
  }

  // ---------- Frozen columns ----------

  [TestMethod]
  public void FrozenColumns_StickBetweenTheOtherFrozenColumns() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(nameof(Person.Id), ("Width", "80px"), ("Locked", true)),
      Column(nameof(Person.First), ("Width", "150px")),
      Column(nameof(Person.Last), ("Width", "120px"), ("Locked", true)),
      Column(nameof(Person.Age), ("Width", "100px"), ("Locked", true))));

    IRefreshableElementCollection<IElement> cells = grid.FindAll("tbody tr:first-child td");
    Assert.AreEqual("left: 0px; right: 220px;", cells[0].GetAttribute("style"));
    Assert.IsNull(cells[1].GetAttribute("style"));
    Assert.AreEqual("left: 80px; right: 100px;", cells[2].GetAttribute("style"));
    Assert.AreEqual("left: 200px; right: 0px;", cells[3].GetAttribute("style"));
    Assert.IsTrue(cells[0].ClassList.Contains("pixata-blazor-grid-locked"));
    Assert.IsTrue(grid.FindAll("thead th")[3].ClassList.Contains("pixata-blazor-grid-locked"));
  }

  [TestMethod]
  public void FrozenColumns_NeedWidthsInPixels() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(nameof(Person.Id), ("Width", "80px"), ("Locked", true)),
      Column(nameof(Person.First), ("Width", "20%"), ("Locked", true))));

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-locked").Count);
  }

  // ---------- Resizing ----------

  [TestMethod]
  public void Resizable_GivesResizableColumnsAHandle() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(nameof(Person.Id), ("MinResizableWidth", (double?)50), ("MaxResizableWidth", (double?)300)),
      Column(nameof(Person.First), ("Resizable", false))), p => p.Add(g => g.Resizable, true));

    IElement handle = grid.Find(".pixata-blazor-grid-column-resizer");
    Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-column-resizer").Count);
    Assert.AreEqual("50", handle.GetAttribute("data-pixata-min"));
    Assert.AreEqual("300", handle.GetAttribute("data-pixata-max"));
  }

  [TestMethod]
  public void NotResizable_HasNoHandles() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(Column(nameof(Person.Id))));

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-column-resizer").Count);
  }

  [TestMethod]
  public void AfterAResize_EveryColumnHasAWidthAndTheTableIsAsWideAsThem() {
    PixataGridColumnResizeEventArgs<Person>? resized = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(Column(nameof(Person.Id)), Column(nameof(Person.First))), p => p
      .Add(g => g.Resizable, true)
      .Add(g => g.OnColumnResize, EventCallback.Factory.Create<PixataGridColumnResizeEventArgs<Person>>(this, args => resized = args)));
    string[] keys = [.. grid.FindAll("col").Select(c => c.GetAttribute("data-pixata-column-key")!)];

    grid.InvokeAsync(() => grid.Instance.ColumnsResized(keys[0], [new(keys[0], 200), new(keys[1], 150.5)]));

    CollectionAssert.AreEqual(new[] { "width: 200px", "width: 150.5px" }, grid.FindAll("col").Select(c => c.GetAttribute("style")).ToArray());
    Assert.AreEqual("width: 350.5px;", grid.Find("table").GetAttribute("style"));
    Assert.AreEqual("Id", resized!.Column.HeaderText);
    Assert.AreEqual(200, resized.Width);
  }

  [TestMethod]
  public void AutoFitColumn_SetsTheWidthsTheJavaScriptMeasures() {
    BunitJSModuleInterop module = _ctx.JSInterop.SetupModule(PixataBlazorScript.Path);
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(Column(nameof(Person.Id), ("Id", "id")), Column(nameof(Person.First))));
    string[] keys = [.. grid.FindAll("col").Select(c => c.GetAttribute("data-pixata-column-key")!)];
    module.Setup<PixataGridColumnWidth[]?>("autoFitColumns", i => ((string[])i.Arguments[1]!).SequenceEqual([keys[0]]))
      .SetResult([new(keys[0], 42), new(keys[1], 100)]);

    grid.InvokeAsync(() => grid.Instance.AutoFitColumn("id"));

    Assert.AreEqual("width: 42px", grid.Find("col").GetAttribute("style"));
  }

  // ---------- Reordering ----------

  [TestMethod]
  public void Reorderable_MarksTheHeadersThatCanBeMoved() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(Column(nameof(Person.Id)), Column(nameof(Person.First), ("Reorderable", false))), p => p.Add(g => g.Reorderable, true));

    IRefreshableElementCollection<IElement> headers = grid.FindAll("thead th");
    Assert.IsNotNull(headers[0].GetAttribute("data-pixata-reorder-key"));
    Assert.IsNull(headers[1].GetAttribute("data-pixata-reorder-key"));
  }

  [TestMethod]
  public void DroppingAHeader_MovesTheColumn() {
    PixataGridColumnReorderEventArgs<Person>? moved = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(Column(nameof(Person.Id)), Column(nameof(Person.First)), Column(nameof(Person.Last))), p => p
      .Add(g => g.Reorderable, true)
      .Add(g => g.OnColumnReorder, EventCallback.Factory.Create<PixataGridColumnReorderEventArgs<Person>>(this, args => moved = args)));
    string[] keys = [.. grid.FindAll("thead th").Select(th => th.GetAttribute("data-pixata-reorder-key")!)];

    grid.InvokeAsync(() => grid.Instance.ColumnDrop(keys[2], keys[0], true));

    CollectionAssert.AreEqual(new[] { "Last", "Id", "First" }, Titles(grid));
    CollectionAssert.AreEqual(new[] { "Smith", "1", "Ann" }, FirstRow(grid));
    Assert.AreEqual(2, moved!.OldIndex);
    Assert.AreEqual(0, moved.NewIndex);

    grid.InvokeAsync(() => grid.Instance.ColumnDrop(keys[2], keys[1], false));

    CollectionAssert.AreEqual(new[] { "Id", "First", "Last" }, Titles(grid));
  }

  [TestMethod]
  public void AColumnCanOnlyBeMovedAmongTheColumnsItIsWith() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(
      Column(nameof(Person.Id)),
      Column(null, ("Title", "Name"), ("Columns", Columns(Column(nameof(Person.First)), Column(nameof(Person.Last)))))), p => p.Add(g => g.Reorderable, true));
    IElement first = grid.FindAll("thead tr")[1].Children[0];
    string idKey = grid.Find("thead th").GetAttribute("data-pixata-reorder-key")!;

    grid.InvokeAsync(() => grid.Instance.ColumnDrop(first.GetAttribute("data-pixata-reorder-key")!, idKey, true));

    CollectionAssert.AreEqual(new[] { "1", "Ann", "Smith" }, FirstRow(grid));
    Assert.AreNotEqual(first.GetAttribute("data-pixata-reorder-scope"), grid.Find("thead th").GetAttribute("data-pixata-reorder-scope"));
  }

  // ---------- The column menu ----------

  private IRenderedComponent<PixataGrid<Person>> RenderWithMenu(RenderFragment? settings = null) =>
    Render(Columns(
      Column(nameof(Person.Id), ("Width", "80px")),
      Column(nameof(Person.First), ("Width", "100px")),
      Column(nameof(Person.Last), ("VisibleInColumnChooser", false)),
      Column(nameof(Person.Age), ("ShowColumnMenu", false))), p => {
        p.Add(g => g.ShowColumnMenu, true).Add(g => g.Sortable, true).Add(g => g.FilterMode, PixataGridFilterMode.FilterMenu).Add(g => g.Groupable, true);
        if (settings is not null) {
          p.Add(g => g.GridSettings, settings);
        }
      });

  private static RenderFragment AllMenuSettings =>
    b => {
      b.OpenComponent<PixataGridColumnMenuSettings<Person>>(0);
      b.AddAttribute(1, "Groupable", true);
      b.AddAttribute(2, "Reorderable", true);
      b.CloseComponent();
    };

  private static void OpenMenu(IRenderedFragment grid, int column) =>
    grid.FindAll("thead th:not(.pixata-blazor-grid-group-cell)")[column].QuerySelector(".pixata-blazor-grid-column-menu-button")!.Click();

  private static List<string> MenuItems(IRenderedFragment grid) =>
    [.. grid.FindAll(".pixata-blazor-grid-column-menu .pixata-blazor-grid-columnmenu-item").Select(i => i.TextContent.Trim() + (i.GetAttribute("aria-disabled") is null ? "" : " (disabled)"))];

  private static void ClickItem(IRenderedFragment grid, string text) =>
    grid.FindAll(".pixata-blazor-grid-column-menu .pixata-blazor-grid-columnmenu-item").First(i => i.TextContent.Trim() == text).Click();

  [TestMethod]
  public void ShowColumnMenu_ReplacesTheFilterButtonWithAMenuButton() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();

    Assert.AreEqual(3, grid.FindAll(".pixata-blazor-grid-column-menu-button").Count);
    // A column without a column menu still has its filter menu
    Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-filter-menu-button").Count);
    Assert.IsNotNull(grid.FindAll("thead th")[3].QuerySelector(".pixata-blazor-grid-filter-menu-button"));
  }

  [TestMethod]
  public void TheMenu_OffersWhatTheSettingsAllow() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();
    OpenMenu(grid, 0);

    CollectionAssert.AreEqual(new[] { "Sort Ascending", "Sort Descending", "Columns", "Filter", "Set column position" }, MenuItems(grid));

    grid.Find(".pixata-blazor-grid-column-menu").QuerySelectorAll("[aria-expanded]").Last().Click();

    // Moving isn't allowed by default
    CollectionAssert.AreEqual(new[] { "Lock", "Unlock (disabled)" }, MenuItems(grid).Skip(5).ToArray());
  }

  [TestMethod]
  public void TheMenu_WithEverythingAllowed() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu(AllMenuSettings);
    OpenMenu(grid, 0);
    ClickItem(grid, "Set column position");

    CollectionAssert.AreEqual(new[] { "Sort Ascending", "Sort Descending", "Columns", "Filter", "Group column", "Set column position", "Lock", "Unlock (disabled)", "Move Previous (disabled)", "Move Next" }, MenuItems(grid));
  }

  [TestMethod]
  public void SortingFromTheMenu_SortsAndClosesIt() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();
    OpenMenu(grid, 0);

    ClickItem(grid, "Sort Descending");

    Assert.AreEqual("descending", grid.FindAll("thead th")[0].GetAttribute("aria-sort"));
    Assert.AreEqual("2", FirstRow(grid)[0]);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-column-menu").Count);

    // Picking the same sort again removes it
    OpenMenu(grid, 0);
    ClickItem(grid, "Sort Descending");

    Assert.IsNull(grid.FindAll("thead th")[0].GetAttribute("aria-sort"));
  }

  [TestMethod]
  public void TheColumnChooser_HidesColumnsWhenApplied() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();
    OpenMenu(grid, 0);
    ClickItem(grid, "Columns");

    IRefreshableElementCollection<IElement> choices = grid.FindAll(".pixata-blazor-grid-column-list-item");
    CollectionAssert.AreEqual(new[] { "Id", "First", "Age" }, choices.Select(c => c.TextContent.Trim()).ToArray());

    grid.FindAll(".pixata-blazor-grid-column-list-item input")[1].Change(false);

    // Nothing changes until Apply is clicked
    CollectionAssert.AreEqual(new[] { "Id", "First", "Last", "Age" }, Titles(grid));

    grid.Find(".pixata-blazor-grid-columnmenu-actions .pixata-blazor-button-primary").Click();

    CollectionAssert.AreEqual(new[] { "Id", "Last", "Age" }, Titles(grid));
  }

  [TestMethod]
  public void TheColumnChooser_WontHideTheLastColumn() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();
    OpenMenu(grid, 0);
    ClickItem(grid, "Columns");

    grid.FindAll(".pixata-blazor-grid-column-list-item input")[0].Change(false);
    grid.FindAll(".pixata-blazor-grid-column-list-item input")[2].Change(false);

    Assert.IsTrue(grid.FindAll(".pixata-blazor-grid-column-list-item input")[1].HasAttribute("disabled"));
  }

  [TestMethod]
  public void LockingFromTheMenu_FreezesTheColumnAfterTheOtherFrozenOnes() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();
    OpenMenu(grid, 1);
    ClickItem(grid, "Set column position");
    ClickItem(grid, "Lock");

    CollectionAssert.AreEqual(new[] { "First", "Id", "Last", "Age" }, Titles(grid));
    Assert.AreEqual("left: 0px; right: 0px;", grid.FindAll("thead th")[0].GetAttribute("style"));

    OpenMenu(grid, 0);
    ClickItem(grid, "Set column position");
    ClickItem(grid, "Unlock");

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-locked").Count);
  }

  [TestMethod]
  public void AColumnLockedByThePage_CannotBeUnlocked() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(Columns(Column(nameof(Person.Id), ("Width", "80px"), ("Locked", true))), p => p.Add(g => g.ShowColumnMenu, true));
    OpenMenu(grid, 0);
    ClickItem(grid, "Set column position");

    CollectionAssert.AreEqual(new[] { "Lock (disabled)", "Unlock (disabled)" }, MenuItems(grid).Skip(2).ToArray());
  }

  [TestMethod]
  public void MovingFromTheMenu_MovesTheColumn() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu(AllMenuSettings);
    OpenMenu(grid, 0);
    ClickItem(grid, "Set column position");
    ClickItem(grid, "Move Next");

    CollectionAssert.AreEqual(new[] { "First", "Id", "Last", "Age" }, Titles(grid));
  }

  [TestMethod]
  public void GroupingFromTheMenu_GroupsAndUngroups() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu(AllMenuSettings);
    OpenMenu(grid, 1);
    ClickItem(grid, "Group column");

    Assert.AreEqual("First", grid.Find(".pixata-blazor-grid-group-chip").TextContent.Trim());

    OpenMenu(grid, 1);
    ClickItem(grid, "Ungroup column");

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-group-chip").Count);
  }

  [TestMethod]
  public void TheFilterSection_ShowsTheFilterForm() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithMenu();
    OpenMenu(grid, 1);
    ClickItem(grid, "Filter");

    Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-column-menu form.pixata-blazor-grid-filter-menu").Count);

    // Only one section is open at a time
    ClickItem(grid, "Columns");

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-column-menu form").Count);
    Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-column-list").Count);
  }
}
