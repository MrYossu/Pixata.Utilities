using Microsoft.Extensions.DependencyInjection;
using Telerik.Documents.Extensibility;
using Telerik.Documents.ImageUtils;

namespace Pixata.AspNetCore.Pdf.Telerik;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers <see cref="TelerikPdfConverter"/> as the <see cref="PdfConverterInterface"/> used by <c>DocumentTemplateHelper</c>
  /// </summary>
  public static IServiceCollection AddPixataTelerikPdf(this IServiceCollection services) {
    // .NET Standard has no API for decoding images, so the PDF export needs these to include anything other than JPEGs. They are static, so
    // only set them if the app hasn't set its own
    FixedExtensibilityManager.ImagePropertiesResolver ??= new ImagePropertiesResolver();
    FixedExtensibilityManager.JpegImageConverter ??= new JpegImageConverter();
    services.AddHttpClient(TelerikPdfConverter.HttpClientName);
    services.AddSingleton<PdfConverterInterface, TelerikPdfConverter>();
    return services;
  }
}
