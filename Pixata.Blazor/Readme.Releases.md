# Pixata.Blazor releases

What changed in each version of [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.md), newest first.

>**Version 5.17.0** hides the clear button in the filter row for `PixataTextGridColumn` and `PixataNumericGridColumn` (`PixataEnumGridColumn` never showed it), as it's easy enough to delete what was typed. **Behaviour change:** set `ShowFilterClearButton="true"` on the column to get it back. See [typed columns](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#typed-columns)

>**Version 5.16.0** adds `DisplayStyle` to `PixataEnumGridColumn`, which sets how values whose names are several words are shown, in the cells and in the filter's drop-down: `LowerCamelCase` ("Awaiting signature", the default), `UpperCamelCase` ("Awaiting Signature") or `NotSplit` ("AwaitingSignature"). **Behaviour change:** enum columns used to show the names as they are, so set `DisplayStyle="PixataEnumDisplayStyle.NotSplit"` to keep the old look. See [typed columns](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md#typed-columns)

>**Version 5.15.0** adds donut charts to `PixataChart`. A `PixataChartSeries` with `Type="PixataChartSeriesType.Donut"` is a ring, with a `HoleSize`, and several donut series make rings inside each other that share their legend items. A `PixataChartDonutCenterTemplate` puts content (such as a total) in the hole. Every chart now shows "No data" over its plot area when none of its series has any data, which a `PixataChartNoDataTemplate` can replace or turn off. See the [charts documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Charts.md#donuts)

>**Version 5.14.0** makes the picked radio button in a `PixataRadioGroup` easier to see, by drawing it with a solid dot in the middle rather than as a ring around a hollow middle. The old look is still there with `CheckedStyle="PixataRadioGroupCheckedStyle.Ring"`. See [styling the radio group](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.RadioGroup.md#styling)

>**Version 5.13.2** fixes typing a date with its separators into a `PixataDateInput`, `PixataDatePicker` or `PixataDateRangePicker`. Typing `01/10/2026` skipped the month, as the `/` moved on again after the day had already moved on by itself. A separator typed straight after a part has filled itself is now ignored, while one typed after a part that isn't complete (as in `1/10/2026`) still moves on (#109)

>**Version 5.13.1** fixes `PixataHeatmap` on narrow screens. Inside a flex item, such as the main column of a layout with a sidebar, the table's width stretched the page rather than the heatmap scrolling sideways in its own box (#110)

>**Version 5.13.0** adds `PixataHeatmap`, which shows a grid of values as coloured cells, with a label for each row and column, such as the days of the week against the hours of the day, to show when something is busy. See the [heatmap documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Heatmap.md) (#110)

>**Version 5.12.1** fixes restoring the scroll position of a `PixataGrid` with `ScrollMode="Virtual"`. The position was saved, but the grid went straight back to the top: the browser's first report of where the grid was scrolled to arrived before the grid had scrolled to the saved row, and the grid took it to mean the user had scrolled back to the start (#104)

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
