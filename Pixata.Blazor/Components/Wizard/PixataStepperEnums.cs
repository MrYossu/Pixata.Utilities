namespace Pixata.Blazor.Components;

/// <summary>
/// What a <see cref="PixataStepper"/> shows for each step
/// </summary>
public enum PixataStepperStepType {
  /// <summary>
  /// A round indicator (holding the step's number, text or icon) and the step's label (the default)
  /// </summary>
  Steps,

  /// <summary>
  /// Just the step's label
  /// </summary>
  Labels
}

/// <summary>
/// Which way a <see cref="PixataStepper"/> runs
/// </summary>
public enum PixataStepperOrientation {
  /// <summary>
  /// The steps are in a row (the default)
  /// </summary>
  Horizontal,

  /// <summary>
  /// The steps are in a column
  /// </summary>
  Vertical
}

/// <summary>
/// Where a <see cref="PixataWizard"/> puts its stepper, relative to the current step
/// </summary>
public enum PixataWizardStepperPosition {
  /// <summary>
  /// Above the step (the default)
  /// </summary>
  Top,

  /// <summary>
  /// Below the step
  /// </summary>
  Bottom,

  /// <summary>
  /// To the left of the step, running down the page
  /// </summary>
  Left,

  /// <summary>
  /// To the right of the step, running down the page
  /// </summary>
  Right
}
