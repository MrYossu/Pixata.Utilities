using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components.Grid;

public partial class PixataGrid<TItem> {
  /// <summary>
  /// Makes the grid a single tab stop, with the keyboard moving between cells: the arrow keys, Home and End (with Ctrl, to
  /// the first and last cells), Page Up and Page Down to change the page, Enter or F2 to edit, Space to select, and Alt+Down
  /// on a header to open its menu. Enter on a header sorts by it
  /// </summary>
  [Parameter]
  public bool Navigable { get; set; }

  private bool _navigationReady;

  private async Task InitNavigation() {
    if (!Navigable || _navigationReady || _disposed) {
      return;
    }
    _navigationReady = true;
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      _self ??= DotNetObjectReference.Create(this);
      await _module.InvokeVoidAsync("initNavigation", _gridId, _self);
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
      // The page has gone
    }
  }

  // The number of rows and columns, for screen readers, counting the header rows and all the data, not just what's shown
  private int AriaRowCount(int headerRows) =>
    headerRows + (FilterMode == PixataGridFilterMode.FilterRow ? 1 : 0) + _total;

  /// <summary>
  /// Called by the JavaScript for the keys that act on the data
  /// </summary>
  /// <param name="key">The key: PageUp, PageDown, Enter, F2, a space, or Menu for Alt+Down on a header</param>
  /// <param name="rowIndex">The row's position among the rows shown, or -1</param>
  /// <param name="columnKey">The column's key, if the key was pressed in a cell or header</param>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task NavigationKey(string key, int rowIndex, string? columnKey) {
    PixataGridColumn<TItem>? column = columnKey is null ? null : AllColumns().FirstOrDefault(c => c.Key == columnKey);
    TItem? item = rowIndex >= 0 && rowIndex < _items.Count ? _items[rowIndex] : default;
    switch (key) {
      case "PageDown" or "PageUp" when Pageable && !VirtualRows && _pageSize > 0: {
        int pageCount = (_total + _pageSize - 1) / _pageSize;
        int page = _page + (key == "PageDown" ? 1 : -1);
        if (page >= 1 && page <= pageCount) {
          await OnPageChanged(page);
        }
        break;
      }
      case "Enter" or "F2" when item is not null && EditMode == PixataGridEditMode.InCell && column is not null && CanEdit(column):
        await StartCellEdit(item, column);
        break;
      case "Enter" or "F2" when item is not null && EditMode is PixataGridEditMode.Inline or PixataGridEditMode.Popup && !_editing:
        await Command("Edit", item);
        break;
      case " " when item is not null && SelectionMode != PixataGridSelectionMode.None:
        if (SelectionType == PixataGridSelectionType.Cell && column is not null) {
          await CellSelectionClick(item, column, new MouseEventArgs { CtrlKey = true });
        } else {
          await RowSelectionClick(item, new MouseEventArgs { CtrlKey = SelectionMode == PixataGridSelectionMode.Multiple });
        }
        break;
      case "Menu" when column is not null:
        if (HasColumnMenu(column)) {
          await ToggleColumnMenu(column);
        } else if (FilterMode == PixataGridFilterMode.FilterMenu && CanFilter(column)) {
          await ToggleFilterMenu(column);
        }
        break;
    }
    StateHasChanged();
  }
}
