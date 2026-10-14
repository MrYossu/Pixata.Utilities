using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// How the user edits rows
/// </summary>
public enum PixataGridEditMode {
  None,

  /// <summary>
  /// The row being edited turns into editors, with Save and Cancel buttons in the command column
  /// </summary>
  Inline,

  /// <summary>
  /// The row is edited in a form in a popup window
  /// </summary>
  Popup,

  /// <summary>
  /// Clicking a cell edits just that cell
  /// </summary>
  InCell
}

/// <summary>
/// The editor used for a column, when the default for its type isn't wanted
/// </summary>
public enum PixataGridEditorType {
  TextBox,
  TextArea,
  NumericTextBox,
  DatePicker,
  CheckBox,
  DropDownList
}

/// <summary>
/// What the grid's editing events (and a command button's <c>OnClick</c>) are given
/// </summary>
public class PixataGridCommandEventArgs<TItem>(TItem item, bool isNew = false) {
  /// <summary>
  /// The item. In <c>OnCreate</c> and <c>OnUpdate</c>, this is the edited copy, with the user's changes
  /// </summary>
  public TItem Item { get; } = item;

  /// <summary>
  /// True if the item is being added, rather than edited
  /// </summary>
  public bool IsNew { get; } = isNew;

  /// <summary>
  /// Set to true to stop what the user is doing. In <c>OnCreate</c> or <c>OnUpdate</c>, this keeps the row in edit mode,
  /// for example when saving failed
  /// </summary>
  public bool IsCancelled { get; set; }

  /// <summary>
  /// The field that was edited, when editing a single cell
  /// </summary>
  public string? Field { get; init; }

  /// <summary>
  /// The new value of the field, when editing a single cell
  /// </summary>
  public object? Value { get; init; }
}

/// <summary>
/// What a command column's buttons are given, so they know which row they're on
/// </summary>
public record PixataGridRowContext<TItem>(TItem Item, bool IsEditing, bool IsNew);

public partial class PixataGrid<TItem> {
  /// <summary>
  /// How the user edits rows: not at all (the default), inline, in a popup, or a cell at a time
  /// </summary>
  [Parameter]
  public PixataGridEditMode EditMode { get; set; } = PixataGridEditMode.None;

  /// <summary>
  /// Fires when the user clicks an Add button, before the new row appears. Set <c>IsCancelled</c> to stop it
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridCommandEventArgs<TItem>> OnAdd { get; set; }

  /// <summary>
  /// Fires when the user saves a new item. Add it to your data here
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridCommandEventArgs<TItem>> OnCreate { get; set; }

  /// <summary>
  /// Fires when the user starts editing a row. Set <c>IsCancelled</c> to stop it
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridCommandEventArgs<TItem>> OnEdit { get; set; }

  /// <summary>
  /// Fires when the user saves an edited item. <c>Item</c> is the edited copy; update your data with it here
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridCommandEventArgs<TItem>> OnUpdate { get; set; }

  /// <summary>
  /// Fires when the user deletes a row (after confirming, if <see cref="ConfirmDelete"/> is set). Remove it from your data here
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridCommandEventArgs<TItem>> OnDelete { get; set; }

  /// <summary>
  /// Fires when the user cancels an edit. Set <c>IsCancelled</c> to keep the row in edit mode
  /// </summary>
  [Parameter]
  public EventCallback<PixataGridCommandEventArgs<TItem>> OnCancel { get; set; }

  /// <summary>
  /// Creates a new item when the user clicks Add. Only needed if <typeparamref name="TItem"/> has no parameterless
  /// constructor, or a new item needs some values set
  /// </summary>
  [Parameter]
  public Func<TItem>? OnModelInit { get; set; }

  /// <summary>
  /// Asks the user to confirm before deleting a row
  /// </summary>
  [Parameter]
  public bool ConfirmDelete { get; set; }

