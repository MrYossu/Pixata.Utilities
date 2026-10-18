using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// How the user filters the grid
/// </summary>
public enum PixataGridFilterMode {
  None,

  /// <summary>
  /// A row under the headers, with an editor for each column, that filters as the user types
  /// </summary>
  FilterRow,

  /// <summary>
  /// A filter icon in each header, which opens a menu with two conditions, or a list of checkboxes
  /// </summary>
  FilterMenu
}

/// <summary>
/// What a column's filter menu shows
/// </summary>
public enum PixataGridFilterMenuType {
  /// <summary>
  /// Two conditions, joined by And or Or
  /// </summary>
  Menu,

  /// <summary>
  /// A checkbox for each distinct value in the column
  /// </summary>
  CheckBoxList
}

/// <summary>
/// What a column's custom filter template is given. Change <see cref="Filter"/> and call <see cref="FilterAsync"/> to apply it
/// </summary>
public class PixataGridFilterTemplateContext {
  internal PixataGridFilterTemplateContext(string field, GridCompositeFilterDescriptor filter, Func<Task> apply, Func<Task> clear) {
    Field = field;
    Filter = filter;
    _apply = apply;
    _clear = clear;
  }

  private readonly Func<Task> _apply;
  private readonly Func<Task> _clear;

  /// <summary>
  /// The column's field, for the descriptors you add to <see cref="Filter"/>
  /// </summary>
  public string Field { get; }

  /// <summary>
  /// The column's filter. Add, change or remove its descriptors, then call <see cref="FilterAsync"/>
  /// </summary>
  public GridCompositeFilterDescriptor Filter { get; }

  public Task FilterAsync() =>
    _apply();

  public Task ClearFilterAsync() =>
    _clear();
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// How the user filters the grid: not at all (the default), with a filter row, or with a menu in each header
  /// </summary>
  [Parameter]
  public PixataGridFilterMode FilterMode { get; set; } = PixataGridFilterMode.None;

  /// <summary>
  /// Content for the toolbar above the grid, such as a <see cref="PixataGridSearchBox{TItem}"/>
  /// </summary>
  [Parameter]
  public RenderFragment? GridToolBarTemplate { get; set; }

  internal enum FilterKind {
    None,
    Text,
    Number,
    Date,
    Boolean,
    Enum
  }

  // The filter for a column while it's being edited (in the filter row, or the menu before Filter is clicked)
  internal sealed class ColumnFilterState {
    public GridFilterOperator Operator1 { get; set; }
    public object? Value1 { get; set; }
    public GridFilterLogic Logic { get; set; } = GridFilterLogic.And;
    public GridFilterOperator Operator2 { get; set; }
    public object? Value2 { get; set; }
    public HashSet<object?> Checked { get; set; } = [];
  }

  private readonly Dictionary<string, GridCompositeFilterDescriptor> _filters = [];
  private readonly Dictionary<string, ColumnFilterState> _filterRowState = [];
  private readonly Dictionary<string, CancellationTokenSource> _filterDebounce = [];
  private GridCompositeFilterDescriptor? _search;
  private readonly string _gridId = $"pixata-grid-{Guid.NewGuid():N}";

  // The filter menu that's open, if any
  private PixataGridColumn<TItem>? _menuColumn;
  private ColumnFilterState _menuState = new();
  private string _menuSearch = "";
  private ElementReference _menuPopup;
  private bool _menuJustOpened;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataGrid<TItem>>? _self;

  private static readonly GridFilterOperator[] ValuelessOperators = [
    GridFilterOperator.IsNull, GridFilterOperator.IsNotNull, GridFilterOperator.IsEmpty, GridFilterOperator.IsNotEmpty,
    GridFilterOperator.IsNullOrEmpty, GridFilterOperator.IsNotNullOrEmpty
  ];

  internal static FilterKind KindOf(Type type) {
    Type underlying = Nullable.GetUnderlyingType(type) ?? type;
    if (underlying == typeof(string)) {
      return FilterKind.Text;
    }
    if (underlying == typeof(bool)) {
      return FilterKind.Boolean;
    }
    if (underlying.IsEnum) {
      return FilterKind.Enum;
    }
    if (underlying == typeof(DateTime) || underlying == typeof(DateOnly)) {
      return FilterKind.Date;
    }
    return Type.GetTypeCode(underlying) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
             or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal
      ? FilterKind.Number
      : FilterKind.None;
  }

