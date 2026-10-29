using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataTextBox"/> and <see cref="PixataTextArea"/> have in common: the text is shown as the user types it,
/// <c>ValueChanged</c> fires as they type (or after <see cref="DebounceDelay"/>), and <see cref="OnChange"/> fires when they
/// press Enter or leave the box
/// </summary>
/// <remarks>Derived components render <see cref="Text"/>, capture their input as <see cref="InputElement"/>, and wire its
/// <c>oninput</c>, <c>onchange</c> and <c>onblur</c> to <see cref="OnInput"/>, <see cref="OnInputChange"/> and
/// <see cref="OnInputBlur"/></remarks>
public abstract class PixataTextInputBase : PixataInputBase<string?> {
  /// <summary>
  /// Fires when the user has finished changing the value, which is when they press Enter (in a text box) or leave the box,
  /// rather than on every key. The argument is the new value
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  /// <summary>
  /// Fires when the box loses focus
  /// </summary>
  [Parameter]
  public EventCallback OnBlur { get; set; }

  /// <summary>
  /// How long (in milliseconds) to wait after the user stops typing before <c>ValueChanged</c> fires. The default of 0 raises
  /// it on every key, so the bound value is always current. Leaving the box (or pressing Enter in a text box) raises it
  /// straight away
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
  /// The input, for focusing it
  /// </summary>
  protected ElementReference InputElement;

  /// <summary>
  /// The text in the box, which runs ahead of <c>Value</c> while a debounced change is waiting
  /// </summary>
  protected string Text { get; set; } = "";

  private string? _lastValue;
  private CancellationTokenSource? _debounce;

  protected string? WidthStyle =>
    string.IsNullOrWhiteSpace(Width) ? null : $"width:{Width};";

  protected string? SpellCheckAttribute =>
    SpellCheck switch {
      true => "true",
      false => "false",
      null => null
    };

  /// <summary>
  /// Puts the focus in the box
  /// </summary>
  public ValueTask FocusAsync() =>
    InputElement.FocusAsync();

  protected override void OnParametersSet() {
    base.OnParametersSet();
    // Only replaces the text when the value changes from outside, so a debounced value arriving late doesn't undo typing
    if (Value != _lastValue) {
      _lastValue = Value;
      Text = Value ?? "";
      OnTextSetFromOutside();
    }
  }

  /// <summary>
  /// Called when <c>Value</c> has been set from outside, replacing what was in the box
  /// </summary>
  protected virtual void OnTextSetFromOutside() { }

  protected async Task OnInput(ChangeEventArgs e) {
    Text = e.Value?.ToString() ?? "";
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

  // The browser's change event, which comes when the user leaves the box (or presses Enter in a text box) after changing
  // the text
  protected async Task OnInputChange(ChangeEventArgs e) {
    Text = e.Value?.ToString() ?? "";
    await Commit();
    await OnChange.InvokeAsync(Value);
  }

  protected async Task OnInputBlur(FocusEventArgs e) {
    await Commit();
    await OnBlur.InvokeAsync();
  }

  /// <summary>
  /// Sets <c>Value</c> to <see cref="Text"/> now, cancelling any debounced change that's waiting
  /// </summary>
  protected async Task Commit() {
    CancelDebounce();
    _lastValue = Text;
    await UpdateValue(Text);
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
