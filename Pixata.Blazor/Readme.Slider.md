# Slider

>Part of [Pixata.Blazor](Readme.md)

`PixataSlider` is for picking a number between a minimum and a maximum by dragging a thumb along a track. It can have ticks and labels along the track, and buttons at each end, and it can be horizontal or vertical. It works on its own or inside an `EditForm`. It has the same parameters as Telerik's Slider, so markup can usually move across by changing the component's name.

See the [live demo](https://test.pixata.co.uk/SliderSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/SliderSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label for="volume">Volume</label>
<PixataSlider Id="volume" @bind-Value="_volume" Max="100" SmallStep="5" LargeStep="25" />

@code {
  private int _volume = 50;
}
```

The value can be an `int`, `long`, `short`, `decimal`, `double` or `float`. Any other type throws an `InvalidOperationException` when the component is first rendered. `Min`, `Max`, `SmallStep` and `LargeStep` are the same type as the value, so for a `double` write `Max="1.0"`, and for a `decimal`, `Max="1m"`.

The value changes as the thumb is dragged, so anything bound to it updates straight away. `OnChange` fires when the user has finished: when they let go of the thumb, press a key or click a button.

The thumb is a native range input, so it works with the keyboard as well as the mouse and touch. The arrow keys move it by `SmallStep`, Page Up and Page Down by `LargeStep`, and Home and End take it to the ends. Screen readers announce it as a slider. Give it an `Id` and a `<label for="...">`, or an `AriaLabel`.

## Ticks and labels

There is a tick every `SmallStep`, and a longer, labelled tick every `LargeStep`, counting from `Min`. If there would be more than 100 small ticks, only the large ones are drawn, and if there would be more than 100 of those, there are no ticks at all.

`TickPosition` says where they go...

| `TickPosition` | Ticks |
| --- | --- |
| `Both` | On both sides of the track, with the labels after it (the default) |
| `Before` | Above a horizontal slider, or to the left of a vertical one, with the labels |
| `After` | Below a horizontal slider, or to the right of a vertical one, with the labels |
| `None` | No ticks or labels |

The labels show each large tick's value. Use `LabelTemplate` to show something else. Its context is the value...

```xml
<PixataSlider @bind-Value="_opacity" Max="1m" SmallStep="0.05m" LargeStep="0.25m" Decimals="2">
  <LabelTemplate>@context.ToString("P0")</LabelTemplate>
</PixataSlider>
```

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `Value` | `TValue` | The value. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<TValue>` | Fires on every change, including while the thumb is dragged |
| `ValueExpression` | `Expression<Func<TValue>>` | Identifies the field for validation. Set for you by `@bind-Value` |
| `OnChange` | `EventCallback<object?>` | Fires when the user lets go of the thumb, presses a key or clicks a button |
| `Min` | `TValue` | The lowest value. Defaults to 0 |
| `Max` | `TValue` | The highest value. Defaults to 10 |
| `SmallStep` | `TValue` | How far the arrow keys and buttons move the value, and the gap between ticks. Defaults to 1 |
| `LargeStep` | `TValue` | How far Page Up and Page Down move the value, and the gap between labels. Defaults to 5 |
| `Decimals` | `int?` | The number of decimal places to round to. Whole number types are always whole numbers |
| `ShowButtons` | `bool` | Shows the decrease and increase buttons at the ends. Defaults to `true` |
| `DecreaseButtonText`, `IncreaseButtonText` | `string` | The buttons' tooltips and accessible names, for translating. Default to "Decrease" and "Increase" |
| `TickPosition` | `PixataSliderTickPosition` | `Both` (the default), `Before`, `After` or `None` |
| `LabelTemplate` | `RenderFragment<TValue>?` | Shows each large tick's label |
| `Orientation` | `PixataSliderOrientation` | `Horizontal` (the default) or `Vertical`, with the lowest value at the bottom |
| `Enabled` | `bool` | Defaults to `true` |
| `ReadOnly` | `bool` | The value can be seen, and the slider focused, but not changed |
| `Width` | `string?` | The width of a horizontal slider, as a CSS width. By default it fills the width it's given |
| `Height` | `string?` | The height of a vertical slider, as a CSS height. Defaults to `200px` |
| `Id` | `string?` | The range input's id, for a `<label for="...">` |
| `Class` | `string?` | Extra classes for the outer element |
| `TabIndex` | `int?` | The range input's tab index |
| `AriaLabel`, `AriaLabelledBy` | `string?` | The range input's accessible name, when there's no `<label>` |

Any other attributes, such as `data-` attributes, are added to the range input. `FocusAsync()` puts the focus on it.

## In a form

Inside an `EditForm`, the slider tells the form when its field changes, and turns red when the field has validation messages...

```xml
<EditForm Model="_review" OnValidSubmit="Save">
  <DataAnnotationsValidator />
  <FormSingle Label="Rating" Id="rating" Required="true">
    <PixataSlider Id="rating" @bind-Value="_review.Rating" Max="5" LargeStep="1" />
    <ValidationMessage For="() => _review.Rating" />
  </FormSingle>
  <PixataPrimaryButton>Save</PixataPrimaryButton>
</EditForm>
```

## Styling

The slider is styled by these custom properties, which you can set on `:root` or on a particular slider...

| Property | Default |
| --- | --- |
| `--pixata-blazor-slider-track-size` | `4px`, the thickness of the track |
| `--pixata-blazor-slider-track-bg` | A light shade of the text colour |
| `--pixata-blazor-slider-selection-bg` | `--pixata-blazor-primary`, the part of the track before the thumb |
| `--pixata-blazor-slider-thumb-size` | `16px` |
| `--pixata-blazor-slider-thumb-bg`, `--pixata-blazor-slider-thumb-border-color` | `--pixata-blazor-primary` |
| `--pixata-blazor-slider-thumb-focus-shadow` | A ring in a light shade of `--pixata-blazor-primary` |
| `--pixata-blazor-slider-tick-color` | A mid shade of the text colour |
| `--pixata-blazor-slider-tick-size`, `--pixata-blazor-slider-tick-large-size` | `4px` and `8px`, the lengths of the ticks |
| `--pixata-blazor-slider-label-font-size` | `.8em` |
| `--pixata-blazor-slider-vertical-height` | `200px`, the height of a vertical slider without a `Height` |

The elements have these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-slider` | The outer element, with `pixata-blazor-slider-horizontal` or `pixata-blazor-slider-vertical`, and `pixata-blazor-slider-readonly` when `ReadOnly` |
| `pixata-blazor-slider-input` | The range input, whose `--pixata-blazor-slider-fraction` (0 to 1) is how far along the value is |
| `pixata-blazor-slider-button`, with `pixata-blazor-slider-decrease` or `pixata-blazor-slider-increase` | The buttons |
| `pixata-blazor-slider-ticks`, with `pixata-blazor-slider-ticks-before` or `pixata-blazor-slider-ticks-after` | The rows of ticks |
| `pixata-blazor-slider-tick`, with `pixata-blazor-slider-tick-large` for the labelled ones | A tick |
| `pixata-blazor-slider-label` | A tick's label |
