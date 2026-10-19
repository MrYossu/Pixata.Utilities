using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A calendar that looks and behaves like Telerik's. It shows a month of days, and the user can go up to a year of months, a
/// decade of years and a century of decades by clicking the title. It can pick a single date or a range, and can show more
/// than one month (or year, decade or century) side by side
/// </summary>
public partial class PixataCalendar : IAsyncDisposable {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The selected date. Can be two-way bound. Not used when <see cref="SelectionMode"/> is
  /// <see cref="PixataCalendarSelectionMode.Range"/>
  /// </summary>
  [Parameter]
  public DateTime? Value { get; set; }

  [Parameter]
  public EventCallback<DateTime> ValueChanged { get; set; }

  /// <summary>
  /// Whether clicking a cell picks a single date (the default) or one end of a range
  /// </summary>
  [Parameter]
  public PixataCalendarSelectionMode SelectionMode { get; set; } = PixataCalendarSelectionMode.Single;

  /// <summary>
  /// The start of the selected range, when <see cref="SelectionMode"/> is <see cref="PixataCalendarSelectionMode.Range"/>.
  /// Can be two-way bound
  /// </summary>
  [Parameter]
  public DateTime? RangeStart { get; set; }

  /// <summary>
  /// The end of the selected range, when <see cref="SelectionMode"/> is <see cref="PixataCalendarSelectionMode.Range"/>.
  /// Can be two-way bound
  /// </summary>
  [Parameter]
  public DateTime? RangeEnd { get; set; }

  [Parameter]
  public EventCallback<DateTime?> RangeStartChanged { get; set; }

  [Parameter]
  public EventCallback<DateTime?> RangeEndChanged { get; set; }

  /// <summary>
  /// Fires when the user picks either end of the range
  /// </summary>
  [Parameter]
  public EventCallback<PixataCalendarRangeChangeEventArgs> RangeChanged { get; set; }

  /// <summary>
  /// In range mode, whether the next date picked is the end of the range (true) or the start (false). It changes as the user
  /// picks dates, and can be two-way bound
  /// </summary>
  [Parameter]
  public bool SelectingRangeEnd { get; set; }

  [Parameter]
  public EventCallback<bool> SelectingRangeEndChanged { get; set; }

  /// <summary>
  /// In range mode, whether picking an end date before the start swaps them round. When false (the default), it starts a new
  /// range from that date instead
  /// </summary>
  [Parameter]
  public bool AllowReverse { get; set; }

  /// <summary>
  /// How many months (or years, decades or centuries) are shown side by side. Defaults to 1. On a narrow screen, such as a
  /// phone, only the first is shown
  /// </summary>
  [Parameter]
  public int Views { get; set; } = 1;

  /// <summary>
  /// The date the calendar is showing (and has the keyboard focus on). Defaults to the value, or today. Can be two-way bound
  /// </summary>
  [Parameter]
  public DateTime? Date { get; set; }

  [Parameter]
  public EventCallback<DateTime> DateChanged { get; set; }

  /// <summary>
  /// The view being shown. Can be two-way bound
  /// </summary>
  [Parameter]
  public PixataCalendarView View { get; set; } = PixataCalendarView.Month;

  [Parameter]
  public EventCallback<PixataCalendarView> ViewChanged { get; set; }

  /// <summary>
  /// The most detailed view, in which clicking a cell selects it. Defaults to <see cref="PixataCalendarView.Month"/>, so
  /// the user picks a day. Set it to <see cref="PixataCalendarView.Year"/> to pick a month
  /// </summary>
  [Parameter]
  public PixataCalendarView BottomView { get; set; } = PixataCalendarView.Month;

  /// <summary>
  /// The least detailed view the user can go up to. Defaults to <see cref="PixataCalendarView.Century"/>
  /// </summary>
  [Parameter]
  public PixataCalendarView TopView { get; set; } = PixataCalendarView.Century;

  [Parameter]
  public DateTime Min { get; set; } = new(1900, 1, 1);

  [Parameter]
  public DateTime Max { get; set; } = new(2099, 12, 31);

  /// <summary>
  /// Dates the user can't pick
  /// </summary>
  [Parameter]
  public List<DateTime>? DisabledDates { get; set; }

  /// <summary>
  /// Shows the ISO-8601 week number at the start of each week
  /// </summary>
  [Parameter]
  public bool ShowWeekNumbers { get; set; }