  internal PixataGridPopupEditSettings<TItem>? PopupEditSettings { get; set; }
  internal PixataGridPopupEditFormSettings<TItem>? PopupEditFormSettings { get; set; }
  internal PixataGridValidationSettings<TItem>? ValidationSettings { get; set; }

  private TItem? _editItem;
  private TItem? _originalEditItem;
  private bool _isNew;
  private bool _editing;
  private EditContext? _editContext;
  private TItem? _confirmDeleteItem;
  private bool _confirmingDelete;

  /// <summary>
  /// The copy of the item being edited, or the default if nothing is being edited
  /// </summary>
  public TItem? EditItem =>
    _editing ? _editItem : default;

  private bool IsEditing(TItem item) =>
    _editing && !_isNew && EditMode == PixataGridEditMode.Inline && SameItem(item, _originalEditItem);

  private PixataGridRowContext<TItem> RowContext(TItem item) =>
    new(item, _editing && SameItem(item, _isNew ? _editItem : _originalEditItem), _isNew && _editing && SameItem(item, _editItem));

  /// <summary>
  /// Runs a command, as a command button does. The built-in commands are Add, Edit, Save, Cancel and Delete
  /// </summary>
  internal async Task Command(string command, TItem? item) {
    switch (command) {
      case "Add":
        await StartAdd();
        break;
      case "Edit" when item is not null:
        await StartEdit(item);
        break;
      case "Save" when EditMode == PixataGridEditMode.InCell:
        await LeaveRow();
        break;
      case "Save":
        await Save();
        break;
      case "Cancel" when EditMode == PixataGridEditMode.InCell:
        await CancelCellEdit();
        break;
      case "Cancel":
        await CancelEdit();
        break;
      case "CsvExport" or "ExcelExport":
        await RunExportCommand(command);
        break;
      case "Delete" when item is not null:
        if (ConfirmDelete) {
          _confirmDeleteItem = item;
          _confirmingDelete = true;
        } else {
          await Delete(item);
        }
        break;
    }
    StateHasChanged();
  }

  private async Task StartAdd() {
    TItem item = OnModelInit is null ? NewItem() : OnModelInit();
    PixataGridCommandEventArgs<TItem> args = new(item, true);
    await OnAdd.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    BeginEditing(item, default, true);
    if (EditMode == PixataGridEditMode.InCell) {
      // In in-cell mode, the new row opens with its first editable cell being edited
      _editField = EditableColumns.FirstOrDefault()?.Field;
      _focusEditCell = true;
    }
  }

  private static TItem NewItem() {
    try {
      return Activator.CreateInstance<TItem>();
    } catch (MissingMethodException) {
      throw new InvalidOperationException($"{typeof(TItem).Name} has no parameterless constructor, so give the grid an OnModelInit that creates a new one");
    }
  }

  private async Task StartEdit(TItem item) {
    PixataGridCommandEventArgs<TItem> args = new(item);
    await OnEdit.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    // The user edits a copy, so cancelling leaves the original as it was
    BeginEditing(Copy(item), item, false);
  }

  // A shallow copy, as Telerik's grid makes. Nested objects are shared with the original
  private static TItem Copy(TItem item) =>
    item is null ? item! : (TItem)CloneMethod.Invoke(item, null)!;

  private static readonly System.Reflection.MethodInfo CloneMethod =
    typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

  private void BeginEditing(TItem editItem, TItem? original, bool isNew) {
    _editItem = editItem;
    _originalEditItem = original;
    _isNew = isNew;
    _editing = true;
    _editContext = editItem is null ? null : new EditContext(editItem);
  }

  private void EndEditing() {
    _editing = false;
    _editItem = default;
    _originalEditItem = default;
    _isNew = false;
    _editContext = null;
    _editField = null;
    _ = StopWatchingOutside();
  }

  private async Task Save() {
    if (!_editing || _editItem is null) {
      return;
    }
    if ((ValidationSettings?.Enabled ?? true) && _editContext is not null && !_editContext.Validate()) {
      return;
    }
    PixataGridCommandEventArgs<TItem> args = new(_editItem, _isNew);
    await (_isNew ? OnCreate : OnUpdate).InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    EndEditing();
    RefreshAfterEdit();
  }

