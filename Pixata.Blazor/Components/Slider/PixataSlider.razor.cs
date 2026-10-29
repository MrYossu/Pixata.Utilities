using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A slider for picking a number between <see cref="Min"/> and <see cref="Max"/>. It has the same parameters as Telerik's
/// Slider. <typeparamref name="TValue"/> can be <see cref="int"/>, <see cref="long"/>, <see cref="short"/>,
/// <see cref="decimal"/>, <see cref="double"/> or <see cref="float"/>
/// </summary>
public partial class PixataSlider<TValue> {
  // More ticks than this are too close together to be useful, so only the large ones are drawn
  private const int MaxTicks = 100;

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The lowest value. Defaults to 0
  /// </summary>
  [Parameter]
  public TValue Min { get; set; } = default!;

  /// <summary>
  /// The highest value. Defaults to 10
  /// </summary>
  [Parameter]
  public TValue Max { get; set; } = default!;

  /// <summary>
  /// How far the arrow keys and the buttons move the value, and the gap between the small ticks. Defaults to 1
  /// </summary>
  [Parameter]
  public TValue SmallStep { get; set; } = default!;

  /// <summary>
  /// How far Page Up and Page Down move the value, and the gap between the large (labelled) ticks. Defaults to 5
  /// </summary>
  [Parameter]
  public TValue LargeStep { get; set; } = default!;

  /// <summary>
  /// The number of decimal places the value is rounded to. Whole number types are always whole numbers
  /// </summary>
  [Parameter]
  public int? Decimals { get; set; }

  /// <summary>
  /// Fires when the user has finished changing the value: when they let go of the thumb, press a key or click a button.
  /// <c>ValueChanged</c> fires as well, and also while the thumb is being dragged. The argument is the new value
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  /// <summary>
  /// Shows the decrease and increase buttons at the ends. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowButtons { get; set; } = true;

  /// <summary>
  /// The tooltip and accessible name of the decrease button, for translating. Defaults to "Decrease"
  /// </summary>
  [Parameter]
  public string DecreaseButtonText { get; set; } = "Decrease";

  /// <summary>
  /// The tooltip and accessible name of the increase button, for translating. Defaults to "Increase"
  /// </summary>
  [Parameter]
  public string IncreaseButtonText { get; set; } = "Increase";

  /// <summary>
  /// Where the ticks go: on both sides of the track (the default), before it (above a horizontal slider), after it, or
  /// nowhere
  /// </summary>
  [Parameter]
  public PixataSliderTickPosition TickPosition { get; set; } = PixataSliderTickPosition.Both;

  /// <summary>
  /// Shows each large tick's label. The context is the tick's value. By default it's the value as a plain number
  /// </summary>
  [Parameter]
  public RenderFragment<TValue>? LabelTemplate { get; set; }

  /// <summary>
  /// Horizontal (the default) or vertical, with the lowest value at the bottom
  /// </summary>
  [Parameter]
  public PixataSliderOrientation Orientation { get; set; } = PixataSliderOrientation.Horizontal;

  /// <summary>
  /// When true, the value can be seen and the slider focused, but the user can't change it
  /// </summary>
  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// The width of a horizontal slider, as a CSS width. By default it fills the width it's given
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The height of a vertical slider, as a CSS height. Defaults to 200px
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public string? AriaLabelledBy { get; set; }

  private static readonly Type UnderlyingType = typeof(TValue);
  private static readonly bool IsWholeNumber = UnderlyingType == typeof(int) || UnderlyingType == typeof(long) || UnderlyingType == typeof(short);
  private static readonly Type[] SupportedTypes = [typeof(int), typeof(long), typeof(short), typeof(decimal), typeof(double), typeof(float)];

  private ElementReference _input;
  // TValue's default (0) can't tell us whether these were set, so this is how we know
  private bool _hasMax;
  private bool _hasSmallStep;
  private bool _hasLargeStep;

  private decimal MinNumber =>
    ToNumber(Min);

  private decimal MaxNumber =>
    _hasMax ? ToNumber(Max) : 10;

  private decimal SmallStepNumber =>
    _hasSmallStep && ToNumber(SmallStep) > 0 ? ToNumber(SmallStep) : 1;

  private decimal LargeStepNumber =>
    _hasLargeStep && ToNumber(LargeStep) > 0 ? ToNumber(LargeStep) : 5;

  private decimal CurrentNumber =>
    ToNumber(Value);

  private int? EffectiveDecimals =>
    IsWholeNumber ? 0 : Decimals;

  private string CssClass =>
    PixataCss.Classes("pixata-blazor-slider",
      Orientation == PixataSliderOrientation.Vertical ? "pixata-blazor-slider-vertical" : "pixata-blazor-slider-horizontal",
      ReadOnly ? "pixata-blazor-slider-readonly" : null,
      PixataCss.Disabled(Enabled),
      InvalidClass,
      Class);

