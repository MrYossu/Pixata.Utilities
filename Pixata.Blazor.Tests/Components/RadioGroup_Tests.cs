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
public class RadioGroup_Tests {
  private static readonly string ModulePath = PixataJs.ModulePath;

  private class ContactMethod {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Unavailable { get; set; }
  }

  private class TextValueItem {
    public string Text { get; set; } = "";
    public int Value { get; set; }
  }

  private enum Colour {
    Red,
    Green,
    Blue
  }

  private class Model {
    public int? ContactMethodId { get; set; }
  }

  private static readonly List<ContactMethod> ContactMethods = [
    new() { Id = 1, Name = "Email" },
    new() { Id = 2, Name = "Phone" },
    new() { Id = 3, Name = "Post", Unavailable = true }
  ];

  private static readonly List<string> Sizes = ["Small", "Medium", "Large"];

  [TestMethod]
  public void PrimitiveData_RendersARadioButtonPerItemInARadioGroup() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Value, "Medium"));
    Assert.AreEqual("radiogroup", cut.Find("ul").GetAttribute("role"));
    IReadOnlyList<IElement> inputs = cut.FindAll("input[type=radio]");
    Assert.AreEqual(3, inputs.Count);
    Assert.AreEqual(1, inputs.Select(i => i.GetAttribute("name")).Distinct().Count());
    CollectionAssert.AreEqual(Sizes, cut.FindAll("label").Select(l => l.TextContent.Trim()).ToList());
    Assert.IsFalse(inputs[0].HasAttribute("checked"));
    Assert.IsTrue(inputs[1].HasAttribute("checked"));
    Assert.IsTrue(cut.FindAll("li")[1].ClassList.Contains("pixata-blazor-radiogroup-item-checked"));
  }

  [TestMethod]
  public void Labels_PointAtTheirRadioButtons() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Id, "size")
      .Add(c => c.Name, "size-choice"));
    Assert.AreEqual("size", cut.Find("ul").Id);
    IReadOnlyList<IElement> inputs = cut.FindAll("input");
    IReadOnlyList<IElement> labels = cut.FindAll("label");
    for (int i = 0; i < inputs.Count; i++) {
      Assert.AreEqual($"size-{i}", inputs[i].Id);
      Assert.AreEqual(inputs[i].Id, labels[i].GetAttribute("for"));
      Assert.AreEqual("size-choice", inputs[i].GetAttribute("name"));
    }
  }

  [TestMethod]
  public void Choosing_UpdatesTheValueAndRaisesOnChange() {
    using BunitContext context = PixataTestContext.Create();
    string? newValue = null;
    object? changed = null;
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Value, "Small")
      .Add(c => c.ValueChanged, (string v) => newValue = v)
      .Add(c => c.OnChange, (object v) => changed = v));
    cut.FindAll("input")[2].Change(true);
    Assert.AreEqual("Large", newValue);
    Assert.AreEqual("Large", changed);
    Assert.IsTrue(cut.FindAll("input")[2].HasAttribute("checked"));
    Assert.IsFalse(cut.FindAll("input")[0].HasAttribute("checked"));
  }

  [TestMethod]
  public void ChoosingTheCurrentValue_DoesNotRaiseOnChange() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Value, "Small")
      .Add(c => c.OnChange, (object _) => raised = true));
    cut.FindAll("input")[0].Change(true);
    Assert.IsFalse(raised);
  }

  [TestMethod]
  public void ModelData_UsesTextFieldAndValueField() {
    using BunitContext context = PixataTestContext.Create();
    int? newValue = null;
    IRenderedComponent<PixataRadioGroup<ContactMethod, int?>> cut = context.Render<PixataRadioGroup<ContactMethod, int?>>(p => p
      .Add(c => c.Data, ContactMethods)
      .Add(c => c.TextField, nameof(ContactMethod.Name))
      .Add(c => c.ValueField, nameof(ContactMethod.Id))
      .Add(c => c.Value, 2)
      .Add(c => c.ValueChanged, (int? v) => newValue = v));
    CollectionAssert.AreEqual(new[] { "Email", "Phone", "Post" }, cut.FindAll("label").Select(l => l.TextContent.Trim()).ToList());
    Assert.IsTrue(cut.FindAll("input")[1].HasAttribute("checked"));
    cut.FindAll("input")[0].Change(true);
    Assert.AreEqual(1, newValue);
  }

  [TestMethod]
  public void ModelData_DefaultsToTextAndValueFields() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<TextValueItem, int>> cut = context.Render<PixataRadioGroup<TextValueItem, int>>(p => p
      .Add(c => c.Data, [new() { Text = "One", Value = 1 }, new() { Text = "Two", Value = 2 }])
      .Add(c => c.Value, 2));
    Assert.AreEqual("Two", cut.FindAll("label")[1].TextContent.Trim());
    Assert.IsTrue(cut.FindAll("input")[1].HasAttribute("checked"));
  }

  [TestMethod]
  public void EnumData_IsTreatedAsPrimitive() {
    using BunitContext context = PixataTestContext.Create();
    Colour newValue = Colour.Red;
    IRenderedComponent<PixataRadioGroup<Colour, Colour>> cut = context.Render<PixataRadioGroup<Colour, Colour>>(p => p
      .Add(c => c.Data, [Colour.Red, Colour.Green, Colour.Blue])
      .Add(c => c.Value, Colour.Red)
      .Add(c => c.ValueChanged, (Colour v) => newValue = v));
    Assert.AreEqual("Green", cut.FindAll("label")[1].TextContent.Trim());
    cut.FindAll("input")[2].Change(true);
    Assert.AreEqual(Colour.Blue, newValue);
  }

  [TestMethod]
  public void ItemTemplate_RendersTheLabels() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<ContactMethod, int>> cut = context.Render<PixataRadioGroup<ContactMethod, int>>(p => p
      .Add(c => c.Data, ContactMethods)
      .Add(c => c.ValueField, nameof(ContactMethod.Id))
      .Add(c => c.ItemTemplate, (RenderFragment<ContactMethod>)(item => builder => {
        builder.OpenElement(0, "strong");
        builder.AddContent(1, item.Name.ToUpperInvariant());
        builder.CloseElement();
      })));
    Assert.AreEqual("PHONE", cut.FindAll("label strong")[1].TextContent);
  }

  [TestMethod]
  public void LayoutAndLabelPosition_AreApplied() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Layout, PixataRadioGroupLayout.Horizontal)
      .Add(c => c.LabelPosition, PixataRadioGroupLabelPosition.Before));
    Assert.IsTrue(cut.Find("ul").ClassList.Contains("pixata-blazor-radiogroup-horizontal"));
    IElement item = cut.Find("li");
    Assert.AreEqual("LABEL", item.Children[0].TagName);
    Assert.AreEqual("INPUT", item.Children[1].TagName);
  }

  [TestMethod]
  public void Defaults_AreVerticalWithLabelsAfter() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p.Add(c => c.Data, Sizes));
    Assert.IsTrue(cut.Find("ul").ClassList.Contains("pixata-blazor-radiogroup-vertical"));
    IElement item = cut.Find("li");
    Assert.AreEqual("INPUT", item.Children[0].TagName);
    Assert.AreEqual("LABEL", item.Children[1].TagName);
    Assert.IsTrue(cut.Find("input").ClassList.Contains("pixata-blazor-radiogroup-radio-md"));
    Assert.IsTrue(cut.Find("ul").ClassList.Contains("pixata-blazor-radiogroup-filled"));
  }

  [TestMethod]
  public void RingCheckedStyle_IsApplied() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.CheckedStyle, PixataRadioGroupCheckedStyle.Ring));
    Assert.IsTrue(cut.Find("ul").ClassList.Contains("pixata-blazor-radiogroup-ring"));
    Assert.IsFalse(cut.Find("ul").ClassList.Contains("pixata-blazor-radiogroup-filled"));
  }

  [TestMethod]
  public void SizeClassAndTabIndex_AreRendered() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Size, PixataThemeConstants.Size.Large)
      .Add(c => c.TabIndex, 4)
      .Add(c => c.Class, "mine"));
    Assert.IsTrue(cut.Find("input").ClassList.Contains("pixata-blazor-radiogroup-radio-lg"));
    Assert.AreEqual("4", cut.Find("input").GetAttribute("tabindex"));
    Assert.IsTrue(cut.Find("ul").ClassList.Contains("mine"));
  }

  [TestMethod]
  public void Disabled_DisablesEveryRadioButtonAndIgnoresChanges() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Enabled, false)
      .Add(c => c.ValueChanged, (string _) => raised = true));
    Assert.AreEqual("true", cut.Find("ul").GetAttribute("aria-disabled"));
    Assert.IsTrue(cut.FindAll("input").All(i => i.HasAttribute("disabled")));
    cut.FindAll("input")[1].Change(true);
    Assert.IsFalse(raised);
  }

  [TestMethod]
  public void DisabledField_DisablesThoseItems() {
    using BunitContext context = PixataTestContext.Create();
    bool raised = false;
    IRenderedComponent<PixataRadioGroup<ContactMethod, int>> cut = context.Render<PixataRadioGroup<ContactMethod, int>>(p => p
      .Add(c => c.Data, ContactMethods)
      .Add(c => c.TextField, nameof(ContactMethod.Name))
      .Add(c => c.ValueField, nameof(ContactMethod.Id))
      .Add(c => c.DisabledField, nameof(ContactMethod.Unavailable))
      .Add(c => c.ValueChanged, (int _) => raised = true));
    IReadOnlyList<IElement> inputs = cut.FindAll("input");
    Assert.IsFalse(inputs[0].HasAttribute("disabled"));
    Assert.IsTrue(inputs[2].HasAttribute("disabled"));
    Assert.IsTrue(cut.FindAll("li")[2].ClassList.Contains("pixata-blazor-disabled"));
    inputs[2].Change(true);
    Assert.IsFalse(raised);
  }

  [TestMethod]
  public void NoData_RendersAnEmptyGroup() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p.Add(c => c.Data, null));
    Assert.AreEqual(0, cut.FindAll("li").Count);
  }

  [TestMethod]
  public void AriaAttributes_AreRenderedOnTheGroup() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.AriaLabelledBy, "heading")
      .Add(c => c.AriaDescribedBy, "help")
      .AddUnmatched("data-test", "group"));
    IElement group = cut.Find("ul");
    Assert.AreEqual("heading", group.GetAttribute("aria-labelledby"));
    Assert.AreEqual("help", group.GetAttribute("aria-describedby"));
    Assert.AreEqual("group", group.GetAttribute("data-test"));
  }

  [TestMethod]
  public async Task InsideAnEditForm_NotifiesTheFormAndShowsTheInvalidState() {
    using BunitContext context = PixataTestContext.Create();
    Model model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    List<string> changed = [];
    editContext.OnFieldChanged += (_, e) => changed.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataRadioGroup<ContactMethod, int?>> cut = context.Render<PixataRadioGroup<ContactMethod, int?>>(p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Data, ContactMethods)
      .Add(c => c.TextField, nameof(ContactMethod.Name))
      .Add(c => c.ValueField, nameof(ContactMethod.Id))
      .Add(c => c.Value, model.ContactMethodId)
      .Add(c => c.ValueChanged, (int? v) => model.ContactMethodId = v)
      .Add(c => c.ValueExpression, () => model.ContactMethodId));
    await cut.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(Model.ContactMethodId)), "Required");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(cut.Find("ul").ClassList.Contains("pixata-blazor-invalid"));
    Assert.AreEqual("true", cut.Find("ul").GetAttribute("aria-invalid"));
    Assert.IsTrue(cut.Find("input").ClassList.Contains("pixata-blazor-invalid"));
    cut.FindAll("input")[1].Change(true);
    Assert.AreEqual(2, model.ContactMethodId);
    CollectionAssert.AreEqual(new[] { nameof(Model.ContactMethodId) }, changed);
  }

  [TestMethod]
  public async Task FocusItemAsync_FocusesThatRadioButton() {
    using BunitContext context = PixataTestContext.Create();
    BunitJSModuleInterop module = context.JSInterop.SetupModule(ModulePath);
    module.Mode = JSRuntimeMode.Loose;
    IRenderedComponent<PixataRadioGroup<string, string>> cut = context.Render<PixataRadioGroup<string, string>>(p => p
      .Add(c => c.Data, Sizes)
      .Add(c => c.Id, "size"));
    await cut.InvokeAsync(() => cut.Instance.FocusItemAsync(2));
    JSRuntimeInvocation invocation = module.VerifyInvoke("focusSelector");
    Assert.AreEqual("#size-2", invocation.Arguments[1]);
  }
}
