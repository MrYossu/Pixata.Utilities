namespace Pixata.AspNetCore;

/// <summary>
/// Controls which parts of this package <see cref="ServiceCollectionExtensions.AddPixataAspNetCore{T}"/> registers, so that you don't have to
/// take dependencies you have no use for
/// </summary>
public class PixataAspNetCoreOptions {
  /// <summary>
  /// No longer used. As of v2.0.0, this package doesn't include a PDF engine. Add Pixata.AspNetCore.Pdf.Telerik (or the obsolete
  /// Pixata.AspNetCore.Pdf.WkHtmlToPdf) and call its registration method instead
  /// </summary>
  [Obsolete("This package no longer includes a PDF engine, so this does nothing. Add Pixata.AspNetCore.Pdf.Telerik and call AddPixataTelerikPdf() instead")]
  public bool RegisterPdfConverter { get; set; } = true;

  /// <summary>
  /// Whether to register <see cref="Helpers.DocumentTemplateHelper"/>, along with the <c>HtmlRenderer</c> and <c>IHttpContextAccessor</c> that it needs.
  /// The helper can render HTML without anything else. To create PDFs, it also needs a <see cref="Pdf.PdfConverterInterface"/>
  /// </summary>
  public bool RegisterDocumentTemplateHelper { get; set; } = true;

  /// <summary>
  /// The base URL passed to document templates as their <c>BaseUrl</c> parameter (eg <c>https://www.example.com</c>). When this is empty,
  /// <see cref="Helpers.DocumentTemplateHelper"/> uses the current request's scheme and host, but there isn't a request inside an interactive
  /// Blazor Server component or a background service, so set this if you generate documents from either
  /// </summary>
  public string DocumentBaseUrl { get; set; } = "";

  /// <summary>
  /// Whether to register your FluentValidation validators and the <see cref="Extensions.ValidationEndpointFilter"/>. This is only used by the
  /// generic overload of <c>AddPixataAspNetCore</c>, as the non-generic one has no way of knowing which assembly your validators are in
  /// </summary>
  public bool RegisterValidation { get; set; } = true;
}
