namespace Pixata.Blazor.Components;

public static partial class PixataSvgIcons {
  /// <summary>
  /// A tick in a circle, shown by a <see cref="PixataStepper"/> step that is valid
  /// </summary>
  public static readonly PixataSvgIcon StepSuccess = new("step-success", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M8 12.5l2.75 2.75L16 10\"/>");

  /// <summary>
  /// An exclamation mark in a circle, shown by a <see cref="PixataStepper"/> step that is invalid
  /// </summary>
  public static readonly PixataSvgIcon StepError = new("step-error", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7.5v5.5M12 16.5v.5\"/>");
}
