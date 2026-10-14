using System.Collections.Generic;
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
public class PixataGridGrouping_Tests {
  private BunitContext _ctx = null!;

  public record Person(int Id, string Team, bool Manager, decimal Salary);

  private static readonly List<Person> People = [
    new(1, "B", false, 10m),
    new(2, "A", true, 20m),
    new(3, "B", true, 30m),
    new(4, "A", false, 40m),
    new(5, "C", false, 50m)
  ];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static void Column(RenderTreeBuilder b, string field, RenderFragment<PixataGridGroupTemplateContext>? groupFooter = null,
    RenderFragment<PixataGridAggregateTemplateContext>? footer = null, RenderFragment<PixataGridGroupTemplateContext>? groupHeader = null) {
    b.OpenComponent<PixataGridColumn<Person>>(0);
    b.AddAttribute(1, "Field", field);
    if (groupFooter is not null) {
      b.AddAttribute(2, "GroupFooterTemplate", groupFooter);
    }
    if (footer is not null) {
      b.AddAttribute(3, "FooterTemplate", footer);
    }
    if (groupHeader is not null) {
      b.AddAttribute(4, "GroupHeaderTemplate", groupHeader);
    }
    b.CloseComponent();
  }

  private static RenderFragment Text(string text) =>
    b => b.AddContent(0, text);

