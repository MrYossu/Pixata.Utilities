namespace Pixata.Blazor.Components;

/// <summary>
/// How the content of a <see cref="PixataWindow"/>'s <c>WindowFooter</c> is laid out
/// </summary>
public enum PixataWindowFooterLayoutAlign {
  /// <summary>
  /// Together at the start of the footer (the left, in a left-to-right page)
  /// </summary>
  Start,

  /// <summary>
  /// Together in the middle of the footer
  /// </summary>
  Center,

  /// <summary>
  /// Together at the end of the footer (the right, in a left-to-right page)
  /// </summary>
  End,

  /// <summary>
  /// Sharing the whole footer equally (the default)
  /// </summary>
  Stretch
}
