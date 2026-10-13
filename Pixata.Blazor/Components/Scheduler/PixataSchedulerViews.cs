using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base for the view tags that go inside a scheduler's <c>SchedulerViews</c>. A view renders nothing itself; it tells the
/// scheduler that the view is available, and how to set it up. The views appear in the toolbar's view switcher in the order
/// they are declared
/// </summary>
public abstract class PixataSchedulerViewBase<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataScheduler<TItem>? Scheduler { get; set; }

  /// <summary>
  /// The template for the appointments in this view. Overrides the scheduler's <c>ItemTemplate</c>
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? ItemTemplate { get; set; }

  /// <summary>
  /// The template for the content of each slot (cell). The context is a <see cref="PixataSchedulerSlotTemplateContext"/>
  /// </summary>
  [Parameter]
  public RenderFragment<PixataSchedulerSlotTemplateContext>? SlotTemplate { get; set; }

  /// <summary>
  /// The template for the date headers. The context is the date
  /// </summary>
  [Parameter]
  public RenderFragment<DateTime>? DateHeaderTemplate { get; set; }

  /// <summary>
  /// The template for the resource group headers, when the scheduler is grouped by resource
  /// </summary>
  [Parameter]
  public RenderFragment<PixataSchedulerResourceGroupHeaderTemplateContext>? SchedulerResourceGroupHeaderTemplate { get; set; }

  /// <summary>
  /// Which view this is
  /// </summary>
  public abstract PixataSchedulerView View { get; }

  /// <summary>
  /// The number of days the view shows, for the views where that is set by a parameter
  /// </summary>
  internal virtual int DayCount => 1;

  private string _signature = "";

  protected override void OnInitialized() {
    if (Scheduler is null) {
      throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} must be inside a PixataScheduler's SchedulerViews");
    }
    _signature = Signature();
    Scheduler.AddView(this);
  }

  protected override void OnParametersSet() {
    // The scheduler reads the view's settings when it renders, so it needs to render again when they change. Only the
    // simple settings are compared, as templates are new objects every time the page renders
    string signature = Signature();
    if (signature != _signature) {
      _signature = signature;
      Scheduler?.ViewSettingsChanged();
    }
  }

  /// <summary>
  /// The view's simple (non-template) settings as text, so the view can tell when they change
  /// </summary>
  internal virtual string Signature() =>
    $"{View}";

  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    Scheduler?.RemoveView(this);
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// The base for the views that have time slots (the day, multi-day, week and timeline views)
/// </summary>
public abstract class PixataSchedulerTimeViewBase<TItem> : PixataSchedulerViewBase<TItem> {
  /// <summary>
  /// The length of each major time slot, in minutes. 60 by default
  /// </summary>
  [Parameter]
  public int SlotDuration { get; set; } = 60;

  /// <summary>
  /// The number of divisions each major slot is split into. 2 by default
  /// </summary>
  [Parameter]
  public int SlotDivisions { get; set; } = 2;

  /// <summary>
  /// The first time shown each day. Only the time part is used. Midnight by default
  /// </summary>
  [Parameter]
  public DateTime StartTime { get; set; } = DateTime.Today;

  /// <summary>
  /// The end of the time shown each day. Only the time part is used, and midnight (the default) means the end of the day
  /// </summary>
  [Parameter]
  public DateTime EndTime { get; set; } = DateTime.Today;

  /// <summary>
  /// The start of the working day, which is shown with a different background, and is where the view starts when the
  /// scheduler's <c>ShowWorkHours</c> is true. Only the time part is used. 08:00 by default
  /// </summary>
  [Parameter]
  public DateTime WorkDayStart { get; set; } = DateTime.Today.AddHours(8);

  /// <summary>
  /// The end of the working day. Only the time part is used. 17:00 by default
  /// </summary>
  [Parameter]
  public DateTime WorkDayEnd { get; set; } = DateTime.Today.AddHours(17);

  internal override string Signature() =>
    $"{base.Signature()}|{SlotDuration}|{SlotDivisions}|{StartTime.TimeOfDay}|{EndTime.TimeOfDay}|{WorkDayStart.TimeOfDay}|{WorkDayEnd.TimeOfDay}";
}

/// <summary>
/// The base for the views with days side by side and the hours down the side (the day, multi-day and week views)
/// </summary>
public abstract class PixataSchedulerDayViewBase<TItem> : PixataSchedulerTimeViewBase<TItem> {
  /// <summary>
  /// The template for the all-day appointments in this view. Overrides the scheduler's <c>AllDayItemTemplate</c>
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? AllDayItemTemplate { get; set; }

