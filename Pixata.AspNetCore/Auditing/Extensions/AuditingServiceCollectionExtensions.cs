using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pixata.AspNetCore.Auditing.Interceptors;
using Pixata.AspNetCore.Auditing.Services;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.AspNetCore.Auditing.Extensions;

public static class AuditingServiceCollectionExtensions {
  public static IServiceCollection AddAuditing<TContext>(this IServiceCollection services) where TContext : DbContext =>
    AddAuditing<TContext>(services, null);

  public static IServiceCollection AddAuditing<TContext>(this IServiceCollection services, Action<AuditRetentionOptions>? configureRetention) where TContext : DbContext {
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddScoped<AuditUserContextInterface, AuditUserContext>();
    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
    services.AddScoped<AuditServiceInterface, AuditService>();
    services.AddScoped<AuditingInterceptor>();

    AuditRetentionOptions retentionOptions = new();
    configureRetention?.Invoke(retentionOptions);
    services.AddSingleton(retentionOptions);

    if (retentionOptions.RetentionPeriod.HasValue) {
      services.AddHostedService<AuditRetentionService>();
    }

    return services;
  }

  /// <summary>
  /// Registers the server-side <see cref="AuditViewerService"/> as <see cref="AuditViewerServiceInterface"/>,
  /// which is what <c>MapAuditApi()</c> and the server-side audit viewer resolve.
  /// Call this in the server project's DI setup alongside <c>AddAuditing&lt;TContext&gt;()</c>.
  /// </summary>
  /// <remarks>
  /// WASM clients should call <c>AddAuditViewerHttpService()</c> from Pixata.Blazor instead, which talks to
  /// the API rather than to a DbContext.
  /// </remarks>
  public static IServiceCollection AddPixataAuditViewer(this IServiceCollection services) {
    services.AddScoped<AuditViewerServiceInterface, AuditViewerService>();
    return services;
  }
}
