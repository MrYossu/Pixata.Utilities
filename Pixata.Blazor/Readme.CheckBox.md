# Checkbox

>Part of [Pixata.Blazor](Readme.md)

`PixataCheckBox` is a checkbox for `bool` and `bool?` values, with an indeterminate state. It has the same parameters as Telerik's CheckBox, so markup can move across by changing the name. It works on its own or inside an `EditForm`, and is styled through CSS custom properties, so you can fit it to your own theme.

Use it where the user picks from a set of options (a row of roles, say), or for a single "I agree" box. For a setting that is turned on or off, [`PixataSwitch`](Readme.Switch.md) may suit better.

See the [live demo](https://test.pixata.co.uk/CheckBoxSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/CheckBoxSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label><PixataCheckBox @bind-Value="_canLogIn" /> Can log in?</label>

@code {
  private bool _canLogIn;
}
```

The checkbox can go inside its label, as above, or a `<label for="...">` can point at its `Id`. Either way, clicking the label's text toggles it...

```xml
<PixataCheckBox Id="global-admin" @bind-Value="_globalAdmin" />
<label for="global-admin">Global admin</label>
```

If there is no visible label, give it an `AriaLabel` instead.

The checkbox is a `<button>` with `role="checkbox"` and `aria-checked`, rather than a native `<input type="checkbox">`, so it can show the indeterminate state without any script. Screen readers announce it as a checkbox, and Space (or Enter) toggles it. If it has a `name`, it is a native checkbox instead (see [Posting with a form](#posting-with-a-form)).

## Three states

A `bool?` that is null is shown as indeterminate (a dash), with `aria-checked="mixed"`. Clicking it ticks it (sets it to true), and from then on it toggles between true and false, as a native checkbox does.

Any checkbox can also be shown as indeterminate, whatever its value, by setting `Indeterminate`. This is useful for a "select all" box when only some of the items are selected. Clicking it clears `Indeterminate` and ticks the box, so bind it with `@bind-Indeterminate` if you want to know...

```xml
<PixataCheckBox Value="@AllSelected" Indeterminate="@SomeSelected" ValueChanged="@((bool v) => SelectAll(v))" AriaLabel="Select all" />
```

Using any type other than `bool` or `bool?` throws an `InvalidOperationException` when the checkbox is rendered.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` | `bool` or `bool?` | | The value. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<TValue>` | | Raised when the value changes. Set for you by `@bind-Value` |
| `ValueExpression` | `Expression<Func<TValue>>` | | Identifies the bound field for validation. Set for you by `@bind-Value` |
| `Indeterminate` | `bool` | `false` | Shows the checkbox as indeterminate, whatever its value. Cleared when the user clicks it. Use `@bind-Indeterminate` for two-way binding |
| `IndeterminateChanged` | `EventCallback<bool>` | | Raised when a click clears `Indeterminate` |
| `Size` | `string` | `"md"` | `"sm"`, `"md"` or `"lg"` (12, 16 or 20 pixels). The values are in `PixataThemeConstants.Size` |
| `Rounded` | `string` | `"md"` | The roundness of the corners: `"none"`, `"sm"`, `"md"`, `"lg"` or `"full"` (a circle). The values are in `PixataThemeConstants.Rounded` |
| `Enabled` | `bool` | `true` | When false, the checkbox is greyed out, can't be focused and can't be changed |
| `Id` | `string` | | The `id` of the button, so a `<label for="...">` can point at it |
| `Name` | `string` | | The form field name. When set, the checkbox is a native `<input type="checkbox" value="true">`, so it is posted with the form. See [Posting with a form](#posting-with-a-form) |
| `TabIndex` | `int?` | | The `tabindex` of the button |
| `Class` | `string` | | Extra CSS classes for the button |
| `AriaLabel` | `string` | | The `aria-label`, for when there is no visible label |
| `AriaLabelledBy` | `string` | | The `aria-labelledby` |
| `AriaDescribedBy` | `string` | | The `aria-describedby` |

Any other attributes you add (`title`, `data-*` and so on) are rendered on the button (or the native checkbox, if it has a `name`).

## Events

| Event | Argument | Description |
| --- | --- | --- |
| `ValueChanged` | `TValue` | Raised when the value changes |
| `OnChange` | `object` | Raised when the user changes the checkbox, after `ValueChanged`, with the new value (a `bool`). It is also raised when a click only clears `Indeterminate` |

`OnChange` is the place to react to the user's change, as the bound value has already been updated...

```xml
<PixataCheckBox @bind-Value="_user.CanLogIn" OnChange="@((object value) => CanLogInChanged((bool)value))" AriaLabel="Can log in?" />

@code {
  private void CanLogInChanged(bool canLogIn) {
    if (!canLogIn) {
      _user.Email = "";
    }
  }
}
```

## Inside an EditForm

Bind it with `@bind-Value` and it takes part in validation like any other input. It tells the form when the value changes, and gets the `pixata-blazor-invalid` class (a red border) and `aria-invalid="true"` when the field has validation messages...

```xml
<EditForm Model="_model" OnValidSubmit="Save">
  <FluentValidator />
  <label><PixataCheckBox @bind-Value="_model.AcceptTerms" /> I agree to the terms</label>
  <ValidationMessage For="() => _model.AcceptTerms" />
  <PixataPrimaryButton>Save</PixataPrimaryButton>
</EditForm>
```

`FormRowCheckbox` (see [Forms](Readme.Forms.md)) uses a `PixataCheckBox` too.

## Posting with a form

Statically rendered pages, such as the Identity pages (log in, reset password and so on), are plain HTML forms posted back to the server, so their values have to come from real form fields with a `name`, and there is no circuit to handle the button's clicks. Give the checkbox a `name` and it renders a native `<input type="checkbox" value="true">` instead of the button, styled the same way. It is posted as `true` when ticked and not at all when not, as `InputCheckbox` is, so it can replace the "Remember me" box on the log in page...

```xml
<EditForm Model="Input" method="post" OnValidSubmit="LoginUser" FormName="login">
  ...
  <label><PixataCheckBox @bind-Value="Input.RememberMe" name="Input.RememberMe" /> Remember me</label>
  ...
</EditForm>
```

Unlike `InputCheckbox`, the name isn't worked out from `@bind-Value`, so you have to give it yourself. The checkbox still works in an interactive page, where it raises `ValueChanged` and `OnChange` as usual.

A native checkbox can only show the indeterminate state (a `null` `bool?`, or `Indeterminate`) by its look, as screen readers can't be told about it without script. An indeterminate checkbox isn't ticked, so it isn't posted. As with any native checkbox, a disabled one isn't posted either, and on touch screens its tappable area is its own size rather than `--pixata-blazor-touch-target`.

## Styling

The checkbox is a button with the `pixata-blazor-checkbox` class, a size class (`pixata-blazor-checkbox-sm`, `-md` or `-lg`) and a `pixata-blazor-rounded-*` class. It also has `pixata-blazor-checkbox-checked` or `pixata-blazor-checkbox-indeterminate`, and `pixata-blazor-disabled` or `pixata-blazor-invalid`, when they apply.

The same `pixata-blazor-checkbox` class draws the checkboxes in the grid and the tree view, so all the checkboxes in an app look the same, and the custom properties below change them all. Override them on `:root` or on any container...

| Variable | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-checkbox-size-sm`, `-md`, `-lg` | `12px`, `16px`, `20px` | The size for each `Size` |
| `--pixata-blazor-checkbox-bg` | `--pixata-blazor-input-bg` | The background when not ticked |
| `--pixata-blazor-checkbox-border-color` | `--pixata-blazor-input-border-color` | The border when not ticked |
| `--pixata-blazor-checkbox-hover-border-color` | `--pixata-blazor-input-hover-border-color` | The border under the mouse |
| `--pixata-blazor-checkbox-checked-bg` | `--pixata-blazor-primary` | The background when ticked. The tick itself is white |
| `--pixata-blazor-checkbox-checked-border-color` | `--pixata-blazor-primary` | The border when ticked |
| `--pixata-blazor-checkbox-indeterminate-color` | `--pixata-blazor-primary` | The colour of the dash when indeterminate |
| `--pixata-blazor-checkbox-focus-border-color` | `--pixata-blazor-input-focus-border-color` | The border when focused from the keyboard |
| `--pixata-blazor-checkbox-focus-shadow` | `--pixata-blazor-input-focus-shadow` | The focus ring, the same as the other inputs' |

For example, to have green checkboxes...

```css
:root {
  --pixata-blazor-checkbox-checked-bg: #198754;
  --pixata-blazor-checkbox-checked-border-color: #198754;
}
```

On touch screens the area you can tap is at least `--pixata-blazor-touch-target` (2.5rem) square, and the transitions are turned off for users who have asked for reduced motion.
