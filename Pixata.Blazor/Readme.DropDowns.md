# Drop-downs

>Part of [Pixata.Blazor](Readme.md)

Three components for choosing from a list...

| Component | What the user does | `Value` is |
| --- | --- | --- |
| `PixataDropDownList` | Picks one item from the list. They can't type a value of their own | The selected item's value |
| `PixataComboBox` | Picks one item from the list, or types to find one. With `AllowCustom`, they can type a value that isn't in the list | The selected item's value (or the typed value) |
| `PixataMultiSelect` | Picks any number of items, which are shown as removable tags | A `List<TValue>` of the selected items' values |

They share the same list, data binding, filtering, grouping, templates, virtual scrolling and keyboard handling, so most of what follows applies to all three.

You can see them in action on the [sample page](https://test.pixata.co.uk/DropDownsSample) ([source](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/DropDownsSample.razor)).

## Setup

There are no services to register. Add `@using Pixata.Blazor.Components` to your `_Imports.razor`, and make sure your host page includes the package's stylesheet (see [Styling](Readme.Styling.md))...

```html
<link href="_content/Pixata.Blazor/pixata.css" rel="stylesheet" />
```

The components load their own script module (`_content/Pixata.Blazor/pixata-dropdowns.js`) the first time one is shown, so there is nothing else to add to the page. They render fine when prerendered, and work in Blazor Server and WebAssembly.

## Quick start

A list of strings, where the value is the string itself...

```razor
<PixataDropDownList Data="@_fruit" @bind-Value="@_chosenFruit" DefaultText="Choose a fruit..." />

@code {
  private readonly List<string> _fruit = ["Apple", "Banana", "Cherry"];
  private string? _chosenFruit;
}
```

A list of objects, where the user sees the `Name` and the value is the `Id`...

```razor
<PixataComboBox Data="@_countries" TextField="Name" ValueField="Id" @bind-Value="@_countryId" Placeholder="Choose a country..." />

<PixataMultiSelect Data="@_countries" TextField="Name" ValueField="Id" @bind-Value="@_countryIds" Placeholder="Choose countries..." />

@code {
  private readonly List<Country> _countries = [new(1, "France"), new(2, "Japan"), new(3, "Kenya")];
  private int _countryId;
  private List<int> _countryIds = [];

  private record Country(int Id, string Name);
}
```

## Data

The components have two type parameters, `TItem` (the type of the items in the list) and `TValue` (the type of the value), which Blazor can usually work out from `Data` and `Value`. If it can't (for example when you use `OnRead` instead of `Data`), set them yourself: `<PixataComboBox TItem="Product" TValue="int" ...>`.

- **Primitive data** (strings, numbers, enums, dates and so on): each item is both the text shown and the value, and `TextField` and `ValueField` are ignored.
- **Model data**: `TextField` names the field holding the text shown in the list (default `Text`), and `ValueField` names the field holding the value (default `Value`). Nested fields can be given with a dot, eg `TextField="Address.City"`. Set `ValueField=""` to have the item itself as the value (`TValue` is then the same as `TItem`).

If you change the items in a list you passed as `Data` without passing a new list, call `Rebind()` so the component picks up the change. Passing a new list (or a list with a different number of items) is picked up automatically.

When no item is selected, the `PixataDropDownList` shows its `DefaultText`, which is also the first option in the list, so the user can go back to having nothing selected. The `PixataComboBox` and `PixataMultiSelect` show their `Placeholder` instead. All three can show a button to clear the value (`ShowClearButton`, which is on by default for the combo box and multi-select, and off for the drop-down list).

## Filtering

Set `Filterable="true"` to let the user filter the list by typing. The `PixataDropDownList` shows a filter box at the top of the list; the `PixataComboBox` and `PixataMultiSelect` filter by what is typed into the component itself.

`FilterOperator` says how the typed text is matched against the text of each item, ignoring case...

| `PixataStringFilterOperator` | Matches items whose text... |
| --- | --- |
| `StartsWith` (the default) | starts with the typed text |
| `EndsWith` | ends with the typed text |
| `Contains` | contains the typed text anywhere |
| `IsContainedIn` | is contained in the typed text |
| `DoesNotContain` | doesn't contain the typed text |
| `IsEqualTo` | is the typed text |

The same matching is available to your own code (eg in an `OnRead` handler) as an extension method: `PixataStringFilterOperator.Contains.Matches(itemText, typedText)`.

Without `Filterable`, typing still jumps to the first item that starts with what was typed. In the `PixataDropDownList`, typing a letter repeatedly cycles through the items starting with that letter, as in a native `<select>`.

For the `PixataMultiSelect`, `MinLength` sets how many characters must be typed before the list opens, which is useful with large lists, and `PersistFilterOnSelect` keeps the typed text (and so the filtered list) after an item is chosen.

## Grouping

Set `GroupField` to the field to group by. The items are sorted into groups by that field, and each group has a header, which stays at the top of the list while its items are scrolled past...

```razor
<PixataDropDownList Data="@_countries" TextField="Name" ValueField="Id" GroupField="Continent" @bind-Value="@_countryId" />
```

Grouping works with `Data`, and with `OnRead` when not virtually scrolling (the items your handler returns are grouped). It isn't applied when virtually scrolling with `OnRead`, as the component only has one page of items at a time.

## Custom values

With `AllowCustom="true"`, the `PixataComboBox` and `PixataMultiSelect` accept values that aren't in the list. The typed text becomes the value, so `TValue` should be `string` (or a type the text can be converted to, such as `int`).

- In the `PixataComboBox`, the typed text is kept when the user presses Enter or leaves the component. If the text matches an item exactly (ignoring case), that item is selected instead. Without `AllowCustom`, text that doesn't match an item clears the value.
- In the `PixataMultiSelect`, the typed text is shown at the top of the list with a plus sign; pressing Enter (or clicking it) adds it as a tag.

## Multi-select options

| Parameter | Default | What it does |
| --- | --- | --- |
| `AutoClose` | `true` | Whether the list closes after an item is chosen. Set it to `false` so the user can choose several items in one go |
| `TagMode` | `Multiple` | `PixataMultiSelectTagMode.Multiple` shows a tag for each selected item; `Single` shows one summary tag, eg "3 items selected" |
| `MaxAllowedTags` | none | In `Multiple` mode, the most tags to show. The rest are counted in a summary tag, eg "2 more selected" |
| `EnableCheckBoxes` | `false` | Shows a checkbox by each item |
| `EnableSelectAll` | `false` | Adds a "Select all" option at the top of the list, which selects all the items in the (filtered) list, or deselects them if they are all selected. Its checkbox shows when only some are selected. It isn't shown when virtually scrolling with `OnRead`, as the component doesn't have all the items |
| `ShowArrowButton` | `false` | Shows an arrow button that opens the list |

Removing a tag (with its &times; button, or Backspace in the empty text box) removes that value. Removing the summary tag removes all the values it stands for.

```razor
<PixataMultiSelect Data="@_countries" TextField="Name" ValueField="Id" @bind-Value="@_countryIds"
                   AutoClose="false" EnableCheckBoxes="true" EnableSelectAll="true" MaxAllowedTags="3" />
```

## Templates

| Template | Context | Component | What it renders |
| --- | --- | --- | --- |
| `ItemTemplate` | the item | all | Each item in the list |
| `ValueTemplate` | the selected item | `PixataDropDownList` | The selected item in the box |
| `TagTemplate` | the selected item | `PixataMultiSelect` | The content of each tag. For a custom value, it is only used if the value is a `TItem` (eg both are `string`) |
| `SummaryTagTemplate` | `PixataMultiSelectSummaryTagTemplateContext<TItem>`, with `Items` (all the selected items) and `Count` (how many the tag stands for) | `PixataMultiSelect` | The content of the summary tag |
| `HeaderTemplate` | none | all | Content at the top of the popup, above the list |
| `FooterTemplate` | none | all | Content at the bottom of the popup, below the list |
| `NoDataTemplate` | none | all | What the popup shows when there are no items, or none match the filter. Defaults to "No data found." |

```razor
<PixataDropDownList Data="@_countries" TextField="Name" ValueField="Id" @bind-Value="@_countryId">
  <ItemTemplate><strong>@context.Name</strong> (@context.Continent)</ItemTemplate>
  <ValueTemplate>@context.Name, @context.Continent</ValueTemplate>
  <FooterTemplate><div style="padding: 4px 8px;">@_countries.Count countries</div></FooterTemplate>
  <NoDataTemplate>No country matches that</NoDataTemplate>
</PixataDropDownList>
```

## Icon-only

Set `Icon` on a `PixataDropDownList` to show it as just an icon, in a small square box, rather than a box showing the selected item's text. Use it where there isn't room for the text, and the selected value doesn't need to be seen all the time, such as an operator picker in a narrow filter cell...

```razor
@* A PixataSvgIcon... *@
<PixataDropDownList Data="@_views" @bind-Value="@_view" Icon="PixataSvgIcons.MoreVertical" AriaLabel="View" />

@* ...or the CSS classes of an icon font *@
<PixataDropDownList Data="@_operators" @bind-Value="@_operator" Icon="@("fas fa-filter")" AriaLabel="Operator" />
```

For anything else, use `IconTemplate` in place of `Icon`. It can depend on the value, for example to show which operator is selected...

```razor
<PixataDropDownList Data="@_operators" @bind-Value="@_operator" AriaLabel="Operator">
  <IconTemplate><strong>@(_operator == "Greater than" ? ">" : "<")</strong></IconTemplate>
</PixataDropDownList>
```

How it works...

- The box shows only the icon. It has no text, `DefaultText`, clear button or arrow, is as high as a normal drop-down list and as wide as it is high. `Size`, `Rounded` and `FillMode` work as usual
- The list opens as normal when the box is clicked (or with Alt+Down, Enter or Space), and shows the items with their text, with the selected one marked. The keyboard works as for any drop-down list, so the arrow keys change the value while it's closed. `Filterable` works too, as the filter box is in the popup
- As the selected item can't be seen, its text is the box's tooltip, and is added to its accessible name, after `AriaLabel` if you set one (eg "Operator: Less than"), so both mouse and screen reader users can tell what's selected. Set `AriaLabel` to say what the list is for. A `title` attribute of your own replaces the tooltip
- The popup is as wide as its items need (it is only ever at least as wide as the box), so it isn't squashed to the width of the icon. Set `PopupSettings.Width` to fix its width. `Width` only affects the box, and you won't normally need it

## Virtual scrolling

For long lists, set `ScrollMode="PixataDropDownScrollMode.Virtual"`, and only the items that can be seen are rendered as the list scrolls. Every item must be the same height, given by `ItemHeight` (in pixels, default 28, which suits the medium size and plain text). If your `ItemTemplate` makes the items taller, or you use the large size, set `ItemHeight` to match.

```razor
<PixataDropDownList Data="@_products" TextField="Name" ValueField="Id" @bind-Value="@_productId"
                    ScrollMode="PixataDropDownScrollMode.Virtual" Filterable="true" />
```

## Supplying the data yourself (`OnRead`)

Instead of setting `Data`, you can handle `OnRead` and supply the items yourself, eg from a database or an API. It is raised when the component is first shown, when the user filters (after a short pause in their typing, set by `FilterDebounceDelay` on the drop-down list and `DebounceDelay` on the others, 150ms by default), when `Rebind()` is called, and, when virtually scrolling, each time the list needs another page. The component shows a loading indicator while it waits.

The handler gets a `PixataDropDownReadEventArgs`...

| Property | Set by | What it is |
| --- | --- | --- |
| `Filter` | the component | What the user typed, or `""`. Only return the items that match it |
| `FilterOperator` | the component | How to match `Filter` against each item's text (the component's `FilterOperator`) |
| `Skip` | the component | How many matching items to skip. Always 0 unless virtually scrolling |
| `Take` | the component | How many items to return. 0 means all of them; when virtually scrolling it is the `PageSize` (default 20) |
| `Data` | you | The items to show |
| `Total` | you | The total number of matching items (not just the ones in `Data`). Needed when virtually scrolling, so the component knows how long the list is |

