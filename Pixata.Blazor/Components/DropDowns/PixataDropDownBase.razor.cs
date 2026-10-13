using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataDropDownList{TItem, TValue}"/>, <see cref="PixataComboBox{TItem, TValue}"/> and
/// <see cref="PixataMultiSelect{TItem, TValue}"/> have in common: the data (<c>Data</c> or <c>OnRead</c>, with
/// <c>TextField</c>, <c>ValueField</c> and <c>GroupField</c>), filtering, grouping, virtual scrolling, the popup list and its
/// templates, and moving around the list with the keyboard
/// </summary>
/// <typeparam name="TItem">The type of the items in the list</typeparam>
/// <typeparam name="TValue">The type of an item's value (the field named by <c>ValueField</c>)</typeparam>
/// <typeparam name="TBound">The type of the bound <c>Value</c>: <typeparamref name="TValue"/> for the single-selection
/// components, and a <c>List&lt;TValue&gt;</c> for the multi-select</typeparam>
public abstract partial class PixataDropDownBase<TItem, TValue, TBound> : PixataInputBase<TBound>, IAsyncDisposable {
  private const int PageJump = 8;
  private const int TypeAheadResetMilliseconds = 700;

  private static readonly bool ItemIsSimple = IsSimple(typeof(TItem));

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  #region Data parameters

  /// <summary>
  /// The items to choose from. Either set this, or handle <see cref="OnRead"/> to supply the items yourself
  /// </summary>
  [Parameter]
  public IEnumerable<TItem>? Data { get; set; }

  /// <summary>
  /// The field of each item that holds the text shown in the list. Nested fields can be given as <c>Address.City</c>. Ignored
  /// for primitive data (strings, numbers, enums and so on), where the item itself is the text. Defaults to <c>Text</c>
  /// </summary>
  [Parameter]
  public string? TextField { get; set; } = "Text";

  /// <summary>
  /// The field of each item that holds its value, which is what <c>Value</c> is bound to. Ignored for primitive data, where the
  /// item itself is the value. Defaults to <c>Value</c>. Set it to <c>""</c> to have the item itself as the value
  /// </summary>
  [Parameter]
  public string? ValueField { get; set; } = "Value";

  /// <summary>
  /// The field to group the items by. Each group gets a header, and the groups are sorted by this field
  /// </summary>
  [Parameter]
  public string? GroupField { get; set; }

  /// <summary>
  /// Raised when the component needs data, if you want to supply it yourself (eg from a database or an API) rather than setting
  /// <see cref="Data"/>. It is raised when the component is first shown, when the user filters, and (when virtually scrolling)
  /// as the user scrolls. Set the event args' <c>Data</c> (and <c>Total</c> when virtually scrolling)
  /// </summary>
  [Parameter]
  public EventCallback<PixataDropDownReadEventArgs> OnRead { get; set; }

  /// <summary>
  /// Whether the user can type to filter the list
  /// </summary>
  [Parameter]
  public bool Filterable { get; set; }

  /// <summary>
  /// How the text the user types is matched against the items when filtering. Defaults to
  /// <see cref="PixataStringFilterOperator.StartsWith"/>
  /// </summary>
  [Parameter]
  public PixataStringFilterOperator FilterOperator { get; set; } = PixataStringFilterOperator.StartsWith;

  /// <summary>
  /// <see cref="PixataDropDownScrollMode.Virtual"/> renders only the items that can be seen, for very long lists
  /// </summary>
  [Parameter]
  public PixataDropDownScrollMode ScrollMode { get; set; } = PixataDropDownScrollMode.Scrollable;

  /// <summary>
  /// The height of each item in pixels when virtually scrolling. Defaults to 28, which suits the medium size and plain text
  /// items. If your <see cref="ItemTemplate"/> makes the items taller, set this to match
  /// </summary>
  [Parameter]
  public decimal ItemHeight { get; set; } = 28;

  /// <summary>
  /// When virtually scrolling with <see cref="OnRead"/>, the number of items requested at a time. Defaults to 20
  /// </summary>
  [Parameter]
  public int PageSize { get; set; } = 20;

  #endregion

  #region Template parameters

  /// <summary>
  /// Renders each item in the list. The context is the item
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? ItemTemplate { get; set; }

  /// <summary>
  /// Content shown at the top of the popup, above the list
  /// </summary>
  [Parameter]
  public RenderFragment? HeaderTemplate { get; set; }

