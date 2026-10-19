using Microsoft.Extensions.DependencyInjection;

namespace Pixata.AspNetCore.Pdf.Playwright;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers <see cref="PlaywrightPdfConverter"/> as the <see cref="PdfConverterInterface"/> used by <c>DocumentTemplateHelper</c>. The
  /// browser isn't started until the first PDF is created
  /// </summary>
  public static IServiceCollection AddPixataPlaywrightPdf(this IServiceCollection services, Action<PlaywrightPdfOptions>? configure = null) {
    PlaywrightPdfOptions options = new();
    configure?.Invoke(options);
    options.Validate();
    services.AddSingleton(options);
    // A singleton, so that every PDF shares one browser. The container disposes it when the app stops, which closes the browser, so that
    // its files aren't left locked when the app is taken offline to deploy a new version
    services.AddSingleton<PdfConverterInterface, PlaywrightPdfConverter>();
    return services;
  }
}
