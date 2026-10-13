using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A drop-down list: the user picks one item from a list, and can't type a value of their own. Bind <c>Value</c> to the
/// selected item's value (the field named by <c>ValueField</c>)
/// </summary>
/// <typeparam name="TItem">The type of the items</typeparam>
/// <typeparam name="TValue">The type of the value</typeparam>
public partial class PixataDropDownList<TItem, TValue> {
  /// <summary>
  /// The text shown when no item is selected. When set, it is also the first option in the list, which clears the value
  /// </summary>
  [Parameter]
  public string? DefaultText { get; set; }

  /// <summary>
  /// Renders the selected item in the box. The context is the item
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? ValueTemplate { get; set; }

  /// <summary>
  /// Whether to show a button that clears the value
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; }

  /// <summary>
  /// How long (in milliseconds) to wait after the user stops typing in the filter box before raising <c>OnRead</c>. Defaults to
  /// 150
  /// </summary>
  [Parameter]
  public int FilterDebounceDelay { get; set; } = 150;

  /// <summary>
  /// The placeholder of the filter box shown in the popup when <c>Filterable</c> is true
  /// </summary>
  [Parameter]
  public string? FilterPlaceholder { get; set; }

  private protected override string ComponentName => "dropdownlist";

  private protected override ElementReference FocusTarget => Element;

  private protected override int FilterDelay => FilterDebounceDelay;

  private protected override bool ShowPopupFilter => Filterable;

  private protected override string? PopupFilterPlaceholder => FilterPlaceholder;

  private static readonly LeadingOption[] OptionLabelOnly = [LeadingOption.OptionLabel];

  private protected override IReadOnlyList<LeadingOption> LeadingOptions =>
    DefaultText is null || FilterText != "" ? [] : OptionLabelOnly;

  private protected override string LeadingOptionText(LeadingOption option) =>
    DefaultText ?? "";

  private protected override bool? LeadingOptionSelected(LeadingOption option) =>
    !HasSelectedItem;

  private protected override int InitialFocusedPosition() =>
    !HasSelectedItem && LeadingCount > 0 ? 0 : base.InitialFocusedPosition();

  private string DisplayText =>
    HasSelectedItem ? TextOf(SelectedItem!) : DefaultText ?? "";

  private protected override async Task ChooseItem(TItem item) {
    await SelectItem(item);
    await ClosePopup();
  }

  private protected override async Task ChooseLeadingOption(LeadingOption option) {
    await SetValueWithoutItem(default!);
    await ClosePopup();
  }

  private async Task HandleClick() {
    if (Enabled && !ReadOnly) {
      await TogglePopup();
    }
  }

  private async Task HandleClear() {
    await SetValueWithoutItem(default!);
    await ClosePopup();
  }

  private async Task HandleKeyDown(KeyboardEventArgs e) {
    if (!Enabled || ReadOnly) {
      return;
    }
    if (IsOpen) {
      if (await HandleOpenListKey(e)) {
        return;
      }
      if (e.Key == " ") {
        if (FocusedPosition >= 0) {
          await ChoosePosition(FocusedPosition);
        }
      } else if (IsPrintable(e)) {
        int index = TypeAhead(e.Key, FocusedPosition - LeadingCount);
        if (index >= 0) {
          SetFocusedPosition(LeadingCount + index);
        }
      }
      return;
    }
    switch (e.Key) {
      case "ArrowDown" when e.AltKey:
      case "Enter":
      case " ":
        await OpenPopup();
        break;
      case "ArrowDown":
        await SelectRelative(1);
        break;
      case "ArrowUp":
        if (SelectedIndex == 0 && DefaultText is not null) {
          await SetValueWithoutItem(default!);
        } else {
          await SelectRelative(-1);
        }
        break;
      case "Home":
        await SelectIndex(0);
        break;
      case "End":
        await SelectIndex(ItemCount - 1);
        break;
      default:
        if (IsPrintable(e)) {
          int index = TypeAhead(e.Key, SelectedIndex);
          if (index >= 0) {
            await SelectIndex(index);
          }
        }
        break;
    }
  }
}