When the items come from `OnRead`, the component doesn't filter them itself, so your handler must use `Filter`. When virtually scrolling, the item for the current value may not have been read yet, so give the component a `ValueMapper` that returns the item (or, for the multi-select, the items) for a value, so it can show its text.

```razor
<PixataComboBox TItem="Product" TValue="int" TextField="Name" ValueField="Id" @bind-Value="@_productId"
                OnRead="@ReadProducts" ValueMapper="@FindProduct"
                ScrollMode="PixataDropDownScrollMode.Virtual" Filterable="true" FilterOperator="PixataStringFilterOperator.Contains" />

@code {
  private int _productId;

  private async Task ReadProducts(PixataDropDownReadEventArgs args) {
    // Pass args.Filter, args.Skip and args.Take to your API, which does something like this...
    List<Product> matches = await ProductService.Search(args.Filter, args.FilterOperator);
    args.Data = args.Take > 0 ? matches.Skip(args.Skip).Take(args.Take).ToList() : matches;
    args.Total = matches.Count;
  }

  private Task<Product> FindProduct(int id) =>
    ProductService.Get(id);
}
```

## Forms

The components work inside an `EditForm`. Bind them with `@bind-Value`, and they tell the form when the value changes, so validation runs, and they show the invalid state (a red border, and `aria-invalid="true"`) when the field has validation messages. Show the messages with a `ValidationMessage` as usual...

