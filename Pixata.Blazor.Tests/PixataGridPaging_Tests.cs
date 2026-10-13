using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridPaging_Tests {
  private BunitContext _ctx = null!;

  public record Item(int Id);

  private static readonly List<Item> Items = [.. Enumerable.Range(1, 23).Select(n => new Item(n))];

  [TestInitialize]
  public void Initialise() =>
    _ctx = new();

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataGrid<Item>> Render(int pageSize = 5, int page = 1, int buttonCount = 10, List<int?>? pageSizes = null,
                                                      PixataGridPagerInputType inputType = PixataGridPagerInputType.Buttons,
                                                      PixataGridPagerPosition position = PixataGridPagerPosition.Bottom,
                                                      bool pageable = true, IEnumerable<Item>? data = null,
                                                      EventCallback<int>? pageChanged = null, EventCallback<int>? pageSizeChanged = null) =>
    _ctx.RenderComponent<PixataGrid<Item>>(p => {
      p.Add(g => g.Data, data ?? Items)
        .Add(g => g.Pageable, pageable)
        .Add(g => g.PageSize, pageSize)
        .Add(g => g.Page, page)
        .Add(g => g.GridSettings, b => {
          b.OpenComponent<PixataGridPagerSettings<Item>>(0);
          b.AddAttribute(1, "ButtonCount", buttonCount);
          b.AddAttribute(2, "PageSizes", pageSizes);
          b.AddAttribute(3, "InputType", inputType);
          b.AddAttribute(4, "Position", position);
          b.CloseComponent();
        })
        .Add(g => g.GridColumns, b => {
          b.OpenComponent<PixataGridColumn<Item>>(0);
          b.AddAttribute(1, "Field", nameof(Item.Id));
          b.CloseComponent();
        });
      if (pageChanged is not null) {
        p.Add(g => g.PageChanged, pageChanged.Value);
      }
      if (pageSizeChanged is not null) {
        p.Add(g => g.PageSizeChanged, pageSizeChanged.Value);
      }
    });

  private static string[] Ids(IRenderedFragment grid) =>
    [.. grid.FindAll("tbody td").Select(td => td.TextContent.Trim())];

  private static string[] PageButtons(IRenderedFragment grid) =>
    [.. grid.FindAll(".pixata-blazor-grid-pager-number").Select(b => b.TextContent.Trim())];

  private static IElement Nav(IRenderedFragment grid, string title) =>
    grid.FindAll(".pixata-blazor-grid-pager-nav").Single(b => b.GetAttribute("title") == title);

  [TestMethod]
  public void Paging_ShowsTheFirstPageAndInfo() {
    IRenderedComponent<PixataGrid<Item>> grid = Render();

    CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, Ids(grid));
    Assert.AreEqual("1 - 5 of 23 items", grid.Find(".pixata-blazor-grid-pager-info").TextContent);
    CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, PageButtons(grid));
    Assert.IsTrue(grid.Find(".pixata-blazor-grid-pager-number.pixata-blazor-grid-selected").TextContent.Contains('1'));
  }

  [TestMethod]
  public void Paging_NotPageable_ShowsEverythingAndNoPager() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(pageable: false);

    Assert.AreEqual(23, Ids(grid).Length);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-pager").Count);
  }

  [TestMethod]
  public void Paging_ClickingAPage_ShowsItAndRaisesPageChanged() {
    int changedTo = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(pageChanged: EventCallback.Factory.Create<int>(this, p => changedTo = p));

    grid.FindAll(".pixata-blazor-grid-pager-number").Single(b => b.TextContent.Trim() == "3").Click();

    Assert.AreEqual(3, changedTo);
    CollectionAssert.AreEqual(new[] { "11", "12", "13", "14", "15" }, Ids(grid));
    Assert.AreEqual("11 - 15 of 23 items", grid.Find(".pixata-blazor-grid-pager-info").TextContent);
  }

  [TestMethod]
  public void Paging_NavButtons_MoveAndDisableAtTheEnds() {
    IRenderedComponent<PixataGrid<Item>> grid = Render();

    Assert.IsTrue(Nav(grid, "Go to the first page").HasAttribute("disabled"));
    Assert.IsTrue(Nav(grid, "Go to the previous page").HasAttribute("disabled"));

    Nav(grid, "Go to the next page").Click();
    CollectionAssert.AreEqual(new[] { "6", "7", "8", "9", "10" }, Ids(grid));

    Nav(grid, "Go to the last page").Click();
    CollectionAssert.AreEqual(new[] { "21", "22", "23" }, Ids(grid));
    Assert.IsTrue(Nav(grid, "Go to the next page").HasAttribute("disabled"));
    Assert.IsTrue(Nav(grid, "Go to the last page").HasAttribute("disabled"));

    Nav(grid, "Go to the previous page").Click();
    CollectionAssert.AreEqual(new[] { "16", "17", "18", "19", "20" }, Ids(grid));
  }

  [TestMethod]
  public void Paging_ButtonCount_ShowsPagesInGroupsWithMoreButtons() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(pageSize: 2, buttonCount: 3);

    CollectionAssert.AreEqual(new[] { "1", "2", "3", "..." }, PageButtons(grid));

    grid.FindAll(".pixata-blazor-grid-pager-number").Last().Click();
    CollectionAssert.AreEqual(new[] { "...", "4", "5", "6", "..." }, PageButtons(grid));
    CollectionAssert.AreEqual(new[] { "7", "8" }, Ids(grid));

    grid.FindAll(".pixata-blazor-grid-pager-number").First().Click();
    CollectionAssert.AreEqual(new[] { "1", "2", "3", "..." }, PageButtons(grid));
    CollectionAssert.AreEqual(new[] { "5", "6" }, Ids(grid));
  }

  [TestMethod]
  public void Paging_PagePastTheEnd_MovesToTheLastPageAndRaisesPageChanged() {
    int changedTo = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(page: 9, pageChanged: EventCallback.Factory.Create<int>(this, p => changedTo = p));

    Assert.AreEqual(5, changedTo);
    CollectionAssert.AreEqual(new[] { "21", "22", "23" }, Ids(grid));
  }

  [TestMethod]
  public void Paging_DataShrinks_MovesBackToTheLastPage() {
    List<Item> items = [.. Items];
    IRenderedComponent<PixataGrid<Item>> grid = Render(page: 5, data: items);

    items.RemoveRange(10, 13);
    grid.InvokeAsync(grid.Instance.Rebind);

    CollectionAssert.AreEqual(new[] { "6", "7", "8", "9", "10" }, Ids(grid));
  }

  [TestMethod]
  public void Paging_PickingAPageSize_GoesBackToPageOneAndRaisesPageSizeChanged() {
    int newSize = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(page: 3, pageSizes: [5, 10, null], pageSizeChanged: EventCallback.Factory.Create<int>(this, s => newSize = s));

    grid.Find(".pixata-blazor-grid-pager-sizes .pixata-blazor-grid-dropdown-button").Click();
    CollectionAssert.AreEqual(new[] { "5", "10", "All" }, grid.FindAll(".pixata-blazor-grid-dropdown-item").Select(i => i.TextContent.Trim()).ToArray());
    grid.FindAll(".pixata-blazor-grid-dropdown-item").Single(i => i.TextContent.Trim() == "10").Click();

    Assert.AreEqual(10, newSize);
    Assert.AreEqual(10, Ids(grid).Length);
    Assert.AreEqual("1", Ids(grid)[0]);
  }

  [TestMethod]
  public void Paging_PickingAll_SetsThePageSizeToTheItemCount() {
    int newSize = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(pageSizes: [5, null], pageSizeChanged: EventCallback.Factory.Create<int>(this, s => newSize = s));

    grid.Find(".pixata-blazor-grid-pager-sizes .pixata-blazor-grid-dropdown-button").Click();
    grid.FindAll(".pixata-blazor-grid-dropdown-item").Single(i => i.TextContent.Trim() == "All").Click();

    Assert.AreEqual(23, newSize);
    Assert.AreEqual(23, Ids(grid).Length);
    Assert.AreEqual("23", grid.Find(".pixata-blazor-grid-pager-sizes .pixata-blazor-grid-dropdown-text").TextContent.Trim());
  }

  [TestMethod]
  public void Paging_InputType_GoesToTheTypedPageClampedToTheRange() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(inputType: PixataGridPagerInputType.Input);

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-pager-number").Count);
    Assert.IsTrue(grid.Find(".pixata-blazor-grid-pager-input").TextContent.Contains("of 5"));

    grid.Find(".pixata-blazor-grid-pager-input-box").Change("2");
    CollectionAssert.AreEqual(new[] { "6", "7", "8", "9", "10" }, Ids(grid));

    grid.Find(".pixata-blazor-grid-pager-input-box").Change("99");
    CollectionAssert.AreEqual(new[] { "21", "22", "23" }, Ids(grid));

    grid.Find(".pixata-blazor-grid-pager-input-box").Change("rubbish");
    Assert.AreEqual("5", grid.Find(".pixata-blazor-grid-pager-input-box").GetAttribute("value"));
  }

  [TestMethod]
  public void Paging_PositionTop_PutsThePagerAboveTheRows() {
    IElement root = Render(position: PixataGridPagerPosition.Top).Find(".pixata-blazor-grid");

    Assert.IsTrue(root.FirstElementChild!.ClassList.Contains("pixata-blazor-grid-pager-container"));
    Assert.IsTrue(root.LastElementChild!.ClassList.Contains("pixata-blazor-grid-content"));
  }

  [TestMethod]
  public void Paging_NoData_ShowsZeroInfoAndNoPageButtons() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(data: []);

    Assert.AreEqual("0 - 0 of 0 items", grid.Find(".pixata-blazor-grid-pager-info").TextContent);
    Assert.AreEqual(0, PageButtons(grid).Length);
  }
}
