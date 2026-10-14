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

  /// <summary>
  /// Registers the auditing services. Use <paramref name="configure"/> to set a retention period, leave properties out of the audit trail,
  /// or turn off the transaction used when saving added entities
  /// </summary>
  public static IServiceCollection AddAuditing<TContext>(this IServiceCollection services, Action<AuditingOptions>? configure) where TContext : DbContext {
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddScoped<AuditUserContextInterface, AuditUserContext>();
    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
    services.AddScoped<AuditServiceInterface, AuditService>();
    services.AddScoped<AuditingInterceptor>();

    AuditingOptions options = new();
    configure?.Invoke(options);
    services.AddSingleton(options);
    services.AddSingleton<AuditRetentionOptions>(options);

    if (options.RetentionPeriod.HasValue) {
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
