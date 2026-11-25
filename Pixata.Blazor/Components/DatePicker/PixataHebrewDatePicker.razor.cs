using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A replacement for Pixata.Blazor.TelerikComponents' <c>TelerikHebrewDatePicker</c>, with the same parameters. The calendar
/// shows Gregorian months, with the Hebrew date under each day (<see cref="ShowSecondaryDates"/>), and can shade Shabbos and
/// Yom Tov, other non-working days and bank holidays, each in its own colour, with a tooltip naming the day. The text box
/// shows the Hebrew date, the Gregorian date or both (<see cref="InputDisplay"/>). Binds to a <see cref="DateTime"/> or a
/// <see cref="DateOnly"/>, or a nullable one
/// </summary>
/// <typeparam name="TValue"><see cref="DateTime"/>, <see cref="DateOnly"/>, or a nullable version of either</typeparam>
public partial class PixataHebrewDatePicker<TValue> {
  private static readonly Type UnderlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
  private static readonly string[] PreventedKeys = ["Alt+ArrowDown", "Alt+ArrowUp"];

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The earliest date that can be picked. Defaults to 1 January 1584, near the start of the Hebrew calendar .NET supports
  /// </summary>
  [Parameter]
  public DateTime MinDate { get; set; } = new(1584, 1, 1);

  /// <summary>
  /// The latest date that can be picked. Defaults to 29 September 2238, near the end of the Hebrew calendar .NET supports
  /// </summary>
  [Parameter]
  public DateTime MaxDate { get; set; } = new(2238, 9, 29);

  /// <summary>
  /// The width, as a CSS length. Defaults to 320px, as in <c>TelerikHebrewDatePicker</c>
  /// </summary>
  [Parameter]
  public string? Width { get; set; } = "320px";

  /// <summary>
  /// Shows the Hebrew date (in Hebrew letters) under each day in the calendar
  /// </summary>
  [Parameter]
  public bool ShowSecondaryDates { get; set; }

  /// <summary>
  /// Shows the Hebrew dates under the days in brackets
  /// </summary>
  [Parameter]
  public bool ShowSecondaryDatesInBrackets { get; set; }

  /// <summary>
  /// The calendar always shows Gregorian months, with the Gregorian date as the main one, so this can only be true. It is
  /// here so pages moving from <c>TelerikHebrewDatePicker</c> with <c>GregorianPrimary="true"</c> don't need changing
  /// </summary>
  [Parameter]
  public bool GregorianPrimary { get; set; } = true;

  /// <summary>
  /// Gives each day a tooltip with its Hebrew and Gregorian dates, and the names of any of the days being shaded
  /// </summary>
  [Parameter]
  public bool ShowTooltips { get; set; }

  /// <summary>
  /// What the text box shows: the Hebrew date (the default), the Gregorian date, or both
  /// </summary>
  [Parameter]
  public PixataDateInputDisplay InputDisplay { get; set; } = PixataDateInputDisplay.Hebrew;

  /// <summary>
  /// The .NET format of the Gregorian date in the text box. Defaults to <c>d MMM yyyy</c>
  /// </summary>
  [Parameter]
  public string GregorianFormat { get; set; } = "d MMM yyyy";

  /// <summary>
  /// Shades Shabbos and Yom Tov (as kept outside Israel) with <see cref="ShabbosOrYomTovBgColour"/>
  /// </summary>
  [Parameter]
  public bool IncludeShabbosOrYomTov { get; set; }

  [Parameter]
  public string ShabbosOrYomTovBgColour { get; set; } = "#b1c1d7";

  /// <summary>
  /// Shades Chol Hamoed, Tisha B'Av and Purim with <see cref="OtherNonWorkDayBgColour"/>
  /// </summary>
  [Parameter]
  public bool IncludeOtherNonWorkDays { get; set; }

  [Parameter]
  public string OtherNonWorkDayBgColour { get; set; } = "#d2dae3";

  /// <summary>
  /// Shades the bank holidays of England and Wales with <see cref="BankHolidayBgColour"/>
  /// </summary>
  [Parameter]
  public bool IncludeBankHolidays { get; set; }

  [Parameter]
  public string BankHolidayBgColour { get; set; } = "#d4edda";

  /// <summary>
  /// Which shading a day gets when it is both a bank holiday and a Jewish one. Defaults to
  /// <see cref="PixataBankHolidayClashPriority.HebrewFirst"/>
  /// </summary>
  [Parameter]
  public PixataBankHolidayClashPriority ClashPriority { get; set; } = PixataBankHolidayClashPriority.HebrewFirst;

  /// <summary>
  /// Lines the calendar up with the right-hand edge of the text box, rather than the left
  /// </summary>
  [Parameter]
  public bool OpenOnRight { get; set; }

  /// <summary>
  /// Opens the calendar when the text box gets the focus. The focus stays in the text box; Alt+Down or Space moves it into
  /// the calendar. The calendar also opens when the text box or the button is clicked
  /// </summary>
  [Parameter]
  public bool OpenOnFocus { get; set; }

