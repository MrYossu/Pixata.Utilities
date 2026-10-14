using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// Settings for a <see cref="PixataWizard"/>'s stepper. Put it inside the wizard's <see cref="PixataWizard.WizardSettings"/>
/// </summary>
public class PixataWizardStepperSettings : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataWizard? Wizard { get; set; }

  /// <summary>
  /// When true, the user can only click their way to the steps either side of the current one in the stepper, so can't skip
  /// any. The Previous and Next buttons aren't affected. False by default
  /// </summary>
  [Parameter]
  public bool Linear { get; set; }

  /// <summary>
  /// Whether the stepper's steps show an indicator and a label (<see cref="PixataStepperStepType.Steps"/>, the default) or
  /// just a label
  /// </summary>
  [Parameter]
  public PixataStepperStepType StepType { get; set; } = PixataStepperStepType.Steps;

  protected override void OnInitialized() {
    if (Wizard is null) {
      throw new InvalidOperationException($"{nameof(PixataWizardStepperSettings)} must be inside a {nameof(PixataWizard)}'s WizardSettings");
    }
  }

  protected override void OnParametersSet() =>
    Wizard?.SetStepperSettings(this);

  public void Dispose() {
    Wizard?.RemoveStepperSettings(this);
    GC.SuppressFinalize(this);
  }
}
