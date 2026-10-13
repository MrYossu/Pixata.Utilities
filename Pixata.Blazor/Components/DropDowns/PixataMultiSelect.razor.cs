using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// A multi-select: the user picks any number of items from a list, and the selected items are shown as removable tags. Bind
/// <c>Value</c> to a <c>List&lt;TValue&gt;</c> of the selected items' values (the field named by <c>ValueField</c>)
/// </summary>
/// <typeparam name="TItem">The type of the items</typeparam>
/// <typeparam name="TValue">The type of each value in the list</typeparam>
public partial class PixataMultiSelect<TItem, TValue> {
  private static readonly LeadingOption[] NoLeadingOptions = [];
  private static readonly LeadingOption[] CustomOnly = [LeadingOption.CustomValue];
  private static readonly LeadingOption[] SelectAllOnly = [LeadingOption.SelectAll];
  private static readonly LeadingOption[] CustomAndSelectAll = [LeadingOption.CustomValue, LeadingOption.SelectAll];

  /// <summary>
  /// The hint shown in the text box when nothing is selected
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  /// <summary>
  /// Whether the user can type values that aren't in the list (and press Enter to add them). The typed text becomes a value, so
  /// <c>TValue</c> should be <c>string</c> (or a type the text can be converted to)
  /// </summary>
  [Parameter]
  public bool AllowCustom { get; set; }

  /// <summary>
  /// Whether to show a button that clears all the selected items. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowClearButton { get; set; } = true;

  /// <summary>
  /// Whether to show an arrow button that opens the list
  /// </summary>
  [Parameter]
  public bool ShowArrowButton { get; set; }

  /// <summary>
  /// Whether the list closes after an item is chosen. Defaults to true. Set it to false to let the user pick several items in
  /// one go
  /// </summary>
  [Parameter]
  public bool AutoClose { get; set; } = true;

  /// <summary>
  /// When <c>Filterable</c>, the number of characters the user must type before the list opens. Defaults to 0
  /// </summary>
  [Parameter]
  public int MinLength { get; set; }

  /// <summary>
  /// Whether the typed text stays in the text box after an item is chosen (so the user can choose another that matches it).
  /// Defaults to false
  /// </summary>
  [Parameter]
  public bool PersistFilterOnSelect { get; set; }

  /// <summary>
  /// How the selected items are shown: one tag each (the default), or a single summary tag
  /// </summary>
  [Parameter]
  public PixataMultiSelectTagMode TagMode { get; set; } = PixataMultiSelectTagMode.Multiple;

  /// <summary>
  /// In <see cref="PixataMultiSelectTagMode.Multiple"/> mode, the most tags to show. Any more selected items are counted in a
  /// summary tag
  /// </summary>
  [Parameter]
  public int? MaxAllowedTags { get; set; }

  /// <summary>
  /// Renders the content of each tag. The context is the selected item
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? TagTemplate { get; set; }

  /// <summary>
  /// Renders the content of the summary tag
  /// </summary>
  [Parameter]
  public RenderFragment<PixataMultiSelectSummaryTagTemplateContext<TItem>>? SummaryTagTemplate { get; set; }

  /// <summary>
  /// Whether each item in the list has a checkbox
  /// </summary>
  [Parameter]
  public bool EnableCheckBoxes { get; set; }

  /// <summary>
  /// Whether the list starts with a "Select all" option, which selects (or, if they are all selected, deselects) all the items
  /// in the list
  /// </summary>
  [Parameter]
  public bool EnableSelectAll { get; set; }

  /// <summary>
  /// How long (in milliseconds) to wait after the user stops typing before raising <c>OnRead</c>. Defaults to 150
  /// </summary>
  [Parameter]
  public int DebounceDelay { get; set; } = 150;

  /// <summary>
  /// Returns the items that have the given values. Only needed when virtually scrolling with <c>OnRead</c>, so the component can
  /// show tags for values whose items haven't been loaded yet
  /// </summary>
  [Parameter]
  public Func<List<TValue>, Task<List<TItem>>>? ValueMapper { get; set; }

  /// <summary>
  /// A selected value, with its item if it has one (a custom value doesn't)
  /// </summary>
  private readonly record struct Tag(TValue Value, bool HasItem, TItem? Item, string Text);

  private ElementReference _input;
  private string _text = "";
  private bool _showCustom;
  private List<Tag> _tags = [];
  private HashSet<TValue> _selected = [];
  private List<TValue> _resolvedValues = [];
  private readonly List<(TValue Value, TItem Item)> _rememberedItems = [];

  private protected override string ComponentName => "multiselect";

  private protected override ElementReference FocusTarget => _input;

  private protected override int FilterDelay => DebounceDelay;

  private protected override bool Multiple => true;

  private protected override bool ShowCheckBoxes => EnableCheckBoxes;

  private bool ShowSelectAll =>
    EnableSelectAll && !IsRemoteVirtual && ItemCount > 0;

  private protected override IReadOnlyList<LeadingOption> LeadingOptions =>
    (_showCustom, ShowSelectAll) switch {
      (true, true) => CustomAndSelectAll,
      (true, false) => CustomOnly,
      (false, true) => SelectAllOnly,
      _ => NoLeadingOptions
    };

