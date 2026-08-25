using FluentValidation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Extensions;
using Pixata.AspNetCore.Helpers;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Pixata.AspNetCore;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers the services used by this package. Use the <paramref name="configure"/> parameter if you only want some of them
  /// </summary>
  /// <typeparam name="T">Any type in the assembly containing your FluentValidation validators</typeparam>
  public static IServiceCollection AddPixataAspNetCore<T>(this IServiceCollection services, Action<PixataAspNetCoreOptions>? configure = null) =>
    Register(services, configure, typeof(T));

  /// <summary>
  /// Registers the services used by this package, apart from the validation ones, which need the generic overload so that they know which
  /// assembly your validators are in
  /// </summary>
  public static IServiceCollection AddPixataAspNetCore(this IServiceCollection services, Action<PixataAspNetCoreOptions>? configure = null) =>
    Register(services, configure, null);

  private static IServiceCollection Register(IServiceCollection services, Action<PixataAspNetCoreOptions>? configure, Type? validatorAssemblyMarker) {
    PixataAspNetCoreOptions options = new();
    configure?.Invoke(options);

    if (options.RegisterPdfConverter) {
      // Registered as a factory rather than as an instance, so that the native wkhtmltopdf library isn't loaded until something actually
      // asks for the converter. Registering the instance meant that every app referencing this package paid for wkhtmltopdf at startup,
      // even if it never generated a PDF
      services.AddSingleton<IConverter>(_ => new SynchronizedConverter(new PdfTools()));
    }

    if (options.RegisterDocumentTemplateHelper) {
      if (!options.RegisterPdfConverter && services.All(s => s.ServiceType != typeof(IConverter))) {
        throw new InvalidOperationException($"{nameof(DocumentTemplateHelper)} needs an {nameof(IConverter)}, so either leave {nameof(PixataAspNetCoreOptions.RegisterPdfConverter)} set to true, register an {nameof(IConverter)} of your own before calling {nameof(AddPixataAspNetCore)}, or set {nameof(PixataAspNetCoreOptions.RegisterDocumentTemplateHelper)} to false");
      }
      // The helper needs all three of these, so register them here rather than making the caller work out what's missing
      services.AddHttpContextAccessor();
      services.AddScoped<HtmlRenderer>();
      services.AddScoped<DocumentTemplateHelper>();
    }

    if (options.RegisterValidation && validatorAssemblyMarker is not null) {
      services.AddValidatorsFromAssemblyContaining(validatorAssemblyMarker);
      services.AddTransient<ValidationEndpointFilter>();
    }

    return services;
  }
}