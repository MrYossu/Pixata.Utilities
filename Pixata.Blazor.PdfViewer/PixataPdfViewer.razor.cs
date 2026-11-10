using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Pixata.Blazor.Components;
using Pixata.Extensions.Pdf;

namespace Pixata.Blazor.PdfViewer;

/// <summary>
/// Shows a PDF in the page, with page navigation, zoom, rotation, text search, download and print. The PDF is given as a
/// <see cref="Url"/>, as <see cref="Data"/> or as a <see cref="Stream"/>. It's drawn by PDF.js, which is part of this package,
/// so nothing is loaded from elsewhere
/// </summary>
public partial class PixataPdfViewer {
  private static readonly string Version = typeof(PixataPdfViewer).Assembly.ManifestModule.ModuleVersionId.ToString("N");

  internal static readonly string ModulePath = $"./_content/Pixata.Blazor.PdfViewer/pdfjs/pixata-pdfviewer.js?v={Version}";

  // The scales offered in the zoom drop-down, as well as fitting the width and the page
  private static readonly decimal[] Scales = [0.5m, 0.75m, 1m, 1.25m, 1.5m, 2m, 3m, 4m];

  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The URL of the PDF. It's fetched by the browser, so it can be a page of your app (an API endpoint, say) that the user's
  /// cookies give access to. A URL on another site needs that site to allow it with CORS. Give one of <see cref="Url"/>,
  /// <see cref="Data"/> and <see cref="Stream"/>
  /// </summary>
  [Parameter]
  public string? Url { get; set; }

  /// <summary>
  /// The PDF itself. Give one of <see cref="Url"/>, <see cref="Data"/> and <see cref="Stream"/>. Setting a new array shows
  /// the new PDF
  /// </summary>
  [Parameter]
  public byte[]? Data { get; set; }

  /// <summary>
  /// A stream to read the PDF from. It's read from its current position when it's first given, and isn't disposed, so
  /// dispose of it yourself afterwards. Give one of <see cref="Url"/>, <see cref="Data"/> and <see cref="Stream"/>
  /// </summary>
  [Parameter]
  public Stream? Stream { get; set; }

  /// <summary>
  /// The name the PDF is saved as when it's downloaded. Defaults to "document.pdf"
  /// </summary>
  [Parameter]
  public string FileName { get; set; } = "document.pdf";

  /// <summary>
  /// How big the pages are shown: <see cref="PixataPdfViewerZoom.FitWidth"/> (the default),
  /// <see cref="PixataPdfViewerZoom.FitPage"/> or a scale, such as <c>1.5m</c> for 150%. Changing it changes the zoom, as
  /// do the toolbar, Ctrl and the mouse wheel, and a pinch on a touchpad
  /// </summary>
  [Parameter]
  public PixataPdfViewerZoom Zoom { get; set; } = PixataPdfViewerZoom.FitWidth;

  /// <summary>
  /// The page being shown, counting from 1. Use <c>@bind-Page</c> to know which page the user is on
  /// </summary>
  [Parameter]
  public int Page { get; set; } = 1;

  /// <summary>
  /// Fires when the page being shown changes, as the user scrolls or moves between pages
  /// </summary>
  [Parameter]
  public EventCallback<int> PageChanged { get; set; }

  /// <summary>
  /// Shows the toolbar. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowToolbar { get; set; } = true;

  /// <summary>
  /// Shows the search box on the toolbar. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowSearch { get; set; } = true;

  /// <summary>
  /// Shows the rotate buttons on the toolbar. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowRotate { get; set; } = true;

  /// <summary>
  /// Shows the download button on the toolbar. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowDownload { get; set; } = true;

  /// <summary>
  /// Shows the print button on the toolbar. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowPrint { get; set; } = true;

  /// <summary>
  /// The viewer's height, as a CSS height. By default it fills the height of its container, and is at least
  /// <c>--pixata-blazor-pdfviewer-min-height</c> tall
  /// </summary>
  [Parameter]
  public string? Height { get; set; }

  /// <summary>
  /// The viewer's width, as a CSS width. By default it fills the width of its container
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// Extra classes for the outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// The outer element's id
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// The text the viewer shows, for translating. Defaults to <see cref="PixataPdfViewerMessages.Default"/>
  /// </summary>
  [Parameter]
  public PixataPdfViewerMessages? Messages { get; set; }