  /// <summary>
  /// Content shown at the bottom of the popup, below the list
  /// </summary>
  [Parameter]
  public RenderFragment? FooterTemplate { get; set; }

  /// <summary>
  /// Content shown in the popup when there are no items (or none match the filter). Defaults to "No data found."
  /// </summary>
  [Parameter]
  public RenderFragment? NoDataTemplate { get; set; }

  #endregion

  #region Appearance parameters

  /// <summary>
  /// When true, the value can be seen but not changed
  /// </summary>
  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// The component's width, as a CSS length (eg <c>"300px"</c>). By default it fills the width of its container
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// <c>none</c>, <c>sm</c>, <c>md</c> (the default), <c>lg</c> or <c>full</c>. See <see cref="PixataThemeConstants.Rounded"/>
  /// </summary>
  [Parameter]
  public string? Rounded { get; set; }

  /// <summary>
  /// <c>solid</c> (the default), <c>flat</c> or <c>outline</c>. See <see cref="PixataThemeConstants.FillMode"/>
  /// </summary>
  [Parameter]
  public string? FillMode { get; set; }

  /// <summary>
  /// The size of the popup (eg <c>Height</c>, <c>MaxHeight</c>, <c>Width</c>) and extra CSS classes for it
  /// </summary>
  [Parameter]
  public PixataPopupSettings? PopupSettings { get; set; }

  /// <summary>
  /// The tab index of the focusable element
  /// </summary>
  [Parameter]
  public int? TabIndex { get; set; }

  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public string? AriaLabelledBy { get; set; }

  [Parameter]
  public string? AriaDescribedBy { get; set; }

  #endregion

  #region Events

  /// <summary>
  /// Raised when the user changes the value (by choosing an item, typing a value and pressing Enter or leaving the component,
  /// or clearing it). The argument is the new value. Unlike <c>ValueChanged</c>, it isn't raised when the value is changed by
  /// the code
  /// </summary>
  [Parameter]
  public EventCallback<object> OnChange { get; set; }

  /// <summary>
  /// Raised before the list opens. Set <see cref="PixataDropDownOpenEventArgs.IsCancelled"/> to keep it closed
  /// </summary>
  [Parameter]
  public EventCallback<PixataDropDownOpenEventArgs> OnOpen { get; set; }

  /// <summary>
  /// Raised before the list closes. Set <see cref="PixataDropDownCloseEventArgs.IsCancelled"/> to keep it open
  /// </summary>
  [Parameter]
  public EventCallback<PixataDropDownCloseEventArgs> OnClose { get; set; }

  /// <summary>
  /// Raised when the focus leaves the component
  /// </summary>
  [Parameter]
  public EventCallback OnBlur { get; set; }

  #endregion

  #region State

  /// <summary>
  /// The kinds of option that can sit above the items in the list
  /// </summary>
  private protected enum LeadingOption {
    OptionLabel,
    CustomValue,
    SelectAll
  }

  /// <summary>
  /// A row in the list: an item (<see cref="Index"/> is its index in the list of items) or a group header (<see cref="Index"/>
  /// is -1)
  /// </summary>
  private protected readonly record struct Row(int Index, string? Group, TItem? Item);

  /// <summary>
  /// A unique id, used for the ids of the list and its options, and to link the component's element to its popup in script
  /// </summary>
  private protected readonly string Uid = $"pixata-dropdown-{Guid.NewGuid():N}";

  /// <summary>
  /// The component's outer element, which the popup is placed against
  /// </summary>
  private protected ElementReference Element;

  private ElementReference _content;
  private ElementReference _filterInput;
  private DotNetObjectReference<PixataDropDownBase<TItem, TValue, TBound>>? _dotNet;
  private bool _disposed;

  private List<TItem> _source = [];
  private readonly List<TItem> _items = [];
  private readonly List<Row> _rows = [];
  private readonly List<int> _itemRows = [];
  private readonly Dictionary<int, List<TItem>> _pages = [];
  private int _total;
  private Virtualize<Row>? _virtualize;
  private int _readVersion;
  private CancellationTokenSource? _debounce;
  private bool _initialised;
  private object? _lastData;
  private int _lastCount;
  private string? _lastFields;

  private bool _scrollPending;
  private Func<ElementReference>? _pendingFocus;
  private readonly List<(Func<ElementReference> Element, string Text)> _pendingTexts = [];
  private string _typeAhead = "";
  private DateTime _typeAheadTime;

