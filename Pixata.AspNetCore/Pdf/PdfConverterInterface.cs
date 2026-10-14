namespace Pixata.AspNetCore.Pdf;

/// <summary>
/// Converts HTML to a PDF. <see cref="Helpers.DocumentTemplateHelper"/> uses whichever implementation is registered. This package doesn't
/// include one, so that apps that don't generate PDFs don't have to deploy a PDF engine. Add Pixata.AspNetCore.Pdf.Telerik (or the obsolete
/// Pixata.AspNetCore.Pdf.WkHtmlToPdf) and call its registration method, or register an implementation of your own
/// </summary>
public interface PdfConverterInterface {
  Task<byte[]> Convert(string html, PdfOptions options);
}
