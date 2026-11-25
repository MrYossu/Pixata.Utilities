# Colour picker

>Part of [Pixata.Blazor](Readme.md)

`PixataColorPicker` is a replacement for Telerik's ColorPicker, with the same parameters where they apply. It is a button showing the colour, which opens a popup with two views: a gradient (a square of shades of one hue, a hue slider, an opacity slider and boxes to type the colour) and a palette of colours to pick from. The value is a CSS colour string.

See the [live demo](https://test.pixata.co.uk/ColorPickerSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/ColorPickerSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label for="colour">Colour</label>
<PixataColorPicker Id="colour" @bind-Value="_colour" ValueFormat="PixataColorFormat.Hex" />

@code {
  private string? _colour = "#282f89";
}
```

The user picks a colour in the popup, and it becomes the value when they click Apply. Cancel, Escape or a click outside the popup leave the value as it was. With `ShowButtons="false"`, there are no buttons, and the value changes as the user picks, as in Telerik. Picking from the palette then closes the popup too.

The value is written as `rgb(40, 47, 137)` by default, as in Telerik, or as `#282f89` with `ValueFormat="PixataColorFormat.Hex"`. A colour that isn't opaque is written as `rgba(40, 47, 137, 0.5)` or `#282f8980`. The value can be set to any of those forms, including the short hex forms (`#28f` and `#28f8`), and `rgb()` with spaces, a `/` before the opacity, or percentages. A value set from outside is kept as it is written until the user picks another colour, so picking the same colour again doesn't rewrite `#FFF` as `rgb(255, 255, 255)`. A colour name such as `red` can't be read, so it is shown as no colour. A null value is no colour, which the user can pick with the clear button at the top of the popup.

## The popup

| Part | What it does |
| --- | --- |
| View buttons | Switch between the gradient and the palette, when there are both |
| Clear button | Picks no colour, so the value becomes null. Hidden with `ShowClearButton="false"` |
| Preview | The new colour, next to the colour when the popup was opened. Clicking the old colour picks it again. Hidden with `ShowPreview="false"` |
| Gradient | The saturation from left to right, and the brightness from top to bottom, for the hue on the slider below. Drag in it, or use the arrow keys |
| Hue slider | The hue, from red round to red again |
| Opacity slider | How see-through the colour is. Hidden with the gradient view's `ShowOpacityEditor="false"` |
| Format button and boxes | The colour as hex, or as red, green and blue (0 to 255) and opacity (0 to 100). The button switches between them. A colour typed in a box is used when the user leaves the box or presses Enter, and a box with something that isn't a colour goes back to the colour |
| Palette | A grid of colours. Click one, or move to it with the arrow keys and press Enter |
| Cancel and Apply | Close the popup, throwing the colour away or making it the value. Hidden with `ShowButtons="false"` |

## Choosing the views

Both views are shown by default, with the gradient first. To choose the views, their order and their settings, declare them in `ColorPickerViews`...

```xml
<PixataColorPicker @bind-Value="_colour" View="PixataColorPickerView.Palette">
  <ColorPickerViews>
    <PixataColorPickerPaletteView Colors="_brandColours" Columns="4" TileWidth="2rem" TileHeight="2rem" />
    <PixataColorPickerGradientView ShowOpacityEditor="false" Format="PixataColorFormat.Hex" />
  </ColorPickerViews>
</PixataColorPicker>
```

With only one view, there are no buttons to switch views. `View` is the view the popup opens with. Bind it with `@bind-View` to keep the view the user last switched to.

| `PixataColorPickerGradientView` parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ShowOpacityEditor` | `bool` | `true` | Shows the opacity slider and box. Without them, every colour picked is opaque |
| `Format` | `PixataColorFormat` | `Rgb` | Whether the boxes start as red, green and blue, or as hex |

| `PixataColorPickerPaletteView` parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Colors` | `IEnumerable<string>?` | The picker's `PaletteColors` | The colours, as hex or `rgb()` |
| `Columns` | `int` | `10` | The number of colours in each row |
| `TileWidth`, `TileHeight` | `string?` | `1.5rem` | The size of each colour, as CSS sizes |

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `string?` | | The colour. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<string?>` | | Raised when the value changes. Set for you by `@bind-Value` |
| `ValueExpression` | `Expression<Func<string?>>` | | Identifies the bound field for validation. Set for you by `@bind-Value` |
| `ValueFormat` | `PixataColorFormat` | `Rgb` | How the value is written: `Rgb` or `Hex` |
| `OnChange` | `EventCallback<object?>` | | Raised with the new value when the user clicks Apply with a different colour, or, without the buttons, when the popup closes with a different colour |
| `OnOpen`, `OnClose` | `EventCallback` | | Raised when the popup opens and closes |
| `View` | `PixataColorPickerView` | `Gradient` | The view the popup opens with. Use `@bind-View` to keep the one the user switches to |
| `ColorPickerViews` | `RenderFragment?` | | The views, as above |
| `PaletteColors` | `IEnumerable<string>?` | `PixataColorPicker.DefaultPaletteColors` | The palette's colours, when the palette view doesn't set its own `Colors`. The default is 60 colours: a column of greys, then nine hues from light to dark, as in the editor |
| `ShowButtons` | `bool` | `true` | Shows Apply and Cancel. Without them, the value changes as the user picks |
| `ShowClearButton` | `bool` | `true` | Shows the button that picks no colour |
| `ShowPreview` | `bool` | `true` | Shows the new and the old colour at the top of the popup |
| `Enabled` | `bool` | `true` | When false, the button is greyed out and the popup can't be opened |
| `Size` | `string?` | `md` | `sm`, `md` or `lg`. See `PixataThemeConstants.Size` |
| `Rounded` | `string?` | | `none`, `sm`, `md`, `lg` or `full`. See `PixataThemeConstants.Rounded` |
| `FillMode` | `string?` | | `solid`, `flat` or `outline`. See `PixataThemeConstants.FillMode` |
| `Id` | `string?` | | The button's `id`, for a `<label for="...">` |
| `Class` | `string?` | | Extra classes for the button |
| `PopupClass` | `string?` | | Extra classes for the popup |
| `AriaLabel` | `string` | `"Colour"` | The accessible name of the button and the popup |
| `ApplyText`, `CancelText`, `ClearText`, `NoColourText`, `NewText`, `CurrentText`, `ViewsText`, `GradientText`, `PaletteText`, `SaturationText`, `HueText`, `OpacityText`, `FormatText`, `HexText`, `RedText`, `GreenText`, `BlueText` | `string` | English | The text of the buttons, tooltips and accessible names, for translating |

Any other attributes, such as `data-` attributes, are added to the button.

The component has these methods, for use through a `@ref`...

| Method | What it does |
| --- | --- |
| `Open()` | Opens the popup |
| `Close()` | Closes the popup. With the buttons, the colour picked is thrown away, as with Cancel |
| `FocusAsync()` | Moves the focus to the button |

## In a form

Inside an `EditForm`, the picker tells the form when its field changes, and shows as invalid (with a red border) when the field has validation messages. It is disabled inside a `<fieldset disabled>`, as the other inputs are...

```xml
<EditForm Model="_theme" OnValidSubmit="Save">
  <DataAnnotationsValidator />
  <FormSingle Label="Colour" Id="colour" Required="true">
    <PixataColorPicker Id="colour" @bind-Value="_theme.Colour" ValueFormat="PixataColorFormat.Hex" />
    <ValidationMessage For="() => _theme.Colour" />
  </FormSingle>
</EditForm>
```

## Keyboard

| Key | Where | What it does |
| --- | --- | --- |
| Enter, Space or Alt+Down | The button | Opens the popup, with the focus in the gradient or the palette |
| Arrow keys | The gradient | Moves the handle by 1%, or 10% with Shift |
| Arrow keys | The palette | Moves to the next colour. Home and End go to the start and end of the row, or of the palette with Ctrl |
| Enter or Space | The palette | Picks the colour. Without the buttons, it closes the popup too |
| Tab | The popup | Moves between the parts of the popup |
| Escape or Alt+Up | The popup | Closes the popup without changing the value, and moves the focus back to the button |

## Accessibility

The button has `aria-haspopup="dialog"` and `aria-expanded`, and the popup has `role="dialog"`. The gradient is a `role="slider"` whose `aria-valuetext` gives the saturation and the brightness, and the palette is a `role="grid"` whose `aria-activedescendant` is the colour the arrow keys are on, each with its hex code as its name. Give the picker a `<label for="...">` pointing at its `Id`, or an `AriaLabel`.

## Styling

The button looks like the other inputs, with the colour in it over a chequerboard, so a see-through colour shows as one. No colour is white with a red line across. The popup is styled by these custom properties, which you can set on `:root` or on a picker's `PopupClass`...

| Property | Default |
| --- | --- |
| `--pixata-blazor-colorpicker-popup-width` | `17rem` |
| `--pixata-blazor-colorpicker-swatch-width` | `2.5em`, the width of the colour on the button |
| `--pixata-blazor-colorgradient-height` | `10rem` |
| `--pixata-blazor-colorpalette-tile-width`, `--pixata-blazor-colorpalette-tile-height` | `1.5rem`, set for you by the palette view's `TileWidth` and `TileHeight` |

The elements have these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-colorpicker-wrapper` | The outer element, with `pixata-blazor-colorpicker-open` when the popup is open |
| `pixata-blazor-colorpicker` | The button, with the input classes |
| `pixata-blazor-colorpicker-swatch` | A colour, on the button and in the preview, with `pixata-blazor-colorpicker-none` for no colour |
| `pixata-blazor-colorpicker-popup` | The popup |
| `pixata-blazor-colorpicker-header`, `pixata-blazor-colorpicker-footer` | The rows at the top and bottom of the popup |
| `pixata-blazor-colorgradient` | The gradient, with `pixata-blazor-colorgradient-handle` in it |
| `pixata-blazor-colorgradient-hue`, `pixata-blazor-colorgradient-alpha` | The sliders |
| `pixata-blazor-colorpalette` | The palette, with a `pixata-blazor-colorpalette-tile` for each colour, and `pixata-blazor-selected` on the colour picked |

## Moving from Telerik

The parameters above have the same names and meanings as Telerik's. The views are `PixataColorPickerGradientView` and `PixataColorPickerPaletteView`, and the enums are `PixataColorFormat` and `PixataColorPickerView`, in place of Telerik's `ColorPickerGradientView`, `ColorPickerPaletteView`, `ColorFormat` and `ColorPickerView`, so change those names as well as the component's. A Telerik parameter that isn't in the tables above isn't supported, and the value can't be a colour name such as `red`.

The editor's colour tools use the same default palette and the same reading of colours.
