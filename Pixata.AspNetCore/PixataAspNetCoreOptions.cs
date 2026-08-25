namespace Pixata.AspNetCore;

/// <summary>
/// Controls which parts of this package <see cref="ServiceCollectionExtensions.AddPixataAspNetCore{T}"/> registers, so that you don't have to
/// take dependencies you have no use for
/// </summary>
public class PixataAspNetCoreOptions {
  /// <summary>
  /// Whether to register the wkhtmltopdf converter (<c>IConverter</c>) used by <see cref="Helpers.DocumentTemplateHelper"/> to generate PDFs.
  /// It is registered as a factory, so the native wkhtmltopdf library is only loaded the first time something asks for the converter,
  /// rather than while services are being registered. Set this to false if your app generates its PDFs some other way (QuestPDF, for example)
  /// </summary>
  public bool RegisterPdfConverter { get; set; } = true;

  /// <summary>
  /// Whether to register <see cref="Helpers.DocumentTemplateHelper"/>, along with the <c>HtmlRenderer</c> and <c>IHttpContextAccessor</c> that it needs.
  /// As the helper takes an <c>IConverter</c>, this needs either <see cref="RegisterPdfConverter"/> to be true, or an <c>IConverter</c>
  /// of your own to have been registered before you call <c>AddPixataAspNetCore</c>
  /// </summary>
  public bool RegisterDocumentTemplateHelper { get; set; } = true;

  /// <summary>
  /// Whether to register your FluentValidation validators and the <see cref="Extensions.ValidationEndpointFilter"/>. This is only used by the
  /// generic overload of <c>AddPixataAspNetCore</c>, as the non-generic one has no way of knowing which assembly your validators are in
  /// </summary>
  public bool RegisterValidation { get; set; } = true;
}