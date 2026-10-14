using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// The size of the menu of a <see cref="PixataDropDownButton"/>, given inside its <c>DropDownButtonSettings</c>. You can use
/// its <c>PopupSettings</c> parameter instead
/// </summary>
public class PixataDropDownButtonPopupSettings : PixataMenuButtonPopupSettingsBase {
}

/// <summary>
/// The size of the menu of a <see cref="PixataSplitButton"/>, given inside its <c>SplitButtonSettings</c>. You can use its
/// <c>PopupSettings</c> parameter instead
/// </summary>
public class PixataSplitButtonPopupSettings : PixataMenuButtonPopupSettingsBase {
}

/// <summary>
/// The base of <see cref="PixataDropDownButtonPopupSettings"/> and <see cref="PixataSplitButtonPopupSettings"/>. It renders
/// nothing, but passes its parameters to the button it is inside. Sizes are CSS lengths, eg <c>"300px"</c>
/// </summary>
public abstract class PixataMenuButtonPopupSettingsBase : ComponentBase {
  [CascadingParameter]
  private PixataMenuButtonBase? Owner { get; set; }

  /// <summary>
  /// The width of the menu. By default it is at least as wide as the button, and wider if its items need it
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  [Parameter]
  public string? MinWidth { get; set; }

  [Parameter]
  public string? MaxWidth { get; set; }

  /// <summary>
  /// The height of the menu. It scrolls if the items don't fit
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? MinHeight { get; set; }

  /// <summary>
  /// The most the menu can grow to before it scrolls
  /// </summary>
  [Parameter]
  public string? MaxHeight { get; set; }

  /// <summary>
  /// Extra CSS classes for the menu's popup element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  protected override void OnParametersSet() {
    if (Owner is not null) {
      Owner.NestedPopupSettings = new() {
        Width = Width,
        MinWidth = MinWidth,
        MaxWidth = MaxWidth,
        Height = Height,
        MinHeight = MinHeight,
        MaxHeight = MaxHeight,
        Class = Class
      };
    }
  }
}
