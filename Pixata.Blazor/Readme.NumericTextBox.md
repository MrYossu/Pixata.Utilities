# Numeric text box

>Part of [Pixata.Blazor](Readme.md)

`PixataNumericTextBox` is a text box for numbers, with optional spin buttons, a minimum and maximum, rounding, and a format for showing the value when the input doesn't have focus. It has the same parameters as Telerik's NumericTextBox, so markup can usually move across by changing the component's name.

See the [live demo](https://test.pixata.co.uk/NumericTextBoxSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/NumericTextBoxSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label for="amount">Amount</label>
<PixataNumericTextBox Id="amount" @bind-Value="_amount" Format="C2" Decimals="2" />

@code {
  private decimal _amount;
}
```

The value can be an `int`, `long`, `short`, `decimal`, `double` or `float`, or a nullable version of any of them. Any other type throws an `InvalidOperationException` when the component is first rendered.

While the input has focus, it shows the plain number (eg `1234.5`), so it's easy to edit. When the user leaves it, it shows the value in the `Format` you give (eg `£1,234.50` for `C2`). Without a `Format`, it shows the plain number.

The value changes as the user types, so anything bound to it updates straight away. Text that isn't a number leaves the value as it was, and the input shows as invalid until the user fixes it or leaves the input, when it goes back to showing the value. Clearing the input sets a nullable value to null. A non-nullable one keeps its last value.

Pasted text such as `£1,234.50` is accepted, using the current culture's currency symbol, decimal point and thousands separator.

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `Value` | `TValue` | The value. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<TValue>` | Fires on every change, including as the user types |
| `ValueExpression` | `Expression<Func<TValue>>` | Identifies the field for validation. Set for you by `@bind-Value` |
| `OnChange` | `EventCallback<object?>` | Fires when the user has finished changing the value, which is when they press Enter or leave the input, and only if the value is different from when they started. |
| `OnBlur` | `EventCallback` | Fires when the input loses focus |
| `Min` | `TValue` | The lowest value allowed. Not set by default |
| `Max` | `TValue` | The highest value allowed. Not set by default |
| `Step` | `TValue` | How much the spin buttons and the up and down arrow keys change the value by. Defaults to 1 |
| `Decimals` | `int?` | The number of decimal places to round to. Whole number types always round to whole numbers |
| `Format` | `string?` | A .NET number format, such as `N2` or `C2`, for when the input doesn't have focus |
| `Arrows` | `bool` | Shows the spin buttons. Defaults to `true` |
| `IncreaseValueText`, `DecreaseValueText` | `string` | The spin buttons' tooltips and accessible names, for translating. Default to "Increase value" and "Decrease value" |
| `Placeholder` | `string?` | Shown when the input is empty |
| `Enabled` | `bool` | Defaults to `true` |
| `ReadOnly` | `bool` | The value can be seen and copied but not changed |
| `Width` | `string?` | A CSS width, such as `150px` |
| `Size` | `string?` | `sm`, `md` (the default) or `lg`. See `PixataThemeConstants.Size` |
| `Id` | `string?` | The input's id, for a `<label for="...">` |
| `Class` | `string?` | Extra classes for the outer element |
| `TabIndex` | `int?` | The input's tab index |
| `InputMode` | `string?` | The input's `inputmode`, which picks the keyboard on a phone. Defaults to `decimal`, or `numeric` for whole number types |

Any other attributes, such as `aria-label` or `data-` attributes, are added to the input.

### When Min, Max and Decimals are applied

They are applied when the user presses Enter or leaves the input, not as they type. Otherwise, typing `15` into a box with a `Min` of 10 would turn the first `1` into `10`. While the user is typing, the bound value may be outside the range, so validate the model as well if that matters.

## In a form

Inside an `EditForm`, the input tells the form when its field changes, and shows as invalid (with a red border) when the field has validation messages. Use it with the [form layout components](Readme.Forms.md)...

```xml
<EditForm Model="_payment" OnValidSubmit="Save">
  <DataAnnotationsValidator />
  <FormSingle Label="Amount" Id="amount" Required="true">
    <PixataNumericTextBox Id="amount" @bind-Value="_payment.Amount" Format="N2" Decimals="2" Min="0.01m" />
    <ValidationMessage For="() => _payment.Amount" />
  </FormSingle>
</EditForm>
```

## Keyboard

| Key | What it does |
| --- | --- |
| Up arrow | Adds `Step` to the value, up to `Max` |
| Down arrow | Takes `Step` off the value, down to `Min` |
| Enter | Applies `Min`, `Max` and `Decimals`, and fires `OnChange` if the value has changed |

The spin buttons can't be tabbed to, as the arrow keys do the same job. Clicking them leaves the focus in the input, so `OnChange` fires once the user leaves it, as it does after typing.

## Accessibility

The input has `role="spinbutton"`, with `aria-valuenow`, `aria-valuemin` and `aria-valuemax`, and `aria-invalid` when it shows as invalid. Give it a `<label for="...">` pointing at its `Id`, or an `aria-label`.

## Styling

The numeric text box uses the date input's CSS custom properties (`--pixata-datepicker-*`), so the two look the same next to each other in a form. See [Styling](Readme.Styling.md). The classes are `pixata-blazor-numerictextbox` on the outer element, `pixata-blazor-numerictextbox-input` on the input, `pixata-blazor-numerictextbox-spinner` on the spin buttons, and `pixata-blazor-numerictextbox-invalid` and `pixata-blazor-numerictextbox-disabled` for those states. To right-align the number, set `text-align: right` on the outer element, eg with a `Class`.

## Moving from Telerik

The parameters above have the same names and meanings as Telerik's. These Telerik features aren't supported...

- `SelectOnFocus`, `DebounceDelay`, `LargeStep`, `Rounded` and `FillMode`
- Blocking keys that can't be part of a number as they're typed. Instead, the input shows as invalid until the text is a number
- Limiting the number of decimal places as they're typed. Instead, the value is rounded to `Decimals` when the user leaves the input
