# Radio group

>Part of [Pixata.Blazor](Readme.md)

`PixataRadioGroup` shows a radio button for each item in a list, and binds the value of the one the user picks. It works with lists of primitives (strings, numbers, enum values) or of objects, lays the options out in a column or a row, works inside an `EditForm`, and is styled through CSS custom properties, so you can fit it to your own theme.

See the [live demo](https://test.pixata.co.uk/RadioGroupSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/RadioGroupSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Primitive data

When the items are primitives, each item is both the text and the value...

```xml
<PixataRadioGroup Data="_sizes" @bind-Value="_size" AriaLabel="Size" />

@code {
  private readonly List<string> _sizes = ["Small", "Medium", "Large"];
  private string? _size;
}
```

Strings, numbers, `bool`, `decimal`, dates and times, `Guid` and enum values all count as primitives, so you can bind straight to an enum...

```xml
<PixataRadioGroup Data="Enum.GetValues<Pets>()" @bind-Value="_pet" AriaLabel="Pet" />
```

## Model data

When the items are objects, `TextField` and `ValueField` name the properties that hold the text and the value. They default to `Text` and `Value`, and nested properties can be given with dots (`Address.City`)...

```xml
<PixataRadioGroup Data="_contactMethods" TextField="Name" ValueField="Id" @bind-Value="_contactMethodId" AriaLabel="Contact method" />

@code {
  private List<ContactMethod> _contactMethods = [];
  private int? _contactMethodId;
}
```

The component has two type parameters, `TItem` (the type of the items) and `TValue` (the type of the value), which Blazor works out from `Data` and the bound value, so you rarely need to give them.

## Using it

The radio buttons are native `<input type="radio">` elements sharing one `name`, inside a `<ul role="radiogroup">`, so they behave exactly as the browser's own radio groups do...

| Input | What it does |
| --- | --- |
| Click or tap a radio button or its label | Picks that item |
| Tab | Moves into the group (to the picked item, or the first one if none is picked) and then out of it, as a single stop |
| Arrow keys | Move to the next or previous item and pick it, skipping disabled ones |
| Space | Picks the focused item |

On touch screens each option is at least `--pixata-blazor-touch-target` (2.5rem) high, and its label fills that height, so the options are easy to tap.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Data` | `IEnumerable<TItem>` | | The items, one radio button each |
| `Value` | `TValue` | | The value of the picked item. Use `@bind-Value` for two-way binding |
| `ValueChanged` | `EventCallback<TValue>` | | Raised when the value changes. Set for you by `@bind-Value` |
| `ValueExpression` | `Expression<Func<TValue>>` | | Identifies the bound field for validation. Set for you by `@bind-Value` |
| `TextField` | `string` | `"Text"` | The property of a model item that holds its text. Ignored for primitive data |
| `ValueField` | `string` | `"Value"` | The property of a model item that holds its value. Ignored for primitive data |
| `DisabledField` | `string` | | The `bool` property of a model item that says whether it is disabled |
| `ItemTemplate` | `RenderFragment<TItem>` | | Renders each item's label, with the item as the context |
| `Layout` | `PixataRadioGroupLayout` | `Vertical` | `Vertical` stacks the options; `Horizontal` puts them in a row that wraps when it runs out of room |
| `LabelPosition` | `PixataRadioGroupLabelPosition` | `After` | Whether each label comes `Before` or `After` its radio button |
| `Size` | `string` | `"md"` | The size of the radio buttons: `"sm"`, `"md"` or `"lg"`. The values are in `PixataThemeConstants.Size` |
| `Enabled` | `bool` | `true` | When false, every radio button is disabled |
| `Id` | `string` | | The `id` of the group. The radio buttons get `{Id}-0`, `{Id}-1` and so on. If you don't set it, a unique one is generated |
| `Name` | `string` | | The `name` the radio buttons share. If you don't set it, a unique one is generated |
| `TabIndex` | `int?` | | The `tabindex` of the radio buttons |
| `Class` | `string` | | Extra CSS classes for the group |
| `AriaLabel` | `string` | | The `aria-label` of the group |
| `AriaLabelledBy` | `string` | | The `aria-labelledby` of the group, eg the id of a heading or a paragraph that asks the question |
| `AriaDescribedBy` | `string` | | The `aria-describedby` of the group |

Any other attributes you add (`title`, `data-*` and so on) are rendered on the group's `<ul>`.

Give the group a name, with `AriaLabel` or `AriaLabelledBy`, so screen readers can announce what the options are for.

## Events

| Event | Argument | Description |
| --- | --- | --- |
| `ValueChanged` | `TValue` | Raised when the value changes |
| `OnChange` | `object` | Raised when the user picks an item, with its value |

```xml
<PixataRadioGroup Data="_sizes" @bind-Value="_size" OnChange="@((object value) => _log = $"Picked {value}")" AriaLabel="Size" />
```

## Methods

| Method | Description |
| --- | --- |
| `FocusItemAsync(int index)` | Moves the focus to the radio button for the item at that index (counting from 0) in `Data` |

```xml
<PixataRadioGroup @ref="_group" Data="_sizes" @bind-Value="_size" AriaLabel="Size" />

@code {
  private PixataRadioGroup<string, string> _group = null!;

  private Task FocusFirst() =>
    _group.FocusItemAsync(0);
}
```

## Templates

`ItemTemplate` replaces the text of each label with your own markup. The template goes inside the `<label>`, so clicking anywhere on it still picks the item...

```xml
<PixataRadioGroup Data="_contactMethods" ValueField="Id" @bind-Value="_contactMethodId" AriaLabel="Contact method">
  <ItemTemplate>
    <strong>@context.Name</strong> <small>(@context.Description)</small>
  </ItemTemplate>
</PixataRadioGroup>
```

## Disabling items

`Enabled="false"` disables the whole group. To disable individual items, give your model a `bool` property and name it in `DisabledField`...

```xml
<PixataRadioGroup Data="_contactMethods" TextField="Name" ValueField="Id" DisabledField="Unavailable" @bind-Value="_contactMethodId" AriaLabel="Contact method" />
```

Disabled items are faded, can't be picked, and are skipped by the arrow keys.

## Inside an EditForm

Bind it with `@bind-Value` and it takes part in validation like any other input. It tells the form when the value changes, and when the field has validation messages the group gets the `pixata-blazor-invalid` class (red radio button borders) and `aria-invalid="true"`...

```xml
<EditForm Model="_model" OnValidSubmit="Save">
  <DataAnnotationsValidator />
  <p id="contact-question">How should we contact you?</p>
  <PixataRadioGroup Data="_contactMethods" TextField="Name" ValueField="Id" @bind-Value="_model.ContactMethodId" AriaLabelledBy="contact-question" />
  <ValidationMessage For="() => _model.ContactMethodId" />
  <button type="submit">Save</button>
</EditForm>
```

...where the model has something like this...

```csharp
[Required(ErrorMessage = "Please choose how we should contact you")]
public int? ContactMethodId { get; set; }
```

## Styling

The group is made up of these elements and classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-radiogroup` | The `<ul>`. It also has `pixata-blazor-radiogroup-vertical` or `pixata-blazor-radiogroup-horizontal`, `pixata-blazor-radiogroup-label-before` when the labels come first, and `pixata-blazor-disabled` or `pixata-blazor-invalid` when they apply |
| `pixata-blazor-radiogroup-item` | Each `<li>`, holding a radio button and its label. The picked one also has `pixata-blazor-radiogroup-item-checked`, and a disabled one `pixata-blazor-disabled` |
| `pixata-blazor-radiogroup-radio` | The radio button, plus `pixata-blazor-radiogroup-radio-sm`, `-md` or `-lg` |
| `pixata-blazor-radiogroup-label` | The label |

The colours and sizes come from these custom properties, which you can override on `:root` or on any container...

| Variable | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-radiogroup-radio-size-sm`, `-md`, `-lg` | `12px`, `16px`, `20px` | The size of the radio buttons |
| `--pixata-blazor-radiogroup-radio-bg` | `#fff` | The background of an unpicked radio button |
| `--pixata-blazor-radiogroup-radio-border-color` | `--pixata-blazor-border-color` | The border of an unpicked radio button |
| `--pixata-blazor-radiogroup-checked-bg` | `--pixata-blazor-primary` | The background of the picked radio button |
| `--pixata-blazor-radiogroup-checked-border-color` | `--pixata-blazor-primary` | The border of the picked radio button |
| `--pixata-blazor-radiogroup-dot-color` | `--pixata-blazor-primary-color` | The dot in the picked radio button |
| `--pixata-blazor-radiogroup-dot-size` | `57%` | The size of the dot, as a percentage of the inside of the radio button |
| `--pixata-blazor-radiogroup-focus-shadow` | A faint 2px ring | The focus ring on an unpicked radio button, when focused from the keyboard |
| `--pixata-blazor-radiogroup-checked-focus-shadow` | A 2px ring in the primary colour | The focus ring on the picked radio button |
| `--pixata-blazor-radiogroup-item-gap` | `--pixata-blazor-spacing` | The space between a radio button and its label |
| `--pixata-blazor-radiogroup-item-padding` | `--pixata-blazor-spacing` top and bottom | The padding of each option |
| `--pixata-blazor-radiogroup-horizontal-gap` | 4 x `--pixata-blazor-spacing` | The space between the options in a horizontal group |

For example, to have darker borders and green radio buttons...

```css
:root {
  --pixata-blazor-radiogroup-radio-border-color: rgba(0, 0, 0, .4);
  --pixata-blazor-radiogroup-checked-bg: #37b400;
  --pixata-blazor-radiogroup-checked-border-color: #37b400;
}
```
