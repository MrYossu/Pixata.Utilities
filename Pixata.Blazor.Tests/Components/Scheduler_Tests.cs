using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

/// <summary>
/// PixataScheduler as a whole: rendering the views, navigating, the keyboard, editing (including recurring appointments),
/// dragging, grouping, templates, render events and culture
/// </summary>
[TestClass]
public class Scheduler_Tests {
  // Tuesday 13 October 2026. With en-GB, weeks start on Monday 12 October
  private static readonly DateTime Tuesday = new(2026, 10, 13);
  private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

  public class Meeting {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public string? RecurrenceRule { get; set; }
    public List<DateTime>? RecurrenceExceptions { get; set; }
    public int? RecurrenceId { get; set; }
    public bool ReadOnly { get; set; }
    public string Room { get; set; } = "";
  }

  public record Room(string Text, string Value, string Color);

  private static readonly List<object> Rooms = [new Room("Room A", "A", "red"), new Room("Room B", "B", "blue")];

  private static Meeting Board => new() { Id = 1, Title = "Board meeting", Start = Tuesday.AddDays(1).AddHours(10), End = Tuesday.AddDays(1).AddHours(11), Room = "A" };

  private static Meeting Run => new() { Id = 2, Title = "Run", Start = Tuesday.AddDays(-1).AddHours(7), End = Tuesday.AddDays(-1).AddHours(7.5), RecurrenceRule = "FREQ=DAILY" };

  // Builds a render fragment holding a component, eg one of the scheduler's views
  private static RenderFragment Component<TComponent>(params (string Name, object? Value)[] parameters) where TComponent : IComponent =>
    builder => {
      builder.OpenComponent<TComponent>(0);
      foreach ((string name, object? value) in parameters) {
        builder.AddAttribute(1, name, value);
      }
      builder.CloseComponent();
    };

  private static RenderFragment Fragments(params RenderFragment[] fragments) =>
    builder => {
      foreach (RenderFragment fragment in fragments) {
        builder.AddContent(0, fragment);
      }
    };

  private static RenderFragment AllViews =>
    Fragments(
      Component<PixataSchedulerDayView<Meeting>>(),
      Component<PixataSchedulerWeekView<Meeting>>(),
      Component<PixataSchedulerMonthView<Meeting>>(),
      Component<PixataSchedulerTimelineView<Meeting>>(),
      Component<PixataSchedulerAgendaView<Meeting>>());

  private static IRenderedComponent<PixataScheduler<Meeting>> Render(BunitContext context, List<Meeting> data,
    Action<ComponentParameterCollectionBuilder<PixataScheduler<Meeting>>>? parameters = null, PixataSchedulerView view = PixataSchedulerView.Week,
    CultureInfo? culture = null) =>
    context.Render<PixataScheduler<Meeting>>(p => {
      p.Add(s => s.Data, data)
        .Add(s => s.Date, Tuesday)
        .Add(s => s.View, view)
        .Add(s => s.Culture, culture ?? English)
        .Add(s => s.SchedulerViews, AllViews);
      parameters?.Invoke(p);
    });

  private static IElement Event(IRenderedComponent<PixataScheduler<Meeting>> cut, string title) =>
    cut.Find($".pixata-blazor-scheduler-event[aria-label^='{title},']");

  private static IReadOnlyList<IElement> Events(IRenderedComponent<PixataScheduler<Meeting>> cut, string title) =>
    cut.FindAll($".pixata-blazor-scheduler-event[aria-label^='{title},']");

  private static IElement Slot(IRenderedComponent<PixataScheduler<Meeting>> cut, DateTime start) =>
    cut.Find($".pixata-blazor-scheduler-slot[data-pixata-slot-start='{start.ToString("s", CultureInfo.InvariantCulture)}']:not([data-pixata-slot-all-day='true'])");

  private static IElement Button(IRenderedComponent<PixataScheduler<Meeting>> cut, string text) =>
    cut.FindAll("button").First(b => b.TextContent.Trim() == text);

  #region Rendering

