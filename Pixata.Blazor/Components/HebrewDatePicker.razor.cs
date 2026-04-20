using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Pixata.Blazor.Components;

public partial class HebrewDatePicker<TValue> {
  [Parameter]
  public TValue? Value { get; set; }

  [Parameter]
  public EventCallback<TValue> ValueChanged { get; set; }

  [Parameter]
  public DateTime MinDate { get; set; } = new(1584, 1, 1);

  [Parameter]
  public DateTime MaxDate { get; set; } = new(2238, 9, 29);

  [Parameter]
  public string Class { get; set; } = "";

  [Parameter]
  public string Width { get; set; } = "320px";

  [Parameter]
  public bool ShowDiagnostics { get; set; }

  [Parameter]
  public bool ShowGregorianDates { get; set; }

  [Parameter]
  public bool GregorianPrimary { get; set; }

  [Parameter]
  public bool ShowSecondaryDatesInBrackets { get; set; }

  [Parameter]
  public bool ShowTooltips { get; set; }

  [Parameter]
  public DateInputDisplay InputDisplay { get; set; } = DateInputDisplay.Hebrew;

  [Parameter]
  public bool IncludeShabbosOrYomTov { get; set; }

  [Parameter]
  public string ShabbosOrYomTovBgColour { get; set; }= "#b1c1d7";

  [Parameter]
  public bool IncludeOtherNonWorkDays { get; set; }

  [Parameter]
  public string OtherNonWorkDayBgColour { get; set; } = "#d2dae3";

  private readonly HebrewCalendar _hc = new();
  private int _hebrewYear;
  private int _hebrewMonth;
  private List<(int Number, string Name)> Months { get; set; } = [];
  private List<int> Years { get; set; } = [];

  private bool _calendarOpen;
  private string _textValue = "";
  private int _displayHebrewYear;
  private int _displayHebrewMonth;
  private string _displayHebrewMonthName = "";
  private int _displayGregorianYear;
  private int _displayGregorianMonth;
  private List<int> GregorianYears { get; set; } = [];

  private int DisplayGregorianYear {
    get => _displayGregorianYear;
    set {
      if (_displayGregorianYear == value) {
        return;
      }
      _displayGregorianYear = value;
    }
  }

  private int DisplayHebrewYear {
    get => _displayHebrewYear;
    set {
      if (_displayHebrewYear == value) {
        return;
      }
      _displayHebrewYear = value;
      int monthsInYear = _hc.GetMonthsInYear(_displayHebrewYear);
      if (_displayHebrewMonth > monthsInYear) {
        _displayHebrewMonth = monthsInYear;
      }
      BuildMonths();
      UpdateDisplayMonthName();
    }
  }

  private string GregorianMonthsLabel {
    get {
      DateTime firstDay = _hc.ToDateTime(_displayHebrewYear, _displayHebrewMonth, 1, 0, 0, 0, 0);
      int daysInMonth = _hc.GetDaysInMonth(_displayHebrewYear, _displayHebrewMonth);
      DateTime lastDay = _hc.ToDateTime(_displayHebrewYear, _displayHebrewMonth, daysInMonth, 0, 0, 0, 0);
      return firstDay.Month == lastDay.Month
        ? firstDay.ToString("MMMM yyyy")
        : $"{firstDay:MMMM yyyy} / {lastDay:MMMM yyyy}";
    }
  }

  private string HebrewMonthsLabel {
    get {
      DateTime firstDay = new(_displayGregorianYear, _displayGregorianMonth, 1);
      DateTime lastDay = new(_displayGregorianYear, _displayGregorianMonth, DateTime.DaysInMonth(_displayGregorianYear, _displayGregorianMonth));
      int firstHebYear = _hc.GetYear(firstDay);
      int firstHebMonth = _hc.GetMonth(firstDay);
      int lastHebYear = _hc.GetYear(lastDay);
      int lastHebMonth = _hc.GetMonth(lastDay);
      string firstName = GetHebrewMonthName(firstHebYear, firstHebMonth);
      if (firstHebMonth == lastHebMonth && firstHebYear == lastHebYear) {
        return $"{firstName} {firstHebYear}";
      }
      string lastName = GetHebrewMonthName(lastHebYear, lastHebMonth);
      return $"{firstName} {firstHebYear} / {lastName} {lastHebYear}";
    }
  }

