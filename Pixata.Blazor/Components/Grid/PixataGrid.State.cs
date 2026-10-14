using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Everything about how a grid is set up that the user can change: the page, sorting, filtering, grouping, the columns'
/// order, widths and visibility, the selection and any editing. It serialises to JSON, so it can be saved anywhere
/// </summary>
public class PixataGridState<TItem> {
  public int? Page { get; set; }
  public int? PageSize { get; set; }

  /// <summary>
  /// The number of items before the page. Only used if <see cref="Page"/> isn't set
  /// </summary>
  public int? Skip { get; set; }

  public List<GridSortDescriptor> SortDescriptors { get; set; } = [];
  public List<GridCompositeFilterDescriptor> FilterDescriptors { get; set; } = [];

  /// <summary>
  /// The search box's filter, if anything has been searched for
  /// </summary>
  public GridCompositeFilterDescriptor? SearchFilter { get; set; }

  public List<GridGroupDescriptor> GroupDescriptors { get; set; } = [];

  /// <summary>
  /// The groups that are collapsed, each identified by the values of its own and its outer groups
  /// </summary>
  public List<string> CollapsedGroups { get; set; } = [];

  public List<PixataGridColumnState> ColumnStates { get; set; } = [];

  /// <summary>
  /// The width of the table, once the user has resized a column, as a CSS width
  /// </summary>
  public string? TableWidth { get; set; }

  public List<TItem> SelectedItems { get; set; } = [];

  /// <summary>
  /// The items whose details are shown
  /// </summary>
  public List<TItem> ExpandedItems { get; set; } = [];

  /// <summary>
  /// The copy of an existing item being edited
  /// </summary>
  public TItem? EditItem { get; set; }

  /// <summary>
  /// The item being edited, as it was before editing
  /// </summary>
  public TItem? OriginalEditItem { get; set; }

  /// <summary>
  /// A new item being added
  /// </summary>
  public TItem? InsertedItem { get; set; }

  /// <summary>
  /// The field being edited, when editing a cell at a time
  /// </summary>
  public string? EditField { get; set; }
}

/// <summary>
/// The state of one column
/// </summary>
public class PixataGridColumnState {
  /// <summary>
  /// The column's <c>Id</c>, which is used to find it when the state is restored, if it has one
  /// </summary>
  public string? Id { get; set; }

  /// <summary>
  /// The column's <c>Field</c>, which is used to find it if it has no <c>Id</c>
  /// </summary>
  public string? Field { get; set; }

  /// <summary>
  /// The column's title, which is used to find a column with neither an <c>Id</c> nor a <c>Field</c>
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// The column's position among the columns it is with
  /// </summary>
  public int Index { get; set; }

  public string? Width { get; set; }
  public bool Visible { get; set; } = true;
  public bool Locked { get; set; }
}

/// <summary>
/// What <c>OnStateInit</c> and <c>OnStateChanged</c> are given
/// </summary>
public class PixataGridStateEventArgs<TItem>(PixataGridState<TItem> gridState, string? propertyName = null) {
  /// <summary>
  /// The grid's state. In <c>OnStateInit</c>, change this (or replace it) to set the state the grid starts with
  /// </summary>
  public PixataGridState<TItem> GridState { get; set; } = gridState;

  /// <summary>
  /// In <c>OnStateChanged</c>, the name of the state property that changed, such as "SortDescriptors"
  /// </summary>
  public string? PropertyName { get; } = propertyName;
}

public partial class PixataGrid<TItem> {
  [Inject]
  private IServiceProvider Services { get; set; } = null!;

  /// <summary>
  /// Fires when the grid is first set up, so you can set the state it starts with
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridStateEventArgs<TItem>> OnStateInit { get; set; }

  /// <summary>
  /// Fires when the user changes the state, once for each property that changed
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridStateEventArgs<TItem>> OnStateChanged { get; set; }

