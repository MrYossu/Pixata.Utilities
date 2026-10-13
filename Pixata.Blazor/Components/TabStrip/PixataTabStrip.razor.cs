using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A set of tabs, each showing its own content when selected. Declare the tabs with <see cref="PixataTab"/> components
/// </summary>
public partial class PixataTabStrip {
  private const string ModulePath = "./_content/Pixata.Blazor/Components/TabStrip/PixataTabStrip.razor.js";
  private static readonly string[] NavigationKeys = ["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End"];

  /// <summary>
  /// The <see cref="PixataTab"/> components
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>
  /// The <see cref="PixataTab.Id"/> of the selected tab. Use <c>@bind-ActiveTabId</c> for two-way binding. If it is null, or
  /// doesn't match a visible tab, the first visible tab that isn't disabled is selected
  /// </summary>
  [Parameter]
  public string? ActiveTabId { get; set; }

  /// <summary>
  /// Raised when the user selects a different tab, or the selected tab is closed, hidden or removed
  /// </summary>
  [Parameter]
  public EventCallback<string> ActiveTabIdChanged { get; set; }

  /// <summary>
  /// Where the tabs go, relative to the content. <see cref="PixataTabPosition.Top"/> by default
  /// </summary>
  [Parameter]
  public PixataTabPosition TabPosition { get; set; } = PixataTabPosition.Top;

  /// <summary>
  /// How the tabs are lined up along the tab list. <see cref="PixataTabStripTabAlignment.Start"/> by default
  /// </summary>
  [Parameter]
  public PixataTabStripTabAlignment TabAlignment { get; set; } = PixataTabStripTabAlignment.Start;

  /// <summary>
  /// When false (the default), only the selected tab's content is rendered, so switching tabs creates its content afresh.
  /// When true, a tab's content is kept (hidden) once it has been shown, so anything the user typed, scroll positions and so
  /// on survive switching tabs
  /// </summary>
  [Parameter]
  public bool PersistTabContent { get; set; }

  /// <summary>
  /// When true, the tabs stay on one line (or in one column, for <see cref="PixataTabPosition.Left"/> and
  /// <see cref="PixataTabPosition.Right"/>) and scroll when they don't fit, instead of wrapping. They can be scrolled by
  /// swiping, with the scroll buttons, or with the keyboard. Recommended for anything that is used on phones
  /// </summary>
  [Parameter]
  public bool Scrollable { get; set; }

  /// <summary>
  /// Another way to make the tab strip scrollable: <see cref="PixataTabStripOverflowMode.Scroll"/> is the same as setting
  /// <see cref="Scrollable"/> to true
  /// </summary>
  [Parameter]
  public PixataTabStripOverflowMode OverflowMode { get; set; } = PixataTabStripOverflowMode.None;

  /// <summary>
  /// Where the scroll buttons go when <see cref="Scrollable"/> is true. <see cref="PixataTabStripScrollButtonsPosition.Split"/> by default
  /// </summary>
  [Parameter]
  public PixataTabStripScrollButtonsPosition ScrollButtonsPosition { get; set; } = PixataTabStripScrollButtonsPosition.Split;

  /// <summary>
  /// When the scroll buttons are shown when <see cref="Scrollable"/> is true. <see cref="PixataTabStripScrollButtonsVisibility.Auto"/> by default
  /// </summary>
  [Parameter]
  public PixataTabStripScrollButtonsVisibility ScrollButtonsVisibility { get; set; } = PixataTabStripScrollButtonsVisibility.Auto;

  /// <summary>
  /// The size of the tabs: <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// The width of the tab strip, as a CSS length (eg <c>"400px"</c>)
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The height of the tab strip, as a CSS length. The content scrolls if it doesn't fit
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Extra CSS classes for the tab strip's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Content shown after the tabs, in the tab list's row, eg a button that adds a tab
  /// </summary>
  [Parameter]
  public RenderFragment? TabStripSuffixTemplate { get; set; }

