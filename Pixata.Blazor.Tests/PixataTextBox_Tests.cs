using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataTextBox_Tests {
  private BunitContext _ctx = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static IElement Input(IRenderedComponent<PixataTextBox> box) =>
    box.Find("input");

  [TestMethod]
  public void ShowsTheValue() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p.Add(c => c.Value, "Hello"));

    Assert.AreEqual("Hello", Input(box).GetAttribute("value"));
    Assert.AreEqual("text", Input(box).GetAttribute("type"));
    Assert.IsTrue(box.Find("span").ClassList.Contains("pixata-blazor-input"));
  }

  [TestMethod]
  public void Typing_UpdatesTheValueOnEveryKey() {
    string? value = null;
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

    Input(box).Input("H");
    Assert.AreEqual("H", value);
    Input(box).Input("Hi");
    Assert.AreEqual("Hi", value);
  }

  [TestMethod]
  public void DebounceDelay_WaitsBeforeUpdatingTheValue() {
    string? value = null;
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.DebounceDelay, 100)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

    _ = Input(box).InputAsync(new ChangeEventArgs { Value = "H" });
    _ = Input(box).InputAsync(new ChangeEventArgs { Value = "Hi" });

    Assert.IsNull(value);
    box.WaitForAssertion(() => Assert.AreEqual("Hi", value));
  }

  [TestMethod]
  public void LeavingTheBox_RaisesADebouncedValueStraightAway() {
    string? value = null;
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.DebounceDelay, 10000)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

    _ = Input(box).InputAsync(new ChangeEventArgs { Value = "Hi" });
    Input(box).Blur();

    Assert.AreEqual("Hi", value);
  }

  [TestMethod]
  public void OnChange_FiresWhenTheBrowserRaisesChange() {
    object? changed = null;
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.OnChange, EventCallback.Factory.Create<object?>(this, v => changed = v)));

    Input(box).Input("Hi");
    Assert.IsNull(changed);
    Input(box).Change("Hi");
    Assert.AreEqual("Hi", changed);
  }

  [TestMethod]
  public void Password_HidesTheText_AndTheButtonShowsIt() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.Password, true)
      .Add(c => c.ShowPasswordButton, true));

    Assert.AreEqual("password", Input(box).GetAttribute("type"));
    IElement button = box.Find(".pixata-blazor-textbox-reveal");
    Assert.AreEqual("Show password", button.GetAttribute("aria-label"));

    button.Click();

    Assert.AreEqual("text", Input(box).GetAttribute("type"));
    Assert.AreEqual("Hide password", box.Find(".pixata-blazor-textbox-reveal").GetAttribute("aria-label"));
    Assert.AreEqual("true", box.Find(".pixata-blazor-textbox-reveal").GetAttribute("aria-pressed"));
  }

  [TestMethod]
  public void Password_WithoutShowPasswordButton_HasNoButton() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p.Add(c => c.Password, true));

    Assert.AreEqual(0, box.FindAll(".pixata-blazor-textbox-reveal").Count);
  }

  [TestMethod]
  public void ClearButton_ShowsOnlyWithText_AndClears() {
    string? value = "Hi";
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ShowClearButton, true)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

    box.Find(".pixata-blazor-clear-value").Click();

    Assert.AreEqual("", value);
    Assert.AreEqual(0, box.FindAll(".pixata-blazor-clear-value").Count);
  }

  [TestMethod]
  public void PrefixAndSuffix_AreRenderedAroundTheInput() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.TextBoxPrefixTemplate, b => b.AddContent(0, "£"))
      .Add(c => c.TextBoxSuffixTemplate, b => b.AddContent(0, "GBP"))
      .Add(c => c.ShowSuffixSeparator, false));

    IElement wrapper = box.Find(".pixata-blazor-textbox");
    Assert.AreEqual("£", wrapper.Children[0].TextContent);
    Assert.AreEqual("INPUT", wrapper.Children[1].TagName);
    Assert.AreEqual("GBP", wrapper.Children[2].TextContent);
    Assert.IsTrue(wrapper.Children[0].ClassList.Contains("pixata-blazor-textbox-separator"));
    Assert.IsFalse(wrapper.Children[2].ClassList.Contains("pixata-blazor-textbox-separator"));
  }

  [TestMethod]
  public void Attributes_GoOnTheInput() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.Id, "email")
      .Add(c => c.AutoComplete, "email")
      .Add(c => c.MaxLength, 50)
      .AddUnmatched("aria-label", "Email")
      .AddUnmatched("name", "email"));

    Assert.AreEqual("email", Input(box).Id);
    Assert.AreEqual("email", Input(box).GetAttribute("autocomplete"));
    Assert.AreEqual("50", Input(box).GetAttribute("maxlength"));
    Assert.AreEqual("Email", Input(box).GetAttribute("aria-label"));
    Assert.AreEqual("email", Input(box).GetAttribute("name"));
  }

  [TestMethod]
  public void DisabledAndReadOnly() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p
      .Add(c => c.Enabled, false)
      .Add(c => c.ReadOnly, true));

    Assert.IsTrue(Input(box).HasAttribute("disabled"));
    Assert.IsTrue(Input(box).HasAttribute("readonly"));
    Assert.IsTrue(box.Find(".pixata-blazor-textbox").ClassList.Contains("pixata-blazor-disabled"));
  }

  [TestMethod]
  public void InAnEditForm_ShowsValidationErrorsAsInvalid() {
    Model model = new();
    EditContext editContext = new(model);
    IRenderedComponent<CascadingValue<EditContext>> form = _ctx.Render<CascadingValue<EditContext>>(p => p
      .Add(c => c.Value, editContext)
      .AddChildContent<PixataTextBox>(b => b
        .Add(c => c.Value, model.Name)
        .Add(c => c.ValueExpression, () => model.Name)));

    ValidationMessageStore messages = new(editContext);
    messages.Add(editContext.Field(nameof(Model.Name)), "Required");
    editContext.NotifyValidationStateChanged();

    Assert.IsTrue(form.Find(".pixata-blazor-textbox").ClassList.Contains("pixata-blazor-invalid"));
    Assert.AreEqual("true", form.Find("input").GetAttribute("aria-invalid"));
  }

  [TestMethod]
  public void AValueSetFromOutside_ReplacesTheText() {
    IRenderedComponent<PixataTextBox> box = _ctx.Render<PixataTextBox>(p => p.Add(c => c.Value, "One"));

    box.Render(p => p.Add(c => c.Value, "Two"));

    Assert.AreEqual("Two", Input(box).GetAttribute("value"));
  }

  private class Model {
    public string? Name { get; set; }
  }
}
