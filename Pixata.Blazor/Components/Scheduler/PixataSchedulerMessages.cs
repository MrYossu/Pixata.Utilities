namespace Pixata.Blazor.Components;

/// <summary>
/// The text the scheduler shows, so it can be translated. Pass your own instance to the scheduler's <c>Messages</c>
/// parameter, changing the properties you need. Day and month names come from the scheduler's culture, not from here.
/// Properties ending in <c>Format</c> are composite format strings
/// </summary>
public class PixataSchedulerMessages {
  // Toolbar and footer
  public string Today { get; set; } = "Today";
  public string Previous { get; set; } = "Previous";
  public string Next { get; set; } = "Next";
  public string NewEvent { get; set; } = "New event";
  public string ChooseDate { get; set; } = "Choose a date";
  public string SelectView { get; set; } = "Select a view";
  public string ToolBar { get; set; } = "Scheduler toolbar";
  public string ShowWorkHours { get; set; } = "Show business hours";
  public string ShowFullDay { get; set; } = "Show full day";

  // View names
  public string DayView { get; set; } = "Day";
  public string MultiDayView { get; set; } = "Multi-day";
  public string WeekView { get; set; } = "Week";
  public string MonthView { get; set; } = "Month";
  public string TimelineView { get; set; } = "Timeline";
  public string AgendaView { get; set; } = "Agenda";

  // Views
  public string AllDay { get; set; } = "all day";
  public string AllEvents { get; set; } = "All events";
  public string MoreEventsFormat { get; set; } = "{0} more";
  public string NoEvents { get; set; } = "No events";
  public string Date { get; set; } = "Date";
  public string Time { get; set; } = "Time";
  public string Event { get; set; } = "Event";
  public string Recurring { get; set; } = "Recurring";
  public string RecurrenceException { get; set; } = "Changed occurrence";
  public string DeleteEvent { get; set; } = "Delete";

  /// <summary>
  /// The screen reader label of an appointment: {0} is the title, {1} the start and {2} the end
  /// </summary>
  public string EventLabelFormat { get; set; } = "{0}, starts at {1}, ends at {2}";

  // Edit form
  public string CreateTitle { get; set; } = "Add event";
  public string EditTitle { get; set; } = "Edit event";
  public string Title { get; set; } = "Title";
  public string Start { get; set; } = "Start";
  public string End { get; set; } = "End";
  public string AllDayEvent { get; set; } = "All day event";
  public string Description { get; set; } = "Description";
  public string Repeat { get; set; } = "Repeat";
  public string Save { get; set; } = "Save";
  public string Cancel { get; set; } = "Cancel";
  public string Delete { get; set; } = "Delete";
  public string Close { get; set; } = "Close";
  public string NoResource { get; set; } = "None";
  public string TitleRequired { get; set; } = "The title is required";
  public string EndBeforeStart { get; set; } = "The end must be after the start";

  // Prompts
  public string DeleteTitle { get; set; } = "Delete event";
  public string DeleteConfirmation { get; set; } = "Are you sure you want to delete this event?";
  public string EditRecurringTitle { get; set; } = "Edit recurring item";
  public string EditRecurringText { get; set; } = "Do you want to edit only this occurrence or the whole series?";
  public string EditOccurrence { get; set; } = "Edit this occurrence";
  public string EditSeries { get; set; } = "Edit the series";
  public string DeleteRecurringTitle { get; set; } = "Delete recurring item";
  public string DeleteRecurringText { get; set; } = "Do you want to delete only this occurrence or the whole series?";
  public string DeleteOccurrence { get; set; } = "Delete this occurrence";
  public string DeleteSeries { get; set; } = "Delete the series";
  public string MoveRecurringTitle { get; set; } = "Move recurring item";
  public string MoveRecurringText { get; set; } = "Do you want to move only this occurrence or the whole series?";
  public string MoveOccurrence { get; set; } = "Move this occurrence";
  public string MoveSeries { get; set; } = "Move the series";

  // Recurrence editor
  public string RepeatNever { get; set; } = "Never";
  public string RepeatDaily { get; set; } = "Daily";
  public string RepeatWeekly { get; set; } = "Weekly";
  public string RepeatMonthly { get; set; } = "Monthly";
  public string RepeatYearly { get; set; } = "Yearly";
  public string RepeatEvery { get; set; } = "Repeat every";
  public string Days { get; set; } = "day(s)";
  public string Weeks { get; set; } = "week(s)";
  public string Months { get; set; } = "month(s)";
  public string Years { get; set; } = "year(s)";
  public string RepeatOn { get; set; } = "Repeat on";
  public string OnDay { get; set; } = "Day";
  public string OnThe { get; set; } = "The";
  public string Of { get; set; } = "of";
  public string First { get; set; } = "First";
  public string Second { get; set; } = "Second";
  public string Third { get; set; } = "Third";
  public string Fourth { get; set; } = "Fourth";
  public string Last { get; set; } = "Last";
  public string AnyDay { get; set; } = "Day";
  public string Weekday { get; set; } = "Weekday";
  public string WeekendDay { get; set; } = "Weekend day";
  public string Ends { get; set; } = "End";
  public string EndsNever { get; set; } = "Never";
  public string EndsAfter { get; set; } = "After";
  public string Occurrences { get; set; } = "occurrence(s)";
  public string EndsOn { get; set; } = "On";

  // Recurrence descriptions, eg "Every 2 weeks on Monday, Wednesday, until 31 December 2026"
  public string EveryDay { get; set; } = "Every day";
  public string EveryWeek { get; set; } = "Every week";
  public string EveryMonth { get; set; } = "Every month";
  public string EveryYear { get; set; } = "Every year";
  public string EveryDaysFormat { get; set; } = "Every {0} days";
  public string EveryWeeksFormat { get; set; } = "Every {0} weeks";
  public string EveryMonthsFormat { get; set; } = "Every {0} months";
  public string EveryYearsFormat { get; set; } = "Every {0} years";
  public string OnDaysFormat { get; set; } = "on {0}";
  public string OnMonthDayFormat { get; set; } = "on day {0}";

  /// <summary>
  /// {0} is the ordinal (eg "second", "last") and {1} the day (eg "Tuesday", "weekday")
  /// </summary>
  public string OnTheFormat { get; set; } = "on the {0} {1}";
  public string InMonthsFormat { get; set; } = "in {0}";
  public string CountFormat { get; set; } = "{0} times";
  public string UntilFormat { get; set; } = "until {0}";
}
