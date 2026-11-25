using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A replacement for Telerik's ColorPicker: a button showing the colour, which opens a popup with a gradient (a square of
/// shades, a hue slider, an opacity slider and boxes to type the colour) and a palette. The value is a CSS colour string,
/// written in <see cref="ValueFormat"/>
/// </summary>
public partial class PixataColorPicker {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// How the value is written: <see cref="PixataColorFormat.Rgb"/> (the default, as in Telerik), such as
  /// <c>rgb(40, 47, 137)</c>, or <see cref="PixataColorFormat.Hex"/>, such as <c>#282f89</c>. A colour that isn't opaque is
  /// <c>rgba(...)</c> or <c>#rrggbbaa</c>. A value set from outside is kept as it is until the user picks another
  /// </summary>
  [Parameter]
  public PixataColorFormat ValueFormat { get; set; } = PixataColorFormat.Rgb;

  /// <summary>
  /// The view the popup opens in. Defaults to <see cref="PixataColorPickerView.Gradient"/>. Can be two-way bound
  /// </summary>
  [Parameter]
  public PixataColorPickerView View { get; set; } = PixataColorPickerView.Gradient;

  [Parameter]
  public EventCallback<PixataColorPickerView> ViewChanged { get; set; }

  /// <summary>
  /// The views the popup offers, as <see cref="PixataColorPickerGradientView"/> and <see cref="PixataColorPickerPaletteView"/>,
  /// in the order their buttons appear. Without it, the popup has both
  /// </summary>
  [Parameter]
  public RenderFragment? ColorPickerViews { get; set; }

  /// <summary>
  /// The colours in the palette, unless a <see cref="PixataColorPickerPaletteView"/> sets its own. Defaults to
  /// <see cref="DefaultPaletteColors"/>
  /// </summary>
  [Parameter]
  public IEnumerable<string>? PaletteColors { get; set; }

  /// <summary>
  /// Whether the popup has Apply and Cancel buttons. With them (the default), the value only changes when the user clicks
  /// Apply. Without them, it changes as the user picks
  /// </summary>
  [Parameter]
  public bool ShowButtons { get; set; } = true;

  /// <summary>
  /// Whether the popup has a button that clears the colour, making the value null. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; } = true;

  /// <summary>
  /// Whether the popup shows the new colour next to the current one, which can be clicked to go back to it. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowPreview { get; set; } = true;

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

  /// <summary>
  /// A CSS class for the popup
  /// </summary>
  [Parameter]
  public string? PopupClass { get; set; }

  /// <summary>
  /// The accessible name of the button and the popup. Defaults to "Colour"
  /// </summary>
  [Parameter]
  public string AriaLabel { get; set; } = "Colour";

  /// <summary>
  /// Fires when the user has finished changing the colour: when they click Apply, or (without the buttons) close the popup,
  /// having changed it. The argument is the new value
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  [Parameter]
  public EventCallback OnOpen { get; set; }

  [Parameter]
  public EventCallback OnClose { get; set; }

  /// <summary>
  /// The text of the popup's buttons and the accessible names of its parts, for translating
  /// </summary>
  [Parameter]
  public string ApplyText { get; set; } = "Apply";

  [Parameter]
  public string CancelText { get; set; } = "Cancel";

  [Parameter]
  public string ClearText { get; set; } = "Clear";

  [Parameter]
  public string NoColourText { get; set; } = "No colour";

  [Parameter]
  public string NewText { get; set; } = "New colour";

  [Parameter]
  public string CurrentText { get; set; } = "Current colour";

  [Parameter]
  public string ViewsText { get; set; } = "View";

  [Parameter]
  public string GradientText { get; set; } = "Gradient";

  [Parameter]
  public string PaletteText { get; set; } = "Palette";

  [Parameter]
  public string SaturationText { get; set; } = "Saturation and brightness";

  [Parameter]
  public string HueText { get; set; } = "Hue";