  private bool CanFilter(PixataGridColumn<TItem> column) =>
    FilterMode != PixataGridFilterMode.None && column.Filterable && !string.IsNullOrWhiteSpace(column.Field)
    && (column.FilterCellTemplate is not null || column.FilterMenuTemplate is not null || column.FilterKind != FilterKind.None);

  private bool HasFilter(PixataGridColumn<TItem> column) =>
    column.Field is not null && _filters.ContainsKey(column.Field);

  // The operators offered for each kind of field, in the reference grid's order, named from the messages. Nullable fields also
  // get "Is null" and "Is not null"
  internal List<(GridFilterOperator, string)> OperatorsFor(PixataGridColumn<TItem> column) =>
    column.FilterOperators is { Count: > 0 } custom
      ? [.. custom.Select(o => (o.Operator, o.Text))]
      : OperatorsFor(column.FilterKind, column.IsNullable);

  internal List<(GridFilterOperator, string)> OperatorsFor(FilterKind kind, bool nullable) {
    List<GridFilterOperator> operators = kind switch {
      FilterKind.Text => [
        GridFilterOperator.IsEqualTo, GridFilterOperator.IsNotEqualTo, GridFilterOperator.StartsWith, GridFilterOperator.Contains,
        GridFilterOperator.DoesNotContain, GridFilterOperator.EndsWith, GridFilterOperator.IsNull, GridFilterOperator.IsNotNull,
        GridFilterOperator.IsEmpty, GridFilterOperator.IsNotEmpty, GridFilterOperator.IsNullOrEmpty, GridFilterOperator.IsNotNullOrEmpty
      ],
      FilterKind.Number => [
        GridFilterOperator.IsLessThan, GridFilterOperator.IsLessThanOrEqualTo, GridFilterOperator.IsEqualTo, GridFilterOperator.IsNotEqualTo,
        GridFilterOperator.IsGreaterThanOrEqualTo, GridFilterOperator.IsGreaterThan
      ],
      FilterKind.Date => [
        GridFilterOperator.IsEqualTo, GridFilterOperator.IsNotEqualTo, GridFilterOperator.IsGreaterThanOrEqualTo, GridFilterOperator.IsGreaterThan,
        GridFilterOperator.IsLessThanOrEqualTo, GridFilterOperator.IsLessThan
      ],
      FilterKind.Enum => [GridFilterOperator.IsEqualTo, GridFilterOperator.IsNotEqualTo],
      _ => []
    };
    if (nullable && kind is FilterKind.Number or FilterKind.Date or FilterKind.Enum) {
      operators.AddRange([GridFilterOperator.IsNull, GridFilterOperator.IsNotNull]);
    }
    return [.. operators.Select(op => (op, Text.Operator(op, kind == FilterKind.Date)))];
  }

  // The column's DefaultFilterOperator, otherwise Contains for text and Is equal to for everything else, unless the column has
  // its own FilterOperators that don't include that, in which case the first of them
  private static GridFilterOperator DefaultOperator(PixataGridColumn<TItem> column) {
    if (column.DefaultFilterOperator is GridFilterOperator op) {
      return op;
    }
    GridFilterOperator typeDefault = column.FilterKind == FilterKind.Text ? GridFilterOperator.Contains : GridFilterOperator.IsEqualTo;
    return column.FilterOperators is { Count: > 0 } custom && custom.All(o => o.Operator != typeDefault) ? custom[0].Operator : typeDefault;
  }

  private ColumnFilterState RowState(PixataGridColumn<TItem> column) {
    if (!_filterRowState.TryGetValue(column.Field!, out ColumnFilterState? state)) {
      state = new() { Operator1 = DefaultOperator(column), Operator2 = DefaultOperator(column) };
      _filterRowState[column.Field!] = state;
    }
    return state;
  }

  // ---------- The filter row ----------

