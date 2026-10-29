# Text area

>Part of [Pixata.Blazor](Readme.md)

`PixataTextArea` is a multi-line text box. It works on its own or inside an `EditForm`, and it looks like the [text box](Readme.TextBox.md) and the drop-down lists, so they sit together in a form. It has the same parameters as Telerik's TextArea, so markup can usually move across by changing the component's name.

See the [live demo](https://test.pixata.co.uk/TextAreaSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/TextAreaSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Basic use

```xml
<label for="notes">Notes</label>
<PixataTextArea Id="notes" @bind-Value="_notes" Rows="5" />

@code {
  private string? _notes;
}
```

As with the text box, the value changes as the user types, unless you set a `DebounceDelay`, and `OnChange` fires when the user leaves the box after changing the text. Enter adds a new line rather than committing the text, as it does in any text area.

## Size and resizing

`Rows` sets the height, in lines of text (3 by default). The user can drag the bottom corner to make it taller or shorter. `ResizeMode` changes that...

| `ResizeMode` | What the user can do |
| --- | --- |
| `Vertical` | Make it taller or shorter (the default) |
| `Horizontal` | Make it wider or narrower |
| `Both` | Resize it both ways |
| `None` | Not resize it |
| `Auto` | Nothing: it grows and shrinks to fit its text, starting at `Rows` lines, rather than scrolling |

`AutoSize="true"` is the same as `ResizeMode="PixataTextAreaResizeMode.Auto"`, as in Telerik's.

## Character count

`ShowCharacterCount="true"` shows how many characters have been typed, under the text. With a `MaxLength`, it shows the count out of the maximum (eg `57 / 200`). The count is linked to the text area with `aria-describedby`, so screen readers read it out...

```xml
<PixataTextArea @bind-Value="_bio" MaxLength="200" ShowCharacterCount="true" aria-label="About you" />
```

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `Value` | `string?` | The text. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<string?>` | Fires as the user types, or after `DebounceDelay` |
| `ValueExpression` | `Expression<Func<string?>>` | Identifies the field for validation. Set for you by `@bind-Value` |
| `OnChange` | `EventCallback<object?>` | Fires when the user leaves the box, if the text has changed |
| `OnBlur` | `EventCallback` | Fires when the box loses focus |
| `DebounceDelay` | `int` | Milliseconds to wait after the user stops typing before `ValueChanged` fires. Defaults to 0, which raises it on every key |
| `Rows` | `int` | The height in lines of text, or the starting height with `ResizeMode.Auto`. Defaults to 3 |
| `ResizeMode` | `PixataTextAreaResizeMode` | `Vertical` (the default), `Horizontal`, `Both`, `None` or `Auto` |
| `AutoSize` | `bool` | The same as `ResizeMode="PixataTextAreaResizeMode.Auto"` |
| `ShowCharacterCount` | `bool` | Shows the number of characters typed, out of `MaxLength` if it's set |
| `Placeholder` | `string?` | Shown when the box is empty |
| `MaxLength` | `int?` | The most characters that can be typed |
| `InputMode`, `AutoComplete`, `AutoCapitalize` | `string?` | The text area's `inputmode`, `autocomplete` and `autocapitalize` |
| `SpellCheck` | `bool?` | Whether the browser checks the spelling. Left to the browser when null |
| `Title` | `string?` | The text area's tooltip |
| `Enabled` | `bool` | Defaults to `true` |
| `ReadOnly` | `bool` | The text can be seen and copied but not changed |
| `Width` | `string?` | A CSS width, such as `400px`. By default it fills the width it's given |
| `Size` | `string?` | `sm`, `md` (the default) or `lg`. See `PixataThemeConstants.Size` |
| `Rounded` | `string?` | `none`, `sm`, `md` (the default), `lg` or `full`. See `PixataThemeConstants.Rounded` |
| `FillMode` | `string?` | `solid` (the default), `flat` or `outline`. See `PixataThemeConstants.FillMode` |
| `Id` | `string?` | The text area's id, for a `<label for="...">` |
| `Class` | `string?` | Extra classes for the outer element |
| `TabIndex` | `int?` | The text area's tab index |

Any other attributes, such as `aria-label`, `name` or `data-` attributes, are added to the text area. `FocusAsync()` puts the focus in it.

## Styling

The text area uses the shared input classes from [pixata.css](Readme.Styling.md) (`pixata-blazor-input`, `pixata-blazor-input-inner` and so on), so it follows any changes you make to those. On top of that, it has these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-textarea` | The outer element, which is the part the user resizes |
| `pixata-blazor-textarea-resize-vertical`, `-horizontal`, `-both`, `-none`, `-auto` | Added to the outer element for the `ResizeMode` |
| `pixata-blazor-textarea-input` | The text area |
| `pixata-blazor-textarea-count` | The character count |
