using System;
using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataNumericTextBox_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    // The box sets up its focus script once it's rendered. Without it (as here, where the call returns false), .NET swaps
    // the formatted text for the plain number itself
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  private static IElement Input<TValue>(IRenderedComponent<PixataNumericTextBox<TValue>> box) =>
    box.Find("input");

  [TestMethod]
  public void ShowsTheFormattedValueWithoutFocus_AndThePlainValueWithFocus() {
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Value, 1234.5m)
      .Add(c => c.Format, "N2"));

    Assert.AreEqual("1,234.50", Input(box).GetAttribute("value"));
    Input(box).Focus();
    Assert.AreEqual("1234.5", Input(box).GetAttribute("value"));
    Input(box).Blur();
    Assert.AreEqual("1,234.50", Input(box).GetAttribute("value"));
  }

  [TestMethod]
  public void WithAFormat_TellsTheScriptThePlainNumber_SoItCanSwapItInOnFocus() {
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Value, 60m)
      .Add(c => c.Format, "C2"));

    Assert.AreEqual("£60.00", Input(box).GetAttribute("value"));
    Assert.AreEqual("60", Input(box).GetAttribute("data-pixata-edit-text"));
    Assert.IsNull(Input(box).GetAttribute("data-pixata-select-on-focus"));

    box.Render(p => p.Add(c => c.Value, 1234.5m));
    Assert.AreEqual("1234.5", Input(box).GetAttribute("data-pixata-edit-text"));
  }

  [TestMethod]
  public void WithoutAFormat_HasNoTextForTheScriptToSwapIn() {
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p.Add(c => c.Value, 60m));

    Assert.IsFalse(Input(box).HasAttribute("data-pixata-edit-text"));
  }

  [TestMethod]
  public void SelectOnFocus_IsPassedToTheScript() {
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Value, 60m)
      .Add(c => c.SelectOnFocus, true));

    Assert.AreEqual("true", Input(box).GetAttribute("data-pixata-select-on-focus"));
  }

  [TestMethod]
  public void OnceTheScriptIsSetUp_FocusLeavesTheTextAlone_SoARenderCantPutTheOldNumberBackOverWhatsTyped() {
    // The script has swapped the text in the browser already, keeping the selection. If .NET rendered the plain number too,
    // that render would arrive a round trip later in Blazor Server, and replace the first digits typed (#203)
    BunitJSModuleInterop module = _ctx.JSInterop.SetupModule(PixataBlazorScript.Path);
    module.Setup<bool>("initNumericTextBox", _ => true).SetResult(true);
    decimal value = 60m;
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.Format, "C2")
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<decimal>(this, v => value = v)));

    Input(box).Focus();
    Assert.AreEqual("£60.00", Input(box).GetAttribute("value"));
    Input(box).KeyDown("1");
    Assert.AreEqual("£60.00", Input(box).GetAttribute("value"));

    Input(box).Input("12.34");
    Assert.AreEqual(12.34m, value);
    Assert.AreEqual("12.34", Input(box).GetAttribute("value"));
    Input(box).Blur();
    Assert.AreEqual("£12.34", Input(box).GetAttribute("value"));
  }

  [TestMethod]
  public void OnceTheScriptIsSetUp_ArrowKeysStillShowThePlainNumber() {
    BunitJSModuleInterop module = _ctx.JSInterop.SetupModule(PixataBlazorScript.Path);
    module.Setup<bool>("initNumericTextBox", _ => true).SetResult(true);
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Value, 60m)
      .Add(c => c.Format, "C2"));

    Input(box).Focus();
    Input(box).KeyDown("ArrowUp");

    Assert.AreEqual("61", Input(box).GetAttribute("value"));
  }

  [TestMethod]
  public void Typing_UpdatesTheValue() {
    decimal? value = null;
    IRenderedComponent<PixataNumericTextBox<decimal?>> box = _ctx.Render<PixataNumericTextBox<decimal?>>(p => p
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<decimal?>(this, v => value = v)));

    Input(box).Focus();
    Input(box).Input("12.34");
    Assert.AreEqual(12.34m, value);
    Input(box).Input("");
    Assert.IsNull(value);
  }

  [TestMethod]
  public void PastedCurrencyAndThousands_AreAccepted() {
    decimal value = 0;
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<decimal>(this, v => value = v)));

    Input(box).Focus();
    Input(box).Input("£1,234.50");
    Assert.AreEqual(1234.5m, value);
  }

  [TestMethod]
  public void TextThatIsntANumber_KeepsTheValue_AndShowsAsInvalidUntilBlur() {
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p.Add(c => c.Value, 5));

    Input(box).Focus();
    Input(box).Input("abc");
    Assert.AreEqual(5, box.Instance.Value);
    Assert.IsTrue(box.Find(".pixata-blazor-numerictextbox").ClassList.Contains("pixata-blazor-numerictextbox-invalid"));
    Input(box).Blur();
    Assert.AreEqual("5", Input(box).GetAttribute("value"));
    Assert.IsFalse(box.Find(".pixata-blazor-numerictextbox").ClassList.Contains("pixata-blazor-numerictextbox-invalid"));
  }

  [TestMethod]
  public void MinMaxAndDecimals_AreAppliedOnBlur_NotWhileTyping() {
    decimal value = 0;
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Min, 10m)
      .Add(c => c.Max, 100m)
      .Add(c => c.Decimals, 2)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<decimal>(this, v => value = v)));

    Input(box).Focus();
    Input(box).Input("1");
    Assert.AreEqual(1m, value);
    Input(box).Blur();
    Assert.AreEqual(10m, value);

    Input(box).Focus();
    Input(box).Input("12.345");
    Input(box).Blur();
    Assert.AreEqual(12.35m, value);

    Input(box).Focus();
    Input(box).Input("500");
    Input(box).Blur();
    Assert.AreEqual(100m, value);
  }

  [TestMethod]
  public void WholeNumberTypes_RoundTypedDecimals() {
    int value = 0;
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v)));

    Input(box).Focus();
    Input(box).Input("2.5");
    Assert.AreEqual(3, value);
  }

  [TestMethod]
  public void NumberTooBigForTheType_IsInvalid() {
    IRenderedComponent<PixataNumericTextBox<short>> box = _ctx.Render<PixataNumericTextBox<short>>(p => p.Add(c => c.Value, (short)1));

    Input(box).Focus();
    Input(box).Input("99999");
    Assert.AreEqual((short)1, box.Instance.Value);
    Assert.IsTrue(box.Find(".pixata-blazor-numerictextbox").ClassList.Contains("pixata-blazor-numerictextbox-invalid"));
  }

  [TestMethod]
  public void ArrowKeysAndSpinButtons_StepTheValue_WithinMinAndMax() {
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p
      .Add(c => c.Value, 1m)
      .Add(c => c.Step, 0.5m)
      .Add(c => c.Max, 2m));

    Input(box).Focus();
    Input(box).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
    Assert.AreEqual(1.5m, box.Instance.Value);
    box.Find(".pixata-blazor-numerictextbox-increase").Click();
    Assert.AreEqual(2m, box.Instance.Value);
    box.Find(".pixata-blazor-numerictextbox-increase").Click();
    Assert.AreEqual(2m, box.Instance.Value);
    Input(box).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual(1.5m, box.Instance.Value);
  }

  [TestMethod]
  public void WithoutAStep_TheArrowsStepByOne() {
    IRenderedComponent<PixataNumericTextBox<decimal>> box = _ctx.Render<PixataNumericTextBox<decimal>>(p => p.Add(c => c.Value, 2.5m));

    Input(box).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
    Assert.AreEqual(3.5m, box.Instance.Value);
  }

  [TestMethod]
  public void AMinOfZero_IsApplied() {
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p
      .Add(c => c.Value, 0)
      .Add(c => c.Min, 0));

    Input(box).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    Assert.AreEqual(0, box.Instance.Value);
  }

  [TestMethod]
  public void ArrowsFalse_HidesTheSpinButtons() {
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p.Add(c => c.Arrows, false));
    Assert.AreEqual(0, box.FindAll(".pixata-blazor-numerictextbox-spinner").Count);
  }

  [TestMethod]
  public void OnChange_FiresOnBlurOrEnter_OnlyWhenTheValueChanged() {
    int changes = 0;
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p
      .Add(c => c.Value, 1)
      .Add(c => c.OnChange, EventCallback.Factory.Create<object?>(this, _ => changes++)));

    Input(box).Focus();
    Input(box).Blur();
    Assert.AreEqual(0, changes);

    Input(box).Focus();
    Input(box).Input("7");
    Assert.AreEqual(0, changes);
    Input(box).KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual(1, changes);
    Input(box).Blur();
    Assert.AreEqual(1, changes);
  }

  [TestMethod]
  public void ReadOnly_IgnoresArrowKeys() {
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p
      .Add(c => c.Value, 1)
      .Add(c => c.ReadOnly, true));

    Input(box).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
    Assert.AreEqual(1, box.Instance.Value);
  }

  [TestMethod]
  public void SpinButtonText_CanBeTranslated() {
    IRenderedComponent<PixataNumericTextBox<int>> box = _ctx.Render<PixataNumericTextBox<int>>(p => p
      .Add(c => c.IncreaseValueText, "Plus")
      .Add(c => c.DecreaseValueText, "Moins"));

    Assert.AreEqual("Plus", box.Find(".pixata-blazor-numerictextbox-increase").GetAttribute("aria-label"));
    Assert.AreEqual("Moins", box.Find(".pixata-blazor-numerictextbox-decrease").GetAttribute("title"));
  }

  [TestMethod]
  public void UnsupportedType_Throws() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataNumericTextBox<string>>());

  [TestMethod]
  public void InAnEditForm_ShowsValidationErrorsAsInvalid() {
    Model model = new();
    EditContext editContext = new(model);
    IRenderedComponent<CascadingValue<EditContext>> form = _ctx.Render<CascadingValue<EditContext>>(p => p
      .Add(c => c.Value, editContext)
      .AddChildContent<PixataNumericTextBox<decimal>>(b => b
        .Add(c => c.Value, model.Amount)
        .Add(c => c.ValueExpression, () => model.Amount)));

    ValidationMessageStore messages = new(editContext);
    messages.Add(editContext.Field(nameof(Model.Amount)), "Too much");
    editContext.NotifyValidationStateChanged();

    Assert.IsTrue(form.Find(".pixata-blazor-numerictextbox").ClassList.Contains("pixata-blazor-numerictextbox-invalid"));
  }

  private class Model {
    public decimal Amount { get; set; }
  }
}
