using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Pixata.AspNetCore.Extensions;

public static class RequestLoggingExtensions {
  public static IServiceCollection AddRequestLogging(this IServiceCollection services, Action<RequestLoggingOptions> configure = null) {
    services.AddOptions<RequestLoggingOptions>();
    if (configure is not null) {
      services.Configure(configure);
    }
    return services;
  }

  public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
    app.UseMiddleware<RequestLoggingMiddleware>();
}