  /// <summary>
  /// Whether the popup is open
  /// </summary>
  private protected bool IsOpen { get; private set; }

  /// <summary>
  /// The position of the option that has the keyboard focus (counting the <see cref="LeadingOptions"/> first), or -1
  /// </summary>
  private protected int FocusedPosition { get; private set; } = -1;

  /// <summary>
  /// The filter applied to the data
  /// </summary>
  private protected string FilterText { get; private set; } = "";

  /// <summary>
  /// True while <see cref="OnRead"/> is fetching data
  /// </summary>
  private protected bool Loading { get; private set; }

  #endregion

  #region What the components provide

  /// <summary>
  /// The component's name as used in its CSS classes, eg <c>dropdownlist</c>
  /// </summary>
  private protected abstract string ComponentName { get; }

  /// <summary>
  /// The element <see cref="FocusAsync"/> focuses
  /// </summary>
  private protected abstract ElementReference FocusTarget { get; }

  /// <summary>
  /// How long to wait after the user stops typing before calling <see cref="OnRead"/> with the new filter
  /// </summary>
  private protected abstract int FilterDelay { get; }

  /// <summary>
  /// Whether the item is (one of) the selected item(s)
  /// </summary>
  private protected abstract bool IsItemSelected(TItem item);

  /// <summary>
  /// Called when the user chooses an item in the list, by clicking it or pressing Enter
  /// </summary>
  private protected abstract Task ChooseItem(TItem item);

  /// <summary>
  /// The options shown above the items (eg the default text of the drop-down list, or "Select all")
  /// </summary>
  private protected virtual IReadOnlyList<LeadingOption> LeadingOptions => [];

  private protected virtual string LeadingOptionText(LeadingOption option) => "";

  /// <summary>
  /// Whether a leading option is selected: true, false, or null for partly (the "Select all" option when some items are selected)
  /// </summary>
  private protected virtual bool? LeadingOptionSelected(LeadingOption option) => false;

  private protected virtual Task ChooseLeadingOption(LeadingOption option) => Task.CompletedTask;

  /// <summary>
  /// Called when Enter is pressed with the list open but no option focused
  /// </summary>
  private protected virtual Task EnterWithoutFocusedOption() => Task.CompletedTask;

  /// <summary>
  /// Called when the focus leaves the component, after the popup has closed
  /// </summary>
  private protected virtual Task OnFocusLost() => Task.CompletedTask;

  /// <summary>
  /// Called after the parameters have been applied and the data updated
  /// </summary>
  private protected virtual Task OnParametersApplied(bool dataChanged) => Task.CompletedTask;

  /// <summary>
  /// Called after the data has been fetched again (eg the filter changed)
  /// </summary>
  private protected virtual Task OnDataRefreshed() => Task.CompletedTask;

  /// <summary>
  /// Whether more than one item can be selected
  /// </summary>
  private protected virtual bool Multiple => false;

  /// <summary>
  /// Whether the items have checkboxes
  /// </summary>
  private protected virtual bool ShowCheckBoxes => false;

  /// <summary>
  /// Whether the popup has its own filter box (the drop-down list's), rather than the component's input doing the filtering
  /// </summary>
  private protected virtual bool ShowPopupFilter => false;

  private protected virtual string? PopupFilterPlaceholder => null;

  /// <summary>
  /// The position focused when the popup opens
  /// </summary>
  private protected virtual int InitialFocusedPosition() {
    int selected = IndexOfItem(IsItemSelected);
    if (selected >= 0) {
      return LeadingCount + selected;
    }
    return ItemCount > 0 ? LeadingCount : (LeadingCount > 0 ? 0 : -1);
  }

  #endregion

  #region Data

  private protected bool UsesOnRead =>
    OnRead.HasDelegate;

  private protected bool IsVirtual =>
    ScrollMode == PixataDropDownScrollMode.Virtual;

  private bool RemoteVirtual =>
    UsesOnRead && IsVirtual;

  /// <summary>
  /// Whether the data comes a page at a time from <see cref="OnRead"/>, so not all the items are known
  /// </summary>
  private protected bool IsRemoteVirtual =>
    RemoteVirtual;

  private int PageSizeValue =>
    Math.Max(1, PageSize);

  /// <summary>
  /// The number of items in the list (after filtering)
  /// </summary>
  private protected int ItemCount =>
    RemoteVirtual ? _total : _items.Count;

