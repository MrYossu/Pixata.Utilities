using System;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataDatePickerShowTime_Tests {
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

  private static IElement Day<TComponent>(IRenderedComponent<TComponent> picker, string day) where TComponent : IComponent =>
    picker.FindAll("td.pixata-blazor-calendar-td:not(.pixata-blazor-calendar-other-month)").First(td => td.TextContent.Trim() == day);

  private static IElement[] Options<TComponent>(IRenderedComponent<TComponent> picker, string list) where TComponent : IComponent =>
    [.. picker.Find($"[role=listbox][aria-label='{list}']").QuerySelectorAll("[role=option]")];

  private static IElement Option<TComponent>(IRenderedComponent<TComponent> picker, string list, string text) where TComponent : IComponent =>
    Options(picker, list).Single(o => o.TextContent.Trim() == text);

  private static string Selected<TComponent>(IRenderedComponent<TComponent> picker, string list) where TComponent : IComponent =>
    Options(picker, list).Single(o => o.GetAttribute("aria-selected") == "true").TextContent.Trim();

  private IRenderedComponent<PixataDatePicker<DateTime?>> Render(Action<DateTime?> changed, DateTime? value = null) =>
    _ctx.Render<PixataDatePicker<DateTime?>>(p => p
      .Add(c => c.ShowTime, true)
      .Add(c => c.Value, value ?? new DateTime(2026, 10, 13, 14, 30, 0))
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => changed(d))));

  [TestMethod]
  public void WithoutShowTime_ThereIsNoTimeSelector() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p.Add(c => c.Value, new DateTime(2026, 10, 13)));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-timeselector").Count);
    Assert.AreEqual("13/10/2026", picker.Find("input").GetAttribute("value"));
  }

  [TestMethod]
  public void ShowTime_DefaultsToTheShortDateAndTimeFormat() {
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(_ => { });

    Assert.AreEqual("13/10/2026 14:30", picker.Find("input").GetAttribute("value"));
  }

  [TestMethod]
  public void ShowTime_KeepsAFormatThatIsSet() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.ShowTime, true)
      .Add(c => c.Format, "dd MMM yyyy HH:mm:ss")
      .Add(c => c.Value, new DateTime(2026, 10, 13, 14, 30, 15)));

    Assert.AreEqual("13 Oct 2026 14:30:15", picker.Find("input").GetAttribute("value"));
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Assert.AreEqual("15", Selected(picker, "Seconds"));
  }

  [TestMethod]
  public void Opening_ShowsTheValuesTime_InTwentyFourHourLists() {
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(_ => { });

    picker.Find(".pixata-blazor-datepicker-button").Click();

    Assert.AreEqual(24, Options(picker, "Hours").Length);
    Assert.AreEqual(60, Options(picker, "Minutes").Length);
    Assert.AreEqual("14", Selected(picker, "Hours"));
    Assert.AreEqual("30", Selected(picker, "Minutes"));
    Assert.AreEqual(0, picker.FindAll("[role=listbox][aria-label='AM/PM']").Count);
    Assert.AreEqual(0, picker.FindAll("[role=listbox][aria-label='Seconds']").Count);
  }

  [TestMethod]
  public void PickingADay_KeepsThePopupOpen_AndSetSetsTheDateAndTime() {
    DateTime? value = null;
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(d => value = d);
    picker.Find(".pixata-blazor-datepicker-button").Click();

    Day(picker, "20").Click();
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
    Assert.IsNull(value);

    Option(picker, "Hours", "09").Click();
    Option(picker, "Minutes", "05").Click();
    picker.FindAll(".pixata-blazor-timeselector-footer button").Single(b => b.TextContent.Trim() == "Set").Click();

    Assert.AreEqual(new DateTime(2026, 10, 20, 9, 5, 0), value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
  }

  [TestMethod]
  public void Cancel_ClosesWithoutChangingTheValue() {
    DateTime? value = null;
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(d => value = d);
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Day(picker, "20").Click();
    Option(picker, "Hours", "09").Click();

    picker.FindAll(".pixata-blazor-timeselector-footer button").Single(b => b.TextContent.Trim() == "Cancel").Click();

    Assert.IsNull(value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
  }

  [TestMethod]
  public void EscapeInTheTimeLists_Cancels() {
    DateTime? value = null;
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(d => value = d);
    picker.Find(".pixata-blazor-datepicker-button").Click();

    picker.Find(".pixata-blazor-timeselector").KeyDown("Escape");

    Assert.IsNull(value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-datepicker-popup").Count);
  }

  [TestMethod]
  public void TwelveHourCulture_HasAnAmPmList() {
    CultureInfo.CurrentCulture = new("en-US", useUserOverride: false);
    DateTime? value = null;
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(d => value = d);

    Assert.AreEqual("10/13/2026 2:30 PM", picker.Find("input").GetAttribute("value"));
    picker.Find(".pixata-blazor-datepicker-button").Click();
    Assert.AreEqual(12, Options(picker, "Hours").Length);
    Assert.AreEqual("12", Options(picker, "Hours")[0].TextContent.Trim());
    Assert.AreEqual("02", Selected(picker, "Hours"));
    Assert.AreEqual("PM", Selected(picker, "AM/PM"));

    Option(picker, "AM/PM", "AM").Click();
    Option(picker, "Hours", "12").Click();
    picker.FindAll(".pixata-blazor-timeselector-footer button").Single(b => b.TextContent.Trim() == "Set").Click();

    Assert.AreEqual(new DateTime(2026, 10, 13, 0, 30, 0), value);
  }

  [TestMethod]
  public void TimeStep_SetsTheGapBetweenMinutes() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.ShowTime, true)
      .Add(c => c.TimeStep, 15)
      .Add(c => c.Value, new DateTime(2026, 10, 13, 14, 30, 0)));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    CollectionAssert.AreEqual(new[] { "00", "15", "30", "45" }, Options(picker, "Minutes").Select(o => o.TextContent.Trim()).ToArray());
  }

  [TestMethod]
  public void Set_KeepsTheTimeWithinMinAndMax() {
    DateTime? value = null;
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = _ctx.Render<PixataDatePicker<DateTime?>>(p => p
      .Add(c => c.ShowTime, true)
      .Add(c => c.Value, new DateTime(2026, 10, 13, 14, 30, 0))
      .Add(c => c.Max, new DateTime(2026, 10, 13, 17, 0, 0))
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => value = d)));
    picker.Find(".pixata-blazor-datepicker-button").Click();

    Option(picker, "Hours", "20").Click();
    picker.FindAll(".pixata-blazor-timeselector-footer button").Single(b => b.TextContent.Trim() == "Set").Click();

    Assert.AreEqual(new DateTime(2026, 10, 13, 17, 0, 0), value);
  }

  [TestMethod]
  public void ButtonText_CanBeTranslated() {
    IRenderedComponent<PixataDatePicker<DateTime>> picker = _ctx.Render<PixataDatePicker<DateTime>>(p => p
      .Add(c => c.ShowTime, true)
      .Add(c => c.SetText, "Définir")
      .Add(c => c.CancelText, "Annuler")
      .Add(c => c.HoursText, "Heures"));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    CollectionAssert.AreEqual(new[] { "Annuler", "Définir" }, picker.FindAll(".pixata-blazor-timeselector-footer button").Select(b => b.TextContent.Trim()).ToArray());
    Assert.AreEqual(1, picker.FindAll("[role=listbox][aria-label='Heures']").Count);
  }

  [TestMethod]
  public void ShowTimeWithDateOnly_Throws() =>
    Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataDatePicker<DateOnly>>(p => p.Add(c => c.ShowTime, true)));

  [TestMethod]
  public void Opening_ScrollsTheSelectedTimesIntoView() {
    IRenderedComponent<PixataDatePicker<DateTime?>> picker = Render(_ => { });

    picker.Find(".pixata-blazor-datepicker-button").Click();

    Assert.AreEqual(1, _ctx.JSInterop.Invocations["scrollSelectedOptionsIntoView"].Count);
  }
}
