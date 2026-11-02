using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;
using Pixata.Blazor.Forms;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class CheckBox_Tests {
  private class Model {
    public bool Accepted { get; set; }
  }

  [TestMethod]
  public void Renders_AsAnUncheckedCheckBoxByDefault() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p.Add(c => c.Value, false));
    IElement button = cut.Find("button");
    Assert.AreEqual("checkbox", button.GetAttribute("role"));
    Assert.AreEqual("button", button.GetAttribute("type"));
    Assert.AreEqual("false", button.GetAttribute("aria-checked"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-checkbox"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-checkbox-md"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-rounded-md"));
    Assert.IsFalse(button.ClassList.Contains("pixata-blazor-checkbox-checked"));
  }

  [TestMethod]
  public void True_RendersChecked() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p.Add(c => c.Value, true));
    IElement button = cut.Find("button");
    Assert.AreEqual("true", button.GetAttribute("aria-checked"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-checkbox-checked"));
  }

  [TestMethod]
  public void SizeAndRounded_SetTheClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p
      .Add(c => c.Size, PixataThemeConstants.Size.Large)
      .Add(c => c.Rounded, PixataThemeConstants.Rounded.Full)
      .Add(c => c.Id, "terms")
      .Add(c => c.Class, "extra"));
    IElement button = cut.Find("button");
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-checkbox-lg"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-rounded-full"));
    Assert.IsTrue(button.ClassList.Contains("extra"));
    Assert.AreEqual("terms", button.GetAttribute("id"));
  }

  [TestMethod]
  public void Click_TogglesTheValueAndRaisesOnChangeAfterValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<string> events = [];
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p
      .Add(c => c.Value, false)
      .Add(c => c.ValueChanged, (bool v) => events.Add($"ValueChanged {v}"))
      .Add(c => c.OnChange, (object v) => events.Add($"OnChange {v}")));
    cut.Find("button").Click();
    CollectionAssert.AreEqual(new[] { "ValueChanged True", "OnChange True" }, events);
    Assert.AreEqual("true", cut.Find("button").GetAttribute("aria-checked"));
    cut.Find("button").Click();
    CollectionAssert.AreEqual(new[] { "ValueChanged True", "OnChange True", "ValueChanged False", "OnChange False" }, events);
  }

  [TestMethod]
  public void NullableNull_RendersIndeterminateAndClickSetsTrue() {
    using BunitContext context = PixataTestContext.Create();
    List<bool?> values = [];
    IRenderedComponent<PixataCheckBox<bool?>> cut = context.Render<PixataCheckBox<bool?>>(p => p
      .Add(c => c.Value, null)
      .Add(c => c.ValueChanged, (bool? v) => values.Add(v)));
    IElement button = cut.Find("button");
    Assert.AreEqual("mixed", button.GetAttribute("aria-checked"));
    Assert.IsTrue(button.ClassList.Contains("pixata-blazor-checkbox-indeterminate"));
    button.Click();
    CollectionAssert.AreEqual(new bool?[] { true }, values);
    cut.Find("button").Click();
    CollectionAssert.AreEqual(new bool?[] { true, false }, values);
  }

  [TestMethod]
  public void Indeterminate_ShowsMixedAndIsClearedByAClick() {
    using BunitContext context = PixataTestContext.Create();
    List<bool> indeterminate = [];
    List<bool> values = [];
    object? changed = null;
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p
      .Add(c => c.Value, true)
      .Add(c => c.Indeterminate, true)
      .Add(c => c.IndeterminateChanged, (bool v) => indeterminate.Add(v))
      .Add(c => c.ValueChanged, (bool v) => values.Add(v))
      .Add(c => c.OnChange, (object v) => changed = v));
    Assert.AreEqual("mixed", cut.Find("button").GetAttribute("aria-checked"));
    cut.Find("button").Click();
    CollectionAssert.AreEqual(new[] { false }, indeterminate);
    // It was already true, so only the indeterminate state changed
    Assert.AreEqual(0, values.Count);
    Assert.AreEqual(true, changed);
    Assert.AreEqual("true", cut.Find("button").GetAttribute("aria-checked"));
  }

  [TestMethod]
  public void Disabled_IsDisabledAndCannotBeToggled() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p
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
  public void AriaAttributes_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p
      .Add(c => c.AriaLabel, "Global admin")
      .Add(c => c.AriaLabelledBy, "roles")
      .Add(c => c.AriaDescribedBy, "roles-help")
      .Add(c => c.TabIndex, 3));
    IElement button = cut.Find("button");
    Assert.AreEqual("Global admin", button.GetAttribute("aria-label"));
    Assert.AreEqual("roles", button.GetAttribute("aria-labelledby"));
    Assert.AreEqual("roles-help", button.GetAttribute("aria-describedby"));
    Assert.AreEqual("3", button.GetAttribute("tabindex"));
  }

  [TestMethod]
  public async Task InsideAnEditForm_NotifiesTheFormAndShowsTheInvalidState() {
    using BunitContext context = PixataTestContext.Create();
    Model model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataCheckBox<bool>> cut = context.Render<PixataCheckBox<bool>>(p => p
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
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataCheckBox<int>>(p => p.Add(c => c.Value, 1)));
  }

  [TestMethod]
  public void FormRowCheckbox_UsesPixataCheckBox() {
    using BunitContext context = PixataTestContext.Create();
    bool? checkedValue = null;
    IRenderedComponent<FormRowCheckbox> cut = context.Render<FormRowCheckbox>(p => p
      .Add(c => c.PropertyName, "Subscribe")
      .Add(c => c.Checked, false)
      .Add(c => c.CheckedChanged, (bool v) => checkedValue = v));
    IElement button = cut.Find("button.pixata-blazor-checkbox");
    Assert.AreEqual("Subscribe", button.GetAttribute("id"));
    button.Click();
    Assert.AreEqual(true, checkedValue);
  }
}
