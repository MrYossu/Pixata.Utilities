using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A single-line text box, or a password box when <see cref="Password"/> is true. It has the same parameters as Telerik's
/// TextBox, so markup can usually move across by changing the component's name
/// </summary>
public partial class PixataTextBox {
  /// <summary>
  /// Fires when the user has finished changing the value, which is when they press Enter or leave the box, rather than on
  /// every key. The argument is the new value
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  /// <summary>
  /// Fires when the box loses focus
  /// </summary>
  [Parameter]
  public EventCallback OnBlur { get; set; }

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
  /// How long (in milliseconds) to wait after the user stops typing before <c>ValueChanged</c> fires. The default of 0 raises
  /// it on every key, so the bound value is always current. Pressing Enter or leaving the box raises it straight away
  /// </summary>
  [Parameter]
  public int DebounceDelay { get; set; }

  /// <summary>
  /// Shown when the box is empty
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  [Parameter]
  public int? MaxLength { get; set; }

  /// <summary>
  /// The <c>inputmode</c> of the input, which picks the keyboard on a phone, eg <c>email</c> or <c>tel</c>
  /// </summary>
  [Parameter]
  public string? InputMode { get; set; }

  /// <summary>
  /// The <c>autocomplete</c> of the input, eg <c>email</c>, <c>current-password</c> or <c>off</c>
  /// </summary>
  [Parameter]
  public string? AutoComplete { get; set; }

  /// <summary>
  /// The <c>autocapitalize</c> of the input, eg <c>off</c>, <c>words</c> or <c>sentences</c>
  /// </summary>
  [Parameter]
  public string? AutoCapitalize { get; set; }

  /// <summary>
  /// Whether the browser checks the spelling. Left to the browser when null
  /// </summary>
  [Parameter]
  public bool? SpellCheck { get; set; }

  /// <summary>
  /// The input's tooltip
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  /// <summary>
  /// When true, the text can be seen and copied but not changed
  /// </summary>
  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// The width, as a CSS width. By default the box fills the width it's given
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// <c>none</c>, <c>sm</c>, <c>md</c> (the default), <c>lg</c> or <c>full</c>. See <see cref="PixataThemeConstants.Rounded"/>
  /// </summary>
  [Parameter]
  public string? Rounded { get; set; }

  /// <summary>
  /// <c>solid</c> (the default), <c>flat</c> or <c>outline</c>. See <see cref="PixataThemeConstants.FillMode"/>
  /// </summary>
  [Parameter]
  public string? FillMode { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

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

  private ElementReference _input;
  private string _text = "";
  private string? _lastValue;
  private bool _revealed;
  private CancellationTokenSource? _debounce;

  private string WrapperClass =>
    PixataCss.Classes(PixataCss.Input(Size, Rounded, FillMode), "pixata-blazor-textbox", PixataCss.Disabled(Enabled), InvalidClass, Class);

  private string? WidthStyle =>
    string.IsNullOrWhiteSpace(Width) ? null : $"width:{Width};";

  private string InputType =>
    Password && !_revealed ? "password" : "text";

  private string? SpellCheckAttribute =>
    SpellCheck switch {
      true => "true",
      false => "false",
      null => null
    };

  /// <summary>
  /// Puts the focus in the box
  /// </summary>
  public ValueTask FocusAsync() =>
    _input.FocusAsync();

  protected override void OnParametersSet() {
    base.OnParametersSet();
    // Only replaces the text when the value changes from outside, so a debounced value arriving late doesn't undo typing
    if (Value != _lastValue) {
      _lastValue = Value;
      _text = Value ?? "";
    }
  }

  private async Task OnInput(ChangeEventArgs e) {
    _text = e.Value?.ToString() ?? "";
    if (DebounceDelay <= 0) {
      await Commit();
      return;
    }
    CancelDebounce();
    CancellationTokenSource debounce = new();
    _debounce = debounce;
    try {
      await Task.Delay(DebounceDelay, debounce.Token);
    }
    catch (TaskCanceledException) {
      return;
    }
    await Commit();
  }

  // The browser's change event, which comes when the user presses Enter or leaves the box after changing the text
  private async Task OnInputChange(ChangeEventArgs e) {
    _text = e.Value?.ToString() ?? "";
    await Commit();
    await OnChange.InvokeAsync(Value);
  }

  private async Task OnInputBlur(FocusEventArgs e) {
    await Commit();
    await OnBlur.InvokeAsync();
  }

  private async Task Clear() {
    _text = "";
    await Commit();
    await OnChange.InvokeAsync(Value);
    await FocusAsync();
  }

  private async Task Commit() {
    CancelDebounce();
    _lastValue = _text;
    await UpdateValue(_text);
  }

  private void CancelDebounce() {
    _debounce?.Cancel();
    _debounce?.Dispose();
    _debounce = null;
  }

  protected override void Dispose(bool disposing) {
    if (disposing) {
      CancelDebounce();
    }
    base.Dispose(disposing);
  }
}
