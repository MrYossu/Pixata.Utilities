using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

/// <summary>
/// The scheduler's plain C# parts: the date maths, laying out overlapping appointments, working out what each view shows,
/// reading and writing the appointment fields, and the built-in edit form's model
/// </summary>
[TestClass]
public class Scheduler_Engine_Tests {
  private static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");

  // Tuesday 13 October 2026
  private static readonly DateTime Tuesday = new(2026, 10, 13);

  private class Appointment {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public List<DateTime>? RecurrenceExceptions { get; set; }
  }

  private class LongIds {
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? RecurrenceRule { get; set; }
    public long RecurrenceId { get; set; }
  }

  // A time zone like London's: GMT in the winter, and an hour ahead from the last Sunday in March to the last in October
  private static TimeZoneInfo LondonLike() {
    TimeZoneInfo.TransitionTime start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 3, 5, DayOfWeek.Sunday);
    TimeZoneInfo.TransitionTime end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday);
    TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2000, 1, 1), new DateTime(2099, 12, 31), TimeSpan.FromHours(1), start, end);
    return TimeZoneInfo.CreateCustomTimeZone("Test/London", TimeSpan.Zero, "Test London", "GMT", "BST", [rule]);
  }

  private class NoTitle {
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
  }

  private static PixataSchedulerItem<string> Item(string title, DateTime start, DateTime end, bool isAllDay = false, string? room = null) =>
    new() {
      Key = title,
      Item = title,
      DataItem = title,
      Title = title,
      Start = start,
      End = end,
      IsAllDay = isAllDay,
      Resources = room is null ? new Dictionary<string, object?>() : new Dictionary<string, object?> { ["Room"] = room }
    };

  private static List<DateTime> Days(DateTime first, int count) =>
    Enumerable.Range(0, count).Select(d => first.AddDays(d)).ToList();

  private static List<PixataSchedulerGroup> NoGroups =>
    PixataSchedulerModelBuilder.Groups([]);

  #region Dates

  [TestMethod]
  public void StartOfWeek_UsesTheFirstDayGiven() {
    Assert.AreEqual(new DateTime(2026, 10, 12), PixataSchedulerDates.StartOfWeek(Tuesday.AddHours(15), DayOfWeek.Monday));
    Assert.AreEqual(new DateTime(2026, 10, 11), PixataSchedulerDates.StartOfWeek(Tuesday, DayOfWeek.Sunday));
    Assert.AreEqual(Tuesday, PixataSchedulerDates.StartOfWeek(Tuesday, DayOfWeek.Tuesday));
  }

  [TestMethod]
  public void VisibleRange_ForEachView() {
    Assert.AreEqual((Tuesday, Tuesday.AddDays(1)), PixataSchedulerDates.VisibleRange(PixataSchedulerView.Day, Tuesday.AddHours(10), 1, DayOfWeek.Monday));
    Assert.AreEqual((new DateTime(2026, 10, 12), new DateTime(2026, 10, 19)), PixataSchedulerDates.VisibleRange(PixataSchedulerView.Week, Tuesday, 1, DayOfWeek.Monday));
    Assert.AreEqual((Tuesday, Tuesday.AddDays(3)), PixataSchedulerDates.VisibleRange(PixataSchedulerView.MultiDay, Tuesday, 3, DayOfWeek.Monday));
    Assert.AreEqual((Tuesday, Tuesday.AddDays(7)), PixataSchedulerDates.VisibleRange(PixataSchedulerView.Agenda, Tuesday, 7, DayOfWeek.Monday));
    // October 2026 starts on a Thursday, so with Sunday first the month view starts on 27 September, and always shows six weeks
    Assert.AreEqual((new DateTime(2026, 9, 27), new DateTime(2026, 11, 8)), PixataSchedulerDates.VisibleRange(PixataSchedulerView.Month, Tuesday, 1, DayOfWeek.Sunday));
  }

  [TestMethod]
  public void Navigate_MovesByTheViewsLength() {
    Assert.AreEqual(Tuesday.AddDays(1), PixataSchedulerDates.Navigate(PixataSchedulerView.Day, Tuesday, 1, 1));
    Assert.AreEqual(Tuesday.AddDays(-7), PixataSchedulerDates.Navigate(PixataSchedulerView.Week, Tuesday, 1, -1));
    Assert.AreEqual(Tuesday.AddDays(-3), PixataSchedulerDates.Navigate(PixataSchedulerView.MultiDay, Tuesday, 3, -1));
    Assert.AreEqual(new DateTime(2027, 2, 28), PixataSchedulerDates.Navigate(PixataSchedulerView.Month, new(2027, 1, 31), 1, 1));
  }

  [TestMethod]
  public void DayWindow_TreatsMidnightAsTheEndOfTheDay() {
    Assert.AreEqual((TimeSpan.FromHours(8), TimeSpan.FromHours(24)), PixataSchedulerDates.DayWindow(Tuesday.AddHours(8), Tuesday));
    Assert.AreEqual((TimeSpan.FromHours(8), TimeSpan.FromHours(18)), PixataSchedulerDates.DayWindow(Tuesday.AddHours(8), Tuesday.AddHours(18)));
  }

  [TestMethod]
  public void Divisions_SplitTheSlots() {
    CollectionAssert.AreEqual(new List<TimeSpan> { new(8, 0, 0), new(8, 30, 0), new(9, 0, 0), new(9, 30, 0) },
      PixataSchedulerDates.Divisions(TimeSpan.FromHours(8), TimeSpan.FromHours(10), 60, 2));
    Assert.AreEqual(TimeSpan.FromMinutes(15), PixataSchedulerDates.DivisionLength(60, 4));
  }

  [TestMethod]
  public void DaySpan_IncludesTheEndDate() {
    Assert.AreEqual((Tuesday, Tuesday.AddDays(4)), PixataSchedulerDates.DaySpan(Tuesday, Tuesday.AddDays(3)));
    Assert.AreEqual((Tuesday, Tuesday.AddDays(1)), PixataSchedulerDates.DaySpan(Tuesday, Tuesday));
    Assert.AreEqual((Tuesday, Tuesday.AddDays(1)), PixataSchedulerDates.DaySpan(Tuesday, Tuesday.AddDays(-1)));
  }

  [TestMethod]
  public void TimedDaySpan_DoesNotTouchTheDayItEndsAtMidnight() {
    Assert.AreEqual((Tuesday, Tuesday.AddDays(2)), PixataSchedulerDates.TimedDaySpan(Tuesday.AddHours(22), Tuesday.AddDays(1).AddHours(6)));
    Assert.AreEqual((Tuesday, Tuesday.AddDays(1)), PixataSchedulerDates.TimedDaySpan(Tuesday.AddHours(10), Tuesday.AddDays(1)));
    Assert.AreEqual((Tuesday, Tuesday.AddDays(1)), PixataSchedulerDates.TimedDaySpan(Tuesday, Tuesday));
  }

  [TestMethod]
  public void IsLong_ForAllDayAndDayLongAppointments() {
    Assert.IsTrue(PixataSchedulerDates.IsLong(Tuesday, Tuesday, true));
    Assert.IsTrue(PixataSchedulerDates.IsLong(Tuesday.AddHours(9), Tuesday.AddDays(1).AddHours(9), false));
    Assert.IsFalse(PixataSchedulerDates.IsLong(Tuesday.AddHours(22), Tuesday.AddDays(1).AddHours(6), false));
  }

  [TestMethod]
  public void RangeText_ForEachKindOfRange() {
    Assert.AreEqual("October 13", PixataSchedulerDates.RangeText(PixataSchedulerView.Day, Tuesday, Tuesday, Tuesday.AddDays(1), UnitedStates));
    Assert.AreEqual("October 11 - October 17", PixataSchedulerDates.RangeText(PixataSchedulerView.Week, Tuesday, new(2026, 10, 11), new(2026, 10, 18), UnitedStates));
    Assert.AreEqual("October 2026", PixataSchedulerDates.RangeText(PixataSchedulerView.Month, Tuesday, new(2026, 9, 27), new(2026, 11, 8), UnitedStates));
    Assert.AreEqual("October 2026", PixataSchedulerDates.RangeText(PixataSchedulerView.Month, Tuesday, new(2026, 9, 27), new(2026, 11, 8), CultureInfo.InvariantCulture));
    Assert.AreEqual("December 27 2026 - January 2 2027", PixataSchedulerDates.RangeText(PixataSchedulerView.Week, new(2026, 12, 29), new(2026, 12, 27), new(2027, 1, 3), UnitedStates));
  }

  #endregion

  #region Layout

  [TestMethod]
  public void Columns_PutsOverlappingAppointmentsSideBySide() {
    PixataSchedulerLane[] lanes = PixataSchedulerLayout.Columns([(900, 1000), (930, 1030), (1000, 1100), (1100, 1200)]);
    CollectionAssert.AreEqual(new[] { new PixataSchedulerLane(0, 2), new PixataSchedulerLane(1, 2), new PixataSchedulerLane(0, 2), new PixataSchedulerLane(0, 1) }, lanes);
  }

  [TestMethod]
  public void Columns_LongestFirstWhenTheyStartTogether() {
    PixataSchedulerLane[] lanes = PixataSchedulerLayout.Columns([(900, 930), (900, 1100)]);
    Assert.AreEqual(1, lanes[0].Lane);
    Assert.AreEqual(0, lanes[1].Lane);
  }

  [TestMethod]
  public void Columns_MomentsAtTheSameTimeOverlap() =>
    Assert.AreEqual(2, PixataSchedulerLayout.Columns([(900, 900), (900, 900)])[0].Lanes);

  [TestMethod]
  public void Rows_StackOverlappingBars() {
    int[] rows = PixataSchedulerLayout.Rows([(0, 2), (1, 3), (2, 4), (0, 1)]);
    CollectionAssert.AreEqual(new[] { 0, 1, 0, 1 }, rows);
    Assert.AreEqual(2, PixataSchedulerLayout.RowCount(rows));
    Assert.AreEqual(0, PixataSchedulerLayout.RowCount([]));
  }

  #endregion

  #region The views' models

  [TestMethod]
  public void Groups_AreEveryCombinationInOrder() {
    List<PixataSchedulerGroup> groups = PixataSchedulerModelBuilder.Groups([
      ("Room", [("A", "Room A", "red"), ("B", "Room B", null)]),
      ("Manager", [(1, "Alice", null), (2, "Bob", null)])
    ]);
    CollectionAssert.AreEqual(new[] { "Room A / Alice", "Room A / Bob", "Room B / Alice", "Room B / Bob" }, groups.Select(g => g.Text).ToArray());
    Assert.AreEqual(3, groups[3].Index);
    List<List<(PixataSchedulerGroupLevel Level, int Span)>> headers = PixataSchedulerModelBuilder.GroupHeaders(groups);
    CollectionAssert.AreEqual(new[] { ("Room A", 2), ("Room B", 2) }, headers[0].Select(h => (h.Level.Text, h.Span)).ToArray());
    CollectionAssert.AreEqual(new[] { ("Alice", 1), ("Bob", 1), ("Alice", 1), ("Bob", 1) }, headers[1].Select(h => (h.Level.Text, h.Span)).ToArray());
  }

  [TestMethod]
  public void Group_ContainsTheAppointmentsWithItsResources() {
    List<PixataSchedulerGroup> groups = PixataSchedulerModelBuilder.Groups([("Room", [("A", "Room A", null), ("B", "Room B", null)])]);
    PixataSchedulerItem<string> item = Item("Meeting", Tuesday, Tuesday.AddHours(1), room: "B");
    Assert.IsFalse(groups[0].Contains(item));
    Assert.IsTrue(groups[1].Contains(item));
    Assert.IsTrue(NoGroups.Single().Contains(item));
  }

  [TestMethod]
  public void TimeGrid_PlacesAppointments() {
    DateTime monday = Tuesday.AddDays(-1);
    PixataSchedulerTimeGridModel<string> model = PixataSchedulerModelBuilder.TimeGrid(
      [
        Item("Meeting", Tuesday.AddDays(1).AddHours(10), Tuesday.AddDays(1).AddHours(11)),
        Item("Trip", Tuesday, Tuesday.AddDays(3), true),
        Item("Overnight", Tuesday.AddDays(2).AddHours(22), Tuesday.AddDays(3).AddHours(6))
      ],
      Days(monday, 7), TimeSpan.Zero, TimeSpan.FromDays(1), TimeSpan.FromMinutes(30), 2, NoGroups, false, true);
    PixataSchedulerTimeBlock<string> block = model.Blocks.Single();
    Assert.AreEqual(48, model.Divisions.Count);

    PixataSchedulerBar<string> trip = block.AllDayBars.Single();
    Assert.AreEqual((1, 4, false, false), (trip.FirstColumn, trip.LastColumn, trip.ContinuesBefore, trip.ContinuesAfter));
    Assert.AreEqual(1, block.AllDayRows);

    PixataSchedulerBox<string> meeting = block.Boxes[2].Single();
    Assert.AreEqual(10 / 24d, meeting.Top, 1e-9);
    Assert.AreEqual(1 / 24d, meeting.Height, 1e-9);

    PixataSchedulerBox<string> evening = block.Boxes[3].Single();
    PixataSchedulerBox<string> morning = block.Boxes[4].Single();
    Assert.AreEqual((22 / 24d, true), (evening.Top, evening.ContinuesAfter));
    Assert.AreEqual((0d, true, 6 / 24d), (morning.Top, morning.ContinuesBefore, morning.Height));
  }

  [TestMethod]
  public void TimeGrid_WithoutTheAllDayRow_PutsDayLongTimedAppointmentsInTheColumns() {
    PixataSchedulerTimeGridModel<string> model = PixataSchedulerModelBuilder.TimeGrid(
      [Item("Long", Tuesday.AddHours(9), Tuesday.AddDays(1).AddHours(9)), Item("Trip", Tuesday, Tuesday, true)],
      Days(Tuesday, 2), TimeSpan.FromHours(8), TimeSpan.FromHours(18), TimeSpan.FromMinutes(30), 2, NoGroups, false, false);
    Assert.AreEqual(0, model.Blocks[0].AllDayBars.Count);
    Assert.AreEqual("Long", model.Blocks[0].Boxes[0].Single().Item.Title);
    Assert.AreEqual("Long", model.Blocks[0].Boxes[1].Single().Item.Title);
  }

  [TestMethod]
  public void TimeGrid_GroupedVertically_HasABlockPerGroup() {
    List<PixataSchedulerGroup> groups = PixataSchedulerModelBuilder.Groups([("Room", [("A", "Room A", null), ("B", "Room B", null)])]);
    PixataSchedulerTimeGridModel<string> model = PixataSchedulerModelBuilder.TimeGrid(
      [Item("Meeting", Tuesday.AddHours(10), Tuesday.AddHours(11), room: "B")],
      Days(Tuesday, 1), TimeSpan.FromHours(8), TimeSpan.FromHours(18), TimeSpan.FromMinutes(30), 2, groups, true, true);
    Assert.AreEqual(2, model.Blocks.Count);
    Assert.AreEqual(0, model.Blocks[0].Boxes[0].Count);
    Assert.AreEqual(1, model.Blocks[1].Boxes[0].Count);
  }

  [TestMethod]
  public void Month_CountsTheAppointmentsThatDoNotFit() {
    (DateTime start, DateTime end) = PixataSchedulerDates.VisibleRange(PixataSchedulerView.Month, Tuesday, 1, DayOfWeek.Sunday);
    PixataSchedulerMonthModel<string> model = PixataSchedulerModelBuilder.Month(
      [
        Item("One", Tuesday.AddDays(1).AddHours(9), Tuesday.AddDays(1).AddHours(10)),
        Item("Two", Tuesday.AddDays(1).AddHours(11), Tuesday.AddDays(1).AddHours(12)),
        Item("Three", Tuesday.AddDays(1).AddHours(13), Tuesday.AddDays(1).AddHours(14))
      ],
      Days(start, (end - start).Days), NoGroups, false, 2);
    PixataSchedulerMonthWeek<string> week = model.Blocks.Single().Weeks[2];
    Assert.AreEqual(new DateTime(2026, 10, 11), week.Columns[0].Day);
    Assert.AreEqual(2, week.Bars.Count);
    CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 0, 0, 0 }, week.More);
  }

  [TestMethod]
  public void Month_BarsAcrossTheEndOfAWeekContinue() {
    PixataSchedulerMonthModel<string> model = PixataSchedulerModelBuilder.Month(
      [Item("Holiday", new(2026, 10, 16), new(2026, 10, 20), true)],
      Days(new(2026, 10, 11), 14), NoGroups, false, 3);
    PixataSchedulerBar<string> first = model.Blocks[0].Weeks[0].Bars.Single();
    PixataSchedulerBar<string> second = model.Blocks[0].Weeks[1].Bars.Single();
    Assert.AreEqual((5, 6, false, true), (first.FirstColumn, first.LastColumn, first.ContinuesBefore, first.ContinuesAfter));
    Assert.AreEqual((0, 2, true, false), (second.FirstColumn, second.LastColumn, second.ContinuesBefore, second.ContinuesAfter));
  }

  [TestMethod]
  public void Timeline_PlacesBarsAlongTheVisibleHours() {
    PixataSchedulerTimelineModel<string> model = PixataSchedulerModelBuilder.Timeline(
      [Item("Meeting", Tuesday.AddHours(9), Tuesday.AddHours(10)), Item("Trip", Tuesday, Tuesday.AddDays(3), true)],
      Days(Tuesday, 1), TimeSpan.FromHours(8), TimeSpan.FromHours(18), TimeSpan.FromMinutes(30), 2, NoGroups, false);
    PixataSchedulerTimelineLane<string> lane = model.Lanes.Single();
    PixataSchedulerBar<string> meeting = lane.Bars.Single(b => b.Item.Title == "Meeting");
    PixataSchedulerBar<string> trip = lane.Bars.Single(b => b.Item.Title == "Trip");
    Assert.AreEqual(0.1, meeting.Start, 1e-9);
    Assert.AreEqual(0.1, meeting.Width, 1e-9);
    Assert.AreEqual((0d, 1d, false, true), (trip.Start, trip.Width, trip.ContinuesBefore, trip.ContinuesAfter));
    Assert.AreEqual(2, lane.Rows);
  }

  [TestMethod]
  public void Timeline_GroupedVertically_HasALanePerGroup() {
    List<PixataSchedulerGroup> groups = PixataSchedulerModelBuilder.Groups([("Room", [("A", "Room A", null), ("B", "Room B", null)])]);
    PixataSchedulerTimelineModel<string> model = PixataSchedulerModelBuilder.Timeline(
      [Item("Meeting", Tuesday.AddHours(9), Tuesday.AddHours(10), room: "A")],
      Days(Tuesday, 1), TimeSpan.FromHours(8), TimeSpan.FromHours(18), TimeSpan.FromMinutes(30), 2, groups, true);
    Assert.AreEqual(2, model.Lanes.Count);
    Assert.AreEqual(1, model.ColumnGroups.Count);
    Assert.AreEqual(1, model.Lanes[0].Bars.Count);
    Assert.AreEqual(0, model.Lanes[1].Bars.Count);
  }

  [TestMethod]
  public void Agenda_ListsEachAppointmentOnceOnItsFirstDay() {
    List<PixataSchedulerItem<string>> items = [
      Item("Meeting", Tuesday.AddDays(1).AddHours(10), Tuesday.AddDays(1).AddHours(11)),
      Item("Trip", Tuesday, Tuesday.AddDays(1), true)
    ];
    PixataSchedulerAgendaModel<string> model = PixataSchedulerModelBuilder.Agenda(items, Days(Tuesday, 3), NoGroups, true);
    List<(DateTime Day, List<PixataSchedulerItem<string>> Items)> days = model.Groups.Single().Days;
    Assert.AreEqual(2, days.Count);
    CollectionAssert.AreEqual(new[] { "Trip" }, days[0].Items.Select(i => i.Title).ToArray());
    CollectionAssert.AreEqual(new[] { "Meeting" }, days[1].Items.Select(i => i.Title).ToArray());
    Assert.AreEqual(3, PixataSchedulerModelBuilder.Agenda(items, Days(Tuesday, 3), NoGroups, false).Groups[0].Days.Count);
  }

  [TestMethod]
  public void Covers_AllDayAndMoments() {
    Assert.IsTrue(PixataSchedulerModelBuilder.Covers(Item("Trip", Tuesday, Tuesday.AddDays(1), true), Tuesday.AddDays(1)));
    Assert.IsFalse(PixataSchedulerModelBuilder.Covers(Item("Trip", Tuesday, Tuesday.AddDays(1), true), Tuesday.AddDays(2)));
    Assert.IsTrue(PixataSchedulerModelBuilder.Covers(Item("Moment", Tuesday, Tuesday), Tuesday));
    Assert.IsFalse(PixataSchedulerModelBuilder.Covers(Item("Moment", Tuesday, Tuesday), Tuesday.AddDays(-1)));
  }

  #endregion

  #region Fields

  [TestMethod]
  public void Fields_ReadAndWriteTheDefaultFields() {
    PixataSchedulerItemFields fields = new();
    Appointment item = new() { Id = 1, Title = "Meeting", Start = Tuesday.AddHours(10), End = Tuesday.AddHours(9) };
    Assert.AreEqual("Meeting", fields.GetTitle(item));
    Assert.AreEqual(item.Start, fields.GetEnd(item), "An end before the start is treated as the start");
    Assert.AreEqual("", fields.GetDescription(item), "A missing field is empty");
    Assert.IsFalse(fields.HasDescription(item));
    fields.SetEnd(item, Tuesday.AddHours(11));
    Assert.AreEqual(Tuesday.AddHours(11), item.End);
    fields.ClearId(item);
    Assert.AreEqual(0, item.Id);
  }

  [TestMethod]
  public void Fields_RecurrenceExceptionsAreSortedWithoutDuplicates() {
    PixataSchedulerItemFields fields = new();
    Appointment item = new();
    fields.SetRecurrenceExceptions(item, [Tuesday.AddDays(1), Tuesday, Tuesday.AddDays(1)]);
    CollectionAssert.AreEqual(new List<DateTime> { Tuesday, Tuesday.AddDays(1) }, item.RecurrenceExceptions);
  }

  [TestMethod]
  public void Fields_ReadDictionariesAndExceptionStrings() {
    PixataSchedulerItemFields fields = new();
    Dictionary<string, object?> item = new() {
      ["Title"] = "Meeting",
      ["Start"] = "2026-10-13T10:00:00",
      ["End"] = Tuesday.AddHours(11),
      ["RecurrenceExceptions"] = "2026-10-14T10:00:00, 2026-10-15T10:00:00"
    };
    Assert.AreEqual(Tuesday.AddHours(10), fields.GetStart(item));
    Assert.AreEqual(2, fields.GetRecurrenceExceptions(item).Count);
  }

  [TestMethod]
  public void Fields_Validate_NamesTheParameterToSet() {
    InvalidOperationException missingTitle = Assert.ThrowsException<InvalidOperationException>(() => new PixataSchedulerItemFields().Validate(new NoTitle()));
    StringAssert.Contains(missingTitle.Message, "TitleField");
    InvalidOperationException missingId = Assert.ThrowsException<InvalidOperationException>(() => new PixataSchedulerItemFields { Id = "Key" }.Validate(new Appointment()));
    StringAssert.Contains(missingId.Message, "IdField");
  }

  [TestMethod]
  public void ToOffset_LocalTimesTakeTheLocalOffsetForTheirNewDate() {
    TimeZoneInfo london = LondonLike();
    DateTimeOffset winter = new(2026, 1, 12, 9, 0, 0, TimeSpan.Zero);
    DateTimeOffset summer = new(2026, 6, 15, 9, 0, 0, TimeSpan.FromHours(1));
    Assert.AreEqual(new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.FromHours(1)), PixataSchedulerItemFields.ToOffset(new(2026, 6, 15, 9, 0, 0), winter, london),
      "A winter appointment moved into the summer");
    Assert.AreEqual(new DateTimeOffset(2026, 12, 14, 9, 0, 0, TimeSpan.Zero), PixataSchedulerItemFields.ToOffset(new(2026, 12, 14, 9, 0, 0), summer, london),
      "A summer appointment moved into the winter");
    Assert.AreEqual(TimeSpan.FromHours(1), PixataSchedulerItemFields.ToOffset(new(2026, 6, 15, 9, 0, 0), default(DateTimeOffset), london).Offset,
      "A new appointment");
    Assert.AreEqual(TimeSpan.FromHours(1), PixataSchedulerItemFields.ToOffset(new(2026, 6, 15, 9, 0, 0), null, london).Offset);
  }

  [TestMethod]
  public void ToOffset_OtherOffsetsAreKept() {
    TimeZoneInfo london = LondonLike();
    DateTimeOffset utcInSummer = new(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);
    Assert.AreEqual(new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.Zero), PixataSchedulerItemFields.ToOffset(new(2026, 6, 16, 8, 0, 0), utcInSummer, london));
    DateTimeOffset india = new(2026, 1, 12, 9, 0, 0, new TimeSpan(5, 30, 0));
    Assert.AreEqual(new TimeSpan(5, 30, 0), PixataSchedulerItemFields.ToOffset(new(2026, 6, 15, 9, 0, 0), india, london).Offset);
  }

  [TestMethod]
  public void IsEmptyId_ForEachKindOfId() {
    foreach (object? id in new object?[] { null, 0, 0L, (short)0, "", Guid.Empty }) {
      Assert.IsTrue(PixataSchedulerItemFields.IsEmptyId(id), $"{id?.GetType().Name}");
    }
    foreach (object id in new object[] { 1, 2L, "a", Guid.NewGuid() }) {
      Assert.IsFalse(PixataSchedulerItemFields.IsEmptyId(id), $"{id.GetType().Name}");
    }
  }

  [TestMethod]
  public void ValuesMatch_AllowsForDifferentTypes() {
    Assert.IsTrue(PixataSchedulerItemFields.ValuesMatch(1, 1L));
    Assert.IsTrue(PixataSchedulerItemFields.ValuesMatch(1, "1"));
    Assert.IsTrue(PixataSchedulerItemFields.ValuesMatch(null, null));
    Assert.IsFalse(PixataSchedulerItemFields.ValuesMatch(null, 1));
    Assert.IsFalse(PixataSchedulerItemFields.ValuesMatch(1, 2));
  }

  #endregion

  #region The edit form's model

  [TestMethod]
  public void EditModel_AllDayDatesAreInclusive() {
    PixataSchedulerItemFields fields = new();
    Appointment item = new() { Title = "Trip", Start = Tuesday, End = Tuesday.AddDays(3), IsAllDay = true };
    PixataSchedulerEditModel model = PixataSchedulerEditModel.From<Appointment>(item, fields, []);
    Assert.AreEqual((Tuesday, Tuesday.AddDays(3)), (model.Start, model.End));
    model.SetStart(Tuesday.AddDays(7));
    Assert.AreEqual((Tuesday.AddDays(7), Tuesday.AddDays(10)), (model.Start, model.End), "Moving the start keeps the length");
    model.ApplyTo<Appointment>(item, fields, []);
    Assert.AreEqual((Tuesday.AddDays(7), Tuesday.AddDays(10), true), (item.Start, item.End, item.IsAllDay));
  }

  // With a long (rather than a nullable) recurrence id, zero means the appointment isn't an exception
  [TestMethod]
  public void EditModel_ZeroLongRecurrenceId_IsNotAnException() {
    PixataSchedulerItemFields fields = new();
    LongIds series = new() { Id = 5, Title = "Run", Start = Tuesday.AddHours(7), End = Tuesday.AddHours(8), RecurrenceRule = "FREQ=DAILY" };
    PixataSchedulerEditModel model = PixataSchedulerEditModel.From<LongIds>(series, fields, []);
    Assert.IsFalse(model.IsException);
    model.RecurrenceRule = "FREQ=WEEKLY";
    model.ApplyTo<LongIds>(series, fields, []);
    Assert.AreEqual("FREQ=WEEKLY", series.RecurrenceRule);
    Assert.IsTrue(PixataSchedulerEditModel.From<LongIds>(new LongIds { Title = "Run (late)", RecurrenceId = 5 }, fields, []).IsException);
  }

  [TestMethod]
  public void EditModel_SwitchingToTimedUsesTheDefaultTimes() {
    PixataSchedulerEditModel model = new() { Title = "Trip", Start = Tuesday, End = Tuesday.AddDays(2), IsAllDay = true };
    model.SetAllDay(false, TimeSpan.FromHours(9), TimeSpan.FromMinutes(30));
    Assert.AreEqual((Tuesday.AddHours(9), Tuesday.AddHours(9.5), false), (model.Start, model.End, model.IsAllDay));
    model.SetAllDay(true, TimeSpan.FromHours(9), TimeSpan.FromMinutes(30));
    Assert.AreEqual((Tuesday, Tuesday, true), (model.Start, model.End, model.IsAllDay));
  }

  [TestMethod]
  public void EditModel_Validate() {
    PixataSchedulerEditModel model = new() { Title = " ", Start = Tuesday.AddHours(10), End = Tuesday.AddHours(10) };
    EditContext context = new(model);
    ValidationMessageStore messages = new(context);
    PixataSchedulerMessages text = new();
    Assert.IsFalse(model.Validate(context, messages, text));
    CollectionAssert.AreEqual(new[] { text.TitleRequired }, context.GetValidationMessages(context.Field(nameof(PixataSchedulerEditModel.Title))).ToArray());
    CollectionAssert.AreEqual(new[] { text.EndBeforeStart }, context.GetValidationMessages(context.Field(nameof(PixataSchedulerEditModel.End))).ToArray());
    messages.Clear();
    model.Title = "Meeting";
    model.IsAllDay = true;
    Assert.IsTrue(model.Validate(context, messages, text), "An all-day appointment can start and end on the same day");
  }

  #endregion
}
