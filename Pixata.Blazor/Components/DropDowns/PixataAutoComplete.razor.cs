using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A replacement for Telerik's AutoComplete: a text box that suggests values as the user types. Whatever the user types is the
/// value (a <c>string</c>), which is updated as they type, and they can take a suggestion from the list instead. The suggestions
/// are <c>Data</c> (strings, or items whose <see cref="PixataDropDownBase{TItem, TValue, TBound}.ValueField"/> holds the
/// text), or come from <c>OnRead</c>
/// </summary>
/// <typeparam name="TItem">The type of the suggestions</typeparam>
public partial class PixataAutoComplete<TItem> {
  /// <summary>
  /// The suggestions are filtered by what the user types, unlike Telerik's AutoComplete, where <c>Filterable</c> is false by
  /// default
  /// </summary>
  public PixataAutoComplete() =>
    Filterable = true;

  /// <summary>
  /// The hint shown in the text box when it is empty
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  /// <summary>
  /// Whether to show a button that clears the text. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; } = true;

  /// <summary>
  /// The clear button's tooltip and accessible name, for translating. Defaults to "Clear"
  /// </summary>
  [Parameter]
  public string ClearText { get; set; } = "Clear";

  /// <summary>
  /// How long (in milliseconds) to wait after the user stops typing before updating <c>Value</c> and raising <c>OnRead</c>.
  /// Defaults to 150. Set it to 0 to update the value on every key
  /// </summary>
  [Parameter]
  public int DebounceDelay { get; set; } = 150;

  /// <summary>
  /// The number of characters the user must type before the suggestions are shown. Defaults to 1, and can't be less
  /// </summary>
  [Parameter]
  public int MinLength { get; set; } = 1;

  private ElementReference _input;
  // What is in the text box, as far as the component knows
  private string _text = "";
  // The text rendered into the text box, which only changes when the code sets the text, not as the user types
  private string _shownText = "";
  // The value when OnChange was last raised (or the value was set by the code), so OnChange is only raised for a change
  private string? _lastChange;
  private bool _initialised;
  private CancellationTokenSource? _valueDebounce;
  private bool _valuePending;

  private protected override string ComponentName => "autocomplete";

  private protected override ElementReference FocusTarget => _input;

  private protected override int FilterDelay => DebounceDelay;

  private protected override bool FocusFirstItemAfterFilter => false;

  private int MinimumLength =>
    Math.Max(1, MinLength);

  // A suggestion is shown as selected when it is what is in the text box
  private protected override bool IsItemSelected(TItem item) =>
    _text != "" && TextOf(item) == _text;

  // Nothing is focused when the list opens, so Enter keeps what the user typed until they move to a suggestion
  private protected override int InitialFocusedPosition() => -1;

  protected override Task OnParametersSetAsync() {
    // The suggestions' text is in ValueField, as in Telerik's AutoComplete, which has no TextField
    TextField = ValueField;
    return base.OnParametersSetAsync();
  }

  private protected override Task OnParametersApplied(bool dataChanged) {
    string value = Value ?? "";
    if (!_initialised) {
      _initialised = true;
      _lastChange = Value;
      SetText(value);
    } else if (!_valuePending && value != _text) {
      // The code has set the value
      _lastChange = Value;
      SetText(value);
    }
    return Task.CompletedTask;
  }

  /// <summary>
  /// Sets the text in the text box from code (what the user types only goes in <see cref="_text"/>)
  /// </summary>
  private void SetText(string text) {
    if (text != _text) {
      WriteTextAfterRender(() => _input, text, _text);
    }
    _text = text;
    _shownText = text;
  }

  private protected override async Task ChooseItem(TItem item) {
    CancelValueUpdate();
    SetText(TextOf(item));
    await UpdateValue(_text);
    await ClosePopup();
    await RaiseChange();
  }

  private protected override async Task EnterWithoutFocusedOption() {
    await ClosePopup();
    await Commit();
  }

  private protected override Task OnFocusLost() =>
    Commit();

  /// <summary>
  /// Makes sure the value is what is in the text box, without waiting for <see cref="DebounceDelay"/>, and raises
  /// <c>OnChange</c> if it has changed
  /// </summary>
  private async Task Commit() {
    if (_valuePending) {
      CancelValueUpdate();
      await UpdateValue(_text);
    }
    await RaiseChange();
  }

  private async Task RaiseChange() {
    if (Value != _lastChange) {
      _lastChange = Value;
      await OnChange.InvokeAsync(Value);
    }
  }

  private void CancelValueUpdate() {
    _valuePending = false;
    _valueDebounce?.Cancel();
    _valueDebounce?.Dispose();
    _valueDebounce = null;
  }

  /// <summary>
  /// Updates the value to what the user has typed, once they have stopped typing for <see cref="DebounceDelay"/>
  /// </summary>
  private async Task UpdateValueAfterDelay() {
    CancelValueUpdate();
    if (DebounceDelay <= 0) {
      await UpdateValue(_text);
      return;
    }
    _valuePending = true;
    _valueDebounce = new();
    CancellationToken token = _valueDebounce.Token;
    try {
      await Task.Delay(DebounceDelay, token);
    }
    catch (TaskCanceledException) {
      return;
    }
    if (token.IsCancellationRequested) {
      return;
    }
    _valuePending = false;
    await UpdateValue(_text);
  }

  private async Task HandleInput(ChangeEventArgs e) {
    _text = e.Value as string ?? "";
    // The value and the suggestions wait for the same delay, so they run side by side
    Task valueUpdate = UpdateValueAfterDelay();
    await ShowSuggestions();
    await valueUpdate;
  }

  /// <summary>
  /// Opens the list of suggestions for the text, or closes it if the text is too short
  /// </summary>
  private async Task ShowSuggestions() {
    if (_text.Length < MinimumLength) {
      await ClosePopup(returnFocus: false);
      return;
    }
    if (!IsOpen && !await OpenPopup()) {
      return;
    }
    if (Filterable) {
      await SetFilter(_text);
    }
  }

  private async Task HandleKeyDown(KeyboardEventArgs e) {
    if (!Enabled || ReadOnly) {
      return;
    }
    if (IsOpen) {
      // Home and End move the caret in the text box, rather than moving around the list
      if (e.Key is not ("Home" or "End")) {
        await HandleOpenListKey(e);
      }
      return;
    }
    switch (e.Key) {
      case "ArrowDown":
        await ShowSuggestions();
        break;
      case "Enter":
        await Commit();
        break;
    }
  }

  private async Task HandleClick() {
    if (Enabled) {
      await FocusAsync();
    }
  }

  private async Task HandleClear() {
    CancelValueUpdate();
    SetText("");
    await UpdateValue("");
    await ClosePopup();
    await RaiseChange();
    await FocusAsync();
  }

  protected override void Dispose(bool disposing) {
    if (disposing) {
      CancelValueUpdate();
    }
    base.Dispose(disposing);
  }
}
