using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataDateRangePicker_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    // The components call JavaScript for the input's selection and the popup's position, which doesn't matter here
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  // A day in the given view (0 for the first month shown, 1 for the second)
  private static IElement Day<TComponent>(IRenderedComponent<TComponent> component, string day, int view = 0) where TComponent : IComponent =>
    component.FindAll(".pixata-blazor-calendar-view")[view]
      .QuerySelectorAll("td.pixata-blazor-calendar-td:not(.pixata-blazor-calendar-other-month):not(.pixata-blazor-calendar-week-number)")
      .First(td => td.TextContent.Trim() == day);

  // ---------- Calendar ----------

  [TestMethod]
  public void Calendar_TwoViews_ShowsTwoMonthsWithoutDaysFromOtherMonths() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.Date, new DateTime(2026, 10, 13))
      .Add(c => c.Views, 2));

    Assert.AreEqual("October 2026 - November 2026", calendar.Find(".pixata-blazor-calendar-title-multi").TextContent.Trim());
    Assert.AreEqual("October 2026", calendar.Find(".pixata-blazor-calendar-title-single").TextContent.Trim());
    Assert.AreEqual(2, calendar.FindAll("table").Count);
    Assert.AreEqual("November 2026", calendar.FindAll(".pixata-blazor-calendar-caption")[1].TextContent.Trim());
    Assert.IsTrue(calendar.FindAll(".pixata-blazor-calendar-view")[1].ClassList.Contains("pixata-blazor-calendar-view-extra"));
    Assert.AreEqual(0, calendar.FindAll(".pixata-blazor-calendar-other-month").Count);
  }

  [TestMethod]
  public void Calendar_TwoViews_NextMovesBothOnAMonth() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.Date, new DateTime(2026, 10, 13))
      .Add(c => c.Views, 2));

    calendar.Find(".pixata-blazor-calendar-nav-next").Click();

    Assert.AreEqual("November 2026 - December 2026", calendar.Find(".pixata-blazor-calendar-title-multi").TextContent.Trim());
  }

  [TestMethod]
  public void Calendar_TwoViews_KeyboardMovesIntoTheSecondViewBeforeScrolling() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.Date, new DateTime(2026, 10, 31))
      .Add(c => c.Views, 2));

    calendar.Find("table").KeyDown("ArrowRight");
    Assert.AreEqual("October 2026 - November 2026", calendar.Find(".pixata-blazor-calendar-title-multi").TextContent.Trim());
    Assert.AreEqual("0", calendar.FindAll("table")[1].GetAttribute("tabindex"));

    calendar.Find("table").KeyDown("PageDown");
    Assert.AreEqual("November 2026 - December 2026", calendar.Find(".pixata-blazor-calendar-title-multi").TextContent.Trim());
  }

  [TestMethod]
  public void Calendar_Range_FirstClickPicksTheStartAndTheSecondTheEnd() {
    List<PixataCalendarRangeChangeEventArgs> changes = [];
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.Date, new DateTime(2026, 10, 13))
      .Add(c => c.SelectionMode, PixataCalendarSelectionMode.Range)
      .Add(c => c.RangeChanged, EventCallback.Factory.Create<PixataCalendarRangeChangeEventArgs>(this, changes.Add)));

    Day(calendar, "10").Click();
    Day(calendar, "20").Click();

    CollectionAssert.AreEqual(new List<PixataCalendarRangeChangeEventArgs> {
      new(new DateTime(2026, 10, 10), null),
      new(new DateTime(2026, 10, 10), new DateTime(2026, 10, 20))
    }, changes);
  }

  [TestMethod]
  public void Calendar_Range_MarksTheStartEndAndDaysBetween() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.SelectionMode, PixataCalendarSelectionMode.Range)
      .Add(c => c.RangeStart, new DateTime(2026, 10, 10))
      .Add(c => c.RangeEnd, new DateTime(2026, 10, 13)));

    Assert.IsTrue(Day(calendar, "10").ClassList.Contains("pixata-blazor-calendar-range-start"));
    Assert.IsTrue(Day(calendar, "10").ClassList.Contains("pixata-blazor-calendar-selected"));
    Assert.IsTrue(Day(calendar, "13").ClassList.Contains("pixata-blazor-calendar-range-end"));
    Assert.AreEqual(2, calendar.FindAll(".pixata-blazor-calendar-range-middle").Count);
    Assert.IsTrue(calendar.Find(".pixata-blazor-calendar").ClassList.Contains("pixata-blazor-calendar-range-complete"));
  }

  [TestMethod]
  public void Calendar_Range_AnEndBeforeTheStartStartsAgain() {
    PixataCalendarRangeChangeEventArgs? range = null;
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.SelectionMode, PixataCalendarSelectionMode.Range)
      .Add(c => c.RangeStart, new DateTime(2026, 10, 10))
      .Add(c => c.SelectingRangeEnd, true)
      .Add(c => c.RangeChanged, EventCallback.Factory.Create<PixataCalendarRangeChangeEventArgs>(this, r => range = r)));

    Day(calendar, "5").Click();

    Assert.AreEqual(new PixataCalendarRangeChangeEventArgs(new DateTime(2026, 10, 5), null), range);
  }

  [TestMethod]
  public void Calendar_Range_AllowReverse_SwapsAnEndBeforeTheStart() {
    PixataCalendarRangeChangeEventArgs? range = null;
    IRenderedComponent<PixataCalendar> calendar = _ctx.Render<PixataCalendar>(p => p
      .Add(c => c.SelectionMode, PixataCalendarSelectionMode.Range)
      .Add(c => c.RangeStart, new DateTime(2026, 10, 10))
      .Add(c => c.SelectingRangeEnd, true)
      .Add(c => c.AllowReverse, true)
      .Add(c => c.RangeChanged, EventCallback.Factory.Create<PixataCalendarRangeChangeEventArgs>(this, r => range = r)));

    Day(calendar, "5").Click();

    Assert.AreEqual(new PixataCalendarRangeChangeEventArgs(new DateTime(2026, 10, 5), new DateTime(2026, 10, 10)), range);
  }

  // ---------- Date range picker ----------

  private IRenderedComponent<PixataDateRangePicker<TValue>> RenderPicker<TValue>(TValue start, TValue end, Action<TValue?> setStart, Action<TValue?> setEnd,
                                                                                   Action<ComponentParameterCollectionBuilder<PixataDateRangePicker<TValue>>>? more = null) =>
    _ctx.Render<PixataDateRangePicker<TValue>>(p => {
      p.Add(c => c.StartValue, start)
       .Add(c => c.EndValue, end)
       .Add(c => c.StartValueChanged, EventCallback.Factory.Create<TValue?>(this, setStart))
       .Add(c => c.EndValueChanged, EventCallback.Factory.Create<TValue?>(this, setEnd));
      more?.Invoke(p);
    });

  [TestMethod]
  public void Picker_HasTwoLabelledInputs() {
    IRenderedComponent<PixataDateRangePicker<DateTime?>> picker = RenderPicker<DateTime?>(null, null, _ => { }, _ => { },
      p => p.Add(c => c.StartId, "from").Add(c => c.EndLabel, "To"));

    IReadOnlyList<IElement> inputs = picker.FindAll("input");
    IReadOnlyList<IElement> labels = picker.FindAll("label");
    Assert.AreEqual(2, inputs.Count);
    Assert.AreEqual("from", inputs[0].Id);
    Assert.AreEqual("from", labels[0].GetAttribute("for"));
    Assert.AreEqual("Start", labels[0].TextContent);
    Assert.AreEqual(inputs[1].Id, labels[1].GetAttribute("for"));
    Assert.AreEqual("To", labels[1].TextContent);
    Assert.AreEqual("combobox", inputs[0].GetAttribute("role"));
    Assert.AreEqual("false", inputs[0].GetAttribute("aria-expanded"));
  }

  [TestMethod]
  public void Picker_ClickingAnInputOpensTwoMonths() {
    IRenderedComponent<PixataDateRangePicker<DateTime?>> picker = RenderPicker<DateTime?>(new DateTime(2026, 10, 13), null, _ => { }, _ => { });

    picker.Find(".pixata-blazor-daterangepicker-start").Click();

    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-daterangepicker-popup").Count);
    Assert.AreEqual(2, picker.FindAll(".pixata-blazor-calendar-view").Count);
    Assert.AreEqual("true", picker.Find("input").GetAttribute("aria-expanded"));
    Assert.AreEqual("October 2026 - November 2026", picker.Find(".pixata-blazor-calendar-title-multi").TextContent.Trim());
  }

  [TestMethod]
  public void Picker_PickingTheStartThenTheEnd_SetsBothAndCloses() {
    DateTime? start = null;
    DateTime? end = null;
    PixataDateRangePickerChangeEventArgs? change = null;
    int opened = 0;
    int closed = 0;
    IRenderedComponent<PixataDateRangePicker<DateTime?>> picker = RenderPicker<DateTime?>(null, null, d => start = d, d => end = d, p => p
      .Add(c => c.OnChange, EventCallback.Factory.Create<PixataDateRangePickerChangeEventArgs>(this, e => change = e))
      .Add(c => c.OnOpen, () => opened++)
      .Add(c => c.OnClose, () => closed++));

    picker.Find(".pixata-blazor-daterangepicker-start").Click();
    IElement anyMonthDay = picker.FindAll(".pixata-blazor-calendar-view")[0].QuerySelector("td.pixata-blazor-calendar-td:not(.pixata-blazor-calendar-empty)")!;
    string month = picker.FindAll(".pixata-blazor-calendar-caption")[0].TextContent.Trim();
    DateTime first = DateTime.ParseExact(month, "MMMM yyyy", CultureInfo.CurrentCulture);
    Assert.AreEqual("1", anyMonthDay.TextContent.Trim());

    Day(picker, "3").Click();
    Assert.AreEqual(first.AddDays(2), start);
    Assert.IsNull(end);
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-daterangepicker-popup").Count);
    Assert.IsNull(change);

    Day(picker, "4", 1).Click();
    Assert.AreEqual(first.AddMonths(1).AddDays(3), end);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-daterangepicker-popup").Count);
    Assert.AreEqual(new PixataDateRangePickerChangeEventArgs(start, end), change);
    Assert.AreEqual(1, opened);
    Assert.AreEqual(1, closed);
  }

  [TestMethod]
  public void Picker_ClickingTheEndInput_PicksJustTheEnd() {
    DateTime? start = new DateTime(2026, 10, 5);
    DateTime? end = new DateTime(2026, 10, 10);
    IRenderedComponent<PixataDateRangePicker<DateTime?>> picker = RenderPicker(start, end, d => start = d, d => end = d);

    picker.Find(".pixata-blazor-daterangepicker-end").Click();
    Day(picker, "20").Click();

    Assert.AreEqual(new DateTime(2026, 10, 5), start);
    Assert.AreEqual(new DateTime(2026, 10, 20), end);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-daterangepicker-popup").Count);
  }

  [TestMethod]
  public void Picker_DateOnly_IsSupported() {
    DateOnly start = new(2026, 10, 5);
    DateOnly end = new(2026, 10, 10);
    IRenderedComponent<PixataDateRangePicker<DateOnly>> picker = RenderPicker(start, end, d => start = d, d => end = d);

    picker.Find(".pixata-blazor-daterangepicker-start").Click();
    Day(picker, "7").Click();
    Day(picker, "9").Click();

    Assert.AreEqual(new DateOnly(2026, 10, 7), start);
    Assert.AreEqual(new DateOnly(2026, 10, 9), end);
    Assert.AreEqual(new DateOnly(2026, 10, 7), picker.FindComponents<PixataDateInput<DateOnly>>()[0].Instance.Value);
  }

  [TestMethod]
  public void Picker_NonNullable_KeepsTheEndWhenANewStartIsAfterIt() {
    DateTime start = new(2026, 10, 5);
    DateTime end = new(2026, 10, 10);
    int endChanges = 0;
    IRenderedComponent<PixataDateRangePicker<DateTime>> picker = RenderPicker(start, end, d => start = d, _ => endChanges++);

    picker.Find(".pixata-blazor-daterangepicker-start").Click();
    Day(picker, "20").Click();

    Assert.AreEqual(new DateTime(2026, 10, 20), start);
    Assert.AreEqual(0, endChanges);
  }

  [TestMethod]
  public void Picker_AltDownOpensAndEscapeCloses() {
    IRenderedComponent<PixataDateRangePicker<DateTime?>> picker = RenderPicker<DateTime?>(new DateTime(2026, 10, 13), null, _ => { }, _ => { });

    picker.FindAll("input")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-daterangepicker-popup").Count);

    picker.Find("table").KeyDown("Escape");
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-daterangepicker-popup").Count);
  }

  [TestMethod]
  public void Picker_DisabledOrReadOnly_DoesNotOpen() {
    IRenderedComponent<PixataDateRangePicker<DateTime?>> disabled = RenderPicker<DateTime?>(null, null, _ => { }, _ => { }, p => p.Add(c => c.Enabled, false));
    IRenderedComponent<PixataDateRangePicker<DateTime?>> readOnly = RenderPicker<DateTime?>(null, null, _ => { }, _ => { }, p => p.Add(c => c.ReadOnly, true));

    disabled.Find(".pixata-blazor-daterangepicker-start").Click();
    readOnly.Find(".pixata-blazor-daterangepicker-start").Click();

    Assert.AreEqual(0, disabled.FindAll(".pixata-blazor-daterangepicker-popup").Count);
    Assert.AreEqual(0, readOnly.FindAll(".pixata-blazor-daterangepicker-popup").Count);
    Assert.IsTrue(disabled.FindAll("input").All(i => i.HasAttribute("disabled")));
  }

  [TestMethod]
  public void Picker_InAForm_NotifiesTheEditContextForBothFields() {
    Booking model = new() { From = new DateTime(2026, 10, 5), To = new DateTime(2026, 10, 10) };
    EditContext context = new(model);
    List<string> notified = [];
    context.OnFieldChanged += (_, e) => notified.Add(e.FieldIdentifier.FieldName);
    IRenderedComponent<PixataDateRangePicker<DateTime?>> picker = _ctx.Render<PixataDateRangePicker<DateTime?>>(p => p
      .AddCascadingValue(context)
      .Add(c => c.StartValue, model.From)
      .Add(c => c.StartValueExpression, () => model.From)
      .Add(c => c.StartValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => model.From = d))
      .Add(c => c.EndValue, model.To)
      .Add(c => c.EndValueExpression, () => model.To)
      .Add(c => c.EndValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => model.To = d)));

    picker.Find(".pixata-blazor-daterangepicker-start").Click();
    Day(picker, "7").Click();
    Day(picker, "9").Click();

    CollectionAssert.AreEqual(new List<string> { nameof(Booking.From), nameof(Booking.To) }, notified);
    Assert.AreEqual(new DateTime(2026, 10, 7), model.From);
    Assert.AreEqual(new DateTime(2026, 10, 9), model.To);
  }

  [TestMethod]
  public void Picker_OtherTypes_Throw() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataDateRangePicker<string>>());

  private class Booking {
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
  }
}