  private string? SizeStyle =>
    Orientation == PixataSliderOrientation.Vertical
      ? string.IsNullOrWhiteSpace(Height) ? null : $"height:{Height};"
      : string.IsNullOrWhiteSpace(Width) ? null : $"width:{Width};";

  public override Task SetParametersAsync(ParameterView parameters) {
    _hasMax |= parameters.TryGetValue(nameof(Max), out TValue? _);
    _hasSmallStep |= parameters.TryGetValue(nameof(SmallStep), out TValue? _);
    _hasLargeStep |= parameters.TryGetValue(nameof(LargeStep), out TValue? _);
    return base.SetParametersAsync(parameters);
  }

  protected override void OnInitialized() {
    if (!SupportedTypes.Contains(UnderlyingType)) {
      throw new InvalidOperationException($"PixataSlider only works with int, long, short, decimal, double and float, not {typeof(TValue).Name}");
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      await PixataJs.InvokeVoid(Js, "initSlider", _input);
    }
  }

  /// <summary>
  /// Puts the focus on the slider
  /// </summary>
  public ValueTask FocusAsync() =>
    _input.FocusAsync();

  private async Task OnInput(ChangeEventArgs e) {
    // A read-only slider can't be dragged (its CSS) or moved with the keys (its script), so this is only belt and braces
    if (ReadOnly || !TryParse(e.Value, out decimal number)) {
      return;
    }
    await SetValue(number);
  }

  private async Task OnInputChange(ChangeEventArgs e) {
    if (ReadOnly || !TryParse(e.Value, out decimal number)) {
      return;
    }
    await SetValue(number);
    await OnChange.InvokeAsync(Value);
  }

  private async Task Spin(int direction) {
    if (!Enabled || ReadOnly) {
      return;
    }
    if (await SetValue(CurrentNumber + direction * SmallStepNumber)) {
      await OnChange.InvokeAsync(Value);
    }
  }

  private async Task<bool> SetValue(decimal number) {
    decimal clamped = Math.Clamp(number, MinNumber, Math.Max(MinNumber, MaxNumber));
    decimal rounded = EffectiveDecimals is int decimals ? Math.Round(clamped, decimals, MidpointRounding.AwayFromZero) : clamped;
    return await UpdateValue(FromNumber(rounded));
  }

  private decimal Fraction(decimal number) =>
    MaxNumber <= MinNumber ? 0 : Math.Clamp((number - MinNumber) / (MaxNumber - MinNumber), 0, 1);

  private IEnumerable<Tick> Ticks() {
    decimal range = MaxNumber - MinNumber;
    if (range <= 0) {
      yield break;
    }
    // Ticks are drawn every SmallStep if there's room, otherwise every LargeStep, and not at all if there isn't room for those
    decimal step = range / SmallStepNumber <= MaxTicks ? SmallStepNumber : LargeStepNumber;
    if (range / step > MaxTicks) {
      yield break;
    }
    for (decimal value = MinNumber; value <= MaxNumber; value += step) {
      bool large = (value - MinNumber) % LargeStepNumber == 0;
      yield return new(value, Fraction(value), large);
    }
  }

  private static bool TryParse(object? value, out decimal number) =>
    decimal.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number);

  // The range input's attributes always use a dot for the decimal point, whatever the culture
  private static string Invariant(decimal value) =>
    value.ToString("G29", CultureInfo.InvariantCulture);

  private static decimal ToNumber(TValue value) {
    try {
      return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }
    catch (OverflowException) {
      // A double or float too big (or NaN) to be a decimal
      return 0;
    }
  }

  private static TValue FromNumber(decimal number) {
    decimal rounded = IsWholeNumber ? Math.Round(number, 0, MidpointRounding.AwayFromZero) : number;
    return (TValue)Convert.ChangeType(rounded, UnderlyingType, CultureInfo.InvariantCulture);
  }

  private record Tick(decimal Value, decimal Fraction, bool Large);
}

/// <summary>
/// Where a <see cref="PixataSlider{TValue}"/>'s ticks go
/// </summary>
public enum PixataSliderTickPosition {
  /// <summary>
  /// On both sides of the track, with the labels after it
  /// </summary>
  Both,

  /// <summary>
  /// Above a horizontal slider, or to the left of a vertical one
  /// </summary>
  Before,

  /// <summary>
  /// Below a horizontal slider, or to the right of a vertical one
  /// </summary>
  After,

  /// <summary>
  /// No ticks or labels
  /// </summary>
  None
}

/// <summary>
/// Which way a <see cref="PixataSlider{TValue}"/> goes
/// </summary>
public enum PixataSliderOrientation {
  Horizontal,

  /// <summary>
  /// Upright, with the lowest value at the bottom
  /// </summary>
  Vertical
}
