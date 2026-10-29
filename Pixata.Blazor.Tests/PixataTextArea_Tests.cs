using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataTextArea_Tests {
  private BunitContext _ctx = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private static IElement TextArea(IRenderedComponent<PixataTextArea> area) =>
    area.Find("textarea");

  [TestMethod]
  public void ShowsTheValue_WithThreeRowsByDefault() {
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>(p => p.Add(c => c.Value, "Line one\nLine two"));

    Assert.AreEqual("Line one\nLine two", TextArea(area).GetAttribute("value"));
    Assert.AreEqual("3", TextArea(area).GetAttribute("rows"));
    Assert.IsTrue(area.Find(".pixata-blazor-textarea").ClassList.Contains("pixata-blazor-input"));
  }

  [TestMethod]
  public void Typing_UpdatesTheValue() {
    string? value = null;
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>(p => p
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

    TextArea(area).Input("Hello\nthere");

    Assert.AreEqual("Hello\nthere", value);
  }

  [TestMethod]
  public void OnChange_FiresWhenTheBrowserRaisesChange() {
    object? changed = null;
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>(p => p
      .Add(c => c.OnChange, EventCallback.Factory.Create<object?>(this, v => changed = v)));

    TextArea(area).Change("Hi");

    Assert.AreEqual("Hi", changed);
  }

  [TestMethod]
  public void ResizeMode_IsVerticalByDefault_AndCanBeChanged() {
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>();
    Assert.IsTrue(area.Find(".pixata-blazor-textarea").ClassList.Contains("pixata-blazor-textarea-resize-vertical"));

    area.Render(p => p.Add(c => c.ResizeMode, PixataTextAreaResizeMode.None));
    Assert.IsTrue(area.Find(".pixata-blazor-textarea").ClassList.Contains("pixata-blazor-textarea-resize-none"));

    area.Render(p => p.Add(c => c.AutoSize, true));
    Assert.IsTrue(area.Find(".pixata-blazor-textarea").ClassList.Contains("pixata-blazor-textarea-resize-auto"));
  }

  [TestMethod]
  public void AutoSize_SizesTheTextAreaWhenTheValueIsSetFromOutside() {
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>(p => p
      .Add(c => c.ResizeMode, PixataTextAreaResizeMode.Auto)
      .Add(c => c.Value, "One"));
    int calls = _ctx.JSInterop.Invocations["autoSizeTextArea"].Count;
    Assert.AreEqual(1, calls);

    TextArea(area).Input("One two");
    Assert.AreEqual(1, _ctx.JSInterop.Invocations["autoSizeTextArea"].Count);

    area.Render(p => p.Add(c => c.Value, "Something else"));
    Assert.AreEqual(2, _ctx.JSInterop.Invocations["autoSizeTextArea"].Count);
  }

  [TestMethod]
  public void CharacterCount_ShowsTheLengthOutOfMaxLength() {
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>(p => p
      .Add(c => c.Value, "Hello")
      .Add(c => c.MaxLength, 100)
      .Add(c => c.ShowCharacterCount, true));

    IElement count = area.Find(".pixata-blazor-textarea-count");
    Assert.AreEqual("5 / 100", count.TextContent);
    Assert.AreEqual(count.Id, TextArea(area).GetAttribute("aria-describedby"));
    Assert.AreEqual("100", TextArea(area).GetAttribute("maxlength"));

    TextArea(area).Input("Hello there");
    Assert.AreEqual("11 / 100", area.Find(".pixata-blazor-textarea-count").TextContent);
  }

  [TestMethod]
  public void CharacterCount_KeepsAnAriaDescribedByPassedIn() {
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>(p => p
      .Add(c => c.ShowCharacterCount, true)
      .AddUnmatched("aria-describedby", "hint"));

    string countId = area.Find(".pixata-blazor-textarea-count").Id!;
    Assert.AreEqual($"hint {countId}", TextArea(area).GetAttribute("aria-describedby"));
  }

  [TestMethod]
  public void WithoutCharacterCount_HasNoCountOrDescribedBy() {
    IRenderedComponent<PixataTextArea> area = _ctx.Render<PixataTextArea>();

    Assert.AreEqual(0, area.FindAll(".pixata-blazor-textarea-count").Count);
    Assert.IsFalse(TextArea(area).HasAttribute("aria-describedby"));
  }

  [TestMethod]
  public void InAnEditForm_ShowsValidationErrorsAsInvalid() {
    Model model = new();
    EditContext editContext = new(model);
    IRenderedComponent<CascadingValue<EditContext>> form = _ctx.Render<CascadingValue<EditContext>>(p => p
      .Add(c => c.Value, editContext)
      .AddChildContent<PixataTextArea>(b => b
        .Add(c => c.Value, model.Notes)
        .Add(c => c.ValueExpression, () => model.Notes)));

    ValidationMessageStore messages = new(editContext);
    messages.Add(editContext.Field(nameof(Model.Notes)), "Required");
    editContext.NotifyValidationStateChanged();

    Assert.IsTrue(form.Find(".pixata-blazor-textarea").ClassList.Contains("pixata-blazor-invalid"));
  }

  private class Model {
    public string? Notes { get; set; }
  }
}
