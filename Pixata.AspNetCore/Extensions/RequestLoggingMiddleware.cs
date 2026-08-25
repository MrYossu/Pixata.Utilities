using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pixata.AspNetCore.Extensions;

public class RequestLoggingMiddleware(RequestDelegate next, IOptions<RequestLoggingOptions> options, ILogger<RequestLoggingMiddleware>? logger = null) {
  private readonly RequestLoggingOptions _options = options.Value;
  private readonly RequestLogRedactor _redactor = new(options.Value);

  public async Task InvokeAsync(HttpContext context) {
    bool shouldIgnore = _options.IgnoredPaths
      .Any(i => context.Request.Path.ToString().Contains(i));

    if (!shouldIgnore && logger?.IsEnabled(LogLevel.Information) == true) {
      string body = "(not logged)";

      if (_options.LogBody) {
        context.Request.EnableBuffering();
        body = await GetBodyAsync(context.Request);
        context.Request.Body.Position = 0;
      }

      string headers = _options.LogHeaders
        ? _redactor.Headers(context.Request.Headers)
        : "(not logged)";

      logger.LogInformation($"HTTP {{Method}} {{Path}}{{QueryString}}{Environment.NewLine}Headers: {{Headers}}{Environment.NewLine}Body: {{Body}}{Environment.NewLine}", context.Request.Method, context.Request.Path, _redactor.Query(context.Request.QueryString), headers, body);
    }

    await next(context);
  }

  private async Task<string> GetBodyAsync(HttpRequest request) {
    if (request.ContentLength == 0) {
      return "(empty)";
    }
    if (!_redactor.CanLogBody(request.ContentType)) {
      return string.IsNullOrWhiteSpace(request.ContentType)
        ? "(not logged, as the request has no content type)"
        : $"(not logged, as the content type '{request.ContentType}' isn't in LoggedBodyContentTypes)";
    }
    return _redactor.Body(await ReadBodyAsync(request), request.ContentType);
  }

  private async Task<string> ReadBodyAsync(HttpRequest request) {
    using StreamReader reader = new(request.Body, leaveOpen: true);
    if (_options.MaxBodyLength <= 0) {
      return await reader.ReadToEndAsync();
    }
    // Only read as much as we're going to log, so that a huge upload doesn't get pulled into memory just to be thrown away
    char[] buffer = new char[_options.MaxBodyLength + 1];
    int read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
    return new(buffer, 0, read);
  }
}