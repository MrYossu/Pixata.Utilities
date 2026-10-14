using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridAccessibility_Tests {
  private BunitContext _ctx = null!;

  public record Person(int Id, string Name);

  private static readonly List<Person> People = [.. Enumerable.Range(1, 12).Select(n => new Person(n, $"Person {n}"))];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static void Column(RenderTreeBuilder b, string field) {
    b.OpenComponent<PixataGridColumn<Person>>(0);
    b.AddAttribute(1, "Field", field);
    b.CloseComponent();
  }

  private IRenderedComponent<PixataGrid<Person>> Render(Action<ComponentParameterCollectionBuilder<PixataGrid<Person>>>? more = null, IEnumerable<Person>? data = null) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, data ?? People)
        .Add(g => g.GridColumns, b => {
          Column(b, nameof(Person.Id));
          Column(b, nameof(Person.Name));
        });
      more?.Invoke(p);
    });

  // ---------- Translation ----------

  private static readonly PixataGridMessages French = new() {
    NoRecords = "Aucun enregistrement.",
    PagerInfoFormat = "{0} - {1} sur {2} éléments",
    Contains = "Contient"
  };

  [TestMethod]
  public void Messages_ReplaceTheGridsText() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.Messages, French)
      .Add(g => g.Pageable, true)
      .Add(g => g.PageSize, 5)
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterMenu));

    Assert.AreEqual("1 - 5 sur 12 éléments", grid.Find(".pixata-blazor-grid-pager-info").TextContent.Trim());

    // The Name column's filter menu starts with "Contains"
    grid.FindAll(".pixata-blazor-grid-filter-menu-button")[1].Click();

    StringAssert.Contains(grid.Find(".pixata-blazor-grid-filter-popup").TextContent, "Contient");
  }

  [TestMethod]
  public void Messages_CanBeRegisteredForEveryGrid() {
    _ctx.Services.AddSingleton(French);

    IRenderedComponent<PixataGrid<Person>> grid = Render(data: []);

    Assert.AreEqual("Aucun enregistrement.", grid.Find(".pixata-blazor-grid-norecords").TextContent.Trim());
  }

  [TestMethod]
  public void WithoutMessages_TheTextIsEnglish() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(data: []);

    Assert.AreEqual(PixataGridMessages.Default.NoRecords, grid.Find(".pixata-blazor-grid-norecords").TextContent.Trim());
  }

  // ---------- Highlighting ----------

  [TestMethod]
  public void HighlightedItemsAndCells_AreMarked() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.HighlightedItems, [People[1]])
      .Add(g => g.HighlightedCells, [new(People[0], nameof(Person.Name))]));

    List<AngleSharp.Dom.IElement> rows = [.. grid.FindAll("tbody tr")];
    Assert.IsFalse(rows[0].ClassList.Contains("pixata-blazor-grid-highlighted"));
    Assert.IsTrue(rows[1].ClassList.Contains("pixata-blazor-grid-highlighted"));
    Assert.IsTrue(rows[0].Children[1].ClassList.Contains("pixata-blazor-grid-highlighted"));
    Assert.IsFalse(rows[0].Children[0].ClassList.Contains("pixata-blazor-grid-highlighted"));
  }

  // ---------- Keyboard navigation ----------

  [TestMethod]
  public void Navigable_MakesTheTableAnAriaGrid() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.Navigable, true)
      .Add(g => g.Sortable, true)
      .Add(g => g.Pageable, true)
      .Add(g => g.PageSize, 5));

    AngleSharp.Dom.IElement table = grid.Find("table");
    Assert.AreEqual("grid", table.GetAttribute("role"));
    Assert.AreEqual("13", table.GetAttribute("aria-rowcount"));
    Assert.AreEqual("2", table.GetAttribute("aria-colcount"));
    Assert.AreEqual("2", grid.Find("tbody tr").GetAttribute("aria-rowindex"));
    Assert.IsNotNull(grid.Find("tbody td").GetAttribute("data-pixata-column-key"));
    // The JavaScript looks after the tab stops, so the sortable headers don't have one of their own
    Assert.IsNull(grid.Find("thead th").GetAttribute("tabindex"));
  }

  [TestMethod]
  public void NotNavigable_IsAPlainTable() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Assert.IsNull(grid.Find("table").GetAttribute("role"));
    Assert.IsNull(grid.Find("tbody tr").GetAttribute("data-pixata-row-index"));
  }

  [TestMethod]
  public void PageDown_GoesToTheNextPage() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.Navigable, true).Add(g => g.Pageable, true).Add(g => g.PageSize, 5));

    grid.InvokeAsync(() => grid.Instance.NavigationKey("PageDown", -1, null));

    Assert.AreEqual("6", grid.Find("tbody td").TextContent.Trim());

    grid.InvokeAsync(() => grid.Instance.NavigationKey("PageUp", -1, null));
    grid.InvokeAsync(() => grid.Instance.NavigationKey("PageUp", -1, null));

    Assert.AreEqual("1", grid.Find("tbody td").TextContent.Trim());
  }

  [TestMethod]
  public void Space_SelectsTheRow() {
    List<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.Navigable, true)
      .Add(g => g.SelectionMode, PixataGridSelectionMode.Multiple)
      .Add(g => g.SelectedItemsChanged, EventCallback.Factory.Create<IEnumerable<Person>>(this, items => selected = [.. items])));

    grid.InvokeAsync(() => grid.Instance.NavigationKey(" ", 2, null));
    grid.InvokeAsync(() => grid.Instance.NavigationKey(" ", 4, null));

    CollectionAssert.AreEqual(new[] { 3, 5 }, selected.Select(p => p.Id).ToArray());
  }

  [TestMethod]
  public void Enter_EditsTheRow() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.Navigable, true).Add(g => g.EditMode, PixataGridEditMode.Inline));

    grid.InvokeAsync(() => grid.Instance.NavigationKey("Enter", 1, null));

    Assert.AreEqual(2, grid.Instance.EditItem!.Id);
  }

  [TestMethod]
  public void AltDown_OpensTheHeadersMenu() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.Navigable, true).Add(g => g.FilterMode, PixataGridFilterMode.FilterMenu));
    string key = grid.FindAll("thead th")[1].GetAttribute("data-pixata-column-key")!;

    grid.InvokeAsync(() => grid.Instance.NavigationKey("Menu", -1, key));

    Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-filter-popup").Count);
  }

  // ---------- Small screens ----------

  [TestMethod]
  public void AdaptiveMode_MarksTheGridAndGivesTheMenusATitleBar() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.AdaptiveMode, PixataGridAdaptiveMode.Auto)
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterMenu));

    Assert.IsTrue(grid.Find(".pixata-blazor-grid").ClassList.Contains("pixata-blazor-grid-adaptive"));

    grid.Find(".pixata-blazor-grid-filter-menu-button").Click();
    grid.Find(".pixata-blazor-grid-adaptive-header button").Click();

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-filter-popup").Count);
  }
}