  private protected override string LeadingOptionText(LeadingOption option) =>
    option == LeadingOption.CustomValue ? _text.Trim() : "Select all";

  private protected override bool? LeadingOptionSelected(LeadingOption option) {
    if (option != LeadingOption.SelectAll) {
      return false;
    }
    int selected = ListItems.Count(IsItemSelected);
    return selected == 0 ? false : selected == ItemCount ? true : null;
  }

  private protected override bool IsItemSelected(TItem item) =>
    _selected.Contains(ValueOf(item));

  private List<TValue> CurrentValues =>
    Value ?? [];

  private bool CanEdit =>
    Enabled && !ReadOnly;

  private string TagListId =>
    $"{Uid}-tags";

  #region Tags

  private IEnumerable<Tag> VisibleTags =>
    TagMode == PixataMultiSelectTagMode.Single
      ? []
      : MaxAllowedTags is int max && _tags.Count > max ? _tags.Take(Math.Max(0, max)) : _tags;

  private int SummaryCount =>
    TagMode == PixataMultiSelectTagMode.Single
      ? _tags.Count
      : MaxAllowedTags is int max && _tags.Count > max ? _tags.Count - Math.Max(0, max) : 0;

  private string SummaryText =>
    TagMode == PixataMultiSelectTagMode.Single
      ? $"{SummaryCount} item{(SummaryCount == 1 ? "" : "s")} selected"
      : $"{SummaryCount} more selected";

  private PixataMultiSelectSummaryTagTemplateContext<TItem> SummaryContext =>
    new() { Items = _tags.Where(t => t.HasItem).Select(t => t.Item!).ToList(), Count = SummaryCount };

  /// <summary>
  /// The item to pass to the tag template: the tag's item, or the value itself for a custom value when the items are the
  /// values (eg strings)
  /// </summary>
  private static (bool Found, TItem Item) TemplateItem(Tag tag) =>
    tag.HasItem ? (true, tag.Item!) : tag.Value is TItem item ? (true, item) : (false, default!);

  private protected override async Task OnParametersApplied(bool dataChanged) {
    if (dataChanged || !_resolvedValues.SequenceEqual(CurrentValues)) {
      await ResolveTags();
    }
  }

  private protected override async Task OnDataRefreshed() {
    UpdateCustomOption();
    if (_tags.Any(t => !t.HasItem)) {
      await ResolveTags();
    }
  }

  /// <summary>
  /// Finds the item for each selected value, to show its text in the tag
  /// </summary>
  private async Task ResolveTags() {
    List<TValue> values = CurrentValues.ToList();
    _resolvedValues = values;
    _selected = new(values);
    foreach (TItem item in KnownItems) {
      TValue value = ValueOf(item);
      if (_selected.Contains(value) && !_rememberedItems.Any(r => ValuesEqual(r.Value, value))) {
        _rememberedItems.Add((value, item));
      }
    }
    List<TValue> missing = values.Where(v => !_rememberedItems.Any(r => ValuesEqual(r.Value, v))).ToList();
    if (missing.Count > 0 && ValueMapper is not null) {
      foreach (TItem item in await ValueMapper(missing) ?? []) {
        _rememberedItems.Add((ValueOf(item), item));
      }
    }
    _tags = values.Select(MakeTag).ToList();
  }

  private Tag MakeTag(TValue value) {
    foreach ((TValue known, TItem item) in _rememberedItems) {
      if (ValuesEqual(known, value)) {
        return new(value, true, item, TextOf(item));
      }
    }
    return new(value, false, default, Convert.ToString(value, CultureInfo.CurrentCulture) ?? "");
  }

  private void Remember(TValue value, TItem item) {
    if (!_rememberedItems.Any(r => ValuesEqual(r.Value, value))) {
      _rememberedItems.Add((value, item));
    }
  }

  /// <summary>
  /// Sets the value, raising <c>OnChange</c>
  /// </summary>
  private async Task SetValues(List<TValue> values) {
    if (await UpdateValue(values)) {
      await OnChange.InvokeAsync(values);
    }
    await ResolveTags();
    UpdateCustomOption();
  }

  private async Task RemoveValue(TValue value) {
    if (CanEdit) {
      await SetValues(CurrentValues.Where(v => !ValuesEqual(v, value)).ToList());
    }
  }

  private async Task RemoveSummarised() {
    if (!CanEdit) {
      return;
    }
    int keep = TagMode == PixataMultiSelectTagMode.Single ? 0 : _tags.Count - SummaryCount;
    await SetValues(CurrentValues.Take(keep).ToList());
  }

  #endregion

  #region Choosing

  private protected override async Task ChooseItem(TItem item) {
    TValue value = ValueOf(item);
    List<TValue> values = CurrentValues.ToList();
    int index = values.FindIndex(v => ValuesEqual(v, value));
    if (index >= 0) {
      values.RemoveAt(index);
    } else {
      values.Add(value);
      Remember(value, item);
    }
    await SetValues(values);
    await AfterChoosing(value);
  }

