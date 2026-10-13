using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Pixata.Blazor.Components;

/// <summary>
/// Takes the user through a series of steps, one at a time, with a stepper showing where they are and buttons to move on and
/// back. Declare the steps with <see cref="PixataWizardStep"/> components inside <see cref="WizardSteps"/>
/// </summary>
public partial class PixataWizard : IDisposable {
  /// <summary>
  /// The name the wizard cascades itself under to its own stepper, so a stepper the wizard renders knows it is part of a wizard
  /// (and one inside a step's content doesn't think so)
  /// </summary>
  internal const string StepperCascadeName = "PixataWizardStepper";

  [CascadingParameter]
  private EditContext? CascadedEditContext { get; set; }

  /// <summary>
  /// The <see cref="PixataWizardStep"/> components
  /// </summary>
  [Parameter]
  public RenderFragment? WizardSteps { get; set; }

  /// <summary>
  /// Replaces the Previous, Next and Done buttons. The context is the wizard, whose <see cref="Previous"/>, <see cref="Next"/>,
  /// <see cref="Finish"/>, <see cref="IsFirstStep"/> and <see cref="IsLastStep"/> members your buttons can use
  /// </summary>
  [Parameter]
  public RenderFragment<PixataWizard>? WizardButtons { get; set; }

  /// <summary>
  /// Holds a <see cref="PixataWizardStepperSettings"/>, to configure the stepper
  /// </summary>
  [Parameter]
  public RenderFragment? WizardSettings { get; set; }

  /// <summary>
  /// Whether "Step 2 of 4" (see <see cref="PagerTextFormat"/>) is shown next to the buttons. True by default
  /// </summary>
  [Parameter]
  public bool ShowPager { get; set; } = true;

  /// <summary>
  /// Where the stepper goes, relative to the current step. <see cref="PixataWizardStepperPosition.Top"/> by default
  /// </summary>
  [Parameter]
  public PixataWizardStepperPosition StepperPosition { get; set; } = PixataWizardStepperPosition.Top;

  /// <summary>
  /// The index of the current step, starting at 0. Use <c>@bind-Value</c> for two-way binding. Steps that aren't
  /// <see cref="PixataWizardStep.Visible"/> still count, so a step's index doesn't change when an earlier step is hidden
  /// </summary>
  [Parameter]
  public int Value { get; set; }

  /// <summary>
  /// Raised when the user moves to another step
  /// </summary>
  [Parameter]
  public EventCallback<int> ValueChanged { get; set; }

  /// <summary>
  /// Raised when the user clicks Done on the last step, once every step that has validation is valid
  /// </summary>
  [Parameter]
  public EventCallback OnFinish { get; set; }

  /// <summary>
  /// The text of the Previous button
  /// </summary>
  [Parameter]
  public string PreviousText { get; set; } = "Previous";

  /// <summary>
  /// The text of the Next button
  /// </summary>
  [Parameter]
  public string NextText { get; set; } = "Next";

  /// <summary>
  /// The text of the Done button
  /// </summary>
  [Parameter]
  public string DoneText { get; set; } = "Done";

  /// <summary>
  /// The pager's text, where <c>{0}</c> is the current step's number and <c>{1}</c> the number of steps. <c>"Step {0} of {1}"</c> by default
  /// </summary>
  [Parameter]
  public string PagerTextFormat { get; set; } = "Step {0} of {1}";

  /// <summary>
  /// What follows the label of an <see cref="PixataWizardStep.Optional"/> step in the stepper. <c>"(Optional)"</c> by default
  /// </summary>
  [Parameter]
  public string OptionalText { get; set; } = "(Optional)";

