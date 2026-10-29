using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// A single-line text box, or a password box when <see cref="Password"/> is true. It has the same parameters as Telerik's
/// TextBox, so markup can usually move across by changing the component's name
/// </summary>
public partial class PixataTextBox {
  /// <summary>
  /// Shows the text as dots, for a password
  /// </summary>
  [Parameter]
  public bool Password { get; set; }

  /// <summary>
  /// With <see cref="Password"/>, shows a button that shows and hides the password
  /// </summary>
  [Parameter]
  public bool ShowPasswordButton { get; set; }

  /// <summary>
  /// The tooltip and accessible name of the button that shows the password, for translating. Defaults to "Show password"
  /// </summary>
  [Parameter]
  public string ShowPasswordText { get; set; } = "Show password";

  /// <summary>
  /// The tooltip and accessible name of the button that hides the password, for translating. Defaults to "Hide password"
  /// </summary>
  [Parameter]
  public string HidePasswordText { get; set; } = "Hide password";

  /// <summary>
  /// Shows a button that clears the text, when there is any
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; }

  /// <summary>
  /// The tooltip and accessible name of the clear button, for translating. Defaults to "Clear"
  /// </summary>
  [Parameter]
  public string ClearButtonText { get; set; } = "Clear";

  /// <summary>
  /// Content shown in the box before the text, such as an icon
  /// </summary>
  [Parameter]
  public RenderFragment? TextBoxPrefixTemplate { get; set; }

  /// <summary>
  /// Content shown in the box after the text, such as a unit
  /// </summary>
  [Parameter]
  public RenderFragment? TextBoxSuffixTemplate { get; set; }

  /// <summary>
  /// Draws a line between the prefix and the text. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowPrefixSeparator { get; set; } = true;

  /// <summary>
  /// Draws a line between the text and the suffix. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowSuffixSeparator { get; set; } = true;

  private bool _revealed;

  private string WrapperClass =>
    PixataCss.Classes(PixataCss.Input(Size, Rounded, FillMode), "pixata-blazor-textbox", PixataCss.Disabled(Enabled), InvalidClass, Class);

  private string InputType =>
    Password && !_revealed ? "password" : "text";

  private async Task Clear() {
    Text = "";
    await Commit();
    await OnChange.InvokeAsync(Value);
    await FocusAsync();
  }
}