  [TestMethod]
  public void WithoutViews_ShowsTheDayWeekAndMonthViews() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = context.Render<PixataScheduler<Meeting>>(p => p.Add(s => s.Data, [Board]).Add(s => s.Date, Tuesday));
    CollectionAssert.AreEqual(new[] { "Day", "Week", "Month" }, cut.FindAll(".pixata-blazor-scheduler-view-button").Select(b => b.TextContent).ToArray());
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-day-view"));
  }

  [TestMethod]
  public void WeekView_PutsAppointmentsInTheirDayColumns() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board]);
    IReadOnlyList<IElement> columns = cut.FindAll(".pixata-blazor-scheduler-day-column");
    Assert.AreEqual(7, columns.Count);
    Assert.IsNotNull(columns[2].QuerySelector(".pixata-blazor-scheduler-event"));
    StringAssert.Contains(Event(cut, "Board meeting").GetAttribute("style"), "--pixata-blazor-scheduler-event-top:41.6667%");
    Assert.AreEqual("12 October - 18 October", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
  }

  [TestMethod]
  public void AllDayAppointments_GoInTheAllDayRow_IncludingTheirEndDate() {
    using BunitContext context = PixataTestContext.Create();
    Meeting trip = new() { Id = 3, Title = "Trip", Start = Tuesday, End = Tuesday.AddDays(3), IsAllDay = true };
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [trip]);
    IElement bar = cut.Find(".pixata-blazor-scheduler-all-day-row .pixata-blazor-scheduler-event");
    // Tuesday to Friday is four of the week's seven days, starting at the second
    StringAssert.Contains(bar.GetAttribute("style"), "--pixata-blazor-scheduler-event-start:14.2857%;--pixata-blazor-scheduler-event-width:57.1429%");
  }

  [TestMethod]
  public void EachView_Renders() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board, Run]);
    foreach ((string view, string selector) in new[] {
      ("Day", ".pixata-blazor-scheduler-time-grid"), ("Month", ".pixata-blazor-scheduler-month-grid"),
      ("Timeline", ".pixata-blazor-scheduler-timeline-grid"), ("Agenda", ".pixata-blazor-scheduler-agenda")
    }) {
      Button(cut, view).Click();
      Assert.IsNotNull(cut.Find(selector), view);
      Assert.AreEqual("true", cut.FindAll(".pixata-blazor-scheduler-view-button").Single(b => b.TextContent == view).GetAttribute("aria-pressed"));
    }
  }

  [TestMethod]
  public void Aria_TheToolbarAppointmentsAndOneTabStop() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board, Run]);
    Assert.AreEqual("application", cut.Find(".pixata-blazor-scheduler").GetAttribute("role"));
    Assert.AreEqual("toolbar", cut.Find(".pixata-blazor-scheduler-toolbar").GetAttribute("role"));
    IReadOnlyList<IElement> events = cut.FindAll(".pixata-blazor-scheduler-event");
    Assert.IsTrue(events.All(e => e.GetAttribute("role") == "button"));
    Assert.AreEqual(1, events.Count(e => e.GetAttribute("tabindex") == "0"));
    string label = Event(cut, "Board meeting").GetAttribute("aria-label")!;
    StringAssert.StartsWith(label, "Board meeting, starts at ");
    StringAssert.Contains(label, "10:00, ends at ");
  }

  [TestMethod]
  public void MissingTitleField_SaysWhichParameterToSet() {
    using BunitContext context = PixataTestContext.Create();
    InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => Render(context, [Board], p => p.Add(s => s.TitleField, "Subject")));
    StringAssert.Contains(exception.Message, "TitleField");
  }

  #endregion

  #region Navigation and the keyboard

  [TestMethod]
  public void NextAndPrevious_MoveByTheView() {
    using BunitContext context = PixataTestContext.Create();
    List<DateTime> dates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p.Add(s => s.DateChanged, date => dates.Add(date)));
    cut.Find(".pixata-blazor-scheduler-next").Click();
    Assert.AreEqual("19 October - 25 October", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
    cut.Find(".pixata-blazor-scheduler-previous").Click();
    cut.Find(".pixata-blazor-scheduler-previous").Click();
    CollectionAssert.AreEqual(new[] { Tuesday.AddDays(7), Tuesday, Tuesday.AddDays(-7) }, dates);
  }

  [TestMethod]
  public void ViewButtons_RaiseViewChanged() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerView> views = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p.Add(s => s.ViewChanged, view => views.Add(view)));
    Button(cut, "Month").Click();
    cut.Find(".pixata-blazor-scheduler-views-select").Change("Agenda");
    CollectionAssert.AreEqual(new[] { PixataSchedulerView.Month, PixataSchedulerView.Agenda }, views);
  }

  [TestMethod]
  public void DateHeader_DrillsDownToTheDay() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board]);
    cut.FindAll(".pixata-blazor-scheduler-date-link")[2].Click();
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-day-view"));
    Assert.AreEqual("14 October", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
  }

  [TestMethod]
  public void Keyboard_PagesAndGoesToToday() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board]);
    IElement content = cut.Find(".pixata-blazor-scheduler-content");
    content.KeyDown(new KeyboardEventArgs { Key = "PageDown" });
    Assert.AreEqual("19 October - 25 October", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
    cut.Find(".pixata-blazor-scheduler-content").KeyDown(new KeyboardEventArgs { Key = "t" });
    DateTime monday = PixataSchedulerDates.StartOfWeek(DateTime.Today, DayOfWeek.Monday);
    Assert.AreEqual($"{monday.ToString("d MMMM", English)} - {monday.AddDays(6).ToString("d MMMM", English)}", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
  }

  [TestMethod]
  public void Keyboard_ArrowsSelectSlotsAndEnterCreates() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerEditEventArgs> edits = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [], p => p.Add(s => s.AllowCreate, true).Add(s => s.OnEdit, args => edits.Add(args)));
    cut.Find(".pixata-blazor-scheduler-content").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    cut.Find(".pixata-blazor-scheduler-content").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    cut.Find(".pixata-blazor-scheduler-content").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Assert.AreEqual("2026-10-13T00:30:00", cut.Find(".pixata-blazor-scheduler-slot.pixata-blazor-selected").GetAttribute("data-pixata-slot-start"));
    cut.Find(".pixata-blazor-scheduler-content").KeyDown(new KeyboardEventArgs { Key = "Enter" });
    Assert.AreEqual((true, Tuesday.AddMinutes(30)), (edits.Single().IsNew, edits.Single().Start));
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-edit-dialog"));
  }

  // Ids can hold characters that would break the selector used to focus an appointment
  [TestMethod]
  public void KeySelector_EscapesQuotesAndBackslashes() =>
    Assert.AreEqual("[data-pixata-key=\"a\\\"b\\\\c\"]", PixataScheduler<Meeting>.KeySelector("a\"b\\c"));

  [TestMethod]
  public void Keyboard_ArrowsMoveBetweenAppointments() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board, new() { Id = 5, Title = "Lunch", Start = Tuesday.AddDays(1).AddHours(12), End = Tuesday.AddDays(1).AddHours(13) }]);
    Event(cut, "Board meeting").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
    Assert.IsTrue(Event(cut, "Lunch").ClassList.Contains("pixata-blazor-selected"));
    Assert.AreEqual("0", Event(cut, "Lunch").GetAttribute("tabindex"));
  }

  #endregion

  #region Editing

  [TestMethod]
  public void DoubleClickingAnAppointment_EditsACopy() {
    using BunitContext context = PixataTestContext.Create();
    Meeting board = Board;
    List<PixataSchedulerUpdateEventArgs> updates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [board], p => p.Add(s => s.AllowUpdate, true).Add(s => s.OnUpdate, args => updates.Add(args)));
    Event(cut, "Board meeting").DoubleClick();
    Assert.AreEqual("Edit event", cut.Find(".pixata-blazor-scheduler-dialog-title").TextContent);
    Assert.AreEqual("dialog", cut.Find(".pixata-blazor-scheduler-edit-dialog").GetAttribute("role"));
    cut.Find(".pixata-blazor-scheduler-form input").Change("Annual meeting");
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Meeting saved = (Meeting)updates.Single().Item;
    Assert.AreEqual(("Annual meeting", "Board meeting"), (saved.Title, board.Title));
    Assert.AreEqual(PixataSchedulerRecurrenceEditMode.Series, updates.Single().EditMode);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-dialog").Count);
  }

  [TestMethod]
  public void WithoutAllowUpdate_AppointmentsCannotBeEditedOrDragged() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board]);
    Assert.AreEqual("false", Event(cut, "Board meeting").GetAttribute("data-pixata-draggable"));
    Event(cut, "Board meeting").DoubleClick();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-dialog").Count);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-event-delete").Count);
  }

  [TestMethod]
  public void DoubleClickingASlot_CreatesAnAppointment_AfterValidating() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerCreateEventArgs> creates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [], p => p.Add(s => s.AllowCreate, true).Add(s => s.OnCreate, args => creates.Add(args)));
    Slot(cut, Tuesday.AddDays(1).AddHours(10)).Click(new MouseEventArgs { Detail = 2 });
    Assert.AreEqual("Add event", cut.Find(".pixata-blazor-scheduler-dialog-title").TextContent);
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Assert.AreEqual(0, creates.Count);
    Assert.AreEqual("The title is required", cut.Find(".validation-message").TextContent);
    cut.Find(".pixata-blazor-scheduler-form input").Change("Planning");
    Assert.AreEqual(0, cut.FindAll(".validation-message").Count, "Fields are checked again as they change once the user has tried to save");
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Meeting created = (Meeting)creates.Single().Item;
    Assert.AreEqual(("Planning", Tuesday.AddDays(1).AddHours(10), Tuesday.AddDays(1).AddHours(10.5)), (created.Title, created.Start, created.End));
  }

  [TestMethod]
  public void NewAppointmentInTheAllDayRow_IsAllDayAndEndsTheSameDay() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerCreateEventArgs> creates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [], p => p.Add(s => s.AllowCreate, true).Add(s => s.OnCreate, args => creates.Add(args)));
    cut.Find(".pixata-blazor-scheduler-slot[data-pixata-slot-all-day='true'][data-pixata-slot-start='2026-10-14T00:00:00']").Click(new MouseEventArgs { Detail = 2 });
    Assert.AreEqual("date", cut.FindAll(".pixata-blazor-scheduler-form input")[1].GetAttribute("type"));
    cut.Find(".pixata-blazor-scheduler-form input").Change("Holiday");
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Meeting created = (Meeting)creates.Single().Item;
    Assert.AreEqual((true, Tuesday.AddDays(1), Tuesday.AddDays(1)), (created.IsAllDay, created.Start, created.End));
  }

  [TestMethod]
  public void NewEventButton_OnlyWithAllowCreate() {
    using BunitContext context = PixataTestContext.Create();
    Assert.AreEqual(0, Render(context, []).FindAll(".pixata-blazor-scheduler-new-event").Count);
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [], p => p.Add(s => s.AllowCreate, true));
    cut.Find(".pixata-blazor-scheduler-new-event").Click();
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-edit-dialog"));
  }

  [TestMethod]
  public void OnEdit_CanStopTheFormOpening() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p.Add(s => s.AllowUpdate, true).Add(s => s.OnEdit, args => args.IsCancelled = true));
    Event(cut, "Board meeting").DoubleClick();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-dialog").Count);
  }

  [TestMethod]
  public void Escape_ClosesTheFormAndRaisesOnCancel() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerCancelEventArgs> cancels = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p.Add(s => s.AllowUpdate, true).Add(s => s.OnCancel, args => cancels.Add(args)));
    Event(cut, "Board meeting").DoubleClick();
    cut.Find(".pixata-blazor-scheduler-edit-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-dialog").Count);
    Assert.AreEqual(1, cancels.Count);
  }

  [TestMethod]
  public void Delete_WithConfirmation() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerDeleteEventArgs> deletes = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board],
      p => p.Add(s => s.AllowDelete, true).Add(s => s.ConfirmDelete, true).Add(s => s.OnDelete, args => deletes.Add(args)));
    cut.Find(".pixata-blazor-scheduler-event-delete").Click();
    Assert.AreEqual("alertdialog", cut.Find(".pixata-blazor-scheduler-prompt").GetAttribute("role"));
    Button(cut, "Cancel").Click();
    Assert.AreEqual(0, deletes.Count);
    Event(cut, "Board meeting").KeyDown(new KeyboardEventArgs { Key = "Delete" });
    Button(cut, "Delete").Click();
    Assert.AreEqual(1, ((Meeting)deletes.Single().Item).Id);
  }

  [TestMethod]
  public void ReadOnlyAppointments_CannotBeChanged() {
    using BunitContext context = PixataTestContext.Create();
    Meeting drill = Board;
    drill.ReadOnly = true;
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [drill], p => p.Add(s => s.AllowUpdate, true).Add(s => s.AllowDelete, true));
    Assert.IsTrue(Event(cut, "Board meeting").ClassList.Contains("pixata-blazor-scheduler-event-read-only"));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-event-delete").Count);
    Event(cut, "Board meeting").DoubleClick();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-dialog").Count);
  }

  [TestMethod]
  public void FormTemplate_ReplacesTheBuiltInForm() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerUpdateEventArgs> updates = [];
    RenderFragment<PixataSchedulerEditFormContext<Meeting>> template = form => builder => {
      builder.OpenElement(0, "button");
      builder.AddAttribute(1, "class", "custom-save");
      builder.AddAttribute(2, "type", "button");
      builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => {
        form.Item.Title = "From the template";
        return form.Save();
      }));
      builder.CloseElement();
    };
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p
      .Add(s => s.AllowUpdate, true)
      .Add(s => s.OnUpdate, args => updates.Add(args))
      .Add(s => s.SchedulerSettings, Component<PixataSchedulerPopupEditFormSettings<Meeting>>(("FormTemplate", template))));
    Event(cut, "Board meeting").DoubleClick();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-form").Count);
    cut.Find(".custom-save").Click();
    Assert.AreEqual("From the template", ((Meeting)updates.Single().Item).Title);
  }

  #endregion

  #region Recurrence

  [TestMethod]
  public void RecurringAppointments_AreExpanded_LeavingOutExceptions() {
    using BunitContext context = PixataTestContext.Create();
    Meeting run = Run;
    run.RecurrenceExceptions = [Tuesday.AddDays(1).AddHours(7)];
    Meeting late = new() { Id = 3, Title = "Run (late)", Start = Tuesday.AddDays(1).AddHours(8), End = Tuesday.AddDays(1).AddHours(8.5), RecurrenceId = 2 };
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [run, late]);
    Assert.AreEqual(6, Events(cut, "Run").Count);
    Assert.AreEqual(1, Events(cut, "Run (late)").Count);
    Assert.IsNotNull(Events(cut, "Run")[0].QuerySelector(".pixata-blazor-svg-icon-repeat"));
  }

  // An exception doesn't give the series an extra occurrence: COUNT counts the occurrences before exceptions are taken out
  [TestMethod]
  public void Count_IncludesTheExceptions() {
    using BunitContext context = PixataTestContext.Create();
    Meeting run = Run;
    run.RecurrenceRule = "FREQ=DAILY;COUNT=5";
    run.RecurrenceExceptions = [Tuesday.AddHours(7)];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [run]);
    // The occurrences' keys end in their start times' ticks
    CollectionAssert.AreEqual(new[] { 12, 14, 15, 16 },
      Events(cut, "Run").Select(e => new DateTime(long.Parse(e.GetAttribute("data-pixata-key")!.Split('@')[1], CultureInfo.InvariantCulture)).Day).ToArray());
  }

  // Without WKST, "every other week" follows the scheduler's weeks: here they start on Sunday, so the Sunday after the first
  // Tuesday is in the next week, which is skipped
  [TestMethod]
  public void EveryOtherWeek_FollowsTheSchedulersFirstDayOfTheWeek() {
    using BunitContext context = PixataTestContext.Create();
    Meeting run = Run;
    run.Start = Tuesday.AddHours(7);
    run.End = Tuesday.AddHours(7.5);
    run.RecurrenceRule = "FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU";
    IRenderedComponent<PixataScheduler<Meeting>> unitedStates = Render(context, [run], culture: CultureInfo.GetCultureInfo("en-US"));
    unitedStates.Find(".pixata-blazor-scheduler-next").Click();
    Assert.AreEqual(0, Events(unitedStates, "Run").Count, "18 to 24 October is the week that is skipped");
    unitedStates.Find(".pixata-blazor-scheduler-next").Click();
    Assert.AreEqual(2, Events(unitedStates, "Run").Count, "Sunday 25 and Tuesday 27 October");
    IRenderedComponent<PixataScheduler<Meeting>> britain = Render(context, [run]);
    Assert.AreEqual(2, Events(britain, "Run").Count, "Tuesday 13 and Sunday 18 October are in the same week");
  }

  // Series with no start (so on the first day of the calendar): one whose week would start the day before, and one that
  // lasts until today
  [TestMethod]
  public void SeriesStartingOnTheFirstDayOfTheCalendar_DoNotBreakTheScheduler() {
    using BunitContext context = PixataTestContext.Create();
    Meeting run = new() { Id = 2, Title = "Run", End = DateTime.MinValue.AddHours(1), RecurrenceRule = "FREQ=WEEKLY;INTERVAL=2" };
    Meeting untilToday = new() { Id = 3, Title = "Long", End = Tuesday, RecurrenceRule = "FREQ=YEARLY" };
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [run, untilToday], culture: CultureInfo.GetCultureInfo("en-US"));
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-time-grid"));
  }

  [TestMethod]
  public void EditingAnOccurrence_CreatesAnException() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerCreateEventArgs> creates = [];
    List<PixataSchedulerUpdateEventArgs> updates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Run], p => p
      .Add(s => s.AllowUpdate, true)
      .Add(s => s.OnCreate, args => creates.Add(args))
      .Add(s => s.OnUpdate, args => updates.Add(args)));
    Events(cut, "Run")[2].DoubleClick();
    Assert.AreEqual("Edit recurring item", cut.Find(".pixata-blazor-scheduler-prompt .pixata-blazor-scheduler-dialog-title").TextContent);
    Button(cut, "Edit this occurrence").Click();
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-scheduler-recurrence-editor").Count, "An occurrence can't have its own rule");
    cut.Find(".pixata-blazor-scheduler-form input").Change("Long run");
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Meeting exception = (Meeting)creates.Single().Item;
    Assert.AreEqual(("Long run", 2, (string?)null, Tuesday.AddDays(1).AddHours(7)), (exception.Title, exception.RecurrenceId!.Value, exception.RecurrenceRule, exception.Start));
    Meeting series = (Meeting)updates.Single().Item;
    Assert.AreEqual(PixataSchedulerRecurrenceEditMode.Occurrence, updates.Single().EditMode);
    CollectionAssert.AreEqual(new List<DateTime> { Tuesday.AddDays(1).AddHours(7) }, series.RecurrenceExceptions);
  }

  [TestMethod]
  public void EditingTheSeries_ShowsTheRecurrenceEditor() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerUpdateEventArgs> updates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Run], p => p.Add(s => s.AllowUpdate, true).Add(s => s.OnUpdate, args => updates.Add(args)));
    Events(cut, "Run")[0].DoubleClick();
    Button(cut, "Edit the series").Click();
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-recurrence-editor"));
    Button(cut, "Weekly").Click();
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Assert.AreEqual("FREQ=WEEKLY;BYDAY=MO", ((Meeting)updates.Single().Item).RecurrenceRule);
  }

  [TestMethod]
  public void DeletingAnOccurrence_AddsAnException() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerUpdateEventArgs> updates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Run], p => p.Add(s => s.AllowDelete, true).Add(s => s.OnUpdate, args => updates.Add(args)));
    Events(cut, "Run")[1].KeyDown(new KeyboardEventArgs { Key = "Delete" });
    Button(cut, "Delete this occurrence").Click();
    CollectionAssert.AreEqual(new List<DateTime> { Tuesday.AddHours(7) }, ((Meeting)updates.Single().Item).RecurrenceExceptions);
    Assert.AreEqual(PixataSchedulerRecurrenceEditMode.Occurrence, updates.Single().EditMode);
  }

  #endregion

  #region Dragging

  private static PixataScheduler<Meeting>.DropSlot Drop(DateTime start, DateTime end, bool allDay = false, int group = 0) =>
    new() { Start = start.ToString("s", CultureInfo.InvariantCulture), End = end.ToString("s", CultureInfo.InvariantCulture), AllDay = allDay, Group = group };

  [TestMethod]
  public async Task Dragging_MovesAndResizes() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerUpdateEventArgs> updates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p.Add(s => s.AllowUpdate, true).Add(s => s.OnUpdate, args => updates.Add(args)));
    DateTime wednesday = Tuesday.AddDays(1);
    Assert.AreEqual("true", Event(cut, "Board meeting").GetAttribute("data-pixata-draggable"));
    await cut.InvokeAsync(() => cut.Instance.OnDragEnd("1", "move", Drop(wednesday.AddHours(10), wednesday.AddHours(10.5)), Drop(wednesday.AddDays(1).AddHours(14), wednesday.AddDays(1).AddHours(14.5))));
    Meeting moved = (Meeting)updates[0].Item;
    Assert.AreEqual((wednesday.AddDays(1).AddHours(14), wednesday.AddDays(1).AddHours(15)), (moved.Start, moved.End));
    await cut.InvokeAsync(() => cut.Instance.OnDragEnd("1", "end", Drop(wednesday.AddHours(10.5), wednesday.AddHours(11)), Drop(wednesday.AddHours(12), wednesday.AddHours(12.5))));
    Meeting resized = (Meeting)updates[1].Item;
    Assert.AreEqual((wednesday.AddHours(10), wednesday.AddHours(12.5)), (resized.Start, resized.End));
    await cut.InvokeAsync(() => cut.Instance.OnDragEnd("1", "move", Drop(wednesday.AddHours(10), wednesday.AddHours(10.5)), Drop(wednesday.AddDays(2), wednesday.AddDays(3), true)));
    Meeting allDay = (Meeting)updates[2].Item;
    Assert.AreEqual((true, wednesday.AddDays(2), wednesday.AddDays(2)), (allDay.IsAllDay, allDay.Start, allDay.End));
  }

  // In the timeline every slot has a time, but an all-day appointment still ends on a date
  [TestMethod]
  public async Task ResizingAnAllDayAppointmentInTheTimeline_KeepsItAllDay() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerUpdateEventArgs> updates = [];
    Meeting trip = new() { Id = 3, Title = "Trip", Start = Tuesday, End = Tuesday.AddDays(1), IsAllDay = true };
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [trip], p => p
      .Add(s => s.AllowUpdate, true)
      .Add(s => s.OnUpdate, args => updates.Add(args))
      .Add(s => s.SchedulerViews, Component<PixataSchedulerTimelineView<Meeting>>(("NumberOfDays", 7))), PixataSchedulerView.Timeline);
    await cut.InvokeAsync(() => cut.Instance.OnDragEnd("3", "end", Drop(Tuesday.AddDays(1).AddHours(10), Tuesday.AddDays(1).AddHours(10.5)),
      Drop(Tuesday.AddDays(3).AddHours(14), Tuesday.AddDays(3).AddHours(14.5))));
    Meeting resized = (Meeting)updates.Single().Item;
    Assert.AreEqual((Tuesday, Tuesday.AddDays(3), true), (resized.Start, resized.End, resized.IsAllDay));
  }

  [TestMethod]
  public async Task DraggingAnOccurrence_AsksWhetherToMoveTheSeries() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerUpdateEventArgs> updates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Run], p => p.Add(s => s.AllowUpdate, true).Add(s => s.OnUpdate, args => updates.Add(args)));
    string key = Events(cut, "Run")[1].GetAttribute("data-pixata-key")!;
    await cut.InvokeAsync(() => cut.Instance.OnDragEnd(key, "move", Drop(Tuesday.AddHours(7), Tuesday.AddHours(7.5)), Drop(Tuesday.AddHours(9), Tuesday.AddHours(9.5))));
    Button(cut, "Move the series").Click();
    Meeting series = (Meeting)updates.Single().Item;
    Assert.AreEqual((Tuesday.AddDays(-1).AddHours(9), "FREQ=DAILY"), (series.Start, series.RecurrenceRule));
  }

  #endregion

  #region Resources, templates, render events and culture

  [TestMethod]
  public void Resources_ColourAppointmentsAndGroupTheView() {
    using BunitContext context = PixataTestContext.Create();
    Meeting other = new() { Id = 4, Title = "Interview", Start = Board.Start, End = Board.End, Room = "B" };
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board, other], p => p
      .Add(s => s.SchedulerResources, Component<PixataSchedulerResource<Meeting>>(("Field", "Room"), ("Data", Rooms)))
      .Add(s => s.SchedulerSettings, Component<PixataSchedulerGroupSettings<Meeting>>(("Resources", new List<string> { "Room" }))));
    StringAssert.Contains(Event(cut, "Board meeting").GetAttribute("style"), "--pixata-blazor-scheduler-event-bg:red");
    CollectionAssert.AreEqual(new[] { "Room A", "Room B" }, cut.FindAll(".pixata-blazor-scheduler-group-cell").Select(c => c.TextContent.Trim()).ToArray());
    IReadOnlyList<IElement> columns = cut.FindAll(".pixata-blazor-scheduler-day-column");
    Assert.AreEqual(14, columns.Count);
    Assert.IsNotNull(columns[2].QuerySelector("[aria-label^='Board meeting,']"));
    Assert.IsNotNull(columns[9].QuerySelector("[aria-label^='Interview,']"));
  }

  [TestMethod]
  public void NewAppointmentInAGroup_GetsTheGroupsResource() {
    using BunitContext context = PixataTestContext.Create();
    List<PixataSchedulerCreateEventArgs> creates = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [], p => p
      .Add(s => s.AllowCreate, true)
      .Add(s => s.OnCreate, args => creates.Add(args))
      .Add(s => s.SchedulerResources, Component<PixataSchedulerResource<Meeting>>(("Field", "Room"), ("Data", Rooms)))
      .Add(s => s.SchedulerSettings, Component<PixataSchedulerGroupSettings<Meeting>>(("Resources", new List<string> { "Room" }), ("Orientation", PixataSchedulerGroupOrientation.Vertical))));
    cut.Find(".pixata-blazor-scheduler-slot[data-pixata-slot-group='1'][data-pixata-slot-start='2026-10-14T10:00:00']").Click(new MouseEventArgs { Detail = 2 });
    cut.Find(".pixata-blazor-scheduler-form input").Change("Interview");
    cut.Find(".pixata-blazor-scheduler-edit-form").Submit();
    Assert.AreEqual("B", ((Meeting)creates.Single().Item).Room);
  }

  [TestMethod]
  public void Templates_AndRenderEvents() {
    using BunitContext context = PixataTestContext.Create();
    RenderFragment<Meeting> itemTemplate = meeting => builder => {
      builder.OpenElement(0, "strong");
      builder.AddAttribute(1, "class", "custom-item");
      builder.AddContent(2, meeting.Title.ToUpperInvariant());
      builder.CloseElement();
    };
    Action<PixataSchedulerItemRenderEventArgs> itemRender = args => args.Class = ((Meeting)args.Item).Id == 1 ? "important" : null;
    Action<PixataSchedulerCellRenderEventArgs> cellRender = args => args.Class = args.Start.Hour == 12 && !args.IsAllDay ? "lunch" : null;
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p
      .Add(s => s.ItemTemplate, itemTemplate)
      .Add(s => s.OnItemRender, itemRender)
      .Add(s => s.OnCellRender, cellRender));
    Assert.AreEqual("BOARD MEETING", cut.Find(".custom-item").TextContent);
    Assert.IsTrue(Event(cut, "Board meeting").ClassList.Contains("important"));
    Assert.AreEqual(14, cut.FindAll(".pixata-blazor-scheduler-slot.lunch").Count);
  }

  [TestMethod]
  public void ItemClickEvents() {
    using BunitContext context = PixataTestContext.Create();
    List<string> clicks = [];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p
      .Add(s => s.OnItemClick, args => clicks.Add($"click {((Meeting)args.Item).Title}"))
      .Add(s => s.OnItemDoubleClick, args => clicks.Add($"double {((Meeting)args.Item).Title}"))
      .Add(s => s.OnItemContextMenu, args => clicks.Add($"menu {((Meeting)args.Item).Title}")));
    Event(cut, "Board meeting").Click();
    Event(cut, "Board meeting").DoubleClick();
    Event(cut, "Board meeting").ContextMenu();
    CollectionAssert.AreEqual(new[] { "click Board meeting", "double Board meeting", "menu Board meeting" }, clicks);
    Assert.IsTrue(Event(cut, "Board meeting").ClassList.Contains("pixata-blazor-selected"));
  }

  [TestMethod]
  public void Culture_FormatsDatesAndStartsTheWeek() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], null, PixataSchedulerView.Month, CultureInfo.GetCultureInfo("fr-FR"));
    Assert.AreEqual("octobre 2026", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
    Assert.AreEqual("lundi", cut.Find(".pixata-blazor-scheduler-month-grid .pixata-blazor-scheduler-day-name-long").TextContent);
  }

  [TestMethod]
  public void FirstDayOfWeek_OverridesTheCulture() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board], p => p.Add(s => s.FirstDayOfWeek, DayOfWeek.Sunday));
    Assert.AreEqual("11 October - 17 October", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
  }

  [TestMethod]
  public void MonthView_ShowsHowManyMoreAndDrillsDown() {
    using BunitContext context = PixataTestContext.Create();
    List<Meeting> data = [Board, new() { Id = 5, Title = "Lunch", Start = Board.Start.AddHours(2), End = Board.End.AddHours(2) }, new() { Id = 6, Title = "Tea", Start = Board.Start.AddHours(5), End = Board.End.AddHours(5) }];
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, data, view: PixataSchedulerView.Month);
    IElement more = cut.Find(".pixata-blazor-scheduler-more");
    Assert.AreEqual("1 more", more.TextContent.Trim());
    more.Click();
    Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-day-view"));
    Assert.AreEqual("14 October", cut.Find(".pixata-blazor-scheduler-range-text").TextContent);
  }

  [TestMethod]
  public void Agenda_ListsTheAppointments() {
    using BunitContext context = PixataTestContext.Create();
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board, Run], view: PixataSchedulerView.Agenda);
    // The agenda shows a week from the date: Run every day, and the board meeting
    Assert.AreEqual(8, cut.FindAll(".pixata-blazor-scheduler-agenda .pixata-blazor-scheduler-event").Count);
    Assert.AreEqual("10:00 - 11:00", cut.FindAll(".pixata-blazor-scheduler-agenda-time-cell").Select(c => c.TextContent).ElementAt(2));
  }

  [TestMethod]
  public void Phones_StartInTheDayView() {
    using BunitContext context = PixataTestContext.Create();
    context.JSInterop.SetupModule(PixataJs.ModulePath).Setup<bool>("matchesMedia", _ => true).SetResult(true);
    IRenderedComponent<PixataScheduler<Meeting>> cut = Render(context, [Board]);
    cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find(".pixata-blazor-scheduler-day-view")));
  }

  #endregion
}