  /// <summary>
  /// The width of the wizard, as a CSS length
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The height of the wizard, as a CSS length. The step's content scrolls if it doesn't fit
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Extra CSS classes for the wizard's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Any other attributes are rendered on the wizard's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  private readonly List<PixataWizardStep> _steps = [];
  private readonly List<PixataWizardStep> _newSteps = [];
  private readonly Dictionary<object, EditContext> _modelContexts = new(ReferenceEqualityComparer.Instance);
  private readonly HashSet<EditContext> _watchedContexts = [];
  private readonly string _panelId = $"pixata-blazor-wizard-{Guid.NewGuid():N}-step";
  private PixataWizardStepperSettings? _stepperSettings;
  private bool _linear;
  private PixataStepperStepType _stepType = PixataStepperStepType.Steps;
  private int _value;
  private int? _previousValueParameter;
  private bool _hasRendered;
  private bool _rebuilding;
  private int _generation;
  private bool _validateCurrentStepAfterRender;
  private ElementReference _panel;
  private ElementReference _content;
  private ElementReference _nextButton;
  private ElementReference _doneButton;
  private FocusTarget _focusAfterRender;
  private bool _disposed;

  /// <summary>
  /// The index of the current step, starting at 0
  /// </summary>
  public int CurrentStepIndex =>
    CurrentStep is { } step ? _steps.IndexOf(step) : -1;

  /// <summary>
  /// True if there is no step before the current one that the user can go back to
  /// </summary>
  public bool IsFirstStep =>
    SiblingStep(-1) is null;

  /// <summary>
  /// True if there is no step after the current one that the user can move on to, so Done is shown rather than Next
  /// </summary>
  public bool IsLastStep =>
    SiblingStep(1) is null;

  /// <summary>
  /// The number of visible steps
  /// </summary>
  public int StepCount =>
    _steps.Count(s => s.Visible);

  private bool IsVertical =>
    StepperPosition is PixataWizardStepperPosition.Left or PixataWizardStepperPosition.Right;

  private bool Linear =>
    _linear;

  private PixataStepperStepType StepType =>
    _stepType;

  private string PanelId =>
    Id is null ? _panelId : $"{Id}-step";

  private List<PixataWizardStep> VisibleSteps =>
    [.. _steps.Where(s => s.Visible)];

  /// <summary>
  /// The step whose index is <see cref="Value"/>. If that one is hidden (or doesn't exist), the nearest visible step before it,
  /// or failing that after it
  /// </summary>
  private PixataWizardStep? CurrentStep {
    get {
      if (_value >= 0 && _value < _steps.Count && _steps[_value].Visible) {
        return _steps[_value];
      }
      int from = Math.Clamp(_value, 0, _steps.Count);
      return _steps.Take(from).LastOrDefault(s => s.Visible) ?? _steps.Skip(from).FirstOrDefault(s => s.Visible);
    }
  }

  private string PagerText {
    get {
      List<PixataWizardStep> visible = VisibleSteps;
      int position = CurrentStep is { } step ? visible.IndexOf(step) + 1 : 0;
      return string.Format(CultureInfo.CurrentCulture, PagerTextFormat, position, visible.Count);
    }
  }

  private string RootClass =>
    PixataCss.Classes("pixata-blazor-wizard",
      IsVertical ? "pixata-blazor-wizard-vertical" : "pixata-blazor-wizard-horizontal",
      $"pixata-blazor-wizard-{StepperPosition.ToString().ToLowerInvariant()}",
      Class);

  private string? Style {
    get {
      string style = $"{(string.IsNullOrWhiteSpace(Width) ? "" : $"width:{Width};")}{(string.IsNullOrWhiteSpace(Height) ? "" : $"height:{Height};")}";
      return style == "" ? null : style;
    }
  }

  protected override void OnParametersSet() {
    if (Value != _previousValueParameter) {
      _previousValueParameter = Value;
      _value = Value;
    }
  }

  /// <summary>
  /// Moves to the next step, if the current step is valid and its <see cref="PixataWizardStep.OnChange"/> allows it
  /// </summary>
  public async Task Next() {
    if (SiblingStep(1) is { } next) {
      await ChangeStep(_steps.IndexOf(next));
    }
  }

  /// <summary>
  /// Moves to the previous step, if the current step's <see cref="PixataWizardStep.OnChange"/> allows it
  /// </summary>
  public async Task Previous() {
    if (SiblingStep(-1) is { } previous) {
      await ChangeStep(_steps.IndexOf(previous));
    }
  }

