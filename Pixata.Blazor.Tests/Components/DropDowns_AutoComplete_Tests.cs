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
public class DropDowns_AutoComplete_Tests {
  // Updates the value on every key unless a test says otherwise, so the tests don't wait for the debounce
  private static IRenderedComponent<PixataAutoComplete<string>> RenderFruit(BunitContext context, Action<ComponentParameterCollectionBuilder<PixataAutoComplete<string>>>? extra = null, int debounceDelay = 0) =>
    context.Render<PixataAutoComplete<string>>(p => {
      p.Add(c => c.Data, DropDownTestData.Fruit)
        .Add(c => c.DebounceDelay, debounceDelay);
      extra?.Invoke(p);
    });

  private static string? Text<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.Find(".pixata-blazor-autocomplete-input").GetAttribute("value");

  private static Task Type<T>(IRenderedComponent<T> cut, string text) where T : IComponent =>
    cut.Find("input").InputAsync(new ChangeEventArgs { Value = text });

  private static bool IsOpen<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.Find("input").GetAttribute("aria-expanded") == "true";

  [TestMethod]
  public void AutoComplete_Value_IsShownInTheTextBox() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, "Kiwi")
      .Add(c => c.Placeholder, "Fruit"));
    IElement input = cut.Find("input");
    Assert.AreEqual("Kiwi", Text(cut));
    Assert.AreEqual("Fruit", input.GetAttribute("placeholder"));
    Assert.AreEqual("combobox", input.GetAttribute("role"));
    Assert.AreEqual("list", input.GetAttribute("aria-autocomplete"));
    Assert.IsFalse(IsOpen(cut));
    Assert.IsTrue(cut.Find(".pixata-blazor-dropdown").ClassList.Contains("pixata-blazor-autocomplete"));
    // There is no button to open the list
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-input-button").Count);
  }

  [TestMethod]
  public async Task AutoComplete_Typing_UpdatesTheValueAndShowsMatchingSuggestionsWithNoneFocused() {
    using BunitContext context = PixataTestContext.Create();
    List<string> values = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => values.Add(v)));
    await Type(cut, "a");
    await Type(cut, "ap");
    CollectionAssert.AreEqual(new[] { "a", "ap" }, values);
    Assert.IsTrue(IsOpen(cut));
    CollectionAssert.AreEqual(new[] { "Apple", "Apricot" }, DropDownTestData.Options(cut).ToList());
    Assert.IsNull(DropDownTestData.Focused(cut));
  }

  [TestMethod]
  public async Task AutoComplete_TextThatMatchesNothing_IsStillTheValue() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v));
    await Type(cut, "Kiwi");
    Assert.AreEqual("Kiwi", value);
    Assert.AreEqual(0, cut.FindAll("[role=option]").Count);
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    Assert.AreEqual("Kiwi", value);
    Assert.IsFalse(IsOpen(cut));
  }

  [TestMethod]
  public async Task AutoComplete_FilterOperatorContains_MatchesAnywhereInTheText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.FilterOperator, PixataStringFilterOperator.Contains));
    await Type(cut, "an");
    CollectionAssert.AreEqual(new[] { "Banana" }, DropDownTestData.Options(cut).ToList());
  }

  [TestMethod]
  public async Task AutoComplete_NotFilterable_ShowsEverySuggestion() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.Filterable, false));
    await Type(cut, "ch");
    Assert.AreEqual(5, cut.FindAll("[role=option]").Count);
  }

  [TestMethod]
  public async Task AutoComplete_MinLength_WaitsForEnoughCharacters() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.MinLength, 2));
    await Type(cut, "a");
    Assert.IsFalse(IsOpen(cut));
    await Type(cut, "ap");
    Assert.IsTrue(IsOpen(cut));
    await Type(cut, "a");
    Assert.IsFalse(IsOpen(cut));
  }

  [TestMethod]
  public async Task AutoComplete_DeletingTheText_ClosesTheList() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v));
    await Type(cut, "b");
    Assert.IsTrue(IsOpen(cut));
    await Type(cut, "");
    Assert.IsFalse(IsOpen(cut));
    Assert.AreEqual("", value);
  }

  [TestMethod]
  public async Task AutoComplete_ArrowsAndEnter_TakeASuggestion() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    List<object?> changes = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnChange, (object v) => changes.Add(v)));
    await Type(cut, "ap");
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("Apple", DropDownTestData.Focused(cut));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.AreEqual("Apricot", DropDownTestData.Focused(cut));
    // Moving through the suggestions doesn't change the value
    Assert.AreEqual("ap", value);
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual("Apricot", value);
    Assert.AreEqual("Apricot", Text(cut));
    Assert.IsFalse(IsOpen(cut));
    CollectionAssert.AreEqual(new object?[] { "Apricot" }, changes);
  }

  [TestMethod]
  public async Task AutoComplete_EnterWithNoSuggestionFocused_KeepsWhatWasTyped() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    List<object?> changes = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnChange, (object v) => changes.Add(v)));
    await Type(cut, "ap");
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual("ap", value);
    Assert.IsFalse(IsOpen(cut));
    CollectionAssert.AreEqual(new object?[] { "ap" }, changes);
    // Enter again, with nothing changed, doesn't raise OnChange again
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual(1, changes.Count);
  }

  [TestMethod]
  public async Task AutoComplete_Escape_ClosesTheListAndKeepsWhatWasTyped() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v));
    await Type(cut, "ch");
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Escape"));
    Assert.IsFalse(IsOpen(cut));
    Assert.AreEqual("ch", value);
  }

  [TestMethod]
  public async Task AutoComplete_ArrowDownWithTheListClosed_ShowsTheSuggestionsAgain() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context);
    await Type(cut, "ch");
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Escape"));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.IsTrue(IsOpen(cut));
    CollectionAssert.AreEqual(new[] { "Cherry" }, DropDownTestData.Options(cut).ToList());
  }

  [TestMethod]
  public async Task AutoComplete_HomeAndEnd_DoNotMoveAroundTheList() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context);
    await Type(cut, "a");
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("End"));
    Assert.IsNull(DropDownTestData.Focused(cut));
    Assert.IsTrue(IsOpen(cut));
  }

  [TestMethod]
  public async Task AutoComplete_ClickingASuggestion_TakesIt() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => value = v));
    await Type(cut, "d");
    await cut.FindAll("[role=option]")[0].ClickAsync(new());
    Assert.AreEqual("Damson", value);
    Assert.AreEqual("Damson", Text(cut));
    Assert.IsFalse(IsOpen(cut));
  }

  [TestMethod]
  public async Task AutoComplete_SuggestionMatchingTheText_IsShownAsSelected() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.FilterOperator, PixataStringFilterOperator.Contains));
    await Type(cut, "Apple");
    Assert.AreEqual("true", cut.Find("[role=option]").GetAttribute("aria-selected"));
  }

  [TestMethod]
  public async Task AutoComplete_DebounceDelay_UpdatesTheValueOnceTheUserStopsTyping() {
    using BunitContext context = PixataTestContext.Create();
    List<string> values = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => values.Add(v)), 100);
    Task first = Type(cut, "b");
    Task second = Type(cut, "ba");
    Assert.AreEqual(0, values.Count);
    await Task.WhenAll(first, second);
    CollectionAssert.AreEqual(new[] { "ba" }, values);
  }

  [TestMethod]
  public async Task AutoComplete_LeavingWhileTheValueIsWaiting_UpdatesItStraightAway() {
    using BunitContext context = PixataTestContext.Create();
    List<string> values = [];
    List<object?> changes = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.ValueChanged, (string v) => values.Add(v))
      .Add(c => c.OnChange, (object v) => changes.Add(v)), 5000);
    Task typing = Type(cut, "Fig");
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    CollectionAssert.AreEqual(new[] { "Fig" }, values);
    CollectionAssert.AreEqual(new object?[] { "Fig" }, changes);
    // The waiting update was cancelled, so the value isn't set again
    await typing;
    Assert.AreEqual(1, values.Count);
  }

  [TestMethod]
  public async Task AutoComplete_LeavingWithoutAChange_DoesNotRaiseOnChange() {
    using BunitContext context = PixataTestContext.Create();
    List<object?> changes = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, "Apple")
      .Add(c => c.OnChange, (object v) => changes.Add(v)));
    await cut.InvokeAsync(cut.Instance.HandleFocusLeft);
    Assert.AreEqual(0, changes.Count);
  }

  [TestMethod]
  public async Task AutoComplete_ClearButton_ClearsTheText() {
    using BunitContext context = PixataTestContext.Create();
    string? value = "Cherry";
    List<object?> changes = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (string v) => value = v)
      .Add(c => c.OnChange, (object v) => changes.Add(v)));
    await cut.Find(".pixata-blazor-clear-value").ClickAsync(new());
    Assert.AreEqual("", value);
    Assert.AreEqual("", Text(cut));
    CollectionAssert.AreEqual(new object?[] { "" }, changes);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
  }

  [TestMethod]
  public void AutoComplete_ShowClearButtonFalse_HasNoClearButton() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, "Cherry")
      .Add(c => c.ShowClearButton, false));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
  }

  [TestMethod]
  public void AutoComplete_ValueSetByTheCode_IsShown() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p.Add(c => c.Value, "Apple"));
    cut.Render(p => p.Add(c => c.Value, "Banana"));
    Assert.AreEqual("Banana", Text(cut));
    cut.Render(p => p.Add(c => c.Value, null));
    Assert.AreEqual("", Text(cut));
  }

  [TestMethod]
  public async Task AutoComplete_Items_UseValueFieldForTheirText() {
    using BunitContext context = PixataTestContext.Create();
    string? value = null;
    IRenderedComponent<PixataAutoComplete<DropDownTestData.Country>> cut = context.Render<PixataAutoComplete<DropDownTestData.Country>>(p => p
      .Add(c => c.Data, DropDownTestData.Countries)
      .Add(c => c.ValueField, nameof(DropDownTestData.Country.Name))
      .Add(c => c.DebounceDelay, 0)
      .Add(c => c.ValueChanged, (string v) => value = v));
    await Type(cut, "j");
    CollectionAssert.AreEqual(new[] { "Japan" }, DropDownTestData.Options(cut).ToList());
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("Enter"));
    Assert.AreEqual("Japan", value);
  }

  [TestMethod]
  public async Task AutoComplete_OnRead_IsCalledWithTheTypedText() {
    using BunitContext context = PixataTestContext.Create();
    List<string> filters = [];
    IRenderedComponent<PixataAutoComplete<string>> cut = context.Render<PixataAutoComplete<string>>(p => p
      .Add(c => c.DebounceDelay, 0)
      .Add(c => c.FilterOperator, PixataStringFilterOperator.Contains)
      .Add(c => c.OnRead, (PixataDropDownReadEventArgs e) => {
        filters.Add(e.Filter);
        Assert.AreEqual(PixataStringFilterOperator.Contains, e.FilterOperator);
        e.Data = DropDownTestData.Fruit.Where(f => e.FilterOperator.Matches(f, e.Filter)).ToList();
      }));
    await Type(cut, "rr");
    Assert.AreEqual("rr", filters.Last());
    CollectionAssert.AreEqual(new[] { "Cherry" }, DropDownTestData.Options(cut).ToList());
  }

  [TestMethod]
  public async Task AutoComplete_InAnEditForm_NotifiesTheFormAndShowsTheInvalidState() {
    using BunitContext context = PixataTestContext.Create();
    DropDownTestData.Model model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.Fruit)
      .Add(c => c.ValueChanged, (string v) => model.Fruit = v)
      .Add(c => c.ValueExpression, () => model.Fruit!));
    await Type(cut, "Plum");
    Assert.AreEqual("Plum", model.Fruit);
    CollectionAssert.AreEqual(new[] { nameof(DropDownTestData.Model.Fruit) }, changed);
    await cut.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(DropDownTestData.Model.Fruit)), "Required");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(cut.Find(".pixata-blazor-autocomplete").ClassList.Contains("pixata-blazor-invalid"));
    Assert.AreEqual("true", cut.Find("input").GetAttribute("aria-invalid"));
  }

  [TestMethod]
  public async Task AutoComplete_Disabled_CannotBeChanged() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataAutoComplete<string>> cut = RenderFruit(context, p => p
      .Add(c => c.Value, "Apple")
      .Add(c => c.Enabled, false));
    Assert.IsTrue(cut.Find("input").HasAttribute("disabled"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-clear-value").Count);
    await cut.Find("input").KeyDownAsync(DropDownTestData.Key("ArrowDown"));
    Assert.IsFalse(IsOpen(cut));
  }
}
