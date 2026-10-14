using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// One step of a <see cref="PixataStepper"/>. It renders nothing itself: the stepper draws it from these parameters
/// </summary>
public class PixataStepperStep : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataStepper? Stepper { get; set; }

  /// <summary>
  /// The step's label
  /// </summary>
  [Parameter]
  public string? Label { get; set; }

  /// <summary>
  /// The text in the step's indicator. If neither this nor <see cref="Icon"/> is set, the indicator shows the step's number
  /// </summary>
  [Parameter]
  public string? Text { get; set; }

  /// <summary>
  /// An icon for the step's indicator: a <see cref="PixataSvgIcon"/>, or the CSS class(es) of an icon font
  /// </summary>
  [Parameter]
  public object? Icon { get; set; }

  /// <summary>
  /// When true, the step is greyed out and can't be selected
  /// </summary>
  [Parameter]
  public bool Disabled { get; set; }

  /// <summary>
  /// When true, the step's label is followed by <see cref="PixataStepper.OptionalText"/>
  /// </summary>
  [Parameter]
  public bool Optional { get; set; }

  /// <summary>
  /// Shows whether the step is valid: true shows a tick, false an exclamation mark and an error colour, and null (the
  /// default) neither. This is only an indication, and doesn't stop the user moving between steps (use <see cref="OnChange"/>
  /// for that)
  /// </summary>
  [Parameter]
  public bool? Valid { get; set; }

  /// <summary>
  /// Extra CSS classes for the step
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Custom content for the step, replacing its indicator and label
  /// </summary>
  [Parameter]
  public RenderFragment? Template { get; set; }

  /// <summary>
  /// Raised when the user tries to move from this step to another. Set
  /// <see cref="PixataStepperStepChangeEventArgs.IsCancelled"/> to stay on this step
  /// </summary>
  [Parameter]
  public EventCallback<PixataStepperStepChangeEventArgs> OnChange { get; set; }

  /// <summary>
  /// The id of the element the step controls, rendered as its aria-controls attribute
  /// </summary>
  [Parameter]
  public string? AriaControls { get; set; }

  private (string?, string?, object?, bool, bool, bool?, string?, bool, string?) _display;
  private bool _registered;
  private PixataTemplateHost? _templateHost;

  /// <summary>
  /// The step's element in the stepper, set by the stepper
  /// </summary>
  internal ElementReference Element { get; set; }

  /// <summary>
  /// The step's <see cref="Template"/>, for the stepper to render. It is rendered by a host that this step refreshes when it is
  /// given a new template, as the stepper may render before the steps are given their new parameters
  /// </summary>
  internal RenderFragment TemplateContent =>
    PixataTemplateHost.For(() => Template, host => _templateHost = host);

  protected override void OnInitialized() {
    if (Stepper is null) {
      throw new InvalidOperationException($"{nameof(PixataStepperStep)} must be inside a {nameof(PixataStepper)}");
    }
    Stepper.AddStep(this);
    _registered = true;
  }

  protected override void OnParametersSet() {
    // The template is compared by whether there is one, as a new delegate is created each time the parent renders
    (string?, string?, object?, bool, bool, bool?, string?, bool, string?) display = (Label, Text, Icon, Disabled, Optional, Valid, Class, Template is not null, AriaControls);
    if (!display.Equals(_display)) {
      _display = display;
      Stepper?.StepChanged();
    }
    if (Template is not null) {
      _templateHost?.Refresh();
    }
  }

  public void Dispose() {
    if (_registered) {
      Stepper?.RemoveStep(this);
    }
    GC.SuppressFinalize(this);
  }
}
