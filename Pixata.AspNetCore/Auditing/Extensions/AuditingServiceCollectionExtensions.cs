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
  /// or turn off the transaction used when saving added entities. Works whether the app registers its context with <c>AddDbContext()</c>
  /// or with <c>AddDbContextFactory()</c> (or a pooled factory). When there's a factory, the audit trail is read through a short-lived
  /// context of its own for each read
  /// </summary>
  public static IServiceCollection AddAuditing<TContext>(this IServiceCollection services, Action<AuditingOptions>? configure) where TContext : DbContext {
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddScoped<AuditUserContextInterface, AuditUserContext>();
    services.AddScoped<DbContext>(GetContext<TContext>);
    services.AddScoped<AuditServiceInterface>(sp => sp.GetService<IDbContextFactory<TContext>>() is { } factory
      ? new AuditService(() => factory.CreateDbContext())
      : new AuditService(sp.GetRequiredService<TContext>()));
    services.AddScoped<AuditingInterceptor>();

    AuditingOptions options = new() {
      ContextType = typeof(TContext)
    };
    configure?.Invoke(options);
    services.AddSingleton(options);
    services.AddSingleton<AuditRetentionOptions>(options);

    if (options.RetentionPeriod.HasValue) {
      services.AddHostedService<AuditRetentionService>();
    }

    return services;
  }

  // The scope's context, or a new one from the factory if the app only registers a factory (eg with AddPooledDbContextFactory). Either way,
  // the scope disposes of it
  private static DbContext GetContext<TContext>(IServiceProvider serviceProvider) where TContext : DbContext =>
    serviceProvider.GetService<TContext>()
    ?? serviceProvider.GetService<IDbContextFactory<TContext>>()?.CreateDbContext()
    ?? serviceProvider.GetRequiredService<TContext>();

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
    services.AddScoped<AuditViewerServiceInterface>(sp => sp.GetService<AuditingOptions>()?.ContextType is { } contextType
      ? new AuditViewerService(sp.GetRequiredService<AuditServiceInterface>(), contextType)
      : new AuditViewerService(sp.GetRequiredService<AuditServiceInterface>(), sp.GetRequiredService<DbContext>()));
    return services;
  }
}
