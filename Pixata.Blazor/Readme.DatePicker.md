# Date picker, date input and calendar

>Part of [Pixata.Blazor](Readme.md)

`PixataDatePicker`, `PixataDateInput` and `PixataCalendar` are a date picker (an input with a calendar popup), a date input on its own, and a calendar on its own. They render on the client or the server, so they can be used in WASM as well as Blazor Server apps. The [sample page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/PixataDatePickerSample.razor) shows each one.

They are in the `Pixata.Blazor.Components` namespace, and need [the stylesheet](Readme.Styling.md). They use a small JavaScript module (`_content/Pixata.Blazor/pixata-blazor.js`), which they load themselves, so there's nothing to add to your page.

```razor
<PixataDatePicker @bind-Value="_order.Due" Min="DateTime.Today" />

<PixataDateInput @bind-Value="_dateOfBirth" Format="dd/MM/yyyy" />

<PixataCalendar @bind-Value="_holiday" ShowWeekNumbers="true" />
```

## Typing a date

The date input (and the input in the date picker) works a part at a time. Clicking in the input selects the part (day, month, year and so on) under the mouse, and then...

- Typing digits fills in the selected part. It moves on to the next part when another digit couldn't follow, so typing 3 in the day waits for a second digit (it could be 30 or 31), but typing 4 moves on to the month. Turn this off with `AutoSwitchParts="false"`
- A digit that would make an impossible date is ignored, so with a day of 31, the month can't be set to November
- Typing a separator, such as `/`, moves to the next part, so you can type `1/2/2026`
- For a month shown by name (`MMM` or `MMMM`), typing letters picks the month. Typing the same letter again moves through the months starting with it, so J, J, J gives January, June and then July
- Up and Down change the selected part. On a complete date, they work like adding a day, month or year, so the 31st plus a day is the 1st of the next month, and 31st January plus a month is 28th February. On an empty part, they start from today
- Left and Right (and Home and End) move between the parts
- Backspace and Delete clear the selected part. An empty part shows its format, such as `MMM`
- Pasting a whole date works, as long as it's in the input's format or one the culture recognises

The value only changes when the input holds a complete date between `Min` and `Max`. Until then, the input is shown as invalid (with a red border). A nullable value becomes `null` while the date is incomplete.

A two-digit year (`yy`) is in the 2000s up to `TwoDigitYearMax` (68 by default), and in the 1900s above it.

On a phone, the input asks for a numeric keyboard when the format only has numbers in it. Set `InputMode` to change that.

## Parameters

These apply to both `PixataDateInput` and `PixataDatePicker`. `TValue` can be `DateTime`, `DateOnly`, or a nullable version of either.

