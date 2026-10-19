using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

/// <summary>
/// Data and helpers shared by the drop-down tests
/// </summary>
public static class DropDownTestData {
  public record Country(int Id, string Name, string Continent);

  public class Model {
    public int CountryId { get; set; }
    public string? Fruit { get; set; }
    public List<int> CountryIds { get; set; } = [];
  }

  public static readonly List<string> Fruit = ["Apple", "Apricot", "Banana", "Cherry", "Damson"];

  public static readonly List<Country> Countries = [
    new(1, "France", "Europe"),
    new(2, "Japan", "Asia"),
    new(3, "Germany", "Europe"),
    new(4, "India", "Asia"),
    new(5, "Kenya", "Africa")
  ];

  public static KeyboardEventArgs Key(string key, bool alt = false) =>
    new() { Key = key, AltKey = alt };

  public static IReadOnlyList<string> Options<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.FindAll("[role=option]").Select(o => o.TextContent.Trim()).ToList();

  public static string? Focused<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.FindAll("[role=option].pixata-blazor-focus").SingleOrDefault()?.TextContent.Trim();
}

[TestClass]
public class DropDowns_Tests {
  private static IRenderedComponent<PixataDropDownList<string, string>> RenderFruit(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataDropDownList<string, string>>>? extra = null) =>
    context.Render<PixataDropDownList<string, string>>(p => {
      p.Add(c => c.Data, DropDownTestData.Fruit);
      extra?.Invoke(p);
    });

  private static IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> RenderCountries(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataDropDownList<DropDownTestData.Country, int>>>? extra = null) =>
    context.Render<PixataDropDownList<DropDownTestData.Country, int>>(p => {
      p.Add(c => c.Data, DropDownTestData.Countries)
        .Add(c => c.TextField, nameof(DropDownTestData.Country.Name))
        .Add(c => c.ValueField, nameof(DropDownTestData.Country.Id));
      extra?.Invoke(p);
    });

