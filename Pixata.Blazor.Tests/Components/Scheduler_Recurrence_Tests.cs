using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

/// <summary>
/// The scheduler's recurrence rules: parsing and writing RRULEs, working out the occurrences, and the changes the recurrence
/// editor makes
/// </summary>
[TestClass]
public class Scheduler_Recurrence_Tests {
  private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

  private static List<DateTime> Occurrences(string rule, DateTime seriesStart, DateTime from, DateTime to) =>
    PixataRecurrenceRule.Parse(rule).GetOccurrences(seriesStart, from, to);

  private static void AreDates(IEnumerable<DateTime> actual, params string[] expected) =>
    CollectionAssert.AreEqual(expected.Select(e => DateTime.Parse(e, CultureInfo.InvariantCulture)).ToList(), actual.ToList(),
      $"Got {string.Join(", ", actual.Select(a => a.ToString("s", CultureInfo.InvariantCulture)))}");

  #region Parsing and writing

  [TestMethod]
  public void Parse_ReadsEveryPart() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("RRULE:FREQ=MONTHLY;INTERVAL=2;BYDAY=-1FR,2TU;BYMONTHDAY=1,-1;BYMONTH=3,9;BYSETPOS=1;COUNT=6;WKST=SU");
    Assert.AreEqual(PixataRecurrenceFrequency.Monthly, rule.Frequency);
    Assert.AreEqual(2, rule.Interval);
    CollectionAssert.AreEqual(new List<PixataRecurrenceDay> { new(DayOfWeek.Friday, -1), new(DayOfWeek.Tuesday, 2) }, rule.ByDay);
    CollectionAssert.AreEqual(new List<int> { 1, -1 }, rule.ByMonthDay);
    CollectionAssert.AreEqual(new List<int> { 3, 9 }, rule.ByMonth);
    CollectionAssert.AreEqual(new List<int> { 1 }, rule.BySetPosition);
    Assert.AreEqual(6, rule.Count);
    Assert.AreEqual(DayOfWeek.Sunday, rule.WeekStart);
  }

  [TestMethod]
  public void Parse_IsCaseInsensitiveAndIgnoresExtensions() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("rrule:freq=weekly;byday=mo,we;X-NAME=whatever");
    Assert.AreEqual(PixataRecurrenceFrequency.Weekly, rule.Frequency);
    Assert.AreEqual(2, rule.ByDay.Count);
  }

  [TestMethod]
  [DataRow("FREQ=MONTHLY;INTERVAL=2;BYDAY=-1FR;COUNT=6")]
  [DataRow("FREQ=WEEKLY;BYDAY=MO,WE,FR;UNTIL=20261231T235959")]
  [DataRow("FREQ=DAILY;UNTIL=20261231T235959Z")]
  [DataRow("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1")]
  [DataRow("FREQ=YEARLY;BYMONTHDAY=14;BYMONTH=2")]
  [DataRow("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU;WKST=SU")]
  [DataRow("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU;WKST=MO")]
  [DataRow("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,SU")]
  public void ToString_RoundTrips(string rule) =>
    Assert.AreEqual(rule, PixataRecurrenceRule.Parse(rule).ToString());

  [TestMethod]
  public void Until_SetInCode_IsNotUtc() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=DAILY;UNTIL=20261231T235959Z");
    rule.Until = new DateTime(2027, 1, 31, 23, 59, 59);
    Assert.AreEqual("FREQ=DAILY;UNTIL=20270131T235959", rule.ToString());
  }

  [TestMethod]
  [DataRow("")]
  [DataRow("INTERVAL=2")]
  [DataRow("FREQ=HOURLY")]
  [DataRow("FREQ=DAILY;INTERVAL=0")]
  [DataRow("FREQ=DAILY;COUNT=-1")]
  [DataRow("FREQ=DAILY;COUNT=3;UNTIL=20261231")]
  [DataRow("FREQ=WEEKLY;BYDAY=XX")]
  [DataRow("FREQ=MONTHLY;BYMONTHDAY=32")]
  [DataRow("FREQ=YEARLY;BYMONTH=13")]
  [DataRow("FREQ=DAILY;BYHOUR=9")]
  [DataRow("FREQ=DAILY;nonsense")]
  public void Parse_RejectsBadRules(string rule) {
    Assert.ThrowsException<FormatException>(() => PixataRecurrenceRule.Parse(rule));
    Assert.IsFalse(PixataRecurrenceRule.TryParse(rule, out PixataRecurrenceRule? result));
    Assert.IsNull(result);
  }

  [TestMethod]
  [DataRow("FREQ=DAILY;UNTIL=99991231")]
  [DataRow("FREQ=DAILY;UNTIL=99991231T235959Z")]
  public void Until_AtTheEndOfTheCalendar_MeansForEver(string rule) =>
    AreDates(Occurrences(rule, new(2026, 10, 13, 9, 0, 0), new(2026, 10, 13), new(2026, 10, 15)),
      "2026-10-13T09:00", "2026-10-14T09:00");

  [TestMethod]
  public void Clone_IsIndependent() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");
    PixataRecurrenceRule copy = rule.Clone();
    copy.ByDay.Add(new(DayOfWeek.Friday));
    Assert.AreEqual(1, rule.ByDay.Count);
  }

  #endregion

  #region Occurrences

  [TestMethod]
  public void Daily_WithCount() =>
    AreDates(Occurrences("FREQ=DAILY;COUNT=3", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 11, 1)),
      "2026-10-13T09:00", "2026-10-14T09:00", "2026-10-15T09:00");

  [TestMethod]
  public void Daily_WithInterval() =>
    AreDates(Occurrences("FREQ=DAILY;INTERVAL=2", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 13), new(2026, 10, 18)),
      "2026-10-13T09:00", "2026-10-15T09:00", "2026-10-17T09:00");

  [TestMethod]
  public void Daily_UntilADate_IncludesThatDay() =>
    AreDates(Occurrences("FREQ=DAILY;UNTIL=20261015", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 11, 1)),
      "2026-10-13T09:00", "2026-10-14T09:00", "2026-10-15T09:00");

  [TestMethod]
  public void Daily_UntilATime_StopsBeforeIt() =>
    AreDates(Occurrences("FREQ=DAILY;UNTIL=20261015T080000", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 11, 1)),
      "2026-10-13T09:00", "2026-10-14T09:00");

  [TestMethod]
  public void Count_IsCountedFromTheStartOfTheSeries() =>
    AreDates(Occurrences("FREQ=DAILY;COUNT=5", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 16), new(2026, 10, 31)),
      "2026-10-16T09:00", "2026-10-17T09:00");

  [TestMethod]
  public void RangeEnd_IsExclusive() =>
    AreDates(Occurrences("FREQ=DAILY", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 13), new(2026, 10, 14, 9, 0, 0)),
      "2026-10-13T09:00");

  [TestMethod]
  public void LongRunningSeries_JumpsToTheRange() =>
    Assert.AreEqual(7, Occurrences("FREQ=DAILY", new(2000, 1, 1, 9, 0, 0), new(2030, 6, 3), new(2030, 6, 10)).Count);

  [TestMethod]
  public void Weekly_OnSeveralDays() =>
    AreDates(Occurrences("FREQ=WEEKLY;BYDAY=MO,WE,FR", new(2026, 10, 12, 9, 0, 0), new(2026, 10, 12), new(2026, 10, 19)),
      "2026-10-12T09:00", "2026-10-14T09:00", "2026-10-16T09:00");

  [TestMethod]
  public void Weekly_WithoutByDay_UsesTheStartDay() =>
    AreDates(Occurrences("FREQ=WEEKLY;COUNT=3", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 12, 1)),
      "2026-10-13T09:00", "2026-10-20T09:00", "2026-10-27T09:00");

  [TestMethod]
  public void Weekly_EveryOtherWeek() =>
    AreDates(Occurrences("FREQ=WEEKLY;INTERVAL=2;BYDAY=TU", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 11, 15)),
      "2026-10-13T09:00", "2026-10-27T09:00", "2026-11-10T09:00");

  // The examples from RFC 5545, section 3.8.5.3, showing that WKST matters
  [TestMethod]
  public void Weekly_WeekStartMonday() =>
    AreDates(Occurrences("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO", new(1997, 8, 5, 9, 0, 0), new(1997, 1, 1), new(1998, 1, 1)),
      "1997-08-05T09:00", "1997-08-10T09:00", "1997-08-19T09:00", "1997-08-24T09:00");

  [TestMethod]
  public void Weekly_WeekStartSunday() =>
    AreDates(Occurrences("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU", new(1997, 8, 5, 9, 0, 0), new(1997, 1, 1), new(1998, 1, 1)),
      "1997-08-05T09:00", "1997-08-17T09:00", "1997-08-19T09:00", "1997-08-31T09:00");

  // Without WKST, the week starts on the day given (the scheduler gives its first day of the week), so that every other week
  // means the same as it does on the calendar the user sees
  [TestMethod]
  public void Weekly_WithoutWeekStart_UsesTheFirstDayOfTheWeekGiven() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU");
    Assert.IsNull(rule.WeekStart);
    AreDates(rule.GetOccurrences(new(1997, 8, 5, 9, 0, 0), new(1997, 1, 1), new(1998, 1, 1), DayOfWeek.Sunday),
      "1997-08-05T09:00", "1997-08-17T09:00", "1997-08-19T09:00", "1997-08-31T09:00");
    AreDates(rule.GetOccurrences(new(1997, 8, 5, 9, 0, 0), new(1997, 1, 1), new(1998, 1, 1)),
      "1997-08-05T09:00", "1997-08-10T09:00", "1997-08-19T09:00", "1997-08-24T09:00");
  }

  [TestMethod]
  public void Weekly_WithWeekStart_IgnoresTheFirstDayOfTheWeekGiven() =>
    AreDates(PixataRecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO")
        .GetOccurrences(new(1997, 8, 5, 9, 0, 0), new(1997, 1, 1), new(1998, 1, 1), DayOfWeek.Sunday),
      "1997-08-05T09:00", "1997-08-10T09:00", "1997-08-19T09:00", "1997-08-24T09:00");

  // A UTC UNTIL (as calendar apps write it) is in UTC, while the occurrences are in local time
  [TestMethod]
  public void Until_InUtc_IsComparedInLocalTime() {
    TimeZoneInfo twoHoursAhead = TimeZoneInfo.CreateCustomTimeZone("Test+2", TimeSpan.FromHours(2), "Test+2", "Test+2");
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=DAILY;UNTIL=20261015T073000Z");
    // 07:30 UTC is 09:30 here, so the 09:00 occurrence on the 15th is in
    AreDates(rule.GetOccurrences(new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 11, 1), null, twoHoursAhead),
      "2026-10-13T09:00", "2026-10-14T09:00", "2026-10-15T09:00");
    // A series in UTC is compared in UTC
    AreDates(rule.GetOccurrences(new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc), new(2026, 10, 1), new(2026, 11, 1), null, twoHoursAhead),
      "2026-10-13T09:00", "2026-10-14T09:00");
    // And an UNTIL without a Z is already local
    AreDates(PixataRecurrenceRule.Parse("FREQ=DAILY;UNTIL=20261015T073000").GetOccurrences(new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 11, 1), null, twoHoursAhead),
      "2026-10-13T09:00", "2026-10-14T09:00");
  }

  [TestMethod]
  public void FirstAppointment_IsAnOccurrenceEvenIfItDoesNotMatch() =>
    AreDates(Occurrences("FREQ=WEEKLY;BYDAY=MO;COUNT=2", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2026, 12, 1)),
      "2026-10-13T09:00", "2026-10-19T09:00");

  [TestMethod]
  public void Monthly_OnTheLastFriday() =>
    AreDates(Occurrences("FREQ=MONTHLY;BYDAY=-1FR;COUNT=3", new(2026, 10, 30, 12, 0, 0), new(2026, 1, 1), new(2027, 12, 31)),
      "2026-10-30T12:00", "2026-11-27T12:00", "2026-12-25T12:00");

  [TestMethod]
  public void Monthly_OnTheSecondTuesday() =>
    AreDates(Occurrences("FREQ=MONTHLY;BYDAY=2TU", new(2026, 10, 13, 9, 0, 0), new(2026, 10, 1), new(2027, 1, 1)),
      "2026-10-13T09:00", "2026-11-10T09:00", "2026-12-08T09:00");

  [TestMethod]
  public void Monthly_OnThe31st_SkipsShortMonths() =>
    AreDates(Occurrences("FREQ=MONTHLY;BYMONTHDAY=31", new(2026, 10, 31, 9, 0, 0), new(2026, 10, 1), new(2027, 2, 1)),
      "2026-10-31T09:00", "2026-12-31T09:00", "2027-01-31T09:00");

  [TestMethod]
  public void Monthly_OnTheLastDay() =>
    AreDates(Occurrences("FREQ=MONTHLY;BYMONTHDAY=-1", new(2026, 10, 31, 9, 0, 0), new(2026, 10, 1), new(2027, 1, 1)),
      "2026-10-31T09:00", "2026-11-30T09:00", "2026-12-31T09:00");

  [TestMethod]
  public void Monthly_OnTheLastWeekday_UsesBySetPosition() =>
    AreDates(Occurrences("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1", new(2026, 10, 30, 9, 0, 0), new(2026, 10, 1), new(2027, 1, 1)),
      "2026-10-30T09:00", "2026-11-30T09:00", "2026-12-31T09:00");

  [TestMethod]
  public void Monthly_WithoutByParts_UsesTheStartDay() =>
    AreDates(Occurrences("FREQ=MONTHLY;INTERVAL=2;COUNT=3", new(2026, 10, 13, 9, 0, 0), new(2026, 1, 1), new(2028, 1, 1)),
      "2026-10-13T09:00", "2026-12-13T09:00", "2027-02-13T09:00");

  [TestMethod]
  public void Yearly_OnTheFourthThursdayOfNovember() =>
    AreDates(Occurrences("FREQ=YEARLY;BYMONTH=11;BYDAY=4TH", new(2026, 11, 26, 18, 0, 0), new(2026, 1, 1), new(2028, 1, 1)),
      "2026-11-26T18:00", "2027-11-25T18:00");

  [TestMethod]
  public void Yearly_OnTheTwentyNinthOfFebruary_SkipsOtherYears() =>
    AreDates(Occurrences("FREQ=YEARLY", new(2024, 2, 29, 9, 0, 0), new(2024, 1, 1), new(2029, 1, 1)),
      "2024-02-29T09:00", "2028-02-29T09:00");

  [TestMethod]
  public void Yearly_ByMonthDayAndMonth() =>
    AreDates(Occurrences("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=14;COUNT=2", new(2026, 2, 14, 19, 30, 0), new(2026, 1, 1), new(2030, 1, 1)),
      "2026-02-14T19:30", "2027-02-14T19:30");

  #endregion

  #region The recurrence editor

  [TestMethod]
  public void Create_Weekly_RepeatsOnTheStartDay() {
    PixataRecurrenceRule rule = PixataSchedulerRecurrenceEditing.Create(PixataRecurrenceFrequency.Weekly, new(2026, 10, 13), null);
    Assert.AreEqual("FREQ=WEEKLY;BYDAY=TU", rule.ToString());
  }

  [TestMethod]
  public void Create_KeepsTheIntervalAndEndOfThePreviousRule() {
    PixataRecurrenceRule previous = PixataRecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=3;BYDAY=TU;COUNT=4;WKST=SU");
    PixataRecurrenceRule rule = PixataSchedulerRecurrenceEditing.Create(PixataRecurrenceFrequency.Monthly, new(2026, 10, 13), previous);
    Assert.AreEqual("FREQ=MONTHLY;INTERVAL=3;BYMONTHDAY=13;COUNT=4;WKST=SU", rule.ToString());
  }

  [TestMethod]
  public void Create_Yearly_RepeatsOnTheStartDate() =>
    Assert.AreEqual("FREQ=YEARLY;BYMONTHDAY=13;BYMONTH=10", PixataSchedulerRecurrenceEditing.Create(PixataRecurrenceFrequency.Yearly, new(2026, 10, 13), null).ToString());

  [TestMethod]
  public void OrdinalDay_ReadsAnOffsetDay() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=-1FR");
    Assert.AreEqual(new PixataRecurrenceOrdinalDay(-1, "FR"), PixataSchedulerRecurrenceEditing.OrdinalDay(rule));
    Assert.IsFalse(PixataSchedulerRecurrenceEditing.UsesMonthDay(rule));
  }

  [TestMethod]
  public void OrdinalDay_ReadsASetPosition() =>
    Assert.AreEqual(new PixataRecurrenceOrdinalDay(2, PixataSchedulerRecurrenceEditing.WeekendDay),
      PixataSchedulerRecurrenceEditing.OrdinalDay(PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=SA,SU;BYSETPOS=2")));

  [TestMethod]
  public void OrdinalDay_IsNullForAMonthDay() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=14");
    Assert.IsNull(PixataSchedulerRecurrenceEditing.OrdinalDay(rule));
    Assert.IsTrue(PixataSchedulerRecurrenceEditing.UsesMonthDay(rule));
  }

  [TestMethod]
  public void SetOrdinalDay_DayOfTheWeek() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=14");
    PixataSchedulerRecurrenceEditing.SetOrdinalDay(rule, new(2, "TU"));
    Assert.AreEqual("FREQ=MONTHLY;BYDAY=2TU", rule.ToString());
  }

  [TestMethod]
  public void SetOrdinalDay_LastWeekday() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=14");
    PixataSchedulerRecurrenceEditing.SetOrdinalDay(rule, new(-1, PixataSchedulerRecurrenceEditing.Weekday));
    Assert.AreEqual("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1", rule.ToString());
  }

  [TestMethod]
  public void SetMonthDay_ClearsTheOrdinalDay() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1");
    PixataSchedulerRecurrenceEditing.SetMonthDay(rule, 40);
    Assert.AreEqual("FREQ=MONTHLY;BYMONTHDAY=31", rule.ToString());
  }

  [TestMethod]
  public void ToggleDay_AddsInWeekOrderAndKeepsAtLeastOne() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=WE");
    PixataSchedulerRecurrenceEditing.ToggleDay(rule, DayOfWeek.Sunday);
    PixataSchedulerRecurrenceEditing.ToggleDay(rule, DayOfWeek.Monday);
    Assert.AreEqual("FREQ=WEEKLY;BYDAY=MO,WE,SU", rule.ToString());
    PixataSchedulerRecurrenceEditing.ToggleDay(rule, DayOfWeek.Monday);
    PixataSchedulerRecurrenceEditing.ToggleDay(rule, DayOfWeek.Sunday);
    PixataSchedulerRecurrenceEditing.ToggleDay(rule, DayOfWeek.Wednesday);
    Assert.AreEqual("FREQ=WEEKLY;BYDAY=WE", rule.ToString());
  }

  [TestMethod]
  public void Ends_SwitchBetweenCountUntilAndNever() {
    PixataRecurrenceRule rule = PixataRecurrenceRule.Parse("FREQ=DAILY");
    PixataSchedulerRecurrenceEditing.EndAfter(rule, 0);
    Assert.AreEqual("FREQ=DAILY;COUNT=1", rule.ToString());
    PixataSchedulerRecurrenceEditing.EndOn(rule, new(2026, 12, 31, 10, 0, 0));
    Assert.AreEqual("FREQ=DAILY;UNTIL=20261231T235959", rule.ToString());
    PixataSchedulerRecurrenceEditing.EndNever(rule);
    Assert.AreEqual("FREQ=DAILY", rule.ToString());
  }

  [TestMethod]
  [DataRow("FREQ=DAILY", "Every day")]
  [DataRow("FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE", "Every 2 weeks, on Monday, Wednesday")]
  [DataRow("FREQ=MONTHLY;BYDAY=-1FR;COUNT=6", "Every month, on the last Friday, 6 times")]
  [DataRow("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1", "Every month, on the first weekday")]
  [DataRow("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=14", "Every year, in February, on day 14")]
  public void Describe_PutsTheRuleIntoWords(string rule, string expected) =>
    Assert.AreEqual(expected, PixataSchedulerRecurrenceEditing.Describe(PixataRecurrenceRule.Parse(rule), English, new()));

  [TestMethod]
  public void Describe_GivesTheEndDateInTheCulturesFormat() =>
    Assert.AreEqual($"Every month, on day 14, until {new DateTime(2026, 12, 31).ToString(English.DateTimeFormat.LongDatePattern, English)}",
      PixataSchedulerRecurrenceEditing.Describe(PixataRecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=14;UNTIL=20261231T235959"), English, new()));

  #endregion
}
