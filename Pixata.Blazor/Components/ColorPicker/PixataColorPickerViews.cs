using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// How <see cref="PixataColorPicker"/> writes its value, as Telerik's <c>ColorFormat</c>
/// </summary>
public enum PixataColorFormat {
  /// <summary>
  /// <c>rgb(r, g, b)</c>, or <c>rgba(r, g, b, a)</c> when the colour isn't opaque
  /// </summary>
  Rgb,

  /// <summary>
  /// <c>#rrggbb</c>, or <c>#rrggbbaa</c> when the colour isn't opaque
  /// </summary>
  Hex
}

/// <summary>
/// The views <see cref="PixataColorPicker"/>'s popup can show, as Telerik's <c>ColorPickerView</c>
/// </summary>
public enum PixataColorPickerView {
  /// <summary>
  /// A square of the shades of one hue, a hue slider, an opacity slider and boxes to type the colour
  /// </summary>
  Gradient,

  /// <summary>
  /// A grid of colours to pick from
  /// </summary>
  Palette
}

/// <summary>
/// A view of <see cref="PixataColorPicker"/>'s popup, declared inside its <c>ColorPickerViews</c>. The views appear in the
/// order they are declared, and their settings are read each time the popup opens. Renders nothing itself
/// </summary>
public abstract class PixataColorPickerViewBase : ComponentBase, IDisposable {
  [CascadingParameter]
  internal PixataColorPicker? Picker { get; set; }

  internal abstract PixataColorPickerView View { get; }

  protected override void OnInitialized() =>
    Picker?.AddView(this);

  public void Dispose() {
    Picker?.RemoveView(this);
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// The gradient view of <see cref="PixataColorPicker"/>'s popup, as Telerik's <c>ColorPickerGradientView</c>
/// </summary>
public class PixataColorPickerGradientView : PixataColorPickerViewBase {
  /// <summary>
  /// Whether the view has an opacity slider and box. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowOpacityEditor { get; set; } = true;

  /// <summary>
  /// Whether the boxes for typing the colour start as hex or as red, green and blue. The user can switch between them.
  /// Defaults to <see cref="PixataColorFormat.Rgb"/>, as in Telerik
  /// </summary>
  [Parameter]
  public PixataColorFormat Format { get; set; } = PixataColorFormat.Rgb;

  internal override PixataColorPickerView View =>
    PixataColorPickerView.Gradient;
}

/// <summary>
/// The palette view of <see cref="PixataColorPicker"/>'s popup, as Telerik's <c>ColorPickerPaletteView</c>
/// </summary>
public class PixataColorPickerPaletteView : PixataColorPickerViewBase {
  /// <summary>
  /// The colours, as hex or <c>rgb()</c> colours. Defaults to the picker's <see cref="PixataColorPicker.PaletteColors"/>
  /// </summary>
  [Parameter]
  public IEnumerable<string>? Colors { get; set; }

  /// <summary>
  /// The number of colours in each row. Defaults to 10
  /// </summary>
  [Parameter]
  public int Columns { get; set; } = 10;

  /// <summary>
  /// The width and height of each colour, as CSS sizes. Default to 1.5rem
  /// </summary>
  [Parameter]
  public string? TileWidth { get; set; }

  [Parameter]
  public string? TileHeight { get; set; }

  internal override PixataColorPickerView View =>
    PixataColorPickerView.Palette;
}
