namespace Pixata.Blazor.Components;

/// <summary>
/// A <see cref="PixataButton"/> for the main action on a page or form, in the primary theme colour. It submits its form
/// unless you set <see cref="PixataButton.ButtonType"/>
/// </summary>
public class PixataPrimaryButton : PixataButton {
  public PixataPrimaryButton() {
    ThemeColor = PixataThemeConstants.Button.ThemeColor.Primary;
  }
}