  [TestMethod]
  public void DropDownList_NoValue_ShowsDefaultText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p.Add(c => c.DefaultText, "Pick one"));
    Assert.AreEqual("Pick one", cut.Find(".pixata-blazor-input-value-text").TextContent);
    Assert.AreEqual("combobox", cut.Find(".pixata-blazor-dropdownlist").GetAttribute("role"));
    Assert.AreEqual("false", cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-expanded"));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public void DropDownList_ModelData_ShowsTheTextOfTheItemWithTheValue() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p.Add(c => c.Value, 4));
    Assert.AreEqual("India", cut.Find(".pixata-blazor-input-value-text").TextContent);
  }

  [TestMethod]
  public void DropDownList_ValueChangedByTheParent_UpdatesTheText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p.Add(c => c.Value, 4));
    cut.Render(p => p.Add(c => c.Value, 2));
    Assert.AreEqual("Japan", cut.Find(".pixata-blazor-input-value-text").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_Click_OpensTheListWithTheOptionLabel() {
    using BunitContext context = PixataTestContext.Create();
    bool opened = false;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.DefaultText, "Pick one")
      .Add(c => c.OnOpen, (PixataDropDownOpenEventArgs _) => opened = true));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    Assert.IsTrue(opened);
    Assert.AreEqual("true", cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-expanded"));
    IElement listbox = cut.Find("[role=listbox]");
    Assert.AreEqual(listbox.Id, cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-controls"));
    CollectionAssert.AreEqual(new[] { "Pick one", "Apple", "Apricot", "Banana", "Cherry", "Damson" }, DropDownTestData.Options(cut).ToList());
    // Nothing is selected, so the option label has the focus
    Assert.AreEqual("Pick one", DropDownTestData.Focused(cut));
  }

  [TestMethod]
  public async Task DropDownList_ClickingAnItem_SelectsItAndCloses() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    object? changed = null;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnChange, (object v) => changed = v));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    await cut.FindAll("[role=option]")[2].ClickAsync(new());
    Assert.AreEqual("Banana", value);
    Assert.AreEqual("Banana", changed);
    Assert.AreEqual("Banana", cut.Find(".pixata-blazor-input-value-text").TextContent);
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public void DropDownList_Icon_ShowsJustTheIconWithTheSelectedItemAsItsName() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, "Banana")
      .Add(c => c.DefaultText, "Pick one")
      .Add(c => c.ShowClearButton, true)
      .Add(c => c.Icon, PixataSvgIcons.MoreVertical)
      .Add(c => c.AriaLabel, "Fruit"));
    IElement box = cut.Find(".pixata-blazor-dropdownlist");
    Assert.IsTrue(box.ClassList.Contains("pixata-blazor-dropdownlist-icon-only"));
    Assert.AreEqual(1, box.QuerySelectorAll(".pixata-blazor-dropdownlist-icon .pixata-blazor-svg-icon").Length);
    Assert.AreEqual(0, box.QuerySelectorAll(".pixata-blazor-input-value-text, .pixata-blazor-dropdownlist-button, .pixata-blazor-clear-value").Length);
    Assert.AreEqual("Banana", box.GetAttribute("title"));
    Assert.AreEqual("Fruit: Banana", box.GetAttribute("aria-label"));
  }

  [TestMethod]
  public async Task DropDownList_IconFontClass_OpensTheListAndSelectingUpdatesTheName() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Icon, "fas fa-filter")
      .Add(c => c.ValueChanged, (string v) => value = v));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-dropdownlist-icon .pixata-blazor-font-icon.fa-filter").Count);
    // Nothing is selected, so there's nothing to say
    Assert.IsNull(cut.Find(".pixata-blazor-dropdownlist").GetAttribute("title"));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    CollectionAssert.AreEqual(new[] { "Apple", "Apricot", "Banana", "Cherry", "Damson" }, DropDownTestData.Options(cut).ToList());
    await cut.FindAll("[role=option]")[3].ClickAsync(new());
    Assert.AreEqual("Cherry", value);
    Assert.AreEqual("Cherry", cut.Find(".pixata-blazor-dropdownlist").GetAttribute("title"));
    Assert.AreEqual("Cherry", cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-label"));
  }

  [TestMethod]
  public void DropDownList_IconTemplate_IsRenderedInPlaceOfTheIcon() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.IconTemplate, "<b class=\"mine\">&lt;</b>"));
    Assert.IsTrue(cut.Find(".pixata-blazor-dropdownlist").ClassList.Contains("pixata-blazor-dropdownlist-icon-only"));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-dropdownlist-icon b.mine").Count);
  }

  [TestMethod]
  public async Task DropDownList_SelectedItem_IsMarkedSelectedAndFocusedWhenOpened() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p.Add(c => c.Value, 3));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    IElement selected = cut.Find("[role=option][aria-selected=true]");
    Assert.AreEqual("Germany", selected.TextContent.Trim());
    Assert.IsTrue(selected.ClassList.Contains("pixata-blazor-selected"));
    Assert.AreEqual(selected.Id, cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-activedescendant"));
  }

  [TestMethod]
  public async Task DropDownList_ArrowKeysWhenClosed_ChangeTheValue() {
    using BunitContext context = PixataTestContext.Create();
    string? value = "Banana";
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (string v) => value = v));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    await element.KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("Cherry", value);
    await element.KeyDownAsync(DropDownTestData.Key("ArrowUp"));
    await element.KeyDownAsync(DropDownTestData.Key("ArrowUp"));
    Assert.AreEqual("Apricot", value);
    await element.KeyDownAsync(DropDownTestData.Key("End"));
    Assert.AreEqual("Damson", value);
    await element.KeyDownAsync(DropDownTestData.Key("Home"));
    Assert.AreEqual("Apple", value);
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_ArrowUpOnTheDefaultText_StaysThere() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    int changes = 0;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.DefaultText, "Pick one")
      .Add(c => c.ValueChanged, (string v) => {
        value = v;
        changes++;
      }));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    // The default text is at the top of the list, so there is nothing above it to move to
    await element.KeyDownAsync(DropDownTestData.Key("ArrowUp"));
    Assert.AreEqual(0, changes);
    Assert.AreEqual("Pick one", cut.Find(".pixata-blazor-input-value-text").TextContent);
    await element.KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("Apple", value);
  }

  [TestMethod]
  public async Task DropDownList_ArrowUpFromTheFirstItem_GoesBackToTheDefaultText() {
    using BunitContext context = PixataTestContext.Create();
    string? value = "Apple";
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.DefaultText, "Pick one")
      .Add(c => c.ValueChanged, (string v) => value = v));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowUp"));
    Assert.IsNull(value);
    Assert.AreEqual("Pick one", cut.Find(".pixata-blazor-input-value-text").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_TypeAhead_SelectsTheNextItemStartingWithTheLetter() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p.Add(c => c.ValueChanged, (string v) => value = v));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    await element.KeyDownAsync(DropDownTestData.Key("a"));
    Assert.AreEqual("Apple", value);
    await element.KeyDownAsync(DropDownTestData.Key("a"));
    Assert.AreEqual("Apricot", value);
    await element.KeyDownAsync(DropDownTestData.Key("d"));
    // "ad" matches nothing, so the value stays put
    Assert.AreEqual("Apricot", value);
  }

  [TestMethod]
  public async Task DropDownList_KeyboardWhenOpen_MovesTheFocusAndEnterSelects() {
    using BunitContext context = PixataTestContext.Create();
    string? value = "Apple";
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (string v) => value = v));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    await element.KeyDownAsync(DropDownTestData.Key("ArrowDown", alt: true));
    Assert.AreEqual("Apple", DropDownTestData.Focused(cut));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("Banana", DropDownTestData.Focused(cut));
    // Moving the focus doesn't change the value until Enter is pressed
    Assert.AreEqual("Apple", value);
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("End"));
    Assert.AreEqual("Damson", DropDownTestData.Focused(cut));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Home"));
    Assert.AreEqual("Apple", DropDownTestData.Focused(cut));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("c"));
    Assert.AreEqual("Cherry", DropDownTestData.Focused(cut));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual("Cherry", value);
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_Escape_ClosesWithoutChangingTheValue() {
    using BunitContext context = PixataTestContext.Create();
    bool closed = false;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, "Apple")
      .Add(c => c.OnClose, (PixataDropDownCloseEventArgs _) => closed = true));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Enter"));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Escape"));
    Assert.IsTrue(closed);
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    Assert.AreEqual("Apple", cut.Find(".pixata-blazor-input-value-text").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_OnOpenCancelled_StaysClosed() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.OnOpen, (PixataDropDownOpenEventArgs e) => e.IsCancelled = true));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_OnCloseCancelled_StaysOpen() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.OnClose, (PixataDropDownCloseEventArgs e) => e.IsCancelled = true));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Escape"));
    Assert.AreEqual(1, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_FocusLeavingTheComponent_ClosesTheListAndRaisesOnBlur() {
    using BunitContext context = PixataTestContext.Create();
    bool blurred = false;
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p.Add(c => c.OnBlur, () => blurred = true));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    Assert.IsTrue(blurred);
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_Disabled_CannotBeOpenedOrFocused() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p.Add(c => c.Enabled, false));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    Assert.AreEqual("-1", element.GetAttribute("tabindex"));
    Assert.AreEqual("true", element.GetAttribute("aria-disabled"));
    Assert.IsTrue(element.ClassList.Contains("pixata-blazor-disabled"));
    await element.ClickAsync(new());
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_ReadOnly_CannotBeOpenedOrChanged() {
    using BunitContext context = PixataTestContext.Create();
    string? value = "Apple";
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ReadOnly, true)
      .Add(c => c.ValueChanged, (string v) => value = v));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    Assert.AreEqual("0", element.GetAttribute("tabindex"));
    Assert.AreEqual("true", element.GetAttribute("aria-readonly"));
    await element.ClickAsync(new());
    await element.KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    Assert.AreEqual("Apple", value);
  }

  [TestMethod]
  public async Task DropDownList_Filterable_FiltersWithTheOperator() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.FilterOperator, PixataStringFilterOperator.Contains)
      .Add(c => c.FilterPlaceholder, "Search"));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    IElement filter = cut.Find(".pixata-blazor-dropdown-filter input");
    Assert.AreEqual("Search", filter.GetAttribute("placeholder"));
    Assert.AreEqual("searchbox", filter.GetAttribute("role"));
    await filter.InputAsync(new ChangeEventArgs { Value = "an" });
    CollectionAssert.AreEqual(new[] { "France", "Japan", "Germany" }, DropDownTestData.Options(cut).ToList());
    Assert.AreEqual("France", DropDownTestData.Focused(cut));
    await cut.Find(".pixata-blazor-dropdown-filter input").InputAsync(new ChangeEventArgs { Value = "xyz" });
    Assert.AreEqual(0, cut.FindAll("[role=option]").Count);
    Assert.AreEqual("No data found.", cut.Find(".pixata-blazor-no-data").TextContent.Trim());
  }

  [TestMethod]
  public async Task DropDownList_FilterKeys_NavigateAndSelect() {
    using BunitContext context = PixataTestContext.Create();
    int value = 0;
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    await cut.Find(".pixata-blazor-dropdown-filter input").InputAsync(new ChangeEventArgs { Value = "i" });
    CollectionAssert.AreEqual(new[] { "India" }, DropDownTestData.Options(cut).ToList());
    await cut.Find(".pixata-blazor-dropdown-filter input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual(4, value);
    // Closing removes the filter
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    Assert.AreEqual(5, cut.FindAll("[role=option]").Count);
  }

  [TestMethod]
  public void PixataStringFilterOperator_Matches_IgnoresCase() {
    Assert.IsTrue(PixataStringFilterOperator.StartsWith.Matches("Banana", "ba"));
    Assert.IsFalse(PixataStringFilterOperator.StartsWith.Matches("Banana", "na"));
    Assert.IsTrue(PixataStringFilterOperator.EndsWith.Matches("Banana", "NA"));
    Assert.IsTrue(PixataStringFilterOperator.Contains.Matches("Banana", "nan"));
    Assert.IsTrue(PixataStringFilterOperator.DoesNotContain.Matches("Banana", "x"));
    Assert.IsTrue(PixataStringFilterOperator.IsEqualTo.Matches("Banana", "banana"));
    Assert.IsTrue(PixataStringFilterOperator.IsContainedIn.Matches("nan", "Banana"));
    Assert.IsTrue(PixataStringFilterOperator.Contains.Matches("Banana", ""));
  }

  [TestMethod]
  public async Task DropDownList_GroupField_GroupsAndSortsTheItems() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.GroupField, nameof(DropDownTestData.Country.Continent)));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    List<string> rows = cut.FindAll(".pixata-blazor-dropdown-list > li").Select(l => l.TextContent.Trim()).ToList();
    CollectionAssert.AreEqual(new[] { "Africa", "Kenya", "Asia", "Japan", "India", "Europe", "France", "Germany" }, rows);
    Assert.AreEqual(3, cut.FindAll(".pixata-blazor-dropdown-group-header[role=presentation]").Count);
    // The keyboard skips the group headers
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("India", DropDownTestData.Focused(cut));
  }

  [TestMethod]
  public async Task DropDownList_Templates_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, 2)
      .Add(c => c.ItemTemplate, country => $"<b class='item'>{country.Name} ({country.Continent})</b>")
      .Add(c => c.ValueTemplate, country => $"<i class='value'>{country.Name}!</i>")
      .Add(c => c.HeaderTemplate, "<span class='header'>Head</span>")
      .Add(c => c.FooterTemplate, "<span class='footer'>Foot</span>"));
    Assert.AreEqual("Japan!", cut.Find("i.value").TextContent);
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    Assert.AreEqual("Japan (Asia)", cut.FindAll("b.item")[1].TextContent);
    Assert.AreEqual("Head", cut.Find(".pixata-blazor-dropdown-header .header").TextContent);
    Assert.AreEqual("Foot", cut.Find(".pixata-blazor-dropdown-footer .footer").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_NoDataTemplate_IsShownWhenThereAreNoItems() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = context.Render<PixataDropDownList<string, string>>(p => p
      .Add(c => c.Data, new List<string>())
      .Add(c => c.NoDataTemplate, "<em>Nothing here</em>"));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    Assert.AreEqual("Nothing here", cut.Find(".pixata-blazor-no-data em").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_ClearButton_ClearsTheValue() {
    using BunitContext context = PixataTestContext.Create();
    string? value = "Apple";
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ShowClearButton, true)
      .Add(c => c.ValueChanged, (string v) => value = v));
    await cut.Find(".pixata-blazor-clear-value").ClickAsync(new());
    Assert.IsNull(value);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
  }

  [TestMethod]
  public void DropDownList_Appearance_IsAppliedToTheElement() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.Id, "fruit")
      .Add(c => c.Class, "mine")
      .Add(c => c.Width, "200px")
      .Add(c => c.Size, PixataThemeConstants.Size.Large)
      .Add(c => c.Rounded, PixataThemeConstants.Rounded.Full)
      .Add(c => c.FillMode, PixataThemeConstants.FillMode.Outline)
      .Add(c => c.AriaLabel, "Fruit")
      .AddUnmatched("title", "Choose a fruit"));
    IElement element = cut.Find(".pixata-blazor-dropdownlist");
    Assert.AreEqual("fruit", element.Id);
    Assert.AreEqual("width:200px;", element.GetAttribute("style"));
    Assert.AreEqual("Fruit", element.GetAttribute("aria-label"));
    Assert.AreEqual("Choose a fruit", element.GetAttribute("title"));
    foreach (string css in new[] { "mine", "pixata-blazor-input-lg", "pixata-blazor-rounded-full", "pixata-blazor-input-outline", "pixata-blazor-dropdown" }) {
      Assert.IsTrue(element.ClassList.Contains(css), css);
    }
  }

  [TestMethod]
  public async Task DropDownList_PopupSettings_AreApplied() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context, p => p
      .Add(c => c.PopupSettings, new PixataPopupSettings { Width = "400px", MaxHeight = "300px", Class = "my-popup" }));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    IElement popup = cut.Find(".pixata-blazor-popup.pixata-blazor-dropdown-popup.pixata-blazor-dropdownlist-popup.my-popup");
    StringAssert.Contains(popup.GetAttribute("style"), "width:400px");
    StringAssert.Contains(popup.GetAttribute("style"), "--pixata-blazor-popup-max-height:300px");
  }

  [TestMethod]
  public async Task DropDownList_InAnEditForm_NotifiesTheFormAndShowsTheInvalidState() {
    using BunitContext context = PixataTestContext.Create();
    DropDownTestData.Model model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.CountryId)
      .Add(c => c.ValueChanged, (int v) => model.CountryId = v)
      .Add(c => c.ValueExpression, () => model.CountryId));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual(1, model.CountryId);
    CollectionAssert.AreEqual(new[] { nameof(DropDownTestData.Model.CountryId) }, changed);
    Assert.IsFalse(cut.Find(".pixata-blazor-dropdownlist").ClassList.Contains("pixata-blazor-invalid"));
    await cut.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(DropDownTestData.Model.CountryId)), "Required");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(cut.Find(".pixata-blazor-dropdownlist").ClassList.Contains("pixata-blazor-invalid"));
    Assert.AreEqual("true", cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-invalid"));
  }

  [TestMethod]
  public async Task DropDownList_OnRead_SuppliesTheDataAndIsCalledWithTheFilter() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataDropDownReadEventArgs> reads = [];
    IRenderedComponent<PixataDropDownList<DropDownTestData.Country, int>> cut = context.Render<PixataDropDownList<DropDownTestData.Country, int>>(p => p
      .Add(c => c.TextField, nameof(DropDownTestData.Country.Name))
      .Add(c => c.ValueField, nameof(DropDownTestData.Country.Id))
      .Add(c => c.Value, 5)
      .Add(c => c.Filterable, true)
      .Add(c => c.FilterDebounceDelay, 0)
      .Add(c => c.OnRead, (PixataDropDownReadEventArgs e) => {
        reads.Add(e);
        e.Data = DropDownTestData.Countries.Where(country => e.FilterOperator.Matches(country.Name, e.Filter)).ToList();
      }));
    Assert.AreEqual(1, reads.Count);
    Assert.AreEqual("", reads[0].Filter);
    Assert.AreEqual(0, reads[0].Take);
    Assert.AreEqual("Kenya", cut.Find(".pixata-blazor-input-value-text").TextContent);
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    await cut.Find(".pixata-blazor-dropdown-filter input").InputAsync(new ChangeEventArgs { Value = "j" });
    Assert.AreEqual("j", reads[^1].Filter);
    CollectionAssert.AreEqual(new[] { "Japan" }, DropDownTestData.Options(cut).ToList());
    // The selected item is still shown although the read data no longer has it
    Assert.AreEqual("Kenya", cut.Find(".pixata-blazor-input-value-text").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_VirtualScrolling_RendersFixedHeightRows() {
    using BunitContext context = PixataTestContext.Create();
    List<string> items = Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();
    IRenderedComponent<PixataDropDownList<string, string>> cut = context.Render<PixataDropDownList<string, string>>(p => p
      .Add(c => c.Data, items)
      .Add(c => c.ScrollMode, PixataDropDownScrollMode.Virtual)
      .Add(c => c.ItemHeight, 30));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    // bUnit reports a viewport big enough for every row, so we can only check that the rows have the fixed height that
    // Virtualize relies on
    IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
    Assert.IsTrue(options.Count > 0);
    Assert.IsTrue(options.All(o => o.GetAttribute("style") == "height:30px;"));
  }

  [TestMethod]
  public async Task DropDownList_VirtualScrollingWithOnRead_ReadsAPageAtATime() {
    using BunitContext context = PixataTestContext.Create();
    List<string> items = Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();
    List<(int Skip, int Take)> reads = [];
    string? value = null;
    IRenderedComponent<PixataDropDownList<string, string>> cut = context.Render<PixataDropDownList<string, string>>(p => p
      .Add(c => c.ScrollMode, PixataDropDownScrollMode.Virtual)
      .Add(c => c.PageSize, 25)
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnRead, (PixataDropDownReadEventArgs e) => {
        reads.Add((e.Skip, e.Take));
        e.Data = items.Skip(e.Skip).Take(e.Take).ToList();
        e.Total = items.Count;
      }));
    Assert.AreEqual((0, 25), reads[0]);
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    Assert.IsTrue(cut.FindAll("[role=option]").Count > 0);
    // End goes to the last item, which means reading its page
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("End"));
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual("Item 1000", value);
    Assert.IsTrue(reads.Contains((975, 25)));
    Assert.IsTrue(reads.All(r => r.Take == 25 && r.Skip % 25 == 0));
  }

  [TestMethod]
  public async Task DropDownList_VirtualScrollingWithOnRead_ReadsEachPageOnce() {
    using BunitContext context = PixataTestContext.Create();
    List<string> items = Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();
    List<int> reads = [];
    TaskCompletionSource slowReads = new();
    string? value = null;
    IRenderedComponent<PixataDropDownList<string, string>> cut = context.Render<PixataDropDownList<string, string>>(p => p
      .Add(c => c.ScrollMode, PixataDropDownScrollMode.Virtual)
      .Add(c => c.PageSize, 25)
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnRead, async (PixataDropDownReadEventArgs e) => {
        reads.Add(e.Skip);
        if (e.Skip > 0) {
          // Every page but the first takes a while, so the next request for it arrives while it is still being read
          await slowReads.Task;
        }
        e.Data = items.Skip(e.Skip).Take(e.Take).ToList();
        e.Total = items.Count;
      }));
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("End"));
    // Enter needs the last page, which is still being read when Enter is pressed again
    Task first = cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Enter"));
    Task second = cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("Enter"));
    slowReads.SetResult();
    await Task.WhenAll(first, second);
    Assert.AreEqual("Item 1000", value);
    Assert.AreEqual(1, reads.Count(r => r == 975));
    Assert.AreEqual(reads.Count, reads.Distinct().Count(), $"Pages read more than once: {string.Join(", ", reads)}");
  }

  [TestMethod]
  public void DropDownList_ValueMapper_FindsTheTextOfAnItemNotYetRead() {
    using BunitContext context = PixataTestContext.Create();
    List<string> items = Enumerable.Range(1, 1000).Select(i => $"Item {i}").ToList();
    IRenderedComponent<PixataDropDownList<string, string>> cut = context.Render<PixataDropDownList<string, string>>(p => p
      .Add(c => c.ScrollMode, PixataDropDownScrollMode.Virtual)
      .Add(c => c.Value, "Item 500")
      .Add(c => c.ValueMapper, v => Task.FromResult(v))
      .Add(c => c.OnRead, (PixataDropDownReadEventArgs e) => {
        e.Data = items.Skip(e.Skip).Take(e.Take).ToList();
        e.Total = items.Count;
      }));
    Assert.AreEqual("Item 500", cut.Find(".pixata-blazor-input-value-text").TextContent);
  }

  [TestMethod]
  public async Task DropDownList_OptionIds_AreUniqueAndReferencedByActiveDescendant() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context);
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    List<string> ids = cut.FindAll("[role=option]").Select(o => o.Id!).ToList();
    Assert.AreEqual(ids.Count, ids.Distinct().Count());
    await cut.Find(".pixata-blazor-dropdownlist").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    string? active = cut.Find(".pixata-blazor-dropdownlist").GetAttribute("aria-activedescendant");
    Assert.AreEqual(ids[1], active);
  }

  [TestMethod]
  public async Task DropDowns_ScrollingList_IsNotATabStop() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context);
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    // Some browsers make scrolling elements Tab stops, which would take the focus into the list (and lose it when the list
    // closes) rather than out of the component
    Assert.AreEqual("-1", cut.Find(".pixata-blazor-dropdown-content").GetAttribute("tabindex"));
  }

  [TestMethod]
  public async Task DropDownList_Open_AndClose_CanBeCalledFromCode() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataDropDownList<string, string>> cut = RenderFruit(context);
    await cut.Instance.Open();
    Assert.AreEqual(1, cut.FindAll("[role=listbox]").Count);
    await cut.Instance.Close();
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task DropDownList_Rebind_PicksUpChangesToTheData() {
    using BunitContext context = PixataTestContext.Create();
    List<string> items = ["One", "Two"];
    IRenderedComponent<PixataDropDownList<string, string>> cut = context.Render<PixataDropDownList<string, string>>(p => p.Add(c => c.Data, items));
    items[0] = "Uno";
    await cut.Instance.Rebind();
    await cut.Find(".pixata-blazor-dropdownlist").ClickAsync(new());
    CollectionAssert.AreEqual(new[] { "Uno", "Two" }, DropDownTestData.Options(cut).ToList());
  }
}
