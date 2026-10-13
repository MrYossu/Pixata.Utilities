using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base of <see cref="PixataDropDownButton"/> and <see cref="PixataSplitButton"/>: the button styling parameters, and the
/// popup menu of items with its opening, closing, keyboard handling and focus management
/// </summary>
public abstract class PixataMenuButtonBase : ComponentBase {
  // The keys the menu handles itself, so the browser mustn't also scroll the page. Space is only stopped in the menu, so that
  // it still clicks a focused button
  private static readonly string[] NavigationKeys = ["ArrowDown", "ArrowUp", "Home", "End"];
  private static readonly string[] MenuKeys = [" "];

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// When false, the button is greyed out and can't be used
  /// </summary>
  [Parameter]
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// The colour of the button: <c>base</c> (the default), <c>primary</c>, <c>secondary</c> and so on. See <see cref="PixataThemeConstants.Button.ThemeColor"/>
  /// </summary>
  [Parameter]
  public string? ThemeColor { get; set; }

  /// <summary>
  /// <c>solid</c> (the default), <c>outline</c>, <c>flat</c>, <c>link</c> or <c>clear</c>. See <see cref="PixataThemeConstants.Button.FillMode"/>
  /// </summary>
  [Parameter]
  public string? FillMode { get; set; }

  /// <summary>
  /// <c>none</c>, <c>sm</c>, <c>md</c> (the default), <c>lg</c> or <c>full</c>. See <see cref="PixataThemeConstants.Button.Rounded"/>
  /// </summary>
  [Parameter]
  public string? Rounded { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. The menu's items are sized to match. See <see cref="PixataThemeConstants.Button.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// An icon shown in the (main) button, before its content. A <see cref="PixataSvgIcon"/> (eg one from
  /// <see cref="PixataSvgIcons"/>), or the CSS class(es) of an icon font
  /// </summary>
  [Parameter]
  public object? Icon { get; set; }

  /// <summary>
  /// Extra CSS classes for the component's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// The <c>id</c> of the (main) button. A unique one is generated if you don't set it
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// The tooltip of the (main) button
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  /// <summary>
  /// The <c>aria-label</c> of the (main) button. Set it when the button only has an icon
  /// </summary>
  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public string? AriaLabelledBy { get; set; }

  [Parameter]
  public string? AriaDescribedBy { get; set; }

  /// <summary>
  /// The size and extra classes of the menu. You can also give these with a popup settings component inside the
  /// component's settings tag
  /// </summary>
  [Parameter]
  public PixataPopupSettings? PopupSettings { get; set; }

  /// <summary>
  /// Raised when the (main) button is clicked
  /// </summary>
  [Parameter]
  public EventCallback<MouseEventArgs> OnClick { get; set; }

  /// <summary>
  /// Raised before the menu opens. Set <see cref="PixataMenuButtonOpenEventArgs.IsCancelled"/> to keep it closed
  /// </summary>
  [Parameter]
  public EventCallback<PixataMenuButtonOpenEventArgs> OnOpen { get; set; }

  /// <summary>
  /// Raised before the menu closes. Set <see cref="PixataMenuButtonCloseEventArgs.IsCancelled"/> to keep it open
  /// </summary>
  [Parameter]
  public EventCallback<PixataMenuButtonCloseEventArgs> OnClose { get; set; }

  /// <summary>
  /// Any other attributes (eg <c>data-*</c>) are rendered on the component's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  /// <summary>
  /// The component's outer element, which the menu is placed against
  /// </summary>
  protected ElementReference WrapperElement;

  /// <summary>
  /// The button that <see cref="FocusAsync"/> focuses, and the focus goes back to when the menu closes (unless it was opened
  /// from another button)
  /// </summary>
  protected abstract ElementReference MainButtonElement { get; }

  /// <summary>
  /// True while the menu is open
  /// </summary>
  protected bool IsOpen { get; private set; }

  /// <summary>
  /// The id of the (main) button
  /// </summary>
  protected string ButtonId =>
    string.IsNullOrWhiteSpace(Id) ? _generatedId : Id;

  /// <summary>
  /// The id of the menu
  /// </summary>
  protected string MenuId =>
    $"{_generatedId}-menu";

  /// <summary>
  /// The popup settings given by a popup settings component in the component's settings tag
  /// </summary>
  internal PixataPopupSettings? NestedPopupSettings { get; set; }

  private readonly string _generatedId = $"pixata-blazor-menubutton-{Guid.NewGuid():N}";
  private readonly List<PixataMenuButtonItemBase> _items = [];
  private ElementReference _listElement;
  private ElementReference? _opener;
  private int _focusedIndex = -1;
  private int _nextItemId;
  private bool _justOpened;
  private bool _justClosed;
  private bool _focusFirstItem;
  private bool _focusOpener;

  /// <summary>
  /// Opens the menu, as if the user had clicked the button
  /// </summary>
  public Task OpenAsync() =>
    Open(false, null);

  /// <summary>
  /// Closes the menu
  /// </summary>
  public Task CloseAsync() =>
    Close(false);

  /// <summary>
  /// Moves the focus to the (main) button
  /// </summary>
  public Task FocusAsync() =>
    PixataJs.InvokeVoid(Js, "focusElement", MainButtonElement);

  protected override void OnParametersSet() {
    if (!Enabled && IsOpen) {
      IsOpen = false;
      _justClosed = true;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_justOpened && IsOpen) {
      _justOpened = false;
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", WrapperElement, NavigationKeys);
      await PixataJs.InvokeVoid(Js, "preventDefaultKeys", _listElement, MenuKeys);
      if (_focusFirstItem) {
        _focusFirstItem = false;
        await FocusItem(NextEnabledIndex(-1, 1));
      }
    }
    if (_justClosed && !IsOpen) {
      _justClosed = false;
      await PixataJs.InvokeVoid(Js, "releaseDefaultKeys", WrapperElement);
      if (_focusOpener) {
        _focusOpener = false;
        await PixataJs.InvokeVoid(Js, "focusElement", _opener ?? MainButtonElement);
      }
    }
  }

  /// <summary>
  /// Opens the menu if it is closed, and closes it if it is open. Opening it focuses the first item, so the user can carry on
  /// with the arrow keys
  /// </summary>
  protected Task Toggle(ElementReference opener) =>
    IsOpen ? Close(false) : Open(true, opener);

  /// <summary>
  /// Handles the keys for the buttons (pass the button as <paramref name="source"/>) and the menu (null): Alt+Down opens the
  /// menu, the arrow keys, Home and End move through the items, Enter and Space click the focused item, and Esc, Alt+Up and
  /// Tab close the menu
  /// </summary>
  protected async Task HandleKeyDown(KeyboardEventArgs args, ElementReference? source) {
    if (!Enabled) {
      return;
    }
    if (!IsOpen) {
      if (args.AltKey && args.Key == "ArrowDown") {
        await Open(true, source);
      }
      return;
    }
    switch (args.Key) {
      case "Escape":
      case "ArrowUp" when args.AltKey:
        await Close(true);
        break;
      case "ArrowDown":
        await FocusItem(NextEnabledIndex(_focusedIndex, 1));
        break;
      case "ArrowUp":
        await FocusItem(NextEnabledIndex(_focusedIndex, -1));
        break;
      case "Home":
        await FocusItem(NextEnabledIndex(-1, 1));
        break;
      case "End":
        await FocusItem(NextEnabledIndex(-1, -1));
        break;
      case "Enter" or " " when source is null && _focusedIndex >= 0 && _focusedIndex < _items.Count:
        await ActivateItem(_items[_focusedIndex], new());
        break;
      case "Tab":
        await Close(false);
        break;
    }
  }

  /// <summary>
  /// Renders the settings tag's content (eg a popup settings component), so it can pass its settings to this component
  /// </summary>
  protected RenderFragment RenderSettings(RenderFragment? settings) => builder => {
    if (settings is null) {
      return;
    }
    builder.OpenComponent<CascadingValue<PixataMenuButtonBase>>(0);
    builder.AddComponentParameter(1, nameof(CascadingValue<PixataMenuButtonBase>.Value), this);
    builder.AddComponentParameter(2, nameof(CascadingValue<PixataMenuButtonBase>.IsFixed), true);
    builder.AddComponentParameter(3, nameof(CascadingValue<PixataMenuButtonBase>.ChildContent), settings);
    builder.CloseComponent();
  };

  /// <summary>
  /// Renders the popup menu holding the items, which are only rendered while it is open
  /// </summary>
  protected RenderFragment RenderMenu(RenderFragment? items) => builder => {
    builder.OpenComponent<PixataPopup>(0);
    builder.AddComponentParameter(1, nameof(PixataPopup.Visible), IsOpen);
    builder.AddComponentParameter(2, nameof(PixataPopup.AnchorElement), WrapperElement);
    builder.AddComponentParameter(3, nameof(PixataPopup.Settings), PopupSettings ?? NestedPopupSettings);
    builder.AddComponentParameter(4, nameof(PixataPopup.Class), "pixata-blazor-button-menu-popup");
    builder.AddComponentParameter(5, nameof(PixataPopup.OnOutsideClick), EventCallback.Factory.Create(this, () => Close(false)));
    builder.AddComponentParameter(6, nameof(PixataPopup.ChildContent), (RenderFragment)(content => RenderList(content, items)));
    builder.CloseComponent();
  };

  private void RenderList(RenderTreeBuilder builder, RenderFragment? items) {
    builder.OpenElement(0, "ul");
    builder.AddAttribute(1, "id", MenuId);
    builder.AddAttribute(2, "class", PixataCss.Classes("pixata-blazor-list", "pixata-blazor-popup-content", "pixata-blazor-button-menu",
      $"pixata-blazor-button-menu-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Button.Size.Medium : Size)}"));
    builder.AddAttribute(3, "role", "menu");
    builder.AddAttribute(4, "aria-labelledby", ButtonId);
    builder.AddAttribute(5, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, args => HandleKeyDown(args, null)));
    builder.AddElementReferenceCapture(6, element => _listElement = element);
    builder.OpenComponent<CascadingValue<PixataMenuButtonBase>>(7);
    builder.AddComponentParameter(8, nameof(CascadingValue<PixataMenuButtonBase>.Value), this);
    builder.AddComponentParameter(9, nameof(CascadingValue<PixataMenuButtonBase>.IsFixed), true);
    builder.AddComponentParameter(10, nameof(CascadingValue<PixataMenuButtonBase>.ChildContent), items);
    builder.CloseComponent();
    builder.CloseElement();
  }