  /// <summary>
  /// If set, the grid saves its state in the browser's local storage under this key whenever it changes, and restores it when
  /// it is next shown. Needs Blazored.LocalStorage, which <c>AddPixataBlazor()</c> registers
  /// </summary>
  [Parameter]
  public string? StorageKey { get; set; }

  private bool _stateInitDone;
  private Dictionary<string, string>? _stateSnapshot;
  private bool _resnapshot;
  private List<PixataGridColumnState> _pendingColumnStates = [];
  private string _searchText = "";

  // The search box shows the text of a search restored from the state
  internal string SearchText =>
    _searchText;

  internal event Action<string>? SearchTextRestored;

  /// <summary>
  /// The grid's current state
  /// </summary>
  public PixataGridState<TItem> GetState() =>
    new() {
      Page = _page,
      PageSize = _pageSize,
      Skip = VirtualRows ? _skip : (_page - 1) * _pageSize,
      SortDescriptors = [.. _sorts],
      FilterDescriptors = CurrentFilters,
      SearchFilter = _search,
      GroupDescriptors = [.. _groups],
      CollapsedGroups = [.. _collapsedGroups],
      ColumnStates = [.. AllColumns().Select(c => new PixataGridColumnState {
        Id = c.Id,
        Field = c.Field,
        Title = c.HeaderText,
        Index = Ordered(Siblings(c)).IndexOf(c),
        Width = c.CurrentWidth,
        Visible = c.IsVisible,
        Locked = c.IsLocked
      })],
      TableWidth = TableWidthPixels(RenderedColumns()) is double width ? FormattableString.Invariant($"{width}px") : null,
      SelectedItems = [.. _selected],
      ExpandedItems = [.. _expanded],
      EditItem = _editing && !_isNew ? _editItem : default,
      OriginalEditItem = _editing && !_isNew ? _originalEditItem : default,
      InsertedItem = _editing && _isNew ? _editItem : default,
      EditField = _editing ? _editField : null
    };

  /// <summary>
  /// Sets the grid's state, such as one saved earlier with <see cref="GetState"/>. Pass null to go back to how the grid
  /// started
  /// </summary>
  public async Task SetState(PixataGridState<TItem>? state) {
    if (state is null) {
      foreach (PixataGridColumn<TItem> column in AllColumns()) {
        column.RuntimeOrder = null;
        column.RuntimeWidth = null;
        column.RuntimeVisible = null;
        column.RuntimeLocked = null;
      }
    }
    await ApplyState(state ?? new() { PageSize = PageSize });
    if (ProcessData()) {
      await PageChanged.InvokeAsync(_page);
    }
    // The state has been set on purpose, so it isn't reported as a change, but it is saved
    _resnapshot = true;
    await SaveState(GetState());
    StateHasChanged();
  }

  // Called before the data is first read. A saved state comes first, then OnStateInit can change it
  private async Task InitState() {
    _stateInitDone = true;
    PixataGridState<TItem>? saved = await LoadState();
    if (saved is null && !OnStateInit.HasDelegate) {
      return;
    }
    PixataGridStateEventArgs<TItem> args = new(saved ?? GetState());
    try {
      await OnStateInit.InvokeAsync(args);
    } catch (InvalidOperationException) {
      // A handler that uses JavaScript can't while the page is being prerendered. It gets another go when the page is
      // interactive
    }
    await ApplyState(args.GridState);
  }

