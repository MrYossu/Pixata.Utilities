using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataDropDownList{TItem, TValue}"/> and <see cref="PixataComboBox{TItem, TValue}"/> share on top of
/// <see cref="PixataDropDownBase{TItem, TValue, TBound}"/>: keeping track of the selected item, and selecting another
/// </summary>
public abstract class PixataSingleSelectBase<TItem, TValue> : PixataDropDownBase<TItem, TValue, TValue> {
  /// <summary>
  /// Returns the item that has a given value. Only needed when virtually scrolling with <c>OnRead</c>, so the component can show
  /// the text of a value whose item hasn't been loaded yet
  /// </summary>
  [Parameter]
  public Func<TValue, Task<TItem>>? ValueMapper { get; set; }

  private bool _resolved;
  private TValue? _resolvedValue;

  /// <summary>
  /// The item whose value is the current value, if <see cref="HasSelectedItem"/>
  /// </summary>
  private protected TItem? SelectedItem { get; private set; }

  private protected bool HasSelectedItem { get; private set; }

  private protected override bool IsItemSelected(TItem item) =>
    HasSelectedItem && ValuesEqual(ValueOf(item), Value);

  private protected override async Task OnParametersApplied(bool dataChanged) {
    if (dataChanged || !_resolved || !ValuesEqual(Value, _resolvedValue)) {
      await ResolveSelectedItem();
    }
  }

  private protected override async Task OnDataRefreshed() {
    if (!HasSelectedItem) {
      await ResolveSelectedItem();
    }
  }

  /// <summary>
  /// Finds the item that has the current value: in the data, or the one selected before (it may have come from an earlier
  /// read), or from <see cref="ValueMapper"/>
  /// </summary>
  private async Task ResolveSelectedItem() {
    TValue value = Value;
    _resolved = true;
    _resolvedValue = value;
    foreach (TItem item in KnownItems) {
      if (ValuesEqual(ValueOf(item), value)) {
        SelectedItem = item;
        HasSelectedItem = true;
        return;
      }
    }
    if (HasSelectedItem && ValuesEqual(ValueOf(SelectedItem!), value)) {
      return;
    }
    SelectedItem = default;
    HasSelectedItem = false;
    if (ValueMapper is not null && !IsEmptyValue(value)) {
      TItem? mapped = await ValueMapper(value);
      if (mapped is not null && ValuesEqual(Value, value)) {
        SelectedItem = mapped;
        HasSelectedItem = true;
      }
    }
  }

  /// <summary>
  /// Selects an item, updating the value and raising <c>OnChange</c> if it changed
  /// </summary>
  private protected async Task SelectItem(TItem item) {
    TValue value = ValueOf(item);
    SelectedItem = item;
    HasSelectedItem = true;
    await SetValue(value);
  }

  /// <summary>
  /// Sets a value that has no item (a custom value typed into the combo box, or the default when clearing)
  /// </summary>
  private protected async Task SetValueWithoutItem(TValue value) {
    SelectedItem = default;
    HasSelectedItem = false;
    await SetValue(value);
  }

  private async Task SetValue(TValue value) {
    _resolved = true;
    _resolvedValue = value;
    if (await UpdateValue(value)) {
      await OnChange.InvokeAsync(value);
    }
  }

  /// <summary>
  /// The index in the list of the selected item, or -1
  /// </summary>
  private protected int SelectedIndex =>
    HasSelectedItem ? IndexOfItem(IsItemSelected) : -1;

  /// <summary>
  /// Selects the item <paramref name="delta"/> places before or after the selected one (or the first or last if there isn't
  /// one), for the arrow keys when the list is closed
  /// </summary>
  private protected async Task SelectRelative(int delta) {
    if (ItemCount == 0) {
      return;
    }
    int current = SelectedIndex;
    int target = current < 0 ? (delta > 0 ? 0 : ItemCount - 1) : Math.Clamp(current + delta, 0, ItemCount - 1);
    if (target == current) {
      return;
    }
    (bool found, TItem item) = await GetItem(target);
    if (found) {
      await SelectItem(item);
    }
  }

  /// <summary>
  /// Selects the item at an index in the list
  /// </summary>
  private protected async Task SelectIndex(int index) {
    (bool found, TItem item) = await GetItem(index);
    if (found) {
      await SelectItem(item);
    }
  }
}