  protected override void OnInitialized() {
    Type targetType = typeof(TValue);
    if (targetType != typeof(DateTime) && targetType != typeof(DateTime?)) {
      throw new InvalidOperationException($"HebrewDatePicker only supports DateTime or DateTime? types. It cannot be used with {targetType.Name}");
    }
  }

  protected override void OnParametersSet() {
    base.OnParametersSet();
    InitializeFromValue();
  }

  private string Diagnostics() {
    DateTime src = SafeCurrentDate();
    int hy = _hc.GetYear(src);
    int hm = _hc.GetMonth(src);
    int hd = _hc.GetDayOfMonth(src);
    string mapped = Months.FirstOrDefault(m => m.Number == hm).Name ?? GetHebrewMonthName(hy, hm);
    return $"Source: {src:yyyy-MM-dd} -> Hebrew: {hd}/{hm}/{hy} -> mapped name: {mapped}";
  }

  private void InitializeFromValue() {
    DateTime? sourceOpt = GetCurrentValue();
    DateTime sourceForDisplay = sourceOpt ?? DateTime.Today;
    _hebrewYear = _hc.GetYear(sourceForDisplay);
    _hebrewMonth = _hc.GetMonth(sourceForDisplay);
    _displayHebrewYear = _hebrewYear;
    _displayHebrewMonth = _hebrewMonth;
    _displayGregorianYear = sourceForDisplay.Year;
    _displayGregorianMonth = sourceForDisplay.Month;
    BuildYearRange();
    BuildGregorianYearRange();
    BuildMonths();
    UpdateDisplayMonthName();
    _textValue = sourceOpt.HasValue ? FormatDisplayDate(sourceOpt.Value) : "No date selected";
  }

  private void BuildYearRange() {
    int minYear = _hc.GetYear(MinDate);
    int maxYear = _hc.GetYear(MaxDate);
    if (minYear > maxYear) {
      (minYear, maxYear) = (maxYear, minYear);
    }
    Years = [];
    for (int y = minYear; y <= maxYear; y++) {
      Years.Add(y);
    }
    if (!Years.Contains(_displayHebrewYear)) {
      Years.Add(_displayHebrewYear);
      Years.Sort();
    }
  }

  private void BuildGregorianYearRange() {
    GregorianYears = [];
    for (int y = MinDate.Year; y <= MaxDate.Year; y++) {
      GregorianYears.Add(y);
    }
    if (!GregorianYears.Contains(_displayGregorianYear)) {
      GregorianYears.Add(_displayGregorianYear);
      GregorianYears.Sort();
    }
  }

  private void BuildMonths() {
    Months = [];
    int monthsInYear = _hc.GetMonthsInYear(_displayHebrewYear);
    // Tishri-based ordering: 1 == Tishri
    if (monthsInYear == 13) {
      Months.Add((1, "\u05EA\u05E9\u05E8\u05D9")); //תשרי
      Months.Add((2, "\u05D7\u05E9\u05D5\u05DF")); //חשון
      Months.Add((3, "\u05DB\u05E1\u05DC\u05D5")); //כסלו
      Months.Add((4, "\u05D8\u05D1\u05EA")); //טבת
      Months.Add((5, "\u05E9\u05D1\u05D8")); //שבט
      Months.Add((6, "\u05D0\u05D3\u05E8 \u05D0")); //אדר א
      Months.Add((7, "\u05D0\u05D3\u05E8 \u05D1")); //אדר ב
      Months.Add((8, "\u05E0\u05D9\u05E1\u05DF")); //ניסן
      Months.Add((9, "\u05D0\u05D9\u05D9\u05E8")); //אייר
      Months.Add((10, "\u05E1\u05D9\u05D5\u05DF")); //סיון
      Months.Add((11, "\u05EA\u05DE\u05D5\u05D6")); //תמוז
      Months.Add((12, "\u05D0\u05D1")); //אב
      Months.Add((13, "\u05D0\u05DC\u05D5\u05DC")); //אלול
    } else {
      Months.Add((1, "\u05EA\u05E9\u05E8\u05D9")); //תשרי
      Months.Add((2, "\u05D7\u05E9\u05D5\u05DF")); //חשון
      Months.Add((3, "\u05DB\u05E1\u05DC\u05D5")); //כסלו
      Months.Add((4, "\u05D8\u05D1\u05EA")); //טבת
      Months.Add((5, "\u05E9\u05D1\u05D8")); //שבט
      Months.Add((6, "\u05D0\u05D3\u05E8")); //אדר
      Months.Add((7, "\u05E0\u05D9\u05E1\u05DF")); //ניסן
      Months.Add((8, "\u05D0\u05D9\u05D9\u05E8")); //אייר
      Months.Add((9, "\u05E1\u05D9\u05D5\u05DF")); //סיון
      Months.Add((10, "\u05EA\u05DE\u05D5\u05D6")); //תמוז
      Months.Add((11, "\u05D0\u05D1")); //אב
      Months.Add((12, "\u05D0\u05DC\u05D5\u05DC")); //אלול
    }
  }

