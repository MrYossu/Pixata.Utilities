using System;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;
using BunitContext = Bunit.TestContext;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataDatePicker_Tests {
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

  private static IElement Day(IRenderedFragment calendar, string day) =>
    calendar.FindAll("td.pixata-blazor-calendar-td:not(.pixata-blazor-calendar-other-month)").First(td => td.TextContent.Trim() == day);

  // ---------- Calendar ----------

  [TestMethod]
  public void Calendar_ShowsSixWeeksStartingOnTheCulturesFirstDay() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.RenderComponent<PixataCalendar>(p => p.Add(c => c.Value, new DateTime(2026, 10, 13)));

    Assert.AreEqual("October 2026", calendar.Find(".pixata-blazor-calendar-title").TextContent.Trim());
    Assert.AreEqual(6, calendar.FindAll("tbody tr").Count);
    Assert.AreEqual("MTWTFSS", string.Concat(calendar.FindAll("th").Select(th => th.TextContent)));
    Assert.AreEqual("28", calendar.Find("tbody td").TextContent.Trim());
    Assert.IsTrue(calendar.Find("tbody td").ClassList.Contains("pixata-blazor-calendar-other-month"));
    Assert.IsTrue(Day(calendar, "13").ClassList.Contains("pixata-blazor-calendar-selected"));
  }

  [TestMethod]
  public void Calendar_ClickingADay_RaisesValueChanged() {
    DateTime? picked = null;
    IRenderedComponent<PixataCalendar> calendar = _ctx.RenderComponent<PixataCalendar>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => picked = d)));

    Day(calendar, "20").Click();

    Assert.AreEqual(new DateTime(2026, 10, 20), picked);
  }

  [TestMethod]
  public void Calendar_TitleGoesUpAndCellsGoBackDown() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.RenderComponent<PixataCalendar>(p => p.Add(c => c.Value, new DateTime(2026, 10, 13)));

    calendar.Find(".pixata-blazor-calendar-title").Click();
    Assert.AreEqual("2026", calendar.Find(".pixata-blazor-calendar-title").TextContent.Trim());
    calendar.Find(".pixata-blazor-calendar-title").Click();
    Assert.AreEqual("2020 - 2029", calendar.Find(".pixata-blazor-calendar-title").TextContent.Trim());
    calendar.Find(".pixata-blazor-calendar-title").Click();
    Assert.AreEqual("2000 - 2090", calendar.Find(".pixata-blazor-calendar-title").TextContent.Trim());
    Assert.IsTrue(calendar.Find(".pixata-blazor-calendar-title").HasAttribute("disabled"));

    calendar.FindAll("td").First(td => td.TextContent.Trim() == "2030").Click();
    calendar.FindAll("td").First(td => td.TextContent.Trim() == "2034").Click();
    calendar.FindAll("td").First(td => td.TextContent.Trim() == "Mar").Click();
    Assert.AreEqual("March 2034", calendar.Find(".pixata-blazor-calendar-title").TextContent.Trim());
  }

  [TestMethod]
  public void Calendar_DatesOutsideTheRangeOrDisabled_CantBePicked() {
    DateTime? picked = null;
    IRenderedComponent<PixataCalendar> calendar = _ctx.RenderComponent<PixataCalendar>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.Min, new DateTime(2026, 10, 5))
      .Add(c => c.Max, new DateTime(2026, 10, 25))
      .Add(c => c.DisabledDates, [new DateTime(2026, 10, 15)])
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => picked = d)));

    Day(calendar, "4").Click();
    Day(calendar, "15").Click();
    Day(calendar, "26").Click();

    Assert.IsNull(picked);
    Assert.IsTrue(Day(calendar, "15").ClassList.Contains("pixata-blazor-calendar-disabled"));
    Assert.IsTrue(calendar.Find(".pixata-blazor-calendar-nav-prev").HasAttribute("disabled"));
    Assert.IsTrue(calendar.Find(".pixata-blazor-calendar-nav-next").HasAttribute("disabled"));
  }

  [TestMethod]
  public void Calendar_Keyboard_MovesAndSelects() {
    DateTime? picked = null;
    IRenderedComponent<PixataCalendar> calendar = _ctx.RenderComponent<PixataCalendar>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 31))
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => picked = d)));
    IElement table = calendar.Find("table");

    table.KeyDown("ArrowRight");
    Assert.AreEqual("November 2026", calendar.Find(".pixata-blazor-calendar-title").TextContent.Trim());
    table.KeyDown("ArrowDown");
    table.KeyDown("PageDown");
    table.KeyDown("Enter");

    Assert.AreEqual(new DateTime(2026, 12, 8), picked);
  }

  [TestMethod]
  public void Calendar_WeekNumbers_AreIso8601() {
    IRenderedComponent<PixataCalendar> calendar = _ctx.RenderComponent<PixataCalendar>(p => p
      .Add(c => c.Value, new DateTime(2026, 1, 1))
      .Add(c => c.ShowWeekNumbers, true));

    // 1st January 2026 is a Thursday, so it's in week 1
    Assert.AreEqual("1", calendar.Find("tbody td.pixata-blazor-calendar-week-number").TextContent.Trim());
  }

  // ---------- Date input ----------

  [TestMethod]
  public void DateInput_TypingACompleteDate_RaisesValueChanged() {
    DateTime? value = null;
    IRenderedComponent<PixataDateInput<DateTime?>> input = _ctx.RenderComponent<PixataDateInput<DateTime?>>(p => p
      .Add(c => c.Format, "dd/MM/yyyy")
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => value = d)));

    foreach (char c in "25122026") {
      input.InvokeAsync(() => input.Instance.OnKey(c.ToString())).Wait();
    }

    Assert.AreEqual(new DateTime(2026, 12, 25), value);
  }

  [TestMethod]
  public void DateInput_ClearingAPart_MakesANullableValueNull() {
    DateTime? value = new DateTime(2026, 3, 1);
    IRenderedComponent<PixataDateInput<DateTime?>> input = _ctx.RenderComponent<PixataDateInput<DateTime?>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => value = d)));

    input.InvokeAsync(() => input.Instance.OnKey("Backspace")).Wait();

    Assert.IsNull(value);
  }

  [TestMethod]
  public void DateInput_ClearingAPart_KeepsANonNullableValueAndShowsInvalid() {
    int changes = 0;
    IRenderedComponent<PixataDateInput<DateTime>> input = _ctx.RenderComponent<PixataDateInput<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 3, 1))
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, _ => changes++)));

    DateInputState state = input.InvokeAsync(() => input.Instance.OnKey("Backspace")).Result;

    Assert.AreEqual(0, changes);
    Assert.AreEqual("dd/03/2026", state.Text);
    Assert.IsTrue(input.Find(".pixata-blazor-dateinput").ClassList.Contains("pixata-blazor-dateinput-invalid"));
  }

  [TestMethod]
  public void DateInput_DateOnly_IsSupported() {
    DateOnly value = new(2026, 3, 1);
    IRenderedComponent<PixataDateInput<DateOnly>> input = _ctx.RenderComponent<PixataDateInput<DateOnly>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateOnly>(this, d => value = d)));

    input.InvokeAsync(() => input.Instance.OnInputFocus()).Wait();
    input.InvokeAsync(() => input.Instance.OnKey("ArrowUp")).Wait();

    Assert.AreEqual(new DateOnly(2026, 3, 2), value);
    Assert.AreEqual("01/03/2026", input.Find("input").GetAttribute("value"));
  }

  [TestMethod]
  public void DateInput_OtherTypes_Throw() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.RenderComponent<PixataDateInput<string>>());

  [TestMethod]
  public void DateInput_OnChange_FiresOnBlurOnlyWhenTheValueChanged() {
    int changes = 0;
    DateTime value = new(2026, 3, 1);
    IRenderedComponent<PixataDateInput<DateTime>> input = _ctx.RenderComponent<PixataDateInput<DateTime>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.OnChange, EventCallback.Factory.Create<object?>(this, _ => changes++)));

    input.InvokeAsync(() => input.Instance.OnInputFocus()).Wait();
    input.InvokeAsync(() => input.Instance.OnInputBlur()).Wait();
    Assert.AreEqual(0, changes);

    input.InvokeAsync(() => input.Instance.OnInputFocus()).Wait();
    input.InvokeAsync(() => input.Instance.OnKey("ArrowUp")).Wait();
    input.InvokeAsync(() => input.Instance.OnInputBlur()).Wait();
    Assert.AreEqual(1, changes);
  }

  [TestMethod]
  public void DateInput_InAForm_NotifiesTheEditContext() {
    Model model = new() { Due = new DateTime(2026, 3, 1) };
    EditContext context = new(model);
    bool notified = false;
    context.OnFieldChanged += (_, e) => notified = e.FieldIdentifier.FieldName == nameof(Model.Due);
    IRenderedComponent<PixataDateInput<DateTime?>> input = _ctx.RenderComponent<PixataDateInput<DateTime?>>(p => p
      .AddCascadingValue(context)
      .Add(c => c.Value, model.Due)
      .Add(c => c.ValueExpression, () => model.Due)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => model.Due = d)));

    input.InvokeAsync(() => input.Instance.OnKey("ArrowUp")).Wait();

    Assert.IsTrue(notified);
    Assert.AreEqual(new DateTime(2026, 3, 2), model.Due);
  }

  private class Model {
    public DateTime? Due { get; set; }
  }

  // ---------- Date picker ----------

  [TestMethod]
  public void DatePicker_ButtonOpensTheCalendarAndPickingADateClosesIt() {
    DateTime? value = new DateTime(2026, 10, 13);
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = _ctx.RenderComponent<PixataDatePicker<DateTime?>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => value = d)));

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-datepicker-popup").Count);

    Day(picker, "20").Click();

    Assert.AreEqual(new DateTime(2026, 10, 20), value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
  }

  [TestMethod]
  public void DatePicker_AltDownOpensAndEscapeCloses() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.RenderComponent<PixataDatePicker<DateTime>>(p => p.Add(c => c.Value, new DateTime(2026, 10, 13)));

    picker.Find("input").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-datepicker-popup").Count);

    picker.Find("table").KeyDown("Escape");
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
  }

  [TestMethod]
  public void DatePicker_ClearButton_OnlyForNullableValues() {
    IRenderedComponent<PixataDatePicker<DateTime?>> nullable = _ctx.RenderComponent<PixataDatePicker<DateTime?>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.ShowClearButton, true));
    IRenderedComponent<PixataDatePicker<DateTime>> notNullable = _ctx.RenderComponent<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.Value, new DateTime(2026, 10, 13))
      .Add(c => c.ShowClearButton, true));

    Assert.AreEqual(1, nullable.FindAll(".pixata-blazor-dateinput-clear").Count);
    Assert.AreEqual(0, notNullable.FindAll(".pixata-blazor-dateinput-clear").Count);
  }
}
