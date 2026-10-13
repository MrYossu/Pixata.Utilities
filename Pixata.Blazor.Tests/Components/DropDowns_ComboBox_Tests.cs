using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class DropDowns_ComboBox_Tests {
  private static IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> RenderCountries(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataComboBox<DropDownTestData.Country, int>>>? extra = null) =>
    context.Render<PixataComboBox<DropDownTestData.Country, int>>(p => {
      p.Add(c => c.Data, DropDownTestData.Countries)
        .Add(c => c.TextField, nameof(DropDownTestData.Country.Name))
        .Add(c => c.ValueField, nameof(DropDownTestData.Country.Id));
      extra?.Invoke(p);
    });

  private static string? Text<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.Find(".pixata-blazor-combobox-input").GetAttribute("value");

  [TestMethod]
  public void ComboBox_Value_ShowsTheTextOfTheItem() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, 3)
      .Add(c => c.Placeholder, "Country"));
    IElement input = cut.Find("input");
    Assert.AreEqual("Germany", Text(cut));
    Assert.AreEqual("Country", input.GetAttribute("placeholder"));
    Assert.AreEqual("combobox", input.GetAttribute("role"));
    Assert.AreEqual("list", input.GetAttribute("aria-autocomplete"));
    Assert.AreEqual("false", input.GetAttribute("aria-expanded"));
  }

  [TestMethod]
  public async Task ComboBox_ArrowButton_TogglesTheList() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context);
    await cut.Find(".pixata-blazor-combobox-button").ClickAsync(new());
    Assert.AreEqual("true", cut.Find("input").GetAttribute("aria-expanded"));
    Assert.AreEqual(5, cut.FindAll("[role=option]").Count);
    await cut.Find(".pixata-blazor-combobox-button").ClickAsync(new());
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task ComboBox_ClickingAnItem_SelectsItAndShowsItsText() {
    using BunitContext context = PixataTestContext.Create();
    int value = 0;
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find(".pixata-blazor-combobox-button").ClickAsync(new());
    await cut.FindAll("[role=option]")[3].ClickAsync(new());
    Assert.AreEqual(4, value);
    Assert.AreEqual("India", Text(cut));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task ComboBox_Typing_OpensTheListAndFocusesTheFirstMatch() {
    using BunitContext context = PixataTestContext.Create();
    int value = 0;
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "ge" });
    // Not filterable, so every item is still there
    Assert.AreEqual(5, cut.FindAll("[role=option]").Count);
    Assert.AreEqual("Germany", DropDownTestData.Focused(cut));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual(3, value);
    Assert.AreEqual("Germany", Text(cut));
  }

  [TestMethod]
  public async Task ComboBox_Filterable_FiltersAsTheUserTypes() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Filterable, true)
      .Add(c => c.FilterOperator, PixataStringFilterOperator.Contains));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "an" });
    CollectionAssert.AreEqual(new[] { "France", "Japan", "Germany" }, DropDownTestData.Options(cut).ToList());
    Assert.AreEqual("France", DropDownTestData.Focused(cut));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("Japan", DropDownTestData.Focused(cut));
  }

  [TestMethod]
  public async Task ComboBox_FullTextTypedAndBlurred_SelectsTheMatchingItem() {
    using BunitContext context = PixataTestContext.Create();
    int value = 0;
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "kenya" });
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    Assert.AreEqual(5, value);
    Assert.AreEqual("Kenya", Text(cut));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
  }

  [TestMethod]
  public async Task ComboBox_UnknownTextWithoutAllowCustom_ClearsTheValue() {
    using BunitContext context = PixataTestContext.Create();
    int value = 2;
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Atlantis" });
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    Assert.AreEqual(0, value);
    Assert.AreEqual("", Text(cut));
  }

  [TestMethod]
  public async Task ComboBox_AllowCustom_KeepsTheTypedText() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    object? changed = null;
    IRenderedComponent<PixataComboBox<string, string>> cut = context.Render<PixataComboBox<string, string>>(p => p
      .Add(c => c.Data, DropDownTestData.Fruit)
      .Add(c => c.AllowCustom, true)
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnChange, (object v) => changed = v));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Kiwi" });
    // Nothing starts with "Kiwi", so Enter commits the text
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual("Kiwi", value);
    Assert.AreEqual("Kiwi", changed);
    Assert.AreEqual("Kiwi", Text(cut));
    // A custom value from the parent is shown as it is
    cut.Render(p => p.Add(c => c.Value, "Mango"));
    Assert.AreEqual("Mango", Text(cut));
  }

  [TestMethod]
  public async Task ComboBox_EscapeWhileEditing_PutsTheTextBack() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p.Add(c => c.Value, 1));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Ja" });
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Escape"));
    Assert.AreEqual(0, cut.FindAll("[role=listbox]").Count);
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Escape"));
    Assert.AreEqual("France", Text(cut));
  }

  [TestMethod]
  public async Task ComboBox_ArrowKeysWhenClosed_ChangeTheValue() {
    using BunitContext context = PixataTestContext.Create();
    int value = 1;
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual(2, value);
    Assert.AreEqual("Japan", Text(cut));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown", alt: true));
    Assert.AreEqual(1, cut.FindAll("[role=listbox]").Count);
    Assert.AreEqual("Japan", DropDownTestData.Focused(cut));
  }

  [TestMethod]
  public async Task ComboBox_ClearButton_ClearsTheTextAndValue() {
    using BunitContext context = PixataTestContext.Create();
    int value = 2;
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (int v) => value = v));
    await cut.Find(".pixata-blazor-clear-value").ClickAsync(new());
    Assert.AreEqual(0, value);
    Assert.AreEqual("", Text(cut));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
  }

  [TestMethod]
  public void ComboBox_Disabled_DisablesTheInput() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = RenderCountries(context, p => p
      .Add(c => c.Value, 2)
      .Add(c => c.Enabled, false));
    Assert.IsTrue(cut.Find("input").HasAttribute("disabled"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
    Assert.IsTrue(cut.Find(".pixata-blazor-combobox").ClassList.Contains("pixata-blazor-disabled"));
  }

  [TestMethod]
  public async Task ComboBox_OnRead_IsCalledWithTheTypedText() {
    using BunitContext context = PixataTestContext.Create();
    List<string> filters = [];
    IRenderedComponent<PixataComboBox<DropDownTestData.Country, int>> cut = context.Render<PixataComboBox<DropDownTestData.Country, int>>(p => p
      .Add(c => c.TextField, nameof(DropDownTestData.Country.Name))
      .Add(c => c.ValueField, nameof(DropDownTestData.Country.Id))
      .Add(c => c.Filterable, true)
      .Add(c => c.DebounceDelay, 0)
      .Add(c => c.OnRead, (PixataDropDownReadEventArgs e) => {
        filters.Add(e.Filter);
        e.Data = DropDownTestData.Countries.Where(country => e.FilterOperator.Matches(country.Name, e.Filter)).ToList();
      }));
    await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "in" });
    CollectionAssert.AreEqual(new[] { "", "in" }, filters);
    CollectionAssert.AreEqual(new[] { "India" }, DropDownTestData.Options(cut).ToList());
  }
}
