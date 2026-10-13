using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Whether, and how many, items the user can select
/// </summary>
public enum PixataGridSelectionMode {
  None,
  Single,
  Multiple
}

/// <summary>
/// Whether the user selects rows or individual cells
/// </summary>
public enum PixataGridSelectionType {
  Row,
  Cell
}

/// <summary>
/// Which rows the checkbox column's "select all" box selects
/// </summary>
public enum PixataGridSelectAllMode {
  /// <summary>
  /// The rows on the current page
  /// </summary>
  Current,

  /// <summary>
  /// Every row that passes the filters, on every page. With <c>OnRead</c>, the grid only has the current page, so this works
  /// like <see cref="Current"/>
  /// </summary>
  All
}

/// <summary>
/// A selected cell, when the grid selects cells rather than rows
/// </summary>
public record PixataGridSelectedCell<TItem>(TItem DataItem, string? ColumnField);

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Whether the user can select one row (or cell), several, or none (the default)
  /// </summary>
  [Parameter]
  public PixataGridSelectionMode SelectionMode { get; set; } = PixataGridSelectionMode.None;

  /// <summary>
  /// The selected items. Can be two-way bound. Items are matched with <see cref="object.Equals(object)"/>, so if your items
  /// are recreated (such as when they're read from a database again), either override <c>Equals</c> or set
  /// <see cref="ItemKey"/>
  /// </summary>
  [Parameter]
  public IEnumerable<TItem>? SelectedItems { get; set; }

  [Parameter]
  public EventCallback<IEnumerable<TItem>> SelectedItemsChanged { get; set; }

  /// <summary>
  /// The selected cells, when the selection settings' <c>SelectionType</c> is <c>Cell</c>. Can be two-way bound
  /// </summary>
  [Parameter]
  public IEnumerable<PixataGridSelectedCell<TItem>>? SelectedCells { get; set; }

  [Parameter]
  public EventCallback<IEnumerable<PixataGridSelectedCell<TItem>>> SelectedCellsChanged { get; set; }

  /// <summary>
  /// Gets a key for an item, such as its id, which is used to match selected items instead of <c>Equals</c>
  /// </summary>
  [Parameter]
  public Func<TItem, object?>? ItemKey { get; set; }

  internal PixataGridSelectionSettings<TItem>? SelectionSettings { get; set; }

  private List<TItem> _selected = [];
  private List<PixataGridSelectedCell<TItem>> _selectedCells = [];
  private IEnumerable<TItem>? _lastSelectedItems;
  private IEnumerable<PixataGridSelectedCell<TItem>>? _lastSelectedCells;
  private TItem? _anchorItem;
  private PixataGridSelectedCell<TItem>? _anchorCell;
  private bool _dragging;

  private PixataGridSelectionType SelectionType =>
    SelectionSettings?.SelectionType ?? PixataGridSelectionType.Row;

  private bool DragToSelect =>
    SelectionMode == PixataGridSelectionMode.Multiple && (SelectionSettings?.DragToSelect ?? true);

  private bool SameItem(TItem? a, TItem? b) =>
    ItemKey is null ? EqualityComparer<TItem?>.Default.Equals(a, b) : a is not null && b is not null && Equals(ItemKey(a), ItemKey(b));

  private bool IsSelected(TItem item) =>
    _selected.Any(s => SameItem(s, item));

  private bool IsCellSelected(TItem item, string? field) =>
    _selectedCells.Any(c => c.ColumnField == field && SameItem(c.DataItem, item));

  // Picks up a selection set from outside
  private void SyncSelectionParameters() {
    if (!ReferenceEquals(SelectedItems, _lastSelectedItems)) {
      _lastSelectedItems = SelectedItems;
      _selected = [.. SelectedItems ?? []];
    }
    if (!ReferenceEquals(SelectedCells, _lastSelectedCells)) {
      _lastSelectedCells = SelectedCells;
      _selectedCells = [.. SelectedCells ?? []];
    }
  }

  private async Task SetSelection(List<TItem> selected) {
    _selected = selected;
    _lastSelectedItems = selected;
    await SelectedItemsChanged.InvokeAsync(selected);
  }

  private async Task SetCellSelection(List<PixataGridSelectedCell<TItem>> cells) {
    _selectedCells = cells;
    _lastSelectedCells = cells;
    await SelectedCellsChanged.InvokeAsync(cells);
  }

  // ---------- Rows ----------

  // A click selects just that row. Ctrl+click adds or removes it, and Shift+click selects the rows between it and the last
  // row clicked, as in Telerik's grid
  private async Task RowSelectionClick(TItem item, MouseEventArgs e) {
    if (SelectionMode == PixataGridSelectionMode.None || SelectionType != PixataGridSelectionType.Row || CheckboxColumnOnly) {
      return;
    }
    bool ctrl = e.CtrlKey || e.MetaKey;
    if (SelectionMode == PixataGridSelectionMode.Single) {
      await SetSelection(ctrl && IsSelected(item) ? [] : [item]);
    } else if (e.ShiftKey && _anchorItem is not null && _items.Any(i => SameItem(i, _anchorItem))) {
      await SetSelection(RowRange(_anchorItem, item));
      return;
    } else if (ctrl) {
      await SetSelection(IsSelected(item) ? [.. _selected.Where(s => !SameItem(s, item))] : [.. _selected, item]);
    } else {
      await SetSelection([item]);
    }
    _anchorItem = item;
  }

  private List<TItem> RowRange(TItem from, TItem to) {
    int start = _items.FindIndex(i => SameItem(i, from));
    int end = _items.FindIndex(i => SameItem(i, to));
    return [.. _items.Skip(Math.Min(start, end)).Take(Math.Abs(end - start) + 1)];
  }

  // A checkbox adds or removes its row without touching the others
  private async Task CheckboxClicked(TItem item) {
    if (SelectionMode == PixataGridSelectionMode.Single) {
      await SetSelection(IsSelected(item) ? [] : [item]);
    } else {
      await SetSelection(IsSelected(item) ? [.. _selected.Where(s => !SameItem(s, item))] : [.. _selected, item]);
    }
    _anchorItem = item;
  }

  private List<TItem> SelectAllCandidates(PixataGridCheckboxColumn<TItem> column) =>
    column.SelectAllMode == PixataGridSelectAllMode.All && !UsesOnRead
      ? (_boundData ?? []).ToGridResult(AllItemsRequest()).Data
      : _items;

  // Every item, in the order shown, without working out the groups and aggregates
  private GridRequest AllItemsRequest() {
    GridRequest request = CurrentRequest();
    return request with { Page = 1, PageSize = 0, Sorts = GridQueryExtensions.SortsWithGroups(request), Groups = [], Aggregates = [] };
  }

  private bool AllSelected(PixataGridCheckboxColumn<TItem> column) {
    List<TItem> candidates = SelectAllCandidates(column);
    return candidates.Count > 0 && candidates.All(IsSelected);
  }

  private bool SomeSelected(PixataGridCheckboxColumn<TItem> column) =>
    SelectAllCandidates(column).Any(IsSelected);

  private async Task SelectAllClicked(PixataGridCheckboxColumn<TItem> column) {
    List<TItem> candidates = SelectAllCandidates(column);
    if (AllSelected(column)) {
      await SetSelection([.. _selected.Where(s => !candidates.Any(c => SameItem(c, s)))]);
    } else {
      await SetSelection([.. _selected, .. candidates.Where(c => !IsSelected(c))]);
    }
  }

  // ---------- Cells ----------

  private async Task CellSelectionClick(TItem item, PixataGridColumn<TItem> column, MouseEventArgs e) {
    if (SelectionMode == PixataGridSelectionMode.None || SelectionType != PixataGridSelectionType.Cell || column is PixataGridCheckboxColumn<TItem>) {
      return;
    }
    PixataGridSelectedCell<TItem> cell = new(item, column.Field);
    bool ctrl = e.CtrlKey || e.MetaKey;
    if (SelectionMode == PixataGridSelectionMode.Single) {
      await SetCellSelection(ctrl && IsCellSelected(item, column.Field) ? [] : [cell]);
    } else if (e.ShiftKey && _anchorCell is not null) {
      await SetCellSelection(CellRange(_anchorCell, cell));
      return;
    } else if (ctrl) {
      await SetCellSelection(IsCellSelected(item, column.Field)
        ? [.. _selectedCells.Where(c => !(c.ColumnField == column.Field && SameItem(c.DataItem, item)))]
        : [.. _selectedCells, cell]);
    } else {
      await SetCellSelection([cell]);
    }
    _anchorCell = cell;
  }

  // The rectangle of cells between two cells
  private List<PixataGridSelectedCell<TItem>> CellRange(PixataGridSelectedCell<TItem> from, PixataGridSelectedCell<TItem> to) {
    List<PixataGridColumn<TItem>> columns = [.. RenderedColumns().Where(c => c is not PixataGridCheckboxColumn<TItem>)];
    int rowStart = _items.FindIndex(i => SameItem(i, from.DataItem));
    int rowEnd = _items.FindIndex(i => SameItem(i, to.DataItem));
    int colStart = columns.FindIndex(c => c.Field == from.ColumnField);
    int colEnd = columns.FindIndex(c => c.Field == to.ColumnField);
    if (rowStart < 0 || rowEnd < 0 || colStart < 0 || colEnd < 0) {
      return [to];
    }
    return [.. _items.Skip(Math.Min(rowStart, rowEnd)).Take(Math.Abs(rowEnd - rowStart) + 1)
      .SelectMany(i => columns.Skip(Math.Min(colStart, colEnd)).Take(Math.Abs(colEnd - colStart) + 1).Select(c => new PixataGridSelectedCell<TItem>(i, c.Field)))];
  }

  // ---------- Dragging ----------

  // Pressing on a cell and dragging over others selects the rows (or cells) in between, as Telerik's DragToSelect does
  private void DragStart(TItem item, PixataGridColumn<TItem> column, MouseEventArgs e) {
    if (!DragToSelect || e.Button != 0 || e.ShiftKey || e.CtrlKey || e.MetaKey || column is PixataGridCheckboxColumn<TItem>) {
      return;
    }
    _dragging = true;
    if (SelectionType == PixataGridSelectionType.Row) {
      _anchorItem = item;
    } else {
      _anchorCell = new(item, column.Field);
    }
  }

  private async Task DragOver(TItem item, PixataGridColumn<TItem> column, MouseEventArgs e) {
    if (!_dragging) {
      return;
    }
    // The button was released outside the grid
    if (e.Buttons == 0) {
      _dragging = false;
      return;
    }
    if (SelectionType == PixataGridSelectionType.Row && _anchorItem is not null) {
      await SetSelection(RowRange(_anchorItem, item));
    } else if (SelectionType == PixataGridSelectionType.Cell && _anchorCell is not null) {
      await SetCellSelection(CellRange(_anchorCell, new(item, column.Field)));
    }
  }

  private void DragEnd() =>
    _dragging = false;

  // Clicking a row selects it, unless a checkbox column says only its checkboxes should, as in Telerik
  private bool CheckboxColumnOnly =>
    RenderedColumns().OfType<PixataGridCheckboxColumn<TItem>>().Any(c => c.CheckBoxOnlySelection);
}
