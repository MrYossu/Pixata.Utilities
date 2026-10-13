using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// Shows the steps of a process and which one the user is on, and lets them move between them. Declare the steps with
/// <see cref="PixataStepperStep"/> components inside <see cref="StepperSteps"/>
/// </summary>
public partial class PixataStepper : IDisposable {
  private static readonly string[] NavigationKeys = ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End"];

  [CascadingParameter(Name = PixataWizard.StepperCascadeName)]
  private PixataWizard? Wizard { get; set; }

  /// <summary>
  /// The <see cref="PixataStepperStep"/> components
  /// </summary>
  [Parameter]
  public RenderFragment? StepperSteps { get; set; }

  /// <summary>
  /// The index of the current step, starting at 0. Use <c>@bind-Value</c> for two-way binding
  /// </summary>
  [Parameter]
  public int Value { get; set; }

  /// <summary>
  /// Raised when the user moves to another step
  /// </summary>
  [Parameter]
  public EventCallback<int> ValueChanged { get; set; }

  /// <summary>
  /// Whether each step shows an indicator and a label (<see cref="PixataStepperStepType.Steps"/>, the default) or just a label
  /// </summary>
  [Parameter]
  public PixataStepperStepType StepType { get; set; } = PixataStepperStepType.Steps;

  /// <summary>
  /// Whether the steps are in a row (<see cref="PixataStepperOrientation.Horizontal"/>, the default) or a column
  /// </summary>
  [Parameter]
  public PixataStepperOrientation Orientation { get; set; } = PixataStepperOrientation.Horizontal;

  /// <summary>
  /// When true, the user can only move one step at a time, to the next or the previous step, so can't skip any. False by default
  /// </summary>
  [Parameter]
  public bool Linear { get; set; }

  /// <summary>
  /// When true (the default), a horizontal stepper with less than 80px for each step (eg less than 400px wide with five steps)
  /// collapses to show just the current step and a progress bar, with buttons to move to the previous and next steps, so it
  /// still works on a phone
  /// </summary>
  [Parameter]
  public bool Collapsible { get; set; } = true;

  /// <summary>
  /// What follows the label of an <see cref="PixataStepperStep.Optional"/> step. <c>"(Optional)"</c> by default
  /// </summary>
  [Parameter]
  public string OptionalText { get; set; } = "(Optional)";

  /// <summary>
  /// The text shown when the stepper is collapsed, where <c>{0}</c> is the current step's number and <c>{1}</c> the number of
  /// steps. <c>"Step {0} of {1}"</c> by default
  /// </summary>
  [Parameter]
  public string CollapsedTextFormat { get; set; } = "Step {0} of {1}";

  /// <summary>
  /// The accessible name of the collapsed stepper's previous step button. <c>"Previous step"</c> by default
  /// </summary>
  [Parameter]
  public string PreviousStepLabel { get; set; } = "Previous step";

  /// <summary>
  /// The accessible name of the collapsed stepper's next step button. <c>"Next step"</c> by default
  /// </summary>
  [Parameter]
  public string NextStepLabel { get; set; } = "Next step";

  /// <summary>
  /// The width of the stepper, as a CSS length
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The height of the stepper, as a CSS length. Mostly useful for a vertical stepper, whose steps share the height equally
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Extra CSS classes for the stepper's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Any other attributes (eg <c>aria-label</c>) are rendered on the stepper's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  private readonly List<PixataStepperStep> _steps = [];
  private readonly List<PixataStepperStep> _newSteps = [];
  private ElementReference _list;
  private int _value;
  private int? _previousValueParameter;
  private int? _focusIndex;
  private bool _focusPending;
  private bool _hasRendered;
  private bool _rebuilding;
  private int _generation;
  private bool _disposed;

  private bool InWizard =>
    Wizard is not null;

  private bool IsVertical =>
    Orientation == PixataStepperOrientation.Vertical;

  /// <summary>
  /// The current step's index, kept in range
  /// </summary>
  private int CurrentIndex =>
    _steps.Count == 0 ? 0 : Math.Clamp(_value, 0, _steps.Count - 1);

  private string RootClass =>
    PixataCss.Classes("pixata-blazor-stepper",
      $"pixata-blazor-stepper-{(IsVertical ? "vertical" : "horizontal")}",
      StepType == PixataStepperStepType.Labels ? "pixata-blazor-stepper-labels" : null,
      Linear ? "pixata-blazor-stepper-linear" : null,
      Collapsible ? "pixata-blazor-stepper-collapsible" : null,
      Collapsible && _steps.Count > 1 ? $"pixata-blazor-stepper-count-{Math.Min(_steps.Count, 8)}" : null,
      Class);

  private string? Style {
    get {
      string style = $"{(string.IsNullOrWhiteSpace(Width) ? "" : $"width:{Width};")}{(string.IsNullOrWhiteSpace(Height) ? "" : $"height:{Height};")}";
      return style == "" ? null : style;
    }
  }

  /// <summary>
  /// The progress line runs from the middle of the first step to the middle of the last
  /// </summary>
  private string ProgressStyle =>
    IsVertical
      ? $"height:{Percent(_steps.Count - 1, _steps.Count)}%;"
      : $"inset-inline:{Percent(1, _steps.Count * 2)}%;";

  private string ProgressValueStyle(int value) =>
    IsVertical
      ? $"height:{Percent(value, _steps.Count - 1)}%;"
      : $"width:{Percent(value, _steps.Count - 1)}%;";