  // days are computed per-month when rendering; no persistent Days collection required

  private void UpdateDisplayMonthName() =>
    _displayHebrewMonthName = Months.FirstOrDefault(m => m.Number == _displayHebrewMonth).Name ?? GetHebrewMonthName(_displayHebrewYear, _displayHebrewMonth);

  private string FormatDisplayDate(DateTime gregorian) =>
    InputDisplay switch {
      DateInputDisplay.Gregorian => $"{gregorian:d MMM yyyy}",
      DateInputDisplay.Both => $"{FormatHebrewDate(gregorian)} ({gregorian:d MMM yyyy})",
      _ => FormatHebrewDate(gregorian),
    };

  private string FormatHebrewDate(DateTime gregorian) {
    const char lrm = '\u200E';
    int hy = _hc.GetYear(gregorian);
    int hm = _hc.GetMonth(gregorian);
    int hd = _hc.GetDayOfMonth(gregorian);
    string monthName = Months.FirstOrDefault(m => m.Number == hm).Name ?? GetHebrewMonthName(hy, hm);
    return $"{lrm}{hd}{lrm} {monthName} {lrm}{hy}{lrm}";
  }

  private string GetHebrewMonthName(int year, int month) {
    int monthsInYear = _hc.GetMonthsInYear(year);
    if (monthsInYear == 13) {
      switch (month) {
        case 6:
          return "אדר א";
        case 7:
          return "אדר ב";
      }
    }
    return month switch {
      1 => "תשרי",
      2 => "חשון",
      3 => "כסלו",
      4 => "טבת",
      5 => "שבט",
      6 => "אדר",
      7 => "ניסן",
      8 => "אייר",
      9 => "סיון",
      10 => "תמוז",
      11 => "אב",
      12 => "אלול",
      _ => $"חודש {month}",
    };
  }

  private void ToggleCalendar() {
    _calendarOpen = !_calendarOpen;
    if (_calendarOpen) {
      DateTime refDate = GetCurrentValue() ?? DateTime.Today;
      _displayHebrewYear = _hc.GetYear(refDate);
      _displayHebrewMonth = _hc.GetMonth(refDate);
      _displayGregorianYear = refDate.Year;
      _displayGregorianMonth = refDate.Month;
      BuildMonths();
      UpdateDisplayMonthName();
      _textValue = GetCurrentValue().HasValue ? FormatDisplayDate(refDate) : "No date selected";
    }
  }

  private void CloseCalendar() =>
    _calendarOpen = false;

  private void PrevHebrewMonth() {
    if (_displayHebrewMonth > 1) {
      _displayHebrewMonth -= 1;
    } else {
      _displayHebrewYear -= 1;
      _displayHebrewMonth = _hc.GetMonthsInYear(_displayHebrewYear);
    }
    BuildMonths();
    UpdateDisplayMonthName();
  }

  private void NextHebrewMonth() {
    int monthsInYear = _hc.GetMonthsInYear(_displayHebrewYear);
    if (_displayHebrewMonth < monthsInYear) {
      _displayHebrewMonth += 1;
    } else {
      _displayHebrewMonth = 1;
      _displayHebrewYear += 1;
    }
    BuildMonths();
    UpdateDisplayMonthName();
  }

