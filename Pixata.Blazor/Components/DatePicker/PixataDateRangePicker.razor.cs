using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A pair of date inputs for the start and end of a range, with a popup that shows two months side by side (one on a phone)
/// for picking both. <typeparamref name="TValue"/> can be <see cref="DateTime"/>, <see cref="DateOnly"/> or a nullable
/// version of either
/// </summary>
public partial class PixataDateRangePicker<TValue> : IAsyncDisposable {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  [CascadingParameter]
  private EditContext? EditContext { get; set; }

  /// <summary>
  /// The start of the range. Can be two-way bound
  /// </summary>
  [Parameter]
  public TValue? StartValue { get; set; }

  [Parameter]
  public EventCallback<TValue?> StartValueChanged { get; set; }

  /// <summary>
  /// Identifies the start's field for validation. Set automatically when you use <c>@bind-StartValue</c>
  /// </summary>
  [Parameter]
  public Expression<Func<TValue?>>? StartValueExpression { get; set; }

  /// <summary>
  /// The end of the range. Can be two-way bound
  /// </summary>
  [Parameter]
  public TValue? EndValue { get; set; }

  [Parameter]
  public EventCallback<TValue?> EndValueChanged { get; set; }

  /// <summary>
  /// Identifies the end's field for validation. Set automatically when you use <c>@bind-EndValue</c>
  /// </summary>
  [Parameter]
  public Expression<Func<TValue?>>? EndValueExpression { get; set; }

  /// <summary>
  /// Fires when the user has finished changing the range: when they pick its end in the calendar, close the calendar, or
  /// press Enter in or leave either input, having changed it. Unlike <see cref="StartValueChanged"/> and
  /// <see cref="EndValueChanged"/>, it doesn't fire on every key
  /// </summary>
  [Parameter]
  public EventCallback<PixataDateRangePickerChangeEventArgs> OnChange { get; set; }

  [Parameter]
  public EventCallback OnOpen { get; set; }

  [Parameter]
  public EventCallback OnClose { get; set; }

  /// <summary>
  /// A .NET date format, such as "dd/MM/yyyy" or "d". Defaults to the current culture's short date pattern
  /// </summary>
  [Parameter]
  public string Format { get; set; } = "d";

  /// <summary>
  /// The earliest date allowed. Defaults to 1st January 1900
  /// </summary>
  [Parameter]
  public DateTime Min { get; set; } = new(1900, 1, 1);

  /// <summary>
  /// The latest date allowed. Defaults to 31st December 2099
  /// </summary>
  [Parameter]
  public DateTime Max { get; set; } = new(2099, 12, 31, 23, 59, 59);

  /// <summary>
  /// Dates the user can't pick from the calendar
  /// </summary>
  [Parameter]
  public List<DateTime>? DisabledDates { get; set; }

  /// <summary>
  /// Whether picking an end date before the start in the calendar swaps them round. When false (the default), that date
  /// becomes the new start instead
  /// </summary>
  [Parameter]
  public bool AllowReverse { get; set; }

  [Parameter]
  public bool ShowWeekNumbers { get; set; }

  /// <summary>
  /// The view the calendar opens in. Defaults to <see cref="PixataCalendarView.Month"/>
  /// </summary>
  [Parameter]
  public PixataCalendarView View { get; set; } = PixataCalendarView.Month;

  /// <summary>
  /// The most detailed view the calendar shows, where clicking a cell picks it
  /// </summary>
  [Parameter]
  public PixataCalendarView BottomView { get; set; } = PixataCalendarView.Month;

  /// <summary>
  /// The id of the start input. One is generated if you don't set it, so the label works
  /// </summary>
  [Parameter]
  public string? StartId { get; set; }

  /// <summary>
  /// The id of the end input. One is generated if you don't set it, so the label works
  /// </summary>
  [Parameter]
  public string? EndId { get; set; }

  /// <summary>
  /// The label above the start input. Defaults to "Start". Set it to null or "" for no label
  /// </summary>
  [Parameter]
  public string? StartLabel { get; set; } = "Start";

