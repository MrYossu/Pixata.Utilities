using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridDetail_Tests {
  private BunitContext _ctx = null!;

  public record Person(int Id, string Name, string Team);

  private static readonly List<Person> People = [new(1, "Ann", "A"), new(2, "Bob", "B"), new(3, "Cat", "A")];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static void Column<TColumn>(RenderTreeBuilder b, string? field = null) where TColumn : IComponent {
    b.OpenComponent<TColumn>(0);
    if (field is not null) {
      b.AddAttribute(1, "Field", field);
    }
    b.CloseComponent();
  }

  private IRenderedComponent<PixataGrid<Person>> RenderWithDetails(Action<ComponentParameterCollectionBuilder<PixataGrid<Person>>>? more = null) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, People)
        .Add(g => g.DetailTemplate, person => b => b.AddContent(0, $"Details of {person.Name}"))
        .Add(g => g.GridColumns, b => {
          Column<PixataGridColumn<Person>>(b, nameof(Person.Id));
          Column<PixataGridColumn<Person>>(b, nameof(Person.Name));
        });
      more?.Invoke(p);
    });

  [TestMethod]
  public void DetailTemplate_AddsAnExpandColumn() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithDetails();

    Assert.AreEqual(1, grid.FindAll("col.pixata-blazor-grid-hierarchy-col").Count);
    Assert.IsTrue(grid.Find("thead th").ClassList.Contains("pixata-blazor-grid-hierarchy-cell"));
    Assert.AreEqual(3, grid.FindAll("tbody .pixata-blazor-grid-hierarchy-toggle").Count);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-detail-row").Count);
  }

  [TestMethod]
  public void WithoutADetailTemplate_ThereIsNoExpandColumn() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People)
      .Add(g => g.GridColumns, b => Column<PixataGridColumn<Person>>(b, nameof(Person.Id))));

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-hierarchy-cell").Count);
  }

  [TestMethod]
  public void Expanding_ShowsTheDetailsAndRaisesTheEvents() {
    List<string> events = [];
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithDetails(p => p
      .Add(g => g.OnRowExpand, EventCallback.Factory.Create<PixataGridRowExpandEventArgs<Person>>(this, args => events.Add($"expand {args.Item.Name}")))
      .Add(g => g.OnRowCollapse, EventCallback.Factory.Create<PixataGridRowCollapseEventArgs<Person>>(this, args => events.Add($"collapse {args.Item.Name}"))));

    grid.FindAll(".pixata-blazor-grid-hierarchy-toggle")[1].Click();

    List<AngleSharp.Dom.IElement> rows = [.. grid.FindAll("tbody tr")];
    Assert.AreEqual(4, rows.Count);
    Assert.IsTrue(rows[1].ClassList.Contains("pixata-blazor-grid-expanded"));
    Assert.IsTrue(rows[2].ClassList.Contains("pixata-blazor-grid-detail-row"));
    Assert.AreEqual("Details of Bob", rows[2].QuerySelector(".pixata-blazor-grid-detail-cell")!.TextContent);
    Assert.AreEqual("2", rows[2].QuerySelector(".pixata-blazor-grid-detail-cell")!.GetAttribute("colspan"));
    Assert.AreEqual("true", rows[1].QuerySelector(".pixata-blazor-grid-hierarchy-cell")!.GetAttribute("aria-expanded"));
    // The detail row doesn't count for the striping
    Assert.IsFalse(rows[3].ClassList.Contains("pixata-blazor-grid-alt-row"));

    grid.FindAll(".pixata-blazor-grid-hierarchy-toggle")[1].Click();

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-detail-row").Count);
    CollectionAssert.AreEqual(new[] { "expand Bob", "collapse Bob" }, events);
  }

  [TestMethod]
  public void ExpandedRows_ArePartOfTheState() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithDetails();
    grid.FindAll(".pixata-blazor-grid-hierarchy-toggle")[2].Click();

    Assert.AreEqual(3, grid.Instance.GetState().ExpandedItems.Single().Id);

    grid.InvokeAsync(() => grid.Instance.SetState(new() { ExpandedItems = [People[0]] }));

    Assert.AreEqual("Details of Ann", grid.Find(".pixata-blazor-grid-detail-cell").TextContent);
  }

  [TestMethod]
  public void ExpandColumn_GoesAfterTheGroupCells() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithDetails(p => p.Add(g => g.Groupable, true));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    AngleSharp.Dom.IElement row = grid.Find("tbody tr.pixata-blazor-grid-row");
    Assert.IsTrue(row.Children[0].ClassList.Contains("pixata-blazor-grid-group-cell"));
    Assert.IsTrue(row.Children[1].ClassList.Contains("pixata-blazor-grid-hierarchy-cell"));
    Assert.AreEqual("4", grid.Find(".pixata-blazor-grid-group-header").GetAttribute("colspan"));
  }

  // ---------- Dragging rows ----------

  private IRenderedComponent<PixataGrid<Person>> RenderDraggable(Action<PixataGridRowDropEventArgs<Person>> dropped, bool multiple = false) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People)
      .Add(g => g.RowDraggable, true)
      .Add(g => g.SelectionMode, multiple ? PixataGridSelectionMode.Multiple : PixataGridSelectionMode.None)
      .Add(g => g.OnRowDrop, EventCallback.Factory.Create(this, dropped))
      .Add(g => g.GridColumns, b => {
        Column<PixataGridDragColumn<Person>>(b);
        Column<PixataGridColumn<Person>>(b, nameof(Person.Name));
      }));

  [TestMethod]
  public void RowDraggable_GivesEachRowAHandle() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderDraggable(_ => { });

    Assert.AreEqual(3, grid.FindAll(".pixata-blazor-grid-drag-cell .pixata-blazor-grid-drag-handle").Count);
    CollectionAssert.AreEqual(new[] { "0", "1", "2" }, grid.FindAll("tbody tr").Select(tr => tr.GetAttribute("data-pixata-row-index")).ToArray());
    Assert.AreEqual("width: 36px", grid.Find("col").GetAttribute("style"));
  }

  [TestMethod]
  public void RowDraggable_WithoutADragColumn_AddsOneAtTheStart() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People)
      .Add(g => g.RowDraggable, true)
      .Add(g => g.GridColumns, b => Column<PixataGridColumn<Person>>(b, nameof(Person.Name))));

    Assert.AreEqual(3, grid.FindAll("tbody td:first-child .pixata-blazor-grid-drag-handle").Count);
    Assert.AreEqual("width: 36px", grid.Find("col").GetAttribute("style"));
  }

  [TestMethod]
  public void DroppingARow_DescribesTheDrop() {
    PixataGridRowDropEventArgs<Person>? drop = null;
    IRenderedComponent<PixataGrid<Person>> grid = RenderDraggable(args => drop = args);
    string gridId = grid.Find(".pixata-blazor-grid").Id!;

    grid.InvokeAsync(() => grid.Instance.RowDrop(0, gridId, 2, "After"));

    Assert.AreEqual("Ann", drop!.Item.Name);
    CollectionAssert.AreEqual(new[] { "Ann" }, drop.Items.Select(i => i.Name).ToArray());
    Assert.AreSame(grid.Instance, drop.DestinationGrid);
    Assert.AreEqual("Cat", drop.DestinationItem!.Name);
    Assert.AreEqual(2, drop.DestinationIndex);
    Assert.AreEqual(PixataGridDropPosition.After, drop.DropPosition);
  }

  [TestMethod]
  public void DroppingOutsideAGrid_HasNoDestination() {
    PixataGridRowDropEventArgs<Person>? drop = null;
    IRenderedComponent<PixataGrid<Person>> grid = RenderDraggable(args => drop = args);

    grid.InvokeAsync(() => grid.Instance.RowDrop(1, null, -1, "After"));

    Assert.AreEqual("Bob", drop!.Item.Name);
    Assert.IsNull(drop.DestinationGrid);
    Assert.IsNull(drop.DestinationItem);
    Assert.IsNull(drop.DestinationIndex);
  }

  [TestMethod]
  public void DroppingOnAnotherGrid_GivesThatGrid() {
    PixataGridRowDropEventArgs<Person>? drop = null;
    IRenderedComponent<PixataGrid<Person>> source = RenderDraggable(args => drop = args);
    IRenderedComponent<PixataGrid<Person>> other = RenderDraggable(_ => { });

    source.InvokeAsync(() => source.Instance.RowDrop(0, other.Find(".pixata-blazor-grid").Id, 1, "Before"));

    Assert.AreSame(other.Instance, drop!.DestinationGrid);
    Assert.AreEqual("Bob", drop.DestinationItem!.Name);
    Assert.AreEqual(PixataGridDropPosition.Before, drop.DropPosition);
  }

  [TestMethod]
  public void DraggingASelectedRow_DragsAllTheSelectedRows() {
    PixataGridRowDropEventArgs<Person>? drop = null;
    IRenderedComponent<PixataGrid<Person>> grid = RenderDraggable(args => drop = args, true);
    grid.SetParametersAndRender(p => p.Add(g => g.SelectedItems, [People[0], People[2]]));

    grid.InvokeAsync(() => grid.Instance.RowDrop(2, null, -1, "After"));

    CollectionAssert.AreEqual(new[] { "Ann", "Cat" }, drop!.Items.Select(i => i.Name).ToArray());
  }
}