  /// <summary>
  /// The mark-up shown over the pages: notes, highlights, strike-through, drawing and text. It's kept apart from the PDF, so
  /// store it yourself (<see cref="PdfMarkup.ToJson"/> makes JSON of it), and the PDF itself is never changed. Use
  /// <c>@bind-Markup</c> to have it changed as the user adds to it. To change it from code, give the viewer a new list, as
  /// changes to the list it has aren't noticed
  /// </summary>
  [Parameter]
  public IReadOnlyList<PdfMarkupItem>? Markup { get; set; }

  /// <summary>
  /// Fires with the new mark-up when the user adds, changes or deletes some. Use <c>@bind-Markup</c>
  /// </summary>
  [Parameter]
  public EventCallback<IReadOnlyList<PdfMarkupItem>> MarkupChanged { get; set; }

  /// <summary>
  /// Fires when the user adds, changes or deletes mark-up, with the item and what happened to it, for an app that stores each
  /// item on its own
  /// </summary>
  [Parameter]
  public EventCallback<PixataPdfViewerMarkupChangedEventArgs> OnMarkupChanged { get; set; }

  /// <summary>
  /// The name of the user, which is recorded with the mark-up they add. When it's set, the toolbar has the mark-up tools, and
  /// the user can change or delete their own mark-up. Other people's is shown, but can't be changed. When it isn't set, all
  /// the mark-up is shown, and none of it can be changed
  /// </summary>
  [Parameter]
  public string? MarkupAuthor { get; set; }

  /// <summary>
  /// The id of the user, such as their user id, recorded with the mark-up they add. When it's set, mark-up whose
  /// <see cref="PdfMarkupItem.AuthorId"/> is the same is the user's, rather than mark-up with the same author name, so two
  /// people with the same name can't change each other's
  /// </summary>
  [Parameter]
  public string? MarkupAuthorId { get; set; }

  /// <summary>
  /// Shows the mark-up, and the tools for adding it. Defaults to true. Printing includes the mark-up when it's shown
  /// </summary>
  [Parameter]
  public bool ShowMarkup { get; set; } = true;

  /// <summary>
  /// Fires when a PDF has been opened, with its number of pages
  /// </summary>
  [Parameter]
  public EventCallback<PixataPdfViewerLoadedEventArgs> OnLoaded { get; set; }

  /// <summary>
  /// Fires when a PDF can't be shown. The viewer shows a message saying why as well
  /// </summary>
  [Parameter]
  public EventCallback<PixataPdfViewerErrorEventArgs> OnError { get; set; }

  /// <summary>
  /// Fires when the download button is clicked, before the PDF is downloaded. Change the file name, or cancel the download
  /// </summary>
  [Parameter]
  public EventCallback<PixataPdfViewerDownloadEventArgs> OnDownload { get; set; }

  private enum ViewerStatus {
    Empty,
    Loading,
    Loaded,
    Failed
  }

  private readonly string _key = Guid.NewGuid().ToString("N")[..12];
  private IJSObjectReference? _module;
  private DotNetObjectReference<PixataPdfViewer>? _self;
  private bool _loading;
  private bool _disposed;

  // What's been asked for, and what the script has been told
  private object? _source;
  private bool _openPending;
  private PixataPdfViewerZoom? _zoomParameter;
  private int? _pageParameter;

  // What the script has told this component
  private ViewerStatus _status = ViewerStatus.Empty;
  private PixataPdfViewerErrorKind _error;
  private int _pageCount;
  private int _page = 1;
  private PixataPdfViewerZoom _zoom = PixataPdfViewerZoom.FitWidth;
  private string _query = "";
  private int _matchCurrent;
  private int _matchTotal;
  private int _pageInputVersion;
  private bool _printing;

  // The mark-up, the parameter it last came from, and what the script needs to be told
  private List<PdfMarkupItem> _markup = [];
  private IReadOnlyList<PdfMarkupItem>? _markupParameter;
  private string? _markupEditor;
  private bool _markupDirty = true;
  private bool? _markupShown;
  private PixataPdfViewerMarkupTool _tool;
  private bool _toolDirty;
  private string _highlightColour = PdfMarkup.DefaultHighlightColour;
  private string _penColour = PdfMarkup.DefaultColour;

  private PixataPdfViewerMessages Text =>
    Messages ?? PixataPdfViewerMessages.Default;

  private string ContainerId =>
    $"pixata-pdfviewer-{_key}";

  private string? Style =>
    string.Join("; ", new[] { string.IsNullOrWhiteSpace(Height) ? null : $"height: {Height}", string.IsNullOrWhiteSpace(Width) ? null : $"width: {Width}" }.OfType<string>()) is { Length: > 0 } style
      ? style
      : null;