  private void PrevGregorianMonth() {
    if (_displayGregorianMonth > 1) {
      _displayGregorianMonth -= 1;
    } else {
      _displayGregorianMonth = 12;
      _displayGregorianYear -= 1;
    }
  }

  private void NextGregorianMonth() {
    if (_displayGregorianMonth < 12) {
      _displayGregorianMonth += 1;
    } else {
      _displayGregorianMonth = 1;
      _displayGregorianYear += 1;
    }
  }

  private List<int?[]> HebrewCalendarWeeks() {
    int daysInMonth = _hc.GetDaysInMonth(_displayHebrewYear, _displayHebrewMonth);
    List<int?> cells = [];
    DateTime firstG = _hc.ToDateTime(_displayHebrewYear, _displayHebrewMonth, 1, 0, 0, 0, 0);
    int dow = (int)firstG.DayOfWeek; // Sunday=0
    for (int i = 0; i < dow; i++) {
      cells.Add(null);
    }
    for (int d = 1; d <= daysInMonth; d++) {
      cells.Add(d);
    }
    while (cells.Count % 7 != 0) {
      cells.Add(null);
    }
    List<int?[]> weeks = [];
    for (int i = 0; i < cells.Count; i += 7) {
      int?[] week = new int?[7];
      for (int j = 0; j < 7; j++) {
        week[j] = cells[i + j];
      }
      weeks.Add(week);
    }
    return weeks;
  }

  private List<int?[]> GregorianCalendarWeeks() {
    int daysInMonth = DateTime.DaysInMonth(_displayGregorianYear, _displayGregorianMonth);
    List<int?> cells = [];
    DateTime firstG = new(_displayGregorianYear, _displayGregorianMonth, 1);
    int dow = (int)firstG.DayOfWeek;
    for (int i = 0; i < dow; i++) {
      cells.Add(null);
    }
    for (int d = 1; d <= daysInMonth; d++) {
      cells.Add(d);
    }
    while (cells.Count % 7 != 0) {
      cells.Add(null);
    }
    List<int?[]> weeks = [];
    for (int i = 0; i < cells.Count; i += 7) {
      int?[] week = new int?[7];
      for (int j = 0; j < 7; j++) {
        week[j] = cells[i + j];
      }
      weeks.Add(week);
    }
    return weeks;
  }

  private async Task SelectGregorianDay(int gregDay) {
    DateTime dt = new(_displayGregorianYear, _displayGregorianMonth, gregDay);
    await SetValue(dt);
    _textValue = FormatDisplayDate(dt);
    InitializeFromValue();
    _calendarOpen = false;
  }

  private async Task SelectHebrewDay(int hebDay) {
    DateTime dt = _hc.ToDateTime(_displayHebrewYear, _displayHebrewMonth, hebDay, 0, 0, 0, 0);
    await SetValue(dt);
    _textValue = FormatDisplayDate(dt);
    InitializeFromValue();
    _calendarOpen = false;
  }

  private async Task SelectToday() {
    DateTime dt = DateTime.Today;
    await SetValue(dt);
    _textValue = FormatDisplayDate(dt);
    InitializeFromValue();
    _calendarOpen = false;
  }

  private async Task ClearSelection() {
    await SetValue(null);
    _textValue = "No date selected";
    InitializeFromValue();
    _calendarOpen = false;
  }

  private DateTime? GetCurrentValue() {
    if (typeof(TValue) == typeof(DateTime)) {
      // If TValue is DateTime, then Value should never null, so we should be safe using !
      return (DateTime)(object)Value!;
    }
    return typeof(TValue) == typeof(DateTime?) ? Value is null ? null : (DateTime?)(object)Value : null;
  }

