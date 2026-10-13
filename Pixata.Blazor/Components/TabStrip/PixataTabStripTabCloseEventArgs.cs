namespace Pixata.Blazor.Components;

/// <summary>
/// Passed to <see cref="PixataTabStrip.OnTabClose"/> when the user closes a tab
/// </summary>
public class PixataTabStripTabCloseEventArgs {
  /// <summary>
  /// The <see cref="PixataTab.Id"/> of the tab being closed (or the id generated for it, if it doesn't have one)
  /// </summary>
  public string TabId { get; init; } = "";

  /// <summary>
  /// Set this to true to keep the tab open
  /// </summary>
  public bool IsCancelled { get; set; }
}
