using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A text box for numbers, with optional spin buttons, minimum and maximum values, rounding and a display format. It has
/// the same parameters as Telerik's NumericTextBox. <typeparamref name="TValue"/> can be <see cref="int"/>,
/// <see cref="long"/>, <see cref="short"/>, <see cref="decimal"/>, <see cref="double"/>, <see cref="float"/> or a
/// nullable version of any of them
/// </summary>
public partial class PixataNumericTextBox<TValue> : IDisposable {
  [CascadingParameter]
  private EditContext? EditContext { get; set; }

  [Parameter]
  public TValue? Value { get; set; }

  [Parameter]
  public EventCallback<TValue> ValueChanged { get; set; }

  /// <summary>
  /// Identifies the bound field for validation. Set automatically when you use <c>@bind-Value</c>
  /// </summary>
  [Parameter]
  public Expression<Func<TValue>>? ValueExpression { get; set; }

  /// <summary>
  /// Fires when the user has finished changing the value, which is when they press Enter or leave the input, rather than on
  /// every key. The argument is the new value
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  /// <summary>
  /// Fires when the input loses focus
  /// </summary>
  [Parameter]
  public EventCallback OnBlur { get; set; }

  /// <summary>
  /// The lowest value allowed. A lower value is raised to this when the user leaves the input
  /// </summary>
  [Parameter]
  public TValue? Min { get; set; }

  /// <summary>
  /// The highest value allowed. A higher value is lowered to this when the user leaves the input
  /// </summary>
  [Parameter]
  public TValue? Max { get; set; }

  /// <summary>
  /// How much the spin buttons and the up and down arrow keys change the value by. Defaults to 1
  /// </summary>
  [Parameter]
  public TValue? Step { get; set; }

  /// <summary>
  /// The number of decimal places the value is rounded to when the user leaves the input. Whole number types are always
  /// rounded to whole numbers
  /// </summary>
  [Parameter]
  public int? Decimals { get; set; }

  /// <summary>
  /// A .NET number format, such as "N2" or "C2", used to show the value when the input doesn't have focus. While it has
  /// focus, the plain number is shown, so it's easy to edit
  /// </summary>
  [Parameter]
  public string? Format { get; set; }

  /// <summary>
  /// Shows the up and down spin buttons. Defaults to true
  /// </summary>
  [Parameter]
  public bool Arrows { get; set; } = true;

  /// <summary>
  /// Shown when the input is empty
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  [Parameter]
  public bool Enabled { get; set; } = true;

  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// The width, as a CSS width
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// The id of the input, for a label's <c>for</c>
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  /// <summary>
  /// The <c>inputmode</c> of the input, which picks the keyboard on a phone. Defaults to "decimal", or "numeric" for whole
  /// number types
  /// </summary>
  [Parameter]
  public string? InputMode { get; set; }

  /// <summary>
  /// Any other attributes, which are added to the input
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public Dictionary<string, object>? AdditionalAttributes { get; set; }

  private static readonly Type UnderlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
  private static readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;
  private static readonly bool IsWholeNumber = UnderlyingType == typeof(int) || UnderlyingType == typeof(long) || UnderlyingType == typeof(short);
  private static readonly Type[] SupportedTypes = [typeof(int), typeof(long), typeof(short), typeof(decimal), typeof(double), typeof(float)];

  // For a non-nullable TValue, Min, Max and Step can't be null, so this is how we know whether they were set at all
  private bool _hasMin;
  private bool _hasMax;
  private bool _hasStep;
  private string _text = "";
  private bool _focused;
  private bool _invalidText;
  private decimal? _lastValue;
  private decimal? _valueAtFocus;
  private FieldIdentifier? _field;

  private decimal? MinNumber =>
    _hasMin ? ToNumber(Min) : null;

  private decimal? MaxNumber =>
    _hasMax ? ToNumber(Max) : null;

  private decimal? CurrentNumber =>
    ToNumber(Value);

  private bool IsInvalid =>
    _invalidText || (_field is FieldIdentifier fieldId && EditContext!.GetValidationMessages(fieldId).Any());

  private string WrapperClass =>
    string.Join(" ", new[] {
      "pixata-blazor-numerictextbox",
      $"pixata-blazor-numerictextbox-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}",
      IsInvalid ? "pixata-blazor-numerictextbox-invalid" : null,
      Enabled ? null : "pixata-blazor-numerictextbox-disabled",
      Class
    }.Where(c => !string.IsNullOrWhiteSpace(c)));

  private string? WidthStyle =>
    Width is null ? null : $"width: {Width}";

  private string EffectiveInputMode =>
    InputMode ?? (IsWholeNumber ? "numeric" : "decimal");

  private int? EffectiveDecimals =>
    IsWholeNumber ? 0 : Decimals;

  public override Task SetParametersAsync(ParameterView parameters) {
    _hasMin |= parameters.TryGetValue(nameof(Min), out TValue? _);
    _hasMax |= parameters.TryGetValue(nameof(Max), out TValue? _);
    _hasStep |= parameters.TryGetValue(nameof(Step), out TValue? _);
    return base.SetParametersAsync(parameters);
  }

  protected override void OnInitialized() {
    if (!SupportedTypes.Contains(UnderlyingType)) {
      throw new InvalidOperationException($"PixataNumericTextBox only works with int, long, short, decimal, double, float and their nullable versions, not {typeof(TValue).Name}");
    }
    if (EditContext is not null && ValueExpression is not null) {
      _field = FieldIdentifier.Create(ValueExpression);
      // So the invalid style shows as soon as the form is validated, eg when it's submitted
      EditContext.OnValidationStateChanged += OnValidationStateChanged;
    }
  }