  private IRenderedComponent<PixataGrid<Person>> Render(bool aggregates = false, RenderFragment<PixataGridGroupTemplateContext>? teamHeader = null) =>
    _ctx.Render<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, People)
        .Add(g => g.Groupable, true)
        .Add(g => g.GridColumns, b => {
          Column(b, nameof(Person.Id));
          Column(b, nameof(Person.Team), groupHeader: teamHeader);
          Column(b, nameof(Person.Salary),
            groupFooter: aggregates ? ctx => Text($"Group sum {ctx.Sum}") : null,
            footer: aggregates ? ctx => Text($"Sum {ctx.Sum}, max {ctx.Max}") : null);
        });
      if (aggregates) {
        p.Add(g => g.GridAggregates, b => {
          b.OpenComponent<PixataGridAggregate<Person>>(0);
          b.AddAttribute(1, "Field", nameof(Person.Salary));
          b.AddAttribute(2, "Aggregate", GridAggregateType.Sum);
          b.CloseComponent();
          b.OpenComponent<PixataGridAggregate<Person>>(3);
          b.AddAttribute(4, "Field", nameof(Person.Salary));
          b.AddAttribute(5, "Aggregate", GridAggregateType.Max);
          b.CloseComponent();
        });
      }
    });

  // Each row as its type and text, such as "group:Team: A" or "row:2"
  private static List<string> Rows<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    [.. grid.FindAll("tbody tr").Select(tr => tr.ClassList.Contains("pixata-blazor-grid-group-row")
      ? $"group:{tr.TextContent.Trim()}"
      : tr.ClassList.Contains("pixata-blazor-grid-group-footer")
        ? $"footer:{string.Concat(tr.Children.Select(c => c.TextContent.Trim()))}"
        : $"row:{tr.Children.First(c => !c.ClassList.Contains("pixata-blazor-grid-group-cell")).TextContent.Trim()}")];

  private static List<string> Chips<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    [.. grid.FindAll(".pixata-blazor-grid-group-chip").Select(c => c.TextContent.Trim())];

  [TestMethod]
  public void Groupable_ShowsAnEmptyGroupPanel() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    IElement panel = grid.Find(".pixata-blazor-grid-group-panel");
    Assert.AreEqual(PixataGridMessages.Default.GroupPanelEmpty, panel.TextContent.Trim());
    Assert.AreEqual(0, Chips(grid).Count);
    Assert.AreEqual("Team", grid.FindAll("th")[1].GetAttribute("data-pixata-group-field"));
  }

  [TestMethod]
  public void NotGroupable_HasNoPanelAndHeadersCannotBeDragged() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People)
      .Add(g => g.GridColumns, b => Column(b, nameof(Person.Team))));

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-group-panel").Count);
    Assert.IsNull(grid.Find("th").GetAttribute("data-pixata-group-field"));
  }

  [TestMethod]
  public void GroupBy_ShowsAHeaderRowForEachGroupAndIndentsTheRows() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    CollectionAssert.AreEqual(new[] { "group:Team: A", "row:2", "row:4", "group:Team: B", "row:1", "row:3", "group:Team: C", "row:5" }, Rows(grid));
    CollectionAssert.AreEqual(new[] { "Team" }, Chips(grid));
    Assert.AreEqual(1, grid.FindAll("col.pixata-blazor-grid-group-col").Count);
    Assert.AreEqual(1, grid.FindAll("thead th.pixata-blazor-grid-group-cell").Count);
    Assert.AreEqual("4", grid.Find(".pixata-blazor-grid-group-header").GetAttribute("colspan"));
    Assert.AreEqual(1, grid.FindAll("tbody tr.pixata-blazor-grid-row")[0].QuerySelectorAll(".pixata-blazor-grid-group-cell").Length);
  }

  [TestMethod]
  public void RowsAreStripedAcrossGroups() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    // As in Telerik's grid, the striping carries on from one group to the next
    CollectionAssert.AreEqual(new[] { false, true, false, true, false },
      grid.FindAll("tbody tr.pixata-blazor-grid-row").Select(tr => tr.ClassList.Contains("pixata-blazor-grid-alt-row")).ToArray());
  }

  [TestMethod]
  public void NestedGroups_HaveAHeaderRowForEachLevel() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Manager)));

    CollectionAssert.AreEqual(new[] { "group:Team: A", "group:Manager: False", "row:4", "group:Manager: True", "row:2" }, Rows(grid).Take(5).ToArray());
    IElement inner = grid.FindAll(".pixata-blazor-grid-group-row")[1];
    Assert.AreEqual(1, inner.QuerySelectorAll(".pixata-blazor-grid-group-cell").Length);
    Assert.AreEqual("4", inner.QuerySelector(".pixata-blazor-grid-group-header")!.GetAttribute("colspan"));
  }

  [TestMethod]
  public void ClickingTheToggle_CollapsesAndExpandsTheGroup() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(true);
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    grid.Find(".pixata-blazor-grid-group-toggle").Click();

    // The footer stays, as in Telerik's grid
    CollectionAssert.AreEqual(new[] { "group:Team: A", "footer:Group sum 60", "group:Team: B" }, Rows(grid).Take(3).ToArray());
    Assert.AreEqual("false", grid.Find(".pixata-blazor-grid-group-row").GetAttribute("aria-expanded"));

    grid.Find(".pixata-blazor-grid-group-toggle").Click();

    CollectionAssert.AreEqual(new[] { "group:Team: A", "row:2", "row:4", "footer:Group sum 60" }, Rows(grid).Take(4).ToArray());
  }

  [TestMethod]
  public void Aggregates_AreShownInTheGroupFootersAndTheGridFooter() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(true);
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    CollectionAssert.AreEqual(new[] { "footer:Group sum 60", "footer:Group sum 40", "footer:Group sum 50" },
      Rows(grid).Where(r => r.StartsWith("footer:")).ToArray());
    Assert.AreEqual("Sum 150, max 50", grid.Find("tfoot").TextContent.Trim());
  }

  [TestMethod]
  public void Aggregates_CoverAllTheDataNotJustThePage() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(true);
    grid.Render(p => p.Add(g => g.Pageable, true).Add(g => g.PageSize, 2));

    Assert.AreEqual(2, grid.FindAll("tbody tr").Count);
    Assert.AreEqual("Sum 150, max 50", grid.Find("tfoot").TextContent.Trim());
  }

  [TestMethod]
  public void PagingInTheMiddleOfAGroup_StillAggregatesTheWholeGroup() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(true);
    grid.Render(p => p.Add(g => g.Pageable, true).Add(g => g.PageSize, 3));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    // The page has team A's two people and the first of team B's
    CollectionAssert.AreEqual(new[] { "group:Team: A", "row:2", "row:4", "footer:Group sum 60", "group:Team: B", "row:1", "footer:Group sum 40" }, Rows(grid));
  }

  [TestMethod]
  public void GroupHeaderTemplate_ReplacesTheTitle() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(teamHeader: ctx => Text($"Team {ctx.Value} has {ctx.ItemsCount}"));

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    Assert.AreEqual("Team A has 2", grid.Find(".pixata-blazor-grid-group-row").TextContent.Trim());
  }

  [TestMethod]
  public void RemovingAChip_Ungroups() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    grid.Find(".pixata-blazor-grid-group-chip-remove").Click();

    Assert.AreEqual(0, Chips(grid).Count);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-group-row").Count);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-group-cell").Count);
  }

  [TestMethod]
  public void DeleteOnAChip_Ungroups() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    grid.Find(".pixata-blazor-grid-group-chip").KeyDown("Delete");

    Assert.AreEqual(0, Chips(grid).Count);
  }

  [TestMethod]
  public void DroppingAHeaderOnThePanel_GroupsByIt() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.InvokeAsync(() => grid.Instance.GroupDrop(nameof(Person.Team), -1, -1, true));
    grid.InvokeAsync(() => grid.Instance.GroupDrop(nameof(Person.Manager), -1, -1, false));

    // A header dropped anywhere else does nothing
    CollectionAssert.AreEqual(new[] { "Team" }, Chips(grid));

    // A header dropped on a chip goes before it
    grid.InvokeAsync(() => grid.Instance.GroupDrop(nameof(Person.Manager), -1, 0, true));

    CollectionAssert.AreEqual(new[] { "Manager", "Team" }, Chips(grid));
  }

  [TestMethod]
  public void DraggingAChip_MovesItOrRemovesIt() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));
    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Manager)));

    grid.InvokeAsync(() => grid.Instance.GroupDrop(null, 1, 0, true));

    CollectionAssert.AreEqual(new[] { "Manager", "Team" }, Chips(grid));
    Assert.AreEqual("group:Manager: False", Rows(grid)[0]);

    grid.InvokeAsync(() => grid.Instance.GroupDrop(null, 0, -1, false));

    CollectionAssert.AreEqual(new[] { "Team" }, Chips(grid));
  }

  [TestMethod]
  public void OnRead_IsAskedForTheGroupsAndAggregates_AndUsesTheResultsGroups() {
    List<GridRequest> requests = [];
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.OnRead, EventCallback.Factory.Create<PixataGridReadEventArgs<Person>>(this, args => {
        requests.Add(args.Request);
        args.SetResult(People.ToGridResult(args.Request));
      }))
      .Add(g => g.Groupable, true)
      .Add(g => g.Pageable, true)
      .Add(g => g.PageSize, 3)
      .Add(g => g.GridAggregates, b => {
        b.OpenComponent<PixataGridAggregate<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Salary));
        b.AddAttribute(2, "Aggregate", GridAggregateType.Sum);
        b.CloseComponent();
      })
      .Add(g => g.GridColumns, b => {
        Column(b, nameof(Person.Id));
        Column(b, nameof(Person.Team), groupHeader: ctx => Text($"{ctx.Value}: {ctx.ItemsCount}"));
        Column(b, nameof(Person.Salary), footer: ctx => Text($"Sum {ctx.Sum}"));
      }));

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    GridRequest last = requests[^1];
    Assert.AreEqual(nameof(Person.Team), last.Groups.Single().Member);
    Assert.AreEqual(new GridAggregateDescriptor(nameof(Person.Salary), GridAggregateType.Sum), last.Aggregates.Single());
    // The counts are for the whole groups, which only the OnRead handler knows, as the page only has the first of team B
    CollectionAssert.AreEqual(new[] { "A: 2", "B: 2" }, grid.FindAll(".pixata-blazor-grid-group-row").Select(r => r.TextContent.Trim()).ToArray());
    Assert.AreEqual("Sum 150", grid.Find("tfoot").TextContent.Trim());
  }

  [TestMethod]
  public void OnRead_WithoutGroupsInTheResult_GroupsThePage() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.OnRead, EventCallback.Factory.Create<PixataGridReadEventArgs<Person>>(this, args => {
        GridResult<Person> result = People.ToGridResult(args.Request);
        args.Data = result.Data;
        args.Total = result.Total;
      }))
      .Add(g => g.Groupable, true)
      .Add(g => g.GridColumns, b => {
        Column(b, nameof(Person.Id));
        Column(b, nameof(Person.Team));
      }));

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    CollectionAssert.AreEqual(new[] { "group:Team: A", "row:2", "row:4", "group:Team: B" }, Rows(grid).Take(4).ToArray());
  }

  [TestMethod]
  public void Grouping_GoesBackToTheFirstPage() {
    int? pageChanged = null;
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    grid.Render(p => p
      .Add(g => g.Pageable, true)
      .Add(g => g.PageSize, 2)
      .Add(g => g.Page, 2)
      .Add(g => g.PageChanged, EventCallback.Factory.Create<int>(this, page => pageChanged = page)));

    grid.InvokeAsync(() => grid.Instance.GroupBy(nameof(Person.Team)));

    Assert.AreEqual(1, pageChanged);
    CollectionAssert.AreEqual(new[] { "group:Team: A", "row:2", "row:4" }, Rows(grid));
  }
}