  /// <summary>
  /// Raised when the user closes a <see cref="PixataTab.Closeable"/> tab, before it is hidden. Set
  /// <see cref="PixataTabStripTabCloseEventArgs.IsCancelled"/> to keep it open. If you add tabs from a collection, remove the
  /// tab from the collection here
  /// </summary>
  [Parameter]
  public EventCallback<PixataTabStripTabCloseEventArgs> OnTabClose { get; set; }

  /// <summary>
  /// The tooltip and accessible name of the close buttons. <c>"Close"</c> by default
  /// </summary>
  [Parameter]
  public string CloseButtonLabel { get; set; } = "Close";

  /// <summary>
  /// Any other attributes are rendered on the tab strip's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  private readonly List<PixataTab> _tabs = [];
  private ElementReference _root;
  private ElementReference _list;
  private string? _activeTabId;
  private string? _previousActiveTabIdParameter;
  private bool _firstParametersSet = true;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataTabStrip>? _dotNetReference;
  private int _scrollHandle;
  private bool _attachedVertical;
  private bool _overflowing;
  private bool _canScrollBack;
  private bool _canScrollForward;
  private bool _hasRendered;
  private bool _orderMayBeWrong;
  private bool _focusActiveTab;
  private bool _scrollActiveTabIntoView;
  private bool _notifyActiveTabChanged;
  private bool _disposed;

  private bool IsScrollable =>
    Scrollable || OverflowMode == PixataTabStripOverflowMode.Scroll;

  private bool IsVertical =>
    TabPosition is PixataTabPosition.Left or PixataTabPosition.Right;

  private bool ShowScrollButtons =>
    IsScrollable && ScrollButtonsVisibility switch {
      PixataTabStripScrollButtonsVisibility.Visible => true,
      PixataTabStripScrollButtonsVisibility.Hidden => false,
      _ => _overflowing
    };

  private string RootClass =>
    PixataCss.Classes("pixata-blazor-tabstrip",
      $"pixata-blazor-tabstrip-{TabPosition.ToString().ToLowerInvariant()}",
      $"pixata-blazor-tabstrip-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}",
      IsScrollable ? "pixata-blazor-tabstrip-scrollable" : null,
      Class);

  private string? Style {
    get {
      string style = $"{(string.IsNullOrWhiteSpace(Width) ? "" : $"width:{Width};")}{(string.IsNullOrWhiteSpace(Height) ? "" : $"height:{Height};")}";
      return style == "" ? null : style;
    }
  }

  /// <summary>
  /// The tab that is showing: the one whose id is <see cref="ActiveTabId"/>, or the first visible tab that isn't disabled
  /// </summary>
  internal PixataTab? ActiveTab =>
    _tabs.FirstOrDefault(t => t.IsShown && t.EffectiveId == _activeTabId)
    ?? _tabs.FirstOrDefault(t => t.IsShown && !t.Disabled);

  protected override void OnParametersSet() {
    // The parent has rendered, and may have moved some tabs (eg by sorting the collection in a keyed @foreach). Moved tabs
    // aren't registered again, and are given their parameters in their old order, so only the page can tell us
    _orderMayBeWrong |= _hasRendered && _tabs.Count > 1;
    if (_firstParametersSet || ActiveTabId != _previousActiveTabIdParameter) {
      _firstParametersSet = false;
      _previousActiveTabIdParameter = ActiveTabId;
      if (_activeTabId != ActiveTabId) {
        _activeTabId = ActiveTabId;
        _scrollActiveTabIntoView = true;
        RedrawTabs();
      }
    }
  }

  /// <summary>
  /// Redraws the tab strip and its tabs, eg after you change something the tabs display without the tab strip's parent
  /// re-rendering
  /// </summary>
  public void Refresh() {
    RedrawTabs();
    StateHasChanged();
  }

  internal bool IsActive(PixataTab tab) =>
    ActiveTab == tab;

  internal void AddTab(PixataTab tab) {
    _tabs.Add(tab);
    // A tab added after the first render (eg by an @if) may belong between the existing ones, which the panels' order in the
    // page will tell us once it has rendered
    _orderMayBeWrong |= _hasRendered;
    StateHasChanged();
  }

