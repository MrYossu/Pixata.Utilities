using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridState_Tests {
  private BunitContext _ctx = null!;

  public record Person(int Id, string Name, string Team, int Age);

  private static readonly List<Person> People = [
    new(1, "Ann", "B", 30),
    new(2, "Bob", "A", 40),
    new(3, "Cat", "B", 20),
    new(4, "Dan", "A", 50),
    new(5, "Eve", "C", 35)
  ];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static RenderFragment Columns =>
    b => {
      foreach ((string field, int seq) in new[] { (nameof(Person.Id), 0), (nameof(Person.Name), 10), (nameof(Person.Team), 20), (nameof(Person.Age), 30) }) {
        b.OpenComponent<PixataGridColumn<Person>>(seq);
        b.AddAttribute(seq + 1, "Field", field);
        b.CloseComponent();
      }
    };

  private IRenderedComponent<PixataGrid<Person>> Render(Action<ComponentParameterCollectionBuilder<PixataGrid<Person>>>? more = null) =>
    _ctx.Render<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, People)
        .Add(g => g.GridColumns, Columns)
        .Add(g => g.Pageable, true)
        .Add(g => g.PageSize, 2)
        .Add(g => g.Sortable, true)
        .Add(g => g.Groupable, true)
        .Add(g => g.FilterMode, PixataGridFilterMode.FilterRow);
      more?.Invoke(p);
    });

  private static List<string> Titles<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : Microsoft.AspNetCore.Components.IComponent =>
    [.. grid.FindAll("thead tr:first-child th:not(.pixata-blazor-grid-group-cell)").Select(th => th.QuerySelector(".pixata-blazor-grid-column-title")!.TextContent.Trim())];

  // The ids of the rows, wherever the Id column is
  private static List<string> Ids<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : Microsoft.AspNetCore.Components.IComponent {
    int column = grid.FindAll("thead tr:first-child th").ToList().FindIndex(th => th.QuerySelector(".pixata-blazor-grid-column-title")?.TextContent.Trim() == "Id");
    return [.. grid.FindAll("tbody tr.pixata-blazor-grid-row").Select(tr => tr.Children[column].TextContent.Trim())];
  }

  private static PixataGridState<Person> SampleState() =>
    new() {
      Page = 2,
      PageSize = 2,
      SortDescriptors = [new(nameof(Person.Age), ListSortDirection.Descending)],
      FilterDescriptors = [new(GridFilterLogic.And, [new(nameof(Person.Age), GridFilterOperator.IsGreaterThan, 25)])],
      ColumnStates = [
        new() { Field = nameof(Person.Age), Index = 0, Width = "90px" },
        new() { Field = nameof(Person.Team), Index = 3, Visible = false }
      ]
    };

  // A state that has been through JSON, as one saved in local storage would have
  private static PixataGridState<Person> RoundTrip(PixataGridState<Person> state) =>
    JsonSerializer.Deserialize<PixataGridState<Person>>(JsonSerializer.Serialize(state))!;

  [TestMethod]
  public void GetState_DescribesWhatTheUserHasDone() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    grid.FindAll("thead th")[3].Click();
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    PixataGridState<Person> state = grid.Instance.GetState();

    Assert.AreEqual(1, state.Page);
    Assert.AreEqual(2, state.PageSize);
    Assert.AreEqual(nameof(Person.Age), state.SortDescriptors.Single().Member);
    Assert.AreEqual(nameof(Person.Team), state.GroupDescriptors.Single().Member);
    CollectionAssert.AreEqual(new[] { "Id", "Name", "Team", "Age" }, state.ColumnStates.Select(c => c.Field).ToArray());
    CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, state.ColumnStates.Select(c => c.Index).ToArray());
    Assert.IsNull(state.EditItem);
  }

  [TestMethod]
  public void GetState_OnlySavesWidthsTheUserHasSet() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People)
      .Add(g => g.Resizable, true)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Name));
        b.AddAttribute(2, "Width", "100px");
        b.CloseComponent();
      }));

    // The column's own width isn't saved, so a new one set in code applies to anyone who has seen the grid before
    Assert.IsNull(grid.Instance.GetState().ColumnStates.Single().Width);

    PixataGridColumn<Person> column = grid.FindComponent<PixataGridColumn<Person>>().Instance;
    grid.InvokeAsync(() => grid.Instance.ColumnsResized(column.Key, [new(column.Key, 150)]));

    Assert.AreEqual("150px", grid.Instance.GetState().ColumnStates.Single().Width);
  }

  [TestMethod]
  public void SetState_AppliesEveryPart() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.SetState(RoundTrip(SampleState())));

    // Over 25, oldest first: Dan 50, Bob 40, Eve 35, Ann 30. Page 2 has Eve and Ann
    CollectionAssert.AreEqual(new[] { "5", "1" }, Ids(grid));
    CollectionAssert.AreEqual(new[] { "Age", "Id", "Name" }, Titles(grid));
    Assert.AreEqual("width: 90px", grid.Find("col").GetAttribute("style"));
    Assert.AreEqual("descending", grid.Find("thead th").GetAttribute("aria-sort"));
    // The filter row shows the restored filter, as a number rather than the JSON it came back as
    Assert.AreEqual("25", grid.FindAll(".pixata-blazor-grid-filter-row td")[0].QuerySelector("input")!.GetAttribute("value"));
  }

  [TestMethod]
  public void SetState_WithNull_GoesBackToHowTheGridStarted() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    grid.InvokeAsync(() => grid.Instance.SetState(SampleState()));

    grid.InvokeAsync(() => grid.Instance.SetState(null));

    CollectionAssert.AreEqual(new[] { "1", "2" }, Ids(grid));
    CollectionAssert.AreEqual(new[] { "Id", "Name", "Team", "Age" }, Titles(grid));
    Assert.IsNull(grid.Find("col").GetAttribute("style"));
  }

  [TestMethod]
  public void SetState_GroupsAndCollapses() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    PixataGridState<Person> state = new() { PageSize = 10, GroupDescriptors = [new(nameof(Person.Team))] };
    grid.InvokeAsync(() => grid.Instance.SetState(state));
    grid.Find(".pixata-blazor-grid-group-toggle").Click();
    List<string> collapsed = grid.Instance.GetState().CollapsedGroups;

    grid.InvokeAsync(() => grid.Instance.SetState(null));
    grid.InvokeAsync(() => grid.Instance.SetState(RoundTrip(new() { PageSize = 10, GroupDescriptors = [new(nameof(Person.Team))], CollapsedGroups = collapsed })));

    Assert.AreEqual(1, collapsed.Count);
    Assert.AreEqual("false", grid.Find(".pixata-blazor-grid-group-row").GetAttribute("aria-expanded"));
  }

  [TestMethod]
  public void SetState_RestoresTheSearch() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.GridToolBarTemplate, b => {
      b.OpenComponent<PixataGridSearchBox<Person>>(0);
      b.CloseComponent();
    }));

    grid.InvokeAsync(() => grid.Instance.SetState(RoundTrip(new() { PageSize = 2, SearchFilter = new(GridFilterLogic.Or, [new(nameof(Person.Name), GridFilterOperator.Contains, "a")]) })));

    CollectionAssert.AreEqual(new[] { "1", "3" }, Ids(grid));
    Assert.AreEqual("a", grid.Find(".pixata-blazor-grid-search input").GetAttribute("value"));
  }

  [TestMethod]
  public void SetState_SelectsItemsThatCameBackFromJson() {
    List<Person> selected = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.SelectionMode, PixataGridSelectionMode.Multiple)
      .Add(g => g.SelectedItemsChanged, EventCallback.Factory.Create<IEnumerable<Person>>(this, items => selected = [.. items])));

    grid.InvokeAsync(() => grid.Instance.SetState(RoundTrip(new() { PageSize = 2, SelectedItems = [People[1]] })));

    // Records compare by value, so the copy from JSON matches the item in the data
    Assert.AreEqual(2, selected.Single().Id);
    Assert.AreEqual("true", grid.FindAll("tbody tr")[1].GetAttribute("aria-selected"));
  }

  [TestMethod]
  public void OnStateInit_SetsTheStateTheGridStartsWith() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.OnStateInit, EventCallback.Factory.Create<PixataGridStateEventArgs<Person>>(this, args => args.GridState = SampleState())));

    CollectionAssert.AreEqual(new[] { "5", "1" }, Ids(grid));
    CollectionAssert.AreEqual(new[] { "Age", "Id", "Name" }, Titles(grid));
  }

  [TestMethod]
  public void OnStateInit_ARerenderWhileItRuns_DoesNotReadTheDataWithoutTheState() {
    TaskCompletionSource waiting = new();
    List<GridRequest> requests = [];
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.GridColumns, Columns)
      .Add(g => g.Sortable, true)
      .Add(g => g.OnRead, EventCallback.Factory.Create<PixataGridReadEventArgs<Person>>(this, args => {
        requests.Add(args.Request);
        args.SetResult(People.ToGridResult(args.Request));
      }))
      .Add(g => g.OnStateInit, EventCallback.Factory.Create<PixataGridStateEventArgs<Person>>(this, async args => {
        await waiting.Task;
        args.GridState.SortDescriptors = [new(nameof(Person.Age))];
      })));

    // The parent re-renders before the handler has finished
    grid.Render(p => p.Add(g => g.Height, "300px"));
    grid.InvokeAsync(() => waiting.SetResult()).Wait();

    grid.WaitForAssertion(() => Assert.AreEqual(1, requests.Count));
    Assert.AreEqual(nameof(Person.Age), requests.Single().Sorts.Single().Member);
    CollectionAssert.AreEqual(new[] { "3", "1", "5", "2", "4" }, Ids(grid));
  }

  [TestMethod]
  public void OnStateChanged_SaysWhatChanged() {
    List<string> changes = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.OnStateChanged, EventCallback.Factory.Create<PixataGridStateEventArgs<Person>>(this, args => changes.Add(args.PropertyName!))));

    grid.FindAll("thead th")[3].Click();

    CollectionAssert.AreEqual(new[] { "SortDescriptors" }, changes);

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    CollectionAssert.AreEqual(new[] { "SortDescriptors", "GroupDescriptors" }, changes);
  }

  [TestMethod]
  public void OnStateChanged_IsNotRaisedForSetState() {
    List<string> changes = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.OnStateChanged, EventCallback.Factory.Create<PixataGridStateEventArgs<Person>>(this, args => changes.Add(args.PropertyName!))));

    grid.InvokeAsync(() => grid.Instance.SetState(SampleState()));

    Assert.AreEqual(0, changes.Count);
  }

  [TestMethod]
  public void SetState_RestoresEditing() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.EditMode, PixataGridEditMode.Inline));

    grid.InvokeAsync(() => grid.Instance.SetState(new() { PageSize = 2, EditItem = People[0] with { Name = "Annie" }, OriginalEditItem = People[0] }));

    Assert.AreEqual("Annie", grid.Find("tr.pixata-blazor-grid-edit-row").Children[1].QuerySelector("input")!.GetAttribute("value"));
    Assert.AreEqual("Annie", grid.Instance.GetState().EditItem!.Name);
    Assert.AreEqual(1, grid.Instance.GetState().OriginalEditItem!.Id);
  }

  [TestMethod]
  public void StorageKey_RestoresTheSavedStateAndSavesChanges() {
    _ctx.Services.AddBlazoredLocalStorage();
    _ctx.JSInterop.Setup<string?>("localStorage.getItem", "people").SetResult(JsonSerializer.Serialize(SampleState()));

    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.StorageKey, "people"));

    CollectionAssert.AreEqual(new[] { "5", "1" }, Ids(grid));

    grid.FindAll("thead th")[1].Click();

    JSRuntimeInvocation saved = _ctx.JSInterop.Invocations["localStorage.setItem"].Last();
    Assert.AreEqual("people", saved.Arguments[0]);
    PixataGridState<Person> state = JsonSerializer.Deserialize<PixataGridState<Person>>((string)saved.Arguments[1]!)!;
    Assert.AreEqual(nameof(Person.Id), state.SortDescriptors.Single().Member);
  }

  [TestMethod]
  public void StorageKey_WithoutLocalStorage_SaysWhatIsMissing() {
    InvalidOperationException ex = Assert.ThrowsException<InvalidOperationException>(() => Render(p => p.Add(g => g.StorageKey, "people")));

    StringAssert.Contains(ex.Message, "AddPixataBlazor");
  }
}