  private static string Percent(int part, int whole) =>
    (whole <= 0 ? 0 : Math.Clamp(part * 100.0 / whole, 0, 100)).ToString("0.####", CultureInfo.InvariantCulture);

  /// <summary>
  /// The step that can be reached with the Tab key: the one last moved to with the arrow keys, or the current step
  /// </summary>
  private int TabStop {
    get {
      int candidate = _focusIndex ?? CurrentIndex;
      if (candidate >= 0 && candidate < _steps.Count && !_steps[candidate].Disabled) {
        return candidate;
      }
      return _steps.FindIndex(s => !s.Disabled);
    }
  }

  private string StepClass(PixataStepperStep step, int index, int value, bool? valid) =>
    PixataCss.Classes("pixata-blazor-stepper-step",
      index == 0 ? "pixata-blazor-stepper-step-first" : null,
      index == _steps.Count - 1 ? "pixata-blazor-stepper-step-last" : null,
      index < value ? "pixata-blazor-stepper-step-done" : null,
      index == value ? "pixata-blazor-stepper-step-current" : null,
      step.Optional ? "pixata-blazor-stepper-step-optional" : null,
      step.Disabled ? "pixata-blazor-stepper-step-disabled" : null,
      !step.Disabled && !IsReachable(index) && index != value ? "pixata-blazor-stepper-step-unreachable" : null,
      valid is true ? "pixata-blazor-stepper-step-success" : null,
      valid is false ? "pixata-blazor-stepper-step-error" : null,
      step.Class);

  protected override void OnParametersSet() {
    if (Value != _previousValueParameter) {
      _previousValueParameter = Value;
      _value = Value;
      _focusIndex = null;
    }
  }

  internal void AddStep(PixataStepperStep step) {
    if (_rebuilding) {
      _newSteps.Add(step);
    } else if (_hasRendered && _steps.Count > 0) {
      // A step added after the first render (eg by an @if) may belong between the existing ones. The steps hold no state of
      // their own, so the simplest way to get them in the right order is to have them all created again, in page order. The
      // old ones are shown until the new ones are all in (see OnAfterRenderAsync)
      _rebuilding = true;
      _generation++;
      _newSteps.Clear();
    } else {
      _steps.Add(step);
    }
    StateHasChanged();
  }

  internal void RemoveStep(PixataStepperStep step) {
    if ((_rebuilding ? _newSteps.Remove(step) : _steps.Remove(step)) && !_disposed) {
      StateHasChanged();
    }
  }

  internal void StepChanged() {
    if (!_disposed) {
      StateHasChanged();
    }
  }

  /// <summary>
  /// Whether the user may click their way to a step: always, unless <see cref="Linear"/> is set, when only the steps either
  /// side of the current one can be reached
  /// </summary>
  private bool IsReachable(int index) =>
    !Linear || Math.Abs(index - CurrentIndex) <= 1;

  /// <summary>
  /// Moves to a step, if the current step's <see cref="PixataStepperStep.OnChange"/> allows it
  /// </summary>
  private async Task Select(int index) {
    int current = CurrentIndex;
    if (index == current || index < 0 || index >= _steps.Count || _steps[index].Disabled || !IsReachable(index)) {
      return;
    }
    _focusIndex = index;
    PixataStepperStepChangeEventArgs args = new() { TargetIndex = index };
    await _steps[current].OnChange.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    _value = index;
    await ValueChanged.InvokeAsync(index);
  }

  /// <summary>
  /// The nearest step before (-1) or after (1) a step that can be selected, or null if there isn't one
  /// </summary>
  private int? SiblingIndex(int from, int direction) {
    for (int i = from + direction; i >= 0 && i < _steps.Count; i += direction) {
      if (!_steps[i].Disabled) {
        return IsReachable(i) ? i : null;
      }
    }
    return null;
  }

  private async Task SelectSibling(int direction) {
    if (SiblingIndex(CurrentIndex, direction) is int index) {
      await Select(index);
    }
  }

  private void KeyDown(KeyboardEventArgs e) {
    List<int> focusable = [.. Enumerable.Range(0, _steps.Count).Where(i => !_steps[i].Disabled)];
    if (focusable.Count == 0) {
      return;
    }
    int position = focusable.IndexOf(TabStop);
    int? target = e.Key switch {
      "ArrowRight" or "ArrowDown" => focusable[Math.Min(position + 1, focusable.Count - 1)],
      "ArrowLeft" or "ArrowUp" => focusable[Math.Max(position - 1, 0)],
      "Home" => focusable[0],
      "End" => focusable[^1],
      _ => null
    };
    if (target is int index) {
      _focusIndex = index;
      _focusPending = true;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      _hasRendered = true;
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", _list, NavigationKeys);
    }
    if (_rebuilding) {
      _rebuilding = false;
      _steps.Clear();
      _steps.AddRange(_newSteps);
      _newSteps.Clear();
      StateHasChanged();
    } else if (_steps.Count > 0 && _value != CurrentIndex) {
      // The step Value pointed to has been removed, so the last step is shown instead. Tell the parent
      _value = CurrentIndex;
      await ValueChanged.InvokeAsync(_value);
    }
    if (_focusPending) {
      _focusPending = false;
      int index = TabStop;
      if (index >= 0 && index < _steps.Count) {
        await PixataJs.InvokeVoid(Js, "focusElement", _steps[index].Element);
      }
    }
  }

  public void Dispose() {
    _disposed = true;
    GC.SuppressFinalize(this);
  }
}