  // The scale being shown, if it isn't one of those in the drop-down
  private decimal? CustomScale =>
    _zoom.ScaleValue is decimal scale && !Scales.Contains(scale) ? scale : null;

  private static string Percent(decimal scale) =>
    (scale * 100).ToString("0", CultureInfo.CurrentCulture) + "%";

  /// <summary>
  /// Whether a PDF is being shown
  /// </summary>
  public bool IsLoaded =>
    _status == ViewerStatus.Loaded;

  /// <summary>
  /// The number of pages in the PDF being shown, or 0 if there isn't one
  /// </summary>
  public int PageCount =>
    IsLoaded ? _pageCount : 0;

  /// <summary>
  /// The page being shown, counting from 1
  /// </summary>
  public int CurrentPage =>
    _page;

  /// <summary>
  /// The mark-up being shown, including what the user has added
  /// </summary>
  public IReadOnlyList<PdfMarkupItem> CurrentMarkup =>
    _markup;

  /// <summary>
  /// The mark-up tool chosen on the toolbar
  /// </summary>
  public PixataPdfViewerMarkupTool MarkupTool =>
    _tool;

  // Whether the user can add mark-up
  private bool CanAddMarkup =>
    ShowMarkup && !string.IsNullOrWhiteSpace(MarkupAuthor);

  // Whether the user can change or delete an item, which is when it's theirs
  private bool CanEdit(PdfMarkupItem item) =>
    CanAddMarkup && (string.IsNullOrWhiteSpace(MarkupAuthorId) ? item.Author == MarkupAuthor : item.AuthorId == MarkupAuthorId);

  // The mark-up tools on the toolbar, in order
  private IEnumerable<(PixataPdfViewerMarkupTool Tool, object Icon, string Label)> MarkupToolButtons => [
    (PixataPdfViewerMarkupTool.Note, PixataPdfViewerIcons.Note, Text.AddNote),
    (PixataPdfViewerMarkupTool.Highlight, PixataPdfViewerIcons.Highlight, Text.Highlight),
    (PixataPdfViewerMarkupTool.StrikeThrough, PixataSvgIcons.Strikethrough, Text.StrikeThrough),
    (PixataPdfViewerMarkupTool.Ink, PixataSvgIcons.Pencil, Text.Draw),
    (PixataPdfViewerMarkupTool.Text, PixataPdfViewerIcons.Text, Text.AddText)
  ];

  private string ToolColour =>
    _tool == PixataPdfViewerMarkupTool.Highlight ? _highlightColour : _penColour;

  protected override void OnParametersSet() {
    int sources = (string.IsNullOrWhiteSpace(Url) ? 0 : 1) + (Data is null ? 0 : 1) + (Stream is null ? 0 : 1);
    if (sources > 1) {
      throw new InvalidOperationException("A PixataPdfViewer needs one of Url, Data or Stream, not more than one");
    }
    object? source = string.IsNullOrWhiteSpace(Url) ? Data ?? (object?)Stream : Url;
    if (!Equals(source, _source)) {
      _source = source;
      _openPending = true;
      _status = source is null ? ViewerStatus.Empty : ViewerStatus.Loading;
      _pageCount = 0;
      _query = "";
      _matchCurrent = _matchTotal = 0;
    }
    // The zoom and page are only changed when the parameters change, so a parent that sets them without binding them doesn't
    // undo what the user has done whenever it renders
    if (_zoomParameter != Zoom) {
      bool first = _zoomParameter is null;
      _zoomParameter = Zoom;
      _zoom = Zoom;
      if (!first && IsLoaded) {
        _ = Call("setZoom", _zoom.Value);
      }
    }
    if (_pageParameter != Page) {
      bool first = _pageParameter is null;
      _pageParameter = Page;
      if (!first && IsLoaded && Page != _page) {
        _ = Call("goToPage", Page);
      }
    }
    // The mark-up is only replaced when the parameter changes, as for the page, so a parent that doesn't bind it doesn't lose
    // what the user has added. A parent that does bind it gives back the list this sent it, which needn't be sent again
    if (!ReferenceEquals(Markup, _markupParameter)) {
      _markupParameter = Markup;
      if (!ReferenceEquals(Markup, _markup)) {
        _markup = Markup is null ? [] : [.. Markup];
        _markupDirty = true;
      }
    }
    string editor = $"{(CanAddMarkup ? MarkupAuthor : null)}\n{MarkupAuthorId}";
    if (editor != _markupEditor) {
      _markupEditor = editor;
      _markupDirty = true;
    }
    if (!CanAddMarkup && _tool != PixataPdfViewerMarkupTool.None) {
      _tool = PixataPdfViewerMarkupTool.None;
      _toolDirty = true;
    }
  }