  /// <summary>
  /// Moves to the step with the given index (in <see cref="Value"/> terms). Moving forward validates the current step first,
  /// and either way the current step's <see cref="PixataWizardStep.OnChange"/> can cancel the move
  /// </summary>
  /// <returns>True if the wizard moved to the step</returns>
  public async Task<bool> ChangeStep(int index) {
    if (!await CanMoveTo(index)) {
      return false;
    }
    await SetValue(index);
    return true;
  }

  /// <summary>
  /// What the Done button does: validates the current step and any others that haven't been shown to be valid (moving to the
  /// first one that isn't), validates the surrounding <see cref="EditForm"/> if there is one, and then raises
  /// <see cref="OnFinish"/>
  /// </summary>
  public async Task Finish() {
    PixataWizardStep? current = CurrentStep;
    if (current is not null && ValidateStep(current) is false) {
      StateHasChanged();
      return;
    }
    foreach (PixataWizardStep step in VisibleSteps.Where(s => s != current)) {
      bool valid = StepEditContext(step) is not null
        // A step's own validators are only attached while its content is shown, so rely on the result from when it was last
        // shown, and show any step that has never been validated so the user can see whether it is valid
        ? step.ValidationResult is true
        : ValidateStep(step) is not false;
      if (!valid) {
        _validateCurrentStepAfterRender = StepEditContext(step) is not null;
        await SetValue(_steps.IndexOf(step));
        return;
      }
    }
    if (CascadedEditContext is not null && !CascadedEditContext.Validate()) {
      StateHasChanged();
      return;
    }
    await OnFinish.InvokeAsync();
  }

  internal void AddStep(PixataWizardStep step) {
    if (_rebuilding) {
      _newSteps.Add(step);
    } else if (_hasRendered && _steps.Count > 0) {
      // A step added after the first render (eg by an @if) may belong between the existing ones. The steps hold no state of
      // their own (their content is rendered here, not by them), so have them all created again, in page order. The old ones
      // are shown until the new ones are all in (see OnAfterRender)
      _rebuilding = true;
      _generation++;
      _newSteps.Clear();
    } else {
      _steps.Add(step);
    }
    StateHasChanged();
  }

  internal void RemoveStep(PixataWizardStep step) {
    if ((_rebuilding ? _newSteps.Remove(step) : _steps.Remove(step)) && !_disposed) {
      StateHasChanged();
    }
  }

  internal void StepChanged() {
    if (!_disposed) {
      StateHasChanged();
    }
  }

  internal void SetStepperSettings(PixataWizardStepperSettings settings) {
    _stepperSettings = settings;
    if (_linear != settings.Linear || _stepType != settings.StepType) {
      _linear = settings.Linear;
      _stepType = settings.StepType;
      StepChanged();
    }
  }

  internal void RemoveStepperSettings(PixataWizardStepperSettings settings) {
    if (_stepperSettings == settings) {
      _stepperSettings = null;
      _linear = false;
      _stepType = PixataStepperStepType.Steps;
      StepChanged();
    }
  }

  /// <summary>
  /// The step after (1) or before (-1) the current one that the user can move to, or null if there isn't one
  /// </summary>
  private PixataWizardStep? SiblingStep(int direction) {
    PixataWizardStep? current = CurrentStep;
    if (current is null) {
      return null;
    }
    for (int i = _steps.IndexOf(current) + direction; i >= 0 && i < _steps.Count; i += direction) {
      if (_steps[i].Visible && !_steps[i].Disabled) {
        return _steps[i];
      }
    }
    return null;
  }

