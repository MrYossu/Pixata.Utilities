using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A rich text editor whose value is HTML. It has a configurable toolbar (formatting, fonts, colours, alignment, lists,
/// links, images, tables, undo and redo, and a view of the HTML), works with or without an <c>EditForm</c>, and sanitises
/// the HTML both when it is given a value and when it emits one
/// </summary>
public partial class PixataEditor : PixataInputBase<string>, IAsyncDisposable {
  internal const string ModulePath = "./_content/Pixata.Blazor/pixata-editor.js";
  internal const string CustomToolNameKey = "PixataEditorCustomToolName";

  // Until the browser reports the selection, assume an empty document: nothing to undo, outdent, unlink or do to a table
  private static readonly string[] InitiallyDisabled = [
    PixataEditorCommands.Undo, PixataEditorCommands.Redo, PixataEditorCommands.Outdent, PixataEditorCommands.Unlink,
    PixataEditorCommands.AddColumnBefore, PixataEditorCommands.AddColumnAfter, PixataEditorCommands.AddRowBefore,
    PixataEditorCommands.AddRowAfter, PixataEditorCommands.DeleteColumn, PixataEditorCommands.DeleteRow,
    PixataEditorCommands.DeleteTable, PixataEditorCommands.MergeCells, PixataEditorCommands.SplitCell
  ];

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The tools in the toolbar, in order. Use <see cref="PixataEditorToolSets.Default"/> (what you get if you leave this
  /// null) or <see cref="PixataEditorToolSets.All"/>, or build your own list. An empty list hides the toolbar
  /// </summary>
  [Parameter]
  public IEnumerable<PixataEditorTool>? Tools { get; set; }

  /// <summary>
  /// Whether the document is in an iframe (the default), isolated from the page's styles, or in a div in the page
  /// </summary>
  [Parameter]
  public PixataEditorEditMode EditMode { get; set; } = PixataEditorEditMode.Iframe;

  /// <summary>
  /// How long (in milliseconds) the editor waits after the user stops typing before updating <c>Value</c>. 100 by default.
  /// The value is also updated straight away when the editor loses the focus
  /// </summary>
  [Parameter]
  public int DebounceDelay { get; set; } = 100;

  /// <summary>
  /// When true, the content can be read, selected and copied, but not changed, and the toolbar is disabled
  /// </summary>
  [Parameter]
  public bool ReadOnly { get; set; }

  /// <summary>
  /// The height of the whole editor, toolbar included, as a CSS length. 250px by default
  /// </summary>
  [Parameter]
  public string? Height { get; set; } = "250px";

  /// <summary>
  /// The width of the editor, as a CSS length. By default it fills its container
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// Text shown in the document while it is empty
  /// </summary>
  [Parameter]
  public string? Placeholder { get; set; }

  /// <summary>
  /// When true, tools that don't fit on one line are moved into a menu at the end of the toolbar. When false (the default),
  /// the toolbar wraps onto more lines
  /// </summary>
  [Parameter]
  public bool Adaptive { get; set; }

  /// <summary>
  /// The document's <c>spellcheck</c> attribute: <c>"true"</c>, <c>"false"</c>, or null to leave it to the browser
  /// </summary>
  [Parameter]
  public string? SpellCheck { get; set; }

  /// <summary>
  /// The document's accessible name. Defaults to "Editor" unless <see cref="AriaLabelledBy"/> is set
  /// </summary>
  [Parameter]
  public string? AriaLabel { get; set; }

  /// <summary>
  /// The id(s) of the element(s) that label the document, eg a <c>&lt;label&gt;</c>
  /// </summary>
  [Parameter]
  public string? AriaLabelledBy { get; set; }

  /// <summary>
  /// The id(s) of the element(s) that describe the document, eg a hint or validation message
  /// </summary>
  [Parameter]
  public string? AriaDescribedBy { get; set; }

  /// <summary>
  /// The content of your custom tools: a <see cref="PixataEditorCustomToolTemplate"/> for each
  /// <see cref="PixataEditorCustomTool"/> in <see cref="Tools"/>, matched by name
  /// </summary>
  [Parameter]
  public RenderFragment? EditorCustomTools { get; set; }

  /// <summary>
  /// Settings for the editor, ie a <see cref="PixataEditorPasteSettings"/>
  /// </summary>
  [Parameter]
  public RenderFragment? EditorSettings { get; set; }

  /// <summary>
  /// Raised when the editor loses the focus, if the value changed while it had it. Receives the new value
  /// </summary>
  [Parameter]
  public EventCallback<string> OnChange { get; set; }

  /// <summary>
  /// Raised when the document loses the focus
  /// </summary>
  [Parameter]
  public EventCallback OnBlur { get; set; }

  private readonly string _id = $"pixata-editor-{Guid.NewGuid():N}";
  private List<PixataEditorTool>? _defaultTools;
  private List<PixataEditorTool> _tools = [];
  private ElementReference _element;
  private ElementReference _toolbar;
  private ElementReference _mount;
  private ElementReference _overflowButton;
  private ElementReference _overflowContent;
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataEditor>? _dotNetReference;
  private int _handle;
  private bool _ready;
  private bool _initialising;
  private bool _disposed;
  private PixataEditorEditMode? _createdMode;
  private string? _sanitisedFor;
  private string _sanitisedValue = "";
  private string? _syncedValue;
  private string? _pushedOptions;
  private PixataEditorPasteSettings? _pasteSettings;
  private PixataEditorSelectionState _state = new() { Disabled = [.. InitiallyDisabled] };
  private string? _valueAtFocus;
  private int _visibleToolCount = int.MaxValue;
  private bool _overflowOpen;
  private bool _focusOverflowContent;
  private bool _focusOverflowButton;
  private bool _focusEditor;

  private string ContentId => Id ?? $"{_id}-content";
  private string OverflowId => $"{_id}-overflow";
  private string ContentLabel => AriaLabel ?? "Editor";
  private string? DocumentLabel => AriaLabel ?? (AriaLabelledBy is null ? "Editor" : null);
  private bool ToolsEnabled => Enabled && !ReadOnly;
  private bool Overflowing => Adaptive && _visibleToolCount < _tools.Count;

  private string RootClass =>
    PixataCss.Classes("pixata-blazor-editor",
      EditMode == PixataEditorEditMode.Iframe ? "pixata-blazor-editor-iframe-mode" : "pixata-blazor-editor-div-mode",
      ReadOnly ? "pixata-blazor-editor-readonly" : null,
      PixataCss.Disabled(Enabled),
      InvalidClass,
      Class);

  private string? RootStyle {
    get {
      string style = $"{(string.IsNullOrWhiteSpace(Height) ? "" : $"height: {Height};")}{(string.IsNullOrWhiteSpace(Width) ? "" : $"width: {Width};")}";
      return style == "" ? null : style;
    }
  }

  protected override void OnParametersSet() {
    base.OnParametersSet();
    _tools = Tools?.ToList() ?? (_defaultTools ??= PixataEditorToolSets.Default);
    string value = Value ?? "";
    if (value != _sanitisedFor) {
      _sanitisedFor = value;
      _sanitisedValue = PixataHtmlSanitiser.Sanitise(value);
    }
    if (!Adaptive) {
      _visibleToolCount = int.MaxValue;
      _overflowOpen = false;
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_disposed || _initialising) {
      return;
    }
    if (firstRender || _createdMode != EditMode) {
      await Create();
      return;
    }
    if (_handle != 0) {
      await Sync();
    }
    await AfterRenderFocus();
  }

  // Creating and updating the editor in the browser

  private object Options() => new {
    editMode = EditMode == PixataEditorEditMode.Iframe ? "iframe" : "div",
    readOnly = ReadOnly || !Enabled,
    placeholder = Placeholder ?? "",
    debounceDelay = DebounceDelay,
    id = Id,
    ariaLabel = DocumentLabel,
    ariaLabelledBy = AriaLabelledBy,
    ariaDescribedBy = AriaDescribedBy,
    spellCheck = SpellCheck,
    invalid = IsInvalid,
    adaptive = Adaptive,
    pasteSettings = (_pasteSettings ?? new PixataEditorPasteSettings()).ToOptions()
  };

  private async Task Create() {
    _initialising = true;
    try {
      if (_handle != 0 && _module is not null) {
        int old = _handle;
        _handle = 0;
        await _module.InvokeVoidAsync("destroy", old);
      }
      _createdMode = EditMode;
      _ready = false;
      _module ??= await Js.InvokeAsync<IJSObjectReference>("import", ModulePath);
      _dotNetReference ??= DotNetObjectReference.Create(this);
      object options = Options();
      string value = _sanitisedValue;
      object? toolbar = _tools.Count > 0 ? _toolbar : null;
      _handle = await _module.InvokeAsync<int>("create", _element, _mount, toolbar, _dotNetReference, options, value);
      _syncedValue = value;
      _pushedOptions = JsonSerializer.Serialize(options);
      _ready = _handle != 0;
    }
    catch (JSDisconnectedException) {
    }
    catch (TaskCanceledException) {
    }
    catch (InvalidOperationException) {
      // JavaScript isn't available (eg while prerendering)
    }
    finally {
      _initialising = false;
    }
    if (_ready && !_disposed) {
      // Removes the prerendered content, and catches up with anything that changed while the editor was being created
      StateHasChanged();
    }
  }

  private async Task Sync() {
    object options = Options();
    string key = JsonSerializer.Serialize(options);
    if (key != _pushedOptions) {
      _pushedOptions = key;
      await InvokeVoid("update", _handle, options);
    }
    if (_sanitisedValue != _syncedValue) {
      _syncedValue = _sanitisedValue;
      await InvokeVoid("setValue", _handle, _sanitisedValue);
    }
  }

  private async Task AfterRenderFocus() {
    if (_focusOverflowContent) {
      _focusOverflowContent = false;
      await PixataJs.InvokeVoid(Js, "focusFirst", _overflowContent);
    }
    if (_focusOverflowButton) {
      _focusOverflowButton = false;
      await PixataJs.InvokeVoid(Js, "focusElement", _overflowButton);
    }
    if (_dialogOpened) {
      _dialogOpened = false;
      _dialogTrap = await Invoke("trapFocus", 0, _dialogElement);
      await PixataJs.InvokeVoid(Js, "focusSelector", _dialogElement, "input, textarea");
    }
    if (_focusEditor) {
      _focusEditor = false;
      await FocusAsync();
    }
  }

  private async Task InvokeVoid(string identifier, params object?[] args) {
    if (_module is null) {
      return;
    }
    try {
      await _module.InvokeVoidAsync(identifier, args);
    }
    catch (JSDisconnectedException) {
    }
    catch (TaskCanceledException) {
    }
  }

  private async Task<T> Invoke<T>(string identifier, T fallback, params object?[] args) {
    if (_module is null) {
      return fallback;
    }
    try {
      return await _module.InvokeAsync<T>(identifier, args) ?? fallback;
    }
    catch (JSDisconnectedException) {
    }
    catch (TaskCanceledException) {
    }
    return fallback;
  }

  // Methods

  /// <summary>
  /// Runs an editor command, as if the user had used a tool, eg
  /// <c>ExecuteAsync(new PixataEditorToolCommandArgs(PixataEditorCommands.Bold))</c> or
  /// <c>ExecuteAsync(new PixataEditorHtmlCommandArgs(PixataEditorCommands.InsertHtml, "&lt;p&gt;Hello&lt;/p&gt;"))</c>.
  /// Any HTML is sanitised before it reaches the document
  /// </summary>
  public async Task ExecuteAsync(PixataEditorToolCommandArgsBase args) {
    ArgumentNullException.ThrowIfNull(args);
    if (_module is null || _handle == 0) {
      // Before the editor has loaded in the browser, setting the HTML just sets the value
      if (args is PixataEditorHtmlCommandArgs html && args.CommandName == PixataEditorCommands.ViewHtml) {
        await ValueFromEditor(html.Value);
      }
      return;
    }
    await Invoke("execute", false, _handle, args.CommandName, args);
  }

  /// <summary>
  /// Puts the focus in the document
  /// </summary>
  public Task FocusAsync() =>
    _handle == 0 ? Task.CompletedTask : InvokeVoid("focus", _handle);

  internal async Task ExecuteTool(PixataEditorToolCommandArgsBase args, bool fromOverflow) {
    if (fromOverflow) {
      _overflowOpen = false;
    }
    await ExecuteAsync(args);
  }

  internal void SetPasteSettings(PixataEditorPasteSettings settings) =>
    _pasteSettings = settings;

  internal void RemovePasteSettings(PixataEditorPasteSettings settings) {
    if (ReferenceEquals(_pasteSettings, settings)) {
      _pasteSettings = null;
    }
  }

  // Called from the browser

  [JSInvokable]
  public Task OnEditorValueChanged(string html) =>
    _disposed ? Task.CompletedTask : ValueFromEditor(html);

  private async Task ValueFromEditor(string? html) {
    string clean = PixataHtmlSanitiser.Sanitise(html);
    _syncedValue = clean;
    _sanitisedFor = clean;
    _sanitisedValue = clean;
    await UpdateValue(clean);
  }

  [JSInvokable]
  public Task OnEditorStateChanged(string json) {
    if (!_disposed) {
      _state = PixataEditorSelectionState.Parse(json);
      StateHasChanged();
    }
    return Task.CompletedTask;
  }

  [JSInvokable]
  public Task OnEditorFocus() {
    _valueAtFocus = Value ?? "";
    return Task.CompletedTask;
  }

  [JSInvokable]
  public async Task OnEditorBlur() {
    if (_disposed) {
      return;
    }
    await OnBlur.InvokeAsync();
    string value = Value ?? "";
    if (_valueAtFocus is not null && value != _valueAtFocus) {
      _valueAtFocus = value;
      await OnChange.InvokeAsync(value);
    }
  }

  [JSInvokable]
  public Task OnToolbarOverflow(int visibleCount) {
    if (!_disposed && visibleCount != _visibleToolCount) {
      _visibleToolCount = visibleCount;
      if (_visibleToolCount >= _tools.Count) {
        _overflowOpen = false;
      }
      StateHasChanged();
    }
    return Task.CompletedTask;
  }

  // Toolbar

  internal bool IsActive(string command) =>
    _state.Active.Contains(command);

  internal bool ToolEnabled(string command, bool toolEnabled) =>
    ToolsEnabled && toolEnabled && !_state.Disabled.Contains(command);

  internal string? ToolValue(string command) =>
    command switch {
      PixataEditorCommands.Format => _state.Format,
      PixataEditorCommands.FontFamily => _state.FontFamily,
      PixataEditorCommands.FontSize => _state.FontSize,
      PixataEditorCommands.ForeColor => _state.ForeColor,
      PixataEditorCommands.BackgroundColor => _state.BackColor,
      _ => null
    };

  private static string Kebab(string name) =>
    string.Concat(name.Select((c, i) => char.IsUpper(c) ? $"{(i > 0 ? "-" : "")}{char.ToLowerInvariant(c)}" : c.ToString()));

  private async Task ClickButton(PixataEditorButtonTool button, bool overflow) {
    if (overflow) {
      _overflowOpen = false;
    }
    if (button is PixataEditorDialogTool dialogTool) {
      switch (button.CommandName) {
        case PixataEditorCommands.CreateLink:
          await OpenLinkDialog(dialogTool);
          return;
        case PixataEditorCommands.InsertImage:
          await OpenImageDialog(dialogTool);
          return;
        case PixataEditorCommands.ViewHtml:
          await OpenHtmlDialog(dialogTool);
          return;
      }
    }
    await ExecuteAsync(new PixataEditorToolCommandArgs(button.CommandName));
  }

  private void ToggleOverflow(MouseEventArgs e) {
    _overflowOpen = !_overflowOpen;
    _focusOverflowContent = _overflowOpen && e.Detail == 0;
  }

  private void CloseOverflow() =>
    _overflowOpen = false;

  private void OverflowKeyDown(KeyboardEventArgs e) {
    if (e.Key == "Escape" && _overflowOpen) {
      _overflowOpen = false;
    } else if (e.Key == "ArrowDown" && !_overflowOpen && Overflowing) {
      _overflowOpen = true;
      _focusOverflowContent = true;
    }
  }

  private void OverflowContentKeyDown(KeyboardEventArgs e) {
    if (e.Key == "Escape") {
      _overflowOpen = false;
      _focusOverflowButton = true;
    }
  }

  public async ValueTask DisposeAsync() {
    _disposed = true;
    try {
      if (_module is not null) {
        if (_dialogTrap != 0) {
          await _module.InvokeVoidAsync("releaseFocus", _dialogTrap);
        }
        if (_handle != 0) {
          await _module.InvokeVoidAsync("destroy", _handle);
        }
        await _module.DisposeAsync();
      }
    }
    catch (JSDisconnectedException) {
    }
    catch (TaskCanceledException) {
    }
    _dotNetReference?.Dispose();
    Dispose();
    GC.SuppressFinalize(this);
  }
}