  // What the script needs to know when it creates the viewer. The page is given with each PDF
  private string Options() =>
    JsonSerializer.Serialize(new {
      zoom = _zoom.Value,
      buttonClass = PixataCss.Button(PixataThemeConstants.Button.ThemeColor.Base, PixataThemeConstants.Button.FillMode.Solid, null, PixataThemeConstants.Button.Size.Small),
      inputClass = PixataCss.Input(PixataThemeConstants.Size.Small, null, null),
      markupText = new {
        newNote = Text.NewNote,
        note = Text.Note,
        notePlaceholder = Text.NotePlaceholder,
        text = Text.Text,
        textPlaceholder = Text.TextPlaceholder,
        delete = Text.DeleteMarkup,
        close = Text.CloseMarkup
      }
    });

  // The mark-up for the script, with whether the user can change each item. Created is given as UTC, so the browser shows it
  // in the user's time zone
  private string MarkupJson() {
    JsonArray items = [];
    foreach (PdfMarkupItem item in _markup) {
      JsonObject node = JsonSerializer.SerializeToNode(item, PdfMarkup.JsonOptions)!.AsObject();
      node["created"] = DateTime.SpecifyKind(item.Created, DateTimeKind.Utc);
      node["editable"] = CanEdit(item);
      items.Add(node);
    }
    return items.ToJsonString();
  }

  // Tells the script what's changed in the mark-up
  private async Task SendMarkup() {
    if (_module is null) {
      return;
    }
    if (_markupShown != ShowMarkup) {
      _markupShown = ShowMarkup;
      await Call("showMarkup", ShowMarkup);
    }
    if (_markupDirty) {
      _markupDirty = false;
      await Call("setMarkup", MarkupJson(), CanAddMarkup);
    }
    if (_toolDirty) {
      _toolDirty = false;
      await Call("setMarkupTool", _tool == PixataPdfViewerMarkupTool.None ? "" : _tool.ToString(), ToolColour);
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (_disposed || _loading) {
      return;
    }
    try {
      if (_module is null) {
        _loading = true;
        try {
          IJSObjectReference module = await Js.InvokeAsync<IJSObjectReference>("import", ModulePath);
          _self ??= DotNetObjectReference.Create(this);
          await module.InvokeVoidAsync("create", _key, ContainerId, _self, Options());
          _module = module;
        } finally {
          _loading = false;
        }
        if (_disposed) {
          await DisposeAsync();
          return;
        }
      }
      await SendMarkup();
      if (_openPending) {
        _openPending = false;
        await Open();
      }
    } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or InvalidOperationException) {
      // The page has gone, or JavaScript isn't available yet (prerendering), in which case this is tried again after the next
      // render
    }
  }

  private async Task Open() {
    if (_module is null) {
      return;
    }
    switch (_source) {
      case string url:
        await _module.InvokeVoidAsync("openUrl", _key, url, Page);
        break;
      case byte[] data:
        using (DotNetStreamReference stream = new(new MemoryStream(data, false))) {
          await _module.InvokeVoidAsync("openData", _key, stream, Page);
        }
        break;
      case Stream stream:
        using (DotNetStreamReference reference = new(stream, leaveOpen: true)) {
          await _module.InvokeVoidAsync("openData", _key, reference, Page);
        }
        break;
      default:
        await _module.InvokeVoidAsync("clear", _key);
        break;
    }
  }