  /// <summary>
  /// Whether the user can leave the current step for another. Moving forward validates the current step (and is refused if it
  /// is invalid). Moving back doesn't stop the user, but re-validates a step that has been validated before, so its indicator
  /// stays accurate. Either way, the step's OnChange can cancel the move
  /// </summary>
  private async Task<bool> CanMoveTo(int index) {
    PixataWizardStep? current = CurrentStep;
    if (index < 0 || index >= _steps.Count || !_steps[index].Visible || _steps[index].Disabled || _steps[index] == current) {
      return false;
    }
    if (current is null) {
      return true;
    }
    int currentIndex = _steps.IndexOf(current);
    if (index > currentIndex) {
      if (ValidateStep(current) is false) {
        StateHasChanged();
        return false;
      }
    } else if (current.ValidationResult is not null) {
      ValidateStep(current);
    }
    PixataWizardStepChangeEventArgs args = new() { TargetIndex = index };
    await current.OnChange.InvokeAsync(args);
    return !args.IsCancelled;
  }

  private async Task SetValue(int index) {
    if (_value != index) {
      _value = index;
      await ValueChanged.InvokeAsync(index);
    }
    StateHasChanged();
  }

  /// <summary>
  /// Validates a step against its own edit context, or the fields it lists in the surrounding form. Returns null if the step
  /// has no validation
  /// </summary>
  private bool? ValidateStep(PixataWizardStep step) {
    bool? result = null;
    if (StepEditContext(step) is { } context) {
      Watch(context);
      result = context.Validate();
    } else if (CascadedEditContext is not null && step.Fields is not null) {
      Watch(CascadedEditContext);
      foreach (FieldIdentifier identifier in FieldIdentifiers(step)) {
        CascadedEditContext.NotifyFieldChanged(identifier);
      }
      result = FieldsAreValid(step, CascadedEditContext);
    }
    if (result is not null) {
      step.ValidationResult = result;
    }
    return result;
  }

  private static List<FieldIdentifier> FieldIdentifiers(PixataWizardStep step) =>
    [.. (step.Fields ?? []).Select(f => FieldIdentifier.Create(f))];

  private static bool FieldsAreValid(PixataWizardStep step, EditContext context) =>
    FieldIdentifiers(step).All(i => !context.GetValidationMessages(i).Any());

  /// <summary>
  /// Once a step has been validated, its indicator in the stepper follows its validation messages, so it turns from an
  /// exclamation mark to a tick as soon as the user fixes the step
  /// </summary>
  private void Watch(EditContext context) {
    if (_watchedContexts.Add(context)) {
      context.OnValidationStateChanged += ValidationStateChanged;
    }
  }

