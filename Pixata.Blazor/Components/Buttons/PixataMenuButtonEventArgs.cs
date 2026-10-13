namespace Pixata.Blazor.Components;

/// <summary>
/// The details of a <see cref="PixataMenuButtonBase.OnOpen"/> event
/// </summary>
public class PixataMenuButtonOpenEventArgs {
  /// <summary>
  /// Set to true to stop the menu opening
  /// </summary>
  public bool IsCancelled { get; set; }
}

/// <summary>
/// The details of a <see cref="PixataMenuButtonBase.OnClose"/> event
/// </summary>
public class PixataMenuButtonCloseEventArgs {
  /// <summary>
  /// Set to true to keep the menu open
  /// </summary>
  public bool IsCancelled { get; set; }
}