  // Text and numbers are applied a moment after the user stops typing, as in Telerik's filter row
  private async Task OnFilterRowValueChanged(PixataGridColumn<TItem> column, object? value, bool debounce) {
    ColumnFilterState state = RowState(column);
    state.Value1 = value is string text && text.Length == 0 ? null : value;
    if (_filterDebounce.Remove(column.Field!, out CancellationTokenSource? previous)) {
      await previous.CancelAsync();
    }
    if (debounce) {
      CancellationTokenSource cancel = new();
      _filterDebounce[column.Field!] = cancel;
      try {
        await Task.Delay(150, cancel.Token);
      } catch (TaskCanceledException) {
        return;
      }
    }
    ApplyFilterRow(column);
  }

  private void OnFilterRowOperatorChanged(PixataGridColumn<TItem> column, GridFilterOperator op) {
    RowState(column).Operator1 = op;
    ApplyFilterRow(column);
  }

  private void ApplyFilterRow(PixataGridColumn<TItem> column) {
    ColumnFilterState state = RowState(column);
    GridFilterDescriptor? descriptor = Descriptor(column, state.Operator1, state.Value1);
    SetFilter(column, descriptor is null ? null : new(GridFilterLogic.And, [descriptor]));
  }

  private void ClearFilterRow(PixataGridColumn<TItem> column) {
    _filterRowState.Remove(column.Field!);
    SetFilter(column, null);
  }

  // A filter needs a value, unless its operator doesn't use one (such as "Is null")
  private static GridFilterDescriptor? Descriptor(PixataGridColumn<TItem> column, GridFilterOperator op, object? value) =>
    value is null && !ValuelessOperators.Contains(op) ? null : new(column.Field!, op, ValuelessOperators.Contains(op) ? null : value);

  // ---------- Applying filters ----------

  private void SetFilter(PixataGridColumn<TItem> column, GridCompositeFilterDescriptor? filter) {
    ResetVirtualScroll();
    if (filter is null || filter.Filters.Count == 0) {
      _filters.Remove(column.Field!);
    } else {
      _filters[column.Field!] = filter;
    }
    // Filtering goes back to the first page, as Telerik's grid does
    if (_page != 1) {
      _page = 1;
      _ = PageChanged.InvokeAsync(1);
    }
    ProcessData();
    StateHasChanged();
  }

  internal async Task SetSearch(string text, List<string>? fields) {
    _searchText = text;
    ResetVirtualScroll();
    List<string> searchFields = fields ?? [.. RenderedColumns().Where(c => c.Field is not null && c.FilterKind == FilterKind.Text).Select(c => c.Field!)];
    _search = string.IsNullOrEmpty(text) || searchFields.Count == 0
      ? null
      : new(GridFilterLogic.Or, [.. searchFields.Select(f => new GridFilterDescriptor(f, GridFilterOperator.Contains, text))]);
    if (_page != 1) {
      _page = 1;
      await PageChanged.InvokeAsync(1);
    }
    ProcessData();
    StateHasChanged();
  }

  private List<GridCompositeFilterDescriptor> CurrentFilters =>
    [.. _filters.Values];

  // ---------- The filter menu ----------

  private string MenuAnchorId(PixataGridColumn<TItem> column) =>
    $"{_gridId}-menu-{column.Key}";

  private async Task ToggleFilterMenu(PixataGridColumn<TItem> column) {
    if (_menuColumn == column) {
      await CloseFilterMenu();
      return;
    }
    await CloseFilterMenu();
    _menuColumn = column;
    _columnMenuOpen = false;
    _menuSearch = "";
    _menuState = new() { Operator1 = DefaultOperator(column), Operator2 = DefaultOperator(column) };
    // The menu starts with the column's current filter, if it has one
    if (_filters.TryGetValue(column.Field!, out GridCompositeFilterDescriptor? current)) {
      _menuState.Logic = current.Logic;
      if (column.FilterMenuType == PixataGridFilterMenuType.CheckBoxList) {
        _menuState.Checked = [.. current.Filters.Select(f => f.Value)];
      } else {
        if (current.Filters.Count > 0) {
          (_menuState.Operator1, _menuState.Value1) = (current.Filters[0].Operator, current.Filters[0].Value);
        }
        if (current.Filters.Count > 1) {
          (_menuState.Operator2, _menuState.Value2) = (current.Filters[1].Operator, current.Filters[1].Value);
        }
      }
    }
    _menuJustOpened = true;
  }

