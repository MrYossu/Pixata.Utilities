using System;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataSlider_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    // A comma for the decimal point, to check the range input's attributes don't use it
    CultureInfo.CurrentCulture = new("fr-FR", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  private static IElement Input<TValue>(IRenderedComponent<PixataSlider<TValue>> slider) =>
    slider.Find("input[type=range]");

  [TestMethod]
  public void Defaults_AreZeroToTenInSteps() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p.Add(c => c.Value, 3));

    Assert.AreEqual("0", Input(slider).GetAttribute("min"));
    Assert.AreEqual("10", Input(slider).GetAttribute("max"));
    Assert.AreEqual("1", Input(slider).GetAttribute("step"));
    Assert.AreEqual("5", Input(slider).GetAttribute("data-large-step"));
    Assert.AreEqual("3", Input(slider).GetAttribute("value"));
  }

  [TestMethod]
  public void Attributes_UseADotForTheDecimalPoint() {
    IRenderedComponent<PixataSlider<double>> slider = _ctx.Render<PixataSlider<double>>(p => p
      .Add(c => c.Value, 1.5)
      .Add(c => c.Max, 2.5)
      .Add(c => c.SmallStep, 0.25));

    Assert.AreEqual("1.5", Input(slider).GetAttribute("value"));
    Assert.AreEqual("2.5", Input(slider).GetAttribute("max"));
    Assert.AreEqual("0.25", Input(slider).GetAttribute("step"));
    Assert.AreEqual("--pixata-blazor-slider-fraction:0.6;", Input(slider).GetAttribute("style"));
  }

  [TestMethod]
  public void Dragging_UpdatesTheValue_AndLettingGoRaisesOnChange() {
    int value = 0;
    object? changed = null;
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v))
      .Add(c => c.OnChange, EventCallback.Factory.Create<object?>(this, v => changed = v)));

    Input(slider).Input("4");
    Assert.AreEqual(4, value);
    Assert.IsNull(changed);

    Input(slider).Change("6");
    Assert.AreEqual(6, value);
    Assert.AreEqual(6, changed);
  }

  [TestMethod]
  public void Buttons_MoveBySmallStep_AndStopAtTheEnds() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.Value, 9)
      .Add(c => c.SmallStep, 2));

    slider.Find(".pixata-blazor-slider-increase").Click();
    Assert.AreEqual(10, slider.Instance.Value);
    Assert.IsTrue(slider.Find(".pixata-blazor-slider-increase").HasAttribute("disabled"));

    slider.Find(".pixata-blazor-slider-decrease").Click();
    Assert.AreEqual(8, slider.Instance.Value);
  }

  [TestMethod]
  public void ShowButtonsFalse_HasNoButtons() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p.Add(c => c.ShowButtons, false));

    Assert.AreEqual(0, slider.FindAll(".pixata-blazor-slider-button").Count);
  }

  [TestMethod]
  public void Decimals_RoundsTheValue() {
    IRenderedComponent<PixataSlider<decimal>> slider = _ctx.Render<PixataSlider<decimal>>(p => p
      .Add(c => c.Max, 1m)
      .Add(c => c.SmallStep, 0.001m)
      .Add(c => c.Decimals, 2));

    Input(slider).Input("0.456");

    Assert.AreEqual(0.46m, slider.Instance.Value);
  }

  [TestMethod]
  public void Ticks_AreDrawnEverySmallStep_WithLabelsOnTheLargeOnes() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.TickPosition, PixataSliderTickPosition.After));

    IElement ticks = slider.Find(".pixata-blazor-slider-ticks-after");
    Assert.AreEqual(11, ticks.QuerySelectorAll(".pixata-blazor-slider-tick").Length);
    CollectionAssert.AreEqual(new[] { "0", "5", "10" }, ticks.QuerySelectorAll(".pixata-blazor-slider-label").Select(l => l.TextContent.Trim()).ToArray());
    Assert.AreEqual(0, slider.FindAll(".pixata-blazor-slider-ticks-before").Count);
  }

  [TestMethod]
  public void Ticks_BothSides_LabelOnlyAfter() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>();

    Assert.AreEqual(0, slider.Find(".pixata-blazor-slider-ticks-before").QuerySelectorAll(".pixata-blazor-slider-label").Length);
    Assert.AreEqual(3, slider.Find(".pixata-blazor-slider-ticks-after").QuerySelectorAll(".pixata-blazor-slider-label").Length);
  }

  [TestMethod]
  public void Ticks_TooManySmallOnes_DrawsOnlyTheLargeOnes() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.Max, 1000)
      .Add(c => c.LargeStep, 100)
      .Add(c => c.TickPosition, PixataSliderTickPosition.After));

    Assert.AreEqual(11, slider.FindAll(".pixata-blazor-slider-tick").Count);
  }

  [TestMethod]
  public void TickPositionNone_HasNoTicks() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p.Add(c => c.TickPosition, PixataSliderTickPosition.None));

    Assert.AreEqual(0, slider.FindAll(".pixata-blazor-slider-ticks").Count);
  }

  [TestMethod]
  public void LabelTemplate_ShowsEachLargeTicksLabel() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.TickPosition, PixataSliderTickPosition.After)
      .Add(c => c.LabelTemplate, v => b => b.AddContent(0, $"{v}%")));

    CollectionAssert.AreEqual(new[] { "0%", "5%", "10%" }, slider.FindAll(".pixata-blazor-slider-label").Select(l => l.TextContent.Trim()).ToArray());
  }

  [TestMethod]
  public void Vertical_SetsTheOrientation() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.Orientation, PixataSliderOrientation.Vertical)
      .Add(c => c.Height, "300px"));

    Assert.IsTrue(slider.Find(".pixata-blazor-slider").ClassList.Contains("pixata-blazor-slider-vertical"));
    Assert.AreEqual("height:300px;", slider.Find(".pixata-blazor-slider").GetAttribute("style"));
    Assert.AreEqual("vertical", Input(slider).GetAttribute("aria-orientation"));
  }

  [TestMethod]
  public void ReadOnly_IgnoresInput_AndDisablesTheButtons() {
    IRenderedComponent<PixataSlider<int>> slider = _ctx.Render<PixataSlider<int>>(p => p
      .Add(c => c.Value, 5)
      .Add(c => c.ReadOnly, true));

    Input(slider).Input("8");

    Assert.AreEqual(5, slider.Instance.Value);
    Assert.AreEqual("true", Input(slider).GetAttribute("data-readonly"));
    Assert.IsTrue(slider.Find(".pixata-blazor-slider-increase").HasAttribute("disabled"));
  }

  [TestMethod]
  public void FirstRender_SetsUpTheInput() {
    _ctx.Render<PixataSlider<int>>();

    Assert.AreEqual(1, _ctx.JSInterop.Invocations["initSlider"].Count);
  }

  [TestMethod]
  public void UnsupportedType_Throws() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataSlider<string>>());

  [TestMethod]
  public void InAnEditForm_ShowsValidationErrorsAsInvalid() {
    Model model = new();
    EditContext editContext = new(model);
    IRenderedComponent<CascadingValue<EditContext>> form = _ctx.Render<CascadingValue<EditContext>>(p => p
      .Add(c => c.Value, editContext)
      .AddChildContent<PixataSlider<int>>(b => b
        .Add(c => c.Value, model.Rating)
        .Add(c => c.ValueExpression, () => model.Rating)));

    ValidationMessageStore messages = new(editContext);
    messages.Add(editContext.Field(nameof(Model.Rating)), "Too low");
    editContext.NotifyValidationStateChanged();

    Assert.IsTrue(form.Find(".pixata-blazor-slider").ClassList.Contains("pixata-blazor-invalid"));
  }

  private class Model {
    public int Rating { get; set; }
  }
}