  private protected int LeadingCount =>
    LeadingOptions.Count;

  /// <summary>
  /// The number of options the user can move between: the leading options and the items
  /// </summary>
  private protected int OptionCount =>
    LeadingCount + ItemCount;

  /// <summary>
  /// The items in the list, in the order shown (after filtering and grouping). When virtually scrolling with
  /// <see cref="OnRead"/>, only the pages loaded so far
  /// </summary>
  private protected IEnumerable<TItem> ListItems =>
    RemoteVirtual ? _pages.OrderBy(p => p.Key).SelectMany(p => p.Value) : _items;

  /// <summary>
  /// The items the component knows about, unfiltered where possible, for finding the item that has a given value
  /// </summary>
  private protected IEnumerable<TItem> KnownItems =>
    RemoteVirtual ? _pages.OrderBy(p => p.Key).SelectMany(p => p.Value) : _source;

  private protected string TextOf(TItem item) =>
    PixataFieldReader.GetText(item, ItemIsSimple ? null : TextField);

  private protected TValue ValueOf(TItem item) {
    object? value = PixataFieldReader.GetValue(item, ItemIsSimple ? null : ValueField);
    return value is null ? default! : PixataFieldReader.ConvertTo<TValue>(value)!;
  }

  private protected static bool ValuesEqual(TValue? a, TValue? b) =>
    EqualityComparer<TValue?>.Default.Equals(a, b);

  /// <summary>
  /// True for null, the type's default and <c>""</c>
  /// </summary>
  private protected static bool IsEmptyValue(TValue? value) =>
    value is null || ValuesEqual(value, default) || value is string { Length: 0 };

  private protected bool TryGetItem(int index, out TItem item) {
    if (RemoteVirtual) {
      if (index >= 0 && _pages.TryGetValue(index / PageSizeValue, out List<TItem>? page) && index % PageSizeValue < page.Count) {
        item = page[index % PageSizeValue];
        return true;
      }
    } else if (index >= 0 && index < _items.Count) {
      item = _items[index];
      return true;
    }
    item = default!;
    return false;
  }

  /// <summary>
  /// The item at an index in the list, fetching its page first if it hasn't been loaded
  /// </summary>
  private protected async Task<(bool Found, TItem Item)> GetItem(int index) {
    if (TryGetItem(index, out TItem item)) {
      return (true, item);
    }
    if (RemoteVirtual && index >= 0 && index < _total) {
      await LoadPage(index / PageSizeValue, _readVersion);
      if (TryGetItem(index, out item)) {
        return (true, item);
      }
    }
    return (false, default!);
  }

  /// <summary>
  /// The index in the list of the first item that matches, or -1. When virtually scrolling with <see cref="OnRead"/>, only the
  /// loaded pages are searched
  /// </summary>
  private protected int IndexOfItem(Func<TItem, bool> match) {
    if (!RemoteVirtual) {
      return _items.FindIndex(i => match(i));
    }
    foreach ((int page, List<TItem> items) in _pages.OrderBy(p => p.Key)) {
      int index = items.FindIndex(i => match(i));
      if (index >= 0) {
        return page * PageSizeValue + index;
      }
    }
    return -1;
  }

  protected override async Task OnParametersSetAsync() {
    if (!Enabled && IsOpen) {
      IsOpen = false;
      FocusedPosition = -1;
    }
    bool dataChanged = false;
    string fields = $"{TextField}|{ValueField}|{GroupField}|{ScrollMode}";
    if (UsesOnRead) {
      if (!_initialised) {
        _initialised = true;
        _lastFields = fields;
        await RefreshData();
        dataChanged = true;
      } else if (fields != _lastFields) {
        _lastFields = fields;
        Rebuild();
        dataChanged = true;
      }
    } else {
      int count = Data switch {
        ICollection<TItem> collection => collection.Count,
        IReadOnlyCollection<TItem> collection => collection.Count,
        ICollection collection => collection.Count,
        _ => -1
      };
      if (!_initialised || !ReferenceEquals(Data, _lastData) || count != _lastCount || fields != _lastFields) {
        _initialised = true;
        _lastData = Data;
        _lastCount = count;
        _lastFields = fields;
        _source = Data?.ToList() ?? [];
        Rebuild();
        dataChanged = true;
      }
    }
    await OnParametersApplied(dataChanged);
  }

