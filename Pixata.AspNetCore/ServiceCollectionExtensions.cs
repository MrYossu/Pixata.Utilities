using FluentValidation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Pixata.AspNetCore.Extensions;
using Pixata.AspNetCore.Helpers;

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

    services.AddSingleton(options);

    if (options.RegisterDocumentTemplateHelper) {
      // The helper needs both of these, so register them here rather than making the caller work out what's missing. It also uses a
      // PdfConverterInterface if one is registered, but doesn't need one to render HTML
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