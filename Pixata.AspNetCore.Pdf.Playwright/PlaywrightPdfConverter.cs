using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Pixata.AspNetCore.Pdf.Playwright;

/// <summary>
/// Converts HTML to PDF with Chromium, driven by Playwright. One browser is started the first time a PDF is created, and shared by every
/// PDF after that, with each one in its own isolated browser context. The browser is copied into the app's build and publish output by this
/// package, so nothing needs installing on the server
/// </summary>
public partial class PlaywrightPdfConverter(PlaywrightPdfOptions options, IHostEnvironment? hostEnvironment = null) : PdfConverterInterface, IAsyncDisposable, IDisposable {
  /// <summary>
  /// The folder in the app's output that the browser is copied to, by the targets in this package
  /// </summary>
  public const string BrowsersFolder = ".playwright-browsers";

  // The document is never actually fetched from here. Requests for it are answered with the HTML, so that relative URLs resolve against the
  // base URL, as they would on the site. This host is used when there is no base URL, and .invalid can never resolve to a real address
  private const string NoBaseUrl = "https://pixata-pdf.invalid/";

  // A 1x1 transparent PNG, used in place of an image that mustn't be loaded, so that it leaves a gap rather than a broken image icon
  private static readonly byte[] BlankImage = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

  private readonly SemaphoreSlim _launchLock = new(1, 1);
  private readonly SemaphoreSlim _conversionLock = new(options.MaxConcurrentConversions, options.MaxConcurrentConversions);
  private readonly string _contentRoot = hostEnvironment?.ContentRootPath ?? AppContext.BaseDirectory;
  private IPlaywright? _playwright;
  private IBrowser? _browser;
  private string? _headerImage;
  private string? _footerImage;
  private bool _disposed;

  public async Task<byte[]> Convert(string html, PdfOptions pdfOptions) {
    await _conversionLock.WaitAsync();
    try {
      IBrowser browser = await GetBrowser();
      (Uri documentUri, Uri? allowedOrigin) = DocumentUri(pdfOptions.BaseUrl);
      try {
        await using IBrowserContext context = await browser.NewContextAsync(new() {
          // Templates are rendered on the server, so they don't need scripts, and turning them off means user-supplied content can't run any
          JavaScriptEnabled = false,
          ServiceWorkers = ServiceWorkerPolicy.Block,
          AcceptDownloads = false
        });
        context.SetDefaultTimeout(options.TimeoutSeconds * 1000);
        await context.RouteAsync("**/*", route => HandleRequest(route, documentUri, allowedOrigin, html));
        IPage page = await context.NewPageAsync();
        await page.GotoAsync(documentUri.AbsoluteUri, new() { WaitUntil = WaitUntilState.Load });
        return await page.PdfAsync(await PagePdfOptions(html, pdfOptions));
      }
      catch (PlaywrightException ex) {
        throw new InvalidOperationException($"Chromium couldn't create the PDF: {ex.Message}", ex);
      }
      catch (TimeoutException ex) {
        throw new InvalidOperationException($"Chromium didn't create the PDF within {options.TimeoutSeconds} seconds. Check that the images and style sheets in the template load from {allowedOrigin?.AbsoluteUri ?? "data: URIs"}, or increase {nameof(PlaywrightPdfOptions)}.{nameof(PlaywrightPdfOptions.TimeoutSeconds)}", ex);
      }
    }
    finally {
      _conversionLock.Release();
    }
  }