| Parameter | Type | What it does |
| --- | --- | --- |
| `Value` | `TValue` | The date. Can be two-way bound |
| `Format` | `string` | A .NET date format, such as `dd/MM/yyyy`, or a standard one such as `d` (the default, which is the culture's short date) |
| `Min`, `Max` | `DateTime` | The range allowed. Default to 1st January 1900 and 31st December 2099 |
| `Placeholder` | `string` | Shown when the value is null and the input doesn't have focus |
| `ShowClearButton` | `bool` | Shows an x that clears the value. Only for nullable types |
| `Enabled`, `ReadOnly` | `bool` | As you'd expect |
| `AutoSwitchParts` | `bool` | Whether the selection moves on once a part is complete. Defaults to true |
| `TwoDigitYearMax` | `int` | See above. Defaults to 68 |
| `Width`, `Class`, `Id`, `TabIndex`, `InputMode` | | Passed to the component's elements |
| `OnChange` | `EventCallback<object?>` | Fires when the user has finished changing the value: when they press Enter, leave the input, or pick a date from the calendar. Unlike `ValueChanged`, it doesn't fire on every key |
| `OnBlur` | `EventCallback` | Fires when the input loses focus |

The date picker also has...

| Parameter | Type | What it does |
| --- | --- | --- |
| `View`, `BottomView` | `PixataCalendarView` | The view the calendar opens in, and the most detailed one. Set `BottomView` to `Year` to pick a month |
| `DisabledDates` | `List<DateTime>` | Dates the user can't pick from the calendar |
| `ShowWeekNumbers`, `ShowOtherMonthDays` | `bool` | Passed to the calendar |
| `PopupClass` | `string` | A CSS class for the popup |
| `OnOpen`, `OnClose` | `EventCallback` | Fire when the calendar opens and closes |

and `Open()` and `Close()` methods. Both components have `FocusAsync()`.

The calendar opens from the button, or with Alt+Down in the input. It closes when a date is picked, on Escape or Alt+Up, or when the user clicks somewhere else. It's positioned with fixed positioning, so it isn't cut off by a container with hidden overflow (such as a grid cell), and it stays inside the window.

### Forms

Both work inside an `EditForm`. Bind with `@bind-Value`, and they tell the form when the value changes, and show as invalid when the field has validation errors...

```razor
<EditForm Model="_order">
  <DataAnnotationsValidator />
  <PixataDatePicker @bind-Value="_order.Due" />
  <ValidationMessage For="() => _order.Due" />
</EditForm>
```

## Calendar

| Parameter | Type | What it does |
| --- | --- | --- |
| `Value` | `DateTime?` | The selected date. Can be two-way bound |
| `Date` | `DateTime?` | The date being shown (and with the keyboard focus). Defaults to the value, or today. Can be two-way bound |
| `View` | `PixataCalendarView` | `Month`, `Year`, `Decade` or `Century`. Can be two-way bound |
| `BottomView`, `TopView` | `PixataCalendarView` | How far down and up the user can go. Clicking a cell in the bottom view selects it. Default to `Month` and `Century` |
| `Min`, `Max` | `DateTime` | The range of dates allowed |
| `DisabledDates` | `List<DateTime>` | Dates that can't be picked |
| `ShowWeekNumbers` | `bool` | Shows ISO-8601 week numbers |
| `ShowOtherMonthDays` | `bool` | Shows the days from the months either side that fill the first and last weeks. Defaults to true |

It has `FocusAsync()` and `NavigateTo(date, view)` methods.

The keyboard works like this: the arrow keys move by one cell or one row, Page Up and Page Down by one month (or year, decade or century), Home and End go to the start and end of it, Ctrl+Up goes up a view, and Enter or Space picks the cell (or goes down a view).

The first day of the week, and the day and month names, come from the current culture.

## Styling

The colours and sizes come from these custom properties, which you can override on `:root` or on any container...

| Property | Default | What it sets |
| --- | --- | --- |
| `--pixata-datepicker-font-size` | `.875rem` | The font size |
| `--pixata-datepicker-line-height` | `1.4285` | The line height |
| `--pixata-datepicker-color` | `#3d3d3d` | The text colour |
| `--pixata-datepicker-subtle-color` | `#666` | Placeholders, day names, week numbers and days from other months |
| `--pixata-datepicker-bg` | `#fff` | The background |
| `--pixata-datepicker-border-color` | `rgba(0, 0, 0, .08)` | Borders |
| `--pixata-datepicker-hover-border-color` | `rgba(0, 0, 0, .16)` | The input's border on hover and focus |
| `--pixata-datepicker-invalid-border-color` | `#ca1400` | The input's border when the date is invalid |
| `--pixata-datepicker-button-bg`, `--pixata-datepicker-button-hover-bg` | `#f5f5f5`, `#ebebeb` | The calendar button |
| `--pixata-datepicker-primary`, `--pixata-datepicker-primary-hover`, `--pixata-datepicker-primary-color` | `#ff6358`, `#ea5a51`, `#fff` | The selected date, today, and the calendar title |
| `--pixata-datepicker-focus-shadow` | `0 0 0 2px rgba(61, 61, 61, .08)` | The input's focus ring |
| `--pixata-datepicker-popup-shadow` | A soft two-layer shadow | The calendar popup's shadow |
| `--pixata-datepicker-border-radius` | `4px` | Rounded corners |

All the classes start with `pixata-blazor-dateinput-`, `pixata-blazor-datepicker-` or `pixata-blazor-calendar-` (for example `pixata-blazor-calendar-title`, `pixata-blazor-calendar-selected` and `pixata-blazor-datepicker-popup`), so you can target any part of them in your own CSS.
