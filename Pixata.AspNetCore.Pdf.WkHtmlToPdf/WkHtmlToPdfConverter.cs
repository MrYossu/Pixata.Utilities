using System.Text.RegularExpressions;
using Pixata.AspNetCore.Pdf;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Pixata.AspNetCore.Pdf.WkHtmlToPdf;

/// <summary>
/// Converts HTML to PDF with wkhtmltopdf. This is what Pixata.AspNetCore used before v2.0.0. wkhtmltopdf was archived in January 2023, and
/// gets no more fixes, so use Pixata.AspNetCore.Pdf.Telerik instead
/// </summary>
[Obsolete("wkhtmltopdf is no longer maintained, and is unsafe with user-supplied HTML. Use Pixata.AspNetCore.Pdf.Telerik instead")]
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