  internal void RemoveTab(PixataTab tab) {
    int index = _tabs.IndexOf(tab);
    if (index < 0) {
      return;
    }
    bool wasActive = ActiveTab == tab;
    PixataTab? neighbour = wasActive ? Neighbour(tab) : null;
    _tabs.RemoveAt(index);
    if (_disposed) {
      return;
    }
    if (wasActive) {
      _activeTabId = neighbour?.EffectiveId;
      _notifyActiveTabChanged = true;
      neighbour?.Redraw();
    }
    StateHasChanged();
  }

  internal void TabChanged() {
    if (!_disposed) {
      StateHasChanged();
    }
  }

  /// <summary>
  /// Called when the active tab is hidden, or becomes a different tab (when tabs in a @foreach without a @key are removed, the
  /// tab components that are left are given the parameters of the tabs after them). Selects the tab that is now in its place,
  /// or failing that its neighbour, and tells the parent
  /// </summary>
  internal void ActiveTabChanged(PixataTab tab) {
    if (_disposed) {
      return;
    }
    PixataTab? active = tab.IsShown ? tab : Neighbour(tab);
    _activeTabId = active?.EffectiveId;
    _scrollActiveTabIntoView = true;
    _notifyActiveTabChanged = true;
    active?.Redraw();
    StateHasChanged();
  }

  private async Task Select(PixataTab tab) {
    if (!tab.Disabled) {
      await Activate(tab);
    }
  }

  private async Task Activate(PixataTab tab) {
    PixataTab? previous = ActiveTab;
    if (_activeTabId == tab.EffectiveId && previous == tab) {
      return;
    }
    _activeTabId = tab.EffectiveId;
    _scrollActiveTabIntoView = true;
    previous?.Redraw();
    tab.Redraw();
    await ActiveTabIdChanged.InvokeAsync(_activeTabId);
  }

  private async Task Close(PixataTab tab, bool fromKeyboard) {
    if (tab.Disabled || !tab.Closeable) {
      return;
    }
    bool wasActive = ActiveTab == tab;
    PixataTab? neighbour = wasActive ? Neighbour(tab) : null;
    PixataTabStripTabCloseEventArgs args = new() { TabId = tab.EffectiveId };
    await OnTabClose.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    if (!tab.IsDisposed) {
      await tab.Close();
    }
    if (wasActive) {
      if (neighbour is not null && !neighbour.IsDisposed) {
        await Activate(neighbour);
      } else {
        _activeTabId = null;
        await ActiveTabIdChanged.InvokeAsync(ActiveTab?.EffectiveId);
      }
      _focusActiveTab = fromKeyboard;
    }
    StateHasChanged();
  }

  /// <summary>
  /// The tab to select when <paramref name="tab"/> goes: the next one that can be selected, or failing that the previous one
  /// </summary>
  private PixataTab? Neighbour(PixataTab tab) {
    int index = _tabs.IndexOf(tab);
    return _tabs.Skip(index + 1).FirstOrDefault(t => t.IsShown && !t.Disabled)
           ?? _tabs.Take(Math.Max(index, 0)).LastOrDefault(t => t.IsShown && !t.Disabled);
  }

  private async Task KeyDown(KeyboardEventArgs e) {
    List<PixataTab> selectable = _tabs.Where(t => t.IsShown && !t.Disabled).ToList();
    if (selectable.Count == 0) {
      return;
    }
    PixataTab? active = ActiveTab;
    int index = active is null ? -1 : selectable.IndexOf(active);
    PixataTab? target = e.Key switch {
      "ArrowRight" or "ArrowDown" => selectable[(index + 1) % selectable.Count],
      "ArrowLeft" or "ArrowUp" => selectable[index <= 0 ? selectable.Count - 1 : index - 1],
      "Home" => selectable[0],
      "End" => selectable[^1],
      "Enter" or " " when index < 0 => selectable[0],
      _ => null
    };
    if (target is not null) {
      await Activate(target);
      _focusActiveTab = true;
      return;
    }
    if (e.Key == "Delete" && active is { Closeable: true }) {
      await Close(active, true);
    }
  }

