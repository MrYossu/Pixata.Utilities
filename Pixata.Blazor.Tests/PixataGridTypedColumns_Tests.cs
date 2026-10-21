using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridTypedColumns_Tests {
  private BunitContext _ctx = null!;

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
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static void Column<TColumn>(RenderTreeBuilder b, string field, Action<RenderTreeBuilder>? extra = null) where TColumn : IComponent {
    b.OpenComponent<TColumn>(0);
    b.AddAttribute(1, "Field", field);
    extra?.Invoke(b);
    b.CloseComponent();
  }

  private IRenderedComponent<PixataGrid<Person>> Render(bool showClearButton = false) =>
    _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow)
      .Add(g => g.GridColumns, b => {
        Column<PixataTextGridColumn<Person>>(b, "Name");
        Column<PixataNumericGridColumn<Person>>(b, "Age");
        Column<PixataGridColumn<Person>>(b, "Manager", c => c.AddAttribute(2, "FilterCellTemplate", PixataGridBoolFilter.Filter(showClearButton)));
        Column<PixataEnumGridColumn<Person>>(b, "Pet");
        Column<PixataDateGridColumn<Person>>(b, "Born");
      }));

  private static string Names(IRenderedComponent<PixataGrid<Person>> grid) =>
    string.Join(",", grid.FindAll("tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim()));

  private static IElement FilterCell(IRenderedComponent<PixataGrid<Person>> grid, int column) =>
    grid.FindAll(".pixata-blazor-grid-filter-cell")[column];

  // Opens the column's operator list, and returns the operators and the one selected
  private static (string Operators, string Selected) Operators(IRenderedComponent<PixataGrid<Person>> grid, int column) {
    FilterCell(grid, column).QuerySelector(".pixata-blazor-grid-filter-operator .pixata-blazor-grid-dropdown-button")!.Click();
    IElement cell = FilterCell(grid, column);
    return (string.Join(",", cell.QuerySelectorAll(".pixata-blazor-grid-dropdown-item").Select(li => li.TextContent.Trim())),
      cell.QuerySelector(".pixata-blazor-grid-dropdown-selected")!.TextContent.Trim());
  }

  [TestMethod]
  public void TextColumn_OffersItsOperators_StartingWithContains() =>
    Assert.AreEqual(("contains,starts with,ends with", "contains"), Operators(Render(), 0));

  [TestMethod]
  public void NumericColumn_OffersItsOperators_StartingWithEquals() =>
    Assert.AreEqual(("equals,less than,more than", "equals"), Operators(Render(), 1));

  [TestMethod]
  public void DateColumn_OffersItsOperators_StartingWithOn() =>
    Assert.AreEqual(("before,on,on or after", "on"), Operators(Render(), 4));

  [TestMethod]
  public void EnumColumn_HasNoOperatorOrClearButton() {
    IElement cell = FilterCell(Render(), 3);

    Assert.IsNull(cell.QuerySelector(".pixata-blazor-grid-filter-operator"));
    Assert.IsNull(cell.QuerySelector(".pixata-blazor-grid-filter-clear"));
    Assert.IsNotNull(cell.QuerySelector(".pixata-blazor-grid-filter-editor"));
  }

  // Opens the enum column's drop-down, and returns its items and the text on the button
  private static (string Items, string Text) EnumFilter(IRenderedComponent<PixataGrid<Person>> grid, int column) {
    FilterCell(grid, column).QuerySelector(".pixata-blazor-grid-dropdown-button")!.Click();
    IElement cell = FilterCell(grid, column);
    return (string.Join(",", cell.QuerySelectorAll(".pixata-blazor-grid-dropdown-item").Select(li => li.TextContent.Trim())),
      cell.QuerySelector(".pixata-blazor-grid-dropdown-text")!.TextContent.Trim());
  }

  private static void PickEnum(IRenderedComponent<PixataGrid<Person>> grid, int column, string text) {
    FilterCell(grid, column).QuerySelector(".pixata-blazor-grid-dropdown-button")!.Click();
    FilterCell(grid, column).QuerySelectorAll(".pixata-blazor-grid-dropdown-item").First(li => li.TextContent.Trim() == text).Click();
  }

  [TestMethod]
  public void EnumColumn_StartsWithAnAllEntry() =>
    Assert.AreEqual(("(All),Cat,Dog", "(All)"), EnumFilter(Render(), 3));

  [TestMethod]
  public void EnumColumn_AllEntryClearsTheFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    PickEnum(grid, 3, "Dog");
    Assert.AreEqual("Bob,Carol", Names(grid));
    PickEnum(grid, 3, "(All)");

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
    Assert.AreEqual("(All)", FilterCell(grid, 3).QuerySelector(".pixata-blazor-grid-dropdown-text")!.TextContent.Trim());
  }

  [TestMethod]
  public void EnumColumn_AllEntryCanBeLeftOut() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow)
      .Add(g => g.GridColumns, b => Column<PixataEnumGridColumn<Person>>(b, "Pet", c => c.AddAttribute(2, "AddAllEntry", false))));

    Assert.AreEqual(("Cat,Dog", "-Select value-"), EnumFilter(grid, 0));
  }

  [TestMethod]
  public void ShowFilterClearButton_HidesJustTheClearButton() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow)
      .Add(g => g.GridColumns, b => Column<PixataNumericGridColumn<Person>>(b, "Age", c => c.AddAttribute(2, "ShowFilterClearButton", false))));
    IElement cell = FilterCell(grid, 0);

    Assert.IsNotNull(cell.QuerySelector(".pixata-blazor-grid-filter-operator"));
    Assert.IsNull(cell.QuerySelector(".pixata-blazor-grid-filter-clear"));
  }

  [TestMethod]
  public void FilterOperators_CanBeSetOnAnyColumn() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow)
      .Add(g => g.GridColumns, b => Column<PixataGridColumn<Person>>(b, "Name",
        c => c.AddAttribute(2, "FilterOperators", PixataGridFilterOperators.StringOperators))));

    Assert.AreEqual("contains,starts with,ends with", Operators(grid, 0).Operators);
  }

  [TestMethod]
  public void BoolFilter_CyclesThroughTrueFalseAndAll() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    IElement Box() => FilterCell(grid, 2).QuerySelector("[role=checkbox]")!;

    Assert.AreEqual("mixed", Box().GetAttribute("aria-checked"));
    Box().Click();
    Assert.AreEqual("Alice,Carol", Names(grid));
    Assert.AreEqual("true", Box().GetAttribute("aria-checked"));
    Box().Click();
    Assert.AreEqual("Bob,Dave", Names(grid));
    Assert.AreEqual("false", Box().GetAttribute("aria-checked"));
    Box().Click();
    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
    Assert.AreEqual("mixed", Box().GetAttribute("aria-checked"));
  }

  [TestMethod]
  public void BoolFilter_ClearButtonClearsTheFilter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(showClearButton: true);
    IElement Clear() => FilterCell(grid, 2).QuerySelector(".pixata-blazor-grid-filter-clear")!;

    Assert.IsTrue(Clear().HasAttribute("disabled"));
    FilterCell(grid, 2).QuerySelector("[role=checkbox]")!.Click();
    Assert.AreEqual("Alice,Carol", Names(grid));
    Clear().Click();

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
    Assert.AreEqual("mixed", FilterCell(grid, 2).QuerySelector("[role=checkbox]")!.GetAttribute("aria-checked"));
  }

  [TestMethod]
  public void BoolFilter_HasNoClearButtonByDefault() =>
    Assert.IsNull(FilterCell(Render(), 2).QuerySelector(".pixata-blazor-grid-filter-clear"));
}
