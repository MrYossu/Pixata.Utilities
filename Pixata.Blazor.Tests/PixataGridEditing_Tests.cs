using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGridEditing_Tests {
  private BunitContext _ctx = null!;

  public class Person {
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = "";

    public int Age { get; set; }
    public string Code { get; } = "X";
  }

  private List<Person> _people = null!;
  private readonly List<string> _events = [];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _people = [new() { Id = 1, Name = "Alice", Age = 30 }, new() { Id = 2, Name = "Bob", Age = 40 }];
    _events.Clear();
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static void Button(RenderTreeBuilder b, int seq, string command, bool showInEdit = false) {
    b.OpenComponent<PixataGridCommandButton<Person>>(seq);
    b.AddAttribute(seq + 1, "Command", command);
    b.AddAttribute(seq + 2, "ShowInEdit", showInEdit);
    b.AddAttribute(seq + 3, "ChildContent", (RenderFragment)(c => c.AddContent(0, command)));
    b.CloseComponent();
  }

  private IRenderedComponent<PixataGrid<Person>> Render(PixataGridEditMode mode = PixataGridEditMode.Inline, bool confirmDelete = false, Action<ComponentParameterCollectionBuilder<PixataGrid<Person>>>? extra = null) =>
    _ctx.RenderComponent<PixataGrid<Person>>(p => {
      p.Add(g => g.Data, _people)
        .Add(g => g.EditMode, mode)
        .Add(g => g.ConfirmDelete, confirmDelete)
        .Add(g => g.OnCreate, args => {
          _events.Add($"create {args.Item.Name}");
          _people.Add(args.Item);
        })
        .Add(g => g.OnUpdate, args => {
          _events.Add($"update {args.Item.Name}");
          _people[_people.FindIndex(x => x.Id == args.Item.Id)] = args.Item;
        })
        .Add(g => g.OnDelete, args => {
          _events.Add($"delete {args.Item.Name}");
          _people.Remove(args.Item);
        })
        .Add(g => g.OnCancel, args => _events.Add($"cancel {args.Item.Name}"))
        .Add(g => g.GridToolBarTemplate, b => Button(b, 0, "Add"))
        .Add(g => g.GridColumns, b => {
          foreach ((string field, int seq) in new[] { ("Name", 0), ("Age", 10), ("Code", 20) }) {
            b.OpenComponent<PixataGridColumn<Person>>(seq);
            b.AddAttribute(seq + 1, "Field", field);
            b.CloseComponent();
          }
          b.OpenComponent<PixataGridCommandColumn<Person>>(30);
          b.AddAttribute(31, "ChildContent", (RenderFragment)(c => {
            Button(c, 0, "Edit");
            Button(c, 10, "Delete");
            Button(c, 20, "Save", true);
            Button(c, 30, "Cancel", true);
          }));
          b.CloseComponent();
        });
      extra?.Invoke(p);
    });

  private static IElement CommandButton(IElement row, string text) =>
    row.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == text);

  private static IElement Row(IRenderedFragment grid, int index) =>
    grid.FindAll("tbody tr")[index];

  [TestMethod]
  public void Inline_EditShowsEditorsAndOnlyTheEditButtons() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    Assert.AreEqual("Edit Delete", string.Join(" ", Row(grid, 0).QuerySelectorAll("button").Select(b => b.TextContent.Trim())));
    CommandButton(Row(grid, 0), "Edit").Click();

    IElement row = Row(grid, 0);
    Assert.IsTrue(row.ClassList.Contains("pixata-blazor-grid-edit-row"));
    Assert.AreEqual("Alice", row.QuerySelector("input")!.GetAttribute("value"));
    Assert.AreEqual("Save Cancel", string.Join(" ", row.QuerySelectorAll(".pixata-blazor-grid-command-cell button").Select(b => b.TextContent.Trim())));
    // A property with no setter isn't editable
    Assert.AreEqual("X", row.QuerySelectorAll("td")[2].TextContent.Trim());
  }

  [TestMethod]
  public void Inline_SaveRaisesOnUpdateWithTheEditedCopy() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();
    Person original = _people[0];

    CommandButton(Row(grid, 0), "Edit").Click();
    Row(grid, 0).QuerySelector("input")!.Change("Alicia");
    Assert.AreEqual("Alice", original.Name);
    CommandButton(Row(grid, 0), "Save").Click();

    CollectionAssert.AreEqual(new[] { "update Alicia" }, _events);
    Assert.AreNotSame(original, _people[0]);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-edit-row").Count);
    Assert.AreEqual("Alicia", Row(grid, 0).QuerySelector("td")!.TextContent.Trim());
  }

  [TestMethod]
  public void Inline_InvalidValuesStopTheSave() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    CommandButton(Row(grid, 0), "Edit").Click();
    Row(grid, 0).QuerySelector("input")!.Change("");
    CommandButton(Row(grid, 0), "Save").Click();

    Assert.AreEqual(0, _events.Count);
    IElement editor = Row(grid, 0).QuerySelector(".pixata-blazor-grid-textbox")!;
    Assert.IsTrue(editor.ClassList.Contains("pixata-blazor-grid-invalid"));
    Assert.AreEqual("Name is required", editor.GetAttribute("title"));
  }

  [TestMethod]
  public void Inline_CancelLeavesTheOriginalAlone() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    CommandButton(Row(grid, 1), "Edit").Click();
    Row(grid, 1).QuerySelector("input")!.Change("Robert");
    CommandButton(Row(grid, 1), "Cancel").Click();

    CollectionAssert.AreEqual(new[] { "cancel Robert" }, _events);
    Assert.AreEqual("Bob", _people[1].Name);
    Assert.AreEqual("Bob", Row(grid, 1).QuerySelector("td")!.TextContent.Trim());
  }

  [TestMethod]
  public void Inline_AddPutsANewRowAtTheTopAndCreatesIt() {
    IRenderedComponent<PixataGrid<Person>> grid = Render();

    grid.Find(".pixata-blazor-grid-toolbar button").Click();
    Assert.IsTrue(Row(grid, 0).ClassList.Contains("pixata-blazor-grid-edit-row"));
    Assert.AreEqual(3, grid.FindAll("tbody tr").Count);
    Row(grid, 0).QuerySelector("input")!.Change("Carol");
    CommandButton(Row(grid, 0), "Save").Click();

    CollectionAssert.AreEqual(new[] { "create Carol" }, _events);
    Assert.AreEqual(3, _people.Count);
  }

  [TestMethod]
  public void OnModelInit_CreatesTheNewItem() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(extra: p => p.Add(g => g.OnModelInit, () => new Person { Id = 99, Name = "Template" }));

    grid.Find(".pixata-blazor-grid-toolbar button").Click();

    Assert.AreEqual("Template", Row(grid, 0).QuerySelector("input")!.GetAttribute("value"));
  }

  [TestMethod]
  public void OnEdit_CanCancelEditing() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(extra: p => p.Add(g => g.OnEdit, args => args.IsCancelled = args.Item.Name == "Alice"));

    CommandButton(Row(grid, 0), "Edit").Click();

    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-edit-row").Count);
  }

  [TestMethod]
  public void Delete_WithConfirmation_OnlyDeletesAfterOk() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(confirmDelete: true);

    CommandButton(Row(grid, 0), "Delete").Click();
    Assert.AreEqual("Are you sure you want to delete this record?", grid.Find(".pixata-blazor-dialog .pixata-blazor-window-content").TextContent.Trim());
    grid.FindAll(".pixata-blazor-dialog button").Single(b => b.TextContent.Trim() == "Cancel").Click();
    Assert.AreEqual(0, _events.Count);

    CommandButton(Row(grid, 0), "Delete").Click();
    grid.FindAll(".pixata-blazor-dialog button").Single(b => b.TextContent.Trim() == "OK").Click();

    CollectionAssert.AreEqual(new[] { "delete Alice" }, _events);
    Assert.AreEqual(1, grid.FindAll("tbody tr").Count);
  }

  [TestMethod]
  public void Popup_EditsInAWindow() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridEditMode.Popup);

    CommandButton(Row(grid, 1), "Edit").Click();

    IElement window = grid.Find(".pixata-blazor-grid-popup-edit");
    Assert.AreEqual("Edit", window.QuerySelector(".pixata-blazor-window-title")!.TextContent.Trim());
    CollectionAssert.AreEqual(new[] { "Name", "Age", "Code" }, window.QuerySelectorAll(".pixata-blazor-form-label").Select(l => l.TextContent.Trim()).ToArray());
    window.QuerySelector(".pixata-blazor-grid-textbox input")!.Change("Robert");
    grid.FindAll(".pixata-blazor-grid-popup-edit .pixata-blazor-window-actions button").Single(b => b.TextContent.Trim() == "Update").Click();

    CollectionAssert.AreEqual(new[] { "update Robert" }, _events);
    Assert.AreEqual(0, grid.FindAll(".pixata-blazor-grid-popup-edit").Count);
  }

  [TestMethod]
  public void Popup_ShowsValidationMessagesAndAddTitle() {
    IRenderedComponent<PixataGrid<Person>> grid = Render(PixataGridEditMode.Popup, extra: p => p.Add(g => g.GridSettings, b => {
      b.OpenComponent<PixataGridPopupEditFormSettings<Person>>(0);
      b.AddAttribute(1, "Columns", 2);
      b.CloseComponent();
    }));

    grid.Find(".pixata-blazor-grid-toolbar button").Click();
    Assert.AreEqual("Add new record", grid.Find(".pixata-blazor-window-title").TextContent.Trim());
    Assert.IsTrue(grid.Find(".pixata-blazor-form-layout").GetAttribute("style")!.Contains("repeat(2"));
    grid.FindAll(".pixata-blazor-grid-popup-edit .pixata-blazor-window-actions button").Single(b => b.TextContent.Trim() == "Update").Click();

    Assert.AreEqual("Name is required", grid.Find(".pixata-blazor-form-error").TextContent.Trim());
    Assert.AreEqual(0, _events.Count);
  }

  [TestMethod]
  public void CustomCommand_GetsTheRowsItem() {
    string? clicked = null;
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.RenderComponent<PixataGrid<Person>>(p => p
      .Add(g => g.Data, _people)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridCommandColumn<Person>>(0);
        b.AddAttribute(1, "ChildContent", (RenderFragment)(c => {
          c.OpenComponent<PixataGridCommandButton<Person>>(0);
          c.AddAttribute(1, "OnClick", EventCallback.Factory.Create<PixataGridCommandEventArgs<Person>>(this, args => clicked = args.Item.Name));
          c.AddAttribute(2, "ChildContent", (RenderFragment)(t => t.AddContent(0, "Hello")));
          c.CloseComponent();
        }));
        b.CloseComponent();
      }));

    CommandButton(Row(grid, 1), "Hello").Click();

    Assert.AreEqual("Bob", clicked);
  }
}