  [Parameter]
  public string OpacityText { get; set; } = "Opacity";

  [Parameter]
  public string FormatText { get; set; } = "Change the format";

  [Parameter]
  public string HexText { get; set; } = "Hex";

  [Parameter]
  public string RedText { get; set; } = "Red";

  [Parameter]
  public string GreenText { get; set; } = "Green";

  [Parameter]
  public string BlueText { get; set; } = "Blue";

  /// <summary>
  /// The 60 colours in the palette by default: a column of greys, then nine hues, each from a light tint down to a dark shade.
  /// The editor's colour tools use them too
  /// </summary>
  public static IReadOnlyList<string> DefaultPaletteColors { get; } = [
    "#ffffff", "#fcd9d9", "#fce9d9", "#fcf6d9", "#d9fcd9", "#d9fcf5", "#d9f0fc", "#d9e2fc", "#e8d9fc", "#fcd9f0",
    "#d9d9d9", "#f5a3a3", "#f5c9a3", "#f5e7a3", "#a3f5a3", "#a3f5e4", "#a3daf5", "#a3b8f5", "#c5a3f5", "#f5a3da",
    "#a6a6a6", "#ed5e5e", "#eda15e", "#edd55e", "#5eed5e", "#5eedd1", "#5ebeed", "#5e82ed", "#9a5eed", "#ed5ebe",
    "#7f7f7f", "#e61919", "#e67919", "#e6c419", "#19e619", "#19e6bd", "#19a1e6", "#194ce6", "#6e19e6", "#e619a1",
    "#404040", "#a51212", "#a55712", "#a58d12", "#12a512", "#12a588", "#1274a5", "#1237a5", "#5012a5", "#a51274",
    "#000000", "#6e0c0c", "#6e3a0c", "#6e5e0c", "#0c6e0c", "#0c6e5b", "#0c4e6e", "#0c256e", "#350c6e", "#6e0c4e"
  ];

  private static readonly string[] OpenKeys = ["Alt+ArrowDown"];

  private readonly string _uid = Guid.NewGuid().ToString("N");
  private readonly List<PixataColorPickerViewBase> _views = [];
  private ElementReference _button;
  private ElementReference _gradient;
  private ElementReference _palette;
  private DotNetObjectReference<PixataColorPicker>? _dotNet;
  private bool _open;
  private bool _focusPopup;
  private bool _keysAttached;
  private string? _gradientWired;
  private string? _paletteWired;
  private PixataColorPickerView _view;
  private PixataColorFormat _inputFormat;
  // What the user has picked in the popup (null for no colour), which becomes the value when they click Apply
  private PixataColour? _pending;
  // The colour when the popup was opened, shown as the current one in the preview
  private PixataColour? _atOpen;
  private string? _valueAtOpen;
  // The gradient's position. The hue is kept apart from the colour, so it isn't lost when the colour is grey
  private double _hue;
  private double _saturation;
  private double _brightness = 1;
  private double _alpha = 1;
  private List<PixataColour> _paletteColours = [];
  private int _focusedCell;
  private int _inputsVersion;

  private string PopupId =>
    $"pixata-colorpicker-{_uid}";

  private string CellId(int index) =>
    $"pixata-colorpicker-{_uid}-{index}";

  private PixataColour? Current =>
    PixataColour.Parse(Value);

  private string MainClass =>
    PixataCss.Classes(PixataCss.Input(Size, Rounded, FillMode), "pixata-blazor-colorpicker", PixataCss.Disabled(Enabled), InvalidClass,
                      _open ? "pixata-blazor-focus" : null, Class);

  private PixataColorPickerGradientView? GradientSettings =>
    _views.OfType<PixataColorPickerGradientView>().FirstOrDefault();

  private PixataColorPickerPaletteView? PaletteSettings =>
    _views.OfType<PixataColorPickerPaletteView>().FirstOrDefault();

