# Text box

>Part of [Pixata.Blazor](Readme.md)

`PixataTextBox` is a single-line text box, which can also be a password box. It works on its own or inside an `EditForm`, and it looks like the drop-down lists, so the two sit side by side in a form. It has the same parameters as Telerik's TextBox, so markup can usually move across by changing the component's name.

See the [live demo](https://test.pixata.co.uk/TextBoxSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/TextBoxSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label for="name">Name</label>
<PixataTextBox Id="name" @bind-Value="_name" Placeholder="Your name" />

@code {
  private string? _name;
}
```

The value changes as the user types, so anything bound to it updates straight away. If that's too often (say each change starts a search), set `DebounceDelay` to the number of milliseconds to wait after the user stops typing. Pressing Enter or leaving the box updates the value straight away, whatever the delay.

`OnChange` fires when the user has finished changing the text, which is when they press Enter or leave the box, and only if the text is different from when they started.

By default the box fills the width it's given. Set `Width` to make it narrower.

## Passwords

`Password="true"` shows the text as dots. Add `ShowPasswordButton="true"` for a button that shows and hides it...

```xml
<PixataTextBox Id="password" @bind-Value="_password" Password="true" ShowPasswordButton="true" AutoComplete="current-password" />
```

Set `AutoComplete` to `current-password` on a login form, or `new-password` when the user is choosing one, so the browser's password manager knows what to do.

## Prefix and suffix

`TextBoxPrefixTemplate` and `TextBoxSuffixTemplate` put content (an icon, a unit and so on) inside the box, before and after the text. A line separates them from the text, unless you set `ShowPrefixSeparator` or `ShowSuffixSeparator` to `false`...

```xml
<PixataTextBox @bind-Value="_search" Placeholder="Search" ShowClearButton="true" aria-label="Search">
  <TextBoxPrefixTemplate><PixataIcon Icon="PixataSvgIcons.Search" /></TextBoxPrefixTemplate>
</PixataTextBox>
```

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `Value` | `string?` | The text. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<string?>` | Fires as the user types, or after `DebounceDelay` |
| `ValueExpression` | `Expression<Func<string?>>` | Identifies the field for validation. Set for you by `@bind-Value` |
| `OnChange` | `EventCallback<object?>` | Fires when the user presses Enter or leaves the box, if the text has changed |
| `OnBlur` | `EventCallback` | Fires when the box loses focus |
| `DebounceDelay` | `int` | Milliseconds to wait after the user stops typing before `ValueChanged` fires. Defaults to 0, which raises it on every key |
| `Password` | `bool` | Shows the text as dots |
| `ShowPasswordButton` | `bool` | With `Password`, shows a button that shows and hides the text |
| `ShowPasswordText`, `HidePasswordText` | `string` | That button's tooltip and accessible name, for translating. Default to "Show password" and "Hide password" |
| `ShowClearButton` | `bool` | Shows a button that clears the text, when there is any |
| `ClearButtonText` | `string` | The clear button's tooltip and accessible name. Defaults to "Clear" |
| `Placeholder` | `string?` | Shown when the box is empty |
| `MaxLength` | `int?` | The most characters that can be typed |
| `InputMode` | `string?` | The input's `inputmode`, which picks the keyboard on a phone, eg `email` or `tel` |
| `AutoComplete` | `string?` | The input's `autocomplete`, eg `email`, `current-password` or `off` |
| `AutoCapitalize` | `string?` | The input's `autocapitalize`, eg `off` or `words` |
| `SpellCheck` | `bool?` | Whether the browser checks the spelling. Left to the browser when null |
| `Title` | `string?` | The input's tooltip |
| `TextBoxPrefixTemplate`, `TextBoxSuffixTemplate` | `RenderFragment?` | Content before and after the text |
| `ShowPrefixSeparator`, `ShowSuffixSeparator` | `bool` | Draw a line between the prefix or suffix and the text. Default to `true` |
| `Enabled` | `bool` | Defaults to `true` |
| `ReadOnly` | `bool` | The text can be seen and copied but not changed |
| `Width` | `string?` | A CSS width, such as `250px` |
| `Size` | `string?` | `sm`, `md` (the default) or `lg`. See `PixataThemeConstants.Size` |
| `Rounded` | `string?` | `none`, `sm`, `md` (the default), `lg` or `full`. See `PixataThemeConstants.Rounded` |
| `FillMode` | `string?` | `solid` (the default), `flat` or `outline`. See `PixataThemeConstants.FillMode` |
| `Id` | `string?` | The input's id, for a `<label for="...">` |
| `Class` | `string?` | Extra classes for the outer element |
| `TabIndex` | `int?` | The input's tab index |

Any other attributes, such as `aria-label`, `name` or `data-` attributes, are added to the input. `FocusAsync()` puts the focus in the box.

## In a form

Inside an `EditForm`, the box tells the form when its field changes, and shows as invalid (with a red border) when the field has validation messages...

```xml
<EditForm Model="_contact" OnValidSubmit="Save">
  <DataAnnotationsValidator />
  <FormSingle Label="Email" Id="email" Required="true">
    <PixataTextBox Id="email" @bind-Value="_contact.Email" InputMode="email" AutoComplete="email" />
    <ValidationMessage For="() => _contact.Email" />
  </FormSingle>
  <PixataPrimaryButton>Save</PixataPrimaryButton>
</EditForm>
```

## Styling

The text box uses the shared input classes from [pixata.css](Readme.Styling.md) (`pixata-blazor-input`, `pixata-blazor-input-inner` and so on), as the drop-down lists do, so it follows any changes you make to those. On top of that, it has these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-textbox` | The outer element |
| `pixata-blazor-textbox-input` | The input |
| `pixata-blazor-textbox-prefix`, `pixata-blazor-textbox-suffix` | Around the prefix and suffix templates, with `pixata-blazor-textbox-separator` when they have a line |
| `pixata-blazor-textbox-reveal` | The button that shows and hides a password |
| `pixata-blazor-clear-value` | The clear button |
