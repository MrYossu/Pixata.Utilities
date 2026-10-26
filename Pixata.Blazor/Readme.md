# Pixata.Blazor [![Pixata.Blazor Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor)](https://www.nuget.org/packages/Pixata.Blazor/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Blazor/avion.png "Pixata")

Like many developers, I find myself writing the same Blazor components over and over again. I have gathered some of those components together in one place, and am making them available for anyone else who might find them useful.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for these components.

>**Version 5.12.0** lets `PixataGridCommandButton.Icon` take a `PixataSvgIcon` or the CSS class(es) of an icon font (such as `"fas fa-trash"`), as well as a `PixataGridIcon`, as the other components' `Icon` parameters do. Existing code doesn't change, but `Icon` is now an `object`, so any code that reads it as a `PixataGridIcon` needs a cast. See [command buttons](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#command-buttons) (#106)

>**Version 5.11.1** fixes four things in `PixataGrid`'s state...
>- A column's width is only saved once the user has resized it, so a column whose `Width` you change in your code gets the new width for anyone who has seen the grid before (#102)
>- If the parent re-rendered while the saved state (or an async `OnStateInit` handler) was being read, the grid could read its first page with `OnRead` without the saved sorting and filtering, while the header showed them. It now waits for the state (#103)
>- A grid with `ScrollMode="Virtual"` now saves its scroll position, as the first row in view, so it comes back where the user left it (#104)
>- A restored date filter is now shown in the filter row. This was in the date input, so a `PixataDateInput`, `PixataDatePicker` or `PixataDateRangePicker` given a value while it is starting up now shows it (#105)

>**Version 5.11.0** adds `PixataButton`, a plain button with the package's theme colours, fill modes, roundness and sizes, which can show a spinner while its action runs, and `PixataPrimaryButton` and `PixataSecondaryButton`, the same button with defaults for a page's main action and the others next to it. See the [buttons documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Buttons.md#plain-buttons)

>**Version 5.10.0** adds `FocusedElementSelector` to `PixataWindow`, to choose the element that gets the focus when the window opens

>**Version 5.9.1** fixes pressing Enter in a `PixataDatePicker`, `PixataDateInput` or `PixataDateRangePicker` input, which didn't submit the form it was in

>**Version 5.9.0** adds `PixataTreeView`, which shows flat or hierarchical data as a tree, with expanding and collapsing, loading children on demand, selection, checkboxes, templates, links, and drag and drop within a tree, between trees and on to a grid. It also adds `PixataFieldReader.TryGetValue()`, which reads a property without throwing if the item doesn't have it. See the [tree view documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.TreeView.md)

>**Version 5.8.0** makes two changes to the grid's filter row...
>- Columns have a `ShowFilterClearButton` parameter. Set it to `false` to hide the clear button in the column's filter row cell but keep the operator drop-down, such as for a numeric column, where it's easy enough to delete what was typed
>- **Behaviour change:** the drop-down in a `PixataEnumGridColumn`'s filter row cell now starts with an "(All)" entry, which clears the filter, and shows it when there's no filter, in place of "-Select value-". Its text comes from the grid's `FilterAll` message. Set `AddAllEntry="false"` on the column to leave it out. See [typed columns](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#typed-columns)

>**Version 5.7.0** lets a grid column's custom filter template replace its `Filter` with a new one, so it can join its descriptors with Or, such as to search two fields at once. See [custom filters](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#custom-filters)

>**Version 5.6.0** makes some more changes that help when moving from Telerik's components...
>- `PixataDateInput`, `PixataDatePicker` and `PixataDateRangePicker` have a `Size` parameter (`sm`, `md` or `lg`, from `PixataThemeConstants.Size`), as the drop-downs and radio group do. See the [date picker documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.DatePicker.md#parameters)
>- `PixataGrid` has a `FilterRowDebounceDelay` parameter, which sets how many milliseconds the filter row waits after the user stops typing before it filters (150 by default, as before), so a grid that reads from the server through `OnRead` isn't asked for data on every key
>- Grid columns have an `IgnoreTime` parameter, which makes the filter on a `DateTime` column compare the date only, so "on 21/10/2026" finds anything that day. For data that comes through `OnRead`, you can call `args.Request.RemoveTime(...)` instead, in place of Telerik's `TelerikFilterHelper.RemoveTime()`. See [dates with a time](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#dates-with-a-time)
>- **Behaviour change:** `IgnoreTime` is on by default for a `PixataDateGridColumn`, so its "on" filter now finds the items at any time on the day, rather than only those at midnight. "Before" and "on or after" are unchanged. Set `IgnoreTime="false"` to compare the time as well
>- This version depends on Pixata.Extensions 2.23.0

>**Version 5.5.0** makes the notifications suit a dark theme, and stops them needing Font Awesome. The toast's background and text now follow `--pixata-blazor-surface-bg` and `--pixata-blazor-text-color` (or the new `--pixata-notification-bg` and `--pixata-notification-color`), the close button's cross is drawn in the colour of the text around it, and the default icons are built-in SVG ones. It also depends on Pixata.Extensions 2.22.0. Versions 5.0.0 to 5.4.0 depended on Pixata.Extensions 2.21.0, which doesn't have the grid types, so any page with a `PixataGrid` threw a `TypeLoadException`. The notifications' class names have changed, so if you styled them yourself, see [styling changes in v5.5.0](#styling-changes-in-v550) below.

>**Version 5.4.0** lets you theme the audit viewer, `Confirm`, `Inform` and the notifications through the CSS variables in pixata.css, lets notifications show formatted messages (such as a list), and changes the notifications' default icons to ones in Font Awesome's free set. Some class names and ids have changed, so if you styled any of these yourself, see [styling changes in v5.4.0](#styling-changes-in-v540) below.

>**Version 5.3.0** makes some changes that help when moving from Telerik's components...
>- `PixataDropDownList` has an icon-only mode, set with the new `Icon` (or `IconTemplate`) parameter. See the [drop-downs documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.DropDowns.md#icon-only)
>- The content of a `PixataGridCommandColumn` is now given the row's item, so its buttons can depend on the row. If a template inside the column also uses `context`, add `Context="row"` to the column. See the [grid documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#using-the-row-in-a-command-column)
>- **Behaviour change:** when you set `PixataTabStrip.ActiveTabId` one way and handle `ActiveTabIdChanged` yourself, the tab now only changes when you set `ActiveTabId`, as in Telerik, so you can load a tab's data before it is shown. `@bind-ActiveTabId` works as before. See the [tab strip documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.TabStrip.md#who-changes-the-tab)
>- Leaving a page while a date picker, calendar or `VirtualiseWithState` is starting up no longer crashes the circuit

>**Version 5.2.0** adds `PixataDateRangePicker`, for picking the start and end of a range of dates, and gives `PixataCalendar` a range selection mode and the `Views` parameter for showing more than one month side by side. See the [date picker documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.DatePicker.md#date-range-picker) for details.

>**Version 5.1.0** adds typed grid columns (`PixataTextGridColumn`, `PixataNumericGridColumn`, `PixataDateGridColumn` and `PixataEnumGridColumn`) with short lists of filter operators, the `FilterOperators` and `ShowFilterCellButtons` column parameters, and `PixataGridBoolFilter`, a three-state checkbox filter for `bool` columns. See the [grid documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#typed-columns) for details.

>**Version 5.0.0** adds a large set of new components: `PixataGrid` (with Excel export in the separate [Pixata.Blazor.Grid.Excel](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Grid.Excel/Readme.md) package), the date picker, drop-downs, tab strip, editor, file select and upload, dialogs, window, wizard and stepper, radio group, switch, charts, drop-down and split buttons, scheduler, and the `FormRowDate` form row. See the documentation pages below for details.

## Styling changes in v5.5.0

These only affect you if you wrote CSS of your own for the notifications, or relied on their default look. See [Notifications](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Notifications.md#icons-and-colours) for the details.

- **Notification classes.** The classes inside the notification area now start with `pixata-blazor-notification`, so your own styles for classes such as `message` or `icon` no longer land on the notifications. `notifications-area` is now `pixata-blazor-notification-area`, `notification` is `pixata-blazor-notification`, and `new`, `remove`, `icon`, `message` and `dismiss` are `pixata-blazor-notification-` followed by the old name. Their styles have moved from the component's scoped CSS into pixata.css.
- **Notification icons.** The default icons are now built-in SVG ones, so you don't need Font Awesome for them. Call `NotificationHelper.UseFontAwesomeIcons()` in `Program.cs` if you want Font Awesome's icons back. `NotificationHelper.GetIconClass()` now returns `""` for a type that shows the built-in icon, and the new `GetDisplayIcon()` gives you what to pass to `PixataIcon` to show the same icon as the notifications.
- **Notification colours.** The background was always white. It now comes from `--pixata-notification-bg`, which defaults to `--pixata-blazor-surface-bg`, and the text from `--pixata-notification-color` (which defaults to `--pixata-blazor-text-color`) rather than the page's text colour. The corners are now rounded (`--pixata-notification-border-radius`).
- **Close buttons.** The cross on a `pixata-btn-close` button was always black. It's now drawn in the colour of the text around it, or in `--pixata-close-button-color` if you set that.

## Styling changes in v5.4.0

These only affect you if you wrote CSS of your own for these components, or relied on their default look.

- **Audit viewer classes.** The viewer's styles have moved from a `<style>` block inside the component into pixata.css, and every class now starts with `pixata-blazor-audit-`, followed by the old name without any `audit-` it started with. So `view-btn` is now `pixata-blazor-audit-view-btn`, and `audit-timeline` is now `pixata-blazor-audit-timeline`. The old names were global, so they could clash with your own CSS, and the block's rules (for `.filters`, `.spinner`, `.user` and so on) applied to the rest of your page while the viewer was on it. The colours now come from the `--pixata-*` variables, so the viewer follows your theme, and it uses your page's font rather than the system font. See [styling the audit viewer](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.AuditViewer.md#styling).
- **`Confirm` and `Inform` ids.** The `confirmOverlay`, `confirmOverlayContent`, `informOverlay` and `informOverlayContent` ids are now the classes `pixata-blazor-confirm-overlay`, `pixata-blazor-confirm-content`, `pixata-blazor-inform-overlay` and `pixata-blazor-inform-content`, so you can have more than one on a page. The pop-up has a softer shadow and smaller corners, and both can be changed with variables. See [Containers](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Containers.md#styling-confirm-and-inform).
- **Notification icons.** The default icons are now Font Awesome's solid (`fas`) ones, which are in the free set, rather than the regular (`far`) ones, most of which are Pro only. The icon colours now come from CSS variables, so `NotificationHelper.GetIcon()` is obsolete. See [Notifications](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Notifications.md#icons-and-colours).

I sometimes waffle on about these components [in my blog](https://www.pixata.co.uk/tag/blazor/). No-one reads it, but it satisfies my deeply buried desire to be a writer. If you read the blog, you'll realize why the desire is still deeply buried!

There is a [complementary package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents), which contains additional components for those who have a subscription to Telerik.

## Sample project

I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Sample) to the repository, and use that to try out and demonstrate the components. It doesn't contain samples for all the components yet, but I hope to add more over time. It is deployed at [test.pixata.co.uk](https://test.pixata.co.uk).

## Documentation

The documentation is split over the following pages...

| Page | What's in it |
| --- | --- |
| [Styling](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Styling.md) | The stylesheet the components ship with, how to fit it to your own theme, and what changed when the Bootstrap dependency was dropped |
| [Components](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Components.md) | `VirtualiseWithState`, `SitePageTitle`, `IdentityInspector`, `HebrewDatePicker`, `ListOrGridChooser` and the `CommonComponentBase` base class |
| [Grid](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md) | `PixataGrid`, a data grid with paging, sorting, filtering, selection, editing, grouping, resizable, movable and frozen columns, saved state, virtual scrolling, detail rows, row dragging, CSV export, keyboard navigation, translation and right-to-left layout. Excel export is in the [Pixata.Blazor.Grid.Excel](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Grid.Excel/Readme.md) package |
| [Date picker](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.DatePicker.md) | `PixataDatePicker`, `PixataDateInput` and `PixataCalendar`, for picking and typing dates |
| [Date range picker](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.DatePicker.md#date-range-picker) | `PixataDateRangePicker`, for picking the start and end of a range of dates, and the calendar's range mode |
| [Containers](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Containers.md) | `HtmlRaw`, `Busy`, `Loader`, `Confirm`, `Inform`, `MessageView`, `Expander` and `DumpCollection` |
| [ApiResponseView](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.ApiResponseView.md) | The container that renders an `ApiResponse<T>`, handling loading, errors and expired sessions for you |
| [Forms](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Forms.md) | The form layout components and the complete `FormRowXxx` rows |
| [Notifications](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Notifications.md) | Toast-style notifications, and how to have API errors reported through them |
| [Audit viewer](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.AuditViewer.md) | The component for browsing the audit trail recorded by Pixata.AspNetCore |
| [Helpers and services](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Helpers.md) | `PersistentStateHelper`, `TemplateHelper`, `MessageBroker`, `PasswordOptionsHelper`, `ScrollStateService`, `PixataBaseClientService` and `TryGetQueryString()` |
| [Payload encryption](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Encryption.md) | Client-side setup for encrypting API traffic between a WASM app and an ASP.NET Core server |
| [Declarative control flow](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.ControlFlow.md) | `If`, `Switch` and `ForEach`, which replace the Razor `@if`, `@switch` and `@foreach` statements |
| [Drop-downs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.DropDowns.md) | `PixataComboBox`, `PixataDropDownList` and `PixataMultiSelect` |
| [Tab strip](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.TabStrip.md) | `PixataTabStrip` and `PixataTab` |
| [Tree view](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.TreeView.md) | `PixataTreeView`, for showing flat or hierarchical data as a tree, with selection, checkboxes, loading on demand, and drag and drop |
| [Editor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Editor.md) | `PixataEditor`, a rich (HTML) text editor |
| [File select and upload](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.FileUpload.md) | `PixataFileSelect`, which hands the chosen files to your code, and `PixataUpload`, which posts them to an endpoint |
| [Dialogs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Dialogs.md) | `PixataDialog`, and the alert, confirm and prompt dialogs you can show from code |
| [Window](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Window.md) | `PixataWindow`, a floating window that can be moved, resized, minimised and maximised |
| [Wizard and stepper](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Wizard.md) | `PixataWizard` and `PixataStepper` |
| [Radio group](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.RadioGroup.md) | `PixataRadioGroup` |
| [Switch](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Switch.md) | `PixataSwitch`, a toggle switch for booleans |
| [Charts](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Charts.md) | `PixataChart`, with column, line and pie series |
| [Buttons](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Buttons.md) | `PixataButton`, `PixataPrimaryButton`, `PixataSecondaryButton`, `PixataDropDownButton` and `PixataSplitButton` |
| [Scheduler](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Scheduler.md) | `PixataScheduler`, for showing and editing appointments in day, multi-day, week, month, timeline and agenda views |

## Registering dependencies

Some components in this package require services to be registered in the DI container. To make this easier, you can use the `AddPixataBlazor` extension method in your `Program.cs` file...

```csharp
builder.Services.AddPixataBlazor();
```

This registers the following services (all from this package)...

| Service | What needs it |
| --- | --- |
| `MessageBrokerInstance` | The [`MessageBroker`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Helpers.md#messagebroker), which lets components send messages to each other without knowing about each other |
| `NotificationHelper` | The [notifications](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Notifications.md) ([sample page](https://test.pixata.co.uk/Notifications)) |
| `PasswordOptionsHelper` | Showing the user your Identity password requirements |
| `PersistentStateHelper<T>` | Avoiding hitting the database twice when a page loads. Used by the [`ApiResponseView`](https://test.pixata.co.uk/ApiResponseViewRegular), but can be used independently |
| `PixataDialogFactory` | The [alert, confirm and prompt dialogs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Dialogs.md) you show from code ([sample page](https://test.pixata.co.uk/DialogsSample)). It also needs `<PixataDialogHost />` in your layout, as described below |
| `PixataDialogTexts` | The button text and tooltips used by the dialogs and windows (OK, Cancel, Close and so on). Register your own instance before calling `AddPixataBlazor()` to translate them |
| `ScrollStateService` | The `VirtualiseWithState` component |
| `TemplateHelper` | Building templates for grids and other templated components ([sample page](https://test.pixata.co.uk/pixata-grid)) |

>`PersistentStateHelper<T>` does the same job as the [`[PersistentState]` attribute](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#declarative-model-for-persisting-state-from-components-and-services) added in .NET 10. This one was written around .NET 8, and is still useful for projects targetting .NET versions before 10.

It also registers [Blazored.LocalStorage](https://github.com/Blazored/LocalStorage), which `ScrollStateService` needs. This package has always referenced Blazored.LocalStorage, but until v2.34.0 it left you to register it, so unless you knew to add `builder.Services.AddBlazoredLocalStorage()` yourself, your app fell over at startup when the container was validated. If you have already registered it (with your own options, for example), your registration is left alone.

Note that you need to do this in any `Program.cs` file, so if you have a mixed rendering mode (both server-side and client-side), you'll need to call `AddPixataBlazor` in both `Program.cs` files.

It is a good idea to add this line **after** your own service registrations, as it checks for duplicate registrations. Therefore, if you have already registered any of the services, you will see a message in your console...

>A service of type TemplateHelper has already been registered

This isn't actually a problem, but removing the duplicate registration will keep the code file a bit cleaner.

The audit viewer and the payload encryption have their own registration methods, as most apps don't want them. See the pages listed above.

### The dialog host

If you want to show the alert, confirm and prompt dialogs from code (with `PixataDialogFactory`), add the host component to your layout, inside an interactively rendered component...

```razor
@using Pixata.Blazor.Components

<PixataDialogHost />
```

If you ask for one of these dialogs without a host on the page, you get an exception telling you to add it. If your layout is rendered statically and only some pages are interactive, put the host in those pages instead, as it has to share a DI scope with the code that shows the dialogs. See [Dialogs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Dialogs.md) for more.

## Styling

Don't forget to add the stylesheet, or the components will render unstyled...

```html
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

See [Styling](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Styling.md) for what's in it, and how to fit it to your own theme.
