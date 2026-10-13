using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridSorting_Tests {
  private BunitContext _ctx = null!;

  public class Address {
    public string City { get; set; } = "";
  }

  public class Person {
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public Address? Address { get; set; }
  }

  private static List<Person> People() => [
    new() { Name = "Carol", Age = 30, Address = new() { City = "Leeds" } },
    new() { Name = "Alice", Age = 40 },
    new() { Name = "Bob", Age = 30, Address = new() { City = "Bath" } },
    new() { Name = "Dave", Age = 20, Address = new() { City = "York" } }
  ];

  [TestInitialize]
  public void Initialise() =>
    _ctx = new();

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataGrid<Person>> Render(PixataGridSortMode mode = PixataGridSortMode.Single, bool sortable = true, bool ageSortable = true) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.Sortable, sortable)
      .Add(g => g.SortMode, mode)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Name));
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(2);
        b.AddAttribute(3, "Field", nameof(Person.Age));
        b.AddAttribute(4, "Sortable", ageSortable);
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(5);
        b.AddAttribute(6, "Field", "Address.City");
        b.CloseComponent();
      }));

  private static string Names(IRenderedFragment grid) =>
    string.Join(",", grid.FindAll("tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim()));

  private static IElement Header(IRenderedFragment grid, string title) =>
    grid.FindAll("th").Single(th => th.QuerySelector(".pixata-blazor-grid-column-title")!.TextContent.Trim() == title);

  [TestMethod]
  public void Sorting_ClicksCycleThroughAscendingDescendingAndNone() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Header(grid, "Name").Click();
    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
    Assert.AreEqual("ascending", Header(grid, "Name").GetAttribute("aria-sort"));
    Assert.IsNotNull(Header(grid, "Name").QuerySelector(".pixata-blazor-grid-sort-icon"));

    Header(grid, "Name").Click();
    Assert.AreEqual("Dave,Carol,Bob,Alice", Names(grid));
    Assert.AreEqual("descending", Header(grid, "Name").GetAttribute("aria-sort"));

    Header(grid, "Name").Click();
    Assert.AreEqual("Carol,Alice,Bob,Dave", Names(grid));
    Assert.IsNull(Header(grid, "Name").GetAttribute("aria-sort"));
    Assert.IsNull(Header(grid, "Name").QuerySelector(".pixata-blazor-grid-sort-icon"));
  }

  [TestMethod]
  public void Sorting_SingleMode_ReplacesTheSort() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Header(grid, "Name").Click();
    Header(grid, "Age").Click();

    Assert.AreEqual("Dave,Carol,Bob,Alice", Names(grid));
    Assert.IsNull(Header(grid, "Name").GetAttribute("aria-sort"));
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-sort-order").Count);
  }

  [TestMethod]
  public void Sorting_MultipleMode_AddsSortsAndShowsTheirOrder() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridSortMode.Multiple);

    Header(grid, "Age").Click();
    Header(grid, "Name").Click();
    Header(grid, "Name").Click();

    Assert.AreEqual("Dave,Carol,Bob,Alice", Names(grid));
    Assert.AreEqual("1", Header(grid, "Age").QuerySelector(".pixata-blazor-grid-sort-order")!.TextContent);
    Assert.AreEqual("2", Header(grid, "Name").QuerySelector(".pixata-blazor-grid-sort-order")!.TextContent);

    // Removing the first sort promotes the second
    Header(grid, "Age").Click();
    Header(grid, "Age").Click();
    Assert.AreEqual("1", Header(grid, "Name").QuerySelector(".pixata-blazor-grid-sort-order")!.TextContent);
    Assert.AreEqual("Dave,Carol,Bob,Alice", Names(grid));
  }

  [TestMethod]
  public void Sorting_NestedFieldWithNulls_PutsNullsFirst() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Header(grid, "City").Click();

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
  }

  [TestMethod]
  public void Sorting_UnsortableColumnOrGrid_IgnoresClicks() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(ageSortable: false);
    IRenderedComponent<PixataGrid<Person>> unsortable = Render(sortable: false);

    Header(grid, "Age").Click();
    Header(unsortable, "Name").Click();

    Assert.AreEqual("Carol,Alice,Bob,Dave", Names(grid));
    Assert.IsFalse(Header(grid, "Age").ClassList.Contains("pixata-blazor-grid-sortable"));
    Assert.IsTrue(Header(grid, "Name").ClassList.Contains("pixata-blazor-grid-sortable"));
    Assert.AreEqual("Carol,Alice,Bob,Dave", Names(unsortable));
    Assert.IsNull(Header(unsortable, "Name").GetAttribute("tabindex"));
  }

  [TestMethod]
  public void Sorting_EnterOnAHeader_Sorts() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Header(grid, "Name").KeyDown("Enter");

    Assert.AreEqual("Alice,Bob,Carol,Dave", Names(grid));
  }
}
