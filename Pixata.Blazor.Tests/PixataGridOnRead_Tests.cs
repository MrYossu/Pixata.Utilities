using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using Pixata.Extensions;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridOnRead_Tests {
  private BunitContext _ctx = null!;

  public record Item(int Id, string Name);

  private static readonly List<Item> Items = [.. Enumerable.Range(1, 23).Select(n => new Item(n, $"Item {n}"))];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataGrid<Item>> Render(Func<PixataGridReadEventArgs<Item>, Task> onRead, int page = 1) =>
    _ctx.Render<PixataGrid<Item>>(p => p
      .Add(g => g.OnRead, EventCallback.Factory.Create(this, onRead))
      .Add(g => g.Pageable, true)
      .Add(g => g.PageSize, 5)
      .Add(g => g.Page, page)
      .Add(g => g.Sortable, true)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Item>>(0);
        b.AddAttribute(1, "Field", nameof(Item.Id));
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Item>>(2);
        b.AddAttribute(3, "Field", nameof(Item.Name));
        b.CloseComponent();
      }));

  private static string Ids<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : Microsoft.AspNetCore.Components.IComponent =>
    string.Join(",", grid.FindAll("tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim()));

  [TestMethod]
  public void OnRead_IsCalledWithTheRequestAndItsResultIsShown() {
    List<GridRequest> requests = [];
    IRenderedComponent<PixataGrid<Item>> grid = Render(args => {
      requests.Add(args.Request);
      args.SetResult(Items.ToGridResult(args.Request));
      return Task.CompletedTask;
    });

    Assert.AreEqual(1, requests.Count);
    Assert.AreEqual(1, requests[0].Page);
    Assert.AreEqual(5, requests[0].PageSize);
    Assert.AreEqual("1,2,3,4,5", Ids(grid));
    Assert.AreEqual("1 - 5 of 23 items", grid.Find(".pixata-blazor-grid-pager-info").TextContent);
  }

  [TestMethod]
  public void OnRead_PagingAndSorting_AskForNewData() {
    List<GridRequest> requests = [];
    IRenderedComponent<PixataGrid<Item>> grid = Render(args => {
      requests.Add(args.Request);
      args.SetResult(Items.ToGridResult(args.Request));
      return Task.CompletedTask;
    });

    grid.FindAll(".pixata-blazor-grid-pager-number").Single(b => b.TextContent.Trim() == "3").Click();
    grid.WaitForAssertion(() => Assert.AreEqual("11,12,13,14,15", Ids(grid)));
    Assert.AreEqual(3, requests[^1].Page);

    grid.FindAll("th")[0].Click();
    grid.FindAll("th")[0].Click();
    grid.WaitForAssertion(() => Assert.AreEqual(new GridSortDescriptor("Id", ListSortDirection.Descending), requests[^1].Sorts.Single()));
    grid.WaitForAssertion(() => Assert.AreEqual("13,12,11,10,9", Ids(grid)));
  }

  [TestMethod]
  public void OnRead_IsNotCalledAgainWhenTheParentReRenders() {
    int calls = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(args => {
      calls++;
      args.SetResult(Items.ToGridResult(args.Request));
      return Task.CompletedTask;
    });

    grid.Render(p => p.Add(g => g.Size, PixataGridSize.Small));

    Assert.AreEqual(1, calls);
  }

  [TestMethod]
  public void Rebind_CallsOnReadAgain() {
    int calls = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(args => {
      calls++;
      args.SetResult(Items.ToGridResult(args.Request));
      return Task.CompletedTask;
    });

    grid.InvokeAsync(grid.Instance.Rebind);

    grid.WaitForAssertion(() => Assert.AreEqual(2, calls));
  }

  [TestMethod]
  public void OnRead_AnOlderResultArrivingLate_IsIgnored() {
    TaskCompletionSource slow = new();
    int call = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(async args => {
      call++;
      if (call == 2) {
        // The second read (page 2) is slow, and finishes after the third (page 3)
        await slow.Task;
      }
      args.SetResult(Items.ToGridResult(args.Request));
    });

    grid.FindAll(".pixata-blazor-grid-pager-number").Single(b => b.TextContent.Trim() == "2").Click();
    grid.FindAll(".pixata-blazor-grid-pager-number").Single(b => b.TextContent.Trim() == "3").Click();
    grid.WaitForAssertion(() => Assert.AreEqual("11,12,13,14,15", Ids(grid)));
    grid.InvokeAsync(() => slow.SetResult()).Wait();

    Assert.AreEqual("11,12,13,14,15", Ids(grid));
  }

  [TestMethod]
  public void OnRead_FailedApiResponse_ShowsItsMessage() {
    IRenderedComponent<PixataGrid<Item>> grid = Render(args => {
      args.SetResult(new ApiResponse<GridResult<Item>>(ApiResponseStates.Failure, Message: "The server is having a lie down"));
      return Task.CompletedTask;
    });

    Assert.AreEqual("The server is having a lie down", grid.Find(".pixata-blazor-grid-error td").TextContent.Trim());
  }

  [TestMethod]
  public void OnRead_PagePastTheEnd_GoesToTheLastPageAndAsksAgain() {
    List<int> pages = [];
    IRenderedComponent<PixataGrid<Item>> grid = Render(args => {
      pages.Add(args.Request.Page);
      args.SetResult(Items.ToGridResult(args.Request));
      return Task.CompletedTask;
    }, page: 9);

    grid.WaitForAssertion(() => Assert.AreEqual("21,22,23", Ids(grid)));
    CollectionAssert.AreEqual(new[] { 9, 5 }, pages);
  }

  [TestMethod]
  public void OnRead_SlowReads_ShowTheLoaderAfterADelay() {
    TaskCompletionSource slow = new();
    int call = 0;
    IRenderedComponent<PixataGrid<Item>> grid = Render(async args => {
      call++;
      if (call == 2) {
        await slow.Task;
      }
      args.SetResult(Items.ToGridResult(args.Request));
    });

    grid.FindAll(".pixata-blazor-grid-pager-number").Single(b => b.TextContent.Trim() == "2").Click();
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-loader").Count);
    grid.WaitForAssertion(() => Assert.AreEqual(1, grid.FindAll(".pixata-blazor-grid-loader").Count), TimeSpan.FromSeconds(3));

    grid.InvokeAsync(() => slow.SetResult()).Wait();
    grid.WaitForAssertion(() => Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-loader").Count));
    Assert.AreEqual("6,7,8,9,10", Ids(grid));
  }

  [TestMethod]
  public void DataAndOnRead_Throws() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataGrid<Item>>(p => p
      .Add(g => g.Data, Items)
      .Add(g => g.OnRead, EventCallback.Factory.Create<PixataGridReadEventArgs<Item>>(this, _ => { }))));
}
