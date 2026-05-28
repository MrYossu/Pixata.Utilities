using System;
using Microsoft.Extensions.DependencyInjection;
using Pixata.Blazor.Auditing.Services;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.Blazor.Auditing.Extensions;

public static class AuditViewerServiceCollectionExtensions {
  /// <summary>
  /// Registers the server-side <see cref="AuditViewerService"/> as <see cref="AuditViewerServiceInterface"/>.
  /// Call this in the server project's DI setup alongside <c>AddAuditing&lt;TContext&gt;()</c>.
  /// </summary>
  public static IServiceCollection AddPixataAuditViewer(this IServiceCollection services) {
    services.AddScoped<AuditViewerServiceInterface, AuditViewerService>();
    return services;
  }

  /// <summary>
  /// Registers the WASM <see cref="AuditViewerHttpService"/> as <see cref="AuditViewerServiceInterface"/>
  /// using a typed <see cref="HttpClient"/> pointed at <paramref name="baseUrl"/>.
  /// Call this in the WASM client project's DI setup.
  /// </summary>
  public static IServiceCollection AddAuditViewerHttpService(this IServiceCollection services, string baseUrl) {
    services.AddHttpClient<AuditViewerHttpService>(client => {
      client.BaseAddress = new Uri(baseUrl);
    });
    services.AddScoped<AuditViewerServiceInterface, AuditViewerHttpService>();
    return services;
  }
}