  [JSInvokable]
  public async Task ClosePopup() {
    await CloseFilterMenu();
    StateHasChanged();
  }

  private async Task CloseFilterMenu() {
    _menuJustOpened = false;
    if (_menuColumn is null) {
      return;
    }
    _menuColumn = null;
    _columnMenuOpen = false;
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("closePopup", _menuPopup);
      } catch (JSException) {
        // The menu has already gone
      } catch (JSDisconnectedException) {
        // So has the page
      }
    }
  }

  private async Task OpenFilterMenuPopup() {
    if (!_menuJustOpened || _menuColumn is null) {
      return;
    }
    _menuJustOpened = false;
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      _self ??= DotNetObjectReference.Create(this);
      await _module.InvokeVoidAsync("openPopup", MenuAnchorId(_menuColumn), _menuPopup, _self);
    } catch (JSException) {
      // The menu was closed before it could be positioned
    } catch (JSDisconnectedException) {
      // The page has gone
    }
  }

  private async Task ApplyFilterMenu() {
    PixataGridColumn<TItem>? column = _menuColumn;
    if (column is null) {
      return;
    }
    GridCompositeFilterDescriptor filter;
    if (column.FilterMenuType == PixataGridFilterMenuType.CheckBoxList) {
      filter = new(GridFilterLogic.Or, [.. _menuState.Checked.Select(v => new GridFilterDescriptor(column.Field!, v is null ? GridFilterOperator.IsNull : GridFilterOperator.IsEqualTo, v))]);
    } else {
      List<GridFilterDescriptor> descriptors = [];
      if (Descriptor(column, _menuState.Operator1, _menuState.Value1) is GridFilterDescriptor first) {
        descriptors.Add(first);
      }
      if (column.FilterKind != FilterKind.Boolean && Descriptor(column, _menuState.Operator2, _menuState.Value2) is GridFilterDescriptor second) {
        descriptors.Add(second);
      }
      filter = new(_menuState.Logic, descriptors);
    }
    await CloseFilterMenu();
    SetFilter(column, filter);
  }

  private async Task ClearFilterMenu() {
    PixataGridColumn<TItem>? column = _menuColumn;
    await CloseFilterMenu();
    if (column is not null) {
      SetFilter(column, null);
    }
  }

  // The distinct values in the column, for the checkbox list, from all the data rather than just the filtered rows. With
  // OnRead, the grid only has the current page, so that's all it can offer, as with Telerik's grid
  private List<(object? Value, string Text)> DistinctValues(PixataGridColumn<TItem> column) {
    Func<TItem, object?> getter = FieldAccessor.Getter<TItem>(column.Field!);
    return [.. (UsesOnRead ? _items : _boundData ?? [])
      .Select(getter)
      .Distinct()
      .OrderBy(v => v)
      .Select(v => (v, Convert.ToString(v, CultureInfo.CurrentCulture) ?? ""))];
  }

  private PixataGridFilterTemplateContext TemplateContext(PixataGridColumn<TItem> column) {
    GridCompositeFilterDescriptor filter = _filters.TryGetValue(column.Field!, out GridCompositeFilterDescriptor? current)
      ? current with { Filters = [.. current.Filters] }
      : new(GridFilterLogic.And, []);
    return new(column.Field!, filter, async () => {
      await CloseFilterMenu();
      SetFilter(column, filter);
    }, async () => {
      await CloseFilterMenu();
      _filterRowState.Remove(column.Field!);
      SetFilter(column, null);
    });
  }

  // ---------- Converting values for the editors ----------

  private static decimal? ToDecimal(object? value) =>
    value is null ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

  private static object? FromDecimal(PixataGridColumn<TItem> column, decimal? value) =>
    value is null ? null : GridFilterExpressions.ConvertValue(value.Value, column.FieldType!);

  private static DateTime? ToDate(object? value) => value switch {
    DateTime dateTime => dateTime,
    DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
    _ => null
  };

  private static object? FromDate(PixataGridColumn<TItem> column, DateTime? value) =>
    value is null ? null : GridFilterExpressions.ConvertValue(value.Value, column.FieldType!);
}
