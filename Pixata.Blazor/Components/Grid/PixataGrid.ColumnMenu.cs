using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Whether the column menu offers filtering
/// </summary>
public enum PixataGridColumnMenuFilterMode {
  None,
  FilterMenu
}

/// <summary>
/// What the column menus offer, when the grid's <c>ShowColumnMenu</c> is set. Goes in the grid's <c>GridSettings</c>
/// </summary>
public class PixataGridColumnMenuSettings<TItem> : PixataGridSettingsBase<TItem> {
  /// <summary>
  /// Whether the menu has a list of the columns, to show and hide them. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowColumnChooser { get; set; } = true;

  /// <summary>
  /// Whether the menu offers sorting, for a sortable column. Defaults to true
  /// </summary>
  [Parameter]
  public bool Sortable { get; set; } = true;

  /// <summary>
  /// Whether the menu offers filtering, for a filterable column. Defaults to <c>FilterMenu</c>
  /// </summary>
  [Parameter]
  public PixataGridColumnMenuFilterMode FilterMode { get; set; } = PixataGridColumnMenuFilterMode.FilterMenu;

  /// <summary>
  /// Whether the menu offers freezing and unfreezing the column. Defaults to true
  /// </summary>
  [Parameter]
  public bool Lockable { get; set; } = true;

  /// <summary>
  /// Whether the menu offers grouping by the column, when the grid is <c>Groupable</c>
  /// </summary>
  [Parameter]
  public bool Groupable { get; set; }

  /// <summary>
  /// Whether the menu offers moving the column to the left or right
  /// </summary>
  [Parameter]
  public bool Reorderable { get; set; }

  internal override void Register(PixataGrid<TItem> grid, bool add) =>
    grid.ColumnMenuSettings = add ? this : null;
}

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Gives each column header a menu, for sorting, filtering, showing and hiding columns, freezing, grouping and moving. See
  /// <see cref="PixataGridColumnMenuSettings{TItem}"/>
  /// </summary>
  [Parameter]
  public bool ShowColumnMenu { get; set; }

  internal PixataGridColumnMenuSettings<TItem>? ColumnMenuSettings { get; set; }

  // Whether the popup open for _menuColumn is the column menu, rather than the filter menu
  private bool _columnMenuOpen;

  // The section of the column menu that's expanded, if any
  private ColumnMenuSection? _menuSection;

  // The column chooser's ticks, which only take effect when the user clicks Apply
  private Dictionary<PixataGridColumn<TItem>, bool> _chooser = [];

  private enum ColumnMenuSection {
    Columns,
    Filter,
    Position
  }

  private bool HasColumnMenu(PixataGridColumn<TItem> column) =>
    ShowColumnMenu && column.ShowColumnMenu && !column.IsGroupColumn
    && column is not PixataGridCommandColumn<TItem> and not PixataGridCheckboxColumn<TItem>;

  private bool MenuSorts(PixataGridColumn<TItem> column) =>
    (ColumnMenuSettings?.Sortable ?? true) && CanSort(column);

  private bool MenuFilters(PixataGridColumn<TItem> column) =>
    (ColumnMenuSettings?.FilterMode ?? PixataGridColumnMenuFilterMode.FilterMenu) == PixataGridColumnMenuFilterMode.FilterMenu && CanFilter(column);

  private bool MenuChoosesColumns =>
    ColumnMenuSettings?.ShowColumnChooser ?? true;

  private bool MenuGroups(PixataGridColumn<TItem> column) =>
    (ColumnMenuSettings?.Groupable ?? false) && CanGroup(column);

  private bool MenuLocks(PixataGridColumn<TItem> column) =>
    (ColumnMenuSettings?.Lockable ?? true) && column.Lockable;

  private bool MenuMoves(PixataGridColumn<TItem> column) =>
    (ColumnMenuSettings?.Reorderable ?? false) && column.Reorderable;

  private async Task ToggleColumnMenu(PixataGridColumn<TItem> column) {
    if (_menuColumn == column && _columnMenuOpen) {
      await CloseFilterMenu();
      return;
    }
    await ToggleFilterMenu(column);
    _columnMenuOpen = true;
    _menuSection = null;
    ResetChooser();
  }

  private void ToggleSection(ColumnMenuSection section) =>
    _menuSection = _menuSection == section ? null : section;

  // The columns the chooser lists: every column with cells, whether shown or not, in the order they're shown in
  private List<PixataGridColumn<TItem>> ChooserColumns() {
    static IEnumerable<PixataGridColumn<TItem>> All(List<PixataGridColumn<TItem>> siblings) =>
      Ordered(siblings).SelectMany(c => c.IsGroupColumn ? All(c.Children) : [c]);
    return [.. All(TopColumns()).Where(c => c.VisibleInColumnChooser)];
  }

  private void ResetChooser() =>
    _chooser = ChooserColumns().ToDictionary(c => c, c => c.IsVisible);

  // The last ticked column can't be unticked, so the grid always has a column
  private bool ChooserLocked(PixataGridColumn<TItem> column) =>
    _chooser.GetValueOrDefault(column) && _chooser.Count(c => c.Value) == 1;

  private async Task ApplyChooser() {
    foreach ((PixataGridColumn<TItem> column, bool visible) in _chooser) {
      column.RuntimeVisible = visible;
    }
    await CloseFilterMenu();
  }

  // Clicking the sort the column already has removes it
  private async Task MenuSort(PixataGridColumn<TItem> column, ListSortDirection direction) {
    GridSortDescriptor? current = SortFor(column);
    if (current?.SortDirection == direction) {
      _sorts.Remove(current);
    } else if (current is not null) {
      _sorts[_sorts.IndexOf(current)] = current with { SortDirection = direction };
    } else {
      if (SortMode == PixataGridSortMode.Single) {
        _sorts.Clear();
      }
      _sorts.Add(new(column.Field!, direction));
    }
    await CloseFilterMenu();
    ProcessData();
  }

  private async Task MenuGroup(PixataGridColumn<TItem> column) {
    await CloseFilterMenu();
    if (_groups.Any(g => g.Member == column.Field)) {
      Ungroup(column.Field!);
    } else {
      GroupBy(column.Field!);
    }
  }

  // A column the page froze with its Locked parameter can't be unfrozen
  private static bool CanUnlock(PixataGridColumn<TItem> column) =>
    column.IsLocked && !column.Locked && column.ParentColumn?.IsLocked != true;

  // A frozen column moves to the end of the frozen columns at the start, and an unfrozen one moves to just after them
  private async Task MenuLock(PixataGridColumn<TItem> column, bool locked) {
    column.RuntimeLocked = locked;
    List<PixataGridColumn<TItem>> others = Ordered(Siblings(column));
    others.Remove(column);
    await CloseFilterMenu();
    await MoveTo(column, others.TakeWhile(c => c.IsLocked).Count());
  }

  private int SiblingIndex(PixataGridColumn<TItem> column) =>
    Ordered(Siblings(column)).IndexOf(column);

  private async Task MenuMove(PixataGridColumn<TItem> column, int by) {
    await CloseFilterMenu();
    await MoveTo(column, SiblingIndex(column) + by);
  }
}
