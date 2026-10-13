# Scheduler

>Part of [Pixata.Blazor](Readme.md)

`PixataScheduler` shows appointments in day, multi-day, week, month, timeline and agenda views. Users can add, edit and delete appointments in a built-in form (or your own), move and resize them by dragging (with a mouse, a pen or a finger), and set up recurring appointments with exceptions. Appointments can be linked to resources (rooms, people...), which colour them and can group the views. Everything can be done from the keyboard, and on a phone the scheduler starts in a single-day view.

The scheduler never changes your data itself. It raises events (`OnCreate`, `OnUpdate`, `OnDelete`) with the item to add, the updated copy, or the item to remove, and you update your data (and your database) in your handlers.

There is a [live demo](https://test.pixata.co.uk/SchedulerSample) on the sample site ([source](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/SchedulerSample.razor)).

## A simple example

```xml
<PixataScheduler Data="@_appointments" @bind-Date="@_date" @bind-View="@_view" Height="600px"
                 AllowCreate="true" AllowUpdate="true" AllowDelete="true"
                 OnCreate="@Create" OnUpdate="@Update" OnDelete="@Delete">
  <SchedulerViews>
    <PixataSchedulerDayView StartTime="@_dayStart" />
    <PixataSchedulerWeekView StartTime="@_dayStart" />
    <PixataSchedulerMonthView />
    <PixataSchedulerAgendaView />
  </SchedulerViews>
</PixataScheduler>

@code {
  private List<Appointment> _appointments = [];
  private DateTime _date = DateTime.Today;
  private PixataSchedulerView _view = PixataSchedulerView.Week;
  private readonly DateTime _dayStart = DateTime.Today.AddHours(7);

  private void Create(PixataSchedulerCreateEventArgs args) =>
    _appointments = [.. _appointments, (Appointment)args.Item];

  private void Update(PixataSchedulerUpdateEventArgs args) {
    Appointment updated = (Appointment)args.Item;
    _appointments = _appointments.Select(a => a.Id == updated.Id ? updated : a).ToList();
  }

  private void Delete(PixataSchedulerDeleteEventArgs args) {
    Appointment deleted = (Appointment)args.Item;
    // Deleting a recurring series should also delete its exceptions
    _appointments = _appointments.Where(a => a.Id != deleted.Id && a.RecurrenceId != deleted.Id).ToList();
  }

  public class Appointment {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public string? RecurrenceRule { get; set; }
    public List<DateTime>? RecurrenceExceptions { get; set; }
    public int? RecurrenceId { get; set; }
  }
}
```

The child tags (`PixataSchedulerDayView` and so on) pick up the appointment type from the scheduler, so you don't need to give it to them. Assigning a new list (rather than changing the one you passed) lets the scheduler see the change; if you change the list in place, call the scheduler's `Rebind()` method (see [Methods](#methods)).

No registration is needed. The scheduler's styles are in `_content/Pixata.Blazor/pixata.css`, and its script (for dragging and scrolling) is loaded when it is first needed.

## The appointments

The scheduler reads the appointments' fields by name, so any class will do. These are the defaults; set the matching parameter if your field has another name. Nested fields (`"Details.Title"`) and dictionaries (including `ExpandoObject`) work too.

| Parameter | Default | Type of the field | Description |
| --- | --- | --- | --- |
| `IdField` | `Id` | any | Identifies the appointment. Recurrence exceptions point at their series with it |
| `TitleField` | `Title` | `string` | Required |
| `StartField` | `Start` | `DateTime`, `DateTimeOffset`, `DateOnly` or `string` | Required |
| `EndField` | `End` | as for the start | Required. See the note on all-day appointments below |
| `IsAllDayField` | `IsAllDay` | `bool` | All-day appointments go in the all-day row of the day, multi-day and week views |
| `DescriptionField` | `Description` | `string` | Shown in the edit form |
| `ReadOnlyField` | `ReadOnly` | `bool` | Read-only appointments can't be edited, moved, resized or deleted |
| `RecurrenceRuleField` | `RecurrenceRule` | `string` | An iCalendar `RRULE`, eg `FREQ=WEEKLY;BYDAY=MO,WE`. See [Recurring appointments](#recurring-appointments) |
| `RecurrenceExceptionsField` | `RecurrenceExceptions` | a list (or array) of `DateTime`, or a comma-separated `string` | The occurrences of a series that have been changed or deleted |
| `RecurrenceIdField` | `RecurrenceId` | the type of the id | For an appointment that replaces one occurrence of a series, the id of the series |

Only the title, start and end fields are required; leave out the others and the scheduler (and its edit form) does without them. If a field you named explicitly is missing, the scheduler throws an exception that says which parameter to fix.

**All-day appointments** cover every day from the date they start to the date they end, inclusive: one from 13 October to 16 October covers the 13th, 14th, 15th and 16th, and one that starts and ends on the same date covers that day. The time parts are ignored. The edit form shows (and saves) the dates the same way.

A timed appointment that lasts a day or more also goes in the all-day row.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Data` | `IEnumerable<TItem>?` | | The appointments |
| `Date` | `DateTime` | today | The date the view shows (or contains, for the week and month views). Use `@bind-Date` to keep track of the user's navigation |
| `View` | `PixataSchedulerView` | the first view | `Day`, `MultiDay`, `Week`, `Month`, `Timeline` or `Agenda`. Use `@bind-View` to keep track of the user's choice |
| `SchedulerViews` | `RenderFragment?` | | The views the user can choose from (see [Views](#views)). Without any, there are day, week and month views |
| `SchedulerResources` | `RenderFragment?` | | `PixataSchedulerResource` tags (see [Resources and grouping](#resources-and-grouping)) |
| `SchedulerSettings` | `RenderFragment?` | | `PixataSchedulerGroupSettings`, `PixataSchedulerPopupEditSettings` and `PixataSchedulerPopupEditFormSettings` tags |
| `SchedulerToolBar` | `RenderFragment?` | | Replaces the toolbar (see [The toolbar](#the-toolbar)) |
| `ItemTemplate` | `RenderFragment<TItem>?` | | The content of each appointment (see [Templates](#templates)) |
| `AllDayItemTemplate` | `RenderFragment<TItem>?` | | The content of the appointments in the all-day row |
| `AllowCreate`, `AllowUpdate`, `AllowDelete` | `bool` | `false` | What the user can do (see [Editing](#editing)) |
| `ConfirmDelete` | `bool` | `false` | Asks the user to confirm before an appointment is deleted |
| `OnModelInit` | `Func<TItem>?` | | Creates the item for a new appointment. Needed if `TItem` has no parameterless constructor, or to give new appointments default values |
| `ShowWorkHours` | `bool` | `false` | Shows only the working day in the views with time slots. The footer has a button to toggle it; use `@bind-ShowWorkHours` to keep track |
| `WorkDays` | `IEnumerable<DayOfWeek>?` | Monday to Friday | The working days, which have a different background from the rest of the week |
| `Culture` | `CultureInfo?` | the current culture | For day and month names, date and time formats and the first day of the week |
| `FirstDayOfWeek` | `DayOfWeek?` | from the culture | Overrides the culture's first day of the week |
| `Messages` | `PixataSchedulerMessages?` | English | The text the scheduler shows (see [Culture and translation](#culture-and-translation)) |
| `MobileView` | `PixataSchedulerView?` | `Day`, or `Agenda` | The view a phone starts in, instead of a week, multi-day, month or timeline view |
| `MobileBreakpoint` | `int` | `768` | The screen width (in pixels) below which `MobileView` is used. `0` turns this off |
| `Height`, `Width` | `string?` | | CSS sizes. With a height, the views scroll inside the scheduler; without one, the scheduler is as tall as the view |
| `Class`, `Id` | `string?` | | Added to the scheduler's outer element. Other attributes are passed on to it as well |

## Events

| Event | Arguments | Raised when |
| --- | --- | --- |
| `OnCreate` | `PixataSchedulerCreateEventArgs` | The user saves a new appointment. `Item` is the new item; add it to your data. Also raised for the exception that replaces an occurrence the user changed |
| `OnUpdate` | `PixataSchedulerUpdateEventArgs` | The user saves changes to an appointment, or moves or resizes one. `Item` is an updated copy; replace the original (find it by its id). `EditMode` is `Occurrence` when one occurrence of a series was changed or deleted, in which case `Item` is the series with that occurrence added to its exceptions |
| `OnDelete` | `PixataSchedulerDeleteEventArgs` | The user deletes an appointment or a whole series. `Item` is the item to remove. Remove a series' exceptions too |
| `OnEdit` | `PixataSchedulerEditEventArgs` | The edit form is about to open. `Item` is the copy being edited (or the new item), `IsNew`, `EditMode`, `IsAllDay`, `Start` and `End` say what is being edited. Set `IsCancelled` to stop the form opening, eg to show your own |
| `OnCancel` | `PixataSchedulerCancelEventArgs` | The user closes the edit form without saving |
| `OnItemClick`, `OnItemDoubleClick`, `OnItemContextMenu` | `PixataSchedulerItemClickEventArgs` and so on | The user clicks, double-clicks or right-clicks (long-presses) an appointment. `Item` is the appointment (for an occurrence, a copy of the series with the occurrence's times) and `EventArgs` the browser's event. With an `OnItemContextMenu` handler, the browser's own menu isn't shown |
| `OnItemRender` | `Action<PixataSchedulerItemRenderEventArgs>` | Each appointment is rendered. Set `Class` to add CSS classes to it |
| `OnCellRender` | `Action<PixataSchedulerCellRenderEventArgs>` | Each slot is rendered. `Start`, `End`, `IsAllDay` and `Resources` say which slot it is; set `Class` to add CSS classes |
| `DateChanged`, `ViewChanged`, `ShowWorkHoursChanged` | | The user navigates, changes the view, or toggles the working hours |

The items in the events are `object`; cast them to your type. Double-clicking an appointment raises `OnItemDoubleClick` and then (if `AllowUpdate` is set) opens the edit form.

## Methods

| Method | Description |
| --- | --- |
| `Rebind()` | Reads the data again. Call it when you change the list you passed in place (eg `_appointments.Add(...)`), as the scheduler can't see that |
| `Refresh()` | Renders the scheduler again |

Get hold of the scheduler with `@ref`.

## Views

Put the views the user can choose from in `SchedulerViews`. They appear in the toolbar in the order you declare them.

| View | Shows |
| --- | --- |
| `PixataSchedulerDayView` | A day, with the hours down the side and an all-day row |
| `PixataSchedulerMultiDayView` | `NumberOfDays` days (1 by default) side by side, starting on the date |
| `PixataSchedulerWeekView` | A week, starting on the first day of the week |
| `PixataSchedulerMonthView` | Six weeks of day cells, with up to `ItemsPerSlot` appointments in each day (2 by default) and a "3 more" button that goes to the day view |
| `PixataSchedulerTimelineView` | `NumberOfDays` days (1 by default) laid out horizontally, with a lane for each resource when grouped vertically. `ColumnWidth` sets the width (in pixels) of each column |
| `PixataSchedulerAgendaView` | A list of the appointments of `NumberOfDays` days (7 by default), each listed on the first day it covers. `HideEmptyAgendaDays` (true by default) leaves out days without any |

The views with time slots (day, multi-day, week and timeline) also have these parameters. Only the time parts of the `DateTime` values are used.

| Parameter | Default | Description |
| --- | --- | --- |
| `StartTime`, `EndTime` | midnight, midnight | The hours shown. An end time of midnight means the end of the day |
| `WorkDayStart`, `WorkDayEnd` | 08:00, 17:00 | The working day, shown with a different background. The view scrolls to the start of it when it is shown, and only shows it when `ShowWorkHours` is set |
| `SlotDuration` | 60 | The length of each slot, in minutes |
| `SlotDivisions` | 2 | How many divisions each slot is split into |

The day, multi-day and week views have `HideAllDayRow`, which puts all-day appointments in the time slots instead.

Overlapping appointments are shown side by side. Appointments that carry on beyond what the view shows have a small arrow at that end.

## Editing

With `AllowCreate`, the user adds an appointment by double-clicking (or double-tapping) a slot, selecting one with the arrow keys and pressing Enter, or with the toolbar's New event button. With `AllowUpdate`, double-clicking an appointment (or pressing Enter on it) opens it in the edit form, and appointments can be dragged. With `AllowDelete`, appointments have a delete button (shown on hover or when selected), the Delete key deletes the selected appointment, and the edit form has a Delete button.

The built-in form has the title, start and end (with date and time inputs, or date inputs for an all-day appointment), the all-day check box, the recurrence editor, the description, and a drop-down for each resource. Fields your appointments don't have are left out. It checks that there is a title and that the end is after the start, and checks again as the user changes the fields once they've tried to save. If your class has data annotations (`[Required]` and so on), use a form template to have them checked.

### Changing the popup

`PixataSchedulerPopupEditSettings` (in `SchedulerSettings`) sets the dialog's `Title`, `Class`, `Width`, `Height`, `MinWidth`, `MaxWidth`, `MinHeight` and `MaxHeight`. The dialog is modal: Esc closes it, and the focus stays inside it. On a phone it fills the width of the screen.

### Your own form

`PixataSchedulerPopupEditFormSettings` replaces the form's fields (`FormTemplate`) and its buttons (`ButtonsTemplate`). Both get a `PixataSchedulerEditFormContext<TItem>`, with the copy of the appointment being edited (`Item`), `IsNew`, `EditMode`, the form's `EditContext`, `CanDelete`, and the `Save()`, `Cancel()` and `Delete()` methods. The form template is inside the dialog's `EditForm`, so the Blazor input components work in it, and a submit button saves. Data annotations on your class are checked, along with the title and the end.

```xml
<SchedulerSettings>
  <PixataSchedulerPopupEditSettings Title="Booking" Width="30rem" />
  <PixataSchedulerPopupEditFormSettings>
    <FormTemplate>
      <label>Title <InputText @bind-Value="context.Item.Title" /></label>
      <label>Starts <InputDate Type="InputDateType.DateTimeLocal" @bind-Value="context.Item.Start" /></label>
      <label>Ends <InputDate Type="InputDateType.DateTimeLocal" @bind-Value="context.Item.End" /></label>
      <PixataSchedulerRecurrenceEditor @bind-Rule="context.Item.RecurrenceRule" Start="context.Item.Start" />
    </FormTemplate>
  </PixataSchedulerPopupEditFormSettings>
</SchedulerSettings>
```

To use a form of your own outside the scheduler, handle `OnEdit`, set `IsCancelled`, and show your form for `args.Item`.

## Recurring appointments

An appointment with a recurrence rule repeats. The scheduler supports these parts of iCalendar (RFC 5545) rules:

| Part | Example | Meaning |
| --- | --- | --- |
| `FREQ` | `FREQ=WEEKLY` | `DAILY`, `WEEKLY`, `MONTHLY` or `YEARLY` |
| `INTERVAL` | `INTERVAL=2` | Every second day, week, month or year |
| `COUNT` | `COUNT=10` | Ten occurrences in all, including the first |
| `UNTIL` | `UNTIL=20261231T235959` | No occurrences start after this. A date on its own means the end of that day |
| `BYDAY` | `BYDAY=MO,WE` or `BYDAY=-1FR` | On these days, optionally with an ordinal: `2TU` is the second Tuesday, `-1FR` the last Friday |
| `BYMONTHDAY` | `BYMONTHDAY=14` or `BYMONTHDAY=-1` | On these days of the month (negative numbers count back from the end) |
| `BYMONTH` | `BYMONTH=3,9` | In these months |
| `BYSETPOS` | `BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1` | Picks from each month's matches: this one is the last weekday of the month |
| `WKST` | `WKST=SU` | The day weeks start on, which matters for weekly rules with an interval |

A leading `RRULE:` is allowed. Every occurrence starts at the time of day of the series' first appointment, and lasts as long. Rules the scheduler can't read are ignored, so the appointment shows once. `PixataRecurrenceRule` parses and writes rules, and works out occurrences, if you need to do that in your own code:

```csharp
PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=-1FR");
List<DateTime> lunches = rule.GetOccurrences(firstLunch, from, to);
```

When the user edits, moves, resizes or deletes an occurrence, the scheduler asks whether they mean that occurrence or the whole series.

- **Changing one occurrence** raises `OnCreate` for a new appointment that replaces it (a copy of the series with the new details, no recurrence rule, and its recurrence id set to the series' id), and `OnUpdate` (with `EditMode` set to `Occurrence`) for the series with the occurrence's original start added to its exceptions.
- **Deleting one occurrence** raises `OnUpdate` for the series with the occurrence added to its exceptions.
- **Changing the series** raises `OnUpdate` for the series. Moving the series moves its exceptions with it.
- **Deleting the series** raises `OnDelete` for the series. Delete its exceptions (the appointments whose recurrence id is the series' id) in your handler.

Occurrences have a repeat icon, and exceptions a crossed-out one. The edit form only shows the recurrence editor for a series (or a new appointment), not for a single occurrence.

`PixataSchedulerRecurrenceEditor` is the recurrence editor from the built-in form, which you can use in your own form with `@bind-Rule`. It takes the series' `Start`, and optionally `Culture`, `FirstDayOfWeek` and `Messages`.

## Dragging and resizing

With `AllowUpdate`, the user can drag an appointment to another slot, day or resource, and drag its edges to change its start or end. Esc cancels a drag, and the view scrolls when the pointer nears its edge. Dragging an appointment from the time slots to the all-day row makes it an all-day appointment, and the other way round. On a touch screen, the user taps an appointment to select it before dragging it, so that swiping over the scheduler still scrolls it. Read-only appointments can't be dragged. Every drag ends in `OnUpdate` (or, for an occurrence, whatever the user chooses).

## Resources and grouping

Resources are things appointments are linked to, such as rooms or people. Each `PixataSchedulerResource` names the appointment field that holds its value, and lists the possible values:

```xml
<SchedulerResources>
  <PixataSchedulerResource Field="RoomId" Title="Room" Data="@_rooms" />
</SchedulerResources>
<SchedulerSettings>
  <PixataSchedulerGroupSettings Resources="@_groupBy" Orientation="PixataSchedulerGroupOrientation.Horizontal" />
</SchedulerSettings>

@code {
  private readonly List<string> _groupBy = ["RoomId"];
}
```

| Parameter | Default | Description |
| --- | --- | --- |
| `Field` | | The appointment field that holds the resource's value |
| `Title` | the field | The label in the edit form |
| `Data` | | The resource values |
| `TextField`, `ValueField`, `ColorField` | `Text`, `Value`, `Color` | The fields of each value that hold its text, the value stored in the appointment, and its colour (any CSS colour) |
| `Icon` | | An icon next to the label in the edit form |

Appointments take the colour of the first resource they match. The edit form has a drop-down for each resource.

`PixataSchedulerGroupSettings` groups the views by one or more resources (every combination of their values, in order). `Horizontal` puts the groups side by side, with a header for each; `Vertical` puts them one under another (and gives each a lane in the timeline view). A new appointment gets the resource values of the group it was created in, and dragging an appointment to another group changes them.

## Templates

| Template | Where | Context |
| --- | --- | --- |
| `ItemTemplate` | the scheduler, or a view (which overrides the scheduler's) | the appointment (for an occurrence, a copy of the series with its times) |
| `AllDayItemTemplate` | the scheduler, or the day, multi-day and week views | the appointment. Without one, all-day appointments show their title |
| `DateHeaderTemplate` | a view | the date |
| `SlotTemplate` | a view | `PixataSchedulerSlotTemplateContext`: `Start`, `End` and `Resources` |
| `AllDaySlotTemplate` | the day, multi-day and week views | `PixataSchedulerAllDaySlotTemplateContext` |
| `SchedulerResourceGroupHeaderTemplate` | a view | `PixataSchedulerResourceGroupHeaderTemplateContext`: `Text`, `Value` and `Field` |

```xml
<PixataScheduler Data="@_appointments" OnItemRender="@ItemRender">
  <ItemTemplate>
    <strong>@context.Title</strong>
    <div>@context.Description</div>
  </ItemTemplate>
</PixataScheduler>

@code {
  private void ItemRender(PixataSchedulerItemRenderEventArgs args) =>
    args.Class = ((Appointment)args.Item).Title.Contains("urgent") ? "urgent" : null;
}
```

## The toolbar

By default the toolbar has a New event button (when `AllowCreate` is set), Today, previous and next buttons, the date range (which opens a date picker), and the view switcher, which becomes a drop-down list when the scheduler is narrow. To change it, put the tools you want in `SchedulerToolBar`:

| Tool | Parameters |
| --- | --- |
| `PixataSchedulerToolBarNewEventTool` | `ThemeColor` (primary by default), `Icon`, `Enabled`, `Class` |
| `PixataSchedulerToolBarNavigationTool` | |
| `PixataSchedulerToolBarCalendarTool` | |
| `PixataSchedulerToolBarSpacerTool` | |
| `PixataSchedulerToolBarViewsTool` | |
| `PixataSchedulerToolBarCustomTool` | `ChildContent`, `Class` |

```xml
<SchedulerToolBar>
  <PixataSchedulerToolBarNavigationTool />
  <PixataSchedulerToolBarCalendarTool />
  <PixataSchedulerToolBarSpacerTool />
  <PixataSchedulerToolBarCustomTool>
    <button type="button" @onclick="Print">Print</button>
  </PixataSchedulerToolBarCustomTool>
  <PixataSchedulerToolBarViewsTool />
</SchedulerToolBar>
```

The date picker uses the browser's own date input for now.

## Culture and translation

Day and month names, date and time formats, and the first day of the week come from `Culture` (the current culture by default). All the other text comes from `Messages`: create a `PixataSchedulerMessages`, change the properties you need, and pass it in. Properties ending in `Format` are format strings, eg `MoreEventsFormat` is `"{0} more"`.

```csharp
private readonly PixataSchedulerMessages _french = new() { Today = "Aujourd'hui", NewEvent = "Nouveau", MoreEventsFormat = "{0} de plus" };
```

Right-to-left pages are supported.

## Keyboard and accessibility

The toolbar, views and appointments can be reached with Tab. The view itself has these keys:

| Key | In the view | On an appointment |
| --- | --- | --- |
| Page Up, Page Down | The previous or next day, week or month | |
| T | Today | |
| Arrow keys | Select a slot | Select the previous or next appointment |
| Enter | Add an appointment in the selected slot | Edit the appointment (F2 too) |
| Delete | | Delete the appointment |
| Space | | Select the appointment |
| Esc | Clear the selection | Go back to the view |

Appointments are buttons, labelled with their title and times, and only the selected one (or the first) is a tab stop. The dialogs are modal (`role="dialog"` and `role="alertdialog"`), keep the focus inside them, close with Esc, and put the focus back in the scheduler when they close. Changes of view and date are announced to screen readers.

## Phones

On a screen narrower than `MobileBreakpoint`, a scheduler that would start in a week, multi-day, month or timeline view starts in `MobileView` instead (the day view, or the agenda view if there's no day view). The user can still switch views. The toolbar's view switcher becomes a drop-down list, day names are shortened, the edit form is a full-width sheet, and the slots and handles are bigger for fingers.

## Styling

The scheduler's styles use these CSS variables, which you can set on `:root` or on one scheduler (eg through its `Class`). They default to the general `--pixata-blazor-*` theme variables.

| Variable | Default | Description |
| --- | --- | --- |
| `--pixata-blazor-scheduler-bg` | surface background | The scheduler's background |
| `--pixata-blazor-scheduler-border-color` | border colour | Grid lines and borders |
| `--pixata-blazor-scheduler-head-bg` | surface background | The headers |
| `--pixata-blazor-scheduler-nonwork-bg` | base background | Outside the working day |
| `--pixata-blazor-scheduler-selected-slot-bg` | primary, 12% | The selected slot, and the slot an appointment is dragged over |
| `--pixata-blazor-scheduler-today-color` | primary | Today's date |
| `--pixata-blazor-scheduler-event-bg`, `--pixata-blazor-scheduler-event-color` | primary | Appointments without a resource colour |
| `--pixata-blazor-scheduler-event-selected-overlay` | `rgba(0, 0, 0, .14)` | Darkens the selected appointment |
| `--pixata-blazor-scheduler-slot-height` | `2.375rem` | The height of a slot division |
| `--pixata-blazor-scheduler-side-width` | `4.5rem` | The column of times (or resources) |
| `--pixata-blazor-scheduler-column-min-width` | `3.5rem` | The narrowest a day column gets before the view scrolls sideways |
| `--pixata-blazor-scheduler-event-gutter` | `.875rem` | The space left beside appointments in a day column, for clicking the slots |
| `--pixata-blazor-scheduler-bar-height`, `--pixata-blazor-scheduler-bar-gap` | `1.5rem`, `2px` | Appointments in the all-day row, month and timeline views |
| `--pixata-blazor-scheduler-month-day-height` | `6rem` | A day in the month view |
| `--pixata-blazor-scheduler-month-column-min-width` | `2.75rem` | The narrowest a month view column gets |
| `--pixata-blazor-scheduler-timeline-column-width` | `6.25rem` | A column of the timeline view, when `ColumnWidth` isn't set |
| `--pixata-blazor-scheduler-timeline-lane-height` | `4.5rem` | The smallest height of a timeline lane |
| `--pixata-blazor-scheduler-dialog-width` | `40rem` | The edit form |

The main CSS classes are `pixata-blazor-scheduler` (the outer element), `-toolbar`, `-content` (with `-day-view`, `-week-view` and so on), `-footer`, `-event` (with `-event-vertical`, `-event-horizontal` or `-event-agenda`, and `-event-read-only`), `-slot` (with `-nonwork`), `-today`, `-all-day-row`, `-dialog`, `-edit-dialog`, `-prompt`, `-form` and `-recurrence-editor`, all prefixed with `pixata-blazor-scheduler`. The selected appointment or slot has `pixata-blazor-selected`.
