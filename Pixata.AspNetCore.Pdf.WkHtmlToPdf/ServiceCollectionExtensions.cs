using Microsoft.Extensions.DependencyInjection;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Pixata.AspNetCore.Pdf.WkHtmlToPdf;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers <see cref="WkHtmlToPdfConverter"/> as the <see cref="PdfConverterInterface"/> used by <c>DocumentTemplateHelper</c>
  /// </summary>
  [Obsolete("wkhtmltopdf is no longer maintained, and is unsafe with user-supplied HTML. Use Pixata.AspNetCore.Pdf.Telerik instead")]
  public static IServiceCollection AddPixataWkHtmlToPdf(this IServiceCollection services) {
    // Registered as a factory rather than as an instance, so that the native wkhtmltopdf library isn't loaded until something actually
    // asks for the converter
    services.AddSingleton<IConverter>(_ => new SynchronizedConverter(new PdfTools()));
    services.AddSingleton<PdfConverterInterface, WkHtmlToPdfConverter>();
    return services;
  }
}