  private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) =>
    InvokeAsync(StateHasChanged);

  public void Dispose() {
    if (_field is not null && EditContext is not null) {
      EditContext.OnValidationStateChanged -= OnValidationStateChanged;
    }
  }

  protected override void OnParametersSet() {
    // A value set from outside (rather than typed) replaces whatever is in the input. Without focus, the text is always
    // the formatted value, so a change of Format shows straight away
    decimal? current = CurrentNumber;
    if (!_focused) {
      _lastValue = current;
      _text = DisplayText();
      _invalidText = false;
    } else if (current != _lastValue) {
      _lastValue = current;
      _text = EditText(current);
      _invalidText = false;
    }
  }

  private void OnFocus() {
    _focused = true;
    _valueAtFocus = CurrentNumber;
    _text = EditText(_valueAtFocus);
  }

  private async Task OnInputBlur(FocusEventArgs e) {
    _focused = false;
    await Normalise();
    _text = DisplayText();
    _invalidText = false;
    await OnBlur.InvokeAsync();
    await RaiseChangeIfNeeded();
  }

  private async Task OnInput(ChangeEventArgs e) {
    _text = e.Value?.ToString() ?? "";
    if (_text.Trim() == "") {
      _invalidText = !IsNullable;
      if (IsNullable) {
        await SetValue(null);
      }
      return;
    }
    if (TryParse(_text, out decimal parsed) && TryConvert(parsed, out _)) {
      _invalidText = false;
      await SetValue(parsed);
    } else {
      _invalidText = true;
    }
  }

  private async Task OnKeyDown(KeyboardEventArgs e) {
    if (!Enabled || ReadOnly) {
      return;
    }
    switch (e.Key) {
      case "ArrowUp":
        await Spin(1);
        break;
      case "ArrowDown":
        await Spin(-1);
        break;
      case "Enter":
        await Normalise();
        _text = EditText(CurrentNumber);
        await RaiseChangeIfNeeded();
        break;
    }
  }

  // Blazor only stops the button taking the focus from the input (with preventDefault) if the event has a handler
  private static void KeepFocus() { }

  private async Task Spin(int direction) {
    if (!Enabled || ReadOnly) {
      return;
    }
    decimal step = _hasStep ? ToNumber(Step) ?? 1 : 1;
    decimal value = Constrain((CurrentNumber ?? Clamp(0)) + direction * step);
    _invalidText = false;
    await SetValue(value);
    _text = _focused ? EditText(value) : DisplayText();
    if (!_focused) {
      await RaiseChangeIfNeeded();
    }
  }

  // Applies Min, Max and Decimals, which are left alone while the user is typing, so that typing "1" on the way to "15"
  // isn't raised to a minimum of 10
  private async Task Normalise() {
    if (CurrentNumber is decimal value) {
      await SetValue(Constrain(value));
    }
  }

  private async Task RaiseChangeIfNeeded() {
    decimal? current = CurrentNumber;
    if (current != _valueAtFocus) {
      _valueAtFocus = current;
      await OnChange.InvokeAsync(Value);
    }
  }

  private async Task SetValue(decimal? number) {
    TValue? value = FromNumber(number);
    if (!EqualityComparer<TValue>.Default.Equals(value, Value)) {
      Value = value;
      _lastValue = ToNumber(value);
      await ValueChanged.InvokeAsync(value!);
      if (_field is FieldIdentifier fieldId) {
        EditContext!.NotifyFieldChanged(fieldId);
      }
    }
  }

  private decimal Constrain(decimal value) {
    decimal clamped = Clamp(value);
    return EffectiveDecimals is int decimals ? Math.Round(clamped, decimals, MidpointRounding.AwayFromZero) : clamped;
  }

  private decimal Clamp(decimal value) {
    if (MinNumber is decimal min && value < min) {
      return min;
    }
    if (MaxNumber is decimal max && value > max) {
      return max;
    }
    return value;
  }

  private string DisplayText() =>
    Value is null
      ? ""
      : Format is null
        ? EditText(CurrentNumber)
        : ((IFormattable)Value).ToString(Format, CultureInfo.CurrentCulture);

  // G29 drops trailing zeros, so 23751.0 shows as 23751, and there are no thousands separators to get in the way
  private static string EditText(decimal? value) =>
    value?.ToString("G29", CultureInfo.CurrentCulture) ?? "";

  // Accepts what a user might paste, such as "£1,234.50", as well as plain numbers
  private static bool TryParse(string text, out decimal value) =>
    decimal.TryParse(text.Trim(), NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out value);

  private static decimal? ToNumber(TValue? value) {
    if (value is null) {
      return null;
    }
    try {
      return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }
    catch (OverflowException) {
      // A double or float too big (or NaN) to be a decimal
      return null;
    }
  }

  private static TValue? FromNumber(decimal? number) =>
    number is decimal value && TryConvert(value, out TValue? converted) ? converted : default;

  private static bool TryConvert(decimal value, out TValue? converted) {
    try {
      decimal rounded = IsWholeNumber ? Math.Round(value, 0, MidpointRounding.AwayFromZero) : value;
      converted = (TValue)Convert.ChangeType(rounded, UnderlyingType, CultureInfo.InvariantCulture);
      return true;
    }
    catch (OverflowException) {
      converted = default;
      return false;
    }
  }
}
