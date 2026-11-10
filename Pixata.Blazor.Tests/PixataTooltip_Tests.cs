using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataTooltip_Tests {
  private BunitContext _ctx = null!;
  private BunitJSModuleInterop _module = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _module = _ctx.JSInterop.SetupModule(PixataTooltip.ModulePath);
    _module.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  private IRenderedComponent<PixataTooltip> Render(Action<ComponentParameterCollectionBuilder<PixataTooltip>> parameters) =>
    _ctx.Render(parameters);

  private static void ChildContent(ComponentParameterCollectionBuilder<PixataTooltip> p) =>
    p.AddChildContent("<button id=\"save\" title=\"Save the invoice\">Save</button>");

  // The options the component sent the script when it was first rendered
  private JsonElement AttachedOptions() =>
    JsonDocument.Parse((string)_module.Invocations["attach"].Single().Arguments[0]!).RootElement;

  [TestMethod]
  public void WithoutATargetSelectorOrChildContent_Throws() {
    InvalidOperationException ex = Assert.ThrowsException<InvalidOperationException>(() => Render(_ => { }));

    StringAssert.Contains(ex.Message, "TargetSelector");
  }

  [TestMethod]
  public void ChildContent_IsWrappedSoTheScriptCanFindIt_AndTheTooltipIsntRenderedUntilItsShown() {
    IRenderedComponent<PixataTooltip> tooltip = Render(ChildContent);

    AngleSharp.Dom.IElement targets = tooltip.Find(".pixata-blazor-tooltip-targets");
    Assert.IsNotNull(targets.QuerySelector("#save"));
    Assert.AreEqual(0, tooltip.FindAll(".pixata-blazor-tooltip").Count);
    Assert.IsFalse(tooltip.Instance.IsShown);
    Assert.AreEqual(targets.Id, AttachedOptions().GetProperty("targetsId").GetString());
  }

  [TestMethod]
  public void WithOnlyATargetSelector_NothingIsRenderedUntilItsShown() {
    IRenderedComponent<PixataTooltip> tooltip = Render(p => p.Add(c => c.TargetSelector, ".overdue"));

    Assert.AreEqual("", tooltip.Markup.Trim());
    JsonElement options = AttachedOptions();
    Assert.AreEqual(JsonValueKind.Null, options.GetProperty("targetsId").ValueKind);
    Assert.AreEqual(".overdue", options.GetProperty("selector").GetString());
  }

  [TestMethod]
  public void Options_TellTheScriptHowToShowTheTooltip() {
    Render(p => p
      .Add(c => c.TargetSelector, "[title]")
      .Add(c => c.Position, PixataTooltipPosition.Right)
      .Add(c => c.ShowOn, PixataTooltipShowEvent.Click)
      .Add(c => c.ShowDelay, 250)
      .Add(c => c.HideDelay, -5)
      .Add(c => c.Id, "help"));

    JsonElement options = AttachedOptions();
    Assert.AreEqual("right", options.GetProperty("position").GetString());
    Assert.AreEqual("click", options.GetProperty("showOn").GetString());
    Assert.AreEqual(250, options.GetProperty("showDelay").GetInt32());
    Assert.AreEqual(0, options.GetProperty("hideDelay").GetInt32());
    Assert.AreEqual("help", options.GetProperty("tooltipId").GetString());
    Assert.IsFalse(options.GetProperty("content").GetBoolean());
  }

  [TestMethod]
  public void Options_SayWhetherTheTooltipHasContentOfItsOwn() {
    Render(p => p.Add(c => c.TargetSelector, ".a").Add(c => c.Text, "Hello"));

    Assert.IsTrue(AttachedOptions().GetProperty("content").GetBoolean());
  }

  [TestMethod]
  public void ChangedParameters_AreSentToTheScript() {
    IRenderedComponent<PixataTooltip> tooltip = Render(p => p.Add(c => c.TargetSelector, ".a"));

    tooltip.Render(p => p.Add(c => c.Position, PixataTooltipPosition.Bottom));

    JsonElement updated = JsonDocument.Parse((string)_module.Invocations["update"].Single().Arguments[0]!).RootElement;
    Assert.AreEqual("bottom", updated.GetProperty("position").GetString());
    Assert.AreEqual(AttachedOptions().GetProperty("key").GetString(), updated.GetProperty("key").GetString());
  }

  [TestMethod]
  public void ShowFor_ShowsTheTargetsTitle_AndAsksTheScriptToPlaceIt() {
    IRenderedComponent<PixataTooltip> tooltip = Render(ChildContent);

    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor("save", "Save the invoice", []));

    AngleSharp.Dom.IElement tip = tooltip.Find(".pixata-blazor-tooltip");
    Assert.AreEqual("tooltip", tip.GetAttribute("role"));
    Assert.AreEqual(AttachedOptions().GetProperty("tooltipId").GetString(), tip.Id);
    Assert.AreEqual("Save the invoice", tooltip.Find(".pixata-blazor-tooltip-content").TextContent.Trim());
    Assert.IsTrue(tooltip.Instance.IsShown);
    Assert.AreEqual(AttachedOptions().GetProperty("key").GetString(), _module.Invocations["place"].Single().Arguments[0]);
  }

  [TestMethod]
  public void Text_IsShownInsteadOfTheTitle() {
    IRenderedComponent<PixataTooltip> tooltip = Render(p => {
      ChildContent(p);
      p.Add(c => c.Text, "Saves it");
    });

    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor("save", "Save the invoice", []));

    Assert.AreEqual("Saves it", tooltip.Find(".pixata-blazor-tooltip-content").TextContent.Trim());
  }

  [TestMethod]
  public void Template_IsGivenTheTargetsIdTitleAndDataAttributes() {
    IRenderedComponent<PixataTooltip> tooltip = Render(p => p
      .Add(c => c.TargetSelector, ".invoice")
      .Add(c => c.Template, (RenderFragment<PixataTooltipTemplateContext>)(context => builder => {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "detail");
        builder.AddContent(2, $"{context.TargetId}|{context.Title}|{context.DataAttributes["invoice-id"]}");
        builder.CloseElement();
      })));

    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor("inv-7", "Overdue", new Dictionary<string, string> { ["invoice-id"] = "7" }));

    Assert.AreEqual("inv-7|Overdue|7", tooltip.Find(".detail").TextContent);
  }

  [TestMethod]
  public void ShowFor_WithoutAnId_GivesANullTargetId() {
    PixataTooltipTemplateContext? shown = null;
    IRenderedComponent<PixataTooltip> tooltip = Render(p => p
      .Add(c => c.TargetSelector, ".a")
      .Add(c => c.Template, (RenderFragment<PixataTooltipTemplateContext>)(context => _ => shown = context)));

    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor("", null, null));

    Assert.IsNull(shown!.TargetId);
    Assert.IsNull(shown.Title);
    Assert.AreEqual(0, shown.DataAttributes.Count);
  }

  [TestMethod]
  public void WidthHeightAndClass_AreUsed() {
    IRenderedComponent<PixataTooltip> tooltip = Render(p => p
      .Add(c => c.TargetSelector, ".a")
      .Add(c => c.Text, "Hi")
      .Add(c => c.Width, "12rem")
      .Add(c => c.Height, "80px")
      .Add(c => c.Class, "warning"));

    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor(null, null, null));

    Assert.AreEqual("pixata-blazor-tooltip warning", tooltip.Find("[role=tooltip]").ClassName);
    Assert.AreEqual("width: 12rem; height: 80px", tooltip.Find(".pixata-blazor-tooltip-content").GetAttribute("style"));
  }

  [TestMethod]
  public void Hidden_RemovesTheTooltip() {
    IRenderedComponent<PixataTooltip> tooltip = Render(ChildContent);
    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor("save", "Save the invoice", []));

    tooltip.InvokeAsync(() => tooltip.Instance.Hidden());

    Assert.AreEqual(0, tooltip.FindAll(".pixata-blazor-tooltip").Count);
    Assert.IsFalse(tooltip.Instance.IsShown);
  }

  [TestMethod]
  public void Hide_TellsTheScript_AndRemovesTheTooltip() {
    IRenderedComponent<PixataTooltip> tooltip = Render(ChildContent);
    tooltip.InvokeAsync(() => tooltip.Instance.ShowFor("save", "Save the invoice", []));

    tooltip.InvokeAsync(() => tooltip.Instance.Hide());

    Assert.AreEqual(1, _module.Invocations["hide"].Count);
    Assert.AreEqual(0, tooltip.FindAll(".pixata-blazor-tooltip").Count);
  }

  [TestMethod]
  public void Disposing_DetachesTheScript() {
    IRenderedComponent<PixataTooltip> tooltip = Render(ChildContent);
    string key = AttachedOptions().GetProperty("key").GetString()!;

    tooltip.InvokeAsync(() => tooltip.Instance.DisposeAsync().AsTask());

    Assert.AreEqual(key, _module.Invocations["detach"].Single().Arguments[0]);
  }
}