  private async Task Scroll(int direction) {
    if (_scrollHandle != 0 && _module is not null) {
      await InvokeModuleVoid("scrollTabs", _scrollHandle, direction);
    }
  }

  /// <summary>
  /// Called by the script when the tabs start or stop overflowing, or are scrolled to either end
  /// </summary>
  [JSInvokable]
  public void OnScrollStateChanged(bool overflowing, bool canScrollBack, bool canScrollForward) {
    if (_disposed) {
      return;
    }
    _overflowing = overflowing;
    _canScrollBack = canScrollBack;
    _canScrollForward = canScrollForward;
    StateHasChanged();
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      _hasRendered = true;
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", _list, NavigationKeys);
    }
    if (_disposed) {
      return;
    }
    if (_scrollHandle != 0 && (!IsScrollable || _attachedVertical != IsVertical)) {
      // Not scrollable any more, or the tabs have moved between a row and a column, which the script measures differently
      await Detach();
    }
    if (IsScrollable && _scrollHandle == 0) {
      _dotNetReference ??= DotNetObjectReference.Create(this);
      _attachedVertical = IsVertical;
      _scrollHandle = await InvokeModule("attach", 0, _list, _dotNetReference, IsVertical);
      _scrollActiveTabIntoView = true;
    }
    if (_orderMayBeWrong) {
      _orderMayBeWrong = false;
      await PutTabsInPageOrder();
    }
    PixataTab? active = ActiveTab;
    if (_scrollActiveTabIntoView && _scrollHandle != 0 && active is not null) {
      _scrollActiveTabIntoView = false;
      await InvokeModuleVoid("scrollToTab", _scrollHandle, active.HeaderElement);
    }
    if (_focusActiveTab) {
      _focusActiveTab = false;
      if (active is not null) {
        await PixataJs.InvokeVoid(Js, "focusElement", active.HeaderElement);
      }
    }
    if (_notifyActiveTabChanged) {
      _notifyActiveTabChanged = false;
      await ActiveTabIdChanged.InvokeAsync(active?.EffectiveId);
    }
  }

  /// <summary>
  /// Sorts the tabs into the order their panels are in the page, which is the order they are declared in
  /// </summary>
  private async Task PutTabsInPageOrder() {
    string[]? order = await InvokeModule<string[]?>("tabOrder", null, _root);
    if (order is null || order.Length == 0) {
      return;
    }
    List<PixataTab> sorted = [.. _tabs.OrderBy(t => Array.IndexOf(order, t.DomId) is int i and >= 0 ? i : int.MaxValue)];
    if (!sorted.SequenceEqual(_tabs)) {
      _tabs.Clear();
      _tabs.AddRange(sorted);
      StateHasChanged();
    }
  }

  private void RedrawTabs() {
    foreach (PixataTab tab in _tabs) {
      tab.Redraw();
    }
  }

  private async Task<IJSObjectReference?> Module() {
    if (_module is null && !_disposed) {
      try {
        _module = await Js.InvokeAsync<IJSObjectReference>("import", ModulePath);
      } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
      }
    }
    return _module;
  }

  private async Task InvokeModuleVoid(string identifier, params object?[] args) {
    try {
      IJSObjectReference? module = await Module();
      if (module is not null) {
        await module.InvokeVoidAsync(identifier, args);
      }
    } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
    }
  }

  private async Task<T> InvokeModule<T>(string identifier, T fallback, params object?[] args) {
    try {
      IJSObjectReference? module = await Module();
      if (module is not null) {
        return await module.InvokeAsync<T>(identifier, args);
      }
    } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException or JSException) {
    }
    return fallback;
  }

  private async Task Detach() {
    int handle = _scrollHandle;
    _scrollHandle = 0;
    _overflowing = _canScrollBack = _canScrollForward = false;
    await InvokeModuleVoid("detach", handle);
  }

  public async ValueTask DisposeAsync() {
    _disposed = true;
    if (_scrollHandle != 0) {
      await Detach();
    }
    if (_module is not null) {
      try {
        await _module.DisposeAsync();
      } catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or InvalidOperationException) {
      }
    }
    _dotNetReference?.Dispose();
    GC.SuppressFinalize(this);
  }
}