  /// <summary>
  /// The views the popup offers, in the order they were declared, or both if none were
  /// </summary>
  internal List<PixataColorPickerView> AvailableViews =>
    _views.Count == 0 ? [PixataColorPickerView.Gradient, PixataColorPickerView.Palette] : [.. _views.Select(v => v.View).Distinct()];

  private PixataColour GradientColour =>
    PixataColour.FromHsv(_hue, _saturation, _brightness, _alpha);

  private string HandleStyle =>
    $"left: {Number(_saturation * 100)}%; top: {Number((1 - _brightness) * 100)}%; background-color: {PixataColour.FromHsv(_hue, _saturation, _brightness).ToHex()};";

  private string? PaletteStyle {
    get {
      string style = string.Concat(
        PaletteSettings?.TileWidth is { Length: > 0 } width ? $"--pixata-blazor-colorpalette-tile-width: {width};" : "",
        PaletteSettings?.TileHeight is { Length: > 0 } height ? $"--pixata-blazor-colorpalette-tile-height: {height};" : "");
      return style == "" ? null : style;
    }
  }

  internal void AddView(PixataColorPickerViewBase view) {
    if (!_views.Contains(view)) {
      _views.Add(view);
    }
  }

  internal void RemoveView(PixataColorPickerViewBase view) =>
    _views.Remove(view);

  private static string Number(double n) =>
    n.ToString("0.##", CultureInfo.InvariantCulture);

  private static int Percent(double n) =>
    (int)Math.Round(n * 100);

  private static string SwatchClass(PixataColour? colour, string? extra = null) =>
    PixataCss.Classes("pixata-blazor-colorpicker-swatch", colour is null ? "pixata-blazor-colorpicker-none" : null, extra);

  private static string? SwatchStyle(PixataColour? colour) =>
    colour is { } c ? $"--pixata-blazor-colorpicker-swatch-colour: {c.ToRgb()};" : null;

  private string Describe(PixataColour? colour) =>
    colour?.ToHex() ?? NoColourText;

