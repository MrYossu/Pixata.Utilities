using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A toggle switch for a <c>bool</c> or <c>bool?</c> value
/// </summary>
/// <typeparam name="TValue"><c>bool</c> or <c>bool?</c></typeparam>
public partial class PixataSwitch<TValue> {
  // How far (in CSS pixels) the pointer has to move between going down and coming up to count as a swipe rather than a tap
  private const double SwipeDistance = 10;

  private static readonly bool IsSupportedType = typeof(TValue) == typeof(bool) || typeof(TValue) == typeof(bool?);

  /// <summary>
  /// The text shown in the track when the switch is on. Defaults to "On"
  /// </summary>
  [Parameter]
  public string OnLabel { get; set; } = "On";

  /// <summary>
  /// The text shown in the track when the switch is off. Defaults to "Off"
  /// </summary>
  [Parameter]
  public string OffLabel { get; set; } = "Off";

  /// <summary>
  /// The width of the switch, as a CSS length (eg <c>"80px"</c>). By default it depends on <see cref="Size"/>
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// The roundness of both the track and the thumb, unless <see cref="TrackRounded"/> or <see cref="ThumbRounded"/> say
  /// otherwise: <c>none</c>, <c>sm</c>, <c>md</c>, <c>lg</c> or <c>full</c> (the default). See <see cref="PixataThemeConstants.Rounded"/>
  /// </summary>
  [Parameter]
  public string Rounded { get; set; } = PixataThemeConstants.Rounded.Full;

  /// <summary>
  /// The roundness of the track. Defaults to <see cref="Rounded"/>
  /// </summary>
  [Parameter]
  public string? TrackRounded { get; set; }

  /// <summary>
  /// The roundness of the thumb (the part that slides). Defaults to <see cref="Rounded"/>
  /// </summary>
  [Parameter]
  public string? ThumbRounded { get; set; }

  /// <summary>
  /// When true, the switch shows its value and can be focused, but the user can't change it
  /// </summary>
  [Parameter]
  public bool ReadOnly { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public string? AriaLabelledBy { get; set; }

  [Parameter]
  public string? AriaDescribedBy { get; set; }

  /// <summary>
  /// Raised when the user changes the value, with the new value (a <c>bool</c>)
  /// </summary>
  [Parameter]
  public EventCallback<object> OnChange { get; set; }

  private double? _pointerDownX;
  private bool _ignoreNextClick;

  /// <summary>
  /// True when the value is true. False and null are both shown as off
  /// </summary>
  private bool IsOn =>
    Value is true;

  private string CssClass =>
    PixataCss.Classes("pixata-blazor-switch",
      $"pixata-blazor-switch-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}",
      IsOn ? "pixata-blazor-switch-on" : "pixata-blazor-switch-off",
      ReadOnly ? "pixata-blazor-switch-readonly" : null,
      PixataCss.Disabled(Enabled),
      InvalidClass,
      Class);

  private string? Style =>
    string.IsNullOrWhiteSpace(Width) ? null : $"--pixata-blazor-switch-width:{Width};";

  protected override void OnParametersSet() {
    if (!IsSupportedType) {
      throw new InvalidOperationException($"PixataSwitch only works with bool and bool? values, not {typeof(TValue).Name}");
    }
    base.OnParametersSet();
  }

  private async Task HandleClick(MouseEventArgs args) {
    bool afterSwipe = _ignoreNextClick;
    _ignoreNextClick = false;
    // The click that follows a swipe, which has already set the value, comes from the pointer. A click from the keyboard or
    // a label has a detail of 0, and always toggles, even after a swipe on a touch screen that wasn't followed by a click
    if (afterSwipe && args.Detail > 0) {
      return;
    }
    await SetOn(!IsOn);
  }

  private void HandlePointerDown(PointerEventArgs args) {
    _pointerDownX = args.ClientX;
    // A swipe on a touch screen isn't always followed by a click, so forget about any earlier swipe
    _ignoreNextClick = false;
  }

  private async Task HandlePointerUp(PointerEventArgs args) {
    if (_pointerDownX is not { } start) {
      return;
    }
    _pointerDownX = null;
    double distance = args.ClientX - start;
    if (Math.Abs(distance) < SwipeDistance) {
      // A tap, which the click handler deals with
      return;
    }
    _ignoreNextClick = true;
    await SetOn(distance > 0);
  }

  private async Task SetOn(bool on) {
    if (!Enabled || ReadOnly) {
      return;
    }
    if (await UpdateValue((TValue)(object)on)) {
      await OnChange.InvokeAsync(on);
    }
  }
}
