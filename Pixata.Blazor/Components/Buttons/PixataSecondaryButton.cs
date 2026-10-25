namespace Pixata.Blazor.Components;

/// <summary>
/// A <see cref="PixataButton"/> for the other actions next to a <see cref="PixataPrimaryButton"/> (Cancel, Back and so on),
/// in the base theme colour
/// </summary>
public class PixataSecondaryButton : PixataButton {
  public PixataSecondaryButton() {
    ThemeColor = PixataThemeConstants.Button.ThemeColor.Base;
    // Secondary actions are rarely the one that should submit the form, so pressing Enter mustn't trigger them
    ButtonType = PixataButtonType.Button;
  }
}
