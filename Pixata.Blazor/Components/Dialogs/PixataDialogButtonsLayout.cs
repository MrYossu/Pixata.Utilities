namespace Pixata.Blazor.Components;

/// <summary>
/// How the buttons in a <see cref="PixataDialog"/>'s <c>DialogButtons</c> are laid out
/// </summary>
public enum PixataDialogButtonsLayout {
  /// <summary>
  /// Together at the start of the row (the left, in a left-to-right page)
  /// </summary>
  Start,

  /// <summary>
  /// Together in the middle of the row
  /// </summary>
  Center,

  /// <summary>
  /// Together at the end of the row (the right, in a left-to-right page)
  /// </summary>
  End,

  /// <summary>
  /// Sharing the whole row equally (the default)
  /// </summary>
  Stretch
}