  /// <summary>
  /// Shows the days from the previous and next months that fill the first and last weeks. Defaults to true. They are never
  /// shown when there is more than one view, so no date appears twice
  /// </summary>
  [Parameter]
  public bool ShowOtherMonthDays { get; set; } = true;

  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Fires when the user presses Escape, so a date picker can close its popup
  /// </summary>
  [Parameter]
  public EventCallback OnEscape { get; set; }

  private ElementReference _viewsElement;
  private ElementReference[] _tables = new ElementReference[1];
  private IJSObjectReference? _module;
  private DateTime _focused = DateTime.Today;
  // The start of the period shown in the first view
  private DateTime _anchor = DateTime.Today;
  private PixataCalendarView _view = PixataCalendarView.Month;
  private DateTime? _lastDateParameter;
  private bool _initialised;
  private PixataCalendarView? _lastViewParameter;
  private bool? _lastSelectingRangeEndParameter;
  private bool _selectingEnd;
  private bool _tableHasFocus;

  private static DateTimeFormatInfo Info =>
    CultureInfo.CurrentCulture.DateTimeFormat;

  private bool IsRange =>
    SelectionMode == PixataCalendarSelectionMode.Range;

  protected override void OnParametersSet() {
    Views = Math.Max(1, Views);
    if (_tables.Length != Views) {
      _tables = new ElementReference[Views];
    }
    bool viewChanged = View != _lastViewParameter;
    if (viewChanged) {
      _lastViewParameter = View;
      _view = (PixataCalendarView)Math.Clamp((int)View, (int)BottomView, (int)TopView);
    }
    if (!_initialised || Date != _lastDateParameter) {
      _initialised = true;
      _lastDateParameter = Date;
      _focused = Clamp((Date ?? (IsRange ? RangeStart : Value) ?? DateTime.Today).Date);
      _anchor = PeriodStart(_focused);
    } else if (viewChanged) {
      _anchor = PeriodStart(_focused);
    }
    if (SelectingRangeEnd != _lastSelectingRangeEndParameter) {
      _lastSelectingRangeEndParameter = SelectingRangeEnd;
      _selectingEnd = SelectingRangeEnd;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
        await _module.InvokeVoidAsync("preventNavigationKeys", _viewsElement);
      } catch (JSDisconnectedException) {
        // The page has gone
      }
    }
  }

  /// <summary>
  /// Moves the keyboard focus into the calendar, on the focused date
  /// </summary>
  public async ValueTask FocusAsync() =>
    await _tables[FocusedViewIndex].FocusAsync();

  /// <summary>
  /// Shows the given date in the given view
  /// </summary>
  public async Task NavigateTo(DateTime date, PixataCalendarView view) {
    _view = (PixataCalendarView)Math.Clamp((int)view, (int)BottomView, (int)TopView);
    _anchor = PeriodStart(Clamp(date.Date));
    await SetFocused(date);
    await ViewChanged.InvokeAsync(_view);
  }

  private DateTime Clamp(DateTime date) =>
    date < Min.Date ? Min.Date : date > Max.Date ? Max.Date : date;

  private async Task SetFocused(DateTime date) {
    DateTime clamped = Clamp(date.Date);
    // The views move along when the focus goes past either end of them
    if (clamped < _anchor) {
      _anchor = PeriodStart(clamped);
    } else if (clamped > PeriodEnd(Next(_anchor, Views - 1))) {
      _anchor = Next(PeriodStart(clamped), 1 - Views);
    }
    if (clamped != _focused) {
      _focused = clamped;
      await DateChanged.InvokeAsync(_focused);
    }
  }

  // ---------- What each view shows ----------

  private string Title =>
    Views == 1 ? PeriodTitle(_anchor) : _view switch {
      PixataCalendarView.Month => $"{PeriodTitle(_anchor)} - {PeriodTitle(Next(_anchor, Views - 1))}",
      PixataCalendarView.Year => $"{_anchor.Year} - {Next(_anchor, Views - 1).Year}",
      _ => $"{PeriodStart(_anchor).Year} - {PeriodEnd(Next(_anchor, Views - 1)).Year}"
    };

  private string PeriodTitle(DateTime date) => _view switch {
    PixataCalendarView.Month => date.ToString(Info.YearMonthPattern, CultureInfo.CurrentCulture),
    PixataCalendarView.Year => date.Year.ToString(CultureInfo.CurrentCulture),
    PixataCalendarView.Decade => $"{DecadeStart(date.Year)} - {DecadeStart(date.Year) + 9}",
    _ => $"{CenturyStart(date.Year)} - {CenturyStart(date.Year) + 90}"
  };

  private string TitleTooltip => _view switch {
    PixataCalendarView.Month => "Choose a month",
    PixataCalendarView.Year => "Choose a year",
    _ => "Choose a decade"
  };

  private static int DecadeStart(int year) =>
    year / 10 * 10;

  private static int CenturyStart(int year) =>
    year / 100 * 100;

  // The view the focused date is in, which is the one the keyboard focus goes to
  private int FocusedViewIndex =>
    Enumerable.Range(0, Views).FirstOrDefault(i => _focused <= PeriodEnd(Next(_anchor, i)));

  // The cell a date falls in, in the current view: the day, the first of the month, the 1st January of the year, or the 1st
  // January of the decade
  private DateTime CellDate(DateTime date) => _view switch {
    PixataCalendarView.Month => date.Date,
    PixataCalendarView.Year => new(date.Year, date.Month, 1),
    PixataCalendarView.Decade => new(date.Year, 1, 1),
    _ => new(Math.Max(DecadeStart(date.Year), 1), 1, 1)
  };

  private record Cell(DateTime Date, string Text, string Title, bool OtherPeriod, bool Disabled, bool Weekend) {
    public bool Selected { get; init; }
    public bool Today { get; init; }
    public bool Focused { get; init; }
    public bool RangeStart { get; init; }
    public bool RangeEnd { get; init; }
    public bool InRange { get; init; }
  }

  // Works out whether a cell is selected, focused, or part of the range
  private Cell Mark(Cell cell) {
    DateTime? start = RangeStart is DateTime s ? CellDate(s) : null;
    DateTime? end = RangeEnd is DateTime e ? CellDate(e) : null;
    bool rangeStart = IsRange && _view == BottomView && cell.Date == start;
    bool rangeEnd = IsRange && _view == BottomView && cell.Date == end;
    return cell with {
      Selected = IsRange ? rangeStart || rangeEnd : Value is DateTime v && CellDate(v) == cell.Date,
      Focused = CellDate(_focused) == cell.Date,
      RangeStart = rangeStart,
      RangeEnd = rangeEnd,
      InRange = IsRange && _view == BottomView && start is not null && end is not null && cell.Date > start && cell.Date < end
    };
  }

  // Six weeks, starting on the culture's first day of the week, so the calendar is always the same height
  private List<List<Cell?>> MonthRows(DateTime period) {
    DateTime first = new(period.Year, period.Month, 1);
    int offset = ((int)first.DayOfWeek - (int)Info.FirstDayOfWeek + 7) % 7;
    DateTime start = first.AddDays(-offset);
    bool showOther = ShowOtherMonthDays && Views == 1;
    List<List<Cell?>> rows = [];
    for (int week = 0; week < 6; week++) {
      List<Cell?> row = [];
      for (int day = 0; day < 7; day++) {
        DateTime date = start.AddDays(week * 7 + day);
        bool other = date.Month != period.Month;
        row.Add(other && !showOther
          ? null
          : Mark(new(date, date.Day.ToString(CultureInfo.CurrentCulture), date.ToString("D", CultureInfo.CurrentCulture), other, IsDisabled(date),
                     date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) { Today = date == DateTime.Today }));
      }
      rows.Add(row);
    }
    return rows;
  }

  private IEnumerable<string> DayHeaders =>
    Enumerable.Range(0, 7).Select(i => Info.DayNames[((int)Info.FirstDayOfWeek + i) % 7]);

  private static string WeekNumber(List<Cell?> row) {
    // The week number comes from the Thursday, as ISO-8601 weeks do. A row with no days in it (which happens when the days
    // from the months either side aren't shown) has no week number
    int index = row.FindIndex(c => c is not null);
    if (index < 0) {
      return "";
    }
    DateTime anyDay = row[index]!.Date.AddDays(-index);
    return ISOWeek.GetWeekOfYear(anyDay.AddDays(DayOfWeek.Thursday - anyDay.DayOfWeek)).ToString(CultureInfo.CurrentCulture);
  }

  // Twelve cells (months, years or decades) in rows of four. A decade or century only fills ten of them
  private List<List<Cell?>> GridRows(DateTime period) {
    List<Cell?> cells = _view switch {
      PixataCalendarView.Year => [.. Enumerable.Range(1, 12).Select(m => {
        DateTime date = new(period.Year, m, 1);
        return (Cell?)Mark(new(date, Info.GetAbbreviatedMonthName(m), date.ToString("Y", CultureInfo.CurrentCulture), false,
                               date > Max || new DateTime(period.Year, m, DateTime.DaysInMonth(period.Year, m)) < Min, false));
      })],
      PixataCalendarView.Decade => [.. Enumerable.Range(DecadeStart(period.Year), 10).Select(y => (Cell?)Mark(new(
        new(y, 1, 1), y.ToString(CultureInfo.CurrentCulture), y.ToString(CultureInfo.CurrentCulture), false, y > Max.Year || y < Min.Year, false)))],
      _ => [.. Enumerable.Range(0, 10).Select(i => CenturyStart(period.Year) + i * 10).Select(y => (Cell?)Mark(new(
        new(Math.Max(y, 1), 1, 1), y.ToString(CultureInfo.CurrentCulture), $"{y} - {y + 9}", false, y > Max.Year || y + 9 < Min.Year, false)))]
    };
    while (cells.Count < 12) {
      cells.Add(null);
    }
    return [.. cells.Chunk(4).Select(c => c.ToList())];
  }

  private bool IsDisabled(DateTime date) =>
    date < Min.Date || date > Max.Date || DisabledDates?.Any(d => d.Date == date) == true;

  private bool CanGoBack =>
    _anchor > Min.Date;

  private bool CanGoForward =>
    PeriodEnd(Next(_anchor, Views - 1)) < Max.Date;

  private bool TodayAvailable =>
    !IsDisabled(DateTime.Today);

  private DateTime PeriodStart(DateTime date) => _view switch {
    PixataCalendarView.Month => new(date.Year, date.Month, 1),
    PixataCalendarView.Year => new(date.Year, 1, 1),
    PixataCalendarView.Decade => new(Math.Max(DecadeStart(date.Year), 1), 1, 1),
    _ => new(Math.Max(CenturyStart(date.Year), 1), 1, 1)
  };

  private DateTime PeriodEnd(DateTime date) =>
    Next(PeriodStart(date), 1).AddDays(-1);

  private DateTime Next(DateTime date, int periods) => _view switch {
    PixataCalendarView.Month => date.AddMonths(periods),
    PixataCalendarView.Year => date.AddYears(periods),
    PixataCalendarView.Decade => date.AddYears(10 * periods),
    _ => date.AddYears(100 * periods)
  };

  // ---------- What the user does ----------

  private async Task GoUp() {
    if (_view < TopView) {
      _view++;
      _anchor = PeriodStart(_focused);
      await ViewChanged.InvokeAsync(_view);
    }
  }

  // The previous and next buttons, and Page Up and Down, move all the views along by one period
  private async Task Navigate(int periods) {
    _anchor = Next(_anchor, periods);
    await SetFocused(Next(_focused, periods));
  }

  private async Task GoToToday() {
    _view = BottomView;
    _anchor = PeriodStart(Clamp(DateTime.Today));
    await SetFocused(DateTime.Today);
    // In range mode, Today only shows today, as it can't know which end of the range to set
    if (TodayAvailable && BottomView == PixataCalendarView.Month && !IsRange) {
      await Select(DateTime.Today);
    }
  }

  private async Task CellClicked(Cell cell) {
    if (cell.Disabled) {
      return;
    }
    if (_view == BottomView) {
      await SetFocused(cell.Date);
      await Select(cell.Date);
    } else {
      // Going down keeps the day and month where possible, so going from October to the year view and back stays in October
      int year = _view == PixataCalendarView.Century ? cell.Date.Year + _focused.Year % 10 : cell.Date.Year;
      int month = _view == PixataCalendarView.Year ? cell.Date.Month : _focused.Month;
      DateTime date = new(year, month, Math.Min(_focused.Day, DateTime.DaysInMonth(year, month)));
      _view--;
      _anchor = PeriodStart(Clamp(date));
      await SetFocused(date);
      await ViewChanged.InvokeAsync(_view);
    }
  }

  private async Task Select(DateTime date) {
    if (IsRange) {
      await SelectRangePart(date);
      return;
    }
    Value = date;
    await ValueChanged.InvokeAsync(date);
  }

  // The first date picked is the start, and the second the end. Picking a new start keeps the end if it's still after it.
  // An end before the start either swaps them (with AllowReverse) or starts again from there
  private async Task SelectRangePart(DateTime date) {
    DateTime? start = RangeStart?.Date;
    DateTime? end = RangeEnd?.Date;
    if (!_selectingEnd || start is null) {
      start = date;
      if (end < date) {
        end = null;
      }
      _selectingEnd = true;
    } else if (date < start) {
      if (AllowReverse) {
        (start, end) = (date, start);
        _selectingEnd = false;
      } else {
        (start, end) = (date, null);
      }
    } else {
      end = date;
      _selectingEnd = false;
    }
    bool startChanged = start != RangeStart;
    bool endChanged = end != RangeEnd;
    RangeStart = start;
    RangeEnd = end;
    await SelectingRangeEndChanged.InvokeAsync(_selectingEnd);
    if (startChanged) {
      await RangeStartChanged.InvokeAsync(start);
    }
    if (endChanged) {
      await RangeEndChanged.InvokeAsync(end);
    }
    await RangeChanged.InvokeAsync(new(start, end));
  }

  // Keyboard navigation as in Telerik's calendar. The arrows move by one cell or one row, Page Up and Down by one period,
  // Home and End to the start and end of the period, Ctrl+Up goes up a view, and Enter or Space selects (or goes down a view)
  private async Task OnKeyDown(KeyboardEventArgs e) {
    int across = 1;
    int down = _view == PixataCalendarView.Month ? 7 : 4;
    switch (e.Key) {
      case "ArrowUp" when e.CtrlKey:
        await GoUp();
        return;
      case "ArrowDown" when e.CtrlKey:
      case "Enter" or " ": {
        DateTime focusedPeriod = PeriodStart(_focused);
        Cell? cell = (_view == PixataCalendarView.Month ? MonthRows(focusedPeriod) : GridRows(focusedPeriod)).SelectMany(r => r).FirstOrDefault(c => c?.Focused == true);
        if (cell is not null && !(e.CtrlKey && _view == BottomView)) {
          await CellClicked(cell);
        }
        return;
      }
      case "Escape":
        await OnEscape.InvokeAsync();
        return;
      case "ArrowLeft":
        await Move(-across);
        return;
      case "ArrowRight":
        await Move(across);
        return;
      case "ArrowUp":
        await Move(-down);
        return;
      case "ArrowDown":
        await Move(down);
        return;
      case "PageUp":
        await Navigate(-1);
        return;
      case "PageDown":
        await Navigate(1);
        return;
      case "Home":
        await SetFocused(_view == PixataCalendarView.Month ? PeriodStart(_focused) : MoveCells(PeriodStart(_focused), 0));
        return;
      case "End":
        await SetFocused(_view == PixataCalendarView.Month ? PeriodEnd(_focused) : MoveCells(PeriodStart(_focused), 9 + (_view == PixataCalendarView.Year ? 2 : 0)));
        return;
    }
  }

  private async Task Move(int cells) =>
    await SetFocused(MoveCells(_focused, cells));

  private DateTime MoveCells(DateTime date, int cells) => _view switch {
    PixataCalendarView.Month => date.AddDays(cells),
    PixataCalendarView.Year => date.AddMonths(cells),
    PixataCalendarView.Decade => date.AddYears(cells),
    _ => date.AddYears(10 * cells)
  };

  private string CellClass(Cell cell) =>
    DatePickerCss.Join("pixata-blazor-calendar-td",
      cell.Selected ? "pixata-blazor-calendar-selected" : null,
      cell.RangeStart ? "pixata-blazor-calendar-range-start" : null,
      cell.RangeEnd ? "pixata-blazor-calendar-range-end" : null,
      cell.InRange ? "pixata-blazor-calendar-range-middle" : null,
      cell.Today ? "pixata-blazor-calendar-today" : null,
      cell.OtherPeriod ? "pixata-blazor-calendar-other-month" : null,
      cell.Weekend ? "pixata-blazor-calendar-weekend" : null,
      cell.Disabled ? "pixata-blazor-calendar-disabled" : null,
      cell.Focused && _tableHasFocus ? "pixata-blazor-calendar-focus" : null);

  public async ValueTask DisposeAsync() {
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("allowNavigationKeys", _viewsElement);
        await _module.DisposeAsync();
      } catch (JSDisconnectedException) {
        // The circuit has gone
      }
    }
    GC.SuppressFinalize(this);
  }
}