  /// <summary>
  /// Applies the filter (for local data) and the grouping to the source items, giving the items and rows shown
  /// </summary>
  private void Rebuild() {
    IEnumerable<TItem> items = _source;
    if (!UsesOnRead && FilterText != "") {
      items = items.Where(i => FilterOperator.Matches(TextOf(i), FilterText));
    }
    _items.Clear();
    _rows.Clear();
    _itemRows.Clear();
    if (string.IsNullOrEmpty(GroupField)) {
      _items.AddRange(items);
      for (int i = 0; i < _items.Count; i++) {
        _itemRows.Add(i);
        _rows.Add(new(i, null, _items[i]));
      }
    } else {
      IEnumerable<IGrouping<object?, TItem>> groups = items
        .GroupBy(i => PixataFieldReader.GetValue(i, GroupField))
        .OrderBy(g => g.Key, GroupKeyComparer.Instance);
      foreach (IGrouping<object?, TItem> group in groups) {
        _rows.Add(new(-1, GroupText(group.Key), default));
        foreach (TItem item in group) {
          _itemRows.Add(_rows.Count);
          _rows.Add(new(_items.Count, null, item));
          _items.Add(item);
        }
      }
    }
  }

  private static string GroupText(object? key) =>
    key switch {
      null => "",
      IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
      _ => key.ToString() ?? ""
    };

  /// <summary>
  /// Fetches the data again: from <see cref="Data"/>, or by raising <see cref="OnRead"/>
  /// </summary>
  private async Task RefreshData() {
    if (!UsesOnRead) {
      _source = Data?.ToList() ?? [];
      Rebuild();
      await OnDataRefreshed();
      return;
    }
    int version = ++_readVersion;
    Loading = true;
    try {
      if (RemoteVirtual) {
        _pages.Clear();
        _total = 0;
        _items.Clear();
        _rows.Clear();
        _itemRows.Clear();
        await LoadPage(0, version);
        if (_virtualize is not null && version == _readVersion) {
          await _virtualize.RefreshDataAsync();
        }
      } else {
        PixataDropDownReadEventArgs args = new() { Filter = FilterText, FilterOperator = FilterOperator, Skip = 0, Take = 0 };
        await OnRead.InvokeAsync(args);
        if (version != _readVersion) {
          return;
        }
        _source = args.Data?.Cast<TItem>().ToList() ?? [];
        Rebuild();
      }
    }
    finally {
      if (version == _readVersion) {
        Loading = false;
      }
    }
    if (version == _readVersion) {
      await OnDataRefreshed();
    }
  }

  /// <summary>
  /// Fetches one page of data with <see cref="OnRead"/> when virtually scrolling
  /// </summary>
  private async Task<List<TItem>?> LoadPage(int page, int version) {
    if (_pages.TryGetValue(page, out List<TItem>? items)) {
      return items;
    }
    PixataDropDownReadEventArgs args = new() {
      Filter = FilterText,
      FilterOperator = FilterOperator,
      Skip = page * PageSizeValue,
      Take = PageSizeValue
    };
    await OnRead.InvokeAsync(args);
    if (version != _readVersion) {
      return null;
    }
    items = args.Data?.Cast<TItem>().ToList() ?? [];
    _pages[page] = items;
    _total = Math.Max(args.Total, page * PageSizeValue + items.Count);
    return items;
  }

  /// <summary>
  /// Supplies the rows to the virtualised list when the data comes from <see cref="OnRead"/>
  /// </summary>
  private async ValueTask<ItemsProviderResult<Row>> ProvideRows(ItemsProviderRequest request) {
    int version = _readVersion;
    int size = PageSizeValue;
    int end = request.StartIndex + Math.Max(1, request.Count);
    List<Row> rows = [];
    for (int page = request.StartIndex / size; page <= (end - 1) / size; page++) {
      List<TItem>? items = await LoadPage(page, version);
      if (items is null) {
        return new([], _total);
      }
      for (int i = 0; i < items.Count; i++) {
        int index = page * size + i;
        if (index >= request.StartIndex && index < end) {
          rows.Add(new(index, null, items[i]));
        }
      }
      if (items.Count < size) {
        break;
      }
    }
    return new(rows, _total);
  }

