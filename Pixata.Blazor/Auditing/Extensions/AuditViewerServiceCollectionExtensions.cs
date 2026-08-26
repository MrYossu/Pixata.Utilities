using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Pixata.Blazor.Auditing.Services;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.Blazor.Auditing.Extensions;

public static class AuditViewerServiceCollectionExtensions {
  /// <summary>
  /// Registers the WASM <see cref="AuditViewerHttpService"/> as <see cref="AuditViewerServiceInterface"/>
  /// using a typed <see cref="HttpClient"/> pointed at <paramref name="baseUrl"/>.
  /// Call this in the WASM client project's DI setup.
  /// </summary>
  /// <remarks>
  /// The server-side equivalent, <c>AddPixataAuditViewer()</c>, lives in Pixata.AspNetCore, as it needs a
  /// DbContext and so would drag EF Core into the client.
  /// </remarks>
  public static IServiceCollection AddAuditViewerHttpService(this IServiceCollection services, string baseUrl) {
    services.AddHttpClient<AuditViewerHttpService>(client => {
      client.BaseAddress = new Uri(baseUrl);
    });
    services.AddScoped<AuditViewerServiceInterface, AuditViewerHttpService>();
    return services;
  }
}
