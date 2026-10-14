namespace Pixata.Blazor.Components;

/// <summary>
/// Whether a <see cref="PixataWindow"/> is shown normally, minimised or maximised
/// </summary>
public enum PixataWindowState {
  /// <summary>
  /// Shown at its own size and position
  /// </summary>
  Default,

  /// <summary>
  /// Only the titlebar is shown
  /// </summary>
  Minimized,

  /// <summary>
  /// Fills the viewport (or its containment element)
  /// </summary>
  Maximized
}
