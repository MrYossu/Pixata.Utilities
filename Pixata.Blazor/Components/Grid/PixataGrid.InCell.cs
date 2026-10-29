using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components.Grid;

// Editing one cell at a time, as Telerik's InCell mode does. Clicking a cell edits it. Tab and Shift+Tab save it and move to
// the next or previous editable cell (carrying on into the next row), Enter saves it and moves down a row, and Escape cancels.
// Clicking anywhere else saves it. A new row is only created (OnCreate) when the user leaves it
public partial class PixataGrid<TItem> {
  private string? _editField;
  private bool _focusEditCell;
  private bool _watchingOutside;

  private string EditCellId =>
    $"{_gridId}-edit-cell";

  private bool IsCellEditing(TItem item, PixataGridColumn<TItem> column) =>
    EditMode == PixataGridEditMode.InCell && _editing && _editField is not null && column.Field == _editField
    && SameItem(item, _isNew ? _editItem : _originalEditItem);

  private bool IsInCellRow(TItem item) =>
    EditMode == PixataGridEditMode.InCell && _editing && !_isNew && SameItem(item, _originalEditItem);

  private List<PixataGridColumn<TItem>> EditableColumns =>
    [.. RenderedColumns().Where(CanEdit)];

  private async Task StartCellEdit(TItem item, PixataGridColumn<TItem> column) {
    if (!CanEdit(column)) {
      return;
    }
    // Already editing this cell
    if (_editing && _editField == column.Field && SameItem(item, _isNew ? _editItem : _originalEditItem)) {
      return;
    }
    // Moving to another cell in the row being edited just changes the cell
    if (_editing && SameItem(item, _isNew ? _editItem : _originalEditItem)) {
      if (_isNew) {
        _editField = column.Field;
        _focusEditCell = true;
        return;
      }
      // Saving can replace the item in the data (OnUpdate usually puts the edited copy in its place), so the row is found
      // again by its position afterwards
      int row = _items.FindIndex(i => SameItem(i, _originalEditItem));
      if (!await CommitCell()) {
        return;
      }
      if (row >= 0 && row < _items.Count) {
        item = _items[row];
      }
    } else if (_editing && !await LeaveRow()) {
      return;
    }
    PixataGridCommandEventArgs<TItem> args = new(item) { Field = column.Field };
    await OnEdit.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    BeginEditing(Copy(item), item, false);
    _editField = column.Field;
    _focusEditCell = true;
  }

  // Saves the cell being edited. Returns false if it's invalid, or OnUpdate cancelled it, in which case it stays in edit mode
  private async Task<bool> CommitCell() {
    if (!_editing || _editField is null || _editItem is null) {
      return true;
    }
    PixataGridColumn<TItem>? column = RenderedColumns().FirstOrDefault(c => c.Field == _editField);
    if (column is not null && ValidationMessage(column) is not null) {
      return false;
    }
    if (_isNew) {
      return true;
    }
    PixataGridCommandEventArgs<TItem> args = new(_editItem) { Field = _editField, Value = column is null ? null : EditValue(column) };
    await OnUpdate.InvokeAsync(args);
    if (args.IsCancelled) {
      return false;
    }
    await StopWatchingOutside();
    EndEditing();
    _editField = null;
    RefreshAfterEdit();
    return true;
  }

  // Leaving a row: an edited cell is saved, and a new row is validated and created
  private async Task<bool> LeaveRow() {
    if (!_editing) {
      return true;
    }
    if (!_isNew) {
      return await CommitCell();
    }
    if ((ValidationSettings?.Enabled ?? true) && _editContext is not null && !_editContext.Validate()) {
      return false;
    }
    PixataGridCommandEventArgs<TItem> args = new(_editItem!, true);
    await OnCreate.InvokeAsync(args);
    if (args.IsCancelled) {
      return false;
    }
    await StopWatchingOutside();
    EndEditing();
    _editField = null;
    RefreshAfterEdit();
    return true;
  }

  private async Task CancelCellEdit() {
    PixataGridCommandEventArgs<TItem> args = new(_editItem!, _isNew) { Field = _editField };
    await OnCancel.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    await StopWatchingOutside();
    EndEditing();
    _editField = null;
  }

  private async Task EditCellKeyDown(KeyboardEventArgs e) {
    if (!_editing || _editField is null) {
      return;
    }
    TItem current = _isNew ? _editItem! : _originalEditItem!;
    List<PixataGridColumn<TItem>> editable = EditableColumns;
    int columnIndex = editable.FindIndex(c => c.Field == _editField);
    switch (e.Key) {
      case "Escape":
        await CancelCellEdit();
        break;
      case "Enter": {
        // Saves, and moves to the same column in the next row (or, for a new row, creates it)
        if (_isNew) {
          await LeaveRow();
          break;
        }
        int row = _items.FindIndex(i => SameItem(i, current));
        PixataGridColumn<TItem> column = editable[columnIndex];
        if (await CommitCell() && row >= 0 && row + 1 < _items.Count) {
          await StartCellEdit(_items[row + 1], column);
        }
        break;
      }
      case "Tab": {
        int next = columnIndex + (e.ShiftKey ? -1 : 1);
        if (next >= 0 && next < editable.Count) {
          await StartCellEdit(current, editable[next]);
        } else if (!_isNew) {
          // Off the end of the row, so on to the first (or last) editable cell of the next (or previous) row
          int row = _items.FindIndex(i => SameItem(i, current)) + (e.ShiftKey ? -1 : 1);
          if (await CommitCell() && row >= 0 && row < _items.Count) {
            await StartCellEdit(_items[row], e.ShiftKey ? editable[^1] : editable[0]);
          }
        }
        break;
      }
    }
  }

  [JSInvokable]
  public async Task CellEditOutside() {
    await LeaveRow();
    StateHasChanged();
  }

  // After the edited cell has rendered, focus its editor, and watch for clicks outside it
  private async Task AfterInCellRender() {
    if (!_focusEditCell || !_editing) {
      return;
    }
    _focusEditCell = false;
    try {
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", PixataBlazorScript.Path);
      _self ??= DotNetObjectReference.Create(this);
      await _module.InvokeVoidAsync("focusIn", EditCellId);
      await _module.InvokeVoidAsync("watchOutside", EditCellId, _self, nameof(CellEditOutside));
      _watchingOutside = true;
    } catch (JSException) {
      // The cell has gone
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
      // So has the page
    }
  }

  private async Task StopWatchingOutside() {
    if (_watchingOutside && _module is not null) {
      _watchingOutside = false;
      try {
        await _module.InvokeVoidAsync("unwatchOutside", EditCellId);
      } catch (JSException) {
        // Nothing to stop
      } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
        // The page has gone
      }
    }
  }

  // The new row at the top, in in-cell mode. Clicking one of its cells moves the editor there
  private bool IsNewInCellRow =>
    EditMode == PixataGridEditMode.InCell && _editing && _isNew && _editItem is not null;

  private EditContext? InCellEditContext =>
    EditMode == PixataGridEditMode.InCell ? _editContext : null;
}
