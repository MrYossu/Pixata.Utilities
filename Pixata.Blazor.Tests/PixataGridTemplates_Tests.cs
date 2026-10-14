using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridTemplates_Tests {
  private BunitContext _ctx = null!;

  public record Person(string Name, int Age);

  private static readonly List<Person> People = [new("Alice", 30), new("Bob", 60)];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataGrid<Person>> Render(Action<ComponentParameterCollectionBuilder<PixataGrid<Person>>>? grid = null, Action<RenderTreeBuilder>? ageColumn = null) =>
    _ctx.Render<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, People)
        .Add(g => g.GridColumns, b => {
          b.OpenComponent<PixataGridColumn<Person>>(0);
          b.AddAttribute(1, "Title", "Name");
          b.AddAttribute(2, "Template", (RenderFragment<Person>)(person => builder => {
            builder.OpenElement(0, "strong");
            builder.AddContent(1, person.Name);
            builder.CloseElement();
          }));
          b.CloseComponent();
          b.OpenComponent<PixataGridColumn<Person>>(10);
          b.AddAttribute(11, "Field", nameof(Person.Age));
          ageColumn?.Invoke(b);
          b.CloseComponent();
        });
      grid?.Invoke(p);
    });

  [TestMethod]
  public void Template_RendersTheCellContent() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Assert.AreEqual("<strong>Alice</strong>", grid.Find("tbody td").InnerHtml.Trim());
    Assert.AreEqual("Name", grid.Find("th").TextContent.Trim());
  }

  [TestMethod]
  public void HeaderTemplate_ReplacesTheTitle() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(ageColumn: b => b.AddAttribute(12, "HeaderTemplate", (RenderFragment)(builder => builder.AddMarkupContent(0, "Age <em>(years)</em>"))));

    Assert.AreEqual("Age <em>(years)</em>", grid.FindAll(".pixata-blazor-grid-column-title")[1].InnerHtml.Trim());
  }

  [TestMethod]
  public void OnRowRenderAndOnCellRender_AddClasses() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(
      p => p.Add(g => g.OnRowRender, args => args.Class = args.Item.Name == "Bob" ? "bob" : null),
      b => b.AddAttribute(12, "OnCellRender", (Action<PixataGridCellRenderEventArgs<Person>>)(args => args.Class = (int)args.Value! > 50 ? "old" : null)));

    IReadOnlyList<IElement> rows = grid.FindAll("tbody tr");
    Assert.IsFalse(rows[0].ClassList.Contains("bob"));
    Assert.IsTrue(rows[1].ClassList.Contains("bob"));
    Assert.IsFalse(rows[0].QuerySelectorAll("td")[1].ClassList.Contains("old"));
    Assert.IsTrue(rows[1].QuerySelectorAll("td")[1].ClassList.Contains("old"));
  }

  [TestMethod]
  public void RowEvents_GiveTheItemAndTheColumn() {
    List<string> events = [];
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p
      .Add(g => g.OnRowClick, args => events.Add($"click {args.Item.Name} {args.Field}"))
      .Add(g => g.OnRowDoubleClick, args => events.Add($"double {args.Item.Name} {args.Field}"))
      .Add(g => g.OnRowContextMenu, args => events.Add($"menu {args.Item.Name} {args.EventArgs.ClientX}")));

    grid.FindAll("tbody td")[1].Click();
    grid.FindAll("tbody td")[2].DoubleClick();
    grid.FindAll("tbody td")[3].ContextMenu(new MouseEventArgs { ClientX = 42 });

    CollectionAssert.AreEqual(new[] { "click Alice Age", "double Bob ", "menu Bob 42" }, events);
  }

  [TestMethod]
  public void RowTemplate_ReplacesTheCells() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(p => p.Add(g => g.RowTemplate, person => builder => {
      builder.OpenElement(0, "td");
      builder.AddAttribute(1, "colspan", 2);
      builder.AddContent(2, $"{person.Name} is {person.Age}");
      builder.CloseElement();
    }));

    CollectionAssert.AreEqual(new[] { "Alice is 30", "Bob is 60" }, grid.FindAll("tbody td").Select(td => td.TextContent).ToArray());
  }

  [TestMethod]
  public void LoaderTemplate_ReplacesTheDots() {
    TaskCompletionSource slow = new();
    int call = 0;
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.OnRead, EventCallback.Factory.Create<PixataGridReadEventArgs<Person>>(this, async args => {
        call++;
        if (call == 2) {
          await slow.Task;
        }
        args.SetResult(People.ToGridResult(args.Request));
      }))
      .Add(g => g.LoaderTemplate, builder => builder.AddContent(0, "Hang on"))
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Name));
        b.CloseComponent();
      }));

    grid.InvokeAsync(grid.Instance.Rebind);
    grid.WaitForAssertion(() => Assert.AreEqual(2, call));

    grid.WaitForAssertion(() => Assert.AreEqual("Hang on", grid.Find(".pixata-blazor-grid-loader-template").TextContent), TimeSpan.FromSeconds(3));
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-loader").Count);
    grid.InvokeAsync(() => slow.SetResult()).Wait();
  }
}
