using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataTimePicker_Tests {
  private BunitContext _ctx = null!;
  private CultureInfo _culture = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    // The picker calls JavaScript for the input's selection and the popup's position, which doesn't matter here
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    _culture = CultureInfo.CurrentCulture;
    CultureInfo.CurrentCulture = new("en-GB", useUserOverride: false);
  }

  [TestCleanup]
  public void Cleanup() {
    _ctx.Dispose();
    CultureInfo.CurrentCulture = _culture;
  }

  private static string[] Options<TComponent>(IRenderedComponent<TComponent> picker) where TComponent : IComponent =>
    [.. picker.FindAll("[role=option]").Select(o => o.TextContent.Trim())];

  private static IElement Option<TComponent>(IRenderedComponent<TComponent> picker, string text) where TComponent : IComponent =>
    picker.FindAll("[role=option]").Single(o => o.TextContent.Trim() == text);

  private static void TypeKeys<TValue>(IRenderedComponent<PixataTimePicker<TValue>> picker, string keys) {
    picker.InvokeAsync(() => picker.Instance.OnInputFocus()).Wait();
    foreach (char c in keys) {
      picker.InvokeAsync(() => picker.Instance.OnKey(c.ToString())).Wait();
    }
  }

  private IRenderedComponent<PixataTimePicker<TimeOnly?>> Render(Action<TimeOnly?> changed, TimeOnly? value = null,
                                                                 Action<ComponentParameterCollectionBuilder<PixataTimePicker<TimeOnly?>>>? more = null) =>
    _ctx.Render<PixataTimePicker<TimeOnly?>>(p => {
      p.Add(c => c.Value, value)
        .Add(c => c.ValueChanged, EventCallback.Factory.Create<TimeOnly?>(this, t => changed(t)));
      more?.Invoke(p);
    });

  // ---------- Showing and typing a time ----------

  [TestMethod]
  public void Format_DefaultsToTheCulturesShortTime() {
    IRenderedComponent<PixataTimePicker<TimeOnly>> picker = _ctx.Render<PixataTimePicker<TimeOnly>>(p => p.Add(c => c.Value, new TimeOnly(14, 30)));

    Assert.AreEqual("14:30", picker.Find("input").GetAttribute("value"));
  }

  [TestMethod]
  public void Format_CanBeTwelveHour() {
    CultureInfo.CurrentCulture = new("en-US", useUserOverride: false);
    IRenderedComponent<PixataTimePicker<TimeOnly>> picker = _ctx.Render<PixataTimePicker<TimeOnly>>(p => p
      .Add(c => c.Value, new TimeOnly(14, 30))
      .Add(c => c.Format, "h:mm tt"));

    Assert.AreEqual("2:30 PM", picker.Find("input").GetAttribute("value"));
  }

  [TestMethod]
  public void Typing_ATime_SetsATimeOnly() {
    TimeOnly? value = null;
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(t => value = t);

    TypeKeys(picker, "0930");

    Assert.AreEqual(new TimeOnly(9, 30), value);
  }

  [TestMethod]
  public void Typing_IntoADateTime_KeepsTheDate() {
    DateTime value = new(2026, 10, 13, 14, 30, 0);
    IRenderedComponent<PixataTimePicker<DateTime>> picker = _ctx.Render<PixataTimePicker<DateTime>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => value = d)));

    TypeKeys(picker, "0815");

    Assert.AreEqual(new DateTime(2026, 10, 13, 8, 15, 0), value);
  }

  [TestMethod]
  public void ArrowKeys_GoRoundMidnight_WithoutChangingTheDate() {
    DateTime value = new(2026, 10, 13, 23, 30, 0);
    IRenderedComponent<PixataTimePicker<DateTime>> picker = _ctx.Render<PixataTimePicker<DateTime>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => value = d)));

    picker.InvokeAsync(() => picker.Instance.OnInputFocus()).Wait();
    picker.InvokeAsync(() => picker.Instance.OnKey("ArrowUp")).Wait();

    Assert.AreEqual(new DateTime(2026, 10, 13, 0, 30, 0), value);
  }

  [TestMethod]
  public void ArrowKeys_OnTheDefaultDateTime_DoNotThrow() {
    // default(DateTime) is the earliest date there is, so going back an hour from midnight would be out of range
    DateTime value = default;
    IRenderedComponent<PixataTimePicker<DateTime>> picker = _ctx.Render<PixataTimePicker<DateTime>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => value = d)));

    picker.InvokeAsync(() => picker.Instance.OnInputFocus()).Wait();
    picker.InvokeAsync(() => picker.Instance.OnKey("ArrowDown")).Wait();

    Assert.AreEqual(DateTime.MinValue.AddHours(23), value);
  }

  [TestMethod]
  public void Pasting_ATime_KeepsTheDate() {
    DateTime value = new(2026, 10, 13, 14, 30, 0);
    IRenderedComponent<PixataTimePicker<DateTime>> picker = _ctx.Render<PixataTimePicker<DateTime>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => value = d)));

    picker.InvokeAsync(() => picker.Instance.OnPaste("09:15")).Wait();

    Assert.AreEqual(new DateTime(2026, 10, 13, 9, 15, 0), value);
  }

  [TestMethod]
  public void Pasting_TheSameTime_IsNotAChange() {
    int changes = 0;
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => changes++, new TimeOnly(9, 0));

    picker.InvokeAsync(() => picker.Instance.OnPaste("09:00")).Wait();

    Assert.AreEqual(0, changes);
  }

  [TestMethod]
  public void MinAndMax_OnlyCompareTheTime() {
    TimeOnly? value = new TimeOnly(10, 0);
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(t => value = t, value, p => p
      .Add(c => c.Min, new DateTime(1900, 1, 1, 9, 0, 0))
      .Add(c => c.Max, new DateTime(1900, 1, 1, 17, 0, 0)));

    TypeKeys(picker, "1200");
    Assert.AreEqual(new TimeOnly(12, 0), value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-dateinput-invalid").Count);

    TypeKeys(picker, "1800");
    Assert.IsNull(value);
    Assert.AreEqual(1, picker.FindAll(".pixata-blazor-dateinput-invalid").Count);
  }

  [TestMethod]
  public void MinAfterMax_IsARangeOverMidnight() {
    TimeOnly? value = null;
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(t => value = t, null, p => p
      .Add(c => c.Min, new DateTime(1900, 1, 1, 22, 0, 0))
      .Add(c => c.Max, new DateTime(1900, 1, 1, 6, 0, 0)));

    TypeKeys(picker, "0300");
    Assert.AreEqual(new TimeOnly(3, 0), value);

    TypeKeys(picker, "1200");
    Assert.IsNull(value);
  }

  [TestMethod]
  public void DateOnly_Throws() {
    InvalidOperationException ex = Assert.ThrowsException<InvalidOperationException>(() => _ctx.Render<PixataTimePicker<DateOnly>>());

    StringAssert.Contains(ex.Message, "TimeOnly");
  }

  [TestMethod]
  public void ClearButton_OnlyForNullableValues() {
    IRenderedComponent<PixataTimePicker<TimeOnly?>> nullable = Render(_ => { }, new TimeOnly(9, 0), p => p.Add(c => c.ShowClearButton, true));
    IRenderedComponent<PixataTimePicker<TimeOnly>> notNullable = _ctx.Render<PixataTimePicker<TimeOnly>>(p => p
      .Add(c => c.Value, new TimeOnly(9, 0))
      .Add(c => c.ShowClearButton, true));

    Assert.AreEqual(1, nullable.FindAll(".pixata-blazor-dateinput-clear").Count);
    Assert.AreEqual(0, notNullable.FindAll(".pixata-blazor-dateinput-clear").Count);
  }

  [TestMethod]
  public void InAForm_NotifiesTheEditContext() {
    Settings model = new() { DayStarts = new TimeOnly(9, 0) };
    EditContext context = new(model);
    bool notified = false;
    context.OnFieldChanged += (_, e) => notified = e.FieldIdentifier.FieldName == nameof(Settings.DayStarts);
    IRenderedComponent<PixataTimePicker<TimeOnly>> picker = _ctx.Render<PixataTimePicker<TimeOnly>>(p => p
      .AddCascadingValue(context)
      .Add(c => c.Value, model.DayStarts)
      .Add(c => c.ValueExpression, () => model.DayStarts)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<TimeOnly>(this, t => model.DayStarts = t)));

    picker.InvokeAsync(() => picker.Instance.OnInputFocus()).Wait();
    picker.InvokeAsync(() => picker.Instance.OnKey("ArrowUp")).Wait();

    Assert.IsTrue(notified);
    Assert.AreEqual(new TimeOnly(10, 0), model.DayStarts);
  }

  // ---------- The list of times ----------

  [TestMethod]
  public void List_IsEveryHalfHourByDefault() {
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => { });

    picker.Find(".pixata-blazor-datepicker-button").Click();

    string[] options = Options(picker);
    Assert.AreEqual(48, options.Length);
    Assert.AreEqual("00:00", options[0]);
    Assert.AreEqual("00:30", options[1]);
    Assert.AreEqual("23:30", options[^1]);
  }

  [TestMethod]
  public void List_GoesFromMinToMax_StepMinutesApart() {
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => { }, null, p => p
      .Add(c => c.Min, new DateTime(1900, 1, 1, 8, 15, 0))
      .Add(c => c.Max, new DateTime(1900, 1, 1, 10, 0, 0))
      .Add(c => c.Step, 45));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    CollectionAssert.AreEqual(new[] { "08:15", "09:00", "09:45" }, Options(picker));
  }

  [TestMethod]
  public void List_GoesOverMidnight_WhenMinIsAfterMax() =>
    CollectionAssert.AreEqual(new List<TimeSpan> { new(22, 0, 0), new(23, 0, 0), new(0, 0, 0), new(1, 0, 0), new(2, 0, 0) },
                              PixataTimePicker<TimeOnly>.Times(new(22, 0, 0), new(2, 0, 0), 60));

  [TestMethod]
  public void List_UsesTheFormat() {
    CultureInfo.CurrentCulture = new("en-US", useUserOverride: false);
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => { }, null, p => p
      .Add(c => c.Format, "h:mm tt")
      .Add(c => c.Step, 720));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    CollectionAssert.AreEqual(new[] { "12:00 AM", "12:00 PM" }, Options(picker));
  }

  [TestMethod]
  public void List_MarksTheValue_AndStartsThere() {
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => { }, new TimeOnly(9, 30));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    IElement selected = picker.FindAll("[role=option]").Single(o => o.GetAttribute("aria-selected") == "true");
    Assert.AreEqual("09:30", selected.TextContent.Trim());
    Assert.AreEqual("0", selected.GetAttribute("tabindex"));
  }

  [TestMethod]
  public void List_StartsAtTheNearestTime_WhenTheValueIsntInIt() =>
    Assert.AreEqual(1, PixataTimePicker<TimeOnly>.NearestIndex([new(9, 0, 0), new(9, 30, 0), new(10, 0, 0)], new(9, 40, 0)));

  [TestMethod]
  public void List_NearestTime_GoesRoundMidnight() =>
    Assert.AreEqual(0, PixataTimePicker<TimeOnly>.NearestIndex([new(0, 0, 0), new(12, 0, 0)], new(23, 50, 0)));

  [TestMethod]
  public void PickingATime_SetsItAndCloses() {
    TimeOnly? value = new TimeOnly(9, 0);
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(t => value = t, value);

    picker.Find(".pixata-blazor-datepicker-button").Click();
    Option(picker, "13:30").Click();

    Assert.AreEqual(new TimeOnly(13, 30), value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-timepicker-popup").Count);
  }

  [TestMethod]
  public void PickingATime_ForADateTime_KeepsTheDate() {
    DateTime value = new(2026, 10, 13, 14, 30, 0);
    IRenderedComponent<PixataTimePicker<DateTime>> picker = _ctx.Render<PixataTimePicker<DateTime>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime>(this, d => value = d)));

    picker.Find(".pixata-blazor-datepicker-button").Click();
    Option(picker, "09:00").Click();

    Assert.AreEqual(new DateTime(2026, 10, 13, 9, 0, 0), value);
  }

  [TestMethod]
  public void PickingATime_ForANullDateTime_IsToday() {
    DateTime? value = null;
    IRenderedComponent<PixataTimePicker<DateTime?>> picker = _ctx.Render<PixataTimePicker<DateTime?>>(p => p
      .Add(c => c.Value, value)
      .Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => value = d)));

    picker.Find(".pixata-blazor-datepicker-button").Click();
    Option(picker, "09:00").Click();

    Assert.AreEqual(DateTime.Today.AddHours(9), value);
  }

  [TestMethod]
  public void Keyboard_AltDownOpens_ArrowsMove_AndEnterPicks() {
    TimeOnly? value = new TimeOnly(9, 0);
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(t => value = t, value);

    picker.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });
    picker.Find("[role=listbox]").KeyDown("ArrowDown");
    picker.Find("[role=listbox]").KeyDown("ArrowDown");
    Assert.AreEqual("10:00", picker.Find("[role=option][tabindex='0']").TextContent.Trim());
    picker.Find("[role=listbox]").KeyDown("ArrowUp");
    picker.Find("[role=listbox]").KeyDown("Enter");

    Assert.AreEqual(new TimeOnly(9, 30), value);
    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-timepicker-popup").Count);
  }

  [TestMethod]
  public void Keyboard_HomeAndEnd_GoToTheEnds() {
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => { }, new TimeOnly(9, 0));

    picker.Find(".pixata-blazor-datepicker-button").Click();
    picker.Find("[role=listbox]").KeyDown("End");
    Assert.AreEqual("23:30", picker.Find("[role=option][tabindex='0']").TextContent.Trim());
    picker.Find("[role=listbox]").KeyDown("Home");
    Assert.AreEqual("00:00", picker.Find("[role=option][tabindex='0']").TextContent.Trim());
  }

  [TestMethod]
  public void Keyboard_EscapeCloses_WithoutChangingTheValue() {
    int changes = 0;
    IRenderedComponent<PixataTimePicker<TimeOnly?>> picker = Render(_ => changes++, new TimeOnly(9, 0));

    picker.Find(".pixata-blazor-datepicker-button").Click();
    picker.Find("[role=listbox]").KeyDown("ArrowDown");
    picker.Find("[role=listbox]").KeyDown("Escape");

    Assert.AreEqual(0, picker.FindAll(".pixata-blazor-timepicker-popup").Count);
    Assert.AreEqual(0, changes);
  }

  [TestMethod]
  public void Disabled_OrReadOnly_DoesNotOpen() {
    IRenderedComponent<PixataTimePicker<TimeOnly?>> disabled = Render(_ => { }, null, p => p.Add(c => c.Enabled, false));
    IRenderedComponent<PixataTimePicker<TimeOnly?>> readOnly = Render(_ => { }, null, p => p.Add(c => c.ReadOnly, true));

    disabled.InvokeAsync(() => disabled.Instance.Open()).Wait();
    readOnly.InvokeAsync(() => readOnly.Instance.Open()).Wait();

    Assert.AreEqual(0, disabled.FindAll(".pixata-blazor-timepicker-popup").Count);
    Assert.AreEqual(0, readOnly.FindAll(".pixata-blazor-timepicker-popup").Count);
    Assert.IsTrue(disabled.Find("input").HasAttribute("disabled"));
  }

  [TestMethod]
  public void Opening_FocusesTheTimeTheListIsOn() {
    RecordingJsRuntime js = new();
    _ctx.Services.AddSingleton<IJSRuntime>(js);
    IRenderedComponent<PixataTimePicker<TimeOnly>> picker = _ctx.Render<PixataTimePicker<TimeOnly>>(p => p.Add(c => c.Value, new TimeOnly(9, 0)));

    picker.Find(".pixata-blazor-datepicker-button").Click();

    List<string> calls = [.. js.Calls.Select(c => c.Identifier)];
    int open = calls.IndexOf("openPopup");
    Assert.IsTrue(open >= 0);
    Assert.IsTrue(calls.IndexOf("focusSelector") > open);
  }

  [TestMethod]
  public async Task RemovedByTheHandlerForTheTimePicked_DoesNotThrow() {
    RecordingJsRuntime js = new();
    _ctx.Services.AddSingleton<IJSRuntime>(js);
    TaskCompletionSource reloaded = new();
    IRenderedComponent<RemovedOnChangeHost<PixataTimePicker<DateTime>>> host = _ctx.Render<RemovedOnChangeHost<PixataTimePicker<DateTime>>>(p => p
      .Add(c => c.Reload, reloaded.Task));

    host.Find(".pixata-blazor-datepicker-button").Click();
    Task picked = Option(host, "09:00").ClickAsync(new MouseEventArgs());
    Assert.AreEqual(0, host.FindAll(".pixata-blazor-timepicker").Count);
    await host.InvokeAsync(() => reloaded.SetResult());
    await picked;

    Assert.AreEqual(new DateTime(2026, 10, 13, 9, 0, 0), host.Instance.Value);
    Assert.IsTrue(js.Module.Disposed);
    Assert.IsFalse(_ctx.Renderer.UnhandledException.IsCompleted, _ctx.Renderer.UnhandledException.IsCompleted ? _ctx.Renderer.UnhandledException.Result.ToString() : "");
  }

  private class Settings {
    public TimeOnly DayStarts { get; set; }
  }
}