  private DateTime SafeCurrentDate() {
    DateTime candidate = GetCurrentValue() ?? DateTime.Today;
    if (candidate == default) {
      candidate = DateTime.Today;
    }
    DateTime minSupported = new(1583, 1, 1);
    DateTime maxSupported = new(2239, 9, 29, 23, 59, 59);
    DateTime minBound = MinDate > minSupported ? MinDate : minSupported;
    DateTime maxBound = MaxDate < maxSupported ? MaxDate : maxSupported;
    if (candidate < minBound) {
      return minBound;
    }
    return candidate > maxBound ? maxBound : candidate;
  }

  private bool IsSelectedDate(DateTime date) {
    DateTime? cur = GetCurrentValue();
    return cur.HasValue && cur.Value.Date == date.Date;
  }

  private async Task SetValue(DateTime? newValue) {
    if (typeof(TValue) == typeof(DateTime)) {
      Value = (TValue)(object)(newValue ?? default(DateTime));
      await ValueChanged.InvokeAsync(Value);
      return;
    }
    if (typeof(TValue) == typeof(DateTime?)) {
      Value = (TValue?)(object?)newValue;
      await ValueChanged.InvokeAsync(Value);
    }
  }

  private HebrewDateType GetHebrewDateType(int hebrewYear, int hebrewMonth, int hebrewDay, DateTime gregorianDate) {
    if (IncludeShabbosOrYomTov && (gregorianDate.DayOfWeek == DayOfWeek.Saturday || IsYomTov(hebrewYear, hebrewMonth, hebrewDay))) {
      return HebrewDateType.ShabbosOrYomTov;
    }
    return IncludeOtherNonWorkDays && IsOtherNonWorkDay(hebrewYear, hebrewMonth, hebrewDay)
      ? HebrewDateType.OtherNonWorkDay
      : HebrewDateType.None;
  }

  private bool IsYomTov(int hebrewYear, int hebrewMonth, int hebrewDay) {
    int nissanMonth = GetNissanMonth(hebrewYear);
    int sivanMonth = nissanMonth + 2;
    if (hebrewMonth == nissanMonth && (hebrewDay == 15 || hebrewDay == 16 || hebrewDay == 21 || hebrewDay == 22)) {
      return true; // Pesach (diaspora)
    }
    if (hebrewMonth == sivanMonth && (hebrewDay == 6 || hebrewDay == 7)) {
      return true; // Shavuos (diaspora)
    }
    if (hebrewMonth != 1) {
      return false;
    }
    return hebrewDay is 1   // Rosh Hashona day 1
      or 2     // Rosh Hashona day 2
      or 10    // Yom Kippur
      or 15    // Succos day 1
      or 16    // Succos day 2
      or 22    // Shemini Atzeres
      or 23;   // Simchas Torah
  }

  private bool IsOtherNonWorkDay(int hebrewYear, int hebrewMonth, int hebrewDay) {
    int nissanMonth = GetNissanMonth(hebrewYear);
    if (hebrewMonth == nissanMonth && hebrewDay >= 17 && hebrewDay <= 20) {
      return true; // Chol Hamoed Pesach
    }
    if (hebrewMonth == 1 && hebrewDay >= 17 && hebrewDay <= 21) {
      return true; // Chol Hamoed Succos
    }
    int avMonth = nissanMonth + 4;
    if (hebrewMonth == avMonth && (hebrewDay == 9 || IsPostponedTishaBav(hebrewYear, hebrewMonth, hebrewDay))) {
      return true; // Tisha B'av
    }
    int purimMonth = _hc.IsLeapYear(hebrewYear) ? 7 : 6;
    return hebrewMonth == purimMonth && hebrewDay == 14; // Purim
  }

  private bool IsPostponedTishaBav(int hebrewYear, int hebrewMonth, int hebrewDay) {
    if (hebrewDay != 10) {
      return false;
    }
    DateTime tishaBav = _hc.ToDateTime(hebrewYear, hebrewMonth, 9, 0, 0, 0, 0);
    return tishaBav.DayOfWeek == DayOfWeek.Saturday;
  }

  private int GetNissanMonth(int hebrewYear) =>
    _hc.GetMonthsInYear(hebrewYear) == 13 ? 8 : 7;