  private protected override async Task ChooseLeadingOption(LeadingOption option) {
    if (option == LeadingOption.CustomValue) {
      await AddCustom();
      return;
    }
    List<TItem> visible = ListItems.ToList();
    List<TValue> values = CurrentValues.ToList();
    if (visible.All(IsItemSelected)) {
      HashSet<TValue> remove = new(visible.Select(ValueOf));
      values.RemoveAll(remove.Contains);
    } else {
      foreach (TItem item in visible.Where(i => !IsItemSelected(i))) {
        TValue value = ValueOf(item);
        values.Add(value);
        Remember(value, item);
      }
    }
    await SetValues(values);
  }

  private protected override Task EnterWithoutFocusedOption() =>
    AllowCustom ? AddCustom() : Task.CompletedTask;

  /// <summary>
  /// Adds what the user typed as a value
  /// </summary>
  private async Task AddCustom() {
    string text = _text.Trim();
    if (!AllowCustom || text == "" || !TryConvert(text, out TValue value)) {
      return;
    }
    if (FindItemWithText(text) is (true, TItem item)) {
      if (!IsItemSelected(item)) {
        await ChooseItem(item);
      }
      return;
    }
    if (!CurrentValues.Any(v => ValuesEqual(v, value))) {
      await SetValues([.. CurrentValues, value]);
    }
    await AfterChoosing(value);
  }

  /// <summary>
  /// After an item is chosen, clears the typed text (unless <see cref="PersistFilterOnSelect"/>) and closes the list (if
  /// <see cref="AutoClose"/>)
  /// </summary>
  private async Task AfterChoosing(TValue chosen) {
    if (!PersistFilterOnSelect && _text != "") {
      _text = "";
      _showCustom = false;
      await ClearFilter();
      int index = IndexOfItem(i => ValuesEqual(ValueOf(i), chosen));
      if (IsOpen && index >= 0) {
        SetFocusedPosition(LeadingCount + index);
      }
    }
    if (AutoClose) {
      await ClosePopup();
    }
  }

  private (bool Found, TItem Item) FindItemWithText(string text) {
    foreach (TItem item in KnownItems) {
      if (PixataStringFilterOperator.IsEqualTo.Matches(TextOf(item), text)) {
        return (true, item);
      }
    }
    return (false, default!);
  }

  /// <summary>
  /// Shows the typed text as an option to add, if custom values are allowed and it isn't already an item or a value
  /// </summary>
  private void UpdateCustomOption() {
    string text = _text.Trim();
    _showCustom = AllowCustom && text != "" && !FindItemWithText(text).Found
      && !(TryConvert(text, out TValue value) && CurrentValues.Any(v => ValuesEqual(v, value)));
  }

  private static bool TryConvert(string text, out TValue value) {
    try {
      value = PixataFieldReader.ConvertTo<TValue>(text)!;
      return true;
    }
    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException) {
      value = default!;
      return false;
    }
  }

  #endregion

  #region Events

  private async Task HandleInput(ChangeEventArgs e) {
    _text = e.Value as string ?? "";
    UpdateCustomOption();
    if (Filterable) {
      if (_text.Length >= MinLength) {
        await OpenPopup();
        await SetFilter(_text);
      } else {
        await ClosePopup();
      }
    } else {
      await OpenPopup();
      FocusFirstMatch(_text);
      if (FocusedPosition < 0 && _showCustom) {
        SetFocusedPosition(0);
      }
    }
  }

  private async Task HandleKeyDown(KeyboardEventArgs e) {
    if (!CanEdit) {
      return;
    }
    if (e.Key == "Backspace" && _text == "" && CurrentValues.Count > 0) {
      await SetValues(CurrentValues.Take(CurrentValues.Count - 1).ToList());
      return;
    }
    if (IsOpen) {
      await HandleOpenListKey(e);
      return;
    }
    switch (e.Key) {
      case "ArrowDown":
      case "ArrowUp":
        await OpenIfAllowed();
        break;
      case "Enter":
        if (AllowCustom && _text.Trim() != "") {
          await AddCustom();
        } else {
          await OpenIfAllowed();
        }
        break;
    }
  }

  private async Task OpenIfAllowed() {
    if (!Filterable || _text.Length >= MinLength) {
      await OpenPopup();
    }
  }

  private async Task HandleClick() {
    if (!Enabled) {
      return;
    }
    await FocusAsync();
    if (!ReadOnly && !IsOpen) {
      await OpenIfAllowed();
    }
  }

  private async Task HandleButtonClick() {
    if (!CanEdit) {
      return;
    }
    await FocusAsync();
    if (IsOpen) {
      await ClosePopup();
    } else {
      await OpenIfAllowed();
    }
  }

  private async Task HandleClear() {
    _text = "";
    _showCustom = false;
    await SetValues([]);
    await ClosePopup();
    await FocusAsync();
  }

  private protected override async Task OnFocusLost() {
    _text = "";
    _showCustom = false;
    await ClearFilter();
  }

  #endregion
}
