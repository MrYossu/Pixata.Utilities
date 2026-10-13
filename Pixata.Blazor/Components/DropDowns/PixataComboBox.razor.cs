using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A combo box: a text box with a list. The user picks an item from the list, or types to find one. With
/// <see cref="AllowCustom"/>, they can also type a value that isn't in the list. Bind <c>Value</c> to the selected item's value
/// (the field named by <c>ValueField</c>)
/// </summary>
/// <typeparam name="TItem">The type of the items</typeparam>
/// <typeparam name="TValue">The type of the value</typeparam>
public partial class PixataComboBox<TItem, TValue> {
  /// <summary>
  /// The hint shown in the text box when it is empty
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  /// <summary>
  /// Whether the user can type a value that isn't in the list. The typed text becomes the value, so <c>TValue</c> should be
  /// <c>string</c> (or a type the text can be converted to)
  /// </summary>
  [Parameter]
  public bool AllowCustom { get; set; }

  /// <summary>
  /// Whether to show a button that clears the value. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; } = true;

  /// <summary>
  /// How long (in milliseconds) to wait after the user stops typing before raising <c>OnRead</c>. Defaults to 150
  /// </summary>
  [Parameter]
  public int DebounceDelay { get; set; } = 150;

  private ElementReference _input;
  private string _text = "";
  private string _shownText = "";
  private bool _editing;

  private protected override string ComponentName => "combobox";

  private protected override ElementReference FocusTarget => _input;

  private protected override int FilterDelay => DebounceDelay;

  private protected override async Task OnParametersApplied(bool dataChanged) {
    await base.OnParametersApplied(dataChanged);
    if (!_editing) {
      SyncText();
    }
  }

  /// <summary>
  /// Shows the selected item's text (or the custom value) in the text box
  /// </summary>
  private void SyncText() =>
    SetText(HasSelectedItem
      ? TextOf(SelectedItem!)
      : AllowCustom && !IsEmptyValue(Value) ? Convert.ToString(Value, CultureInfo.CurrentCulture) ?? "" : "");

  /// <summary>
  /// Sets the text in the text box from code (what the user types only goes in <see cref="_text"/>)
  /// </summary>
  private void SetText(string text) {
    if (text != _text) {
      WriteTextAfterRender(() => _input, text);
    }
    _text = text;
    _shownText = text;
  }

  private protected override async Task ChooseItem(TItem item) {
    _editing = false;
    await SelectItem(item);
    SyncText();
    await ClosePopup();
  }

  private protected override Task EnterWithoutFocusedOption() =>
    Commit();

  private protected override Task OnFocusLost() =>
    Commit();

  /// <summary>
  /// Turns what the user typed into the value: the item with that text, the text itself if custom values are allowed, or
  /// nothing
  /// </summary>
  private async Task Commit() {
    if (!_editing) {
      return;
    }
    _editing = false;
    string text = _text;
    if (text != "" && FindItemWithText(text) is (true, TItem match)) {
      await SelectItem(match);
    } else if (text != "" && AllowCustom && TryConvert(text, out TValue custom)) {
      await SetValueWithoutItem(custom);
    } else {
      await SetValueWithoutItem(default!);
    }
    SyncText();
    await ClosePopup(true, false);
  }

  private (bool Found, TItem Item) FindItemWithText(string text) {
    foreach (TItem item in KnownItems) {
      if (PixataStringFilterOperator.IsEqualTo.Matches(TextOf(item), text)) {
        return (true, item);
      }
    }
    return (false, default!);
  }

  private static bool TryConvert(string text, out TValue value) {
    try {
      value = PixataFieldReader.ConvertTo<TValue>(text)!;
      return true;
    }
    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException) {
      value = default!;
      return false;
    }
  }

  private async Task HandleInput(ChangeEventArgs e) {
    _text = e.Value as string ?? "";
    _editing = true;
    if (!IsOpen) {
      await OpenPopup();
    }
    if (Filterable) {
      await SetFilter(_text);
      if (_text == "") {
        ClearFocusedPosition();
      }
    } else {
      FocusFirstMatch(_text);
    }
  }

  private async Task HandleKeyDown(KeyboardEventArgs e) {
    if (!Enabled || ReadOnly) {
      return;
    }
    if (IsOpen) {
      await HandleOpenListKey(e);
      return;
    }
    switch (e.Key) {
      case "ArrowDown" when e.AltKey:
        await OpenPopup();
        break;
      case "ArrowDown":
      case "ArrowUp":
        _editing = false;
        await SelectRelative(e.Key == "ArrowDown" ? 1 : -1);
        SyncText();
        break;
      case "Enter":
        await Commit();
        break;
      case "Escape" when _editing:
        _editing = false;
        SyncText();
        break;
    }
  }

  private async Task HandleClick() {
    if (Enabled) {
      await FocusAsync();
    }
  }

  private async Task HandleButtonClick() {
    if (!Enabled || ReadOnly) {
      return;
    }
    await FocusAsync();
    await TogglePopup();
  }

  private async Task HandleClear() {
    SetText("");
    _editing = false;
    await SetValueWithoutItem(default!);
    await ClosePopup();
    await FocusAsync();
  }
}