  /// <summary>
  /// The template for the content of the slots in the all-day row
  /// </summary>
  [Parameter]
  public RenderFragment<PixataSchedulerAllDaySlotTemplateContext>? AllDaySlotTemplate { get; set; }

  /// <summary>
  /// When true, the all-day row isn't shown
  /// </summary>
  [Parameter]
  public bool HideAllDayRow { get; set; }

  internal override string Signature() =>
    $"{base.Signature()}|{HideAllDayRow}";
}

/// <summary>
/// A view of a single day, with the hours down the side
/// </summary>
public class PixataSchedulerDayView<TItem> : PixataSchedulerDayViewBase<TItem> {
  public override PixataSchedulerView View => PixataSchedulerView.Day;
}

/// <summary>
/// A view of several days side by side, starting on the scheduler's date, with the hours down the side
/// </summary>
public class PixataSchedulerMultiDayView<TItem> : PixataSchedulerDayViewBase<TItem> {
  /// <summary>
  /// The number of days shown. 1 by default
  /// </summary>
  [Parameter]
  public int NumberOfDays { get; set; } = 1;

  public override PixataSchedulerView View => PixataSchedulerView.MultiDay;

  internal override int DayCount => Math.Max(1, NumberOfDays);

  internal override string Signature() =>
    $"{base.Signature()}|{NumberOfDays}";
}

/// <summary>
/// A view of a week, with the hours down the side. The week starts on the scheduler's first day of the week
/// </summary>
public class PixataSchedulerWeekView<TItem> : PixataSchedulerDayViewBase<TItem> {
  public override PixataSchedulerView View => PixataSchedulerView.Week;

  internal override int DayCount => 7;
}

/// <summary>
/// A view of a month, as six weeks of day cells
/// </summary>
public class PixataSchedulerMonthView<TItem> : PixataSchedulerViewBase<TItem> {
  /// <summary>
  /// The number of appointments shown in each day. When there are more, a button shows how many more there are, and takes
  /// the user to the day view (if there is one). 2 by default
  /// </summary>
  [Parameter]
  public int ItemsPerSlot { get; set; } = 2;

  public override PixataSchedulerView View => PixataSchedulerView.Month;

  internal override int DayCount => PixataSchedulerDates.MonthWeeks * 7;

  protected override void OnParametersSet() {
    if (ItemsPerSlot <= 0) {
      throw new ArgumentOutOfRangeException(nameof(ItemsPerSlot), ItemsPerSlot, "ItemsPerSlot must be at least 1");
    }
    base.OnParametersSet();
  }

  internal override string Signature() =>
    $"{base.Signature()}|{ItemsPerSlot}";
}

/// <summary>
/// A view of one or more days laid out horizontally, with a row for each resource when the scheduler is grouped
/// </summary>
public class PixataSchedulerTimelineView<TItem> : PixataSchedulerTimeViewBase<TItem> {
  /// <summary>
  /// The number of days shown. 1 by default
  /// </summary>
  [Parameter]
  public int NumberOfDays { get; set; } = 1;

  /// <summary>
  /// The width of each column (one division of a time slot), in pixels. When not set, the <c>--pixata-blazor-scheduler-timeline-column-width</c>
  /// CSS variable is used
  /// </summary>
  [Parameter]
  public decimal? ColumnWidth { get; set; }

  public override PixataSchedulerView View => PixataSchedulerView.Timeline;

  internal override int DayCount => Math.Max(1, NumberOfDays);

  internal override string Signature() =>
    $"{base.Signature()}|{NumberOfDays}|{ColumnWidth}";
}

/// <summary>
/// A list of the appointments, day by day, starting on the scheduler's date
/// </summary>
public class PixataSchedulerAgendaView<TItem> : PixataSchedulerViewBase<TItem> {
  /// <summary>
  /// The number of days shown. 7 by default
  /// </summary>
  [Parameter]
  public int NumberOfDays { get; set; } = 7;

  /// <summary>
  /// When true (the default), days without appointments aren't listed
  /// </summary>
  [Parameter]
  public bool HideEmptyAgendaDays { get; set; } = true;

  public override PixataSchedulerView View => PixataSchedulerView.Agenda;

  internal override int DayCount => Math.Max(1, NumberOfDays);

  internal override string Signature() =>
    $"{base.Signature()}|{NumberOfDays}|{HideEmptyAgendaDays}";
}
