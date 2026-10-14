using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components.Grid;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataGrid_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  public class Address {
    public string City { get; set; } = "";
  }

  public enum Colour {
    Red,
    Green
  }

  public class Person {
    public int Id { get; set; }
    public string FirstName { get; set; } = "";

    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [DisplayFormat(DataFormatString = "{0:dd MMM yyyy}")]
    public DateTime DateOfBirth { get; set; }

    public decimal Salary { get; set; }
    public Colour Colour { get; set; }

    [Display(AutoGenerateField = false)]
    public string Secret { get; set; } = "";

    [ScaffoldColumn(false)]
    public string AlsoSecret { get; set; } = "";

    public Address? Address { get; set; }
  }

  private static List<Person> People() => [
    new() { Id = 1, FirstName = "Jacob", Email = "jacob@example.com", DateOfBirth = new(1957, 11, 27), Salary = 1234.5m, Address = new() { City = "Leeds" } },
    new() { Id = 2, FirstName = "Elijah", Email = "elijah@example.com", DateOfBirth = new(1958, 8, 2), Salary = 99m, Colour = Colour.Green }
  ];

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  private static string[] Headers<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    [.. grid.FindAll("th").Select(th => th.TextContent.Trim())];

  private static string[][] Cells<TComponent>(IRenderedComponent<TComponent> grid) where TComponent : IComponent =>
    [.. grid.FindAll("tbody tr").Select(tr => tr.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToArray())];

  [TestMethod]
  public void PixataGrid_DeclaredColumns_RenderHeadersAndCellsInOrder() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.FirstName));
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(2);
        b.AddAttribute(3, "Field", nameof(Person.Id));
        b.CloseComponent();
      }));

    CollectionAssert.AreEqual(new[] { "First name", "Id" }, Headers(grid));
    string[][] cells = Cells(grid);
    CollectionAssert.AreEqual(new[] { "Jacob", "1" }, cells[0]);
    CollectionAssert.AreEqual(new[] { "Elijah", "2" }, cells[1]);
  }

  [TestMethod]
  public void PixataGrid_Title_UsesParameterThenDisplayAttributeThenSplitFieldName() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithColumns(
      ("Field", nameof(Person.FirstName), "Title", "Given name"),
      ("Field", nameof(Person.Email), null, null),
      ("Field", nameof(Person.DateOfBirth), null, null),
      ("Field", "Address.City", null, null));

    CollectionAssert.AreEqual(new[] { "Given name", "Email address", "Date of birth", "City" }, Headers(grid));
  }

  [TestMethod]
  public void PixataGrid_Formats_UseParameterOrAttribute() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithColumns(
      ("Field", nameof(Person.DateOfBirth), null, null),
      ("Field", nameof(Person.Salary), "DisplayFormat", "{0:C2}"));

    CollectionAssert.AreEqual(new[] { "27 Nov 1957", "£1,234.50" }, Cells(grid)[0]);
  }

  [TestMethod]
  public void PixataGrid_NestedField_IsNullSafe() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithColumns(("Field", "Address.City", null, null));

    string[][] cells = Cells(grid);
    Assert.AreEqual("Leeds", cells[0][0]);
    Assert.AreEqual("", cells[1][0]);
  }

  [TestMethod]
  public void PixataGrid_ColumnNotVisible_IsNotRendered() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.FirstName));
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(2);
        b.AddAttribute(3, "Field", nameof(Person.Id));
        b.AddAttribute(4, "Visible", (bool?)false);
        b.CloseComponent();
      }));

    CollectionAssert.AreEqual(new[] { "First name" }, Headers(grid));
    Assert.AreEqual(1, grid.FindAll("col").Count);
  }

  [TestMethod]
  public void PixataGrid_ColumnSettings_SetClassesAndWidth() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", nameof(Person.Salary));
        b.AddAttribute(2, "TextAlign", (PixataGridTextAlign?)PixataGridTextAlign.Right);
        b.AddAttribute(3, "Class", "money");
        b.AddAttribute(4, "HeaderClass", "money-header");
        b.AddAttribute(5, "Width", "120px");
        b.CloseComponent();
      }));

    IElement td = grid.Find("tbody td");
    Assert.IsTrue(td.ClassList.Contains("pixata-blazor-grid-text-right"));
    Assert.IsTrue(td.ClassList.Contains("money"));
    Assert.IsTrue(grid.Find("th").ClassList.Contains("money-header"));
    Assert.AreEqual("width: 120px", grid.Find("col").GetAttribute("style"));
  }

  [TestMethod]
  public void PixataGrid_AutoGenerateColumns_SkipsComplexAndExcludedProperties() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.AutoGenerateColumns, true));

    CollectionAssert.AreEqual(new[] { "Id", "First name", "Email address", "Date of birth", "Salary", "Colour" }, Headers(grid));
    Assert.AreEqual("Green", Cells(grid)[1][5]);
  }

  [TestMethod]
  public void PixataGrid_AutoGenerateColumns_GoAfterDeclaredOnesUnlessPlaced() {
    IRenderedComponent<PixataGrid<Person>> after = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.AutoGenerateColumns, true)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridColumn<Person>>(0);
        b.AddAttribute(1, "Field", "Address.City");
        b.CloseComponent();
      }));
    IRenderedComponent<PixataGrid<Person>> placed = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.AutoGenerateColumns, true)
      .Add(g => g.GridColumns, b => {
        b.OpenComponent<PixataGridAutoGeneratedColumns<Person>>(0);
        b.AddAttribute(1, "ColumnWidth", "50px");
        b.CloseComponent();
        b.OpenComponent<PixataGridColumn<Person>>(2);
        b.AddAttribute(3, "Field", "Address.City");
        b.CloseComponent();
      }));

    CollectionAssert.AreEqual(new[] { "City", "Id", "First name", "Email address", "Date of birth", "Salary", "Colour" }, Headers(after));
    CollectionAssert.AreEqual(new[] { "Id", "First name", "Email address", "Date of birth", "Salary", "Colour", "City" }, Headers(placed));
    Assert.AreEqual("width: 50px", placed.Find("col").GetAttribute("style"));
  }

  [TestMethod]
  public void PixataGrid_NoData_ShowsDefaultMessageOrTemplate() {
    IRenderedComponent<PixataGrid<Person>> plain = RenderWithColumns([], ("Field", nameof(Person.FirstName), null, null));
    IRenderedComponent<PixataGrid<Person>> templated = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, [])
      .Add(g => g.NoDataTemplate, b => b.AddContent(0, "Nobody here")));

    Assert.AreEqual("No records available.", plain.Find(".pixata-blazor-grid-norecords td").TextContent.Trim());
    Assert.AreEqual("Nobody here", templated.Find(".pixata-blazor-grid-norecords td").TextContent.Trim());
  }

  [TestMethod]
  public void PixataGrid_GridSettings_SetClassesAndStyle() {
    IRenderedComponent<PixataGrid<Person>> grid = _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, People())
      .Add(g => g.Size, PixataGridSize.Small)
      .Add(g => g.Class, "mine")
      .Add(g => g.Height, "300px")
      .Add(g => g.Width, "50%"));

    IElement root = grid.Find(".pixata-blazor-grid");
    Assert.IsTrue(root.ClassList.Contains("pixata-blazor-grid-sm"));
    Assert.IsTrue(root.ClassList.Contains("mine"));
    Assert.AreEqual("height: 300px; width: 50%;", root.GetAttribute("style"));
  }

  [TestMethod]
  public void PixataGrid_AlternateRows_GetAltClass() {
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithColumns(("Field", nameof(Person.FirstName), null, null));

    IReadOnlyList<IElement> rows = grid.FindAll("tbody tr");
    Assert.IsFalse(rows[0].ClassList.Contains("pixata-blazor-grid-alt-row"));
    Assert.IsTrue(rows[1].ClassList.Contains("pixata-blazor-grid-alt-row"));
  }

  [TestMethod]
  public void PixataGrid_ObservableCollection_UpdatesWhenChanged() {
    ObservableCollection<Person> people = [.. People()];
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithColumns(people, ("Field", nameof(Person.FirstName), null, null));

    people.Add(new() { FirstName = "Samuel" });

    grid.WaitForAssertion(() => Assert.AreEqual(3, grid.FindAll("tbody tr").Count));
  }

  [TestMethod]
  public void PixataGrid_Rebind_PicksUpChangesToTheCollection() {
    List<Person> people = People();
    IRenderedComponent<PixataGrid<Person>> grid = RenderWithColumns(people, ("Field", nameof(Person.FirstName), null, null));

    people.RemoveAt(0);
    grid.InvokeAsync(grid.Instance.Rebind);

    Assert.AreEqual(1, grid.FindAll("tbody tr").Count);
  }

  [TestMethod]
  public void PixataGrid_BadFieldName_Throws() =>
    Assert.ThrowsException<ArgumentException>(() => RenderWithColumns(("Field", "firstName", null, null)));

  [TestMethod]
  public void PixataGridColumn_OutsideAGrid_Throws() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataGridColumn<Person>>());

  private IRenderedComponent<PixataGrid<Person>> RenderWithColumns(params (string Name, string Value, string? Name2, string? Value2)[] columns) =>
    RenderWithColumns(People(), columns);

  private IRenderedComponent<PixataGrid<Person>> RenderWithColumns(IEnumerable<Person> data, params (string Name, string Value, string? Name2, string? Value2)[] columns) =>
    _ctx.Render<PixataGrid<Person>>(p => p
      .Add(g => g.Data, data)
      .Add(g => g.GridColumns, b => {
        int seq = 0;
        foreach ((string name, string value, string? name2, string? value2) in columns) {
          b.OpenComponent<PixataGridColumn<Person>>(seq++);
          b.AddAttribute(seq++, name, value);
          if (name2 is not null) {
            b.AddAttribute(seq++, name2, value2);
          }
          b.CloseComponent();
        }
      }));
}
