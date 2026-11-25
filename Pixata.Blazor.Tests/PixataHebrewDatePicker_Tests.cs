using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataHebrewDatePicker_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  // The cell of a day in the month shown, found by its date in full, which is its tooltip unless the day has one of its own
  private static IElement Day<TComponent>(IRenderedComponent<TComponent> cut, int day) where TComponent : IComponent =>
    cut.FindAll("td.pixata-blazor-calendar-td:not(.pixata-blazor-calendar-other-month)")
      .First(td => td.QuerySelector(".pixata-blazor-calendar-day-text, .pixata-blazor-calendar-link")!.TextContent.Trim().StartsWith(day.ToString(CultureInfo.InvariantCulture))
                   && (td.QuerySelector(".pixata-blazor-calendar-day-text")?.TextContent.Trim() ?? td.TextContent.Trim()) == day.ToString(CultureInfo.InvariantCulture));

  // ---------- PixataHebrewDates ----------

  [TestMethod]
  public void HebrewDates_MonthName_CountsFromTishreiAndNamesBothAdarsInALeapYear() {
    // 5786 is not a leap year, 5784 is
    Assert.AreEqual("תשרי", PixataHebrewDates.MonthName(5786, 1));
    Assert.AreEqual("אדר", PixataHebrewDates.MonthName(5786, 6));
    Assert.AreEqual("ניסן", PixataHebrewDates.MonthName(5786, 7));
    Assert.AreEqual("אדר א", PixataHebrewDates.MonthName(5784, 6));
    Assert.AreEqual("אדר ב", PixataHebrewDates.MonthName(5784, 7));
    Assert.AreEqual("ניסן", PixataHebrewDates.MonthName(5784, 8));
    Assert.AreEqual("אלול", PixataHebrewDates.MonthName(5784, 13));
  }

  [TestMethod]
  public void HebrewDates_Format_GivesTheDayMonthAndYear() {
    // Rosh Hashana 5787
    Assert.AreEqual("\u200E1\u200E תשרי \u200E5787\u200E", PixataHebrewDates.Format(new(2026, 9, 12)));
    Assert.AreEqual("א׳", PixataHebrewDates.DayInLetters(new(2026, 9, 12)));
    Assert.AreEqual("", PixataHebrewDates.Format(new(1, 1, 1)));
  }

  [TestMethod]
  public void HebrewDates_MonthsOf_NamesTheHebrewMonthsAGregorianMonthSpans() {
    Assert.AreEqual("אלול 5786 / תשרי 5787", PixataHebrewDates.MonthsOf(2026, 9));
  }

  [TestMethod]
  public void HebrewDates_YomTovName_NamesTheDaysOfYomTov() {
    Assert.AreEqual("First day Rosh Hashana", PixataHebrewDates.YomTovName(new(2026, 9, 12)));
    Assert.AreEqual("Yom Kippur", PixataHebrewDates.YomTovName(new(2026, 9, 21)));
    Assert.AreEqual("First day Pesach", PixataHebrewDates.YomTovName(new(2026, 4, 2)));
    Assert.AreEqual("Second day Shavuos", PixataHebrewDates.YomTovName(new(2026, 5, 23)));
    // Pesach in a leap year, when Nissan is the eighth month
    Assert.AreEqual("First day Pesach", PixataHebrewDates.YomTovName(new(2024, 4, 23)));
    Assert.IsNull(PixataHebrewDates.YomTovName(new(2026, 9, 14)));
  }

  [TestMethod]
  public void HebrewDates_OtherNonWorkDayName_NamesCholHamoedTishaBavAndPurim() {
    Assert.AreEqual("Chol Hamoed Pesach", PixataHebrewDates.OtherNonWorkDayName(new(2026, 4, 5)));
    Assert.AreEqual("Chol Hamoed Succos", PixataHebrewDates.OtherNonWorkDayName(new(2026, 9, 28)));
    Assert.AreEqual("Purim", PixataHebrewDates.OtherNonWorkDayName(new(2026, 3, 3)));
    // Purim in a leap year is in Adar II
    Assert.AreEqual("Purim", PixataHebrewDates.OtherNonWorkDayName(new(2024, 3, 24)));
    Assert.AreEqual("Tisha B'Av", PixataHebrewDates.OtherNonWorkDayName(new(2026, 7, 23)));
    // In 5785, 9 Av was Shabbos, so the fast was on the Sunday
    Assert.AreEqual("Tisha B'Av", PixataHebrewDates.OtherNonWorkDayName(new(2025, 8, 3)));
    Assert.IsNull(PixataHebrewDates.OtherNonWorkDayName(new(2026, 7, 24)));
  }

  [TestMethod]
  public void HebrewDates_BankHolidayName_NamesTheBankHolidaysOfEnglandAndWales() {
    Assert.AreEqual("Good Friday", PixataHebrewDates.BankHolidayName(new(2026, 4, 3)));
    Assert.AreEqual("Easter Monday", PixataHebrewDates.BankHolidayName(new(2026, 4, 6)));
    Assert.AreEqual("Early May Bank Holiday", PixataHebrewDates.BankHolidayName(new(2026, 5, 4)));
    Assert.AreEqual("Spring Bank Holiday", PixataHebrewDates.BankHolidayName(new(2026, 5, 25)));
    Assert.AreEqual("Summer Bank Holiday", PixataHebrewDates.BankHolidayName(new(2026, 8, 31)));
    // New Year's Day 2022 was a Saturday
    Assert.AreEqual("New Year's Day", PixataHebrewDates.BankHolidayName(new(2022, 1, 3)));
    // Christmas Day 2021 was a Saturday, so the substitutes were Monday and Tuesday
    Assert.AreEqual("Christmas Day", PixataHebrewDates.BankHolidayName(new(2021, 12, 27)));
    Assert.AreEqual("Boxing Day", PixataHebrewDates.BankHolidayName(new(2021, 12, 28)));
    // Christmas Day 2022 was a Sunday, so Boxing Day was the Monday and Christmas Day's substitute the Tuesday
    Assert.AreEqual("Boxing Day", PixataHebrewDates.BankHolidayName(new(2022, 12, 26)));
    Assert.AreEqual("Christmas Day", PixataHebrewDates.BankHolidayName(new(2022, 12, 27)));
    Assert.IsNull(PixataHebrewDates.BankHolidayName(new(2021, 12, 25)));
  }

  [TestMethod]
  public void HebrewDates_DayType_FollowsTheClashPriority() {
    // Easter Monday 2026 was Chol Hamoed Pesach
    DateTime easterMonday = new(2026, 4, 6);
    Assert.AreEqual(PixataHebrewDayType.OtherNonWorkDay, PixataHebrewDates.DayType(easterMonday, true, true, true));
    Assert.AreEqual(PixataHebrewDayType.BankHoliday, PixataHebrewDates.DayType(easterMonday, true, true, true, PixataBankHolidayClashPriority.BankHolidayFirst));
    Assert.AreEqual(PixataHebrewDayType.BankHoliday, PixataHebrewDates.DayType(easterMonday, true, false, true));
    Assert.AreEqual(PixataHebrewDayType.None, PixataHebrewDates.DayType(easterMonday, true, false, false));
    // Good Friday 2026 was the second day of Pesach
    Assert.AreEqual(PixataHebrewDayType.ShabbosOrYomTov, PixataHebrewDates.DayType(new(2026, 4, 3), true, true, true));
    Assert.AreEqual(PixataHebrewDayType.ShabbosOrYomTov, PixataHebrewDates.DayType(new(2026, 4, 4), true, true, true));
    Assert.AreEqual("Chol Hamoed Pesach / Easter Monday", PixataHebrewDates.DayNames(easterMonday, true, true, true));
    Assert.AreEqual("Easter Monday / Chol Hamoed Pesach", PixataHebrewDates.DayNames(easterMonday, true, true, true, PixataBankHolidayClashPriority.BankHolidayFirst));
    Assert.AreEqual("Shabbos / Chol Hamoed Pesach", PixataHebrewDates.DayNames(new(2026, 4, 4), true, true, true));
  }

  // ---------- PixataCalendar's DayInfo ----------

  [TestMethod]
  public void Calendar_DayInfo_ShadesLabelsAndAddsASecondLine() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.DayInfo, date => date.Day == 20
        ? new() { BackgroundColour = "#ffcc00", Class = "holiday", Title = "A holiday", SecondaryText = "x" }
        : null));
    IElement day = Day(calendar, 20);
    Assert.IsTrue(day.ClassList.Contains("pixata-blazor-calendar-shaded"));
    Assert.IsTrue(day.ClassList.Contains("holiday"));
    Assert.AreEqual("--pixata-blazor-calendar-day-bg: #ffcc00;", day.GetAttribute("style"));
    Assert.AreEqual("A holiday", day.GetAttribute("title"));
    Assert.AreEqual("x", day.QuerySelector(".pixata-blazor-calendar-secondary")!.TextContent);
    IElement other = Day(calendar, 21);
    Assert.IsFalse(other.ClassList.Contains("pixata-blazor-calendar-shaded"));
    Assert.IsNull(other.GetAttribute("style"));
    // The long date pattern differs between systems: Windows puts a comma after the day's name, and Linux doesn't
    Assert.AreEqual(new DateTime(2026, 10, 21).ToString("D", CultureInfo.CurrentCulture), other.GetAttribute("title"));
    Assert.IsNull(other.QuerySelector(".pixata-blazor-calendar-secondary"));
  }

  [TestMethod]
  public void DatePicker_DayInfo_IsPassedToTheCalendar() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.DayInfo, date => new() { Title = $"Day {date.Day}" }));
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Assert.AreEqual("Day 20", Day(picker, 20).GetAttribute("title"));
  }

  [TestMethod]
  public async Task DatePicker_OpenOnFocus_OpensTheCalendarAndTabClosesIt() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.OpenOnFocus, true));
    await picker.Find("input").FocusAsync(new FocusEventArgs());
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-calendar").Count);
    await picker.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-calendar").Count);
  }

  [TestMethod]
  public async Task DatePicker_WithoutOpenOnFocus_DoesNotOpenOnFocus() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13)));
    await picker.Find("input").FocusAsync(new FocusEventArgs());
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-calendar").Count);
  }

  // ---------- PixataHebrewDatePicker ----------

  [TestMethod]
  public void HebrewDatePicker_InputDisplay_ShowsTheHebrewTheGregorianOrBoth() {
    DateTime roshHashana = new(2026, 9, 12);
    IRenderedComponent<PixataHebrewDatePicker<DateTime>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime>>(p => p.Add(c => c.Value, roshHashana));
    Assert.AreEqual(PixataHebrewDates.Format(roshHashana), picker.Find("input").GetAttribute("value"));
    Assert.IsTrue(picker.Find("input").HasAttribute("readonly"));
    picker.Render(p => p.Add(c => c.InputDisplay, PixataDateInputDisplay.Gregorian));
    Assert.AreEqual("12 Sept 2026", picker.Find("input").GetAttribute("value")?.Replace("Sep ", "Sept "));
    picker.Render(p => p.Add(c => c.InputDisplay, PixataDateInputDisplay.Both));
    StringAssert.StartsWith(picker.Find("input").GetAttribute("value"), $"{PixataHebrewDates.Format(roshHashana)} (12 Sep");
  }

  [TestMethod]
  public void HebrewDatePicker_NoDate_ShowsThePlaceholder() {
    IRenderedComponent<PixataHebrewDatePicker<DateTime?>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime?>>();
    Assert.AreEqual("", picker.Find("input").GetAttribute("value"));
    Assert.AreEqual("No date selected", picker.Find("input").GetAttribute("placeholder"));
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-dateinput-clear").Count);
  }

  [TestMethod]
  public void HebrewDatePicker_Calendar_ShowsTheHebrewMonthsAndDatesAndShadesTheDays() {
    IRenderedComponent<PixataHebrewDatePicker<DateTime>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 9, 1))
      .Add(c => c.ShowSecondaryDates, true)
      .Add(c => c.ShowTooltips, true)
      .Add(c => c.IncludeShabbosOrYomTov, true)
      .Add(c => c.IncludeOtherNonWorkDays, true)
      .Add(c => c.IncludeBankHolidays, true));
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Assert.AreEqual("אלול 5786 / תשרי 5787", picker.Find(".pixata-blazor-hebrewdatepicker-months").TextContent);
    // Rosh Hashana, on a Shabbos
    IElement roshHashana = Day(picker, 12);
    Assert.AreEqual("א׳", roshHashana.QuerySelector(".pixata-blazor-calendar-secondary")!.TextContent);
    Assert.IsTrue(roshHashana.ClassList.Contains("pixata-blazor-hebrewdatepicker-shabbos-yomtov"));
    Assert.AreEqual("--pixata-blazor-calendar-day-bg: #b1c1d7;", roshHashana.GetAttribute("style"));
    StringAssert.EndsWith(roshHashana.GetAttribute("title"), "Shabbos / First day Rosh Hashana");
    // Chol Hamoed Succos
    Assert.IsTrue(Day(picker, 28).ClassList.Contains("pixata-blazor-hebrewdatepicker-other-non-work-day"));
    Assert.AreEqual("--pixata-blazor-calendar-day-bg: #d2dae3;", Day(picker, 28).GetAttribute("style"));
    // An ordinary Tuesday
    Assert.IsNull(Day(picker, 1).GetAttribute("style"));
  }

  [TestMethod]
  public void HebrewDatePicker_BankHolidays_AreShadedInTheirOwnColour() {
    IRenderedComponent<PixataHebrewDatePicker<DateTime>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 8, 1))
      .Add(c => c.IncludeBankHolidays, true)
      .Add(c => c.BankHolidayBgColour, "pink"));
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Assert.IsTrue(Day(picker, 31).ClassList.Contains("pixata-blazor-hebrewdatepicker-bank-holiday"));
    Assert.AreEqual("--pixata-blazor-calendar-day-bg: pink;", Day(picker, 31).GetAttribute("style"));
  }

  [TestMethod]
  public void HebrewDatePicker_PickingADay_SetsTheValueAndClosesTheCalendar() {
    DateOnly? value = null;
    object? changed = null;
    IRenderedComponent<PixataHebrewDatePicker<DateOnly?>> picker = _ctx.Render<PixataHebrewDatePicker<DateOnly?>>(p => p
      .Add(c => c.Value, new DateOnly(2026, 10, 13))
      .Add(c => c.ValueChanged, (DateOnly? v) => value = v)
      .Add(c => c.OnChange, (object? v) => changed = v));
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Day(picker, 20).Click();
    Assert.AreEqual(new DateOnly(2026, 10, 20), value);
    Assert.AreEqual(new DateOnly(2026, 10, 20), changed);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-calendar").Count);
  }

  [TestMethod]
  public async Task HebrewDatePicker_ClearButtonAndDelete_ClearANullableDate() {
    DateTime? value = new DateTime(2026, 10, 13);
    IRenderedComponent<PixataHebrewDatePicker<DateTime?>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime?>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, (DateTime? v) => value = v));
    picker.Find(".pixata-blazor-dateinput-clear").Click();
    Assert.IsNull(value);
    picker.Render(p => p.Add(c => c.Value, new DateTime(2026, 10, 13)));
    value = new DateTime(2026, 10, 13);
    await picker.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });
    Assert.IsNull(value);
  }

  [TestMethod]
  public async Task HebrewDatePicker_OpenOnFocus_OpensWhenTheTextBoxGetsTheFocus() {
    IRenderedComponent<PixataHebrewDatePicker<DateTime>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.OpenOnFocus, true));
    await picker.Find("input").FocusAsync(new FocusEventArgs());
    Assert.AreEqual("true", picker.Find("input").GetAttribute("aria-expanded"));
    await picker.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual("false", picker.Find("input").GetAttribute("aria-expanded"));
  }

  [TestMethod]
  public async Task HebrewDatePicker_Keyboard_OpensAndClosesTheCalendar() {
    IRenderedComponent<PixataHebrewDatePicker<DateTime>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13)));
    await picker.Find("input").FocusAsync(new FocusEventArgs());
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-calendar").Count);
    await picker.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-calendar").Count);
    await picker.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-calendar").Count);
  }

  [TestMethod]
  public void HebrewDatePicker_Disabled_CannotBeOpened() {
    IRenderedComponent<PixataHebrewDatePicker<DateTime?>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime?>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.Enabled, false));
    Assert.IsTrue(picker.Find("input").HasAttribute("disabled"));
    Assert.IsTrue(picker.Find(".pixata-blazor-datepicker-button").HasAttribute("disabled"));
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-dateinput-clear").Count);
    picker.Find("input").Click();
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-calendar").Count);
  }

  [TestMethod]
  public void HebrewDatePicker_InAnEditForm_ShowsTheInvalidState() {
    Model model = new();
    EditContext editContext = new(model);
    ValidationMessageStore messages = new(editContext);
    IRenderedComponent<PixataHebrewDatePicker<DateTime?>> picker = _ctx.Render<PixataHebrewDatePicker<DateTime?>>(p => p
      .AddCascadingValue(editContext)
      .Add(c => c.Value, model.Date)
      .Add(c => c.ValueChanged, (DateTime? v) => model.Date = v)
      .Add(c => c.ValueExpression, () => model.Date));
    Assert.IsFalse(picker.Find(".pixata-blazor-hebrewdatepicker").ClassList.Contains("pixata-blazor-dateinput-invalid"));
    picker.InvokeAsync(() => {
      messages.Add(editContext.Field(nameof(Model.Date)), "Required");
      editContext.NotifyValidationStateChanged();
    });
    Assert.IsTrue(picker.Find(".pixata-blazor-hebrewdatepicker").ClassList.Contains("pixata-blazor-dateinput-invalid"));
    Assert.AreEqual("true", picker.Find("input").GetAttribute("aria-invalid"));
  }

  [TestMethod]
  public void HebrewDatePicker_GregorianPrimaryFalse_Throws() =>
    Assert.ThrowsException<NotSupportedException>(() => _ctx.Render<PixataHebrewDatePicker<DateTime>>(p => p.Add(c => c.GregorianPrimary, false)));

  [TestMethod]
  public void HebrewDatePicker_OtherTypes_Throw() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataHebrewDatePicker<string>>());

  private class Model {
    public DateTime? Date { get; set; }
  }
}