  /// <summary>
  /// Dates that can't be picked
  /// </summary>
  [Parameter]
  public List<DateTime>? DisabledDates { get; set; }

  [Parameter]
  public bool ShowWeekNumbers { get; set; }

  /// <summary>
  /// When true, the date can be seen but not changed
  /// </summary>
  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// Shows a button that clears the date, for a nullable value. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; } = true;

  /// <summary>
  /// Shown in the text box when there is no date. Defaults to "No date selected", as in <c>TelerikHebrewDatePicker</c>
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; } = "No date selected";

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// A CSS class for the popup
  /// </summary>
  [Parameter]
  public string? PopupClass { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  /// <summary>
  /// The accessible name of the text box and the calendar, when there is no label. Defaults to "Date"
  /// </summary>
  [Parameter]
  public string AriaLabel { get; set; } = "Date";

  /// <summary>
  /// The text of the clear and calendar buttons, for translating. Default to "Clear" and "Open"
  /// </summary>
  [Parameter]
  public string ClearText { get; set; } = "Clear";

  [Parameter]
  public string OpenText { get; set; } = "Open";

  /// <summary>
  /// Raised with the new value when the user picks a different date or clears it
  /// </summary>
  [Parameter]
  public EventCallback<object?> OnChange { get; set; }

  [Parameter]
  public EventCallback OnOpen { get; set; }

  [Parameter]
  public EventCallback OnClose { get; set; }

  private readonly string _uid = Guid.NewGuid().ToString("N");
  private ElementReference _wrapper;
  private ElementReference _input;
  private PixataCalendar? _calendar;
  private bool _open;
  private bool _focusCalendar;
  private bool _keysAttached;
  // True while the picker is putting the focus back in the text box, which shouldn't open the calendar again
  private bool _refocusing;
  private DateTime _calendarDate = DateTime.Today;
  private PixataCalendarView _calendarView = PixataCalendarView.Month;

  private string PopupId =>
    $"pixata-hebrewdatepicker-{_uid}";

  private static bool IsNullable =>
    default(TValue) is null;

  private DateTime EffectiveMin =>
    MinDate < PixataHebrewDates.MinSupportedDate ? PixataHebrewDates.MinSupportedDate : MinDate.Date;

  private DateTime EffectiveMax =>
    MaxDate > PixataHebrewDates.MaxSupportedDate ? PixataHebrewDates.MaxSupportedDate : MaxDate.Date;

  /// <summary>
  /// The date, or null if there isn't one (or it's outside the dates the Hebrew calendar supports, such as an unset
  /// <see cref="DateTime"/>)
  /// </summary>
  private DateTime? CurrentDate {
    get {
      DateTime? date = Value switch {
        DateTime dateTime => dateTime,
        DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
        _ => null
      };
      return date is DateTime d && PixataHebrewDates.IsSupported(d) ? d : null;
    }
  }

  private string Text =>
    CurrentDate is DateTime date
      ? InputDisplay switch {
        PixataDateInputDisplay.Gregorian => Gregorian(date),
        PixataDateInputDisplay.Both => $"{PixataHebrewDates.Format(date)} ({Gregorian(date)})",
        _ => PixataHebrewDates.Format(date)
      }
      : "";

  private string WrapperClass =>
    PixataCss.Classes("pixata-blazor-dateinput", "pixata-blazor-datepicker", "pixata-blazor-hebrewdatepicker",
                      $"pixata-blazor-dateinput-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}",
                      IsInvalid ? "pixata-blazor-dateinput-invalid" : null, Enabled ? null : "pixata-blazor-dateinput-disabled",
                      _open ? "pixata-blazor-datepicker-open" : null, Class);

  private string? WidthStyle =>
    string.IsNullOrWhiteSpace(Width) ? null : $"width: {Width}";

  private string Gregorian(DateTime date) =>
    date.ToString(GregorianFormat, CultureInfo.CurrentCulture);

  protected override void OnInitialized() {
    if (UnderlyingType != typeof(DateTime) && UnderlyingType != typeof(DateOnly)) {
      throw new InvalidOperationException($"PixataHebrewDatePicker can only be used with DateTime, DateOnly or a nullable version of either, not {typeof(TValue).Name}");
    }
  }

  protected override void OnParametersSet() {
    base.OnParametersSet();
    if (!GregorianPrimary) {
      throw new NotSupportedException("PixataHebrewDatePicker always shows Gregorian months, so GregorianPrimary can't be false. Remove it, or set it to true");
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (!_keysAttached && Enabled) {
      _keysAttached = true;
      // Alt+Down and Alt+Up open and close the calendar, so they shouldn't scroll the page too
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", _input, PreventedKeys);
      await PixataJs.InvokeVoid(Js, "skipPopupOnTab", _input);
    }
    if (_open && _focusCalendar && _calendar is not null) {
      _focusCalendar = false;
      await _calendar.FocusAsync();
    }
  }

  /// <summary>
  /// How the calendar shows a day: shaded for the kinds of day wanted, with a tooltip and the Hebrew date under it
  /// </summary>
  private PixataCalendarDayInfo? DayInfo(DateTime date) {
    if (!PixataHebrewDates.IsSupported(date)) {
      return null;
    }
    PixataHebrewDayType type = PixataHebrewDates.DayType(date, IncludeShabbosOrYomTov, IncludeOtherNonWorkDays, IncludeBankHolidays, ClashPriority);
    string letters = PixataHebrewDates.DayInLetters(date);
    return new() {
      BackgroundColour = type switch {
        PixataHebrewDayType.ShabbosOrYomTov => ShabbosOrYomTovBgColour,
        PixataHebrewDayType.OtherNonWorkDay => OtherNonWorkDayBgColour,
        PixataHebrewDayType.BankHoliday => BankHolidayBgColour,
        _ => null
      },
      Class = type switch {
        PixataHebrewDayType.ShabbosOrYomTov => "pixata-blazor-hebrewdatepicker-shabbos-yomtov",
        PixataHebrewDayType.OtherNonWorkDay => "pixata-blazor-hebrewdatepicker-other-non-work-day",
        PixataHebrewDayType.BankHoliday => "pixata-blazor-hebrewdatepicker-bank-holiday",
        _ => null
      },
      Title = ShowTooltips ? Tooltip(date) : null,
      SecondaryText = ShowSecondaryDates ? ShowSecondaryDatesInBrackets ? $"({letters})" : letters : null
    };
  }

  private string Tooltip(DateTime date) {
    string names = PixataHebrewDates.DayNames(date, IncludeShabbosOrYomTov, IncludeOtherNonWorkDays, IncludeBankHolidays, ClashPriority);
    // Each line starts with a left-to-right mark, so the English lines aren't laid out right to left after the Hebrew one
    string dates = $"{PixataHebrewDates.Format(date)}\n\u200E{date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}";
    return names == "" ? dates : $"{dates}\n\u200E{names}";
  }

  /// <summary>
  /// Opens the calendar, with the focus in it
  /// </summary>
  public Task Open() =>
    Open(true);

  private async Task Open(bool focusCalendar) {
    if (_open || !Enabled || ReadOnly) {
      return;
    }
    _calendarDate = Clamp(CurrentDate ?? DateTime.Today);
    _calendarView = PixataCalendarView.Month;
    _focusCalendar = focusCalendar;
    _open = true;
    await OnOpen.InvokeAsync();
    StateHasChanged();
  }

  /// <summary>
  /// Closes the calendar
  /// </summary>
  public async Task Close() {
    if (!_open) {
      return;
    }
    _open = false;
    _focusCalendar = false;
    await OnClose.InvokeAsync();
    StateHasChanged();
  }

  /// <summary>
  /// Moves the keyboard focus to the text box
  /// </summary>
  public async ValueTask FocusAsync() {
    try {
      await _input.FocusAsync();
    } catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) {
      // The picker has gone from the page, or the page has gone
    }
  }

  private DateTime Clamp(DateTime date) =>
    date < EffectiveMin ? EffectiveMin : date > EffectiveMax ? EffectiveMax : date.Date;

  private async Task Toggle() {
    if (_open) {
      await CloseAndFocus();
    } else {
      await Open(true);
    }
  }

  private async Task CloseAndFocus() {
    await Close();
    _refocusing = OpenOnFocus;
    await FocusAsync();
  }

  private async Task OpenFromInput() =>
    await Open(false);

  private async Task OnInputFocus() {
    if (_refocusing) {
      _refocusing = false;
      return;
    }
    if (OpenOnFocus) {
      await Open(false);
    }
  }

  private async Task OnInputKeyDown(KeyboardEventArgs e) {
    if (!Enabled || ReadOnly) {
      return;
    }
    switch (e.Key) {
      case "ArrowDown" when e.AltKey:
      case " ":
        if (_open) {
          // The calendar was opened by a click or the focus, so the focus is still here
          _focusCalendar = true;
        } else {
          await Open(true);
        }
        break;
      case "ArrowUp" when e.AltKey:
      case "Escape":
        await Close();
        break;
      case "Tab":
        await Close();
        break;
      case "Delete":
      case "Backspace":
        if (IsNullable) {
          await SetValue(null);
        }
        break;
    }
  }

  // Tab out of the calendar closes it, as the focus has left the picker
  private async Task PopupKeyDown(KeyboardEventArgs e) {
    if (e.Key == "Tab" && !e.ShiftKey) {
      await Close();
    }
  }

  private async Task Pick(DateTime date) {
    await SetValue(date.Date);
    await CloseAndFocus();
  }

  private async Task Clear() {
    await SetValue(null);
    await Close();
  }

  private async Task SetValue(DateTime? date) {
    TValue value = date is not DateTime d
      ? default!
      : UnderlyingType == typeof(DateOnly) ? (TValue)(object)DateOnly.FromDateTime(d) : (TValue)(object)d;
    if (await UpdateValue(value)) {
      await OnChange.InvokeAsync(value);
    }
  }
}
