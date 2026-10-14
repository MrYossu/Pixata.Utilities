using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridVirtual_Tests {
  private BunitContext _ctx = null!;

  public record Item(int Id, string Name);

  private static readonly List<Item> Items = [.. Enumerable.Range(1, 200).Select(n => new Item(n, $"Item {n}"))];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static RenderFragment Columns(int count = 2, string? width = null) =>
    b => {
      for (int i = 0; i < count; i++) {
        b.OpenComponent<PixataGridColumn<Item>>(0);
        b.AddAttribute(1, "Field", i % 2 == 0 ? nameof(Item.Id) : nameof(Item.Name));
        b.AddAttribute(2, "Title", $"Column {i}");
        if (width is not null) {
          b.AddAttribute(3, "Width", width);
        }
        b.CloseComponent();
      }
    };

  private IRenderedComponent<PixataGrid<Item>> Render(Action<ComponentParameterCollectionBuilder<PixataGrid<Item>>>? more = null, RenderFragment? columns = null, double? rowHeight = 36) =>
    _ctx.Render<PixataGrid<Item>>(p => {
      p.Add(g => g.Data, Items)
        .Add(g => g.GridColumns, columns ?? Columns())
        .Add(g => g.ScrollMode, PixataGridScrollMode.Virtual)
        .Add(g => g.Height, "400px")
        .Add(g => g.RowHeight, rowHeight)
        .Add(g => g.PageSize, 10)
        .Add(g => g.Pageable, true);
      more?.Invoke(p);
    });

  private static List<string> Ids<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    [.. grid.FindAll("tbody tr.pixata-blazor-grid-row").Select(tr => tr.Children[0].TextContent.Trim())];

  private static List<string?> Spacers<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    [.. grid.FindAll("tbody tr.pixata-blazor-grid-virtual-spacer").Select(tr => tr.GetAttribute("style"))];

  [TestMethod]
  public void Virtual_RendersThreePagesOfRows_WithASpacerForTheRest() {
    IRenderedComponent<PixataGrid<Item>> grid = Render();

    Assert.AreEqual(30, Ids(grid).Count);
    Assert.AreEqual("1", Ids(grid)[0]);
    CollectionAssert.AreEqual(new[] { "height: 6120px;" }, Spacers(grid));
    Assert.AreEqual("height: 36px;", grid.Find("tbody tr.pixata-blazor-grid-row").GetAttribute("style"));
    // There's no pager, as the rows scroll instead
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-pager").Count);
  }

  [TestMethod]
  public void Scrolling_RendersTheRowsInView() {
    IRenderedComponent<PixataGrid<Item>> grid = Render();

    // 3600px down is row 101, so the rows rendered start a page before it
    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(3600, 0, 600, 400));

    Assert.AreEqual("91", Ids(grid)[0]);
    Assert.AreEqual(30, Ids(grid).Count);
    CollectionAssert.AreEqual(new[] { "height: 3240px;", "height: 2880px;" }, Spacers(grid));
    // The striping follows the row's position in all the data
    Assert.IsFalse(grid.Find("tbody tr.pixata-blazor-grid-row").ClassList.Contains("pixata-blazor-grid-alt-row"));
  }

  [TestMethod]
  public void ScrollingALittle_KeepsTheSameRows() {
    IRenderedComponent<PixataGrid<Item>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(200, 0, 600, 400));

    Assert.AreEqual("1", Ids(grid)[0]);
  }

  [TestMethod]
  public void ScrollingToTheEnd_RendersTheLastRows() {
    IRenderedComponent<PixataGrid<Item>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(200 * 36 - 400, 0, 600, 400));

    Assert.AreEqual("200", Ids(grid)[^1]);
    Assert.AreEqual(1, Spacers(grid).Count);
  }

  [TestMethod]
  public void Filtering_GoesBackToTheTop() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(p => p.Add(g => g.FilterMode, PixataGridFilterMode.FilterRow));
    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(3600, 0, 600, 400));

    grid.InvokeAsync(() => grid.Instance.SetState(new() { PageSize = 10, FilterDescriptors = [new(GridFilterLogic.And, [new(nameof(Item.Id), GridFilterOperator.IsGreaterThan, 150)])] }));

    Assert.AreEqual("151", Ids(grid)[0]);
  }

  [TestMethod]
  public void Virtual_NeedsAHeightAndARowHeight() {
    InvalidOperationException ex = Assert.ThrowsException<InvalidOperationException>(() => Render(rowHeight: null));

    StringAssert.Contains(ex.Message, "RowHeight");
  }

  [TestMethod]
  public void Virtual_CannotBeGrouped() {
    Assert.ThrowsException<InvalidOperationException>(() => Render(p => p.Add(g => g.Groupable, true)));
  }

  [TestMethod]
  public void OnRead_AsksForTheRowsToRender_AndShowsPlaceholdersWhileWaiting() {
    List<GridRequest> requests = [];
    TaskCompletionSource? waiting = null;
    IRenderedComponent<PixataGrid<Item>> grid = _ctx.Render<PixataGrid<Item>>(p => p
      .Add(g => g.OnRead, EventCallback.Factory.Create<PixataGridReadEventArgs<Item>>(this, async args => {
        requests.Add(args.Request);
        if (waiting is not null) {
          await waiting.Task;
        }
        args.SetResult(Items.ToGridResult(args.Request));
      }))
      .Add(g => g.GridColumns, Columns())
      .Add(g => g.ScrollMode, PixataGridScrollMode.Virtual)
      .Add(g => g.Height, "400px")
      .Add(g => g.RowHeight, 36)
      .Add(g => g.PageSize, 10));

    Assert.AreEqual(0, requests[0].ItemsToSkip);
    Assert.AreEqual(30, requests[0].PageSize);

    waiting = new();
    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(3600, 0, 600, 400));

    Assert.AreEqual(90, requests[^1].Skip);
    Assert.AreEqual(60, grid.FindAll(".pixata-blazor-grid-skeleton").Count);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-loader").Count);

    waiting.SetResult();

    grid.WaitForAssertion(() => Assert.AreEqual("91", Ids(grid)[0]));
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-skeleton").Count);
  }

  [TestMethod]
  public void ColumnVirtualization_RendersOnlyTheCellsInView() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(p => p.Add(g => g.ColumnVirtualization, true), Columns(20, "100px"));

    // 500px wide, scrolled 1000px across, shows columns 10 to 14, with one either side
    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(0, 1000, 500, 400));

    List<string> cells = [.. grid.Find("tbody tr.pixata-blazor-grid-row").Children.Select(td => td.GetAttribute("colspan") is string span ? $"[{span}]" : td.TextContent.Trim())];
    CollectionAssert.AreEqual(new[] { "[9]", "Item 1", "1", "Item 1", "1", "Item 1", "1", "Item 1", "[4]" }, cells);
    // The header has every column
    Assert.AreEqual(20, grid.FindAll("thead th").Count);
  }

  [TestMethod]
  public void ColumnVirtualization_NeedsWidthsInPixels() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(p => p.Add(g => g.ColumnVirtualization, true), Columns(20));

    grid.InvokeAsync(() => grid.Instance.VirtualScrolled(0, 1000, 500, 400));

    Assert.AreEqual(20, grid.Find("tbody tr.pixata-blazor-grid-row").Children.Length, grid.Find("tbody tr.pixata-blazor-grid-row").OuterHtml);
  }
}
