namespace Pixata.Blazor.Components;

/// <summary>
/// Passed to <see cref="PixataStepperStep.OnChange"/> when the user tries to move away from that step
/// </summary>
public class PixataStepperStepChangeEventArgs {
  /// <summary>
  /// The index of the step the user wants to go to
  /// </summary>
  public int TargetIndex { get; init; }

  /// <summary>
  /// Set this to true to stay on the current step
  /// </summary>
  public bool IsCancelled { get; set; }
}
