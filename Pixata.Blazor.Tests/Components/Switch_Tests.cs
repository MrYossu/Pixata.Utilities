using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Switch_Tests {
  private class Model {
    public bool Accepted { get; set; }
  }

  [TestMethod]
  public void Renders_AsAnOffSwitchByDefault() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p.Add(c => c.Value, false));
    IElement button = cut.Find("button");
    Assert.AreEqual("switch", button.GetAttribute("role"));
    Assert.AreEqual("button", button.GetAttribute("type"));
    Assert.AreEqual("false", button.GetAttribute("aria-checked"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-switch-off"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-switch-md"));
  }

  [TestMethod]
  public void True_RendersOn() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p.Add(c => c.Value, true));
    IElement button = cut.Find("button");
    Assert.AreEqual("true", button.GetAttribute("aria-checked"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-switch-on"));
  }

  [TestMethod]
  public void Click_TogglesTheValueAndRaisesOnChange() {
    using BunitContext context = PixataTestContext.Create();
    bool? newValue = null;
    object? changed = null;
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => newValue = v)
      .Add(c => c.OnChange, (object v) => changed = v));
    cut.Find("button").Click();
    Assert.AreEqual(true, newValue);
    Assert.AreEqual(true, changed);
    Assert.AreEqual("true", cut.Find("button").GetAttribute("aria-checked"));
    cut.Find("button").Click();
    Assert.AreEqual(false, newValue);
  }

  [TestMethod]
  public void NullableNull_RendersOffAndClickSetsTrue() {
    using BunitContext context = PixataTestContext.Create();
    bool? newValue = null;
    bool raised = false;
    IRenderedComponent<PixataSwitch<bool?>> cut = context.Render<PixataSwitch<bool?>>(p => p
      .Add(c => c.Value, null)
      .Add(c => c.ValueChanged, (bool? v) => {
        raised = true;
        newValue = v;
      }));
    Assert.AreEqual("false", cut.Find("button").GetAttribute("aria-checked"));
    cut.Find("button").Click();
    Assert.IsTrue(raised);
    Assert.AreEqual(true, newValue);
  }

  [TestMethod]
  public void Disabled_IsDisabledAndCannotBeToggled() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.Enabled, false)
      .Add(c => c.ValueChanged, (bool _) => raised = true));
    IElement button = cut.Find("button");
    Assert.IsTrue(button.HasAttribute("disabled"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-disabled"));
    button.Click();
    Assert.IsFalse(raised);
  }

  [TestMethod]
  public void ReadOnly_CanBeFocusedButNotToggled() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, true)
      .Add(c => c.ReadOnly, true)
      .Add(c => c.ValueChanged, (bool _) => raised = true));
    IElement button = cut.Find("button");
    Assert.AreEqual("true", button.GetAttribute("aria-readonly"));
    Assert.IsFalse(button.HasAttribute("disabled"));
    button.Click();
    Assert.IsFalse(raised);
    Assert.AreEqual("true", cut.Find("button").GetAttribute("aria-checked"));
  }

  [TestMethod]
  public void SwipeRight_TurnsItOnAndTheFollowingClickIsIgnored() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 10 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 50 });
    cut.Find("button").Click(new MouseEventArgs { Detail = 1 });
    CollectionAssert.AreEqual(new[] { true }, values);
    Assert.AreEqual("true", cut.Find("button").GetAttribute("aria-checked"));
  }

  [TestMethod]
  public void SwipeLeft_TurnsItOff() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, true)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 50 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 10 });
    CollectionAssert.AreEqual(new[] { false }, values);
  }

  [TestMethod]
  public void SwipeTowardsTheCurrentValue_ChangesNothing() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, true)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 10 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 50 });
    Assert.AreEqual(0, values.Count);
  }

  [TestMethod]
  [DataRow(10.0, 50.0, false, DisplayName = "Swipe right")]
  [DataRow(50.0, 10.0, true, DisplayName = "Swipe left")]
  public void RightToLeft_SwipeLeftTurnsItOn(double from, double to, bool expected) {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    // In a right-to-left page, the thumb of a switch that is off is at its right-hand end
    string thumb = cut.Find(".pixata-blazor-switch-thumb").GetAttribute("blazor:elementReference")!;
    BunitJSModuleInterop module = context.JSInterop.SetupModule("./_content/Pixata.Blazor/pixata-blazor.js");
    module.Setup<PixataSwitchBox?>("getBoundingRect", i => ((ElementReference)i.Arguments[0]!).Id == thumb).SetResult(new() { Left = 30, Width = 30 });
    module.Setup<PixataSwitchBox?>("getBoundingRect", i => ((ElementReference)i.Arguments[0]!).Id != thumb).SetResult(new() { Left = 0, Width = 60 });
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = from });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = to });
    CollectionAssert.AreEqual(expected ? new[] { true } : Array.Empty<bool>(), values);
  }

  [TestMethod]
  public void Tap_TogglesThroughTheClick() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 20 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 22 });
    cut.Find("button").Click();
    CollectionAssert.AreEqual(new[] { true }, values);
  }

  [TestMethod]
  public void SwipeWithoutAClick_DoesNotSwallowTheNextTap() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 10 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 50 });
    // A touch screen may not send a click after a swipe, so the next tap must still work
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 20 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 20 });
    cut.Find("button").Click(new MouseEventArgs { Detail = 1 });
    CollectionAssert.AreEqual(new[] { true, false }, values);
  }

  [TestMethod]
  public void SwipeWithoutAClick_DoesNotSwallowTheNextKeyPress() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> values = [];
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => values.Add(v)));
    cut.Find("button").PointerDown(new PointerEventArgs { ClientX = 10 });
    cut.Find("button").PointerUp(new PointerEventArgs { ClientX = 50 });
    // Space or Enter on the focused switch (or a click on its label) is a click with a detail of 0, and no pointer down
    cut.Find("button").Click(new MouseEventArgs { Detail = 0 });
    CollectionAssert.AreEqual(new[] { true, false }, values);
  }

  [TestMethod]
  public void LabelsWidthSizeAndRoundness_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.OnLabel, "Yes")
      .Add(c => c.OffLabel, "No")
      .Add(c => c.Width, "90px")
      .Add(c => c.Size, PixataThemeConstants.Size.Large)
      .Add(c => c.Rounded, PixataThemeConstants.Rounded.Medium)
      .Add(c => c.ThumbRounded, PixataThemeConstants.Rounded.None)
      .Add(c => c.Id, "terms")
      .Add(c => c.TabIndex, 3)
      .Add(c => c.Class, "mine"));
    IElement button = cut.Find("button");
    Assert.AreEqual("Yes", cut.Find(".pixata-blazor-switch-label-on").TextContent);
    Assert.AreEqual("No", cut.Find(".pixata-blazor-switch-label-off").TextContent);
    StringAssert.Contains(button.GetAttribute("style"), "--pixata-blazor-switch-width:90px");
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-switch-lg"));
    Assert.IsTrue(button.ClassList.Contains("mine"));
    Assert.AreEqual("terms", button.Id);
    Assert.AreEqual("3", button.GetAttribute("tabindex"));
    Assert.IsTrue(cut.Find(".pixata-blazor-switch-track").ClassList.Contains("pixata-blazor-rounded-md"));
    Assert.IsTrue(cut.Find(".pixata-blazor-switch-thumb").ClassList.Contains("pixata-blazor-rounded-none"));
  }

  [TestMethod]
  public void AriaAttributes_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.AriaLabel, "Notifications")
      .Add(c => c.AriaDescribedBy, "help"));
    IElement button = cut.Find("button");
    Assert.AreEqual("Notifications", button.GetAttribute("aria-label"));
    Assert.AreEqual("help", button.GetAttribute("aria-describedby"));
    Assert.AreEqual("true", cut.Find(".pixata-blazor-switch-label-on").GetAttribute("aria-hidden"));
  }

  [TestMethod]
  public async Task InsideAnEditForm_NotifiesTheFormAndShowsTheInvalidState() {
    using BunitContext context = PixataTestContext.Create();
    Model model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataSwitch<bool>> cut = context.Render<PixataSwitch<bool>>(p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.Accepted)
      .Add(c => c.ValueChanged, (bool v) => model.Accepted = v)
      .Add(c => c.ValueExpression, () => model.Accepted));
    cut.Find("button").Click();
    Assert.IsTrue(model.Accepted);
    CollectionAssert.AreEqual(new[] { nameof(Model.Accepted) }, changed);
    Assert.IsFalse(cut.Find("button").ClassList.Contains("pixata-blazor-invalid"));
    await cut.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(Model.Accepted)), "You must accept");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(cut.Find("button").ClassList.Contains("pixata-blazor-invalid"));
    Assert.AreEqual("true", cut.Find("button").GetAttribute("aria-invalid"));
  }

  [TestMethod]
  public void UnsupportedType_Throws() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataSwitch<int>>(p => p.Add(c => c.Value, 1)));
  }
}
