using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pixata.Extensions;

namespace Pixata.AspNetCore.Extensions;

public class RequestLoggingMiddleware(RequestDelegate next, IOptions<RequestLoggingOptions> options, ILogger<RequestLoggingMiddleware> logger = null) {
  private readonly RequestLoggingOptions _options = options.Value;

  public async Task InvokeAsync(HttpContext context) {
    bool shouldIgnore = _options.IgnoredPaths
      .Any(i => context.Request.Path.ToString().Contains(i));

    if (!shouldIgnore && logger?.IsEnabled(LogLevel.Information) == true) {
      context.Request.EnableBuffering();

      string body = _options.LogBody
        ? await ReadBodyAsync(context.Request)
        : "(not logged)";

      logger.LogInformation($"HTTP {{Method}} {{Path}}{{QueryString}}{Environment.NewLine}Headers: {{Headers}}{Environment.NewLine}Body: {{Body}}{Environment.NewLine}", context.Request.Method, context.Request.Path, context.Request.QueryString, FormatHeaders(context.Request.Headers), body);

      context.Request.Body.Position = 0;
    }

    await next(context);
  }

  private static async Task<string> ReadBodyAsync(HttpRequest request) {
    if (request.ContentLength == 0) {
      return "(empty)";
    }
    using StreamReader reader = new(request.Body, leaveOpen: true);
    return await reader.ReadToEndAsync();
  }

  private static string FormatHeaders(IHeaderDictionary headers) =>
    headers.Select(h => $"{h.Key}={h.Value}").JoinStr(", ");
}