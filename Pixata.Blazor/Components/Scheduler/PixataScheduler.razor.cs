using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// Shows appointments in day, multi-day, week, month, timeline and agenda views, and lets the user create, edit, move,
/// resize and delete them. See Readme.Scheduler.md for how to use it
/// </summary>
/// <typeparam name="TItem">The type of the appointments in <see cref="Data"/></typeparam>
[CascadingTypeParameter(nameof(TItem))]
public partial class PixataScheduler<TItem> : ComponentBase, IAsyncDisposable {
  private static readonly PixataSchedulerMessages DefaultMessages = new();
  private static readonly DayOfWeek[] DefaultWorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  #region Parameters

  /// <summary>
  /// The appointments
  /// </summary>
  [Parameter]
  public IEnumerable<TItem>? Data { get; set; }

  /// <summary>
  /// The date the current view shows (or contains, for the week and month views). Today by default. Use
  /// <c>@bind-Date</c> to keep it in step with the user's navigation
  /// </summary>
  [Parameter]
  public DateTime Date { get; set; } = DateTime.Today;

  [Parameter]
  public EventCallback<DateTime> DateChanged { get; set; }

  /// <summary>
  /// The current view. Defaults to the first view declared. Use <c>@bind-View</c> to keep it in step with the user's choice
  /// </summary>
  [Parameter]
  public PixataSchedulerView View { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerView> ViewChanged { get; set; }

  /// <summary>
  /// The views the user can choose from: <see cref="PixataSchedulerDayView{TItem}"/>,
  /// <see cref="PixataSchedulerWeekView{TItem}"/> and so on. When there are none, day, week and month views are available
  /// </summary>
  [Parameter]
  public RenderFragment? SchedulerViews { get; set; }

  /// <summary>
  /// The resources appointments can be linked to: <see cref="PixataSchedulerResource{TItem}"/> tags
  /// </summary>
  [Parameter]
  public RenderFragment? SchedulerResources { get; set; }

  /// <summary>
  /// Settings: <see cref="PixataSchedulerGroupSettings{TItem}"/>, <see cref="PixataSchedulerPopupEditSettings{TItem}"/> and
  /// <see cref="PixataSchedulerPopupEditFormSettings{TItem}"/>
  /// </summary>
  [Parameter]
  public RenderFragment? SchedulerSettings { get; set; }

  /// <summary>
  /// Replaces the toolbar. Use the <c>PixataSchedulerToolBar...Tool</c> components to arrange the built-in tools and add your
  /// own
  /// </summary>
  [Parameter]
  public RenderFragment? SchedulerToolBar { get; set; }

  /// <summary>
  /// The template for the content of each appointment. The context is the appointment (for an occurrence of a recurring
  /// appointment, a copy of the series with the occurrence's start and end)
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? ItemTemplate { get; set; }

  /// <summary>
  /// The template for the appointments in the all-day row of the day, week and multi-day views. When not set, they show
  /// their title (<see cref="ItemTemplate"/> isn't used there, as the row is only one line high)
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? AllDayItemTemplate { get; set; }

  /// <summary>The appointment field holding its unique id. <c>Id</c> by default</summary>
  [Parameter]
  public string IdField { get; set; } = PixataSchedulerItemFields.DefaultId;

  /// <summary>The appointment field holding its title. <c>Title</c> by default</summary>
  [Parameter]
  public string TitleField { get; set; } = PixataSchedulerItemFields.DefaultTitle;

  /// <summary>The appointment field holding its start. <c>Start</c> by default</summary>
  [Parameter]
  public string StartField { get; set; } = PixataSchedulerItemFields.DefaultStart;

  /// <summary>The appointment field holding its end. <c>End</c> by default</summary>
  [Parameter]
  public string EndField { get; set; } = PixataSchedulerItemFields.DefaultEnd;

  /// <summary>The appointment field saying whether it is all day. <c>IsAllDay</c> by default</summary>
  [Parameter]
  public string IsAllDayField { get; set; } = PixataSchedulerItemFields.DefaultIsAllDay;

  /// <summary>The appointment field holding its description. <c>Description</c> by default</summary>
  [Parameter]
  public string DescriptionField { get; set; } = PixataSchedulerItemFields.DefaultDescription;

  /// <summary>The appointment field saying whether it can't be changed. <c>ReadOnly</c> by default</summary>
  [Parameter]
  public string ReadOnlyField { get; set; } = PixataSchedulerItemFields.DefaultReadOnly;

  /// <summary>The appointment field holding its recurrence rule. <c>RecurrenceRule</c> by default</summary>
  [Parameter]
  public string RecurrenceRuleField { get; set; } = PixataSchedulerItemFields.DefaultRecurrenceRule;

  /// <summary>The field of a recurring appointment listing the starts of the occurrences that are exceptions. <c>RecurrenceExceptions</c> by default</summary>
  [Parameter]
  public string RecurrenceExceptionsField { get; set; } = PixataSchedulerItemFields.DefaultRecurrenceExceptions;

  /// <summary>The field of an exception holding the id of the recurring appointment it belongs to. <c>RecurrenceId</c> by default</summary>
  [Parameter]
  public string RecurrenceIdField { get; set; } = PixataSchedulerItemFields.DefaultRecurrenceId;

  /// <summary>
  /// Whether the user can create appointments (by double-clicking a slot, pressing Enter on one, or the toolbar's button)
  /// </summary>
  [Parameter]
  public bool AllowCreate { get; set; }

  /// <summary>
  /// Whether the user can edit appointments (by double-clicking one or pressing Enter on it), and move and resize them
  /// </summary>
  [Parameter]
  public bool AllowUpdate { get; set; }

  /// <summary>
  /// Whether the user can delete appointments (with their delete button, the Delete key, or from the edit form)
  /// </summary>
  [Parameter]
  public bool AllowDelete { get; set; }

  /// <summary>
  /// When true, the user is asked to confirm before an appointment is deleted
  /// </summary>
  [Parameter]
  public bool ConfirmDelete { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerCreateEventArgs> OnCreate { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerEditEventArgs> OnEdit { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerUpdateEventArgs> OnUpdate { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerCancelEventArgs> OnCancel { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerDeleteEventArgs> OnDelete { get; set; }

  /// <summary>
  /// Called as each appointment is rendered, so you can add CSS classes to it
  /// </summary>
  [Parameter]
  public Action<PixataSchedulerItemRenderEventArgs>? OnItemRender { get; set; }

  /// <summary>
  /// Called as each slot is rendered, so you can add CSS classes to it
  /// </summary>
  [Parameter]
  public Action<PixataSchedulerCellRenderEventArgs>? OnCellRender { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerItemClickEventArgs> OnItemClick { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerItemDoubleClickEventArgs> OnItemDoubleClick { get; set; }

  [Parameter]
  public EventCallback<PixataSchedulerItemContextMenuEventArgs> OnItemContextMenu { get; set; }

  /// <summary>
  /// Creates the item for a new appointment. Needed when <typeparamref name="TItem"/> has no parameterless constructor, or
  /// to give new items default values
  /// </summary>
  [Parameter]
  public Func<TItem>? OnModelInit { get; set; }

  /// <summary>
  /// When true, the views with time slots only show the working day. The footer has a button to toggle it
  /// </summary>
  [Parameter]
  public bool ShowWorkHours { get; set; }

  [Parameter]
  public EventCallback<bool> ShowWorkHoursChanged { get; set; }

  /// <summary>
  /// The working days of the week, which have a different background in the time views. Monday to Friday by default
  /// </summary>
  [Parameter]
  public IEnumerable<DayOfWeek>? WorkDays { get; set; }

  /// <summary>
  /// The culture for day and month names, date and time formats and the first day of the week. Defaults to the current
  /// culture
  /// </summary>
  [Parameter]
  public CultureInfo? Culture { get; set; }

  /// <summary>
  /// Overrides the culture's first day of the week
  /// </summary>
  [Parameter]
  public DayOfWeek? FirstDayOfWeek { get; set; }

  /// <summary>
  /// The view used on phones (screens narrower than <see cref="MobileBreakpoint"/>) when the scheduler first shows a view
  /// with several days. Defaults to the day view, or the agenda view if there is no day view
  /// </summary>
  [Parameter]
  public PixataSchedulerView? MobileView { get; set; }

  /// <summary>
  /// The screen width, in pixels, below which the scheduler starts in <see cref="MobileView"/>. 768 by default; 0 turns this
  /// off
  /// </summary>
  [Parameter]
  public int MobileBreakpoint { get; set; } = 768;

  /// <summary>
  /// The text the scheduler shows, for translation
  /// </summary>
  [Parameter]
  public PixataSchedulerMessages? Messages { get; set; }

  /// <summary>
  /// The scheduler's height, eg <c>600px</c>. The views scroll inside it. When not set, the scheduler is as tall as its views
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? Width { get; set; }

  [Parameter]
  public string? Class { get; set; }

  [Parameter]
  public string? Id { get; set; }

  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  #endregion

  #region State

  private readonly List<PixataSchedulerViewBase<TItem>> _views = [];
  private readonly List<PixataSchedulerSettingBase<TItem>> _settings = [];
  private readonly List<PixataSchedulerViewBase<TItem>> _defaultViews = [new PixataSchedulerDayView<TItem>(), new PixataSchedulerWeekView<TItem>(), new PixataSchedulerMonthView<TItem>()];
  private ElementReference _element;
  private ElementReference _content;
  private DotNetObjectReference<PixataScheduler<TItem>>? _dotNetReference;
  private int _dragHandle;
  private bool _disposed;
  private bool _viewParameterGiven;
  private PixataSchedulerView? _lastViewParameter;
  private DateTime? _lastDateParameter;
  private bool? _lastShowWorkHoursParameter;
  private PixataSchedulerView? _view;
  private DateTime _date = DateTime.Today;
  private bool _showWorkHours;
  private PixataSchedulerItemFields _fields = new();
  private bool _validated;
  private bool _stale = true;
  private List<PixataSchedulerItem<TItem>> _items = [];
  private HashSet<string> _itemKeys = [];
  private string? _firstKey;
  private List<PixataSchedulerGroup> _groups = [new()];
  private object? _model;
  private string? _selectedKey;
  private PixataSchedulerSlot? _selectedSlot;
  private bool _scrollPending = true;
  private bool _focusContentPending;
  private bool _focusDialogPending;
  private string? _focusItemKey;
  private bool _mobileChecked;
  private string _announcement = "";

  #endregion

  #region Values the views use

  internal CultureInfo CurrentCulture => Culture ?? CultureInfo.CurrentCulture;

  internal PixataSchedulerMessages Text => Messages ?? DefaultMessages;

  internal DayOfWeek WeekStart => FirstDayOfWeek ?? CurrentCulture.DateTimeFormat.FirstDayOfWeek;

  internal IReadOnlyList<PixataSchedulerViewBase<TItem>> AvailableViews => _views.Count > 0 ? _views : _defaultViews;

  /// <summary>
  /// The settings of the view being shown
  /// </summary>
  internal PixataSchedulerViewBase<TItem> ActiveView =>
    AvailableViews.FirstOrDefault(v => v.View == _view) ?? AvailableViews[0];

  /// <summary>
  /// The view being shown
  /// </summary>
  internal PixataSchedulerView CurrentView => ActiveView.View;

  /// <summary>
  /// The date being shown
  /// </summary>
  internal DateTime CurrentDate => _date;

  internal bool WorkHoursOnly => _showWorkHours;

  internal (DateTime Start, DateTime End) Range =>
    PixataSchedulerDates.VisibleRange(CurrentView, _date, ActiveView.DayCount, WeekStart);

  internal string RangeText =>
    PixataSchedulerDates.RangeText(CurrentView, _date, Range.Start, Range.End, CurrentCulture);

  internal IReadOnlyList<PixataSchedulerResource<TItem>> Resources =>
    _settings.OfType<PixataSchedulerResource<TItem>>().ToList();

  internal PixataSchedulerGroupSettings<TItem>? GroupSettings =>
    _settings.OfType<PixataSchedulerGroupSettings<TItem>>().LastOrDefault();

  internal PixataSchedulerPopupEditSettings<TItem>? PopupEditSettings =>
    _settings.OfType<PixataSchedulerPopupEditSettings<TItem>>().LastOrDefault();

  internal PixataSchedulerPopupEditFormSettings<TItem>? PopupEditFormSettings =>
    _settings.OfType<PixataSchedulerPopupEditFormSettings<TItem>>().LastOrDefault();

  internal bool IsGrouped => _groups.Any(g => g.Levels.Count > 0);

  internal bool IsVerticalGrouping => IsGrouped && GroupSettings?.Orientation == PixataSchedulerGroupOrientation.Vertical;

  internal IReadOnlyList<PixataSchedulerGroup> Groups => _groups;

  internal PixataSchedulerItemFields Fields => _fields;

  internal bool CanDrag => AllowUpdate;

  internal string? SelectedKey => _selectedKey;

  internal PixataSchedulerSlot? SelectedSlot => _selectedSlot;

  internal bool IsTimeView => CurrentView is PixataSchedulerView.Day or PixataSchedulerView.MultiDay or PixataSchedulerView.Week or PixataSchedulerView.Timeline;

  internal string ViewName(PixataSchedulerView view) =>
    view switch {
      PixataSchedulerView.Day => Text.DayView,
      PixataSchedulerView.MultiDay => Text.MultiDayView,
      PixataSchedulerView.Week => Text.WeekView,
      PixataSchedulerView.Month => Text.MonthView,
      PixataSchedulerView.Timeline => Text.TimelineView,
      _ => Text.AgendaView
    };

  /// <summary>
  /// The time of day the time views start and end, taking <see cref="ShowWorkHours"/> into account
  /// </summary>
  internal (TimeSpan Start, TimeSpan End) DayWindow(PixataSchedulerTimeViewBase<TItem> view) =>
    _showWorkHours
      ? PixataSchedulerDates.DayWindow(view.WorkDayStart, view.WorkDayEnd)
      : PixataSchedulerDates.DayWindow(view.StartTime, view.EndTime);

  internal bool IsWorkDay(DateTime day) =>
    (WorkDays ?? DefaultWorkDays).Contains(day.DayOfWeek);

  internal bool IsWorkTime(PixataSchedulerTimeViewBase<TItem> view, DateTime start) {
    (TimeSpan workStart, TimeSpan workEnd) = PixataSchedulerDates.DayWindow(view.WorkDayStart, view.WorkDayEnd);
    return IsWorkDay(start) && start.TimeOfDay >= workStart && start.TimeOfDay < workEnd;
  }

  internal T? Model<T>() where T : class {
    Prepare();
    return _model as T;
  }

  #endregion

  #region Registration of the views and settings

  internal void AddView(PixataSchedulerViewBase<TItem> view) {
    _views.Add(view);
    ViewSettingsChanged();
  }

  internal void RemoveView(PixataSchedulerViewBase<TItem> view) {
    if (_views.Remove(view) && !_disposed) {
      ViewSettingsChanged();
    }
  }

  internal void AddSetting(PixataSchedulerSettingBase<TItem> setting) {
    _settings.Add(setting);
    ViewSettingsChanged();
  }

  internal void RemoveSetting(PixataSchedulerSettingBase<TItem> setting) {
    if (_settings.Remove(setting) && !_disposed) {
      ViewSettingsChanged();
    }
  }

  /// <summary>
  /// Called by the views and settings when they change, so the scheduler lays itself out again
  /// </summary>
  internal void ViewSettingsChanged() {
    _stale = true;
    if (!_disposed) {
      StateHasChanged();
    }
  }

  #endregion

  #region Lifecycle

  public override Task SetParametersAsync(ParameterView parameters) {
    if (parameters.TryGetValue(nameof(View), out PixataSchedulerView _)) {
      _viewParameterGiven = true;
    }
    return base.SetParametersAsync(parameters);
  }

  protected override void OnParametersSet() {
    // The view, date and work hours follow their parameters when those change, but otherwise keep what the user chose, so
    // the scheduler works without two-way binding
    if (_viewParameterGiven && View != _lastViewParameter) {
      _view = View;
      _lastViewParameter = View;
      _scrollPending = true;
    }
    if (Date != _lastDateParameter) {
      _date = Date.Date;
      _lastDateParameter = Date;
    }
    if (ShowWorkHours != _lastShowWorkHoursParameter) {
      _showWorkHours = ShowWorkHours;
      _lastShowWorkHoursParameter = ShowWorkHours;
    }
    PixataSchedulerItemFields fields = new() {
      Id = IdField,
      Title = TitleField,
      Start = StartField,
      End = EndField,
      IsAllDay = IsAllDayField,
      Description = DescriptionField,
      ReadOnly = ReadOnlyField,
      RecurrenceRule = RecurrenceRuleField,
      RecurrenceExceptions = RecurrenceExceptionsField,
      RecurrenceId = RecurrenceIdField
    };
    if (!SameFields(fields, _fields)) {
      _fields = fields;
      _validated = false;
    }
    if (AllowCreate && OnModelInit is null && !typeof(TItem).IsValueType && typeof(TItem).GetConstructor(Type.EmptyTypes) is null) {
      throw new InvalidOperationException($"The scheduler can't create new {typeof(TItem).Name} appointments, as the type has no parameterless constructor. Set OnModelInit, or set AllowCreate to false");
    }
    _stale = true;
  }

  private static bool SameFields(PixataSchedulerItemFields a, PixataSchedulerItemFields b) =>
    a.Id == b.Id && a.Title == b.Title && a.Start == b.Start && a.End == b.End && a.IsAllDay == b.IsAllDay && a.Description == b.Description
    && a.ReadOnly == b.ReadOnly && a.RecurrenceRule == b.RecurrenceRule && a.RecurrenceExceptions == b.RecurrenceExceptions && a.RecurrenceId == b.RecurrenceId;

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_disposed) {
      return;
    }
    if (firstRender) {
      _dotNetReference = DotNetObjectReference.Create(this);
      _dragHandle = await PixataSchedulerJs.Invoke(Js, "attach", 0, _element, _dotNetReference);
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", _content, new[] { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "PageUp", "PageDown" });
    }
    if (!_mobileChecked) {
      _mobileChecked = true;
      if (MobileBreakpoint > 0 && CurrentView is PixataSchedulerView.Week or PixataSchedulerView.MultiDay or PixataSchedulerView.Month or PixataSchedulerView.Timeline
          && await PixataJs.Invoke(Js, "matchesMedia", false, $"(max-width: {MobileBreakpoint - 1}px)")) {
        PixataSchedulerView? mobile = MobileView is { } chosen && AvailableViews.Any(v => v.View == chosen)
          ? chosen
          : AvailableViews.Select(v => (PixataSchedulerView?)v.View).FirstOrDefault(v => v == PixataSchedulerView.Day)
            ?? AvailableViews.Select(v => (PixataSchedulerView?)v.View).FirstOrDefault(v => v == PixataSchedulerView.Agenda);
        if (mobile is { } view && view != CurrentView) {
          await ChangeView(view);
          StateHasChanged();
          return;
        }
      }
    }
    if (_scrollPending && IsTimeView && CurrentView != PixataSchedulerView.Timeline) {
      _scrollPending = false;
      await PixataSchedulerJs.InvokeVoid(Js, "scrollToSelector", _content, "[data-pixata-scroll-to]");
    } else if (_scrollPending && CurrentView == PixataSchedulerView.Timeline) {
      _scrollPending = false;
      await PixataSchedulerJs.InvokeVoid(Js, "scrollToSelector", _content, "[data-pixata-scroll-to]", true);
    }
    if (_focusDialogPending) {
      _focusDialogPending = false;
      await PixataJs.Invoke(Js, "focusFirst", false, _prompt is null ? _dialogElement : _promptElement);
    }
    if (_focusItemKey is { } key) {
      _focusItemKey = null;
      await PixataJs.Invoke(Js, "focusSelector", false, _content, KeySelector(key));
    } else if (_focusContentPending) {
      _focusContentPending = false;
      await PixataJs.InvokeVoid(Js, "focusElement", _content, true);
    }
  }

  /// <summary>
  /// The CSS selector for an appointment's element. The key comes from the appointment's id, so quotes and backslashes in it
  /// are escaped (an invalid selector would make the browser throw)
  /// </summary>
  internal static string KeySelector(string key) =>
    $"[data-pixata-key=\"{key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\a ").Replace("\r", "\\d ")}\"]";

  /// <summary>
  /// Reads the data again and re-renders the scheduler, for when the data has changed in a way the scheduler can't see (an
  /// item changed inside the same collection)
  /// </summary>
  public void Rebind() {
    _stale = true;
    StateHasChanged();
  }

  /// <summary>
  /// Re-renders the scheduler
  /// </summary>
  public void Refresh() =>
    StateHasChanged();

  public async ValueTask DisposeAsync() {
    _disposed = true;
    if (_dragHandle != 0) {
      await PixataSchedulerJs.InvokeVoid(Js, "detach", _dragHandle);
    }
    _dotNetReference?.Dispose();
    GC.SuppressFinalize(this);
  }

  #endregion

  #region Building the view

  /// <summary>
  /// Works out the appointments and the layout of the current view, if anything has changed since it was last done
  /// </summary>
  private void Prepare() {
    if (!_stale) {
      return;
    }
    _stale = false;
    _groups = BuildGroups();
    (DateTime start, DateTime end) = Range;
    _items = BuildItems(start, end);
    _itemKeys = _items.Select(i => i.Key).ToHashSet();
    _firstKey = _items.OrderBy(i => i.Start).ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault()?.Key;
    PixataSchedulerViewBase<TItem> view = ActiveView;
    List<DateTime> days = Enumerable.Range(0, (end - start).Days).Select(d => start.AddDays(d)).ToList();
    bool vertical = IsVerticalGrouping;
    switch (view) {
      case PixataSchedulerDayViewBase<TItem> dayView: {
        (TimeSpan dayStart, TimeSpan dayEnd) = DayWindow(dayView);
        _model = PixataSchedulerModelBuilder.TimeGrid(_items, days, dayStart, dayEnd, PixataSchedulerDates.DivisionLength(dayView.SlotDuration, dayView.SlotDivisions),
          Math.Max(1, dayView.SlotDivisions), _groups, vertical, !dayView.HideAllDayRow);
        break;
      }
      case PixataSchedulerTimelineView<TItem> timeline: {
        (TimeSpan dayStart, TimeSpan dayEnd) = DayWindow(timeline);
        _model = PixataSchedulerModelBuilder.Timeline(_items, days, dayStart, dayEnd, PixataSchedulerDates.DivisionLength(timeline.SlotDuration, timeline.SlotDivisions),
          Math.Max(1, timeline.SlotDivisions), _groups, vertical);
        break;
      }
      case PixataSchedulerMonthView<TItem> month:
        _model = PixataSchedulerModelBuilder.Month(_items, days, _groups, vertical, month.ItemsPerSlot);
        break;
      case PixataSchedulerAgendaView<TItem> agenda:
        _model = PixataSchedulerModelBuilder.Agenda(_items, days, _groups, agenda.HideEmptyAgendaDays);
        break;
    }
  }

  private List<PixataSchedulerGroup> BuildGroups() {
    List<string> fields = GroupSettings?.Resources ?? [];
    List<(string Field, IReadOnlyList<(object? Value, string Text, string? Colour)> Items)> resources = [];
    foreach (string field in fields) {
      PixataSchedulerResource<TItem> resource = Resources.FirstOrDefault(r => r.Field == field)
        ?? throw new InvalidOperationException($"The scheduler is grouped by {field}, but there is no PixataSchedulerResource with that Field");
      resources.Add((resource.Field, resource.Items));
    }
    return PixataSchedulerModelBuilder.Groups(resources);
  }

  /// <summary>
  /// The appointments that appear between <paramref name="from"/> and <paramref name="to"/>, with recurring appointments
  /// expanded into their occurrences
  /// </summary>
  private List<PixataSchedulerItem<TItem>> BuildItems(DateTime from, DateTime to) {
    List<PixataSchedulerItem<TItem>> result = [];
    if (Data is null) {
      return result;
    }
    IReadOnlyList<PixataSchedulerResource<TItem>> resources = Resources;
    HashSet<string> usedKeys = [];
    int index = -1;
    foreach (TItem dataItem in Data) {
      index++;
      if (dataItem is null) {
        continue;
      }
      object item = dataItem;
      if (!_validated) {
        _fields.Validate(item);
        _validated = true;
      }
      DateTime start = _fields.GetStart(item);
      DateTime end = _fields.GetEnd(item);
      bool isAllDay = _fields.GetIsAllDay(item);
      object? id = _fields.GetId(item);
      object? recurrenceId = _fields.GetRecurrenceId(item);
      bool isException = !PixataSchedulerItemFields.IsEmptyId(recurrenceId);
      Dictionary<string, object?> resourceValues = resources.GroupBy(r => r.Field).ToDictionary(g => g.Key, g => _fields.GetField(item, g.Key));
      string? colour = resources.Select(r => r.Find(resourceValues[r.Field])?.Colour).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
      string key = PixataSchedulerItemFields.IsEmptyId(id) ? $"#{index}" : Convert.ToString(id, CultureInfo.InvariantCulture) ?? $"#{index}";
      if (!usedKeys.Add(key)) {
        // Two appointments with the same id: keep them apart
        key = $"{key}#{index}";
        usedKeys.Add(key);
      }
      string title = _fields.GetTitle(item);
      string description = _fields.GetDescription(item);
      bool readOnly = _fields.GetReadOnly(item);
      if (!isException && PixataRecurrenceRule.TryParse(_fields.GetRecurrenceRule(item), out PixataRecurrenceRule? rule)) {
        TimeSpan duration = end - start;
        HashSet<DateTime> exceptions = _fields.GetRecurrenceExceptions(item).Select(d => d.Date).ToHashSet();
        // Occurrences that start before the range can last into it
        DateTime earliest = new(Math.Max(0, from.Ticks - duration.Ticks - TimeSpan.TicksPerDay));
        foreach (DateTime occurrenceStart in rule!.GetOccurrences(start, earliest, to, WeekStart)) {
          DateTime occurrenceEnd = occurrenceStart + duration;
          if (exceptions.Contains(occurrenceStart.Date) || !Overlaps(occurrenceStart, occurrenceEnd, isAllDay, from, to)) {
            continue;
          }
          TItem copy = PixataSchedulerItemFields.Clone(dataItem);
          _fields.SetStart(copy!, occurrenceStart);
          _fields.SetEnd(copy!, occurrenceEnd);
          result.Add(new() {
            Key = $"{key}@{occurrenceStart.Ticks}",
            Item = copy,
            DataItem = dataItem,
            Id = id,
            Title = title,
            Description = description,
            Start = occurrenceStart,
            End = occurrenceEnd,
            IsAllDay = isAllDay,
            ReadOnly = readOnly,
            IsOccurrence = true,
            OccurrenceStart = occurrenceStart,
            Colour = colour,
            Resources = resourceValues
          });
        }
      } else if (Overlaps(start, end, isAllDay, from, to)) {
        result.Add(new() {
          Key = key,
          Item = dataItem,
          DataItem = dataItem,
          Id = id,
          Title = title,
          Description = description,
          Start = start,
          End = end,
          IsAllDay = isAllDay,
          ReadOnly = readOnly,
          IsException = isException,
          OccurrenceStart = start,
          Colour = colour,
          Resources = resourceValues
        });
      }
    }
    return result;
  }

  private static bool Overlaps(DateTime start, DateTime end, bool isAllDay, DateTime from, DateTime to) {
    if (isAllDay) {
      (DateTime first, DateTime last) = PixataSchedulerDates.DaySpan(start, end);
      return first < to && last > from;
    }
    return start < to && (end > from || (end == start && start >= from));
  }


  /// <summary>
  /// The CSS classes <see cref="OnItemRender"/> adds to an appointment
  /// </summary>
  internal string? ItemClass(PixataSchedulerItem<TItem> item) {
    if (OnItemRender is null) {
      return null;
    }
    PixataSchedulerItemRenderEventArgs args = new() { Item = item.Item! };
    OnItemRender(args);
    return args.Class;
  }

  /// <summary>
  /// The CSS classes <see cref="OnCellRender"/> adds to a slot
  /// </summary>
  internal string? SlotClass(PixataSchedulerSlot slot) {
    if (OnCellRender is null) {
      return null;
    }
    PixataSchedulerGroup group = slot.Group >= 0 && slot.Group < _groups.Count ? _groups[slot.Group] : new();
    PixataSchedulerCellRenderEventArgs args = new() { Start = slot.Start, End = slot.End, IsAllDay = slot.IsAllDay, Resources = group.Resources };
    OnCellRender(args);
    return args.Class;
  }

  /// <summary>
  /// The appointment that the Tab key reaches: the selected one, or else the first one
  /// </summary>
  internal string? TabStopKey =>
    _selectedKey is { } key && _itemKeys.Contains(key) ? key : _firstKey;

  /// <summary>
  /// The template for an appointment, or null to use the built-in rendering
  /// </summary>
  internal RenderFragment<TItem>? TemplateFor(PixataSchedulerItem<TItem> item, bool inAllDayRow) {
    PixataSchedulerViewBase<TItem> view = ActiveView;
    return inAllDayRow
      ? (view as PixataSchedulerDayViewBase<TItem>)?.AllDayItemTemplate ?? AllDayItemTemplate
      : view.ItemTemplate ?? ItemTemplate;
  }

  /// <summary>
  /// The screen reader label for an appointment
  /// </summary>
  internal string ItemLabel(PixataSchedulerItem<TItem> item) {
    CultureInfo culture = CurrentCulture;
    string format = item.IsAllDay ? culture.DateTimeFormat.ShortDatePattern : $"{culture.DateTimeFormat.ShortDatePattern} {culture.DateTimeFormat.ShortTimePattern}";
    return string.Format(culture, Text.EventLabelFormat, item.Title, item.Start.ToString(format, culture), item.End.ToString(format, culture));
  }

  /// <summary>
  /// The text for an appointment's times in the built-in rendering
  /// </summary>
  internal string ItemTimeText(PixataSchedulerItem<TItem> item) =>
    item.IsAllDay ? item.Start.ToString(CurrentCulture.DateTimeFormat.ShortDatePattern, CurrentCulture) : PixataSchedulerDates.TimeRange(item.Start, item.End, CurrentCulture);

  internal bool CanEdit(PixataSchedulerItem<TItem> item) =>
    AllowUpdate && !item.ReadOnly;

  internal bool CanDelete(PixataSchedulerItem<TItem> item) =>
    AllowDelete && !item.ReadOnly;

  #endregion

  #region Navigation

  internal async Task ChangeView(PixataSchedulerView view, DateTime? date = null) {
    bool viewChanged = view != CurrentView;
    _view = view;
    _scrollPending = true;
    _stale = true;
    _selectedSlot = null;
    if (viewChanged) {
      await ViewChanged.InvokeAsync(view);
    }
    if (date is { } newDate) {
      await ChangeDate(newDate);
    }
    Announce();
  }

  internal async Task ChangeDate(DateTime date) {
    if (date.Date == _date) {
      return;
    }
    _date = date.Date;
    _stale = true;
    _selectedSlot = null;
    await DateChanged.InvokeAsync(_date);
    Announce();
  }

  internal Task Navigate(int step) =>
    ChangeDate(PixataSchedulerDates.Navigate(CurrentView, _date, ActiveView.DayCount, step));

  internal Task GoToToday() =>
    ChangeDate(DateTime.Today);

  internal async Task ToggleWorkHours() {
    _showWorkHours = !_showWorkHours;
    _stale = true;
    _scrollPending = true;
    await ShowWorkHoursChanged.InvokeAsync(_showWorkHours);
  }

  /// <summary>
  /// Clicking a date header goes to the day view for that date, if there is one
  /// </summary>
  internal bool CanDrillDown => AvailableViews.Any(v => v.View == PixataSchedulerView.Day) && CurrentView != PixataSchedulerView.Day;

  internal Task DrillDown(DateTime date) =>
    CanDrillDown ? ChangeView(PixataSchedulerView.Day, date) : Task.CompletedTask;

  private void Announce() =>
    _announcement = $"{ViewName(CurrentView)}: {RangeText}";

  #endregion

  #region Selection and keyboard

  internal void SelectSlot(PixataSchedulerSlot slot) {
    _selectedSlot = slot;
    _selectedKey = null;
  }

  internal bool IsSlotSelected(PixataSchedulerSlot slot) =>
    _selectedSlot is { } selected && selected.Start == slot.Start && selected.IsAllDay == slot.IsAllDay && selected.Group == slot.Group;

  internal async Task SlotClicked(PixataSchedulerSlot slot, MouseEventArgs args) {
    SelectSlot(slot);
    if (args.Detail >= 2) {
      await CreateAt(slot);
    }
  }

  internal async Task ItemClicked(PixataSchedulerItem<TItem> item, MouseEventArgs args) {
    _selectedKey = item.Key;
    _selectedSlot = null;
    if (OnItemClick.HasDelegate) {
      await OnItemClick.InvokeAsync(new() { Item = item.Item!, EventArgs = args });
    }
  }

  internal async Task ItemDoubleClicked(PixataSchedulerItem<TItem> item, MouseEventArgs args) {
    if (OnItemDoubleClick.HasDelegate) {
      await OnItemDoubleClick.InvokeAsync(new() { Item = item.Item!, EventArgs = args });
    }
    await EditItem(item);
  }

  internal async Task ItemContextMenu(PixataSchedulerItem<TItem> item, MouseEventArgs args) {
    _selectedKey = item.Key;
    if (OnItemContextMenu.HasDelegate) {
      await OnItemContextMenu.InvokeAsync(new() { Item = item.Item!, EventArgs = args });
    }
  }

  internal async Task ItemKeyDown(PixataSchedulerItem<TItem> item, KeyboardEventArgs args) {
    switch (args.Key) {
      case "Enter" or "F2":
        _selectedKey = item.Key;
        await EditItem(item);
        break;
      case "Delete" or "Backspace":
        await DeleteItem(item);
        break;
      case " " or "Spacebar":
        await ItemClicked(item, new MouseEventArgs { Detail = 1 });
        break;
      case "Escape":
        _selectedKey = null;
        _focusContentPending = true;
        break;
      case "ArrowDown" or "ArrowRight" or "ArrowUp" or "ArrowLeft": {
        List<PixataSchedulerItem<TItem>> ordered = _items.OrderBy(i => i.Start).ThenBy(i => i.Key, StringComparer.Ordinal).ToList();
        int index = ordered.FindIndex(i => i.Key == item.Key);
        int next = index + (args.Key is "ArrowDown" or "ArrowRight" ? 1 : -1);
        if (index >= 0 && next >= 0 && next < ordered.Count) {
          _selectedKey = ordered[next].Key;
          _focusItemKey = ordered[next].Key;
        }
        break;
      }
    }
  }

  private async Task ContentKeyDown(KeyboardEventArgs args) {
    switch (args.Key) {
      case "PageUp":
        await Navigate(-1);
        return;
      case "PageDown":
        await Navigate(1);
        return;
      case "t" or "T" when !args.CtrlKey && !args.AltKey && !args.MetaKey:
        await GoToToday();
        return;
      case "Escape":
        _selectedSlot = null;
        _selectedKey = null;
        return;
      case "Enter" when _selectedSlot is { } slot:
        await CreateAt(slot);
        return;
      case "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight":
        MoveSlot(args.Key);
        return;
    }
  }

  /// <summary>
  /// Moves the selected slot with the arrow keys: up and down by one slot division (or week, in the month view), left and
  /// right by a day
  /// </summary>
  private void MoveSlot(string key) {
    PixataSchedulerSlot? current = _selectedSlot;
    (DateTime rangeStart, DateTime rangeEnd) = Range;
    int horizontal = key == "ArrowRight" ? 1 : key == "ArrowLeft" ? -1 : 0;
    int vertical = key == "ArrowDown" ? 1 : key == "ArrowUp" ? -1 : 0;
    switch (ActiveView) {
      case PixataSchedulerDayViewBase<TItem> dayView: {
        (TimeSpan dayStart, TimeSpan dayEnd) = DayWindow(dayView);
        TimeSpan division = PixataSchedulerDates.DivisionLength(dayView.SlotDuration, dayView.SlotDivisions);
        if (current is not { } slot || slot.Start < rangeStart || slot.Start >= rangeEnd) {
          _selectedSlot = new(rangeStart + dayStart, rangeStart + dayStart + division, false, 0);
          return;
        }
        if (slot.IsAllDay) {
          DateTime day = Clamp(slot.Start.Date.AddDays(horizontal), rangeStart, rangeEnd.AddDays(-1));
          _selectedSlot = vertical > 0 ? new(day + dayStart, day + dayStart + division, false, slot.Group) : new(day, day.AddDays(1), true, slot.Group);
          return;
        }
        DateTime start = slot.Start.AddDays(horizontal) + division * vertical;
        if (start.Date != slot.Start.AddDays(horizontal).Date || start.TimeOfDay < dayStart) {
          if (vertical < 0 && !dayView.HideAllDayRow) {
            DateTime day = slot.Start.Date;
            _selectedSlot = new(day, day.AddDays(1), true, slot.Group);
          }
          return;
        }
        if (start.TimeOfDay + division > dayEnd || start < rangeStart || start >= rangeEnd) {
          return;
        }
        _selectedSlot = new(start, start + division, false, slot.Group);
        return;
      }
      case PixataSchedulerMonthView<TItem>: {
        DateTime day = current is { } slot && slot.Start >= rangeStart && slot.Start < rangeEnd
          ? slot.Start.Date.AddDays(horizontal + vertical * 7)
          : (_date >= rangeStart && _date < rangeEnd ? _date : rangeStart);
        day = Clamp(day, rangeStart, rangeEnd.AddDays(-1));
        _selectedSlot = new(day, day.AddDays(1), true, current?.Group ?? 0);
        return;
      }
      case PixataSchedulerTimelineView<TItem> timeline: {
        (TimeSpan dayStart, TimeSpan dayEnd) = DayWindow(timeline);
        TimeSpan division = PixataSchedulerDates.DivisionLength(timeline.SlotDuration, timeline.SlotDivisions);
        if (current is not { } slot || slot.Start < rangeStart || slot.Start >= rangeEnd) {
          _selectedSlot = new(rangeStart + dayStart, rangeStart + dayStart + division, false, 0);
          return;
        }
        DateTime start = slot.Start + division * horizontal;
        if (start.TimeOfDay < dayStart) {
          start = start.Date.AddDays(-1) + dayEnd - division;
        } else if (start.TimeOfDay + division > dayEnd) {
          start = start.Date.AddDays(1) + dayStart;
        }
        int group = Math.Clamp(slot.Group + vertical, 0, Math.Max(0, _groups.Count - 1));
        if (start >= rangeStart && start < rangeEnd) {
          _selectedSlot = new(start, start + division, false, group);
        }
        return;
      }
    }
  }

  private static DateTime Clamp(DateTime value, DateTime min, DateTime max) =>
    value < min ? min : value > max ? max : value;

  #endregion

  #region Editing

  private sealed class EditState {
    public required TItem Item { get; init; }
    public required bool IsNew { get; init; }
    public required PixataSchedulerRecurrenceEditMode EditMode { get; init; }
    public required EditContext EditContext { get; init; }
    public required ValidationMessageStore ValidationMessages { get; init; }
    public PixataSchedulerEditModel? Model { get; init; }
    public PixataSchedulerItem<TItem>? Source { get; init; }
    public DateTime OriginalStart { get; init; }

    /// <summary>
    /// True once the user has tried to save, after which the form is validated again whenever a field changes
    /// </summary>
    public bool Submitted { get; set; }
  }

  private sealed class PromptState {
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required List<(string Label, Func<Task> Action, bool Primary)> Buttons { get; init; }
  }

  private EditState? _edit;
  private PromptState? _prompt;
  private ElementReference _dialogElement;
  private ElementReference _promptElement;

  /// <summary>
  /// The slot a new appointment from the toolbar goes in: the selected slot, or the start of the working day on the date
  /// being shown
  /// </summary>
  private PixataSchedulerSlot DefaultSlot() {
    if (_selectedSlot is { } slot) {
      return slot;
    }
    (DateTime start, DateTime end) = Range;
    DateTime day = _date >= start && _date < end ? _date : start;
    if (ActiveView is PixataSchedulerTimeViewBase<TItem> timeView) {
      (TimeSpan dayStart, TimeSpan dayEnd) = DayWindow(timeView);
      TimeSpan workStart = timeView.WorkDayStart.TimeOfDay;
      TimeSpan time = workStart >= dayStart && workStart < dayEnd ? workStart : dayStart;
      TimeSpan length = TimeSpan.FromMinutes(Math.Max(1, timeView.SlotDuration));
      return new(day + time, day + time + length, false, 0);
    }
    return new(day, day.AddDays(1), true, 0);
  }

  internal Task CreateFromToolBar() =>
    CreateAt(DefaultSlot());

  /// <summary>
  /// Starts creating an appointment in a slot
  /// </summary>
  internal async Task CreateAt(PixataSchedulerSlot slot) {
    if (!AllowCreate) {
      return;
    }
    TItem item = NewItem();
    // An all-day appointment ends on the last day it covers
    DateTime end = slot.IsAllDay ? slot.Start.Date : slot.End;
    _fields.SetStart(item!, slot.Start);
    _fields.SetEnd(item!, end);
    _fields.SetIsAllDay(item!, slot.IsAllDay);
    if (slot.Group >= 0 && slot.Group < _groups.Count) {
      foreach (PixataSchedulerGroupLevel level in _groups[slot.Group].Levels) {
        _fields.SetField(item!, level.Field, level.Value);
      }
    }
    PixataSchedulerEditEventArgs args = new() { Item = item, IsNew = true, IsAllDay = slot.IsAllDay, Start = slot.Start, End = end };
    await OnEdit.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    OpenEditor(item, true, PixataSchedulerRecurrenceEditMode.Series, null);
  }

  /// <summary>
  /// Starts editing an appointment, asking first whether to edit the occurrence or the series for a recurring one
  /// </summary>
  internal Task EditItem(PixataSchedulerItem<TItem> item) {
    if (!CanEdit(item)) {
      return Task.CompletedTask;
    }
    if (item.IsOccurrence) {
      _prompt = new() {
        Title = Text.EditRecurringTitle,
        Text = Text.EditRecurringText,
        Buttons = [
          (Text.EditOccurrence, () => BeginEdit(item, PixataSchedulerRecurrenceEditMode.Occurrence), true),
          (Text.EditSeries, () => BeginEdit(item, PixataSchedulerRecurrenceEditMode.Series), false)
        ]
      };
      _focusDialogPending = true;
      return Task.CompletedTask;
    }
    return BeginEdit(item, PixataSchedulerRecurrenceEditMode.Series);
  }

  private async Task BeginEdit(PixataSchedulerItem<TItem> item, PixataSchedulerRecurrenceEditMode mode) {
    _prompt = null;
    TItem copy = item.IsOccurrence && mode == PixataSchedulerRecurrenceEditMode.Occurrence
      ? NewException(item.DataItem, item.Start, item.End)
      : PixataSchedulerItemFields.Clone(item.DataItem);
    PixataSchedulerEditEventArgs args = new() {
      Item = copy,
      IsNew = false,
      EditMode = mode,
      IsAllDay = item.IsAllDay,
      Start = item.Start,
      End = item.End
    };
    await OnEdit.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    OpenEditor(copy, false, mode, item);
  }

  private void OpenEditor(TItem item, bool isNew, PixataSchedulerRecurrenceEditMode mode, PixataSchedulerItem<TItem>? source) {
    PixataSchedulerEditModel? model = PopupEditFormSettings?.FormTemplate is null ? PixataSchedulerEditModel.From(item!, _fields, Resources) : null;
    EditContext context = new(model ?? (object)item!);
    EditState edit = new() {
      Item = item,
      IsNew = isNew,
      EditMode = mode,
      EditContext = context,
      ValidationMessages = new(context),
      Model = model,
      Source = source,
      OriginalStart = _fields.GetStart(item!)
    };
    context.OnFieldChanged += (_, _) => {
      if (edit.Submitted) {
        Revalidate(edit);
      }
    };
    _edit = edit;
    _focusDialogPending = true;
  }

  internal bool IsEditing => _edit is not null;

  /// <summary>
  /// What the built-in edit form edits
  /// </summary>
  internal PixataSchedulerEditModel? EditModel => _edit?.Model;

  internal EditContext? EditFormContext => _edit?.EditContext;

  /// <summary>
  /// True if the built-in edit form shows the recurrence editor: the appointments have a recurrence rule field, and the user
  /// is editing a series (or a new appointment), not an occurrence or an exception
  /// </summary>
  internal bool EditShowsRecurrence =>
    _edit is { Model: { } model } edit && model.HasRecurrence && !model.IsException && edit.EditMode == PixataSchedulerRecurrenceEditMode.Series;

  /// <summary>
  /// The time an appointment gets when the user unticks "All day event": the start of the working day
  /// </summary>
  internal TimeSpan DefaultStartTime =>
    (ActiveView as PixataSchedulerTimeViewBase<TItem> ?? AvailableViews.OfType<PixataSchedulerTimeViewBase<TItem>>().FirstOrDefault())?.WorkDayStart.TimeOfDay
      ?? TimeSpan.FromHours(8);

  /// <summary>
  /// The length an appointment gets when the user unticks "All day event": one slot
  /// </summary>
  internal TimeSpan DefaultLength => SlotLength();

  /// <summary>
  /// Called by the built-in edit form when a field changes
  /// </summary>
  internal void EditFieldChanged(string field) {
    if (_edit is { Model: { } model } edit) {
      edit.EditContext.NotifyFieldChanged(new FieldIdentifier(model, field));
    }
  }

  private void Revalidate(EditState edit) {
    edit.ValidationMessages.Clear();
    if (edit.Model is { } model) {
      model.Validate(edit.EditContext, edit.ValidationMessages, Text);
    } else {
      ValidateItem(edit);
    }
    edit.EditContext.NotifyValidationStateChanged();
  }

  /// <summary>
  /// Validates and saves the appointment in the edit form
  /// </summary>
  internal async Task SaveEdit() {
    if (_edit is not { } edit) {
      return;
    }
    edit.Submitted = true;
    edit.ValidationMessages.Clear();
    bool valid = edit.EditContext.Validate();
    if (edit.Model is { } model) {
      valid &= model.Validate(edit.EditContext, edit.ValidationMessages, Text);
    } else {
      valid &= ValidateItem(edit);
    }
    edit.EditContext.NotifyValidationStateChanged();
    if (!valid) {
      return;
    }
    TItem item = edit.Item;
    edit.Model?.ApplyTo(item!, _fields, Resources);
    _edit = null;
    if (edit.IsNew) {
      await OnCreate.InvokeAsync(new() { Item = item! });
    } else if (edit.EditMode == PixataSchedulerRecurrenceEditMode.Occurrence && edit.Source is { } occurrence) {
      await OnCreate.InvokeAsync(new() { Item = item! });
      await OnUpdate.InvokeAsync(new() { Item = SeriesWithException(occurrence)!, EditMode = PixataSchedulerRecurrenceEditMode.Occurrence });
    } else {
      // When the start of a series moves, its exceptions move with it, so they still mean the same occurrences
      if (edit.Source is { IsOccurrence: true }) {
        ShiftExceptions(item, _fields.GetStart(item!) - edit.OriginalStart);
      }
      await OnUpdate.InvokeAsync(new() { Item = item!, EditMode = PixataSchedulerRecurrenceEditMode.Series });
    }
    _stale = true;
    _focusContentPending = true;
  }

  private bool ValidateItem(EditState edit) {
    object item = edit.Item!;
    bool valid = true;
    if (string.IsNullOrWhiteSpace(_fields.GetTitle(item))) {
      edit.ValidationMessages.Add(new FieldIdentifier(item, _fields.Title), Text.TitleRequired);
      valid = false;
    }
    DateTime start = _fields.GetStart(item);
    DateTime end = PixataFieldReader.GetValue(item, _fields.End) is DateTime rawEnd ? rawEnd : _fields.GetEnd(item);
    if (end < start || (end == start && !_fields.GetIsAllDay(item))) {
      edit.ValidationMessages.Add(new FieldIdentifier(item, _fields.End), Text.EndBeforeStart);
      valid = false;
    }
    return valid;
  }

  internal async Task CancelEdit() {
    if (_edit is not { } edit) {
      return;
    }
    edit.Model?.ApplyTo(edit.Item!, _fields, Resources);
    _edit = null;
    _focusContentPending = true;
    await OnCancel.InvokeAsync(new() { Item = edit.Item! });
  }

  /// <summary>
  /// Deletes the appointment being edited
  /// </summary>
  internal async Task DeleteFromEditor() {
    if (_edit is not { Source: { } source } edit) {
      return;
    }
    if (edit.EditMode == PixataSchedulerRecurrenceEditMode.Occurrence) {
      await Confirm(async () => {
        _edit = null;
        await DeleteOccurrence(source);
      });
      return;
    }
    await Confirm(async () => {
      _edit = null;
      await OnDelete.InvokeAsync(new() { Item = source.DataItem!, EditMode = PixataSchedulerRecurrenceEditMode.Series });
      _stale = true;
      _focusContentPending = true;
    });
  }

  /// <summary>
  /// Deletes an appointment, asking first whether to delete the occurrence or the series for a recurring one, or to confirm
  /// when <see cref="ConfirmDelete"/> is set
  /// </summary>
  internal async Task DeleteItem(PixataSchedulerItem<TItem> item) {
    if (!CanDelete(item)) {
      return;
    }
    if (item.IsOccurrence) {
      _prompt = new() {
        Title = Text.DeleteRecurringTitle,
        Text = Text.DeleteRecurringText,
        Buttons = [
          (Text.DeleteOccurrence, () => DeleteOccurrence(item), true),
          (Text.DeleteSeries, () => DeleteSeries(item), false)
        ]
      };
      _focusDialogPending = true;
      return;
    }
    await Confirm(async () => {
      if (_selectedKey == item.Key) {
        _selectedKey = null;
      }
      await OnDelete.InvokeAsync(new() { Item = item.DataItem!, EditMode = PixataSchedulerRecurrenceEditMode.Series });
      _stale = true;
      _focusContentPending = true;
    });
  }

  private async Task Confirm(Func<Task> action) {
    if (!ConfirmDelete) {
      await action();
      return;
    }
    _prompt = new() {
      Title = Text.DeleteTitle,
      Text = Text.DeleteConfirmation,
      Buttons = [
        (Text.Delete, async () => {
          _prompt = null;
          await action();
        }, true),
        (Text.Cancel, () => {
          ClosePrompt();
          return Task.CompletedTask;
        }, false)
      ]
    };
    _focusDialogPending = true;
  }

  private async Task DeleteOccurrence(PixataSchedulerItem<TItem> item) {
    _prompt = null;
    _selectedKey = null;
    await OnUpdate.InvokeAsync(new() { Item = SeriesWithException(item)!, EditMode = PixataSchedulerRecurrenceEditMode.Occurrence });
    _stale = true;
    _focusContentPending = true;
  }

  private async Task DeleteSeries(PixataSchedulerItem<TItem> item) {
    _prompt = null;
    _selectedKey = null;
    await OnDelete.InvokeAsync(new() { Item = item.DataItem!, EditMode = PixataSchedulerRecurrenceEditMode.Series });
    _stale = true;
    _focusContentPending = true;
  }

  internal void ClosePrompt() {
    _prompt = null;
    if (_edit is null) {
      _focusContentPending = true;
    } else {
      _focusDialogPending = true;
    }
  }

  internal async Task CloseDialog() {
    if (_prompt is not null) {
      ClosePrompt();
    } else if (_edit is not null) {
      await CancelEdit();
    }
  }

  private TItem NewItem() {
    if (OnModelInit is not null) {
      return OnModelInit();
    }
    return Activator.CreateInstance<TItem>();
  }

  /// <summary>
  /// A new appointment to replace one occurrence of a series: a copy of the series, with the occurrence's times, no
  /// recurrence rule, and its recurrence id pointing at the series
  /// </summary>
  private TItem NewException(TItem series, DateTime start, DateTime end) {
    TItem exception;
    if (OnModelInit is not null || (!typeof(TItem).IsValueType && typeof(TItem).GetConstructor(Type.EmptyTypes) is not null)) {
      exception = NewItem();
      PixataSchedulerItemFields.CopyFields(series!, exception!, _fields.Id);
    } else {
      exception = PixataSchedulerItemFields.Clone(series);
      _fields.ClearId(exception!);
    }
    _fields.SetStart(exception!, start);
    _fields.SetEnd(exception!, end);
    _fields.SetRecurrenceRule(exception!, null);
    _fields.SetRecurrenceExceptions(exception!, null);
    _fields.SetRecurrenceId(exception!, _fields.GetId(series!));
    return exception;
  }

  /// <summary>
  /// A copy of an occurrence's series, with the occurrence added to its exceptions
  /// </summary>
  private TItem SeriesWithException(PixataSchedulerItem<TItem> occurrence) {
    TItem series = PixataSchedulerItemFields.Clone(occurrence.DataItem);
    List<DateTime> exceptions = _fields.GetRecurrenceExceptions(series!);
    exceptions.Add(occurrence.OccurrenceStart);
    _fields.SetRecurrenceExceptions(series!, exceptions);
    return series;
  }

  private void ShiftExceptions(TItem series, TimeSpan delta) {
    if (delta == TimeSpan.Zero) {
      return;
    }
    List<DateTime> exceptions = _fields.GetRecurrenceExceptions(series!);
    if (exceptions.Count > 0) {
      _fields.SetRecurrenceExceptions(series!, exceptions.Select(e => e + delta));
    }
  }

  #endregion

  #region Dragging and resizing

  /// <summary>
  /// A slot, as reported by the browser when an appointment is dropped
  /// </summary>
  public sealed class DropSlot {
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public bool AllDay { get; set; }
    public int Group { get; set; }
  }

  /// <summary>
  /// Called by the browser when the user drops an appointment they were moving or resizing
  /// </summary>
  /// <param name="key">The appointment</param>
  /// <param name="mode"><c>move</c>, <c>start</c> (resizing the start) or <c>end</c></param>
  /// <param name="grab">The slot where the pointer was when the drag started</param>
  /// <param name="drop">The slot where it was when the drag ended</param>
  [JSInvokable]
  public async Task OnDragEnd(string key, string mode, DropSlot grab, DropSlot drop) {
    if (_disposed || !AllowUpdate) {
      return;
    }
    Prepare();
    PixataSchedulerItem<TItem>? item = _items.FirstOrDefault(i => i.Key == key);
    if (item is null || item.ReadOnly) {
      return;
    }
    if (DragResult(item, mode, ParseSlot(grab), ParseSlot(drop)) is not { } result) {
      return;
    }
    _selectedKey = item.Key;
    if (item.IsOccurrence) {
      _prompt = new() {
        Title = Text.MoveRecurringTitle,
        Text = Text.MoveRecurringText,
        Buttons = [
          (Text.MoveOccurrence, () => ApplyDrag(item, result, PixataSchedulerRecurrenceEditMode.Occurrence), true),
          (Text.MoveSeries, () => ApplyDrag(item, result, PixataSchedulerRecurrenceEditMode.Series), false)
        ]
      };
      _focusDialogPending = true;
    } else {
      await ApplyDrag(item, result, PixataSchedulerRecurrenceEditMode.Series);
    }
    StateHasChanged();
  }

  private static PixataSchedulerSlot ParseSlot(DropSlot slot) {
    DateTime start = DateTime.Parse(slot.Start, CultureInfo.InvariantCulture);
    DateTime end = DateTime.Parse(slot.End, CultureInfo.InvariantCulture);
    return new(start, end, slot.AllDay, slot.Group);
  }

  internal readonly record struct DragOutcome(DateTime Start, DateTime End, bool IsAllDay, int? Group);

  /// <summary>
  /// Where an appointment ends up after being moved or resized from one slot to another, or null if nothing changes
  /// </summary>
  internal DragOutcome? DragResult(PixataSchedulerItem<TItem> item, string mode, PixataSchedulerSlot grab, PixataSchedulerSlot drop) {
    DateTime start = item.Start;
    DateTime end = item.End;
    bool isAllDay = item.IsAllDay;
    int? group = null;
    bool monthView = CurrentView == PixataSchedulerView.Month;
    switch (mode) {
      case "move":
        if (grab.IsAllDay && drop.IsAllDay) {
          int days = (drop.Start.Date - grab.Start.Date).Days;
          start = start.AddDays(days);
          end = end.AddDays(days);
        } else if (!grab.IsAllDay && !drop.IsAllDay && item.IsAllDay) {
          // An all-day appointment in the timeline moves by whole days
          int days = (drop.Start.Date - grab.Start.Date).Days;
          start = start.AddDays(days);
          end = end.AddDays(days);
        } else if (!grab.IsAllDay && !drop.IsAllDay) {
          TimeSpan delta = drop.Start - grab.Start;
          start += delta;
          end += delta;
        } else if (drop.IsAllDay) {
          // From the time slots to the all-day row
          (DateTime first, DateTime last) = PixataSchedulerDates.TimedDaySpan(start, end);
          int days = Math.Max(1, (last - first).Days);
          isAllDay = true;
          start = drop.Start.Date;
          end = start.AddDays(days - 1);
        } else {
          // From the all-day row to the time slots
          isAllDay = false;
          TimeSpan length = item.IsAllDay ? SlotLength() : end - start;
          start = drop.Start;
          end = start + length;
        }
        if (drop.Group != grab.Group && IsGrouped && drop.Group >= 0 && drop.Group < _groups.Count) {
          group = drop.Group;
        }
        break;
      case "end":
        // An all-day appointment always ends on a date, even when its edge is dropped on a time slot (in the timeline)
        if (drop.IsAllDay || monthView || isAllDay) {
          end = isAllDay ? drop.Start.Date : drop.Start.Date + end.TimeOfDay;
          if (isAllDay ? end < start.Date : end <= start) {
            return null;
          }
        } else {
          end = drop.End > start ? drop.End : start + (drop.End - drop.Start);
        }
        break;
      case "start":
        if (drop.IsAllDay || monthView || isAllDay) {
          start = isAllDay ? drop.Start.Date : drop.Start.Date + start.TimeOfDay;
          if (isAllDay ? start > end.Date : start >= end) {
            return null;
          }
        } else {
          start = drop.Start < end ? drop.Start : end - (drop.End - drop.Start);
        }
        break;
      default:
        return null;
    }
    if (start == item.Start && end == item.End && isAllDay == item.IsAllDay && group is null) {
      return null;
    }
    return new(start, end, isAllDay, group);
  }

  private TimeSpan SlotLength() =>
    ActiveView is PixataSchedulerTimeViewBase<TItem> timeView ? TimeSpan.FromMinutes(Math.Max(1, timeView.SlotDuration)) : TimeSpan.FromHours(1);

  private async Task ApplyDrag(PixataSchedulerItem<TItem> item, DragOutcome result, PixataSchedulerRecurrenceEditMode mode) {
    _prompt = null;
    if (item.IsOccurrence && mode == PixataSchedulerRecurrenceEditMode.Occurrence) {
      TItem exception = NewException(item.DataItem, result.Start, result.End);
      Apply(exception, result.Start, result.End, result.IsAllDay, result.Group);
      await OnCreate.InvokeAsync(new() { Item = exception! });
      await OnUpdate.InvokeAsync(new() { Item = SeriesWithException(item)!, EditMode = PixataSchedulerRecurrenceEditMode.Occurrence });
    } else if (item.IsOccurrence) {
      // Move the whole series by the amount the occurrence moved, and give it the occurrence's new length
      TItem series = PixataSchedulerItemFields.Clone(item.DataItem);
      DateTime seriesStart = _fields.GetStart(series!);
      TimeSpan shift = result.Start - item.Start;
      Apply(series, seriesStart + shift, seriesStart + shift + (result.End - result.Start), result.IsAllDay, result.Group);
      ShiftExceptions(series, shift);
      await OnUpdate.InvokeAsync(new() { Item = series!, EditMode = PixataSchedulerRecurrenceEditMode.Series });
    } else {
      TItem copy = PixataSchedulerItemFields.Clone(item.DataItem);
      Apply(copy, result.Start, result.End, result.IsAllDay, result.Group);
      await OnUpdate.InvokeAsync(new() { Item = copy!, EditMode = PixataSchedulerRecurrenceEditMode.Series });
    }
    _stale = true;
  }

  private void Apply(TItem item, DateTime start, DateTime end, bool isAllDay, int? group) {
    _fields.SetStart(item!, start);
    _fields.SetEnd(item!, end);
    _fields.SetIsAllDay(item!, isAllDay);
    if (group is { } index) {
      foreach (PixataSchedulerGroupLevel level in _groups[index].Levels) {
        _fields.SetField(item!, level.Field, level.Value);
      }
    }
  }

  #endregion
}

/// <summary>
/// A slot (a time slot, a day in the all-day row, or a day in the month view) in a group
/// </summary>
internal readonly record struct PixataSchedulerSlot(DateTime Start, DateTime End, bool IsAllDay, int Group);
