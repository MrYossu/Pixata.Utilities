using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pixata.AspNetCore.Auditing.Interceptors;
using Pixata.AspNetCore.Auditing.Services;

namespace Pixata.AspNetCore.Auditing.Extensions;

public static class AuditingServiceCollectionExtensions {
  public static IServiceCollection AddAuditing(this IServiceCollection services) {
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddScoped<AuditUserContextInterface, AuditUserContext>();
    services.AddScoped<AuditServiceInterface, AuditService>();
    services.AddScoped<AuditingInterceptor>();
    return services;
  }
}
