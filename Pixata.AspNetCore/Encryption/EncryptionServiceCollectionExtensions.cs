using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Pixata.Extensions.Encryption;

namespace Pixata.AspNetCore.Encryption;

public static class EncryptionServiceCollectionExtensions {
  public static IServiceCollection AddEncryptionServer(
    this IServiceCollection services,
    Action<EncryptionOptions> configureOptions) {
    services.Configure(configureOptions);
    services.AddSingleton<SessionKeyStore>();
    return services;
  }

  public static IServiceCollection AddEncryptionServer(
    this IServiceCollection services,
    string siteId) =>
    services.AddEncryptionServer(options => options.SiteId = siteId);

  public static IApplicationBuilder UseEncryptionMiddleware(this IApplicationBuilder app) =>
    app.UseMiddleware<EncryptionMiddleware>();

  public static IApplicationBuilder UseRclFingerprintFallback(this IApplicationBuilder app) =>
    app.Use(async (context, next) => {
      string? path = context.Request.Path.Value;
      if (path is not null && path.StartsWith("/_content/")) {
        string stripped = Regex.Replace(path, @"\.[a-z0-9]{10,12}(\.\w+)$", "$1");
        if (stripped != path) {
          context.Request.Path = stripped;
        }
      }
      await next();
    });
}
