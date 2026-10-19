namespace Pixata.AspNetCore.Pdf.Playwright;

/// <summary>
/// Settings for <see cref="PlaywrightPdfConverter"/>, passed to <c>AddPixataPlaywrightPdf()</c>. These apply to every PDF the converter creates.
/// Page size, orientation and margins are set per PDF with <see cref="PdfOptions"/>
/// </summary>
public class PlaywrightPdfOptions {
  /// <summary>
  /// A letterhead (and/or page numbers) shown at the top of every page. The top margin is increased by its height
  /// </summary>
  public PlaywrightPdfHeaderFooter? Header { get; set; }

  /// <summary>
  /// A letterhead (and/or page numbers) shown at the bottom of every page. The bottom margin is increased by its height
  /// </summary>
  public PlaywrightPdfHeaderFooter? Footer { get; set; }

  /// <summary>
  /// The most PDFs created at the same time. Each one uses a browser tab, so this stops a large batch from using too much memory. Others wait
  /// their turn
  /// </summary>
  public int MaxConcurrentConversions { get; set; } = 4;

  /// <summary>
  /// How long starting the browser, loading the document (including its images) or creating the PDF can take before it fails
  /// </summary>
  public int TimeoutSeconds { get; set; } = 30;

  /// <summary>
  /// When true, a page size set with CSS (<c>@page { size: ... }</c>) in the template is used instead of <see cref="PdfOptions.PaperSize"/>
  /// and <see cref="PdfOptions.Orientation"/>
  /// </summary>
  public bool PreferCssPageSize { get; set; }

  /// <summary>
  /// The full path to a Chromium executable, for when you don't want to use the one copied into the app's output. Leave this empty to use
  /// the copied one. A relative path is relative to the app's content root
  /// </summary>
  public string BrowserExecutablePath { get; set; } = "";

  /// <summary>
  /// Use a browser already installed on the server, eg <c>msedge</c> or <c>chrome</c>, instead of the one copied into the app's output.
  /// Only use this where you control the server
  /// </summary>
  public string Channel { get; set; } = "";

  /// <summary>
  /// A folder that the browser can write its temporary files to, if the server's temporary folder isn't writable by the app. A relative path
  /// is relative to the app's content root, eg <c>App_Data/PdfTemp</c>. Leave this empty to use the server's temporary folder
  /// </summary>
  public string TempFolder { get; set; } = "";

  internal void Validate() {
    if (MaxConcurrentConversions < 1) {
      throw new ArgumentException($"{nameof(MaxConcurrentConversions)} must be at least 1", nameof(MaxConcurrentConversions));
    }
    if (TimeoutSeconds < 1) {
      throw new ArgumentException($"{nameof(TimeoutSeconds)} must be at least 1", nameof(TimeoutSeconds));
    }
    if (!string.IsNullOrWhiteSpace(BrowserExecutablePath) && !string.IsNullOrWhiteSpace(Channel)) {
      throw new ArgumentException($"Set {nameof(BrowserExecutablePath)} or {nameof(Channel)}, not both");
    }
    Header?.Validate(nameof(Header));
    Footer?.Validate(nameof(Footer));
  }
}

public enum PlaywrightPdfAlignment {
  Left,
  Centre,
  Right
}

/// <summary>
/// What to show in the header or footer of every page. Give an image (the letterhead), page numbers, or both. For anything else, set
/// <see cref="Html"/>
/// </summary>
public class PlaywrightPdfHeaderFooter {
  /// <summary>
  /// The height of the header or footer, in millimetres. The page margin is increased by this, so the document's content never overlaps it
  /// </summary>
  public double HeightMm { get; set; }

  /// <summary>
  /// The path to the letterhead image (PNG, JPEG, GIF, WebP or SVG). A relative path is relative to the app's content root, eg
  /// <c>wwwroot/images/letterhead.png</c>. The image is stretched to the full width of the page, and scaled to fit the height, so for an
  /// image that goes from edge to edge, make its aspect ratio match the page width and <see cref="HeightMm"/>
  /// </summary>
  public string ImagePath { get; set; } = "";

  /// <summary>
  /// The letterhead image, if you have it in memory rather than in a file. Set <see cref="ImageContentType"/> as well
  /// </summary>
  public byte[]? Image { get; set; }

  /// <summary>
  /// The content type of <see cref="Image"/>, eg <c>image/png</c>. Worked out from the file extension when you use <see cref="ImagePath"/>
  /// </summary>
  public string ImageContentType { get; set; } = "image/png";

  /// <summary>
  /// Page numbers to show, with <c>{page}</c> and <c>{pages}</c> for the page number and the number of pages, eg <c>Page {page} of {pages}</c>.
  /// Leave this empty for no page numbers. When there is an image as well, the page numbers are shown on top of it, so leave space in the
  /// image where they go
  /// </summary>
  public string PageNumbers { get; set; } = "";

  public PlaywrightPdfAlignment PageNumberAlignment { get; set; } = PlaywrightPdfAlignment.Right;

  public double PageNumberFontSizePt { get; set; } = 9;

  /// <summary>
  /// The distance between the page numbers and the side of the page, in millimetres
  /// </summary>
  public double PageNumberMarginMm { get; set; } = 10;

  /// <summary>
  /// Your own HTML for the header or footer, used instead of <see cref="ImagePath"/>, <see cref="Image"/> and <see cref="PageNumbers"/>.
  /// Chromium renders this on its own, so it can't load images or style sheets: use inline styles, and <c>data:</c> URIs for images. Elements
  /// with the classes <c>pageNumber</c> and <c>totalPages</c> are filled in with the page number and the number of pages
  /// </summary>
  public string Html { get; set; } = "";

  internal static readonly Dictionary<string, string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase) {
    [".png"] = "image/png",
    [".jpg"] = "image/jpeg",
    [".jpeg"] = "image/jpeg",
    [".gif"] = "image/gif",
    [".webp"] = "image/webp",
    [".svg"] = "image/svg+xml"
  };

  internal void Validate(string name) {
    if (HeightMm <= 0) {
      throw new ArgumentException($"{name}.{nameof(HeightMm)} must be more than 0");
    }
    if (!string.IsNullOrWhiteSpace(ImagePath) && Image is not null) {
      throw new ArgumentException($"Set {name}.{nameof(ImagePath)} or {name}.{nameof(Image)}, not both");
    }
    if (!string.IsNullOrWhiteSpace(ImagePath) && !ImageContentTypes.ContainsKey(Path.GetExtension(ImagePath))) {
      throw new ArgumentException($"{name}.{nameof(ImagePath)} must be a PNG, JPEG, GIF, WebP or SVG file, but was {ImagePath}");
    }
    if (string.IsNullOrWhiteSpace(Html) && string.IsNullOrWhiteSpace(ImagePath) && Image is null && string.IsNullOrWhiteSpace(PageNumbers)) {
      throw new ArgumentException($"{name} has nothing to show. Set {nameof(ImagePath)}, {nameof(Image)}, {nameof(PageNumbers)} or {nameof(Html)}");
    }
  }
}
