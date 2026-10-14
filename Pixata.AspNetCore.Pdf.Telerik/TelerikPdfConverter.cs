using Telerik.Documents.Flow.FormatProviders.Html;
using Telerik.Documents.Flow.Model;
using Telerik.Documents.Media;
using Telerik.Documents.Primitives;
using TelerikPdfFormatProvider = Telerik.Documents.Flow.FormatProviders.Pdf.PdfFormatProvider;

namespace Pixata.AspNetCore.Pdf.Telerik;

/// <summary>
/// Converts HTML to PDF with Telerik Document Processing. This is pure .NET, so there is no native binary to deploy, but its CSS support is
/// more limited than a browser's (no flexbox or grid), so templates should use simple, table-based layout
/// </summary>
public class TelerikPdfConverter(IHttpClientFactory httpClientFactory) : PdfConverterInterface {
  public const string HttpClientName = "Pixata.AspNetCore.Pdf.Telerik";

  // A 1x1 transparent PNG, used in place of an image that couldn't (or mustn't) be loaded, as the export fails on an image with no data
  private static readonly byte[] PlaceholderImage = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

  // Run on the thread pool, as the importer loads images and style sheets synchronously, and blocking on HTTP calls inside a Blazor
  // circuit's synchronisation context could deadlock
  public Task<byte[]> Convert(string html, PdfOptions options) =>
    Task.Run(() => ConvertSync(html, options));

  private byte[] ConvertSync(string html, PdfOptions options) {
    HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
    HtmlImportSettings importSettings = new();
    importSettings.LoadImageFromUri += (_, e) => {
      (byte[] Bytes, string Extension)? data = Download(httpClient, options.BaseUrl, e.Uri);
      e.SetImageInfo(data?.Bytes ?? PlaceholderImage, data?.Extension ?? "png");
    };
    importSettings.LoadStyleSheetFromUri += (_, e) => {
      if (Download(httpClient, options.BaseUrl, e.Uri) is { } data) {
        e.SetStyleSheetContent(System.Text.Encoding.UTF8.GetString(data.Bytes));
      }
    };
    HtmlFormatProvider htmlProvider = new() {
      ImportSettings = importSettings
    };
    RadFlowDocument document = htmlProvider.Import(html, TimeSpan.FromSeconds(30));

    (double width, double height) = options.PaperSizeMm();
    Size pageSize = options.Orientation == PdfOrientation.Landscape
      ? new(Unit.MmToDip(height), Unit.MmToDip(width))
      : new(Unit.MmToDip(width), Unit.MmToDip(height));
    double margin = Unit.MmToDip(options.MarginMm);
    foreach (Section section in document.Sections) {
      section.PageSize = pageSize;
      section.PageOrientation = options.Orientation == PdfOrientation.Landscape ? global::Telerik.Documents.Model.PageOrientation.Landscape : global::Telerik.Documents.Model.PageOrientation.Portrait;
      section.PageMargins = new Padding(margin);
    }

    return new TelerikPdfFormatProvider().Export(document, TimeSpan.FromSeconds(30));
  }

  // Only loads resources from the site the template was rendered for, so that user-supplied content in the HTML (a client's name or notes on
  // an invoice, for example) can't make the server fetch arbitrary URLs, including ones on the internal network
  private static (byte[] Bytes, string Extension)? Download(HttpClient httpClient, string baseUrl, string uri) {
    if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out Uri? baseUri)
      || !Uri.TryCreate(baseUri, uri, out Uri? resolved)
      || resolved.Scheme is not ("http" or "https")
      || !string.Equals(resolved.GetLeftPart(UriPartial.Authority), baseUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) {
      return null;
    }
    try {
      byte[] bytes = httpClient.GetByteArrayAsync(resolved).GetAwaiter().GetResult();
      return (bytes, Path.GetExtension(resolved.AbsolutePath).TrimStart('.').ToLowerInvariant());
    }
    catch (HttpRequestException) {
      return null;
    }
  }
}
