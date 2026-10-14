namespace Pixata.Blazor.Components;

/// <summary>
/// The size and extra classes of a component's popup (the list of a drop-down, the menu of a drop-down button and so on).
/// Sizes are CSS lengths, eg <c>"300px"</c> or <c>"20rem"</c>
/// </summary>
public class PixataPopupSettings {
  /// <summary>
  /// The popup's width. By default it is at least as wide as the component it belongs to, and wider if its content needs it
  /// </summary>
  public string? Width { get; set; }

  public string? MinWidth { get; set; }

  public string? MaxWidth { get; set; }

  /// <summary>
  /// The height of the popup's scrolling content (eg the list of items)
  /// </summary>
  public string? Height { get; set; }

  public string? MinHeight { get; set; }

  public string? MaxHeight { get; set; }

  /// <summary>
  /// Extra CSS classes for the popup element
  /// </summary>
  public string? Class { get; set; }
}