  private async Task CancelEdit() {
    if (!_editing) {
      return;
    }
    PixataGridCommandEventArgs<TItem> args = new(_editItem!, _isNew);
    await OnCancel.InvokeAsync(args);
    if (!args.IsCancelled) {
      EndEditing();
    }
  }

  private async Task Delete(TItem item) {
    PixataGridCommandEventArgs<TItem> args = new(item);
    await OnDelete.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    if (_editing && SameItem(item, _originalEditItem)) {
      EndEditing();
    }
    _selected.RemoveAll(s => SameItem(s, item));
    RefreshAfterEdit();
  }

  private async Task ConfirmDeleteAnswered(bool confirmed) {
    _confirmingDelete = false;
    if (confirmed && _confirmDeleteItem is not null) {
      await Delete(_confirmDeleteItem);
    }
    _confirmDeleteItem = default;
  }

  // The data has changed, so read it again. With OnRead, the grid asks for it, as Telerik's does
  private void RefreshAfterEdit() {
    if (ProcessData()) {
      _ = PageChanged.InvokeAsync(_page);
    }
  }

  // ---------- Editors ----------

  private bool CanEdit(PixataGridColumn<TItem> column) =>
    column.Editable && column is not PixataGridCommandColumn<TItem> and not PixataGridCheckboxColumn<TItem>
    && !string.IsNullOrWhiteSpace(column.Field) && FieldAccessor.CanWrite(typeof(TItem), column.Field);

  private object? EditValue(PixataGridColumn<TItem> column) =>
    _editItem is null ? null : FieldAccessor.Getter<TItem>(column.Field!)(_editItem);

  private Task SetEditValue(PixataGridColumn<TItem> column, object? value) {
    if (_editItem is null) {
      return Task.CompletedTask;
    }
    FieldAccessor.SetValue(_editItem, column.Field!, value);
    if (FieldIdentifierFor(column) is FieldIdentifier field) {
      _editContext?.NotifyFieldChanged(field);
    }
    return Task.CompletedTask;
  }

  // Validation messages belong to the object that owns the property, which for a nested field is the inner object
  private FieldIdentifier? FieldIdentifierFor(PixataGridColumn<TItem> column) {
    if (_editItem is null || string.IsNullOrWhiteSpace(column.Field)) {
      return null;
    }
    string[] parts = column.Field.Split('.');
    object? owner = parts.Length == 1 ? _editItem : FieldAccessor.Getter<TItem>(string.Join('.', parts[..^1]))(_editItem);
    return owner is null ? null : new FieldIdentifier(owner, parts[^1]);
  }

  private string? ValidationMessage(PixataGridColumn<TItem> column) =>
    _editContext is not null && FieldIdentifierFor(column) is FieldIdentifier field
      ? _editContext.GetValidationMessages(field).FirstOrDefault()
      : null;

  private PixataGridEditorType EditorTypeFor(PixataGridColumn<TItem> column) =>
    column.EditorType ?? column.FilterKind switch {
      FilterKind.Number => PixataGridEditorType.NumericTextBox,
      FilterKind.Date => PixataGridEditorType.DatePicker,
      FilterKind.Boolean => PixataGridEditorType.CheckBox,
      FilterKind.Enum => PixataGridEditorType.DropDownList,
      _ => PixataGridEditorType.TextBox
    };

  private string PopupTitle =>
    PopupEditSettings?.Title ?? (_isNew ? PixataGridMessages.AddNewRecord : PixataGridMessages.Edit);

  private IEnumerable<PixataGridColumn<TItem>> PopupColumns =>
    RenderedColumns().Where(c => c is not PixataGridCommandColumn<TItem> and not PixataGridCheckboxColumn<TItem> && !string.IsNullOrWhiteSpace(c.Field));
}
