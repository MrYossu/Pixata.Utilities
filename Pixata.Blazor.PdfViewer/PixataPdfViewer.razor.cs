using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

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
  }

  // What the script needs to know when it creates the viewer. The page is given with each PDF
  private string Options() =>
    System.Text.Json.JsonSerializer.Serialize(new { zoom = _zoom.Value });

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
