using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Wizard_Tests {
  private record StepSpec(string Label, bool Disabled = false, bool Optional = false, bool? Valid = null, string? Text = null, Action<PixataStepperStepChangeEventArgs>? OnChange = null);

  private record WizardStepSpec(string Label, bool Disabled = false, bool Visible = true, object? Model = null, IEnumerable<Expression<Func<object?>>>? Fields = null, Action<PixataWizardStepChangeEventArgs>? OnChange = null);

  private class Person {
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Email { get; set; }
  }

  #region Stepper

  /// <summary>
  /// The steps as a render fragment, the way a @foreach in a page would declare them
  /// </summary>
  private static RenderFragment StepperSteps(params StepSpec[] steps) =>
    builder => {
      foreach (StepSpec step in steps) {
        builder.OpenComponent<PixataStepperStep>(0);
        builder.SetKey(step.Label);
        builder.AddAttribute(1, nameof(PixataStepperStep.Label), step.Label);
        builder.AddAttribute(2, nameof(PixataStepperStep.Disabled), step.Disabled);
        builder.AddAttribute(3, nameof(PixataStepperStep.Optional), step.Optional);
        builder.AddAttribute(4, nameof(PixataStepperStep.Valid), step.Valid);
        builder.AddAttribute(5, nameof(PixataStepperStep.Text), step.Text);
        if (step.OnChange is not null) {
          builder.AddAttribute(6, nameof(PixataStepperStep.OnChange), EventCallback.Factory.Create(new object(), step.OnChange));
        }
        builder.CloseComponent();
      }
    };

  private static StepSpec[] Labels(params string[] labels) =>
    [.. labels.Select(l => new StepSpec(l))];

  private static IRenderedComponent<PixataStepper> RenderStepper(BunitContext context, RenderFragment steps, int value = 0, Action<ComponentParameterCollectionBuilder<PixataStepper>>? more = null) =>
    context.Render<PixataStepper>(p => {
      p.Add(c => c.StepperSteps, steps);
      p.Add(c => c.Value, value);
      more?.Invoke(p);
    });

  private static string[] StepLabels(IRenderedComponent<PixataStepper> cut) =>
    [.. cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-text").Select(e => e.TextContent.Trim())];

  private static IReadOnlyList<IElement> StepButtons(IRenderedComponent<PixataStepper> cut) =>
    cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-link");

  private static IElement CurrentStep(IRenderedComponent<PixataStepper> cut) =>
    cut.Find(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-current");

  [TestMethod]
  public void Stepper_RendersTheStepsWithNumbersAndMarksTheCurrentOne() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("Cart", "Delivery", "Payment")), 1);
    CollectionAssert.AreEqual(new[] { "Cart", "Delivery", "Payment" }, StepLabels(cut));
    CollectionAssert.AreEqual(new[] { "1", "2", "3" }, cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-indicator-text").Select(e => e.TextContent).ToArray());
    IReadOnlyList<IElement> steps = cut.FindAll(".pixata-blazor-stepper-list > li");
    Assert.IsTrue(steps[0].ClassList.Contains("pixata-blazor-stepper-step-done"));
    Assert.IsTrue(steps[1].ClassList.Contains("pixata-blazor-stepper-step-current"));
    Assert.IsFalse(steps[2].ClassList.Contains("pixata-blazor-stepper-step-done"));
    IReadOnlyList<IElement> buttons = StepButtons(cut);
    Assert.AreEqual("step", buttons[1].GetAttribute("aria-current"));
    Assert.IsNull(buttons[0].GetAttribute("aria-current"));
    CollectionAssert.AreEqual(new[] { "-1", "0", "-1" }, buttons.Select(b => b.GetAttribute("tabindex")).ToArray());
    Assert.AreEqual("nav", cut.Find(".pixata-blazor-stepper").TagName.ToLowerInvariant());
    Assert.IsNull(cut.Find("ol").GetAttribute("role"));
  }

  [TestMethod]
  public void Stepper_ProgressLineIsFilledUpToTheCurrentStep() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b", "c")), 1);
    Assert.AreEqual("inset-inline:16.6667%;", cut.Find(".pixata-blazor-stepper-progress").GetAttribute("style"));
    Assert.AreEqual("width:50%;", cut.Find(".pixata-blazor-stepper-progress-value").GetAttribute("style"));
  }

  [TestMethod]
  public void Stepper_TextAndIcon_ReplaceTheNumber() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = context.Render<PixataStepper>(p => p
      .Add(c => c.StepperSteps, builder => {
        builder.OpenComponent<PixataStepperStep>(0);
        builder.AddAttribute(1, nameof(PixataStepperStep.Text), "A");
        builder.CloseComponent();
        builder.OpenComponent<PixataStepperStep>(2);
        builder.AddAttribute(3, nameof(PixataStepperStep.Icon), PixataSvgIcons.ChevronRight);
        builder.CloseComponent();
      }));
    IReadOnlyList<IElement> indicators = cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-indicator");
    Assert.AreEqual("A", indicators[0].TextContent.Trim());
    Assert.IsNotNull(indicators[1].QuerySelector("svg"));
    // Steps with no label have no label element
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-label").Count);
  }

  [TestMethod]
  public void Stepper_ClickingAStep_SelectsItAndRaisesValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b", "c")), more: p => p.Add(c => c.ValueChanged, (int v) => changed = v));
    StepButtons(cut)[2].Click();
    Assert.AreEqual(2, changed);
    Assert.AreEqual("step", StepButtons(cut)[2].GetAttribute("aria-current"));
    Assert.IsTrue(CurrentStep(cut).ClassList.Contains("pixata-blazor-stepper-step-last"));
  }

  [TestMethod]
  public void Stepper_ValueChangedByTheParent_SelectsThatStep() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b", "c")));
    cut.Render(p => p.Add(c => c.Value, 2));
    Assert.AreEqual("step", StepButtons(cut)[2].GetAttribute("aria-current"));
  }

  [TestMethod]
  public void Stepper_DisabledStep_CannotBeSelected() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(new StepSpec("a"), new StepSpec("b", Disabled: true)), more: p => p.Add(c => c.ValueChanged, (int v) => changed = v));
    IElement disabled = StepButtons(cut)[1];
    Assert.IsTrue(disabled.HasAttribute("disabled"));
    Assert.IsTrue(cut.FindAll(".pixata-blazor-stepper-list > li")[1].ClassList.Contains("pixata-blazor-stepper-step-disabled"));
    disabled.Click();
    Assert.IsNull(changed);
  }

  [TestMethod]
  public void Stepper_Linear_OnlyTheStepsEitherSideCanBeSelected() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b", "c", "d")), 1, p => p
      .Add(c => c.Linear, true)
      .Add(c => c.ValueChanged, (int v) => changed = v));
    Assert.IsTrue(cut.Find(".pixata-blazor-stepper").ClassList.Contains("pixata-blazor-stepper-linear"));
    IReadOnlyList<IElement> buttons = StepButtons(cut);
    Assert.IsNull(buttons[0].GetAttribute("aria-disabled"));
    Assert.IsNull(buttons[2].GetAttribute("aria-disabled"));
    Assert.AreEqual("true", buttons[3].GetAttribute("aria-disabled"));
    Assert.IsTrue(cut.FindAll(".pixata-blazor-stepper-list > li")[3].ClassList.Contains("pixata-blazor-stepper-step-unreachable"));
    buttons[3].Click();
    Assert.IsNull(changed);
    StepButtons(cut)[2].Click();
    Assert.AreEqual(2, changed);
    // Now the last step is next to the current one, so it can be reached
    Assert.IsNull(StepButtons(cut)[3].GetAttribute("aria-disabled"));
  }

  [TestMethod]
  public void Stepper_OnChangeCancelled_StaysOnTheStep() {
    using BunitContext context = PixataTestContext.Create();
    PixataStepperStepChangeEventArgs? raised = null;
    int? changed = null;
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(new StepSpec("a", OnChange: e => {
      raised = e;
      e.IsCancelled = true;
    }), new StepSpec("b")), more: p => p.Add(c => c.ValueChanged, (int v) => changed = v));
    StepButtons(cut)[1].Click();
    Assert.AreEqual(1, raised?.TargetIndex);
    Assert.IsNull(changed);
    Assert.AreEqual("step", StepButtons(cut)[0].GetAttribute("aria-current"));
  }

  [TestMethod]
  public void Stepper_ArrowKeys_MoveTheFocusButNotTheSelection() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(new StepSpec("a"), new StepSpec("b", Disabled: true), new StepSpec("c"), new StepSpec("d")), more: p => p.Add(c => c.ValueChanged, (int v) => changed = v));
    IElement list = cut.Find("ol");
    list.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    // The disabled step is skipped
    CollectionAssert.AreEqual(new[] { "-1", "-1", "0", "-1" }, StepButtons(cut).Select(b => b.GetAttribute("tabindex")).ToArray());
    Assert.IsTrue(context.JSInterop.Invocations.Any(i => i.Identifier == "focusElement"));
    cut.Find("ol").KeyDown(new KeyboardEventArgs { Key = "End" });
    Assert.AreEqual("0", StepButtons(cut)[3].GetAttribute("tabindex"));
    // There's no wrapping
    cut.Find("ol").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Assert.AreEqual("0", StepButtons(cut)[3].GetAttribute("tabindex"));
    cut.Find("ol").KeyDown(new KeyboardEventArgs { Key = "Home" });
    Assert.AreEqual("0", StepButtons(cut)[0].GetAttribute("tabindex"));
    Assert.IsNull(changed);
    Assert.IsTrue(context.JSInterop.Invocations.Any(i => i.Identifier == "preventDefaultKeys"));
  }

  [TestMethod]
  public void Stepper_Valid_ShowsTheStateInTheIndicator() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(new StepSpec("a", Valid: true), new StepSpec("b", Valid: false), new StepSpec("c")), 2);
    IReadOnlyList<IElement> steps = cut.FindAll(".pixata-blazor-stepper-list > li");
    Assert.IsTrue(steps[0].ClassList.Contains("pixata-blazor-stepper-step-success"));
    Assert.IsTrue(steps[1].ClassList.Contains("pixata-blazor-stepper-step-error"));
    Assert.AreEqual("true", StepButtons(cut)[1].GetAttribute("aria-invalid"));
    Assert.IsNull(StepButtons(cut)[0].GetAttribute("aria-invalid"));
    Assert.IsNotNull(steps[0].QuerySelector(".pixata-blazor-stepper-step-indicator svg"));
    Assert.IsNotNull(steps[1].QuerySelector(".pixata-blazor-stepper-step-indicator svg"));
    Assert.AreEqual("3", steps[2].QuerySelector(".pixata-blazor-stepper-step-indicator")!.TextContent.Trim());
  }

  [TestMethod]
  public void Stepper_Labels_ShowsNoIndicatorsAndPutsTheValidityIconInTheLabel() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(new StepSpec("a", Valid: true), new StepSpec("b")), more: p => p.Add(c => c.StepType, PixataStepperStepType.Labels));
    Assert.IsTrue(cut.Find(".pixata-blazor-stepper").ClassList.Contains("pixata-blazor-stepper-labels"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-indicator").Count);
    Assert.IsNotNull(cut.Find(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-label-icon"));
  }

  [TestMethod]
  public void Stepper_Optional_AddsTheOptionalText() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(new StepSpec("a"), new StepSpec("b", Optional: true)), more: p => p.Add(c => c.OptionalText, "(if you like)"));
    IElement optional = cut.Find(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-label-optional");
    Assert.AreEqual("(if you like)", optional.TextContent);
    Assert.IsTrue(cut.FindAll(".pixata-blazor-stepper-list > li")[1].ClassList.Contains("pixata-blazor-stepper-step-optional"));
  }

  [TestMethod]
  public void Stepper_Vertical_SetsTheClasses() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b", "c")), 1, p => p
      .Add(c => c.Orientation, PixataStepperOrientation.Vertical)
      .Add(c => c.Height, "300px"));
    IElement root = cut.Find(".pixata-blazor-stepper");
    Assert.IsTrue(root.ClassList.Contains("pixata-blazor-stepper-vertical"));
    Assert.AreEqual("height:300px;", root.GetAttribute("style"));
    Assert.IsTrue(cut.Find("ol").ClassList.Contains("pixata-blazor-stepper-list-vertical"));
    Assert.AreEqual("height:50%;", cut.Find(".pixata-blazor-stepper-progress-value").GetAttribute("style"));
  }

  [TestMethod]
  public void Stepper_Template_ReplacesTheIndicatorAndLabel() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = context.Render<PixataStepper>(p => p
      .Add(c => c.StepperSteps, builder => {
        builder.OpenComponent<PixataStepperStep>(0);
        builder.AddAttribute(1, nameof(PixataStepperStep.Label), "Ignored");
        builder.AddAttribute(2, nameof(PixataStepperStep.Template), (RenderFragment)(t => t.AddMarkupContent(0, "<b class=\"custom\">Custom</b>")));
        builder.CloseComponent();
      }));
    Assert.AreEqual("Custom", cut.Find(".pixata-blazor-stepper-list button b.custom").TextContent);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-stepper-list .pixata-blazor-stepper-step-indicator").Count);
  }

  [TestMethod]
  public void Stepper_CompactView_ShowsTheCurrentStepAndProgress() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("Cart", "Delivery", "Payment", "Confirm")), 1, p => p
      .Add(c => c.CollapsedTextFormat, "{0}/{1}")
      .Add(c => c.ValueChanged, (int v) => changed = v));
    Assert.IsTrue(cut.Find(".pixata-blazor-stepper").ClassList.Contains("pixata-blazor-stepper-collapsible"));
    IElement compact = cut.Find(".pixata-blazor-stepper-compact");
    Assert.AreEqual("2/4", compact.QuerySelector(".pixata-blazor-stepper-compact-count")!.TextContent);
    Assert.AreEqual("Delivery", compact.QuerySelector(".pixata-blazor-stepper-compact-label")!.TextContent);
    IElement progress = compact.QuerySelector("[role=progressbar]")!;
    Assert.AreEqual("2", progress.GetAttribute("aria-valuenow"));
    Assert.AreEqual("4", progress.GetAttribute("aria-valuemax"));
    Assert.AreEqual("width:50%", compact.QuerySelector(".pixata-blazor-stepper-compact-progress-value")!.GetAttribute("style"));
    cut.FindAll(".pixata-blazor-stepper-compact-button")[1].Click();
    Assert.AreEqual(2, changed);
    cut.FindAll(".pixata-blazor-stepper-compact-button")[0].Click();
    Assert.AreEqual(1, changed);
  }

  [TestMethod]
  public void Stepper_CompactButtons_AreDisabledAtTheEnds() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b")), more: p => p.Add(c => c.PreviousStepLabel, "Back"));
    IReadOnlyList<IElement> buttons = cut.FindAll(".pixata-blazor-stepper-compact-button");
    Assert.IsTrue(buttons[0].HasAttribute("disabled"));
    Assert.AreEqual("Back", buttons[0].GetAttribute("aria-label"));
    Assert.IsFalse(buttons[1].HasAttribute("disabled"));
  }

  [TestMethod]
  public void Stepper_NotCollapsible_HasNoCollapsibleClass() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b")), more: p => p.Add(c => c.Collapsible, false));
    Assert.IsFalse(cut.Find(".pixata-blazor-stepper").ClassList.Contains("pixata-blazor-stepper-collapsible"));
  }

  [TestMethod]
  public void Stepper_StepsAddedAndRemoved_AreShownInPageOrder() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "c")));
    cut.Render(p => p.Add(c => c.StepperSteps, StepperSteps(Labels("a", "b", "c"))));
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "a", "b", "c" }, StepLabels(cut)));
    cut.Render(p => p.Add(c => c.StepperSteps, StepperSteps(Labels("a", "c"))));
    CollectionAssert.AreEqual(new[] { "a", "c" }, StepLabels(cut));
    cut.Render(p => p.Add(c => c.StepperSteps, StepperSteps(Labels("x", "a", "c", "z"))));
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "x", "a", "c", "z" }, StepLabels(cut)));
  }

  [TestMethod]
  public void Stepper_RemovingTheCurrentStepAtTheEnd_SelectsTheLastStepAndRaisesValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<int> changes = [];
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a", "b", "c")), 2, p => p.Add(c => c.ValueChanged, (int v) => changes.Add(v)));
    cut.Render(p => p.Add(c => c.StepperSteps, StepperSteps(Labels("a", "b"))));
    Assert.AreEqual("b", CurrentStep(cut).QuerySelector(".pixata-blazor-stepper-step-text")!.TextContent);
    CollectionAssert.AreEqual(new[] { 1 }, changes);
  }

  /// <summary>
  /// Declares steps whose templates and content show a value worked out when the host renders, as a page might in a @foreach
  /// </summary>
  private class TemplateHost : ComponentBase {
    [Parameter]
    public bool Wizard { get; set; }

    public int Count { get; private set; }

    public void Increment() {
      Count++;
      StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) {
      string text = $"Count {Count}";
      if (Wizard) {
        builder.OpenComponent<PixataWizard>(0);
        builder.AddAttribute(1, nameof(PixataWizard.WizardSteps), (RenderFragment)(steps => {
          steps.OpenComponent<PixataWizardStep>(0);
          steps.AddAttribute(1, nameof(PixataWizardStep.StepTemplate), (RenderFragment)(t => t.AddContent(0, $"Template {text}")));
          steps.AddAttribute(2, nameof(PixataWizardStep.Content), (RenderFragment)(c => c.AddContent(0, $"Content {text}")));
          steps.CloseComponent();
        }));
        builder.CloseComponent();
      } else {
        builder.OpenComponent<PixataStepper>(0);
        builder.AddAttribute(1, nameof(PixataStepper.StepperSteps), (RenderFragment)(steps => {
          steps.OpenComponent<PixataStepperStep>(0);
          steps.AddAttribute(1, nameof(PixataStepperStep.Template), (RenderFragment)(t => t.AddContent(0, $"Template {text}")));
          steps.CloseComponent();
        }));
        builder.CloseComponent();
      }
    }
  }

  [TestMethod]
  public void Stepper_Template_ShowsWhatTheParentRenderedLast() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<TemplateHost> cut = context.Render<TemplateHost>();
    cut.InvokeAsync(cut.Instance.Increment);
    Assert.AreEqual("Template Count 1", cut.Find(".pixata-blazor-stepper-list button").TextContent.Trim());
  }

  [TestMethod]
  public void Stepper_AdditionalAttributes_AreRenderedOnTheOuterElement() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataStepper> cut = RenderStepper(context, StepperSteps(Labels("a")), more: p => p
      .AddUnmatched("aria-label", "Checkout")
      .Add(c => c.Id, "my-stepper")
      .Add(c => c.Class, "extra"));
    IElement root = cut.Find(".pixata-blazor-stepper");
    Assert.AreEqual("Checkout", root.GetAttribute("aria-label"));
    Assert.AreEqual("my-stepper", root.Id);
    Assert.IsTrue(root.ClassList.Contains("extra"));
  }

  [TestMethod]
  public void StepperStep_OutsideAStepper_Throws() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataStepperStep>(p => p.Add(c => c.Label, "Lost")));
  }

  #endregion

  #region Wizard

  /// <summary>
  /// The steps as a render fragment. Each step's content is its label, plus a validator if it has a model
  /// </summary>
  private static RenderFragment WizardSteps(params WizardStepSpec[] steps) =>
    builder => {
      foreach (WizardStepSpec step in steps) {
        builder.OpenComponent<PixataWizardStep>(0);
        builder.SetKey(step.Label);
        builder.AddAttribute(1, nameof(PixataWizardStep.Label), step.Label);
        builder.AddAttribute(2, nameof(PixataWizardStep.Disabled), step.Disabled);
        builder.AddAttribute(3, nameof(PixataWizardStep.Visible), step.Visible);
        builder.AddAttribute(4, nameof(PixataWizardStep.Model), step.Model);
        builder.AddAttribute(5, nameof(PixataWizardStep.Fields), step.Fields);
        if (step.OnChange is not null) {
          builder.AddAttribute(6, nameof(PixataWizardStep.OnChange), EventCallback.Factory.Create(new object(), step.OnChange));
        }
        builder.AddAttribute(7, nameof(PixataWizardStep.Content), (RenderFragment)(content => {
          content.OpenElement(0, "p");
          content.AddAttribute(1, "class", "step-body");
          content.AddContent(2, $"Content {step.Label}");
          content.CloseElement();
          if (step.Model is not null) {
            content.OpenComponent<DataAnnotationsValidator>(3);
            content.CloseComponent();
            content.OpenComponent<ValidationSummary>(4);
            content.CloseComponent();
          }
        }));
        builder.CloseComponent();
      }
    };

  private static WizardStepSpec[] WizardLabels(params string[] labels) =>
    [.. labels.Select(l => new WizardStepSpec(l))];

  private static IRenderedComponent<PixataWizard> RenderWizard(BunitContext context, RenderFragment steps, Action<ComponentParameterCollectionBuilder<PixataWizard>>? more = null) =>
    context.Render<PixataWizard>(p => {
      p.Add(c => c.WizardSteps, steps);
      more?.Invoke(p);
    });

  private static string Content<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.Find(".step-body").TextContent;

  private static string[] ButtonTexts<T>(IRenderedComponent<T> cut) where T : IComponent =>
    [.. cut.FindAll(".pixata-blazor-wizard-buttons-end button").Select(b => b.TextContent.Trim())];

  private static void Click<T>(IRenderedComponent<T> cut, string text) where T : IComponent =>
    cut.FindAll(".pixata-blazor-wizard-buttons-end button").Single(b => b.TextContent.Trim() == text).Click();

  private static IReadOnlyList<IElement> WizardStepButtons<T>(IRenderedComponent<T> cut) where T : IComponent =>
    cut.FindAll(".pixata-blazor-wizard-stepper .pixata-blazor-stepper-list .pixata-blazor-stepper-step-link");

  [TestMethod]
  public void Wizard_ShowsTheFirstStepWithTheStepperAndButtons() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two", "Three")));
    Assert.AreEqual("Content One", Content(cut));
    Assert.AreEqual("Step 1 of 3", cut.Find(".pixata-blazor-wizard-pager").TextContent);
    CollectionAssert.AreEqual(new[] { "Next" }, ButtonTexts(cut));
    IReadOnlyList<IElement> steps = WizardStepButtons(cut);
    Assert.AreEqual(3, steps.Count);
    Assert.AreEqual("tablist", cut.Find(".pixata-blazor-wizard-stepper ol").GetAttribute("role"));
    Assert.AreEqual("tab", steps[0].GetAttribute("role"));
    Assert.AreEqual("true", steps[0].GetAttribute("aria-selected"));
    IElement panel = cut.Find("[role=tabpanel]");
    Assert.AreEqual(panel.Id, steps[0].GetAttribute("aria-controls"));
    Assert.IsTrue(cut.Find(".pixata-blazor-wizard").ClassList.Contains("pixata-blazor-wizard-top"));
  }

  [TestMethod]
  public void Wizard_NextAndPrevious_MoveBetweenStepsAndRaiseValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two", "Three")), p => p.Add(c => c.ValueChanged, (int v) => changed = v));
    Click(cut, "Next");
    Assert.AreEqual(1, changed);
    Assert.AreEqual("Content Two", Content(cut));
    CollectionAssert.AreEqual(new[] { "Previous", "Next" }, ButtonTexts(cut));
    Click(cut, "Next");
    Assert.AreEqual("Content Three", Content(cut));
    CollectionAssert.AreEqual(new[] { "Previous", "Done" }, ButtonTexts(cut));
    Click(cut, "Previous");
    Assert.AreEqual(1, changed);
    Assert.AreEqual("Content Two", Content(cut));
  }

  [TestMethod]
  public void Wizard_ContentAndStepTemplate_ShowWhatTheParentRenderedLast() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<TemplateHost> cut = context.Render<TemplateHost>(p => p.Add(c => c.Wizard, true));
    Assert.AreEqual("Content Count 0", cut.Find(".pixata-blazor-wizard-content").TextContent.Trim());
    cut.InvokeAsync(cut.Instance.Increment);
    Assert.AreEqual("Content Count 1", cut.Find(".pixata-blazor-wizard-content").TextContent.Trim());
    Assert.AreEqual("Template Count 1", cut.Find(".pixata-blazor-wizard-stepper .pixata-blazor-stepper-list button").TextContent.Trim());
  }

  [TestMethod]
  public void Wizard_Done_RaisesOnFinish() {
    using BunitContext context = PixataTestContext.Create();
    int finished = 0;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two")), p => p
      .Add(c => c.Value, 1)
      .Add(c => c.DoneText, "Finish")
      .Add(c => c.OnFinish, () => finished++));
    Click(cut, "Finish");
    Assert.AreEqual(1, finished);
  }

  [TestMethod]
  public void Wizard_ClickingAStepInTheStepper_MovesToIt() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two", "Three")), p => p.Add(c => c.ValueChanged, (int v) => changed = v));
    WizardStepButtons(cut)[2].Click();
    Assert.AreEqual(2, changed);
    Assert.AreEqual("Content Three", Content(cut));
    Assert.AreEqual("Step 3 of 3", cut.Find(".pixata-blazor-wizard-pager").TextContent);
  }

  [TestMethod]
  public void Wizard_OnChangeCancelled_StaysOnTheStep() {
    using BunitContext context = PixataTestContext.Create();
    List<int> targets = [];
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One", OnChange: e => {
      targets.Add(e.TargetIndex);
      e.IsCancelled = true;
    }), new WizardStepSpec("Two"), new WizardStepSpec("Three")));
    Click(cut, "Next");
    WizardStepButtons(cut)[2].Click();
    CollectionAssert.AreEqual(new[] { 1, 2 }, targets);
    Assert.AreEqual("Content One", Content(cut));
  }

  [TestMethod]
  public void Wizard_DisabledStep_IsSkippedByNext() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Disabled: true), new WizardStepSpec("Three")));
    Click(cut, "Next");
    Assert.AreEqual("Content Three", Content(cut));
    Assert.IsTrue(WizardStepButtons(cut)[1].HasAttribute("disabled"));
  }

  [TestMethod]
  public void Wizard_HiddenSteps_AreLeftOutButKeepTheirIndexes() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Visible: false), new WizardStepSpec("Three")), p => p
      .Add(c => c.ValueChanged, (int v) => changed = v));
    Assert.AreEqual(2, WizardStepButtons(cut).Count);
    Assert.AreEqual("Step 1 of 2", cut.Find(".pixata-blazor-wizard-pager").TextContent);
    Click(cut, "Next");
    Assert.AreEqual(2, changed);
    Assert.AreEqual("Content Three", Content(cut));
    Assert.AreEqual(2, cut.Instance.CurrentStepIndex);
    Assert.AreEqual(2, cut.Instance.StepCount);
  }

  [TestMethod]
  public void Wizard_ValueOfAHiddenStep_ShowsTheNextVisibleStepIfThereIsNoPreviousOne() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One", Visible: false), new WizardStepSpec("Two"), new WizardStepSpec("Three")), p => p
      .Add(c => c.Value, 0)
      .Add(c => c.ValueChanged, (int v) => changed = v));
    Assert.AreEqual("Content Two", Content(cut));
    Assert.AreEqual(1, changed);
    cut.Render(p => p.Add(c => c.Value, 2));
    Assert.AreEqual("Content Three", Content(cut));
  }

  [TestMethod]
  public void Wizard_HidingTheCurrentStep_ShowsThePreviousOneAndRaisesValueChanged() {
    using BunitContext context = PixataTestContext.Create();
    int? changed = null;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two", "Three")), p => p
      .Add(c => c.Value, 1)
      .Add(c => c.ValueChanged, (int v) => changed = v));
    cut.Render(p => p.Add(c => c.WizardSteps, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Visible: false), new WizardStepSpec("Three"))));
    Assert.AreEqual("Content One", Content(cut));
    Assert.AreEqual(0, changed);
  }

  [TestMethod]
  public void Wizard_FocusMovesSensiblyWhenTheStepChanges() {
    using BunitContext context = PixataTestContext.Create();
    Person person = new() { Name = "Jim", Email = "jim@example.com" };
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One", Model: person), new WizardStepSpec("Two"), new WizardStepSpec("Three")));
    // Enter in a step's form moves into the next step's content, as the form has gone
    cut.Find("form").Submit();
    Assert.AreEqual(1, context.JSInterop.Invocations.Count(i => i.Identifier == "focusFirst"));
    // Next taking the user to the last step focuses Done, which replaces it
    int focusCalls = context.JSInterop.Invocations.Count(i => i.Identifier == "focusElement");
    Click(cut, "Next");
    Assert.AreEqual("Content Three", Content(cut));
    Assert.AreEqual(focusCalls + 1, context.JSInterop.Invocations.Count(i => i.Identifier == "focusElement"));
    // Previous from the last step leaves the focus alone, as the button is still there
    Click(cut, "Previous");
    Assert.AreEqual(focusCalls + 1, context.JSInterop.Invocations.Count(i => i.Identifier == "focusElement"));
  }

  [TestMethod]
  public void Wizard_StepWithAnInvalidModel_CannotMoveOn() {
    using BunitContext context = PixataTestContext.Create();
    Person person = new();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One", Model: person), new WizardStepSpec("Two")));
    Assert.IsNotNull(cut.Find("form.pixata-blazor-wizard-form"));
    Click(cut, "Next");
    Assert.AreEqual("Content One", Content(cut));
    Assert.AreEqual(2, cut.FindAll(".validation-errors li").Count);
    Assert.AreEqual("true", WizardStepButtons(cut)[0].GetAttribute("aria-invalid"));
    // Going via the stepper is checked too
    WizardStepButtons(cut)[1].Click();
    Assert.AreEqual("Content One", Content(cut));
    person.Name = "Jim";
    person.Email = "jim@example.com";
    Click(cut, "Next");
    Assert.AreEqual("Content Two", Content(cut));
    Assert.IsTrue(cut.FindAll(".pixata-blazor-wizard-stepper .pixata-blazor-stepper-list > li")[0].ClassList.Contains("pixata-blazor-stepper-step-success"));
  }

  [TestMethod]
  public void Wizard_StepWithAnInvalidModel_CanGoBack() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Model: new Person())), p => p.Add(c => c.Value, 1));
    Click(cut, "Previous");
    Assert.AreEqual("Content One", Content(cut));
  }

  [TestMethod]
  public void Wizard_SubmittingAStepsForm_MovesOn() {
    using BunitContext context = PixataTestContext.Create();
    int finished = 0;
    Person person = new() { Name = "Jim", Email = "jim@example.com" };
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One", Model: person), new WizardStepSpec("Two", Model: person)), p => p.Add(c => c.OnFinish, () => finished++));
    cut.Find("form").Submit();
    Assert.AreEqual("Content Two", Content(cut));
    cut.Find("form").Submit();
    Assert.AreEqual(1, finished);
  }

  [TestMethod]
  public void Wizard_StepWithAnEditContext_IsValidatedAgainstIt() {
    using BunitContext context = PixataTestContext.Create();
    EditContext editContext = new(new Person());
    IRenderedComponent<PixataWizard> cut = context.Render<PixataWizard>(p => p
      .Add(c => c.WizardSteps, builder => {
        builder.OpenComponent<PixataWizardStep>(0);
        builder.AddAttribute(1, nameof(PixataWizardStep.EditContext), editContext);
        builder.AddAttribute(2, nameof(PixataWizardStep.Content), (RenderFragment)(content => {
          content.OpenComponent<DataAnnotationsValidator>(0);
          content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<PixataWizardStep>(3);
        builder.CloseComponent();
      }));
    Click(cut, "Next");
    Assert.IsTrue(editContext.GetValidationMessages().Any());
    Assert.AreEqual("Step 1 of 2", cut.Find(".pixata-blazor-wizard-pager").TextContent);
  }

  [TestMethod]
  public void Wizard_StepsForm_HasASubmitButtonSoEnterWorksWithSeveralTextBoxes() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One", Model: new Person())));
    IElement submit = cut.Find("form.pixata-blazor-wizard-form button[type=submit]");
    Assert.AreEqual("-1", submit.GetAttribute("tabindex"));
    Assert.AreEqual("true", submit.GetAttribute("aria-hidden"));
  }

  [TestMethod]
  public void Wizard_FixingAnInvalidStep_UpdatesItsIndicator() {
    using BunitContext context = PixataTestContext.Create();
    Person person = new();
    EditContext editContext = new(person);
    IRenderedComponent<PixataWizard> cut = context.Render<PixataWizard>(p => p
      .Add(c => c.WizardSteps, builder => {
        builder.OpenComponent<PixataWizardStep>(0);
        builder.AddAttribute(1, nameof(PixataWizardStep.EditContext), editContext);
        builder.AddAttribute(2, nameof(PixataWizardStep.Content), (RenderFragment)(content => {
          content.OpenComponent<DataAnnotationsValidator>(0);
          content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.OpenComponent<PixataWizardStep>(3);
        builder.CloseComponent();
      }));
    Click(cut, "Next");
    Assert.AreEqual("true", WizardStepButtons(cut)[0].GetAttribute("aria-invalid"));
    person.Name = "Jim";
    cut.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(Person.Name))));
    // The email is still missing
    Assert.AreEqual("true", WizardStepButtons(cut)[0].GetAttribute("aria-invalid"));
    person.Email = "jim@example.com";
    cut.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(Person.Email))));
    Assert.IsNull(WizardStepButtons(cut)[0].GetAttribute("aria-invalid"));
    Assert.IsTrue(cut.FindAll(".pixata-blazor-wizard-stepper .pixata-blazor-stepper-list > li")[0].ClassList.Contains("pixata-blazor-stepper-step-success"));
  }

  [TestMethod]
  public void Wizard_LeavingAnInvalidStepBackwards_KeepsItInvalid() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Model: new Person()), new WizardStepSpec("Three")), p => p
      .Add(c => c.Value, 1));
    Click(cut, "Next");
    Assert.AreEqual("Content Two", Content(cut));
    // Going back is allowed, and the step's validator going (which clears its messages) doesn't make it valid
    Click(cut, "Previous");
    Assert.AreEqual("Content One", Content(cut));
    Assert.AreEqual("true", WizardStepButtons(cut)[1].GetAttribute("aria-invalid"));
  }

  [TestMethod]
  public void Wizard_Finish_GoesToAStepThatHasNotBeenValidated() {
    using BunitContext context = PixataTestContext.Create();
    int finished = 0;
    Person person = new();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Model: person), new WizardStepSpec("Three")), p => p
      .Add(c => c.OnFinish, () => finished++));
    // Skip step two using the stepper, which only validates the step being left
    WizardStepButtons(cut)[2].Click();
    Assert.AreEqual("Content Three", Content(cut));
    Click(cut, "Done");
    Assert.AreEqual(0, finished);
    Assert.AreEqual("Content Two", Content(cut));
    cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindAll(".validation-errors li").Count));
    person.Name = "Jim";
    person.Email = "jim@example.com";
    Click(cut, "Next");
    Click(cut, "Done");
    Assert.AreEqual(1, finished);
  }

  [TestMethod]
  public void Wizard_Finish_LeavesOutDisabledSteps() {
    using BunitContext context = PixataTestContext.Create();
    int finished = 0;
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(new WizardStepSpec("One"), new WizardStepSpec("Two", Disabled: true, Model: new Person()), new WizardStepSpec("Three")), p => p
      .Add(c => c.OnFinish, () => finished++));
    Click(cut, "Next");
    Assert.AreEqual("Content Three", Content(cut));
    Click(cut, "Done");
    Assert.AreEqual("Content Three", Content(cut));
    Assert.AreEqual(1, finished);
  }

  [TestMethod]
  public void Wizard_InsideAnEditForm_ValidatesEachStepsFields() {
    using BunitContext context = PixataTestContext.Create();
    Person person = new();
    int finished = 0;
    Expression<Func<object?>> name = () => person.Name;
    Expression<Func<object?>> email = () => person.Email;
    IRenderedComponent<EditForm> cut = context.Render<EditForm>(p => p
      .Add(c => c.Model, person)
      .Add(c => c.ChildContent, (EditContext _) => builder => {
        builder.OpenComponent<DataAnnotationsValidator>(0);
        builder.CloseComponent();
        builder.OpenComponent<PixataWizard>(1);
        builder.AddAttribute(2, nameof(PixataWizard.WizardSteps), WizardSteps(new WizardStepSpec("One", Fields: [name]), new WizardStepSpec("Two", Fields: [email])));
        builder.AddAttribute(3, nameof(PixataWizard.OnFinish), EventCallback.Factory.Create(new object(), () => finished++));
        builder.CloseComponent();
      }));
    // Steps that list fields don't get forms of their own
    Assert.AreEqual(1, cut.FindAll("form").Count);
    Click(cut, "Next");
    Assert.AreEqual("Content One", Content(cut));
    Assert.AreEqual("true", WizardStepButtons(cut)[0].GetAttribute("aria-invalid"));
    person.Name = "Jim";
    // The email is still missing, but that is on the next step, so doesn't stop the user
    Click(cut, "Next");
    Assert.AreEqual("Content Two", Content(cut));
    Click(cut, "Done");
    Assert.AreEqual(0, finished);
    Assert.AreEqual("true", WizardStepButtons(cut)[1].GetAttribute("aria-invalid"));
    person.Email = "jim@example.com";
    Click(cut, "Done");
    Assert.AreEqual(1, finished);
  }

  [TestMethod]
  public void Wizard_WizardButtons_ReplacesTheButtons() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two")), p => p
      .Add(c => c.WizardButtons, (PixataWizard wizard) => builder => {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "class", "custom");
        builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(new object(), wizard.Next));
        builder.AddContent(3, wizard.IsLastStep ? "Last" : "Onward");
        builder.CloseElement();
      }));
    CollectionAssert.AreEqual(new[] { "Onward" }, ButtonTexts(cut));
    cut.Find("button.custom").Click();
    Assert.AreEqual("Content Two", Content(cut));
    CollectionAssert.AreEqual(new[] { "Last" }, ButtonTexts(cut));
  }

  [TestMethod]
  public void Wizard_ShowPagerFalse_HidesThePager() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two")), p => p.Add(c => c.ShowPager, false));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-wizard-pager").Count);
  }

  [TestMethod]
  public void Wizard_StepperPosition_SetsTheClassesAndStepperOrientation() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two")), p => p.Add(c => c.StepperPosition, PixataWizardStepperPosition.Left));
    IElement root = cut.Find(".pixata-blazor-wizard");
    Assert.IsTrue(root.ClassList.Contains("pixata-blazor-wizard-left"));
    Assert.IsTrue(root.ClassList.Contains("pixata-blazor-wizard-vertical"));
    Assert.IsTrue(cut.Find(".pixata-blazor-wizard-stepper").ClassList.Contains("pixata-blazor-stepper-vertical"));
    Assert.AreEqual("vertical", cut.Find(".pixata-blazor-wizard-stepper ol").GetAttribute("aria-orientation"));
  }

  [TestMethod]
  public void Wizard_StepperSettings_ConfigureTheStepper() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two", "Three")), p => p
      .Add(c => c.WizardSettings, builder => {
        builder.OpenComponent<PixataWizardStepperSettings>(0);
        builder.AddAttribute(1, nameof(PixataWizardStepperSettings.Linear), true);
        builder.AddAttribute(2, nameof(PixataWizardStepperSettings.StepType), PixataStepperStepType.Labels);
        builder.CloseComponent();
      }));
    IElement stepper = cut.Find(".pixata-blazor-wizard-stepper");
    Assert.IsTrue(stepper.ClassList.Contains("pixata-blazor-stepper-linear"));
    Assert.IsTrue(stepper.ClassList.Contains("pixata-blazor-stepper-labels"));
    WizardStepButtons(cut)[2].Click();
    Assert.AreEqual("Content One", Content(cut));
  }

  [TestMethod]
  public void Wizard_StepInsertedInTheMiddle_IsPutInPageOrder() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Three")));
    cut.Render(p => p.Add(c => c.WizardSteps, WizardSteps(WizardLabels("One", "Two", "Three"))));
    cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "One", "Two", "Three" },
      cut.FindAll(".pixata-blazor-wizard-stepper .pixata-blazor-stepper-list .pixata-blazor-stepper-step-text").Select(e => e.TextContent).ToArray()));
    Assert.AreEqual("Content One", Content(cut));
    Click(cut, "Next");
    Assert.AreEqual("Content Two", Content(cut));
  }

  [TestMethod]
  public async Task Wizard_PublicMethods_MoveBetweenSteps() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = RenderWizard(context, WizardSteps(WizardLabels("One", "Two", "Three")));
    Assert.IsTrue(cut.Instance.IsFirstStep);
    bool moved = false;
    await cut.InvokeAsync(async () => moved = await cut.Instance.ChangeStep(2));
    Assert.IsTrue(moved);
    Assert.IsTrue(cut.Instance.IsLastStep);
    Assert.AreEqual("Content Three", Content(cut));
    await cut.InvokeAsync(async () => moved = await cut.Instance.ChangeStep(7));
    Assert.IsFalse(moved);
    await cut.InvokeAsync(cut.Instance.Previous);
    Assert.AreEqual("Content Two", Content(cut));
  }

  [TestMethod]
  public void WizardStep_OutsideAWizard_Throws() {
    using BunitContext context = PixataTestContext.Create();
    Assert.ThrowsException<InvalidOperationException>(() => context.Render<PixataWizardStep>(p => p.Add(c => c.Label, "Lost")));
  }

  [TestMethod]
  public void StepperInsideAStep_IsNotTreatedAsTheWizardsStepper() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataWizard> cut = context.Render<PixataWizard>(p => p
      .Add(c => c.WizardSteps, builder => {
        builder.OpenComponent<PixataWizardStep>(0);
        builder.AddAttribute(1, nameof(PixataWizardStep.Content), (RenderFragment)(content => {
          content.OpenComponent<PixataStepper>(0);
          content.AddAttribute(1, nameof(PixataStepper.Class), "inner");
          content.AddAttribute(2, nameof(PixataStepper.StepperSteps), StepperSteps(Labels("a", "b")));
          content.CloseComponent();
        }));
        builder.CloseComponent();
      }));
    Assert.IsNull(cut.Find(".inner ol").GetAttribute("role"));
    Assert.AreEqual("tablist", cut.Find(".pixata-blazor-wizard-stepper ol").GetAttribute("role"));
  }

  #endregion
}
