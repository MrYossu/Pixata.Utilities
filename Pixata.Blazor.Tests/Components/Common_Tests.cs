using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Common_Tests {
  private record Address(string City);

  private record Person(int Id, string Name, Address? Address);

  private class Editable {
    public string Name { get; set; } = "";
    public int Age { get; set; }
  }

  [TestMethod]
  public void PixataFieldReader_NoField_ReturnsTheItem() =>
    Assert.AreEqual("Fred", PixataFieldReader.GetValue("Fred", null));

  [TestMethod]
  public void PixataFieldReader_Field_ReturnsThePropertyValue() =>
    Assert.AreEqual("Fred", PixataFieldReader.GetValue(new Person(1, "Fred", null), nameof(Person.Name)));

  [TestMethod]
  public void PixataFieldReader_NestedField_ReturnsTheNestedValue() =>
    Assert.AreEqual("Leeds", PixataFieldReader.GetValue(new Person(1, "Fred", new("Leeds")), "Address.City"));

  [TestMethod]
  public void PixataFieldReader_NestedFieldThroughNull_ReturnsNull() =>
    Assert.IsNull(PixataFieldReader.GetValue(new Person(1, "Fred", null), "Address.City"));

  [TestMethod]
  public void PixataFieldReader_ExpandoObject_ReadsByKey() {
    dynamic item = new ExpandoObject();
    item.Name = "Fred";
    Assert.AreEqual("Fred", PixataFieldReader.GetValue((object)item, "Name"));
  }

  [TestMethod]
  public void PixataFieldReader_MissingField_Throws() =>
    Assert.ThrowsException<InvalidOperationException>(() => PixataFieldReader.GetValue(new Person(1, "Fred", null), "Nmae"));

  [TestMethod]
  public void PixataFieldReader_GetValueOfT_Converts() =>
    Assert.AreEqual(1L, PixataFieldReader.GetValue<long>(new Person(1, "Fred", null), nameof(Person.Id)));

  [TestMethod]
  public void PixataFieldReader_SetValue_ConvertsAndSets() {
    Editable item = new();
    PixataFieldReader.SetValue(item, nameof(Editable.Age), 42L);
    Assert.AreEqual(42, item.Age);
  }

  [TestMethod]
  public void PixataCss_Classes_SkipsBlanks() =>
    Assert.AreEqual("a b", PixataCss.Classes("a", null, " ", "b"));

  [TestMethod]
  public void PixataCss_Button_Defaults() =>
    Assert.AreEqual("pixata-blazor-button pixata-blazor-button-md pixata-blazor-rounded-md pixata-blazor-button-solid pixata-blazor-button-base",
      PixataCss.Button(null, null, null, null));

  [TestMethod]
  public void PixataCss_Button_UsesTheValuesGiven() =>
    Assert.AreEqual("pixata-blazor-button pixata-blazor-button-lg pixata-blazor-rounded-full pixata-blazor-button-outline pixata-blazor-button-primary pixata-blazor-icon-button",
      PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Primary, PixataThemeConstants.Button.FillMode.Outline, PixataThemeConstants.Button.Rounded.Full, PixataThemeConstants.Button.Size.Large, true));

  [TestMethod]
  public void PixataIcon_SvgIcon_RendersInlineSvg() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataIcon> cut = context.Render<PixataIcon>(p => p.Add(c => c.Icon, PixataSvgIcons.Check));
    Assert.IsNotNull(cut.Find("span.pixata-blazor-svg-icon-check svg path"));
  }

  [TestMethod]
  public void PixataIcon_String_RendersFontIconClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataIcon> cut = context.Render<PixataIcon>(p => p.Add(c => c.Icon, "fas fa-save"));
    Assert.IsTrue(cut.Find("span").ClassList.Contains("fa-save"));
  }

  [TestMethod]
  public void PixataPopup_Hidden_RendersNothing() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataPopup> cut = context.Render<PixataPopup>(p => p.Add(c => c.Visible, false));
    Assert.AreEqual("", cut.Markup.Trim());
  }

  [TestMethod]
  public void PixataPopup_Visible_RendersContentWithSettings() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataPopup> cut = context.Render<PixataPopup>(p => p
      .Add(c => c.Visible, true)
      .Add(c => c.Settings, new PixataPopupSettings { Width = "300px", MaxHeight = "100px", Class = "mine" })
      .AddChildContent("<p>Hello</p>"));
    AngleSharp.Dom.IElement popup = cut.Find("div.pixata-blazor-popup.mine");
    StringAssert.Contains(popup.GetAttribute("style"), "width:300px");
    StringAssert.Contains(popup.GetAttribute("style"), "--pixata-blazor-popup-max-height:100px");
    Assert.AreEqual("Hello", cut.Find("p").TextContent);
  }

  [TestMethod]
  public async Task PixataPopup_OutsideClick_RaisesTheEvent() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataPopup> cut = context.Render<PixataPopup>(p => p
      .Add(c => c.Visible, true)
      .Add(c => c.OnOutsideClick, () => raised = true));
    await cut.InvokeAsync(cut.Instance.HandleOutsideClick);
    Assert.IsTrue(raised);
  }

  [TestMethod]
  public async Task PixataInputBase_UpdateValue_RaisesValueChangedAndValidates() {
    using BunitContext context = PixataTestContext.Create();
    Editable model = new();
    EditContext editContext = new(model);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    string? newValue = null;
    IRenderedComponent<TestInput> cut = context.Render<TestInput>(p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.Name)
      .Add(c => c.ValueChanged, (string v) => newValue = v)
      .Add(c => c.ValueExpression, () => model.Name));
    await cut.InvokeAsync(() => cut.Instance.Set("Jim"));
    Assert.AreEqual("Jim", newValue);
    CollectionAssert.AreEqual(new[] { nameof(Editable.Name) }, changed);
  }

  [TestMethod]
  public async Task PixataInputBase_ValidationMessage_MakesItInvalid() {
    using BunitContext context = PixataTestContext.Create();
    Editable model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    IRenderedComponent<TestInput> cut = context.Render<TestInput>(p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.Name)
      .Add(c => c.ValueExpression, () => model.Name));
    Assert.IsFalse(cut.Find("input").ClassList.Contains("pixata-blazor-invalid"));
    await cut.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(Editable.Name)), "Required");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(cut.Find("input").ClassList.Contains("pixata-blazor-invalid"));
  }

  [TestMethod]
  public async Task PixataInputBase_OutsideAnEditForm_Works() {
    using BunitContext context = PixataTestContext.Create();
    string? newValue = null;
    IRenderedComponent<TestInput> cut = context.Render<TestInput>(p => p
      .Add(c => c.Value, "")
      .Add(c => c.ValueChanged, (string v) => newValue = v));
    await cut.InvokeAsync(() => cut.Instance.Set("Jim"));
    Assert.AreEqual("Jim", newValue);
  }

  public class TestInput : PixataInputBase<string> {
    public Task Set(string value) =>
      UpdateValue(value);

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
      builder.OpenElement(0, "input");
      builder.AddAttribute(1, "class", PixataCss.Classes("pixata-blazor-input", InvalidClass));
      builder.AddAttribute(2, "value", Value);
      builder.CloseElement();
    }
  }
}
