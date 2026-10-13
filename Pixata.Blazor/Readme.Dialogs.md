# Dialogs

>Part of [Pixata.Blazor](Readme.md)

`PixataDialog` is a modal dialog: a titlebar, some content and a row of buttons, shown over a dimmed overlay. For the common cases where you just want to tell the user something, ask them to confirm something or ask them to type something, `PixataDialogFactory` shows ready-made alert, confirm and prompt dialogs from code, and lets you `await` the answer.

There is a [live demo](https://test.pixata.co.uk/DialogsSample) on the sample site ([source](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/DialogsSample.razor)).

See also [`PixataWindow`](Readme.Window.md), a floating window that can be moved, resized, minimised and maximised, which shares the stacking and focus handling described below.

>The older `Confirm` and `Inform` [containers](Readme.Containers.md) are still there, and work as before. The components described here are separate from them.

## PixataDialog

### A simple example

```xml
<button type="button" @onclick="() => _visible = true">Delete customer</button>

<PixataDialog @bind-Visible="_visible" Title="Delete customer" Width="400px">
  <DialogContent>
    Are you sure you want to delete Fred Bloggs? His orders and invoices will be deleted too.
  </DialogContent>
  <DialogButtons>
    <button type="button" class="@PixataCss.Button(null, null, null, null)" @onclick="() => _visible = false">Cancel</button>
    <button type="button" class="@PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Primary, null, null, null)" @onclick="Delete">Delete</button>
  </DialogButtons>
</PixataDialog>

@code {
  private bool _visible;

  private async Task Delete() {
    // ...
    _visible = false;
  }
}
```

The dialog is rendered where you declare it, so it can use your component's fields and methods, and anything cascaded to it (such as an `EditForm`'s context) still reaches its content. While it is visible, its markup is moved to the end of the page's `<body>`, so that it isn't clipped by an `overflow: hidden` container or covered by something with a higher `z-index`.

### Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Visible` | `bool` | `false` | Whether the dialog is shown. Use `@bind-Visible`, so that it is updated when the user closes the dialog |
| `Title` | `string?` | | The title, shown as plain text in the titlebar |
| `ShowCloseButton` | `bool` | `true` | Whether there's a close (X) button in the titlebar |
| `CloseOnEscape` | `bool?` | `null` | Whether Esc closes the dialog. `null` means it does if there's a close button, and doesn't if there isn't |
| `CloseOnOverlayClick` | `bool` | `false` | Whether clicking (or tapping) the overlay around the dialog closes it |
| `Width` | `string?` | | The width as a CSS length, eg `400px` or `50vw`. By default the dialog fits its content |
| `Height` | `string?` | | The height as a CSS length. By default the dialog fits its content |
| `ButtonsLayout` | `PixataDialogButtonsLayout` | `Stretch` | How the `DialogButtons` are laid out: `Start`, `Center`, `End` or `Stretch` (the buttons share the width equally) |
| `ThemeColor` | `string?` | | Colours the titlebar with one of the `PixataThemeConstants.ThemeColor` values, eg `primary` or `error` |
| `FocusedElementSelector` | `string?` | | A CSS selector for the element to focus when the dialog opens, eg `#email`. See [Focus](#focus-esc-and-stacking) |
| `Id` | `string?` | generated | The `id` of the dialog element |
| `Class` | `string?` | | Extra CSS classes for the dialog element |

Any other attributes you add (`data-*`, `aria-*`, or `role="alertdialog"` for a dialog that demands an answer) are rendered on the dialog element.

The dialog is never bigger than the viewport (less a margin of `--pixata-blazor-dialog-viewport-margin`). If the content doesn't fit, the content area scrolls, and the titlebar and buttons stay where they are. On a phone, a dialog takes the full width of the screen.

### Templates

| Template | Description |
| --- | --- |
| `DialogContent` | The body of the dialog |
| `DialogTitle` | The title as markup, eg with an icon. Takes precedence over `Title` |
| `DialogButtons` | The buttons along the bottom. If you don't set it, there's no button row |

```xml
<PixataDialog @bind-Visible="_visible">
  <DialogTitle>
    <PixataIcon Icon="PixataSvgIcons.Pencil" /> Edit contact
  </DialogTitle>
  <DialogContent>...</DialogContent>
</PixataDialog>
```

### Events

| Event | Type | Description |
| --- | --- | --- |
| `VisibleChanged` | `EventCallback<bool>` | Raised with `false` when the user closes the dialog with the close button, Esc or the overlay |

If you handle `VisibleChanged` yourself rather than using `@bind-Visible`, the dialog stays open until you set `Visible` to `false`. That lets you stop it closing, for example while there are unsaved changes...

```xml
<PixataDialog Visible="_visible" VisibleChanged="VisibleChanged" Title="Edit contact">
  ...
</PixataDialog>

@code {
  private async Task VisibleChanged(bool visible) {
    if (!visible && _unsavedChanges && !await Dialogs.ConfirmAsync("Lose your changes?", null, "Lose them", "Keep editing")) {
      return;
    }
    _visible = visible;
  }
}
```

If you don't handle `VisibleChanged` at all, the dialog hides itself when the user closes it.

### Methods

| Method | Description |
| --- | --- |
| `Refresh()` | Re-renders the dialog, eg after changing something its content shows that it doesn't know about |

### A form in a dialog

Put the `EditForm` inside `DialogContent`, and give the submit button in `DialogButtons` a `form` attribute pointing at it, so that the button can live in the button row...

```xml
<PixataDialog @bind-Visible="_visible" Title="Edit contact" FocusedElementSelector="#email">
  <DialogContent>
    <EditForm id="contact-form" Model="_contact" OnValidSubmit="Save">
      <DataAnnotationsValidator />
      <InputText id="email" @bind-Value="_contact.Email" />
      <ValidationMessage For="() => _contact.Email" />
    </EditForm>
  </DialogContent>
  <DialogButtons>
    <button type="submit" form="contact-form" class="@PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Primary, null, null, null)">Save</button>
  </DialogButtons>
</PixataDialog>
```

## Focus, Esc and stacking

These apply to `PixataDialog`, the predefined dialogs and [`PixataWindow`](Readme.Window.md) alike.

- **Focus**: when a dialog opens, the focus moves to the element matching `FocusedElementSelector` if there is one, otherwise to the first element in the dialog that can take the focus, otherwise to the dialog itself. Text in a focused text box is selected, so the user can type over it.
- **Keeping the focus inside**: while a dialog (or a modal window) is open, Tab and Shift+Tab cycle through the elements inside it, and the page behind can't be clicked.
- **Esc** closes the dialog that has the focus (if it closes on Esc), and only that one. Components inside a dialog that use Esc themselves, such as drop-downs, stop the key there, so pressing Esc in an open drop-down closes the drop-down rather than the dialog.
- **Giving the focus back**: when a dialog closes, the focus goes back to whatever had it before the dialog opened (usually the button that opened it).
- **Stacking**: several dialogs and windows can be open at once. The one most recently opened, clicked or focused is in front, and modal ones always stay in front of what was open before them. They are stacked upwards from `--pixata-blazor-overlay-z-index`, and stay below `--pixata-blazor-popup-z-index`, so the drop-downs and menus of components inside a dialog show on top of it. The wrapper of the one in front has a `data-pixata-blazor-active` attribute.
- **Right-to-left**: if the page (or the element the dialog was declared in) has `dir="rtl"`, so does the dialog.

The script that does this is `_content/Pixata.Blazor/pixata-dialogs.js`, which the components load themselves when the first one opens. You don't need to add a `<script>` tag. Nothing is called until the component has rendered interactively, so dialogs are safe to use with prerendering.

## Predefined dialogs

### Setup

`PixataDialogFactory` and `PixataDialogTexts` are registered for you by [`AddPixataBlazor()`](Readme.md#registering-dependencies).

The dialogs are shown by a `PixataDialogHost`, which you add once to your layout (eg `MainLayout.razor`)...

```xml
<PixataDialogHost />
```

The host must be somewhere that is rendered interactively, and in the same DI scope as the code that shows the dialogs. In a Blazor Server or WASM app, or a Blazor Web App with global interactivity, the layout is fine. If your layout is rendered statically and only some pages are interactive, put the host in those pages (or in an interactive component they share) instead.

If a dialog is requested when there's no host, `PixataDialogFactory` throws an `InvalidOperationException` saying so, rather than leaving your code waiting for an answer that will never come. If the host goes while a dialog is waiting for an answer (eg because the circuit has ended), the dialog counts as cancelled: `AlertAsync` completes, `ConfirmAsync` returns `false` and `PromptAsync` returns `null`.

Show the dialogs in response to something the user does (eg in a button's click handler), or from `OnAfterRenderAsync`, rather than while a component is initialising (in `OnInitialized(Async)` or `OnParametersSet(Async)`). A host in the layout is created after the page inside it, so on the first page the user visits it doesn't exist yet at that point, and while a page is being prerendered there is nobody to answer anyway.

### Showing them

Inject the factory and `await` the dialog...

```csharp
@inject PixataDialogFactory Dialogs

await Dialogs.AlertAsync("Your changes have been saved.", "Saved");

if (await Dialogs.ConfirmAsync("Are you sure you want to delete Fred Bloggs?", "Delete customer")) {
  // ...
}

string? name = await Dialogs.PromptAsync("What would you like to call the report?", "Save report", "Monthly sales");
if (name is not null) {
  // ...
}
```

| Method | Returns | Description |
| --- | --- | --- |
| `AlertAsync(text, title, okButtonText)` | `Task` | Shows a message with an OK button. Completes when the user closes it |
| `ConfirmAsync(text, title, okButtonText, cancelButtonText)` | `Task<bool>` | Asks the user to confirm something. `true` if they click OK, `false` if they click Cancel or press Esc |
| `PromptAsync(text, title, defaultText, okButtonText, cancelButtonText)` | `Task<string?>` | Asks the user to type something. What they typed if they click OK or press Enter, or `null` if they click Cancel or press Esc. `defaultText` is what the text box starts with |

Everything but `text` is optional. If you don't give a title, the dialog has no titlebar (unless you have set an app-wide one, see below). Line breaks in `text` are shown.

The alert and confirm dialogs focus the OK button, so the user can just press Enter, and the prompt focuses the text box. They have no close button, and Esc cancels them. Each one has `role="alertdialog"`. You can show one from inside another dialog (or several at once), and each is answered separately. The methods can be called from any thread.

### Button text and translations

The text of the buttons comes from the app's `PixataDialogTexts`, which also has the tooltips (and screen reader labels) of the titlebar buttons of dialogs and [windows](Readme.Window.md)...

| Property | Default | Used for |
| --- | --- | --- |
| `Ok` | `OK` | The button that accepts an alert, confirm or prompt dialog |
| `Cancel` | `Cancel` | The button that cancels a confirm or prompt dialog |
| `Close` | `Close` | The close button in the titlebar of a dialog or window |
| `Minimise` | `Minimise` | A window's minimise button |
| `Maximise` | `Maximise` | A window's maximise button |
| `Restore` | `Restore` | The button that restores a minimised or maximised window |
| `AlertTitle`, `ConfirmTitle`, `PromptTitle` | blank | The title of an alert, confirm or prompt dialog shown without one. Blank means no titlebar |

To translate them for the whole app, register your own before calling `AddPixataBlazor()` (which then leaves yours alone), eg built from an `IStringLocalizer`...

```csharp
builder.Services.AddScoped(sp => {
  IStringLocalizer<SharedResources> l = sp.GetRequiredService<IStringLocalizer<SharedResources>>();
  return new PixataDialogTexts { Ok = l["OK"], Cancel = l["Cancel"], Close = l["Close"] };
});
builder.Services.AddPixataBlazor();
```

To change the text for one dialog, pass it to the method...

```csharp
bool save = await Dialogs.ConfirmAsync("Save the changes?", null, "Yes", "No");
```

## Styling

The dialogs use the shared `--pixata-blazor-*` colours and spacing (see [Styling](Readme.Styling.md)), so they follow your theme. These variables are specific to them...

| Variable | Default | Description |
| --- | --- | --- |
| `--pixata-blazor-overlay-bg` | `rgba(0, 0, 0, .5)` | The overlay behind the dialog (shared with other components) |
| `--pixata-blazor-overlay-z-index` | `10000` | Where the stack of dialogs and windows starts (shared) |
| `--pixata-blazor-window-bg` | surface background | The dialog's background |
| `--pixata-blazor-window-color` | text colour | The dialog's text colour |
| `--pixata-blazor-window-border-color` | border colour | The dialog's border, and the lines around the titlebar and buttons |
| `--pixata-blazor-window-border-radius` | `0` | The corners |
| `--pixata-blazor-window-line-height` | `1.25` | The line height inside the dialog |
| `--pixata-blazor-window-titlebar-bg` | alternative surface background | The titlebar's background |
| `--pixata-blazor-window-titlebar-color` | text colour | The titlebar's text colour |
| `--pixata-blazor-window-titlebar-padding` | `.75rem 1rem` | The titlebar's padding |
| `--pixata-blazor-window-title-font-size` | large font size | The title's font size |
| `--pixata-blazor-window-content-padding` | `1rem` | The content's padding |
| `--pixata-blazor-window-footer-padding` | `.5rem` | The button row's padding |
| `--pixata-blazor-window-footer-gap` | `.5rem` | The gap between the buttons |
| `--pixata-blazor-dialog-shadow` | a soft shadow | The dialog's shadow |
| `--pixata-blazor-dialog-viewport-margin` | `1rem` | The space kept around a dialog at the edges of the viewport (halved on a phone) |
| `--pixata-blazor-dialog-predefined-min-width` | `200px` | The smallest width of an alert, confirm or prompt dialog |

The `--pixata-blazor-window-*` variables are shared with `PixataWindow`, so dialogs and windows look the same.

The main CSS classes are...

| Class | Element |
| --- | --- |
| `pixata-blazor-dialog-wrapper` | The wrapper moved to the end of the body, holding the overlay and the dialog |
| `pixata-blazor-overlay` | The overlay |
| `pixata-blazor-dialog` | The dialog itself, plus `pixata-blazor-dialog-<ThemeColor>` when a `ThemeColor` is set |
| `pixata-blazor-dialog-titlebar`, `pixata-blazor-dialog-title` | The titlebar and the title in it |
| `pixata-blazor-dialog-titlebar-actions`, `pixata-blazor-dialog-titlebar-action` | The close button and its container |
| `pixata-blazor-dialog-content` | The content |
| `pixata-blazor-dialog-actions` | The button row, plus `pixata-blazor-dialog-actions-start`, `-center`, `-end` or `-stretch` |
| `pixata-blazor-dialog-predefined` | An alert, confirm or prompt dialog, plus `pixata-blazor-dialog-alert`, `-confirm` or `-prompt` |
| `pixata-blazor-dialog-message` | The text of an alert, confirm or prompt dialog |
| `pixata-blazor-dialog-prompt-form`, `pixata-blazor-dialog-prompt-input` | A prompt's form and text box |
| `pixata-blazor-dialog-ok`, `pixata-blazor-dialog-cancel` | The OK and Cancel buttons of the predefined dialogs |
| `pixata-blazor-layer-anchor` | The (empty, `display: contents`) placeholder left where the dialog was declared |
