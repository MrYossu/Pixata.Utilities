# Switch

>Part of [Pixata.Blazor](Readme.md)

`PixataSwitch` is a toggle switch for `bool` and `bool?` values. It works on its own or inside an `EditForm`, can be used with the keyboard, mouse or touch, and is styled entirely through CSS custom properties, so you can fit it to your own theme.

See the [live demo](https://test.pixata.co.uk/SwitchSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/SwitchSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label for="notifications">Notifications</label>
<PixataSwitch Id="notifications" @bind-Value="_notifications" />

@code {
  private bool _notifications;
}
```

The switch is a `<button>` with `role="switch"` and `aria-checked`, so screen readers announce it as a switch, and the `Id` lets a `<label for="...">` name it. It can also go inside its label, as a checkbox often does, in which case it doesn't need an `Id`. Clicking either the switch or the label's text toggles it once...

```xml
<label><PixataSwitch @bind-Value="_notifications" /> Notifications</label>
```

If there is no visible label, give it an `AriaLabel` instead.

It can be bound to a `bool?` as well as a `bool`. A null value is shown as off, and the first toggle sets it to true...

```xml
<PixataSwitch @bind-Value="_newsletter" AriaLabel="Send me the newsletter" />

@code {
  private bool? _newsletter;
}
```

Using any other type throws an `InvalidOperationException` when the switch is rendered.

## Using it

| Input | What it does |
| --- | --- |
| Click or tap | Toggles the value |
| Swipe right or left | Turns it on or off (the other way round in a right-to-left page, where on is at the left). The switch takes sideways swipes, so they don't scroll the page |
| Tab | Moves focus to and from the switch |
| Space or Enter | Toggles the value |

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `bool` or `bool?` | | The value. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<TValue>` | | Raised when the value changes. Set for you by `@bind-Value` |
| `ValueExpression` | `Expression<Func<TValue>>` | | Identifies the bound field for validation. Set for you by `@bind-Value` |
| `OnLabel` | `string` | `"On"` | The text shown in the track when the switch is on |
| `OffLabel` | `string` | `"Off"` | The text shown in the track when the switch is off |
| `Width` | `string` | | The width, as a CSS length (eg `"100px"`). By default it depends on `Size` |
| `Size` | `string` | `"md"` | `"sm"`, `"md"` or `"lg"`. The values are in `PixataThemeConstants.Size` |
| `Rounded` | `string` | `"full"` | The roundness of both the track and the thumb: `"none"`, `"sm"`, `"md"`, `"lg"` or `"full"`. The values are in `PixataThemeConstants.Rounded` |
| `TrackRounded` | `string` | `Rounded` | The roundness of the track, if it should differ from `Rounded` |
| `ThumbRounded` | `string` | `Rounded` | The roundness of the thumb, if it should differ from `Rounded` |
| `Enabled` | `bool` | `true` | When false, the switch is greyed out, can't be focused and can't be changed |
| `ReadOnly` | `bool` | `false` | When true, the switch looks normal and can be focused, but can't be changed. It gets `aria-readonly="true"` |
| `Id` | `string` | | The `id` of the button, so a `<label for="...">` can point at it |
| `TabIndex` | `int?` | | The `tabindex` of the button |
| `Class` | `string` | | Extra CSS classes for the button |
| `AriaLabel` | `string` | | The `aria-label`, for when there is no visible label |
| `AriaLabelledBy` | `string` | | The `aria-labelledby` |
| `AriaDescribedBy` | `string` | | The `aria-describedby` |

Any other attributes you add (`title`, `data-*` and so on) are rendered on the button.

## Events

| Event | Argument | Description |
| --- | --- | --- |
| `ValueChanged` | `TValue` | Raised when the value changes |
| `OnChange` | `object` | Raised when the user changes the value, with the new value (a `bool`) |

```xml
<PixataSwitch @bind-Value="_darkMode" OnChange="@((object value) => SaveSetting((bool)value))" AriaLabel="Dark mode" />
```

## Inside an EditForm

Bind it with `@bind-Value` and it takes part in validation like any other input. It tells the form when the value changes, and gets the `pixata-blazor-invalid` class (a red track border) and `aria-invalid="true"` when the field has validation messages...

```xml
<EditForm Model="_model" OnValidSubmit="Save">
  <DataAnnotationsValidator />
  <label for="terms">I accept the terms and conditions</label>
  <PixataSwitch Id="terms" @bind-Value="_model.AcceptTerms" />
  <ValidationMessage For="() => _model.AcceptTerms" />
  <button type="submit">Save</button>
</EditForm>
```

...where the model has something like this...

```csharp
[Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the terms and conditions")]
public bool AcceptTerms { get; set; }
```

## Styling

The switch is made up of these elements and classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-switch` | The button. It also has a size class (`pixata-blazor-switch-sm`, `-md` or `-lg`), `pixata-blazor-switch-on` or `pixata-blazor-switch-off`, and `pixata-blazor-switch-readonly`, `pixata-blazor-disabled` or `pixata-blazor-invalid` when they apply |
| `pixata-blazor-switch-track` | The track, with a `pixata-blazor-rounded-*` class |
| `pixata-blazor-switch-label-on` and `pixata-blazor-switch-label-off` | The two labels in the track. The one that doesn't apply is transparent |
| `pixata-blazor-switch-thumb-wrap` | A zero-sized box the thumb is centred on, which slides along the track |
| `pixata-blazor-switch-thumb` | The thumb, with a `pixata-blazor-rounded-*` class |

The colours, sizes and animations come from these custom properties, which you can override on `:root` or on any container...

| Variable | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-switch-width-sm`, `-md`, `-lg` | `52px`, `60px`, `68px` | The width for each size |
| `--pixata-blazor-switch-height-sm`, `-md`, `-lg` | `26px`, `30px`, `34px` | The height for each size, which is also the size of the thumb |
| `--pixata-blazor-switch-label-inset-sm`, `-md`, `-lg` | `4px`, `6px`, `8px` | How far the labels are from the ends of the track |
| `--pixata-blazor-switch-label-font-size` | `10px` | The size of the label text |
| `--pixata-blazor-switch-off-track-bg` | `#fff` | The track's background when off |
| `--pixata-blazor-switch-off-track-border-color` | `--pixata-blazor-border-color` | The track's border when off |
| `--pixata-blazor-switch-off-color` | `--pixata-blazor-text-color` | The label colour when off |
| `--pixata-blazor-switch-on-track-bg` | `--pixata-blazor-primary` | The track's background when on |
| `--pixata-blazor-switch-on-track-border-color` | `--pixata-blazor-primary` | The track's border when on |
| `--pixata-blazor-switch-on-color` | `--pixata-blazor-primary-color` | The label colour when on |
| `--pixata-blazor-switch-thumb-bg` | `--pixata-blazor-surface-alt-bg` | The thumb's background |
| `--pixata-blazor-switch-thumb-border-color` | `--pixata-blazor-border-color` | The thumb's border |
| `--pixata-blazor-switch-off-focus-outline`, `--pixata-blazor-switch-on-focus-outline` | A faint grey or primary-coloured 2px outline | The focus ring on the track, when focused from the keyboard |
| `--pixata-blazor-switch-on-transition`, `--pixata-blazor-switch-off-transition` | `.3s` with a slight bounce, `.25s` | How the thumb moves when the switch is turned on and off |

For example, to have green switches...

```css
:root {
  --pixata-blazor-switch-on-track-bg: #37b400;
  --pixata-blazor-switch-on-track-border-color: #37b400;
}
```

On touch screens the area you can tap is at least `--pixata-blazor-touch-target` (2.5rem) high, even for the small switch, and the animations are turned off for users who have asked for reduced motion.