  /// <summary>
  /// Filters the list. With <see cref="OnRead"/>, the read is delayed by <see cref="FilterDelay"/> so it isn't raised for
  /// every key press
  /// </summary>
  private protected async Task SetFilter(string text) {
    if (text == FilterText) {
      return;
    }
    FilterText = text;
    if (UsesOnRead) {
      _readVersion++;
      _debounce?.Cancel();
      _debounce = new();
      CancellationToken token = _debounce.Token;
      if (FilterDelay > 0) {
        try {
          await Task.Delay(FilterDelay, token);
        }
        catch (TaskCanceledException) {
          return;
        }
      }
      if (token.IsCancellationRequested || _disposed) {
        return;
      }
      await RefreshData();
    } else {
      Rebuild();
    }
    FocusedPosition = ItemCount > 0 ? LeadingCount : (LeadingCount > 0 ? 0 : -1);
    _scrollPending = true;
  }

  /// <summary>
  /// Removes the filter, if there is one
  /// </summary>
  private protected async Task ClearFilter() {
    if (FilterText == "") {
      return;
    }
    FilterText = "";
    _debounce?.Cancel();
    if (UsesOnRead) {
      await RefreshData();
    } else {
      Rebuild();
    }
  }

  #endregion

  #region Popup and focus

  /// <summary>
  /// Opens the popup, unless the component is disabled or read-only, or an <see cref="OnOpen"/> handler cancels it
  /// </summary>
  /// <returns>True if the popup is open</returns>
  private protected async Task<bool> OpenPopup() {
    if (IsOpen) {
      return true;
    }
    if (!Enabled || ReadOnly) {
      return false;
    }
    PixataDropDownOpenEventArgs args = new();
    await OnOpen.InvokeAsync(args);
    if (args.IsCancelled) {
      return false;
    }
    IsOpen = true;
    FocusedPosition = InitialFocusedPosition();
    _scrollPending = true;
    if (ShowPopupFilter) {
      _pendingFocus = () => _filterInput;
    }
    return true;
  }

  /// <summary>
  /// Closes the popup, unless an <see cref="OnClose"/> handler cancels it (which it can't when <paramref name="force"/> is true).
  /// Any filter is removed
  /// </summary>
  /// <returns>True if the popup is closed</returns>
  private protected async Task<bool> ClosePopup(bool force = false, bool returnFocus = true) {
    if (!IsOpen) {
      return true;
    }
    PixataDropDownCloseEventArgs args = new();
    await OnClose.InvokeAsync(args);
    if (args.IsCancelled && !force) {
      return false;
    }
    IsOpen = false;
    FocusedPosition = -1;
    _virtualize = null;
    if (returnFocus && ShowPopupFilter) {
      // The focus was in the popup's filter box, which has gone
      _pendingFocus = () => FocusTarget;
    }
    await ClearFilter();
    return true;
  }

  private protected async Task TogglePopup() {
    if (IsOpen) {
      await ClosePopup();
    } else {
      await OpenPopup();
    }
  }

  /// <summary>
  /// Focuses an element after the next render (eg one that is about to be rendered)
  /// </summary>
  private protected void FocusAfterRender(Func<ElementReference> element) =>
    _pendingFocus = element;

  /// <summary>
  /// Writes the text of a text box the user types into, after the next render. Those text boxes only render text set by code,
  /// not what the user typed, as in Blazor Server a render that arrives after the user has typed more would put back the older
  /// text. That means rendering alone won't put back text that was rendered before, so it is written by script as well
  /// </summary>
  private protected void WriteTextAfterRender(Func<ElementReference> element, string text) =>
    _pendingTexts.Add((element, text));

  private protected void SetFocusedPosition(int position) {
    FocusedPosition = OptionCount == 0 ? -1 : Math.Clamp(position, 0, OptionCount - 1);
    _scrollPending = true;
  }

  private protected void ClearFocusedPosition() =>
    FocusedPosition = -1;

  private protected void MoveFocus(int delta) {
    if (OptionCount == 0) {
      return;
    }
    SetFocusedPosition(FocusedPosition < 0 ? (delta > 0 ? 0 : OptionCount - 1) : FocusedPosition + delta);
  }

  /// <summary>
  /// Focuses the first item whose text starts with <paramref name="text"/>, without filtering the list
  /// </summary>
  private protected void FocusFirstMatch(string text) {
    int index = text == "" ? -1 : FindItemStartingWith(text, 0);
    if (index >= 0) {
      SetFocusedPosition(LeadingCount + index);
    } else {
      FocusedPosition = -1;
    }
  }