```razor
<EditForm Model="@_model" OnValidSubmit="@Save">
  <DataAnnotationsValidator />
  <label for="country">Country</label>
  <PixataComboBox Data="@_countries" TextField="Name" ValueField="Id" @bind-Value="@_model.CountryId" Id="country" />
  <ValidationMessage For="@(() => _model.CountryId)" />
  <button type="submit">Save</button>
</EditForm>
```

`Id` goes on the element that gets the focus (the text box of the combo box and multi-select, and the box itself for the drop-down list), so a `<label for="...">` works with the combo box and multi-select. For the drop-down list, give the label an `id` and pass it as `AriaLabelledBy`, or use `AriaLabel`.

## Keyboard and mouse

The list closes when an item is chosen (except in a multi-select with `AutoClose="false"`), when the user presses Esc, and when they click outside the component or tab away from it. Esc doesn't propagate, so it won't also close a dialog that the component is in. On touch screens the items and buttons are larger, to be easier to tap.

| Key | When the list is closed | When the list is open |
| --- | --- | --- |
| Alt+Down | Opens the list | |
| Alt+Up | | Closes the list |
| Down / Up | Drop-down list and combo box: selects the next / previous item. Multi-select: opens the list | Moves to the next / previous item |
| Home / End | Drop-down list: selects the first / last item | Moves to the first / last item |
| Page Down / Page Up | | Moves down / up a page of items |
| Enter | Drop-down list: opens the list. Combo box: keeps the typed text (see [custom values](#custom-values)). Multi-select: adds the typed text if `AllowCustom`, otherwise opens the list | Chooses the highlighted item (in a multi-select, selects or deselects it) |
| Space | Drop-down list: opens the list | Drop-down list: chooses the highlighted item |
| Esc | Combo box: puts back the text of the selected item | Closes the list |
| Backspace | Multi-select, with nothing typed: removes the last value | Multi-select, with nothing typed: removes the last value |
| Letters | Drop-down list: selects the next item starting with what was typed. Combo box and multi-select: open the list, and filter it or move to the first match | Drop-down list: moves to the next item starting with what was typed. Combo box and multi-select: filter, or move to the first match |

The components use the ARIA combobox pattern: the focusable element has `role="combobox"`, `aria-expanded` and `aria-controls`, the list has `role="listbox"` (with `aria-multiselectable` for the multi-select), each item has `role="option"` and `aria-selected`, and the highlighted item is announced through `aria-activedescendant`, so the focus stays in the component while the user moves through the list. Use `AriaLabel`, `AriaLabelledBy` and `AriaDescribedBy` to label the component.

## Parameters

These parameters are shared by all three components...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Data` | `IEnumerable<TItem>?` | | The items to choose from. Set this, or handle `OnRead` |
| `Value` | `TValue` (`List<TValue>` for the multi-select) | | The value. Bind it with `@bind-Value` |
| `TextField` | `string?` | `"Text"` | The field of each item holding the text shown. Ignored for primitive data |
| `ValueField` | `string?` | `"Value"` | The field of each item holding its value. Ignored for primitive data. `""` for the item itself |
| `GroupField` | `string?` | | The field to group the items by |
| `Filterable` | `bool` | `false` | Whether the user can type to filter the list |
| `FilterOperator` | `PixataStringFilterOperator` | `StartsWith` | How typed text is matched against the items |
| `ScrollMode` | `PixataDropDownScrollMode` | `Scrollable` | `Virtual` renders only the items that can be seen |
| `ItemHeight` | `decimal` | `28` | The height of each item in pixels when virtually scrolling |
| `PageSize` | `int` | `20` | How many items `OnRead` is asked for at a time when virtually scrolling |
| `ValueMapper` | `Func<TValue, Task<TItem>>?` (`Func<List<TValue>, Task<List<TItem>>>?` for the multi-select) | | Finds the item(s) for a value. Only needed when virtually scrolling with `OnRead` |
| `Enabled` | `bool` | `true` | When false, the component can't be focused or changed |
| `ReadOnly` | `bool` | `false` | When true, the value can be seen (and the component focused) but not changed |
| `Width` | `string?` | | A CSS width, eg `"300px"`. By default the component fills its container |
| `Size` | `string?` | `md` | `sm`, `md` or `lg` (see `PixataThemeConstants.Size`) |
| `Rounded` | `string?` | `md` | `none`, `sm`, `md`, `lg` or `full` |
| `FillMode` | `string?` | `solid` | `solid`, `flat` or `outline` |
| `PopupSettings` | `PixataPopupSettings?` | | The popup's `Width`, `MinWidth`, `MaxWidth`, `Height`, `MinHeight`, `MaxHeight` (the list's maximum height defaults to 200px) and extra `Class` |
| `Id` | `string?` | | The `id` of the focusable element |
| `Class` | `string?` | | Extra CSS classes for the component's outer element |
| `TabIndex` | `int?` | | The tab index of the focusable element |
| `AriaLabel`, `AriaLabelledBy`, `AriaDescribedBy` | `string?` | | Accessible labels for the focusable element |

Any other attributes (eg `title` or `data-*`) are passed to the component's outer element.

Only on the `PixataDropDownList`...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `DefaultText` | `string?` | | The text shown when nothing is selected, which is also the first option in the list (choosing it clears the value) |
| `ShowClearButton` | `bool` | `false` | Whether to show a button that clears the value. Not shown in an icon-only list |
| `Icon` | `object?` | | Shows the list as just this icon (see [icon-only](#icon-only)): a `PixataSvgIcon` (see `PixataSvgIcons`), or the CSS class(es) of an icon font |
| `IconTemplate` | `RenderFragment?` | | Your own content for an icon-only list, in place of `Icon` |
| `FilterPlaceholder` | `string?` | | The placeholder of the filter box |
| `FilterDebounceDelay` | `int` | `150` | Milliseconds to wait after the user stops typing in the filter box before raising `OnRead` |

Only on the `PixataComboBox`...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Placeholder` | `string?` | | The hint shown when the text box is empty |
| `AllowCustom` | `bool` | `false` | Whether the user can type a value that isn't in the list |
| `ShowClearButton` | `bool` | `true` | Whether to show a button that clears the value |
| `DebounceDelay` | `int` | `150` | Milliseconds to wait after the user stops typing before raising `OnRead` |

Only on the `PixataMultiSelect`: `Placeholder`, `AllowCustom`, `ShowClearButton` (default `true`) and `DebounceDelay` as for the combo box, plus `AutoClose`, `TagMode`, `MaxAllowedTags`, `EnableCheckBoxes`, `EnableSelectAll`, `ShowArrowButton` (see [multi-select options](#multi-select-options)), `MinLength` and `PersistFilterOnSelect` (see [filtering](#filtering)).

## Events

| Event | Argument | Description |
| --- | --- | --- |
| `ValueChanged` | `TValue` (`List<TValue>` for the multi-select) | Raised when the value changes. Used by `@bind-Value` |
| `OnChange` | `object` (the new value) | Raised when the user changes the value: by choosing an item, by typing a value and pressing Enter or leaving the component (combo box), by removing a tag, or by clearing it. Unlike `ValueChanged`, it isn't raised when your code sets `Value`, nor when the user chooses the item that is already selected |
| `OnOpen` | `PixataDropDownOpenEventArgs` | Raised before the list opens. Set `IsCancelled` to keep it closed |
| `OnClose` | `PixataDropDownCloseEventArgs` | Raised before the list closes. Set `IsCancelled` to keep it open (it still closes when the focus leaves the component) |
| `OnBlur` | none | Raised when the focus leaves the component (moving between the component and its list doesn't count) |
| `OnRead` | `PixataDropDownReadEventArgs` | Raised when the component needs data. See [supplying the data yourself](#supplying-the-data-yourself-onread) |

The multi-select gives `ValueChanged` and `OnChange` a new list each time, rather than changing the list you passed in.

## Methods

Get a reference to the component with `@ref` to call these...

| Method | Description |
| --- | --- |
| `FocusAsync()` | Puts the focus on the component |
| `Open()` | Opens the list |
| `Close()` | Closes the list |
| `Rebind()` | Reads the data again (raising `OnRead` if you handle it). Call it after changing the items in `Data` without passing a new list |
| `Refresh()` | Re-renders the component |

```razor
<PixataDropDownList @ref="_fruitList" Data="@_fruit" @bind-Value="@_chosenFruit" />
<button @onclick="@(() => _fruitList.Open())">Choose a fruit</button>

@code {
  private PixataDropDownList<string, string> _fruitList = null!;
}
```

## Styling

The components use the shared input, popup and list classes from [pixata.css](Readme.Styling.md) (`pixata-blazor-input`, `pixata-blazor-popup`, `pixata-blazor-list-item` and so on), so they follow any changes you make to those. On top of that, they have these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-dropdown` | The outer element of all three components (which also has `pixata-blazor-dropdownlist`, `pixata-blazor-combobox` or `pixata-blazor-multiselect`) |
| `pixata-blazor-dropdown-readonly` | Added to the outer element when `ReadOnly` |
| `pixata-blazor-dropdownlist-value`, `pixata-blazor-input-value-text` | The drop-down list's box, and the selected item's text in it |
| `pixata-blazor-dropdownlist-default-text` | The text in the drop-down list's box when it shows `DefaultText` |
| `pixata-blazor-dropdownlist-icon-only` | Added to the drop-down list's outer element when it has an `Icon` or `IconTemplate` |
| `pixata-blazor-dropdownlist-icon` | The box around the icon in an icon-only drop-down list |
| `pixata-blazor-combobox-input`, `pixata-blazor-multiselect-input` | The text boxes |
| `pixata-blazor-dropdownlist-button`, `pixata-blazor-combobox-button`, `pixata-blazor-multiselect-button` | The arrow buttons |
| `pixata-blazor-clear-value` | The clear button |
| `pixata-blazor-dropdown-loading` | The loading indicator shown while `OnRead` runs |
| `pixata-blazor-multiselect-tags`, `pixata-blazor-multiselect-tag`, `pixata-blazor-multiselect-tag-text`, `pixata-blazor-multiselect-tag-remove` | The tags |
| `pixata-blazor-multiselect-summary-tag` | The summary tag |
| `pixata-blazor-dropdown-popup` | The popup (which also has `pixata-blazor-dropdownlist-popup`, `pixata-blazor-combobox-popup` or `pixata-blazor-multiselect-popup`, and `pixata-blazor-dropdown-sm`/`-lg` for the small and large sizes) |
| `pixata-blazor-dropdown-filter`, `pixata-blazor-dropdown-filter-box` | The drop-down list's filter box |
| `pixata-blazor-dropdown-header`, `pixata-blazor-dropdown-footer` | Around the header and footer templates |
| `pixata-blazor-dropdown-content`, `pixata-blazor-dropdown-list` | The scrolling part of the popup, and the list in it |
| `pixata-blazor-dropdown-item`, `pixata-blazor-dropdown-item-text` | Each item, and its text (when there is no `ItemTemplate`). The item also has `pixata-blazor-selected` when selected and `pixata-blazor-focus` when highlighted |
| `pixata-blazor-dropdown-group-header` | A group header |
| `pixata-blazor-dropdown-option-label` | The drop-down list's `DefaultText` option |
| `pixata-blazor-dropdown-custom-value` | The multi-select's option for adding the typed text |
| `pixata-blazor-dropdown-select-all` | The multi-select's "Select all" option |
| `pixata-blazor-dropdown-checkbox` | A checkbox in the list, with `pixata-blazor-checked` or `pixata-blazor-indeterminate` |
| `pixata-blazor-dropdown-placeholder` | An item that is still being read when virtually scrolling with `OnRead` |
| `pixata-blazor-dropdown-no-data` | The no-data message |

These CSS variables (set on `:root`) control their look...

| Variable | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-dropdown-popup-padding` | `calc(var(--pixata-blazor-spacing) * 2)` | The padding inside the popup |
| `--pixata-blazor-dropdown-item-radius` | `var(--pixata-blazor-border-radius)` | The corner radius of the items |
| `--pixata-blazor-dropdown-item-hover-bg` | 8% of the text colour | The background of an item under the pointer |
| `--pixata-blazor-dropdown-item-selected-bg` | 12% of the text colour | The background of a selected item |
| `--pixata-blazor-dropdown-item-selected-hover-bg` | 16% of the text colour | The background of a selected item under the pointer |
| `--pixata-blazor-dropdown-item-selected-color` | `var(--pixata-blazor-text-color)` | The text colour of a selected item |
| `--pixata-blazor-dropdown-item-focus-ring` | an inset 2px border | The `box-shadow` that marks the highlighted item |
| `--pixata-blazor-dropdown-group-color` | `var(--pixata-blazor-subtle-color)` | The colour of the group headers |
| `--pixata-blazor-dropdown-checkbox-size` | `1rem` | The size of the checkboxes |
| `--pixata-blazor-dropdown-checkbox-border-color` | `var(--pixata-blazor-border-alt-color)` | The border of an unticked checkbox |
| `--pixata-blazor-dropdown-checkbox-checked-bg` | `var(--pixata-blazor-primary)` | The background of a ticked checkbox |
| `--pixata-blazor-dropdown-checkbox-checked-color` | `var(--pixata-blazor-primary-color)` | The tick colour |
| `--pixata-blazor-dropdownlist-bg` | `var(--pixata-blazor-base-bg)` | The background of the drop-down list's box |
| `--pixata-blazor-dropdownlist-hover-bg` | `var(--pixata-blazor-base-hover-bg)` | The background of the drop-down list's box under the pointer |
| `--pixata-blazor-dropdownlist-border-color` | `var(--pixata-blazor-border-color)` | The border of the drop-down list's box |
| `--pixata-blazor-multiselect-tag-bg` | `#ebebeb` | The background of a tag |
| `--pixata-blazor-multiselect-tag-hover-bg` | `#e0e0e0` | The background of a tag under the pointer |
| `--pixata-blazor-multiselect-tag-border-color` | `#c2c2c2` | The border of a tag |
| `--pixata-blazor-multiselect-tag-color` | `var(--pixata-blazor-text-color)` | The text colour of a tag |
| `--pixata-blazor-multiselect-tag-gap` | `.125rem` | The space between the tags |

For example, to make the selected items stand out in your primary colour...

```css
:root {
  --pixata-blazor-dropdown-item-selected-bg: var(--pixata-blazor-primary);
  --pixata-blazor-dropdown-item-selected-hover-bg: var(--pixata-blazor-primary-hover);
  --pixata-blazor-dropdown-item-selected-color: var(--pixata-blazor-primary-color);
}
```
