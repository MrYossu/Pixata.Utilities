# Drop-down and split buttons

>Part of [Pixata.Blazor](Readme.md)

Two buttons that open a menu of actions...

- `PixataDropDownButton` is a button that opens a menu. Use it when the button itself doesn't do anything, but groups related actions (Export as PDF, Excel or Word, say)
- `PixataSplitButton` is a button for a primary action, with an arrow next to it that opens a menu of secondary ones (Save, with Save as and Save a copy in the menu)

They share their styling (the same theme colours, fill modes, roundness and sizes as the other buttons in this package), their menu, and nearly all their parameters, events and methods. They work with the mouse, keyboard and touch, and the menu stays inside the viewport on small screens.

See the [live demo](https://test.pixata.co.uk/ButtonsSample) for how they look, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/ButtonsSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`.

## Drop-down button

The button's content goes in `DropDownButtonContent`, and the menu's items in `DropDownButtonItems`, as `PixataDropDownButtonItem` components. Each item's content is its text, and it has its own `Icon` and `OnClick`...

```xml
<PixataDropDownButton Icon="PixataSvgIcons.Download">
  <DropDownButtonContent>Export</DropDownButtonContent>
  <DropDownButtonItems>
    <PixataDropDownButtonItem Icon="PixataSvgIcons.File" OnClick="ExportPdf">As PDF</PixataDropDownButtonItem>
    <PixataDropDownButtonItem Icon="PixataSvgIcons.File" OnClick="ExportExcel">As Excel</PixataDropDownButtonItem>
    <PixataDropDownButtonItem Enabled="false">As Word (coming soon)</PixataDropDownButtonItem>
  </DropDownButtonItems>
</PixataDropDownButton>
```

`ShowArrowButton="true"` adds a down arrow after the content, to show that the button opens a menu. A button with an `Icon` and no content is square; give it an `AriaLabel` so screen readers can announce it...

```xml
<PixataDropDownButton Icon="PixataSvgIcons.MoreVertical" AriaLabel="More actions" FillMode="flat">
  <DropDownButtonItems>
    <PixataDropDownButtonItem OnClick="OpenSettings">Settings</PixataDropDownButtonItem>
    <PixataDropDownButtonItem OnClick="OpenHelp">Help</PixataDropDownButtonItem>
  </DropDownButtonItems>
</PixataDropDownButton>
```

## Split button

The main part's content goes in `SplitButtonContent`, and clicking it raises `OnClick`. The arrow opens the menu of `SplitButtonItems`, which are `PixataSplitButtonItem` components...

```xml
<PixataSplitButton ThemeColor="primary" OnClick="Save">
  <SplitButtonContent>Save</SplitButtonContent>
  <SplitButtonItems>
    <PixataSplitButtonItem OnClick="SaveAs">Save as...</PixataSplitButtonItem>
    <PixataSplitButtonItem OnClick="SaveACopy">Save a copy</PixataSplitButtonItem>
  </SplitButtonItems>
</PixataSplitButton>
```

The arrow is a separate button, with `aria-haspopup="menu"` and an `aria-label` of "More options" (change it with `ArrowButtonAriaLabel`). On touch screens it is widened to `--pixata-blazor-splitbutton-arrow-touch-width` (2.75rem), so it is easy to tap.

## Using them

| Input | What it does |
| --- | --- |
| Click or tap the button (the arrow on a split button) | Opens the menu, or closes it if it is open. The first item is focused |
| Click or tap an item | Closes the menu and raises the item's `OnClick` |
| Click or tap anywhere else | Closes the menu |
| Enter or Space on the button | As a click |
| Alt+Down on the button | Opens the menu and focuses the first item |
| Down and Up | Move to the next or previous item, skipping disabled ones and wrapping round at the ends |
| Home and End | Move to the first or last item |
| Enter or Space on an item | Closes the menu and raises the item's `OnClick` |
| Esc or Alt+Up | Closes the menu, and puts the focus back on the button. Esc doesn't go any further, so it won't also close a dialog the button is in |
| Tab | Closes the menu and moves on as usual |

The button has `aria-haspopup="menu"`, `aria-expanded` and `aria-controls`, the menu is a `<ul role="menu">` labelled by the button, and the items are `role="menuitem"`, with `aria-disabled="true"` when they are disabled.

The menu is rendered inside the component, but floats above the page, so it isn't cut off by a container with `overflow` set. It opens upwards when there isn't room below, and stays inside the viewport on small screens.

## Parameters

These parameters are on both buttons...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Icon` | `object` | | An icon shown in the (main) button, before its content. A `PixataSvgIcon` (eg one from `PixataSvgIcons`), or the CSS class(es) of an icon font, such as `"fas fa-save"` |
| `ThemeColor` | `string` | `"base"` | `"base"`, `"primary"`, `"secondary"`, `"tertiary"`, `"info"`, `"success"`, `"warning"`, `"error"`, `"dark"`, `"light"` or `"inverse"`. The values are in `PixataThemeConstants.Button.ThemeColor` |
| `FillMode` | `string` | `"solid"` | `"solid"`, `"outline"`, `"flat"`, `"link"` or `"clear"`. The values are in `PixataThemeConstants.Button.FillMode` |
| `Rounded` | `string` | `"md"` | `"none"`, `"sm"`, `"md"`, `"lg"` or `"full"`. The values are in `PixataThemeConstants.Button.Rounded` |
| `Size` | `string` | `"md"` | `"sm"`, `"md"` or `"lg"`. The menu's items are sized to match. The values are in `PixataThemeConstants.Button.Size` |
| `Enabled` | `bool` | `true` | When false, the button is greyed out and can't be used |
| `PopupSettings` | `PixataPopupSettings` | | The size of the menu: `Width`, `MinWidth`, `MaxWidth`, `Height`, `MinHeight` and `MaxHeight` (CSS lengths), and extra CSS classes in `Class`. See [Popup settings](#popup-settings) |
| `Id` | `string` | | The `id` of the (main) button. A unique one is generated if you don't set it |
| `Title` | `string` | | The tooltip of the (main) button |
| `TabIndex` | `int?` | | The `tabindex` of the button(s) |
| `Class` | `string` | | Extra CSS classes for the component's outer element |
| `AriaLabel` | `string` | | The `aria-label` of the (main) button. Set it when the button only has an icon |
| `AriaLabelledBy` | `string` | | The `aria-labelledby` of the (main) button |
| `AriaDescribedBy` | `string` | | The `aria-describedby` of the (main) button |

Any other attributes you add (`data-*` and so on) are rendered on the component's outer element.

`PixataDropDownButton` also has...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `DropDownButtonContent` | `RenderFragment` | | The content of the button, usually its text |
| `DropDownButtonItems` | `RenderFragment` | | The items in the menu: `PixataDropDownButtonItem` components |
| `DropDownButtonSettings` | `RenderFragment` | | Holds a `PixataDropDownButtonPopupSettings`. See [Popup settings](#popup-settings) |
| `ShowArrowButton` | `bool` | `false` | Whether to show a down arrow after the content |

`PixataSplitButton` also has...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `SplitButtonContent` | `RenderFragment` | | The content of the main button, usually its text |
| `SplitButtonItems` | `RenderFragment` | | The items in the menu: `PixataSplitButtonItem` components |
| `SplitButtonSettings` | `RenderFragment` | | Holds a `PixataSplitButtonPopupSettings`. See [Popup settings](#popup-settings) |
| `ArrowButtonAriaLabel` | `string` | `"More options"` | The `aria-label` of the arrow button |

`DropDownButtonContent`, `DropDownButtonItems` and so on are plain `RenderFragment` parameters, so you write them as tags inside the button, as in the examples above.

### Items

`PixataDropDownButtonItem` and `PixataSplitButtonItem` have the same parameters...

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment` | | The item's content, usually its text. Write it between the tags |
| `Icon` | `object` | | An icon shown before the content, as for the button |
| `OnClick` | `EventCallback<MouseEventArgs>` | | Raised when the item is chosen, with the mouse, touch or keyboard. The menu has already closed |
| `Enabled` | `bool` | `true` | When false, the item is greyed out, can't be chosen, and is skipped by the arrow keys |
| `Class` | `string` | | Extra CSS classes for the item |

Any other attributes you add (`title`, `data-*` and so on) are rendered on the item. An item used anywhere other than inside the items of one of these buttons throws an `InvalidOperationException`.

You can build the items in a loop, or show some of them conditionally...

```xml
<PixataDropDownButton>
  <DropDownButtonContent>Move to</DropDownButtonContent>
  <DropDownButtonItems>
    @foreach (Folder folder in _folders) {
      <PixataDropDownButtonItem Enabled="@(folder != _current)" OnClick="@(() => MoveTo(folder))">@folder.Name</PixataDropDownButtonItem>
    }
  </DropDownButtonItems>
</PixataDropDownButton>
```

## Popup settings

By default the menu is at least as wide as the button (wider if its items need it), and grows to `--pixata-blazor-button-menu-max-height` (80% of the viewport) before it scrolls. You can set its size either with the `PopupSettings` parameter...

```xml
<PixataDropDownButton PopupSettings="@(new() { Width = "220px", MaxHeight = "300px" })">
  ...
</PixataDropDownButton>
```

...or with a popup settings component in the settings tag, which takes the same values as parameters...

```xml
<PixataDropDownButton>
  <DropDownButtonContent>Folders</DropDownButtonContent>
  <DropDownButtonSettings>
    <PixataDropDownButtonPopupSettings Width="220px" MaxHeight="300px" />
  </DropDownButtonSettings>
  <DropDownButtonItems>
    ...
  </DropDownButtonItems>
</PixataDropDownButton>
```

The split button's equivalents are `SplitButtonSettings` and `PixataSplitButtonPopupSettings`. If you use both, the `PopupSettings` parameter wins.

## Events

| Event | Argument | Description |
| --- | --- | --- |
| `OnClick` | `MouseEventArgs` | Raised when the (main) button is clicked. On a drop-down button, the menu opens as well |
| `OnOpen` | `PixataMenuButtonOpenEventArgs` | Raised before the menu opens. Set `IsCancelled` to true to keep it closed |
| `OnClose` | `PixataMenuButtonCloseEventArgs` | Raised before the menu closes. Set `IsCancelled` to true to keep it open |

```xml
<PixataDropDownButton OnOpen="CheckPermissions">
  ...
</PixataDropDownButton>

@code {
  private void CheckPermissions(PixataMenuButtonOpenEventArgs args) =>
    args.IsCancelled = !_canEdit;
}
```

The items' own `OnClick` events are described [above](#items).

## Methods

Get a reference to the button with `@ref`...

| Method | Description |
| --- | --- |
| `OpenAsync()` | Opens the menu. The focus stays where it is |
| `CloseAsync()` | Closes the menu |
| `FocusAsync()` | Moves the focus to the (main) button |

```xml
<PixataSplitButton @ref="_saveButton" OnClick="Save">
  ...
</PixataSplitButton>

@code {
  private PixataSplitButton _saveButton = null!;

  private Task ShowSaveOptions() =>
    _saveButton.OpenAsync();
}
```

## Styling

The buttons use the same `pixata-blazor-button` classes as the other buttons in this package, so they pick up any changes you make to those. On top of those...

| Class | Element |
| --- | --- |
| `pixata-blazor-dropdownbutton` | The outer element of a drop-down button, with `pixata-blazor-dropdownbutton-open` while the menu is showing |
| `pixata-blazor-dropdownbutton-button` | The drop-down button's `<button>` |
| `pixata-blazor-dropdownbutton-text` | The button's content |
| `pixata-blazor-dropdownbutton-arrow` | The down arrow shown when `ShowArrowButton` is true |
| `pixata-blazor-splitbutton` | The outer element of a split button, with `pixata-blazor-splitbutton-open` while the menu is showing |
| `pixata-blazor-splitbutton-main` | The split button's main `<button>` |
| `pixata-blazor-splitbutton-text` | The main button's content |
| `pixata-blazor-splitbutton-arrow` | The split button's arrow `<button>` |
| `pixata-blazor-button-menu-popup` | The floating panel that holds the menu (it also has the shared `pixata-blazor-popup` class) |
| `pixata-blazor-button-menu` | The menu (`<ul>`), with `pixata-blazor-button-menu-sm`, `-md` or `-lg` to match the button's size |
| `pixata-blazor-button-menu-item` | Each item (`<li>`), with `pixata-blazor-disabled` when it is disabled |
| `pixata-blazor-button-menu-item-icon` | An item's icon |
| `pixata-blazor-button-menu-item-text` | An item's content |

The menu's look comes from these custom properties, which you can override on `:root` or on any container...

| Variable | Default | What it sets |
| --- | --- | --- |
| `--pixata-blazor-button-menu-max-height` | `80vh` | How tall the menu can grow before it scrolls, unless `PopupSettings` says otherwise |
| `--pixata-blazor-button-menu-bg` | `--pixata-blazor-popup-bg` | The menu's background |
| `--pixata-blazor-button-menu-color` | `--pixata-blazor-text-color` | The menu's text colour |
| `--pixata-blazor-button-menu-item-padding-sm`, `-md`, `-lg` | 2px 8px, 4px 8px, 6px 8px (in steps of `--pixata-blazor-spacing`) | The padding of the items for each size |
| `--pixata-blazor-button-menu-item-end-padding` | 4 x `--pixata-blazor-spacing` + 1rem | The extra space after each item's content |
| `--pixata-blazor-button-menu-item-hover-bg` | 8% of the text colour | An item's background when the mouse is over it |
| `--pixata-blazor-button-menu-item-active-bg` | 12% of the text colour | An item's background while it is being clicked |
| `--pixata-blazor-button-menu-item-focus-ring` | An inset 2px grey ring | The focus ring on the focused item, when using the keyboard |
| `--pixata-blazor-splitbutton-arrow-touch-width` | `2.75rem` | The width of a split button's arrow on touch screens |

The button colours, the popup's border, shadow and corners, and the touch-screen sizes come from the shared `--pixata-blazor-*` variables at the start of the components section of `pixata.css`, such as `--pixata-blazor-primary`, `--pixata-blazor-popup-shadow` and `--pixata-blazor-touch-target`.