  // Only loads resources from the site the template was rendered for, so that user-supplied content in the HTML (a client's name or notes on
  // an invoice, for example) can't make the server fetch arbitrary URLs, including ones on the internal network. data: URIs never get here,
  // as they aren't requests. Resources are fetched here without following redirects, so the browser makes a new request for a redirect's
  // target, which comes back through here to be checked too
  private static async Task HandleRequest(IRoute route, Uri documentUri, Uri? allowedOrigin, string html) {
    string url = route.Request.Url;
    if (url == documentUri.AbsoluteUri) {
      await route.FulfillAsync(new() { Status = 200, ContentType = "text/html; charset=utf-8", Body = html });
      return;
    }
    if (allowedOrigin is null || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https")
      || !string.Equals(uri.GetLeftPart(UriPartial.Authority), allowedOrigin.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) {
      if (route.Request.ResourceType == "image") {
        await route.FulfillAsync(new() { Status = 200, ContentType = "image/png", BodyBytes = BlankImage });
      } else {
        await route.AbortAsync("blockedbyclient");
      }
      return;
    }
    try {
      IAPIResponse response = await route.FetchAsync(new() { MaxRedirects = 0 });
      await route.FulfillAsync(new() { Response = response });
    }
    catch (PlaywrightException) {
      await route.AbortAsync("failed");
    }
  }

  private static (Uri DocumentUri, Uri? AllowedOrigin) DocumentUri(string baseUrl) {
    if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute, out Uri? baseUri) || baseUri.Scheme is not ("http" or "https")) {
      return (new(new Uri(NoBaseUrl), $"pixata-pdf-{Guid.NewGuid():N}"), null);
    }
    return (new(baseUri, $"pixata-pdf-{Guid.NewGuid():N}"), baseUri);
  }

  private async Task<PagePdfOptions> PagePdfOptions(string html, PdfOptions pdfOptions) {
    (double width, double height) = pdfOptions.PaperSizeMm();
    if (pdfOptions.Orientation == PdfOrientation.Landscape) {
      (width, height) = (height, width);
    }
    bool letterhead = (options.Header is not null || options.Footer is not null) && !LetterheadTurnedOff(html);
    double top = pdfOptions.MarginMm + (letterhead ? options.Header?.HeightMm ?? 0 : 0);
    double bottom = pdfOptions.MarginMm + (letterhead ? options.Footer?.HeightMm ?? 0 : 0);
    if (top + bottom >= height) {
      throw new InvalidOperationException($"The page is {Mm(height)} high, which doesn't leave room for the content after the margins ({Mm(pdfOptions.MarginMm)} each), header and footer");
    }
    PagePdfOptions pagePdfOptions = new() {
      Width = Mm(width),
      Height = Mm(height),
      PrintBackground = true,
      PreferCSSPageSize = options.PreferCssPageSize,
      Margin = new() { Top = Mm(top), Bottom = Mm(bottom), Left = Mm(pdfOptions.MarginMm), Right = Mm(pdfOptions.MarginMm) }
    };
    if (letterhead) {
      // Loaded once, as they're the same for every PDF. "" means there's no image
      _headerImage ??= options.Header is null ? "" : await ImageDataUri(options.Header, nameof(PlaywrightPdfOptions.Header));
      _footerImage ??= options.Footer is null ? "" : await ImageDataUri(options.Footer, nameof(PlaywrightPdfOptions.Footer));
      pagePdfOptions.DisplayHeaderFooter = true;
      pagePdfOptions.HeaderTemplate = Template(options.Header, _headerImage, top, true);
      pagePdfOptions.FooterTemplate = Template(options.Footer, _footerImage, bottom, false);
    }
    return pagePdfOptions;
  }

  private static string Mm(double mm) =>
    $"{mm.ToString(CultureInfo.InvariantCulture)}mm";

  // A template can turn the letterhead off with <meta name="pixata-pdf-letterhead" content="none" />, eg when it has one of its own
  private static bool LetterheadTurnedOff(string html) =>
    MetaTagRegex().Matches(html).Any(m => LetterheadNameRegex().IsMatch(m.Value) && NoneContentRegex().IsMatch(m.Value));

  [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase)]
  private static partial Regex MetaTagRegex();

  [GeneratedRegex(@"\bname\s*=\s*[""']?pixata-pdf-letterhead[""'\s/>]", RegexOptions.IgnoreCase)]
  private static partial Regex LetterheadNameRegex();

  [GeneratedRegex(@"\bcontent\s*=\s*[""']?none[""'\s/>]", RegexOptions.IgnoreCase)]
  private static partial Regex NoneContentRegex();

  // Chromium gives the header the whole top margin, and the footer the whole bottom one, and pads both. The padding is removed, and the
  // template is made the same height as the margin, so that the header is at the very top of the page, and the footer at the very bottom
  private const string HeaderFooterStyle = "<style>#header,#footer{padding:0 !important}</style>";

  private static string Template(PlaywrightPdfHeaderFooter? part, string image, double marginMm, bool header) {
    // Chromium shows the date and title when a template is empty, so this stops it when there's only a header or only a footer
    if (part is null) {
      return "<span></span>";
    }
    if (!string.IsNullOrWhiteSpace(part.Html)) {
      return part.Html;
    }
    string imageTag = image == "" ? "" : $"""<img src="{image}" style="position:absolute;left:0;top:0;width:100%;height:100%;object-fit:contain" />""";
    string pageNumbers = "";
    if (!string.IsNullOrWhiteSpace(part.PageNumbers)) {
      // In one span, as the spaces around the page number spans would be lost if they were flex items of their own
      string text = WebUtility.HtmlEncode(part.PageNumbers)
        .Replace("{page}", """<span class="pageNumber"></span>""")
        .Replace("{pages}", """<span class="totalPages"></span>""");
      string justify = part.PageNumberAlignment switch {
        PlaywrightPdfAlignment.Left => "flex-start",
        PlaywrightPdfAlignment.Centre => "center",
        _ => "flex-end"
      };
      pageNumbers = $"""<div style="position:absolute;top:0;bottom:0;left:{Mm(part.PageNumberMarginMm)};right:{Mm(part.PageNumberMarginMm)};display:flex;align-items:center;justify-content:{justify};font-family:sans-serif;font-size:{part.PageNumberFontSizePt.ToString(CultureInfo.InvariantCulture)}pt"><span>{text}</span></div>""";
    }
    string band = $"""<div style="position:absolute;left:0;right:0;{(header ? "top" : "bottom")}:0;height:{Mm(part.HeightMm)}">{imageTag}{pageNumbers}</div>""";
    return $"""{HeaderFooterStyle}<div style="position:relative;width:100%;height:{Mm(marginMm)};margin:0;padding:0;-webkit-print-color-adjust:exact;print-color-adjust:exact">{band}</div>""";
  }

  private async Task<string> ImageDataUri(PlaywrightPdfHeaderFooter part, string name) {
    if (part.Image is not null) {
      return $"data:{part.ImageContentType};base64,{System.Convert.ToBase64String(part.Image)}";
    }
    if (string.IsNullOrWhiteSpace(part.ImagePath)) {
      return "";
    }
    string path = Path.GetFullPath(part.ImagePath, _contentRoot);
    if (!File.Exists(path)) {
      throw new InvalidOperationException($"The {name.ToLowerInvariant()} image {path} doesn't exist. {nameof(PlaywrightPdfHeaderFooter.ImagePath)} is relative to the app's content root ({_contentRoot})");
    }
    byte[] bytes = await File.ReadAllBytesAsync(path);
    return $"data:{PlaywrightPdfHeaderFooter.ImageContentTypes[Path.GetExtension(path)]};base64,{System.Convert.ToBase64String(bytes)}";
  }

  private async Task<IBrowser> GetBrowser() {
    if (_browser is { IsConnected: true } browser) {
      return browser;
    }
    await _launchLock.WaitAsync();
    try {
      ObjectDisposedException.ThrowIf(_disposed, this);
      if (_browser is { IsConnected: true } launched) {
        return launched;
      }
      // The browser has crashed or been closed, so start a new one. If the Playwright driver went with it, start that again too
      if (_browser is not null) {
        _browser = null;
        _playwright?.Dispose();
        _playwright = null;
      }
      BrowserTypeLaunchOptions launchOptions = new() {
        Headless = true,
        Timeout = options.TimeoutSeconds * 1000
      };
      string launching;
      if (!string.IsNullOrWhiteSpace(options.Channel)) {
        launchOptions.Channel = options.Channel;
        launching = $"the {options.Channel} browser installed on the server";
      } else {
        launchOptions.ExecutablePath = BrowserExecutable();
        launching = launchOptions.ExecutablePath;
      }
      try {
        _playwright ??= await CreatePlaywright();
        _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
        return _browser;
      }
      catch (Exception ex) when (ex is PlaywrightException or TimeoutException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException) {
        _playwright?.Dispose();
        _playwright = null;
        throw new InvalidOperationException($"Couldn't start Chromium ({launching}): {ex.Message}. If this works on your machine but not on the server, check that the "
          + $"host allows the app to start processes and run .exe files from its folder, that antivirus hasn't removed or blocked files in {BrowsersFolder}, "
          + $"and that the app can write to the temporary folder (set {nameof(PlaywrightPdfOptions)}.{nameof(PlaywrightPdfOptions.TempFolder)} to use one in the app's folder)", ex);
      }
    }
    finally {
      _launchLock.Release();
    }
  }

  private async Task<IPlaywright> CreatePlaywright() {
    if (string.IsNullOrWhiteSpace(options.TempFolder)) {
      return await Microsoft.Playwright.Playwright.CreateAsync();
    }
    // The Playwright driver, and the browser it starts, use the temporary folder from the environment they're started with. There's no other
    // way to set it, so it is changed for this process while the driver starts, and then put back
    string tempFolder = Path.GetFullPath(options.TempFolder, _contentRoot);
    Directory.CreateDirectory(tempFolder);
    string? tmp = Environment.GetEnvironmentVariable("TMP");
    string? temp = Environment.GetEnvironmentVariable("TEMP");
    string? tmpDir = Environment.GetEnvironmentVariable("TMPDIR");
    try {
      Environment.SetEnvironmentVariable("TMP", tempFolder);
      Environment.SetEnvironmentVariable("TEMP", tempFolder);
      Environment.SetEnvironmentVariable("TMPDIR", tempFolder);
      return await Microsoft.Playwright.Playwright.CreateAsync();
    }
    finally {
      Environment.SetEnvironmentVariable("TMP", tmp);
      Environment.SetEnvironmentVariable("TEMP", temp);
      Environment.SetEnvironmentVariable("TMPDIR", tmpDir);
    }
  }

  private string BrowserExecutable() {
    if (!string.IsNullOrWhiteSpace(options.BrowserExecutablePath)) {
      string configured = Path.GetFullPath(options.BrowserExecutablePath, _contentRoot);
      return File.Exists(configured)
        ? configured
        : throw new InvalidOperationException($"{nameof(PlaywrightPdfOptions)}.{nameof(PlaywrightPdfOptions.BrowserExecutablePath)} is {configured}, which doesn't exist");
    }
    string browsers = Path.Combine(AppContext.BaseDirectory, BrowsersFolder);
    string revision = ExpectedRevision();
    string folder = Path.Combine(browsers, $"chromium_headless_shell-{revision}");
    string executableName = OperatingSystem.IsWindows() ? "chrome-headless-shell.exe" : "chrome-headless-shell";
    if (Directory.Exists(folder) && Directory.EnumerateFiles(folder, executableName, SearchOption.AllDirectories).FirstOrDefault() is { } executable) {
      return executable;
    }
    string[] others = Directory.Exists(browsers) ? Directory.GetDirectories(browsers).Select(Path.GetFileName).OfType<string>().ToArray() : [];
    string found = others.Length == 0 ? $"{browsers} has no browsers in it" : $"{browsers} only has {string.Join(", ", others)}, which doesn't match the version of Microsoft.Playwright the app uses";
    throw new InvalidOperationException($"Chromium headless shell revision {revision} should be in {folder}, but {found}. "
      + $"The Pixata.AspNetCore.Pdf.Playwright package copies it there when the app is built or published. Check that the {BrowsersFolder} folder was "
      + "uploaded along with the rest of the published files, and was published from a machine running the same operating system as the server");
  }

  // The Playwright driver records which browser builds it works with
  private static string ExpectedRevision() {
    string browsersJson = Path.Combine(AppContext.BaseDirectory, ".playwright", "package", "browsers.json");
    if (!File.Exists(browsersJson)) {
      throw new InvalidOperationException($"The Playwright driver isn't in the app's folder ({browsersJson} doesn't exist). It is copied there by the Microsoft.Playwright package when the app is built or published, so check that the .playwright folder was uploaded with the rest of the app");
    }
    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(browsersJson));
    foreach (JsonElement browser in document.RootElement.GetProperty("browsers").EnumerateArray()) {
      if (browser.GetProperty("name").GetString() == "chromium-headless-shell") {
        return browser.GetProperty("revision").GetString() ?? "";
      }
    }
    throw new InvalidOperationException($"{browsersJson} doesn't list a chromium-headless-shell build");
  }

  public async ValueTask DisposeAsync() {
    await _launchLock.WaitAsync();
    try {
      if (_disposed) {
        return;
      }
      _disposed = true;
      if (_browser is not null) {
        try {
          await _browser.CloseAsync();
        }
        catch (PlaywrightException) {
          // It had already crashed or been closed
        }
        _browser = null;
      }
      _playwright?.Dispose();
      _playwright = null;
    }
    finally {
      _launchLock.Release();
    }
    GC.SuppressFinalize(this);
  }

  public void Dispose() =>
    DisposeAsync().AsTask().GetAwaiter().GetResult();
}