  /// <summary>
  /// The index of the next item whose text starts with <paramref name="text"/>, starting at <paramref name="start"/> and
  /// wrapping round, or -1
  /// </summary>
  private protected int FindItemStartingWith(string text, int start) {
    List<TItem> items = ListItems.ToList();
    for (int i = 0; i < items.Count; i++) {
      int index = ((start % items.Count) + items.Count + i) % items.Count;
      if (PixataStringFilterOperator.StartsWith.Matches(TextOf(items[index]), text)) {
        return index;
      }
    }
    return -1;
  }

  /// <summary>
  /// Type-ahead: adds the key to what has been typed in the last moment, and returns the index of the item it picks out, or -1.
  /// Pressing the same letter repeatedly moves through the items that start with it
  /// </summary>
  private protected int TypeAhead(string key, int currentIndex) {
    DateTime now = DateTime.UtcNow;
    if ((now - _typeAheadTime).TotalMilliseconds > TypeAheadResetMilliseconds) {
      _typeAhead = "";
    }
    _typeAheadTime = now;
    _typeAhead += key;
    bool sameLetter = _typeAhead.All(c => char.ToLowerInvariant(c) == char.ToLowerInvariant(_typeAhead[0]));
    return sameLetter
      ? FindItemStartingWith(_typeAhead[..1], currentIndex + 1)
      : FindItemStartingWith(_typeAhead, Math.Max(0, currentIndex));
  }

  private protected static bool IsPrintable(KeyboardEventArgs e) =>
    e.Key.Length == 1 && !e.CtrlKey && !e.AltKey && !e.MetaKey;

  /// <summary>
  /// Handles the keys that move around the open list, choose an option and close the list
  /// </summary>
  /// <returns>True if the key was handled</returns>
  private protected async Task<bool> HandleOpenListKey(KeyboardEventArgs e) {
    switch (e.Key) {
      case "ArrowUp" when e.AltKey:
        await ClosePopup();
        return true;
      case "ArrowDown" when e.AltKey:
        return true;
      case "ArrowDown":
        MoveFocus(1);
        return true;
      case "ArrowUp":
        MoveFocus(-1);
        return true;
      case "PageDown":
        MoveFocus(PageJump);
        return true;
      case "PageUp":
        MoveFocus(-PageJump);
        return true;
      case "Home":
        SetFocusedPosition(0);
        return true;
      case "End":
        SetFocusedPosition(OptionCount - 1);
        return true;
      case "Enter":
        if (FocusedPosition >= 0) {
          await ChoosePosition(FocusedPosition);
        } else {
          await EnterWithoutFocusedOption();
        }
        return true;
      case "Escape":
        await ClosePopup();
        return true;
      case "Tab":
        await ClosePopup(true, false);
        return false;
    }
    return false;
  }

  private protected async Task ChoosePosition(int position) {
    IReadOnlyList<LeadingOption> leading = LeadingOptions;
    if (position < leading.Count) {
      await ChooseLeadingOption(leading[position]);
      return;
    }
    (bool found, TItem item) = await GetItem(position - leading.Count);
    if (found) {
      await ChooseItem(item);
    }
  }

  private async Task HandleOptionClick(int position) {
    FocusedPosition = position;
    await ChoosePosition(position);
  }

  private async Task HandleOutsideClick() =>
    await ClosePopup(returnFocus: false);

  private async Task HandlePopupFilterInput(ChangeEventArgs e) =>
    await SetFilter(e.Value as string ?? "");

  private async Task HandlePopupFilterKeyDown(KeyboardEventArgs e) =>
    await HandleOpenListKey(e);

