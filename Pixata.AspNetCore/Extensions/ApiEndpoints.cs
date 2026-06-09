using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Pixata.AspNetCore.Extensions;

public static class ApiEndpoints {
  public static void MapPixataAspNetCoreApiEndpoints(this WebApplication app, string[]? prefixesToIgnore = null) {
    string[] defaultPrefixes = ["/_blazor", "/_framework", "/_content"];

    app.Use(async (context, next) => {
      if (context.Request.Path == "/dump-routes") {
        string[] effectivePrefixes = prefixesToIgnore ?? defaultPrefixes;
        EndpointDataSource? endpointDataSource = context.RequestServices.GetRequiredService<EndpointDataSource>();
        List<string?> routes = endpointDataSource?
          .Endpoints
          .OfType<RouteEndpoint>()
          .Select(e => e.RoutePattern.RawText)
          .Where(text => !string.IsNullOrWhiteSpace(text))
          .Where(text => !IsIgnored(text!, effectivePrefixes))
          .ToList() ?? [];
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(string.Join("\n", routes));
        return;
      }
      await next();
    });
  }

  private static bool IsIgnored(string route, string[] prefixes) =>
    prefixes.Any(prefix => route.Equals(prefix, StringComparison.OrdinalIgnoreCase) || route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || route.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
}