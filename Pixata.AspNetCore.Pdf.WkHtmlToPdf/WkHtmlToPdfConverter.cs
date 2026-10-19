using System.Text.RegularExpressions;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Pixata.AspNetCore.Pdf.WkHtmlToPdf;

/// <summary>
/// Converts HTML to PDF with wkhtmltopdf. 
/// </summary>
[Obsolete("wkhtmltopdf is no longer maintained, and is unsafe with user-supplied HTML. Use Pixata.AspNetCore.Pdf.Playwright or Pixata.AspNetCore.Pdf.Telerik instead")]
public class WkHtmlToPdfConverter(IConverter converter) : PdfConverterInterface {
  public Task<byte[]> Convert(string html, PdfOptions options) {
    (double width, double height) = options.PaperSizeMm();
    HtmlToPdfDocument doc = new() {
      GlobalSettings = {
        ColorMode = ColorMode.Color,
        Orientation = options.Orientation == PdfOrientation.Landscape ? Orientation.Landscape : Orientation.Portrait,
        PaperSize = new PechkinPaperSize($"{width}mm", $"{height}mm"),
        Margins = new MarginSettings(options.MarginMm, options.MarginMm, options.MarginMm, options.MarginMm) { Unit = Unit.Millimeters }
      },
      Objects = {
        new ObjectSettings {
          PagesCount = true,
          HtmlContent = FixVoidElements(html),
          WebSettings = { DefaultEncoding = "utf-8" },
        }
      }
    };
    return Task.FromResult(converter.Convert(doc));
  }

  // wkhtmltopdf's parser wants void elements closed
  private static string FixVoidElements(string html) =>
    Regex.Replace(html, @"<(area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr)(\s[^>]*)?>",
      m => m.Value.EndsWith("/>") ? m.Value : m.Value[..^1].TrimEnd() + " />",
      RegexOptions.IgnoreCase);
}