  /// <summary>
  /// Called from script when the focus moves outside the component and its popup
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task HandleFocusLeft() {
    if (_disposed) {
      return;
    }
    await ClosePopup(true, false);
    await OnFocusLost();
    await OnBlur.InvokeAsync();
    await InvokeAsync(StateHasChanged);
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      _dotNet = DotNetObjectReference.Create(this);
      await PixataDropDownJs.InvokeVoid(Js, "attachDropDown", Uid, _dotNet);
    }
    if (_pendingFocus is { } focus) {
      _pendingFocus = null;
      await PixataDropDownJs.InvokeVoid(Js, "focusElement", focus());
    }
    if (_pendingTexts.Count > 0) {
      List<(Func<ElementReference> Element, string Text)> texts = [.. _pendingTexts];
      _pendingTexts.Clear();
      foreach ((Func<ElementReference> element, string text) in texts) {
        await PixataDropDownJs.InvokeVoid(Js, "setValue", element(), text);
      }
    }
    if (_scrollPending && IsOpen && FocusedPosition >= 0) {
      _scrollPending = false;
      double top = IsVirtual ? RowOfPosition(FocusedPosition) * (double)ItemHeight : -1;
      await PixataDropDownJs.InvokeVoid(Js, "scrollToOption", _content, OptionId(FocusedPosition), top, (double)ItemHeight);
    }
  }

  /// <summary>
  /// The row an option is on, counting the leading options and group headers
  /// </summary>
  private int RowOfPosition(int position) {
    int leading = LeadingCount;
    if (position < leading) {
      return position;
    }
    int index = position - leading;
    return leading + (!RemoteVirtual && index < _itemRows.Count ? _itemRows[index] : index);
  }

  #endregion

  #region Public methods

  /// <summary>
  /// Puts the focus on the component
  /// </summary>
  public Task FocusAsync() =>
    PixataDropDownJs.InvokeVoid(Js, "focusElement", FocusTarget);

  /// <summary>
  /// Opens the popup
  /// </summary>
  public Task Open() =>
    InvokeAsync(async () => {
      if (await OpenPopup()) {
        StateHasChanged();
      }
    });

  /// <summary>
  /// Closes the popup
  /// </summary>
  public Task Close() =>
    InvokeAsync(async () => {
      await ClosePopup();
      StateHasChanged();
    });

  /// <summary>
  /// Fetches the data again (raising <see cref="OnRead"/> if you handle it) and re-renders. Use it after changing the items in
  /// <see cref="Data"/> without replacing the collection
  /// </summary>
  public Task Rebind() =>
    InvokeAsync(async () => {
      await RefreshData();
      await OnParametersApplied(true);
      StateHasChanged();
    });

  /// <summary>
  /// Re-renders the component
  /// </summary>
  public void Refresh() =>
    _ = InvokeAsync(StateHasChanged);

  #endregion

  #region Rendering helpers

  private protected string ListId =>
    $"{Uid}-listbox";

  private protected string PopupId =>
    $"{Uid}-popup";

  private protected string OptionId(int position) =>
    $"{Uid}-option-{position}";

  private protected string? ActiveDescendant =>
    IsOpen && FocusedPosition >= 0 ? OptionId(FocusedPosition) : null;

  private protected string AriaExpanded =>
    IsOpen ? "true" : "false";

  private protected string? WidthStyle =>
    string.IsNullOrWhiteSpace(Width) ? null : $"width:{Width};";

  private protected string? SizeClass =>
    $"pixata-blazor-dropdown-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}";

  /// <summary>
  /// The classes for the component's outer element
  /// </summary>
  private protected string MainClasses(params string?[] extra) =>
    PixataCss.Classes([
      PixataCss.Input(Size, Rounded, FillMode),
      "pixata-blazor-dropdown",
      $"pixata-blazor-{ComponentName}",
      PixataCss.Disabled(Enabled),
      InvalidClass,
      ReadOnly ? "pixata-blazor-dropdown-readonly" : null,
      .. extra,
      Class
    ]);

  private string? RowStyle =>
    IsVirtual ? $"height:{ItemHeight.ToString(CultureInfo.InvariantCulture)}px;" : null;

  #endregion

  private static bool IsSimple(Type type) {
    Type t = Nullable.GetUnderlyingType(type) ?? type;
    return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset)
      || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(TimeSpan) || t == typeof(Guid);
  }

  /// <summary>
  /// Orders group keys: nulls first, then by their own ordering if they have one, otherwise by their text
  /// </summary>
  private sealed class GroupKeyComparer : IComparer<object?> {
    public static readonly GroupKeyComparer Instance = new();

    public int Compare(object? x, object? y) {
      if (x is null || y is null) {
        return x is null ? (y is null ? 0 : -1) : 1;
      }
      if (x.GetType() == y.GetType() && x is IComparable comparable) {
        return comparable.CompareTo(y);
      }
      return string.Compare(GroupText(x), GroupText(y), StringComparison.CurrentCulture);
    }
  }

  public async ValueTask DisposeAsync() {
    _disposed = true;
    _debounce?.Cancel();
    _debounce?.Dispose();
    if (_dotNet is not null) {
      await PixataDropDownJs.InvokeVoid(Js, "detachDropDown", Uid);
      _dotNet.Dispose();
    }
    Dispose();
    GC.SuppressFinalize(this);
  }
}
