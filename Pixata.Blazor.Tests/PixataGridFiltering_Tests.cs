using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridFiltering_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  public enum Pet {
    Cat,
    Dog
  }

  public class Person {
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool Manager { get; set; }
    public Pet Pet { get; set; }
    public DateTime Born { get; set; }
  }

  private static List<Person> People() => [
    new() { Name = "Alice", Age = 30, Manager = true, Pet = Pet.Cat, Born = new(1980, 1, 1) },
    new() { Name = "Bob", Age = 40, Pet = Pet.Dog, Born = new(1990, 1, 1) },
    new() { Name = "Carol", Age = 50, Manager = true, Pet = Pet.Dog, Born = new(2000, 1, 1) },
    new() { Name = "Dave", Age = 30, Pet = Pet.Cat, Born = new(1970, 1, 1) }
  ];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  private static void Columns(RenderTreeBuilder b, Action<RenderTreeBuilder, string, int>? extra = null) {
    int seq = 0;
    foreach (string field in new[] { "Name", "Age", "Manager", "Pet", "Born" }) {
      b.OpenComponent<PixataGridColumn<Person>>(seq++);
      b.AddAttribute(seq++, "Field", field);
      extra?.Invoke(b, field, seq);
      seq += 10;
      b.CloseComponent();
    }
  }

  private IRenderedComponent<PixataGrid<Person>> Render(PixataGridFilterMode mode, RenderFragment? toolbar = null, Action<RenderTreeBuilder, string, int>? extra = null) =>
    _ctx.Render<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, People())
        .Add(g => g.FilterMode, mode)
        .Add(g => g.GridColumns, b => Columns(b, extra));
      if (toolbar is not null) {
        p.Add(g => g.GridToolBarTemplate, toolbar);
      }
    });

  private static string Names<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    string.Join(",", grid.FindAll("tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim()));

  private static IElement FilterCell<TComponent>(IRenderedComponent<TComponent> grid, int column) where TComponent : IComponent =>
    grid.FindAll(".pixata-blazor-grid-filter-cell")[column];

  private static void Pick<TComponent>(IRenderedComponent<TComponent> grid, IElement dropdown, string text) where TComponent : IComponent {
    dropdown.QuerySelector(".pixata-blazor-grid-dropdown-button")!.Click();
    grid.FindAll(".pixata-blazor-grid-dropdown-item").Single(li => li.TextContent.Trim() == text).Click();
  }

  [TestMethod]
  public void FilterRow_TextFilters_AfterAShortDelay() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow);

    FilterCell(grid, 0).QuerySelector("input")!.Input("O");

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
    grid.WaitForAssertion(() => Assert.AreEqual("Bob,Carol", Names(grid)));
    Assert.IsFalse(FilterCell(grid, 0).QuerySelector(".pixata-blazor-grid-filter-clear")!.HasAttribute("disabled"));
  }

  [TestMethod]
  public void FilterRow_ChangingTheOperator_ChangesTheFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow);

    Pick(grid, FilterCell(grid, 1).QuerySelector(".pixata-blazor-grid-filter-operator")!, "Is greater than");
    FilterCell(grid, 1).QuerySelector("input")!.Input("35");
    grid.WaitForAssertion(() => Assert.AreEqual("Bob,Carol", Names(grid)));

  }

  [TestMethod]
  public void FilterRow_NumberFilter_IsANumericTextBoxWithTheGridsMessages() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow)
      .Add(g => g.Messages, new PixataGridMessages { IncreaseValue = "Plus", DecreaseValue = "Moins" })
      .Add(g => g.GridColumns, b => Columns(b)));

    IElement box = FilterCell(grid, 1).QuerySelector(".pixata-blazor-numerictextbox")!;
    Assert.AreEqual("Plus", box.QuerySelector(".pixata-blazor-numerictextbox-increase")!.GetAttribute("aria-label"));
    Assert.AreEqual("Moins", box.QuerySelector(".pixata-blazor-numerictextbox-decrease")!.GetAttribute("aria-label"));

    box.QuerySelector("input")!.Input("39");
    box.QuerySelector(".pixata-blazor-numerictextbox-increase")!.Click();
    grid.WaitForAssertion(() => Assert.AreEqual("Bob", Names(grid)));
    Assert.AreEqual("40", FilterCell(grid, 1).QuerySelector("input")!.GetAttribute("value"));
  }

  [TestMethod]
  public void FilterRow_Clear_RemovesTheFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow);
    Pick(grid, FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-editor")!, "is true");
    Assert.AreEqual("Alice,Carol", Names(grid));

    FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-clear")!.Click();

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
    Assert.IsTrue(FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-clear")!.HasAttribute("disabled"));
    Assert.AreEqual("(All)", FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-dropdown-text")!.TextContent.Trim());
  }

  [TestMethod]
  public void FilterRow_OperatorLists_MatchTelerik() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow);

    FilterCell(grid, 4).QuerySelector(".pixata-blazor-grid-filter-operator .pixata-blazor-grid-dropdown-button")!.Click();

    CollectionAssert.AreEqual(new[] { "Is equal to", "Is not equal to", "Is after or equal to", "Is after", "Is before or equal to", "Is before" },
      grid.FindAll(".pixata-blazor-grid-dropdown-item").Select(li => li.TextContent.Trim()).ToArray());
    // A boolean has no operator, just its values
    Assert.IsNull(FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-operator"));
  }

  [TestMethod]
  public void FilterRow_BooleanAndEnumDropdowns_Filter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow);

    Pick(grid, FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-editor")!, "is true");
    Assert.AreEqual("Alice,Carol", Names(grid));

    Pick(grid, FilterCell(grid, 3).QuerySelector(".pixata-blazor-grid-filter-editor")!, "Dog");
    Assert.AreEqual("Carol", Names(grid));
  }

  [TestMethod]
  public void FilterRow_NotFilterable_HasAnEmptyCell() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow, extra: (b, field, seq) => {
      if (field == "Age") {
        b.AddAttribute(seq, "Filterable", false);
      }
    });

    Assert.AreEqual(0, FilterCell(grid, 1).ChildElementCount);
  }

  [TestMethod]
  public void FilterMenu_TwoConditionsWithOr() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterMenu);

    grid.FindAll(".pixata-blazor-grid-filter-menu-button")[0].Click();
    IElement popup = grid.Find(".pixata-blazor-grid-filter-popup");
    Pick(grid, popup.QuerySelectorAll(".pixata-blazor-grid-filter-menu-container > .pixata-blazor-grid-dropdown")[0], "Starts with");
    grid.FindAll(".pixata-blazor-grid-filter-popup .pixata-blazor-grid-textbox input")[0].Input("a");
    grid.FindAll(".pixata-blazor-grid-filter-popup .pixata-blazor-grid-button-group button")[1].Click();
    Pick(grid, grid.Find(".pixata-blazor-grid-filter-popup").QuerySelectorAll(".pixata-blazor-grid-filter-menu-container > .pixata-blazor-grid-dropdown")[1], "Ends with");
    grid.FindAll(".pixata-blazor-grid-filter-popup .pixata-blazor-grid-textbox input")[1].Input("e");
    grid.Find(".pixata-blazor-grid-filter-popup form").Submit();

    Assert.AreEqual("Alice,Dave", Names(grid));
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-filter-popup").Count);
    Assert.IsTrue(grid.FindAll(".pixata-blazor-grid-filter-menu-button")[0].ClassList.Contains("pixata-blazor-grid-filter-active"));
  }

  [TestMethod]
  public void FilterMenu_ClickingTheIconDoesntSort() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.Sortable, true)
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterMenu)
      .Add(g => g.GridColumns, b => Columns(b)));

    grid.FindAll(".pixata-blazor-grid-filter-menu-button")[0].Click();

    Assert.IsNull(grid.Find("th").GetAttribute("aria-sort"));
    Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-filter-popup").Count);
  }

  [TestMethod]
  public void FilterMenu_CheckBoxList_FiltersByTheTickedValues() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterMenu, extra: (b, field, seq) => {
      if (field == "Age") {
        b.AddAttribute(seq, "FilterMenuType", PixataGridFilterMenuType.CheckBoxList);
      }
    });

    grid.FindAll(".pixata-blazor-grid-filter-menu-button")[1].Click();
    CollectionAssert.AreEqual(new[] { "Select All", "30", "40", "50" },
      grid.FindAll(".pixata-blazor-grid-multicheck-item").Select(li => li.TextContent.Trim()).ToArray());
    grid.FindAll(".pixata-blazor-grid-multicheck-item").Single(li => li.TextContent.Trim() == "30").QuerySelector("input")!.Change(true);
    grid.FindAll(".pixata-blazor-grid-multicheck-item").Single(li => li.TextContent.Trim() == "50").QuerySelector("input")!.Change(true);
    grid.Find(".pixata-blazor-grid-filter-popup form").Submit();

    Assert.AreEqual("Alice,Carol,Dave", Names(grid));
  }

  [TestMethod]
  public void FilterMenu_Clear_RemovesTheFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterMenu);
    grid.FindAll(".pixata-blazor-grid-filter-menu-button")[0].Click();
    grid.FindAll(".pixata-blazor-grid-filter-popup .pixata-blazor-grid-textbox input")[0].Input("o");
    grid.Find(".pixata-blazor-grid-filter-popup form").Submit();
    Assert.AreEqual("Bob,Carol", Names(grid));

    grid.FindAll(".pixata-blazor-grid-filter-menu-button")[0].Click();
    grid.FindAll(".pixata-blazor-grid-filter-popup button").Single(b => b.TextContent.Trim() == "Clear").Click();

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
  }

  [TestMethod]
  public void SearchBox_SearchesTextColumnsIgnoringCase() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.None, b => {
      b.OpenComponent<PixataGridSearchBox<Person>>(0);
      b.AddAttribute(1, "DebounceDelay", 1);
      b.CloseComponent();
    });

    grid.Find(".pixata-blazor-grid-toolbar .pixata-blazor-grid-search input").Input("AR");

    grid.WaitForAssertion(() => Assert.AreEqual("Carol", Names(grid)));
  }

  [TestMethod]
  public void Filtering_GoesBackToTheFirstPage() {
    int page = 0;
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.Pageable, true)
      .Add(g => g.PageSize, 1)
      .Add(g => g.Page, 3)
      .Add(g => g.PageChanged, EventCallback.Factory.Create<int>(this, n => page = n))
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow)
      .Add(g => g.GridColumns, b => Columns(b)));

    Pick(grid, FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-editor")!, "is false");

    Assert.AreEqual(1, page);
    Assert.AreEqual("Bob", Names(grid));
  }

  [TestMethod]
  public void FilterCellTemplate_CanApplyItsOwnFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow, extra: (b, field, seq) => {
      if (field == "Name") {
        b.AddAttribute(seq, "FilterCellTemplate", (RenderFragment<PixataGridFilterTemplateContext>)(context => builder => {
          builder.OpenElement(0, "button");
          builder.AddAttribute(1, "class", "custom");
          builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, async () => {
            context.Filter.Filters.Add(new("Name", GridFilterOperator.StartsWith, "d"));
            await context.FilterAsync();
          }));
          builder.CloseElement();
        }));
      }
    });

    grid.Find(".custom").Click();

    Assert.AreEqual("Dave", Names(grid));
  }

  [TestMethod]
  public void FilterCellTemplate_CanReplaceItsFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridFilterMode.FilterRow, extra: (b, field, seq) => {
      if (field == "Name") {
        b.AddAttribute(seq, "FilterCellTemplate", (RenderFragment<PixataGridFilterTemplateContext>)(context => builder => {
          builder.OpenElement(0, "button");
          builder.AddAttribute(1, "class", "custom");
          builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, async () => {
            context.Filter = new(GridFilterLogic.Or, [
              new("Name", GridFilterOperator.StartsWith, "d"),
              new("Name", GridFilterOperator.StartsWith, "b")
            ]);
            await context.FilterAsync();
          }));
          builder.CloseElement();
        }));
      }
    });

    grid.Find(".custom").Click();

    Assert.AreEqual("Bob,Dave", Names(grid));
  }
}