  private void ValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) {
    if (sender is not EditContext context || _disposed) {
      return;
    }
    bool changed = false;
    PixataWizardStep? current = CurrentStep;
    foreach (PixataWizardStep step in _steps.Where(s => s.ValidationResult is not null)) {
      // A step's own validators are only there while it is shown, and clear its messages when they go, so only the current
      // step's messages count
      bool? result = StepEditContext(step) == context ? (step == current ? !context.GetValidationMessages().Any() : null)
        : StepEditContext(step) is null && context == CascadedEditContext && step.Fields is not null ? FieldsAreValid(step, context)
        : null;
      if (result is not null && result != step.ValidationResult) {
        step.ValidationResult = result;
        changed = true;
      }
    }
    if (changed) {
      StateHasChanged();
    }
  }

  /// <summary>
  /// The edit context a step validates against: its own <see cref="PixataWizardStep.EditContext"/>, or one for its
  /// <see cref="PixataWizardStep.Model"/> (kept for as long as the wizard is, so validation messages survive moving between steps)
  /// </summary>
  private EditContext? StepEditContext(PixataWizardStep step) {
    if (step.EditContext is not null) {
      return step.EditContext;
    }
    if (step.Model is null) {
      return null;
    }
    if (!_modelContexts.TryGetValue(step.Model, out EditContext? context)) {
      context = new(step.Model);
      _modelContexts[step.Model] = context;
    }
    return context;
  }

  /// <summary>
  /// Pressing Enter in a step's form moves on, as Next (or Done) would
  /// </summary>
  private async Task SubmitStep(EditContext context) {
    if (IsLastStep) {
      await Finish();
    } else if (SiblingStep(1) is { } next && await ChangeStep(_steps.IndexOf(next))) {
      // The form the user was typing in has gone, so move the focus into the new step
      _focusAfterRender = FocusTarget.Content;
    }
  }

  /// <summary>
  /// The built-in Next button. When it takes the user to the last step, it is replaced by Done, so that gets the focus
  /// </summary>
  private async Task NextClicked() {
    if (SiblingStep(1) is { } next && await ChangeStep(_steps.IndexOf(next)) && IsLastStep) {
      _focusAfterRender = FocusTarget.Done;
    }
  }

  /// <summary>
  /// The built-in Previous button. When it takes the user to the first step, it goes, so Next gets the focus
  /// </summary>
  private async Task PreviousClicked() {
    if (SiblingStep(-1) is { } previous && await ChangeStep(_steps.IndexOf(previous)) && IsFirstStep) {
      _focusAfterRender = IsLastStep ? FocusTarget.Done : FocusTarget.Next;
    }
  }

  private async Task StepperChanging(PixataStepperStepChangeEventArgs args) {
    List<PixataWizardStep> visible = VisibleSteps;
    if (args.TargetIndex < 0 || args.TargetIndex >= visible.Count) {
      args.IsCancelled = true;
      return;
    }
    args.IsCancelled = !await CanMoveTo(_steps.IndexOf(visible[args.TargetIndex]));
  }

  private async Task StepperValueChanged(int visibleIndex) {
    List<PixataWizardStep> visible = VisibleSteps;
    if (visibleIndex >= 0 && visibleIndex < visible.Count) {
      await SetValue(_steps.IndexOf(visible[visibleIndex]));
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      _hasRendered = true;
    }
    if (_rebuilding) {
      CompleteRebuild();
      return;
    }
    if (_validateCurrentStepAfterRender) {
      // The step's validators are attached now that its content has been rendered, so its messages can be shown
      _validateCurrentStepAfterRender = false;
      if (CurrentStep is { } step && ValidateStep(step) is not null) {
        StateHasChanged();
      }
    }
    if (CurrentStep is { } current && _steps.IndexOf(current) != _value) {
      // The step Value pointed to has been hidden or removed, so another one is shown instead. Tell the parent
      _value = _steps.IndexOf(current);
      await ValueChanged.InvokeAsync(_value);
    }
    FocusTarget focus = _focusAfterRender;
    _focusAfterRender = FocusTarget.None;
    switch (focus) {
      case FocusTarget.Content:
        if (!await PixataJs.Invoke(Js, "focusFirst", false, _content)) {
          await PixataJs.InvokeVoid(Js, "focusElement", _panel);
        }
        break;
      case FocusTarget.Next:
        await PixataJs.InvokeVoid(Js, "focusElement", _nextButton);
        break;
      case FocusTarget.Done:
        await PixataJs.InvokeVoid(Js, "focusElement", _doneButton);
        break;
    }
  }

  /// <summary>
  /// Swaps in the steps that were created again in page order. Steps that validate against the same model or edit context as
  /// an old step keep its validation result
  /// </summary>
  private void CompleteRebuild() {
    _rebuilding = false;
    foreach (PixataWizardStep step in _newSteps.Where(s => s.ValidationResult is null)) {
      object? identity = (object?)step.EditContext ?? step.Model;
      if (identity is not null && _steps.FirstOrDefault(s => ReferenceEquals((object?)s.EditContext ?? s.Model, identity)) is { } old) {
        step.ValidationResult = old.ValidationResult;
      }
    }
    _steps.Clear();
    _steps.AddRange(_newSteps);
    _newSteps.Clear();
    StateHasChanged();
  }

  private enum FocusTarget {
    None,
    Content,
    Next,
    Done
  }

  public void Dispose() {
    _disposed = true;
    foreach (EditContext context in _watchedContexts) {
      context.OnValidationStateChanged -= ValidationStateChanged;
    }
    _watchedContexts.Clear();
    GC.SuppressFinalize(this);
  }
}