  /// <summary>
  /// Adds an item to the menu, returning the id for its element
  /// </summary>
  internal string Register(PixataMenuButtonItemBase item) {
    if (!_items.Contains(item)) {
      _items.Add(item);
    }
    return $"{MenuId}-{_nextItemId++}";
  }

  internal void Unregister(PixataMenuButtonItemBase item) =>
    _items.Remove(item);

  internal void ItemFocused(PixataMenuButtonItemBase item) =>
    _focusedIndex = _items.IndexOf(item);

  /// <summary>
  /// Closes the menu and raises the item's <see cref="PixataMenuButtonItemBase.OnClick"/>
  /// </summary>
  internal async Task ActivateItem(PixataMenuButtonItemBase item, MouseEventArgs args) {
    if (!Enabled || !item.Enabled) {
      return;
    }
    await Close(true);
    await item.OnClick.InvokeAsync(args);
  }

  private async Task Open(bool focusFirstItem, ElementReference? opener) {
    if (IsOpen || !Enabled) {
      return;
    }
    PixataMenuButtonOpenEventArgs args = new();
    await OnOpen.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    IsOpen = true;
    _opener = opener;
    _focusedIndex = -1;
    _justOpened = true;
    _justClosed = false;
    _focusFirstItem = focusFirstItem;
    StateHasChanged();
  }

  private async Task Close(bool focusOpener) {
    if (!IsOpen) {
      return;
    }
    PixataMenuButtonCloseEventArgs args = new();
    await OnClose.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    IsOpen = false;
    _focusedIndex = -1;
    _justClosed = true;
    _justOpened = false;
    _focusOpener = focusOpener;
    StateHasChanged();
  }

  /// <summary>
  /// The index of the next enabled item after <paramref name="from"/> in the direction given, wrapping round at the ends, or
  /// -1 if no item is enabled. A <paramref name="from"/> of -1 starts from the first item going forwards, or the last going
  /// backwards
  /// </summary>
  private int NextEnabledIndex(int from, int direction) {
    int count = _items.Count;
    int index = from;
    for (int i = 0; i < count; i++) {
      index = index < 0 ? direction > 0 ? 0 : count - 1 : (index + direction + count) % count;
      if (_items[index].Enabled) {
        return index;
      }
    }
    return -1;
  }

  private async Task FocusItem(int index) {
    if (index < 0 || index >= _items.Count) {
      return;
    }
    _focusedIndex = index;
    await PixataJs.InvokeVoid(Js, "focusSelector", _listElement, $"#{_items[index].ElementId}");
  }
}