  /// <summary>
  /// The label above the end input. Defaults to "End". Set it to null or "" for no label
  /// </summary>
  [Parameter]
  public string? EndLabel { get; set; } = "End";

  /// <summary>
  /// Shown in the start input when it's empty and doesn't have focus
  /// </summary>
  [Parameter]
  public string? StartPlaceholder { get; set; }

  /// <summary>
  /// Shown in the end input when it's empty and doesn't have focus
  /// </summary>
  [Parameter]
  public string? EndPlaceholder { get; set; }

  [Parameter]
  public bool Enabled { get; set; } = true;

  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// Shows an x button in each input that clears it. Only for nullable types
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; }

  /// <summary>
  /// The width of the whole component, as a CSS width
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// A CSS class for the popup
  /// </summary>
  [Parameter]
  public string? PopupClass { get; set; }

  private readonly string _generatedId = $"pixata-blazor-daterangepicker-{Guid.NewGuid():N}";
  private ElementReference _wrapper;
  private ElementReference _popup;
  private PixataDateInput<TValue>? _startInput;
  private PixataDateInput<TValue>? _endInput;
  private PixataCalendar? _calendar;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataDateRangePicker<TValue>>? _dotNet;
  private bool _open;
  private bool _justOpened;
  private bool _focusCalendar;
  private bool _selectingEnd;
  private DateTime? _calendarDate;
  private DateTime? _reportedStart;
  private DateTime? _reportedEnd;

  private DateTime? StartDate =>
    DateValue<TValue>.ToDateTime(StartValue);

  private DateTime? EndDate =>
    DateValue<TValue>.ToDateTime(EndValue);

  private string StartInputId =>
    StartId ?? $"{_generatedId}-start";

  private string EndInputId =>
    EndId ?? $"{_generatedId}-end";

  private string WrapperClass =>
    DatePickerCss.Join("pixata-blazor-daterangepicker", Enabled ? null : "pixata-blazor-daterangepicker-disabled",
                       _open ? "pixata-blazor-daterangepicker-open" : null, Class);

  private string? WidthStyle =>
    Width is null ? null : $"width: {Width}";

  protected override void OnInitialized() {
    if (!DateValue<TValue>.IsSupported) {
      throw new InvalidOperationException($"PixataDateRangePicker only works with DateTime, DateOnly and their nullable versions, not {typeof(TValue).Name}");
    }
    _reportedStart = StartDate;
    _reportedEnd = EndDate;
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      try {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      } catch (JSDisconnectedException) {
        // The page has gone
      }
    }
    if (_justOpened && _open && _module is not null) {
      _justOpened = false;
      _dotNet ??= DotNetObjectReference.Create(this);
      try {
        await _module.InvokeVoidAsync("openPopup", _wrapper, _popup, _dotNet);
        if (_focusCalendar && _calendar is not null) {
          await _calendar.FocusAsync();
        }
      } catch (JSException) {
        // The popup was closed again before it could be positioned
      } catch (JSDisconnectedException) {
        // The page has gone
      }
    }
  }

  /// <summary>
  /// Opens the calendar, ready to pick the start of the range
  /// </summary>
  public async Task Open() =>
    await OpenFor(false, false);

  /// <summary>
  /// Closes the calendar
  /// </summary>
  public async Task Close() {
    // Cleared even when closed, in case the popup is closed before it has been rendered
    _justOpened = false;
    if (!_open) {
      return;
    }
    _open = false;
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("closePopup", _popup);
      } catch (JSException) {
        // The popup has already gone from the page
      } catch (JSDisconnectedException) {
        // The page has gone
      }
    }
    await OnClose.InvokeAsync();
    await RaiseChangeIfNeeded();
    StateHasChanged();
  }

  /// <summary>
  /// Moves the keyboard focus to the start input
  /// </summary>
  public async ValueTask FocusStartAsync() {
    if (_startInput is not null) {
      await _startInput.FocusAsync();
    }
  }

  /// <summary>
  /// Moves the keyboard focus to the end input
  /// </summary>
  public async ValueTask FocusEndAsync() {
    if (_endInput is not null) {
      await _endInput.FocusAsync();
    }
  }

  [JSInvokable]
  public async Task ClosePopup() =>
    await Close();

  // Opens the calendar (or, if it's already open, switches it) to pick one end of the range, showing that end's date
  private async Task OpenFor(bool end, bool focusCalendar) {
    if (!Enabled || ReadOnly) {
      return;
    }
    _selectingEnd = end && StartDate is not null;
    _calendarDate = (end ? EndDate ?? StartDate : StartDate) ?? DateTime.Today;
    if (_open) {
      if (focusCalendar && _calendar is not null) {
        await _calendar.FocusAsync();
      }
      return;
    }
    _open = true;
    _justOpened = true;
    _focusCalendar = focusCalendar;
    await OnOpen.InvokeAsync();
    StateHasChanged();
  }

  // Clicking (or tapping) an input opens the calendar, leaving the focus in the input so the date can still be typed
  private async Task InputClicked(bool end) =>
    await OpenFor(end, false);

  private async Task CloseAndFocus() {
    bool end = _selectingEnd;
    await Close();
    if (end) {
      await FocusEndAsync();
    } else {
      await FocusStartAsync();
    }
  }

  // Alt+Down opens the calendar and moves into it, and Alt+Up or Escape closes it
  private async Task OnInputKeyDown(KeyboardEventArgs e, bool end) {
    switch (e.Key) {
      case "ArrowDown" when e.AltKey:
        await OpenFor(end, true);
        break;
      case "ArrowUp" when e.AltKey:
      case "Escape":
        await Close();
        break;
    }
  }

  private async Task OnStartInputChanged(TValue? value) {
    StartValue = value;
    await StartValueChanged.InvokeAsync(value);
  }

  private async Task OnEndInputChanged(TValue? value) {
    EndValue = value;
    await EndValueChanged.InvokeAsync(value);
  }

  private async Task OnInputChange(object? _) =>
    await RaiseChangeIfNeeded();

  // Picking the start leaves the calendar open for the end. Picking the end completes the range, so it closes
  private async Task OnCalendarRangeChanged(PixataCalendarRangeChangeEventArgs range) {
    await SetFromCalendar(range.Start, StartDate, v => StartValue = v, StartValueChanged, StartValueExpression);
    await SetFromCalendar(range.End, EndDate, v => EndValue = v, EndValueChanged, EndValueExpression);
    if (range.Start is not null && range.End is not null && !_selectingEnd) {
      await Close();
      await FocusEndAsync();
    }
  }

  // A non-nullable value can't be cleared, so it keeps its old date when the calendar clears that end of the range
  private async Task SetFromCalendar(DateTime? date, DateTime? current, Action<TValue?> set, EventCallback<TValue?> changed,
                                     Expression<Func<TValue?>>? expression) {
    if (date == current || (date is null && !DateValue<TValue>.IsNullable)) {
      return;
    }
    TValue? value = DateValue<TValue>.FromDateTime(date);
    set(value);
    await changed.InvokeAsync(value);
    if (EditContext is not null && expression is not null) {
      EditContext.NotifyFieldChanged(FieldIdentifier.Create(expression));
    }
  }

  private async Task RaiseChangeIfNeeded() {
    if (StartDate != _reportedStart || EndDate != _reportedEnd) {
      _reportedStart = StartDate;
      _reportedEnd = EndDate;
      await OnChange.InvokeAsync(new(StartValue, EndValue));
    }
  }

  public async ValueTask DisposeAsync() {
    if (_module is not null) {
      try {
        if (_open) {
          await _module.InvokeVoidAsync("closePopup", _popup);
        }
        await _module.DisposeAsync();
      } catch (JSDisconnectedException) {
        // The circuit has gone
      }
    }
    _dotNet?.Dispose();
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// The range in a <see cref="PixataDateRangePicker{TValue}"/>'s <c>OnChange</c> event. The values are of the picker's
/// <c>TValue</c> type
/// </summary>
public record PixataDateRangePickerChangeEventArgs(object? StartValue, object? EndValue);
