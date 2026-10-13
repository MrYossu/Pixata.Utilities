using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Pixata.Blazor.Components;

/// <summary>
/// One step of a <see cref="PixataWizard"/>. It renders nothing itself: the wizard shows it in its stepper, and shows its
/// <see cref="Content"/> when it is the current step
/// </summary>
public class PixataWizardStep : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataWizard? Wizard { get; set; }

  /// <summary>
  /// The step's label in the stepper
  /// </summary>
  [Parameter]
  public string? Label { get; set; }

  /// <summary>
  /// The text in the step's indicator in the stepper. If neither this nor <see cref="Icon"/> is set, the step's number is shown
  /// </summary>
  [Parameter]
  public string? Text { get; set; }

  /// <summary>
  /// An icon for the step's indicator in the stepper: a <see cref="PixataSvgIcon"/>, or the CSS class(es) of an icon font
  /// </summary>
  [Parameter]
  public object? Icon { get; set; }

  /// <summary>
  /// What the wizard shows when this is the current step
  /// </summary>
  [Parameter]
  public RenderFragment? Content { get; set; }

  /// <summary>
  /// Custom content for the step in the stepper, replacing its indicator and label
  /// </summary>
  [Parameter]
  public RenderFragment? StepTemplate { get; set; }

  /// <summary>
  /// When true, the step is greyed out in the stepper, and the user can't go to it
  /// </summary>
  [Parameter]
  public bool Disabled { get; set; }

  /// <summary>
  /// When false, the step is left out of the wizard altogether. The other steps keep their indexes
  /// </summary>
  [Parameter]
  public bool Visible { get; set; } = true;

  /// <summary>
  /// When true, the step's label in the stepper is followed by <see cref="PixataWizard.OptionalText"/>
  /// </summary>
  [Parameter]
  public bool Optional { get; set; }

  /// <summary>
  /// Shows whether the step is valid in the stepper: true shows a tick, false an exclamation mark, and null (the default)
  /// neither, or the result of the step's own validation if it has a <see cref="Model"/>, <see cref="EditContext"/> or
  /// <see cref="Fields"/>. Setting this doesn't stop the user moving on (use <see cref="OnChange"/> or validation for that)
  /// </summary>
  [Parameter]
  public bool? Valid { get; set; }

  /// <summary>
  /// Extra CSS classes for the step in the stepper
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Raised when the user tries to move from this step to another (but not when they click Done, which raises
  /// <see cref="PixataWizard.OnFinish"/>). Set <see cref="PixataWizardStepChangeEventArgs.IsCancelled"/> to stay on this step
  /// </summary>
  [Parameter]
  public EventCallback<PixataWizardStepChangeEventArgs> OnChange { get; set; }

  /// <summary>
  /// A model to validate this step against. The wizard wraps the step's <see cref="Content"/> in a form for this model (so put
  /// a validator such as <c>&lt;DataAnnotationsValidator /&gt;</c> and your inputs in the content), and won't move forward
  /// from the step, or finish, until it is valid. Pressing Enter in the form moves to the next step
  /// </summary>
  [Parameter]
  public object? Model { get; set; }

  /// <summary>
  /// Instead of <see cref="Model"/>, the <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/> to validate this step
  /// against
  /// </summary>
  [Parameter]
  public EditContext? EditContext { get; set; }

  /// <summary>
  /// When the wizard is inside an <see cref="EditForm"/> (so the whole wizard edits one model), the fields that belong to this
  /// step, eg <c>Fields="@([() => order.Name, () => order.Email])"</c>. Those fields are validated before the user can move
  /// forward from the step, so later steps' fields don't stop them. Ignored if the step has its own <see cref="Model"/> or
  /// <see cref="EditContext"/>
  /// </summary>
  [Parameter]
  public IEnumerable<Expression<Func<object?>>>? Fields { get; set; }

  private (string?, string?, object?, bool, bool, bool, bool?, string?, bool, bool, object?, EditContext?) _display;
  private bool _registered;

  /// <summary>
  /// The result of the last time the wizard validated this step, or null if it hasn't been
  /// </summary>
  internal bool? ValidationResult { get; set; }

  /// <summary>
  /// What the stepper shows: <see cref="Valid"/> if it is set, otherwise the result of the step's validation
  /// </summary>
  internal bool? EffectiveValid =>
    Valid ?? ValidationResult;

  protected override void OnInitialized() {
    if (Wizard is null) {
      throw new InvalidOperationException($"{nameof(PixataWizardStep)} must be inside a {nameof(PixataWizard)}");
    }
    Wizard.AddStep(this);
    _registered = true;
  }

  protected override void OnParametersSet() {
    // Render fragments and Fields are compared by whether there are any, as the parent creates new ones each time it renders
    (string?, string?, object?, bool, bool, bool, bool?, string?, bool, bool, object?, EditContext?) display =
      (Label, Text, Icon, Disabled, Visible, Optional, Valid, Class, Content is not null, StepTemplate is not null, Model, EditContext);
    if (!display.Equals(_display)) {
      _display = display;
      Wizard?.StepChanged();
    }
  }

  public void Dispose() {
    if (_registered) {
      Wizard?.RemoveStep(this);
    }
    GC.SuppressFinalize(this);
  }
}