  // Calls the script, if it's loaded and the page is still there. A failure here would only mean that a toolbar button
  // didn't work, so it isn't allowed to end a Blazor Server circuit
  private async Task Call(string method, params object?[] args) {
    if (_module is null) {
      return;
    }
    try {
      await _module.InvokeVoidAsync(method, [_key, .. args]);
    } catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException) {
      // The page has gone, or the script couldn't do it
    }
  }

  // ---------- Called by the script ----------

  /// <summary>
  /// Called by the script when a PDF has been opened
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task Loaded(int pageCount) {
    _status = ViewerStatus.Loaded;
    _pageCount = pageCount;
    // The script shows the page asked for, or the last page if there aren't that many
    _page = Math.Clamp(Page, 1, Math.Max(pageCount, 1));
    StateHasChanged();
    await OnLoaded.InvokeAsync(new() { PageCount = pageCount });
    if (_page != Page) {
      await PageChanged.InvokeAsync(_page);
    }
  }

  /// <summary>
  /// Called by the script when a PDF can't be opened
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task Failed(string kind, string detail) {
    _status = ViewerStatus.Failed;
    _error = kind switch {
      "password" => PixataPdfViewerErrorKind.PasswordProtected,
      "invalid" => PixataPdfViewerErrorKind.NotAPdf,
      "missing" => PixataPdfViewerErrorKind.NotFound,
      _ => PixataPdfViewerErrorKind.Other
    };
    _pageCount = 0;
    StateHasChanged();
    await OnError.InvokeAsync(new() { Kind = _error, Detail = detail });
  }

  /// <summary>
  /// Called by the script when the page being shown changes
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task PageShown(int page) {
    if (page == _page) {
      return;
    }
    _page = page;
    StateHasChanged();
    await PageChanged.InvokeAsync(page);
  }

  /// <summary>
  /// Called by the script when the zoom changes
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void ScaleChanged(double scale, string? preset) {
    _zoom = preset is "page-width" or "page-fit" ? PixataPdfViewerZoom.FromValue(preset) : PixataPdfViewerZoom.Scale((decimal)scale);
    StateHasChanged();
  }

  /// <summary>
  /// Called by the script when the search matches change
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public void FindChanged(int current, int total) {
    _matchCurrent = current;
    _matchTotal = total;
    StateHasChanged();
  }

  /// <summary>
  /// Called by the script when the user adds mark-up. The item is recorded as the user's, added now
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task MarkupAdded(string json) {
    PdfMarkupItem? item = JsonSerializer.Deserialize<PdfMarkupItem>(json, PdfMarkup.JsonOptions);
    if (!CanAddMarkup || item is null || item.Page < 1 || (IsLoaded && item.Page > _pageCount)) {
      return;
    }
    item.Id = Guid.NewGuid().ToString("N");
    item.Author = MarkupAuthor!;
    item.AuthorId = string.IsNullOrWhiteSpace(MarkupAuthorId) ? null : MarkupAuthorId;
    item.Created = DateTime.UtcNow;
    item.Modified = null;
    await MarkupChange([.. _markup, item], item, PixataPdfViewerMarkupChange.Added);
  }

  /// <summary>
  /// Called by the script when the user changes the text of a note, or of text they've written on the page
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task MarkupTextChanged(string id, string text) {
    int index = _markup.FindIndex(item => item.Id == id);
    if (index < 0 || !CanEdit(_markup[index])) {
      return;
    }
    PdfMarkupItem changed = _markup[index].Clone();
    changed.Text = text;
    changed.Modified = DateTime.UtcNow;
    List<PdfMarkupItem> markup = [.. _markup];
    markup[index] = changed;
    await MarkupChange(markup, changed, PixataPdfViewerMarkupChange.Changed);
  }

  /// <summary>
  /// Called by the script when the user deletes mark-up
  /// </summary>
  [JSInvokable]
  [EditorBrowsable(EditorBrowsableState.Never)]
  public async Task MarkupDeleted(string id) {
    PdfMarkupItem? item = _markup.FirstOrDefault(i => i.Id == id);
    if (item is null || !CanEdit(item)) {
      return;
    }
    await MarkupChange([.. _markup.Where(i => i != item)], item, PixataPdfViewerMarkupChange.Deleted);
  }

  private async Task MarkupChange(List<PdfMarkupItem> markup, PdfMarkupItem item, PixataPdfViewerMarkupChange change) {
    _markup = markup;
    _markupDirty = true;
    await SendMarkup();
    await MarkupChanged.InvokeAsync(markup);
    await OnMarkupChanged.InvokeAsync(new() { Item = item, Change = change, Markup = markup });
  }

  // ---------- The toolbar, and the same things from code ----------

  /// <summary>
  /// Shows a page, counting from 1
  /// </summary>
  public Task GoToPage(int page) =>
    IsLoaded ? Call("goToPage", Math.Clamp(page, 1, _pageCount)) : Task.CompletedTask;

  private async Task PageEntered(ChangeEventArgs e) {
    if (int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int page) && Math.Clamp(page, 1, _pageCount) != _page) {
      await GoToPage(page);
    } else {
      // Puts the current page number back in the box
      _pageInputVersion++;
    }
  }

  /// <summary>
  /// Sets the zoom, as the <see cref="Zoom"/> parameter does
  /// </summary>
  public Task SetZoom(PixataPdfViewerZoom zoom) {
    _zoom = zoom;
    return Call("setZoom", zoom.Value);
  }

  private Task ZoomChosen(ChangeEventArgs e) =>
    e.Value?.ToString() is { Length: > 0 } value ? SetZoom(PixataPdfViewerZoom.FromValue(value)) : Task.CompletedTask;

  private Task ZoomBy(int steps) =>
    Call("zoomBy", steps);

  /// <summary>
  /// Turns the pages a quarter turn, clockwise or anticlockwise. This only changes how they're shown, not the PDF
  /// </summary>
  public Task Rotate(bool clockwise = true) =>
    Call("rotate", clockwise ? 1 : -1);

  /// <summary>
  /// Searches the PDF for some text, highlighting every match and showing the first. An empty string clears the search
  /// </summary>
  public Task Search(string text) {
    _query = text;
    if (text == "") {
      _matchCurrent = _matchTotal = 0;
    }
    return Call("find", text, false, false);
  }

  /// <summary>
  /// Shows the next match of the search
  /// </summary>
  public Task SearchNext() =>
    _query == "" ? Task.CompletedTask : Call("find", _query, true, false);

  /// <summary>
  /// Shows the previous match of the search
  /// </summary>
  public Task SearchPrevious() =>
    _query == "" ? Task.CompletedTask : Call("find", _query, true, true);

  private Task SearchTyped(ChangeEventArgs e) =>
    Search(e.Value?.ToString() ?? "");

  private Task SearchKey(KeyboardEventArgs e) =>
    e.Key switch {
      "Enter" when e.ShiftKey => SearchPrevious(),
      "Enter" => SearchNext(),
      _ => Task.CompletedTask
    };

  /// <summary>
  /// Chooses a mark-up tool, as the toolbar does. It's ignored unless <see cref="MarkupAuthor"/> is set and the mark-up is
  /// shown
  /// </summary>
  public async Task SetMarkupTool(PixataPdfViewerMarkupTool tool) {
    if (!CanAddMarkup && tool != PixataPdfViewerMarkupTool.None) {
      return;
    }
    _tool = tool;
    _toolDirty = true;
    await SendMarkup();
    StateHasChanged();
  }

  // Clicking the tool that's chosen puts it down
  private Task ChooseTool(PixataPdfViewerMarkupTool tool) =>
    SetMarkupTool(_tool == tool ? PixataPdfViewerMarkupTool.None : tool);

  private Task ColourChosen(ChangeEventArgs e) {
    if (e.Value?.ToString() is not { Length: > 0 } colour) {
      return Task.CompletedTask;
    }
    if (_tool == PixataPdfViewerMarkupTool.Highlight) {
      _highlightColour = colour;
    } else {
      _penColour = colour;
    }
    _toolDirty = true;
    return SendMarkup();
  }

  // Escape puts the mark-up tool down
  private Task ContainerKey(KeyboardEventArgs e) =>
    e.Key == "Escape" && _tool != PixataPdfViewerMarkupTool.None ? SetMarkupTool(PixataPdfViewerMarkupTool.None) : Task.CompletedTask;

  /// <summary>
  /// Downloads the PDF, as the toolbar's download button does, raising <see cref="OnDownload"/> first
  /// </summary>
  public async Task Download() {
    if (!IsLoaded) {
      return;
    }
    PixataPdfViewerDownloadEventArgs args = new() { FileName = string.IsNullOrWhiteSpace(FileName) ? "document.pdf" : FileName };
    await OnDownload.InvokeAsync(args);
    if (!args.IsCancelled) {
      await Call("download", args.FileName);
    }
  }

  /// <summary>
  /// Prints the PDF, as the toolbar's print button does
  /// </summary>
  public async Task Print() {
    if (!IsLoaded || _printing) {
      return;
    }
    _printing = true;
    try {
      await Call("print");
    } finally {
      _printing = false;
    }
  }

  public async ValueTask DisposeAsync() {
    _disposed = true;
    if (_module is not null) {
      try {
        await _module.InvokeVoidAsync("destroy", _key);
        await _module.DisposeAsync();
      } catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
        // The page has gone, and the script with it
      }
      _module = null;
    }
    _self?.Dispose();
    _self = null;
    GC.SuppressFinalize(this);
  }
}
