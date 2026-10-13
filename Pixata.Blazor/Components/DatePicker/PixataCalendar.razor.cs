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
/// decade of years and a century of decades by clicking the title
/// </summary>
public partial class PixataCalendar : IAsyncDisposable {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The selected date. Can be two-way bound
  /// </summary>
  [Parameter]
  public DateTime? Value { get; set; }

  [Parameter]
  public EventCallback<DateTime> ValueChanged { get; set; }

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
  /// Shows the days from the previous and next months that fill the first and last weeks. Defaults to true
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

  private ElementReference _table;
  private IJSObjectReference? _module;
  private DateTime _focused = DateTime.Today;
  private PixataCalendarView _view = PixataCalendarView.Month;
  private DateTime? _lastDateParameter;
  private bool _initialised;
  private PixataCalendarView? _lastViewParameter;
  private bool _tableHasFocus;

  private static DateTimeFormatInfo Info =>
    CultureInfo.CurrentCulture.DateTimeFormat;

  protected override void OnParametersSet() {
    if (!_initialised || Date != _lastDateParameter) {
      _initialised = true;
      _lastDateParameter = Date;
      _focused = Clamp((Date ?? Value ?? DateTime.Today).Date);
    }
    if (View != _lastViewParameter) {
      _lastViewParameter = View;
      _view = (PixataCalendarView)Math.Clamp((int)View, (int)BottomView, (int)TopView);
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
        await _module.InvokeVoidAsync("preventNavigationKeys", _table);
      } catch (JSDisconnectedException) {
        // The page has gone
      }
    }
  }

  /// <summary>
  /// Moves the keyboard focus into the calendar, on the focused date
  /// </summary>
  public async ValueTask FocusAsync() =>
    await _table.FocusAsync();

  /// <summary>
  /// Shows the given date in the given view
  /// </summary>
  public async Task NavigateTo(DateTime date, PixataCalendarView view) {
    _view = (PixataCalendarView)Math.Clamp((int)view, (int)BottomView, (int)TopView);
    await SetFocused(date);
    await ViewChanged.InvokeAsync(_view);
  }

  private DateTime Clamp(DateTime date) =>
    date < Min.Date ? Min.Date : date > Max.Date ? Max.Date : date;

  private async Task SetFocused(DateTime date) {
    DateTime clamped = Clamp(date.Date);
    if (clamped != _focused) {
      _focused = clamped;
      await DateChanged.InvokeAsync(_focused);
    }
  }

  // ---------- What each view shows ----------

  private string Title => _view switch {
    PixataCalendarView.Month => _focused.ToString(Info.YearMonthPattern, CultureInfo.CurrentCulture),
    PixataCalendarView.Year => _focused.Year.ToString(CultureInfo.CurrentCulture),
    PixataCalendarView.Decade => $"{DecadeStart(_focused.Year)} - {DecadeStart(_focused.Year) + 9}",
    _ => $"{CenturyStart(_focused.Year)} - {CenturyStart(_focused.Year) + 90}"
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

  private record Cell(DateTime Date, string Text, string Title, bool OtherPeriod, bool Disabled, bool Selected, bool Today, bool Focused, bool Weekend);

  // Six weeks, starting on the culture's first day of the week, so the calendar is always the same height
  private List<List<Cell?>> MonthRows() {
    DateTime first = new(_focused.Year, _focused.Month, 1);
    int offset = ((int)first.DayOfWeek - (int)Info.FirstDayOfWeek + 7) % 7;
    DateTime start = first.AddDays(-offset);
    List<List<Cell?>> rows = [];
    for (int week = 0; week < 6; week++) {
      List<Cell?> row = [];
      for (int day = 0; day < 7; day++) {
        DateTime date = start.AddDays(week * 7 + day);
        bool other = date.Month != _focused.Month;
        row.Add(other && !ShowOtherMonthDays
          ? null
          : new(date, date.Day.ToString(CultureInfo.CurrentCulture), date.ToString("D", CultureInfo.CurrentCulture), other, IsDisabled(date),
                Value?.Date == date, date == DateTime.Today, date == _focused, date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
      }
      rows.Add(row);
    }
    return rows;
  }

  private IEnumerable<string> DayHeaders =>
    Enumerable.Range(0, 7).Select(i => Info.DayNames[((int)Info.FirstDayOfWeek + i) % 7]);

  private static int WeekNumber(List<Cell?> row) {
    // The week number comes from the Thursday, as ISO-8601 weeks do
    DateTime anyDay = row.First(c => c is not null)!.Date;
    return ISOWeek.GetWeekOfYear(anyDay.AddDays(DayOfWeek.Thursday - anyDay.DayOfWeek));
  }

  // Twelve cells (months, years or decades) in rows of four. A decade or century only fills ten of them
  private List<List<Cell?>> GridRows() {
    List<Cell?> cells = _view switch {
      PixataCalendarView.Year => [.. Enumerable.Range(1, 12).Select(m => {
        DateTime date = new(_focused.Year, m, 1);
        return (Cell?)new Cell(date, Info.GetAbbreviatedMonthName(m), date.ToString("Y", CultureInfo.CurrentCulture), false,
                               date > Max || new DateTime(_focused.Year, m, DateTime.DaysInMonth(_focused.Year, m)) < Min,
                               Value is DateTime v && v.Year == date.Year && v.Month == m, false, _focused.Month == m, false);
      })],
      PixataCalendarView.Decade => [.. Enumerable.Range(DecadeStart(_focused.Year), 10).Select(y => (Cell?)new Cell(
        new(y, 1, 1), y.ToString(CultureInfo.CurrentCulture), y.ToString(CultureInfo.CurrentCulture), false, y > Max.Year || y < Min.Year,
        Value?.Year == y, false, _focused.Year == y, false))],
      _ => [.. Enumerable.Range(0, 10).Select(i => CenturyStart(_focused.Year) + i * 10).Select(y => (Cell?)new Cell(
        new(y, 1, 1), y.ToString(CultureInfo.CurrentCulture), $"{y} - {y + 9}", false, y > Max.Year || y + 9 < Min.Year,
        Value is DateTime v && DecadeStart(v.Year) == y, false, DecadeStart(_focused.Year) == y, false))]
    };
    while (cells.Count < 12) {
      cells.Add(null);
    }
    return [.. cells.Chunk(4).Select(c => c.ToList())];
  }

  private bool IsDisabled(DateTime date) =>
    date < Min.Date || date > Max.Date || DisabledDates?.Any(d => d.Date == date) == true;

  private bool CanGoBack =>
    PeriodStart(_focused) > Min.Date;

  private bool CanGoForward =>
    PeriodEnd(_focused) < Max.Date;

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
      await ViewChanged.InvokeAsync(_view);
    }
  }

  private async Task Navigate(int periods) =>
    await SetFocused(Next(_focused, periods));

  private async Task GoToToday() {
    _view = BottomView;
    await SetFocused(DateTime.Today);
    if (TodayAvailable && BottomView == PixataCalendarView.Month) {
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
      await SetFocused(date);
      await ViewChanged.InvokeAsync(_view);
    }
  }

  private async Task Select(DateTime date) {
    Value = date;
    await ValueChanged.InvokeAsync(date);
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
        Cell? cell = (_view == PixataCalendarView.Month ? MonthRows() : GridRows()).SelectMany(r => r).FirstOrDefault(c => c?.Focused == true);
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
      cell.Today ? "pixata-blazor-calendar-today" : null,
      cell.OtherPeriod ? "pixata-blazor-calendar-other-month" : null,
      cell.Weekend ? "pixata-blazor-calendar-weekend" : null,
      cell.Disabled ? "pixata-blazor-calendar-disabled" : null,
      cell.Focused && _tableHasFocus ? "pixata-blazor-calendar-focus" : null);

  public async ValueTask DisposeAsync() {
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("allowNavigationKeys", _table);
        await _module.DisposeAsync();
      } catch (JSDisconnectedException) {
        // The circuit has gone
      }
    }
    GC.SuppressFinalize(this);
  }
}
