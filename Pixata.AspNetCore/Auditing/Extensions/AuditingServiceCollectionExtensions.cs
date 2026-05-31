using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pixata.AspNetCore.Auditing.Interceptors;
using Pixata.AspNetCore.Auditing.Services;

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
}