  private async Task ApplyState(PixataGridState<TItem> state) {
    int oldPageSize = _pageSize;
    if (state.PageSize is > 0) {
      _pageSize = state.PageSize.Value;
    }
    _page = Math.Max(state.Page ?? (state.Skip is int skip && _pageSize > 0 ? skip / _pageSize + 1 : 1), 1);
    if (VirtualRows) {
      ScrollToRow(state.Skip ?? 0);
    }
    _sorts.Clear();
    _sorts.AddRange(state.SortDescriptors);
    _filters.Clear();
    _filterRowState.Clear();
    foreach (GridCompositeFilterDescriptor filter in state.FilterDescriptors.Select(Normalise)) {
      if (filter.Filters.FirstOrDefault()?.Member is string field) {
        _filters[field] = filter;
        RestoreRowState(field, filter);
      }
    }
    _search = state.SearchFilter is null ? null : Normalise(state.SearchFilter);
    _searchText = _search?.Filters.FirstOrDefault()?.Value as string ?? "";
    SearchTextRestored?.Invoke(_searchText);
    _groups.Clear();
    _groups.AddRange(state.GroupDescriptors);
    _collapsedGroups.Clear();
    _collapsedGroups.UnionWith(state.CollapsedGroups);
    _fixedWidths = state.TableWidth is not null;
    _pendingColumnStates = [.. state.ColumnStates];
    ApplyPendingColumnStates();
    if (!_selected.SequenceEqual(state.SelectedItems)) {
      await SetSelection([.. state.SelectedItems]);
    }
    _expanded = [.. state.ExpandedItems];
    if (state.InsertedItem is not null) {
      BeginEditing(state.InsertedItem, default, true);
      _editField = state.EditField;
    } else if (state.EditItem is not null) {
      TItem? original = state.OriginalEditItem is null ? default : (_boundData ?? _items).FirstOrDefault(i => SameItem(i, state.OriginalEditItem));
      BeginEditing(state.EditItem, original ?? state.OriginalEditItem, false);
      _editField = state.EditField;
    } else if (_editing) {
      EndEditing();
    }
    if (_pageSize != oldPageSize) {
      await PageSizeChanged.InvokeAsync(_pageSize);
    }
  }

  // Filter values that have been through JSON arrive as JsonElements, so they are turned back into the field's type, for
  // the filter editors
  private static GridCompositeFilterDescriptor Normalise(GridCompositeFilterDescriptor filter) =>
    filter with {
      Filters = [.. filter.Filters.Select(f => f with { Value = f.Value is JsonElement { ValueKind: not JsonValueKind.Array } ? ConvertFilterValue(f) : f.Value })]
    };

  private static object? ConvertFilterValue(GridFilterDescriptor filter) {
    try {
      return GridFilterExpressions.ConvertValue(filter.Value, FieldAccessor.FieldType(typeof(TItem), filter.Member));
    } catch (ArgumentException) {
      // The field no longer exists
      return null;
    }
  }

  private void RestoreRowState(string field, GridCompositeFilterDescriptor filter) {
    ColumnFilterState state = new() { Logic = filter.Logic, Checked = [.. filter.Filters.Select(f => f.Value)] };
    if (filter.Filters.Count > 0) {
      (state.Operator1, state.Value1) = (filter.Filters[0].Operator, filter.Filters[0].Value);
      state.Operator2 = filter.Filters[0].Operator;
    }
    if (filter.Filters.Count > 1) {
      (state.Operator2, state.Value2) = (filter.Filters[1].Operator, filter.Filters[1].Value);
    }
    _filterRowState[field] = state;
  }

  // The columns register as the grid renders, so their states are applied as they appear
  private void ApplyPendingColumnStates() {
    if (_pendingColumnStates.Count == 0) {
      return;
    }
    List<PixataGridColumn<TItem>> columns = [.. AllColumns()];
    foreach (PixataGridColumnState state in _pendingColumnStates.ToList()) {
      PixataGridColumn<TItem>? column = columns.FirstOrDefault(c => state.Id is not null
        ? c.Id == state.Id
        : c.Id is null && (state.Field is not null ? c.Field == state.Field : c.Field is null && c.HeaderText == state.Title));
      if (column is null) {
        continue;
      }
      columns.Remove(column);
      _pendingColumnStates.Remove(state);
      column.RuntimeOrder = state.Index;
      column.RuntimeWidth = state.Width;
      column.RuntimeVisible = state.Visible;
      column.RuntimeLocked = state.Locked;
    }
  }