  private string GetDayStyle(HebrewDateType hebrewDateType, bool isSelected, bool isToday) =>
    isSelected || isToday
      ? ""
      : hebrewDateType switch {
        HebrewDateType.ShabbosOrYomTov => $"background-color: {ShabbosOrYomTovBgColour};",
        HebrewDateType.OtherNonWorkDay => $"background-color: {OtherNonWorkDayBgColour};",
        _ => "",
      };

  private string GetDayTooltip(int hebrewYear, int hebrewMonth, int hebrewDay, DateTime gregorianDate) {
    const char lrm = '\u200E';
    string hebrewMonthName = GetHebrewMonthName(hebrewYear, hebrewMonth);
    string hebrewDateStr = $"{lrm}{hebrewDay}{lrm} {hebrewMonthName} {lrm}{hebrewYear}";
    string gregorianDateStr = $"{lrm}{gregorianDate:d MMMM yyyy}";
    string holidayName = GetHolidayName(hebrewYear, hebrewMonth, hebrewDay, gregorianDate);
    return string.IsNullOrEmpty(holidayName)
      ? $"{hebrewDateStr}\n{gregorianDateStr}"
      : $"{hebrewDateStr}\n{gregorianDateStr}\n{lrm}{holidayName}";
  }

  private string GetHolidayName(int hebrewYear, int hebrewMonth, int hebrewDay, DateTime gregorianDate) {
    List<string> parts = [];
    if (IncludeShabbosOrYomTov && gregorianDate.DayOfWeek == DayOfWeek.Saturday) {
      parts.Add("Shabbos");
    }
    if (IncludeShabbosOrYomTov) {
      string yomTovName = GetYomTovName(hebrewYear, hebrewMonth, hebrewDay);
      if (!string.IsNullOrEmpty(yomTovName)) {
        parts.Add(yomTovName);
      }
    }
    if (IncludeOtherNonWorkDays) {
      string otherName = GetOtherNonWorkDayName(hebrewYear, hebrewMonth, hebrewDay);
      if (!string.IsNullOrEmpty(otherName)) {
        parts.Add(otherName);
      }
    }
    return string.Join(" / ", parts);
  }

  private string GetYomTovName(int hebrewYear, int hebrewMonth, int hebrewDay) {
    int nissanMonth = GetNissanMonth(hebrewYear);
    int sivanMonth = nissanMonth + 2;
    if (hebrewMonth == nissanMonth) {
      return hebrewDay switch {
        15 => "First day Pesach",
        16 => "Second day Pesach",
        21 => "Seventh day Pesach",
        22 => "Eighth day Pesach",
        _ => "",
      };
    }
    if (hebrewMonth == sivanMonth) {
      return hebrewDay switch {
        6 => "First day Shavuos",
        7 => "Second day Shavuos",
        _ => "",
      };
    }
    if (hebrewMonth == 1) {
      return hebrewDay switch {
        1 => "First day Rosh Hashana",
        2 => "Second day Rosh Hashana",
        10 => "Yom Kippur",
        15 => "First day Succos",
        16 => "Second day Succos",
        22 => "Shemini Atzeres",
        23 => "Simchas Torah",
        _ => "",
      };
    }
    return "";
  }

  private string GetOtherNonWorkDayName(int hebrewYear, int hebrewMonth, int hebrewDay) {
    int nissanMonth = GetNissanMonth(hebrewYear);
    if (hebrewMonth == nissanMonth && hebrewDay >= 17 && hebrewDay <= 20) {
      return "Chol Hamoed Pesach";
    }
    if (hebrewMonth == 1 && hebrewDay >= 17 && hebrewDay <= 21) {
      return "Chol Hamoed Succos";
    }
    int avMonth = nissanMonth + 4;
    if (hebrewMonth == avMonth && (hebrewDay == 9 || IsPostponedTishaBav(hebrewYear, hebrewMonth, hebrewDay))) {
      return "Tisha B'Av";
    }
    int purimMonth = _hc.IsLeapYear(hebrewYear) ? 7 : 6;
    return hebrewMonth == purimMonth && hebrewDay == 14 ? "Purim" : "";
  }

  private enum HebrewDateType {
    None,
    ShabbosOrYomTov,
    OtherNonWorkDay,
  }
}

public enum DateInputDisplay {
  Hebrew,
  Gregorian,
  Both,
}