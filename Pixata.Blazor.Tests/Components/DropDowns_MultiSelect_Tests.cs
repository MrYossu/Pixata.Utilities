using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class DropDowns_MultiSelect_Tests {
  private static IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> RenderCountries(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataMultiSelect<DropDownTestData.Country, int>>>? extra = null) =>
    context.Render<PixataMultiSelect<DropDownTestData.Country, int>>(p => {
      p.Add(c => c.Data, DropDownTestData.Countries)
        .Add(c => c.TextField, nameof(DropDownTestData.Country.Name))
        .Add(c => c.ValueField, nameof(DropDownTestData.Country.Id));
      extra?.Invoke(p);
    });

  private static List<string> Tags<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.FindAll(".pixata-blazor-multiselect-tag-text").Select(t => t.TextContent.Trim()).ToList();

  [TestMethod]
  public void MultiSelect_Value_IsShownAsTags() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, [4, 1])
      .Add(c => c.Placeholder, "Countries"));
    CollectionAssert.AreEqual(new[] { "India", "France" }, Tags(cut));
    IElement input = cut.Find("input");
    // The placeholder is only shown when there are no tags
    Assert.IsNull(input.GetAttribute("placeholder"));
    Assert.AreEqual("combobox", input.GetAttribute("role"));
    StringAssert.Contains(input.GetAttribute("aria-describedby"), cut.Find(".pixata-blazor-multiselect-tags").Id);
    Assert.AreEqual("Remove India", cut.Find(".pixata-blazor-multiselect-tag-remove").GetAttribute("aria-label"));
  }

  [TestMethod]
  public void MultiSelect_NoValue_ShowsThePlaceholder() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p.Add(c => c.Placeholder, "Countries"));
    Assert.AreEqual("Countries", cut.Find("input").GetAttribute("placeholder"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-multiselect-tag").Count);
  }

  [TestMethod]
  public async Task MultiSelect_ClickingItems_AddsAndRemovesThem() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [];
    List<object> changes = [];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.AutoClose, false)
      .Add(c => c.ValueChanged, (List<int> v) => value = v)
      .Add(c => c.OnChange, (object v) => changes.Add(v)));
    await cut.Find(".pixata-blazor-multiselect").ClickAsync(new());
    Assert.AreEqual("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
    await cut.FindAll("[role=option]")[1].ClickAsync(new());
    await cut.FindAll("[role=option]")[4].ClickAsync(new());
    CollectionAssert.AreEqual(new[] { 2, 5 }, value);
    CollectionAssert.AreEqual(new[] { "Japan", "Kenya" }, Tags(cut));
    Assert.AreEqual(2, cut.FindAll("[role=option][aria-selected=true]").Count);
    // Clicking a selected item deselects it
    await cut.FindAll("[role=option]")[1].ClickAsync(new());
    CollectionAssert.AreEqual(new[] { 5 }, value);
    Assert.AreEqual(3, changes.Count);
    // AutoClose is off, so the list is still open
    Assert.AreEqual(1, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task MultiSelect_AutoClose_ClosesAfterChoosing() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context);
    await cut.Find(".pixata-blazor-multiselect").ClickAsync(new());
    await cut.FindAll("[role=option]")[0].ClickAsync(new());
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    CollectionAssert.AreEqual(new[] { "France" }, Tags(cut));
  }

  [TestMethod]
  public async Task MultiSelect_RemovingATag_RemovesTheValue() {
    using BunitContext context = PixataTestContext.Create();
    List<int> original = [1, 2, 3];
    List<int> value = original;
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.FindAll(".pixata-blazor-multiselect-tag-remove")[1].ClickAsync(new());
    CollectionAssert.AreEqual(new[] { 1, 3 }, value);
    CollectionAssert.AreEqual(new[] { "France", "Germany" }, Tags(cut));
    // The component sets a new list rather than changing the one it was given
    CollectionAssert.AreEqual(new[] { 1, 2, 3 }, original);
  }

  [TestMethod]
  public async Task MultiSelect_BackspaceInAnEmptyTextBox_RemovesTheLastValue() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [1, 2];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Backspace"));
    CollectionAssert.AreEqual(new[] { 1 }, value);
  }

  [TestMethod]
  public async Task MultiSelect_KeyboardWhenOpen_TogglesTheFocusedItem() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.AutoClose, false)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("France", DropDownTestData.Focused(cut));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("End"));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    CollectionAssert.AreEqual(new[] { 2, 5 }, value);
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Escape"));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task MultiSelect_Filterable_FiltersAndClearsTheTextAfterChoosing() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.AutoClose, false)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "j" });
    CollectionAssert.AreEqual(new[] { "Japan" }, DropDownTestData.Options(cut).ToList());
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    CollectionAssert.AreEqual(new[] { 2 }, value);
    Assert.AreEqual("", cut.Find("input").GetAttribute("value"));
    // The filter is cleared and the chosen item keeps the focus
    Assert.AreEqual(5, cut.FindAll("[role=option]").Count);
    Assert.AreEqual("Japan", DropDownTestData.Focused(cut));
  }

  [TestMethod]
  public async Task MultiSelect_PersistFilterOnSelect_KeepsTheText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.AutoClose, false)
      .Add(c => c.PersistFilterOnSelect, true));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "g" });
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    CollectionAssert.AreEqual(new[] { "Germany" }, Tags(cut));
    Assert.AreEqual("g", cut.Find("input").GetAttribute("value"));
    CollectionAssert.AreEqual(new[] { "Germany" }, DropDownTestData.Options(cut).ToList());
  }

  [TestMethod]
  public async Task MultiSelect_MinLength_KeepsTheListClosedUntilEnoughIsTyped() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.MinLength, 2));
    await cut.Find(".pixata-blazor-multiselect").ClickAsync(new());
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "g" });
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "ge" });
    CollectionAssert.AreEqual(new[] { "Germany" }, DropDownTestData.Options(cut).ToList());
  }

  [TestMethod]
  public async Task MultiSelect_CheckBoxesAndSelectAll_SelectEverythingThenNothing() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [3];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.EnableCheckBoxes, true)
      .Add(c => c.EnableSelectAll, true)
      .Add(c => c.AutoClose, false)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.Find(".pixata-blazor-multiselect").ClickAsync(new());
    Assert.AreEqual(5, cut.FindAll(".pixata-blazor-dropdown-item .pixata-blazor-dropdown-checkbox").Count);
    IElement selectAll = cut.Find(".pixata-blazor-dropdown-select-all");
    Assert.AreEqual("Select all", selectAll.TextContent.Trim());
    Assert.IsTrue(selectAll.QuerySelector(".pixata-blazor-dropdown-checkbox")!.ClassList.Contains("pixata-blazor-indeterminate"));
    await selectAll.ClickAsync(new());
    CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, value);
    Assert.IsTrue(cut.Find(".pixata-blazor-dropdown-select-all .pixata-blazor-dropdown-checkbox").ClassList.Contains("pixata-blazor-checked"));
    await cut.Find(".pixata-blazor-dropdown-select-all").ClickAsync(new());
    Assert.AreEqual(0, value.Count);
  }

  [TestMethod]
  public async Task MultiSelect_SelectAllWithAFilter_OnlySelectsTheVisibleItems() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.FilterOperator, PixataStringFilterOperator.Contains)
      .Add(c => c.EnableSelectAll, true)
      .Add(c => c.AutoClose, false)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "an" });
    await cut.Find(".pixata-blazor-dropdown-select-all").ClickAsync(new());
    CollectionAssert.AreEqual(new[] { 1, 2, 3 }, value);
  }

  [TestMethod]
  public void MultiSelect_TagModeSingle_ShowsOneSummaryTag() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, [1, 2, 3])
      .Add(c => c.TagMode, PixataMultiSelectTagMode.Single));
    CollectionAssert.AreEqual(new[] { "3 items selected" }, Tags(cut));
    Assert.AreEqual(1, cut.FindAll(".pixata-blazor-multiselect-summary-tag").Count);
  }

  [TestMethod]
  public async Task MultiSelect_MaxAllowedTags_SummarisesTheRest() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [1, 2, 3, 4];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.MaxAllowedTags, 2)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    CollectionAssert.AreEqual(new[] { "France", "Japan", "2 more selected" }, Tags(cut));
    // Removing the summary tag removes the values it stands for
    await cut.Find(".pixata-blazor-multiselect-summary-tag .pixata-blazor-multiselect-tag-remove").ClickAsync(new());
    CollectionAssert.AreEqual(new[] { 1, 2 }, value);
  }

  [TestMethod]
  public void MultiSelect_TagTemplates_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, [1, 2, 3])
      .Add(c => c.MaxAllowedTags, 1)
      .Add(c => c.TagTemplate, country => $"<b>{country.Name}</b>")
      .Add(c => c.SummaryTagTemplate, summary => $"<i>+{summary.Count} of {summary.Items.Count}</i>"));
    Assert.AreEqual("France", cut.Find(".pixata-blazor-multiselect-tag-text b").TextContent);
    Assert.AreEqual("+2 of 3", cut.Find(".pixata-blazor-multiselect-summary-tag i").TextContent);
  }

  [TestMethod]
  public async Task MultiSelect_AllowCustom_AddsTheTypedText() {
    using BunitContext context = PixataTestContext.Create();
    List<string> value = [];
    IRenderedComponent<PixataMultiSelect<string, string>> cut = context.Render<PixataMultiSelect<string, string>>(p => p
      .Add(c => c.Data, DropDownTestData.Fruit)
      .Add(c => c.AllowCustom, true)
      .Add(c => c.AutoClose, false)
      .Add(c => c.ValueChanged, (List<string> v) => value = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Kiwi" });
    IElement custom = cut.Find(".pixata-blazor-dropdown-custom-value");
    Assert.AreEqual("Kiwi", custom.TextContent.Trim());
    Assert.AreEqual("Kiwi", DropDownTestData.Focused(cut));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    CollectionAssert.AreEqual(new[] { "Kiwi" }, value);
    CollectionAssert.AreEqual(new[] { "Kiwi" }, Tags(cut));
    // Typing the text of an item chooses the item rather than adding a custom value
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "banana" });
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-dropdown-custom-value").Count);
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    CollectionAssert.AreEqual(new[] { "Kiwi", "Banana" }, value);
  }

  [TestMethod]
  public async Task MultiSelect_ClearButton_RemovesEverything() {
    using BunitContext context = PixataTestContext.Create();
    List<int> value = [1, 2];
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (List<int> v) => value = v));
    await cut.Find(".pixata-blazor-clear-value").ClickAsync(new());
    Assert.AreEqual(0, value.Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-multiselect-tag").Count);
  }

  [TestMethod]
  public void MultiSelect_ReadOnly_HasNoRemoveButtons() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, [1, 2])
      .Add(c => c.ReadOnly, true));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-multiselect-tag-remove").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
    Assert.IsTrue(cut.Find("input").HasAttribute("readonly"));
  }

  [TestMethod]
  public async Task MultiSelect_FocusLeaving_ClearsTheTextAndCloses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p.Add(c => c.Filterable, true));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "fr" });
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    Assert.AreEqual("", cut.Find("input").GetAttribute("value"));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task MultiSelect_InAnEditForm_NotifiesTheForm() {
    using BunitContext context = PixataTestContext.Create();
    DropDownTestData.Model model = new();
    EditContext editContext = new(model);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.CountryIds)
      .Add(c => c.ValueChanged, (List<int> v) => model.CountryIds = v)
      .Add(c => c.ValueExpression, () => model.CountryIds));
    await cut.Find(".pixata-blazor-multiselect").ClickAsync(new());
    await cut.FindAll("[role=option]")[2].ClickAsync(new());
    CollectionAssert.AreEqual(new[] { 3 }, model.CountryIds);
    CollectionAssert.AreEqual(new[] { nameof(DropDownTestData.Model.CountryIds) }, changed);
  }

  [TestMethod]
  public void MultiSelect_ValueMapper_FindsTheItemsOfValuesNotYetRead() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataMultiSelect<DropDownTestData.Country, int>> cut = context.Render<PixataMultiSelect<DropDownTestData.Country, int>>(p => p
      .Add(c => c.TextField, nameof(DropDownTestData.Country.Name))
      .Add(c => c.ValueField, nameof(DropDownTestData.Country.Id))
      .Add(c => c.ScrollMode, PixataDropDownScrollMode.Virtual)
      .Add(c => c.PageSize, 2)
      .Add(c => c.Value, [1, 5])
      .Add(c => c.ValueMapper, values => Task.FromResult(DropDownTestData.Countries.Where(country => values.Contains(country.Id)).ToList()))
      .Add(c => c.OnRead, (PixataDropDownReadEventArgs e) => {
        e.Data = DropDownTestData.Countries.Skip(e.Skip).Take(e.Take).ToList();
        e.Total = DropDownTestData.Countries.Count;
      }));
    CollectionAssert.AreEqual(new[] { "France", "Kenya" }, Tags(cut));
  }
}
