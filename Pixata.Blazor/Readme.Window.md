# Window

>Part of [Pixata.Blazor](Readme.md)

`PixataWindow` is a floating window with a titlebar, some content and an optional footer. The user can move it by dragging its titlebar, resize it by dragging its edges and corners (with a mouse, a finger or a pen), and minimise, maximise, restore and close it with buttons in the titlebar or from the keyboard. Windows are non-modal by default, so the user can carry on using the page, and can have several open at once.

There is a [live demo](https://test.pixata.co.uk/WindowSample) on the sample site ([source](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/WindowSample.razor)).

For a simple modal question with buttons, a [`PixataDialog`](Readme.Dialogs.md) is usually a better fit. The two share the overlay, the stacking of several open at once, and the focus handling, all described in [Focus, Esc and stacking](Readme.Dialogs.md#focus-esc-and-stacking).

## A simple example

```xml
<button type="button" @onclick="() => _visible = !_visible">Customer details</button>

<PixataWindow @bind-Visible="_visible"
              @bind-Top="_top"
              @bind-Left="_left"
              Width="360px">
  <WindowTitle>Customer details</WindowTitle>
  <WindowActions>
    <PixataWindowAction Name="Minimize" />
    <PixataWindowAction Name="Maximize" />
    <PixataWindowAction Name="Close" />
  </WindowActions>
  <WindowContent>
    <p>Fred Bloggs, 12 High Street, Anytown</p>
  </WindowContent>
  <WindowFooter>
    <button type="button" class="@PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Primary, null, null, null)" @onclick="Save">Save</button>
  </WindowFooter>
</PixataWindow>

@code {
  private bool _visible;
  private string _top = "120px";
  private string _left = "300px";
}
```

As with a dialog, the window is rendered where you declare it (so it can use your component's fields and anything cascaded to it), but while it is visible its markup is moved to the end of the page's `<body>`, so it isn't clipped or covered by the element it was declared in.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Visible` | `bool` | `false` | Whether the window is shown. Use `@bind-Visible`, so that it is updated when the user closes the window |
| `State` | `PixataWindowState` | `Default` | `Default`, `Minimized` (just the titlebar) or `Maximized` (filling the viewport, or the containment element). Use `@bind-State` to keep track of the user's changes |
| `Modal` | `bool` | `false` | Shows the window over a dimmed overlay that blocks the page, and keeps the focus inside it |
| `CloseOnOverlayClick` | `bool` | `false` | Whether clicking (or tapping) the overlay around a modal window closes it |
| `CloseOnEscape` | `bool` | `true` | Whether pressing Esc while the window (or something in it) has the focus closes it |
| `Draggable` | `bool` | `true` | Whether the user can move the window |
| `Resizable` | `bool` | `true` | Whether the user can resize the window |
| `Top`, `Left` | `string?` | | The window's position as CSS lengths, measured from the top left of the viewport (or the containment element). Use `@bind-Top` and `@bind-Left` to keep track of where the user moves it. If neither is set, the window opens in the middle |
| `Width`, `Height` | `string?` | | The window's size as CSS lengths. By default it fits its content. Use `@bind-Width` and `@bind-Height` to keep track of the user's resizing |
| `MinWidth`, `MinHeight` | `string?` | `120px`, `100px` when resizable | The smallest size the user can resize the window to |
| `MaxWidth`, `MaxHeight` | `string?` | | The largest size the user can resize the window to. It is never bigger than the viewport (or the containment element) anyway |
| `Size` | `string?` | | A preset width: `PixataThemeConstants.Window.Size.Small` (300px), `Medium` (800px) or `Large` (1200px). `Width` takes precedence |
| `Centered` | `bool` | `false` | Opens the window in the middle, even if `Top` and `Left` are set |
| `ContainmentSelector` | `string?` | | A CSS selector for an element to keep the window inside, eg `#workspace`. See [Keeping a window inside an element](#keeping-a-window-inside-an-element). Ignored for modal windows |
| `PersistContent` | `bool` | `false` | Keeps the content (and the state of any components in it) while the window is minimised, rather than removing it |
| `FooterLayoutAlign` | `PixataWindowFooterLayoutAlign` | `Stretch` | How the `WindowFooter` content is laid out: `Start`, `Center`, `End` or `Stretch` (buttons share the width equally) |
| `ThemeColor` | `string?` | | Colours the titlebar with one of the `PixataThemeConstants.ThemeColor` values, eg `primary` |
| `Id` | `string?` | generated | The `id` of the window element |
| `Class` | `string?` | | Extra CSS classes for the window element |

Any other attributes you add (`data-*`, `aria-*` and so on) are rendered on the window element.

Once the user has moved or resized a window, it stays where they put it, even if it was opened in the middle, until it is closed. The positions and sizes the window reports are in pixels, eg `"240px"`.

If the viewport (or the containment element) gets smaller, eg when a phone is turned round, a window that would end up partly out of sight is moved back into view, and the new `Top` and `Left` are reported.

## Templates

| Template | Description |
| --- | --- |
| `WindowTitle` | The text or markup in the titlebar |
| `WindowContent` | The body of the window. It scrolls if it doesn't fit |
| `WindowFooter` | An optional row below the content, eg for buttons |
| `WindowActions` | The buttons in the titlebar: `PixataWindowAction`s, see below. There are none unless you add them |

## Titlebar buttons

Add a `PixataWindowAction` to `WindowActions` for each button you want, in the order you want them...

| Parameter | Type | Description |
| --- | --- | --- |
| `Name` | `string` | `Minimize`, `Maximize` or `Close` for a built-in button (`Minimise` and `Maximise` work too, in any case), or any other name for your own |
| `Title` | `string?` | The tooltip, which screen readers announce as the button's name. The built-in buttons have one already, from the app's [`PixataDialogTexts`](Readme.Dialogs.md#button-text-and-translations); a custom one uses its `Name` if you don't give one |
| `Icon` | `object?` | A `PixataSvgIcon` (see `PixataSvgIcons`), or the CSS class(es) of an icon font. The built-in buttons have one already |
| `OnClick` | `EventCallback<MouseEventArgs>` | What a custom button does. Set on a built-in button, it replaces what the button normally does |
| `Hidden` | `bool` | Hides the button |

The minimise button of a minimised window, and the maximise button of a maximised one, become restore buttons. As with a desktop window, a maximised window has no minimise button and a minimised one has no maximise button: restore it first.

```xml
<WindowActions>
  <PixataWindowAction Name="Refresh" Title="Refresh" Icon="PixataSvgIcons.Refresh" OnClick="Refresh" />
  <PixataWindowAction Name="Minimize" />
  <PixataWindowAction Name="Maximize" />
  <PixataWindowAction Name="Close" />
</WindowActions>
```

## Events

| Event | Type | Description |
| --- | --- | --- |
| `VisibleChanged` | `EventCallback<bool>` | Raised with `false` when the user closes the window with a close button, Esc or the overlay |
| `StateChanged` | `EventCallback<PixataWindowState>` | Raised when the window is minimised, maximised or restored |
| `TopChanged`, `LeftChanged` | `EventCallback<string>` | Raised when the user moves the window |
| `WidthChanged`, `HeightChanged` | `EventCallback<string>` | Raised when the user resizes the window |

As with a dialog, if you handle `VisibleChanged` yourself rather than using `@bind-Visible`, the window stays open until you set `Visible` to `false`, so you can stop it closing. If you don't handle it at all, the window hides itself.

## Methods

Get a reference to the window with `@ref` to call these...

| Method | Description |
| --- | --- |
| `Minimize()` | Minimises the window to its titlebar |
| `Maximize()` | Maximises the window to fill the viewport (or the containment element) |
| `Restore()` | Returns a minimised or maximised window to its own size and position |
| `Refresh()` | Re-renders the window, eg after changing something its content shows that it doesn't know about |

Each of these raises `StateChanged`. Setting `State` does the same job.

## The mouse, touch and the keyboard

- Drag the titlebar to move the window. Buttons and other controls in the titlebar still work as normal.
- Drag an edge or a corner to resize it. The handles are bigger on touch screens.
- A maximised window can't be moved or resized until it is restored.
- Clicking, tapping or tabbing into a window brings it to the front.

When the window itself has the focus (click its titlebar, or Tab to it)...

| Keys | Action |
| --- | --- |
| Arrow keys | Move the window by 10 pixels |
| Ctrl + arrow keys | Make the window wider, narrower, taller or shorter by 10 pixels |
| Alt + Up | Maximise the window, or restore a minimised one |
| Alt + Down | Minimise the window, or restore a maximised one |
| Esc | Close the window (from anywhere inside it) |

When a window opens, it takes the focus, and when it closes, the focus goes back to whatever had it before. A modal window keeps the focus inside it, as a dialog does.

## Keeping a window inside an element

Set `ContainmentSelector` to keep a window inside an element on the page, such as a workspace panel...

```xml
<div id="workspace" style="position: relative; height: 400px;">
  <PixataWindow @bind-Visible="_visible" ContainmentSelector="#workspace" Top="20px" Left="20px" Width="240px">
    ...
  </PixataWindow>
</div>
```

`Top` and `Left` are then measured from the inside of the element's border, the window can't be dragged out of it, maximising the window makes it fill the element, and the window follows the element when the page scrolls or the element changes size.

## Styling

The window uses the shared `--pixata-blazor-*` colours and spacing (see [Styling](Readme.Styling.md)), so it follows your theme. These variables are specific to it (most of them are shared with [`PixataDialog`](Readme.Dialogs.md#styling), so the two look the same)...

| Variable | Default | Description |
| --- | --- | --- |
| `--pixata-blazor-window-bg` | surface background | The window's background |
| `--pixata-blazor-window-color` | text colour | The window's text colour |
| `--pixata-blazor-window-border-color` | border colour | The window's border, and the lines around the titlebar and footer |
| `--pixata-blazor-window-border-radius` | `0` | The corners |
| `--pixata-blazor-window-line-height` | `1.25` | The line height inside the window |
| `--pixata-blazor-window-titlebar-bg` | alternative surface background | The titlebar's background |
| `--pixata-blazor-window-titlebar-color` | text colour | The titlebar's text colour |
| `--pixata-blazor-window-titlebar-padding` | `.75rem 1rem` | The titlebar's padding |
| `--pixata-blazor-window-title-font-size` | large font size | The title's font size |
| `--pixata-blazor-window-content-padding` | `1rem` | The content's padding |
| `--pixata-blazor-window-footer-padding` | `.5rem` | The footer's padding |
| `--pixata-blazor-window-footer-gap` | `.5rem` | The gap between the items in the footer |
| `--pixata-blazor-window-active-shadow` | a deeper shadow | The shadow of the window in front |
| `--pixata-blazor-window-resize-handle-size` | `6px` | The thickness of the resize handles along the inside of the window's edges (the corner ones are twice that). `12px` on touch screens |
| `--pixata-blazor-overlay-bg` | `rgba(0, 0, 0, .5)` | The overlay behind a modal window (shared with other components) |
| `--pixata-blazor-overlay-z-index` | `10000` | Where the stack of dialogs and windows starts (shared) |

The main CSS classes are...

| Class | Element |
| --- | --- |
| `pixata-blazor-window-wrapper` | The wrapper moved to the end of the body, plus `pixata-blazor-window-wrapper-modal` for a modal window. It has `data-pixata-blazor-active` when its window is in front |
| `pixata-blazor-overlay` | The overlay behind a modal window |
| `pixata-blazor-window` | The window itself |
| `pixata-blazor-window-sm`, `-md`, `-lg` | The `Size` presets |
| `pixata-blazor-window-centred` | A window shown in the middle, which hasn't been moved |
| `pixata-blazor-window-minimised`, `pixata-blazor-window-maximised` | A minimised or maximised window |
| `pixata-blazor-window-<ThemeColor>` | A window with a `ThemeColor` |
| `pixata-blazor-window-titlebar`, `pixata-blazor-window-title` | The titlebar and the title in it |
| `pixata-blazor-window-titlebar-actions`, `pixata-blazor-window-titlebar-action` | The titlebar buttons and their container. The built-in ones also have `pixata-blazor-window-action-minimise`, `-maximise` or `-close` |
| `pixata-blazor-window-content` | The content |
| `pixata-blazor-window-footer` | The footer, plus `pixata-blazor-window-footer-start`, `-center`, `-end` or `-stretch` |
| `pixata-blazor-window-resize-handle` | The resize handles, plus `pixata-blazor-window-resize-n`, `-e`, `-s`, `-w`, `-ne`, `-nw`, `-se` or `-sw` |
| `pixata-blazor-layer-anchor` | The (empty, `display: contents`) placeholder left where the window was declared |