  // ---------- Reporting changes ----------

  // After each render, the state is compared with what it was, and OnStateChanged raised for each part that has changed.
  // That catches every change, however it was made
  private async Task CheckStateChanged() {
    if (!OnStateChanged.HasDelegate && string.IsNullOrWhiteSpace(StorageKey)) {
      _stateSnapshot = null;
      return;
    }
    PixataGridState<TItem> state = GetState();
    Dictionary<string, string> snapshot = Snapshot(state);
    if (_stateSnapshot is null || _resnapshot) {
      _stateSnapshot = snapshot;
      _resnapshot = false;
      return;
    }
    List<string> changed = [.. snapshot.Where(s => _stateSnapshot[s.Key] != s.Value).Select(s => s.Key)];
    _stateSnapshot = snapshot;
    if (changed.Count == 0) {
      return;
    }
    await SaveState(state);
    foreach (string property in changed) {
      await OnStateChanged.InvokeAsync(new(state, property));
    }
  }

  private static Dictionary<string, string> Snapshot(PixataGridState<TItem> state) =>
    new() {
      [nameof(state.Page)] = $"{state.Page}",
      [nameof(state.PageSize)] = $"{state.PageSize}",
      [nameof(state.SortDescriptors)] = Json(state.SortDescriptors),
      [nameof(state.FilterDescriptors)] = Json(state.FilterDescriptors),
      [nameof(state.SearchFilter)] = Json(state.SearchFilter),
      [nameof(state.GroupDescriptors)] = Json(state.GroupDescriptors),
      [nameof(state.CollapsedGroups)] = Json(state.CollapsedGroups),
      [nameof(state.ColumnStates)] = Json(state.ColumnStates),
      [nameof(state.TableWidth)] = $"{state.TableWidth}",
      // Items are compared by reference, so editing an item's fields isn't reported, but starting and stopping editing is
      [nameof(state.SelectedItems)] = string.Join(",", state.SelectedItems.Select(Reference)),
      [nameof(state.ExpandedItems)] = string.Join(",", state.ExpandedItems.Select(Reference)),
      [nameof(state.EditItem)] = Reference(state.EditItem),
      [nameof(state.OriginalEditItem)] = Reference(state.OriginalEditItem),
      [nameof(state.InsertedItem)] = Reference(state.InsertedItem),
      [nameof(state.EditField)] = $"{state.EditField}"
    };

  private static string Json<T>(T value) =>
    JsonSerializer.Serialize(value);

  private static string Reference(TItem? item) =>
    item is null ? "" : $"{RuntimeHelpers.GetHashCode(item)}";

  // ---------- Saving in local storage ----------

  private ILocalStorageService? Storage =>
    string.IsNullOrWhiteSpace(StorageKey)
      ? null
      : Services.GetService<ILocalStorageService>()
        ?? throw new InvalidOperationException("A PixataGrid with a StorageKey needs Blazored.LocalStorage. Call AddPixataBlazor() (or AddBlazoredLocalStorage()) in Program.cs");

  private async Task<PixataGridState<TItem>?> LoadState() {
    if (Storage is not ILocalStorageService storage) {
      return null;
    }
    try {
      return await storage.GetItemAsync<PixataGridState<TItem>>(StorageKey!);
    } catch (InvalidOperationException) {
      // The page is being prerendered, so local storage can't be read. It will be when the page becomes interactive
      return null;
    } catch (JsonException) {
      // Whatever was saved can't be read, perhaps because TItem has changed
      return null;
    } catch (JSException) {
      return null;
    }
  }

  private async Task SaveState(PixataGridState<TItem> state) {
    if (Storage is not ILocalStorageService storage) {
      return;
    }
    try {
      await storage.SetItemAsync(StorageKey!, state);
    } catch (InvalidOperationException) {
      // Prerendering
    } catch (JSDisconnectedException) {
      // The page has gone
    }
  }
}