  private List<PixataColour> PaletteColours() =>
    [.. (PaletteSettings?.Colors ?? PaletteColors ?? DefaultPaletteColors).Select(PixataColour.Parse).OfType<PixataColour>()];

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (!_keysAttached && Enabled) {
      _keysAttached = true;
      // Alt+Down opens the popup, so it shouldn't scroll the page too
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", _button, OpenKeys);
    }
    if (!_open) {
      return;
    }
    // The gradient and the palette are new elements each time the popup opens or the view changes
    if (_view == PixataColorPickerView.Gradient && _gradient.Id != _gradientWired) {
      _gradientWired = _gradient.Id;
      _dotNet ??= DotNetObjectReference.Create(this);
      await PixataJs.InvokeVoid(Js, "initColourGradient", _gradient, _dotNet);
      await PixataJs.InvokeVoid(Js, "preventNavigationKeys", _gradient);
    } else if (_view == PixataColorPickerView.Palette && _palette.Id != _paletteWired) {
      _paletteWired = _palette.Id;
      await PixataJs.InvokeVoid(Js, "preventNavigationKeys", _palette);
    }
    if (_focusPopup) {
      _focusPopup = false;
      await PixataJs.InvokeVoid(Js, "focusSelector", null, $"#{PopupId} .pixata-blazor-colorgradient, #{PopupId} .pixata-blazor-colorpalette");
    }
  }

  /// <summary>
  /// Opens the popup
  /// </summary>
  public async Task Open() {
    if (_open || !Enabled) {
      return;
    }
    _atOpen = Current;
    _valueAtOpen = Value;
    _pending = _atOpen;
    SetGradientFrom(_pending);
    _inputFormat = GradientSettings?.Format ?? PixataColorFormat.Rgb;
    List<PixataColorPickerView> views = AvailableViews;
    _view = views.Contains(View) ? View : views[0];
    _paletteColours = PaletteColours();
    _focusedCell = FocusedCellFor(_pending);
    _open = true;
    _focusPopup = true;
    await OnOpen.InvokeAsync();
    StateHasChanged();
  }

  /// <summary>
  /// Closes the popup. With the buttons, the colour picked is thrown away, as with Cancel
  /// </summary>
  public async Task Close() {
    if (!_open) {
      return;
    }
    _open = false;
    if (!ShowButtons && Value != _valueAtOpen) {
      await OnChange.InvokeAsync(Value);
    }
    await OnClose.InvokeAsync();
    StateHasChanged();
  }

  /// <summary>
  /// Moves the keyboard focus to the button
  /// </summary>
  public async ValueTask FocusAsync() {
    try {
      await _button.FocusAsync();
    } catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) {
      // The button has gone from the page, or the page has gone
    }
  }

  private async Task Toggle() {
    if (_open) {
      await Close();
    } else {
      await Open();
    }
  }

  private async Task Cancel() =>
    await Close();

  private async Task CancelAndFocus() {
    await Close();
    await FocusAsync();
  }

  private async Task Apply() {
    await Commit();
    string? applied = Value;
    _open = false;
    if (applied != _valueAtOpen) {
      await OnChange.InvokeAsync(applied);
    }
    await OnClose.InvokeAsync();
    await FocusAsync();
  }

  // Makes the colour picked the value. A colour that hasn't changed keeps the value as it was written
  private async Task Commit() {
    string? value = _pending is not { } colour ? null : Current == colour ? Value : colour.Format(ValueFormat);
    await UpdateValue(value);
  }

  // Without the buttons, every change is the new value straight away
  private async Task Picked(PixataColour? colour) {
    _pending = colour;
    _inputsVersion++;
    if (!ShowButtons) {
      await Commit();
    }
  }

  private void SetGradientFrom(PixataColour? colour) {
    if (colour is not { } c) {
      return;
    }
    (double h, double s, double v) = c.ToHsv();
    // A grey has no hue, so the slider stays where it was
    if (s > 0 && v > 0) {
      _hue = h;
    }
    _saturation = s;
    _brightness = v;
    _alpha = c.A;
  }

  private int FocusedCellFor(PixataColour? colour) {
    int index = colour is { } c ? _paletteColours.FindIndex(p => p.Opaque() == c.Opaque()) : -1;
    return index >= 0 ? index : 0;
  }

  private async Task ShowView(PixataColorPickerView view) {
    if (view == _view) {
      return;
    }
    _view = view;
    _focusedCell = FocusedCellFor(_pending);
    View = view;
    await ViewChanged.InvokeAsync(view);
  }

  private async Task ClearPending() =>
    await Picked(null);

  private async Task Revert() {
    SetGradientFrom(_atOpen);
    await Picked(_atOpen);
  }

  private async Task WrapperKeyDown(KeyboardEventArgs e) {
    if (!_open) {
      if (e.Key == "ArrowDown" && e.AltKey) {
        await Open();
      }
      return;
    }
    if (e.Key == "Escape" || (e.Key == "ArrowUp" && e.AltKey)) {
      await CancelAndFocus();
    }
  }

  // ---------- The gradient ----------

  /// <summary>
  /// Called by the script as the user drags in the gradient, with how far across and down it the pointer is, from 0 to 1
  /// </summary>
  [JSInvokable]
  public async Task OnGradientDrag(double x, double y) {
    if (!_open) {
      return;
    }
    _saturation = Math.Clamp(x, 0, 1);
    _brightness = 1 - Math.Clamp(y, 0, 1);
    await Picked(GradientColour);
    StateHasChanged();
  }

  // The arrow keys move around the gradient by 1%, or 10% with Shift
  private async Task GradientKeyDown(KeyboardEventArgs e) {
    double step = e.ShiftKey ? .1 : .01;
    switch (e.Key) {
      case "ArrowLeft":
        _saturation = Math.Round(Math.Max(0, _saturation - step), 2);
        break;
      case "ArrowRight":
        _saturation = Math.Round(Math.Min(1, _saturation + step), 2);
        break;
      case "ArrowUp" when !e.AltKey:
        _brightness = Math.Round(Math.Min(1, _brightness + step), 2);
        break;
      case "ArrowDown" when !e.AltKey:
        _brightness = Math.Round(Math.Max(0, _brightness - step), 2);
        break;
      default:
        return;
    }
    await Picked(GradientColour);
  }

  private async Task HueInput(ChangeEventArgs e) {
    if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double hue)) {
      _hue = Math.Clamp(hue, 0, 359);
      await Picked(GradientColour);
    }
  }

  private async Task AlphaInput(ChangeEventArgs e) {
    if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double alpha)) {
      _alpha = Math.Clamp(alpha, 0, 100) / 100;
      await Picked(GradientColour);
    }
  }

  private void ToggleFormat() =>
    _inputFormat = _inputFormat == PixataColorFormat.Hex ? PixataColorFormat.Rgb : PixataColorFormat.Hex;

  private async Task HexChanged(ChangeEventArgs e) {
    string text = e.Value?.ToString()?.Trim() ?? "";
    if (text.Length > 0 && !text.StartsWith('#')) {
      text = $"#{text}";
    }
    if (PixataColour.Parse(text) is { } colour) {
      if (!(GradientSettings?.ShowOpacityEditor ?? true)) {
        colour = colour.Opaque();
      }
      SetGradientFrom(colour);
      await Picked(colour);
    } else {
      // Puts the colour back in the box
      _inputsVersion++;
    }
  }

  private async Task ChannelChanged(int channel, ChangeEventArgs e) {
    PixataColour colour = _pending ?? GradientColour;
    if (!double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double n)) {
      _inputsVersion++;
      return;
    }
    int value = (int)Math.Round(Math.Clamp(n, 0, channel == 3 ? 100 : 255));
    colour = channel switch {
      0 => colour with { R = value },
      1 => colour with { G = value },
      2 => colour with { B = value },
      _ => colour with { A = value / 100d }
    };
    SetGradientFrom(colour);
    await Picked(colour);
  }

  // ---------- The palette ----------

  private async Task PickTile(int index) {
    if (index < 0 || index >= _paletteColours.Count) {
      return;
    }
    _focusedCell = index;
    PixataColour colour = _paletteColours[index];
    SetGradientFrom(colour);
    await Picked(colour);
    // Without the buttons, picking from the palette is the end of it
    if (!ShowButtons) {
      await CancelAndFocus();
    }
  }

  private async Task PaletteKeyDown(KeyboardEventArgs e) {
    int count = _paletteColours.Count;
    if (count == 0) {
      return;
    }
    int columns = Math.Max(1, PaletteSettings?.Columns ?? 10);
    int row = _focusedCell - _focusedCell % columns;
    switch (e.Key) {
      case "ArrowRight":
        _focusedCell = Math.Min(count - 1, _focusedCell + 1);
        break;
      case "ArrowLeft":
        _focusedCell = Math.Max(0, _focusedCell - 1);
        break;
      case "ArrowDown" when !e.AltKey:
        _focusedCell = _focusedCell + columns < count ? _focusedCell + columns : _focusedCell;
        break;
      case "ArrowUp" when !e.AltKey:
        _focusedCell = _focusedCell - columns >= 0 ? _focusedCell - columns : _focusedCell;
        break;
      case "Home":
        _focusedCell = e.CtrlKey ? 0 : row;
        break;
      case "End":
        _focusedCell = e.CtrlKey ? count - 1 : Math.Min(count - 1, row + columns - 1);
        break;
      case "Enter":
      case " ":
        await PickTile(_focusedCell);
        break;
    }
  }

  protected override void Dispose(bool disposing) {
    if (disposing) {
      _dotNet?.Dispose();
      _dotNet = null;
    }
    base.Dispose(disposing);
  }
}
