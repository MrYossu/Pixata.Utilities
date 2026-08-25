using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Pixata.Extensions;

namespace Pixata.AspNetCore.Extensions;

/// <summary>
/// Removes sensitive values (auth headers, cookies, passwords, tokens and so on) from the things that <see cref="RequestLoggingMiddleware"/> writes to the log
/// </summary>
internal sealed class RequestLogRedactor {
  private readonly RequestLoggingOptions _options;
  private readonly HashSet<string> _redactedHeaders;
  private readonly HashSet<string> _redactedFields;
  private readonly Regex? _jsonFallbackRegex;

  public RequestLogRedactor(RequestLoggingOptions options) {
    _options = options;
    // Rebuild the sets so that matching is case-insensitive, even if the caller supplied a case-sensitive set
    _redactedHeaders = new(options.RedactedHeaders, StringComparer.OrdinalIgnoreCase);
    _redactedFields = new(options.RedactedFields, StringComparer.OrdinalIgnoreCase);
    _jsonFallbackRegex = _redactedFields.Count == 0
      ? null
      : new Regex($"(\"(?:{_redactedFields.Select(Regex.Escape).JoinStr("|")})\"\\s*:\\s*)(\"(?:[^\"\\\\]|\\\\.)*\"|[^,}}\\]\\s]+)", RegexOptions.IgnoreCase);
  }

  public string Headers(IHeaderDictionary headers) =>
    headers
      .Select(h => $"{h.Key}={(_redactedHeaders.Contains(h.Key) ? _options.RedactionPlaceholder : h.Value.ToString())}")
      .JoinStr();

  public string Query(QueryString queryString) =>
    string.IsNullOrEmpty(queryString.Value) || queryString.Value == "?"
      ? queryString.Value ?? ""
      : $"?{FormEncoded(queryString.Value![1..])}";

  public string Body(string body, string? contentType) =>
    Truncate(IsJson(contentType)
      ? Json(body)
      : IsFormEncoded(contentType)
        ? FormEncoded(body)
        : body);

  public bool CanLogBody(string? contentType) {
    if (string.IsNullOrWhiteSpace(contentType)) {
      return false;
    }
    string type = contentType;
    return _options.LoggedBodyContentTypes.Any(t => type.StartsWith(t, StringComparison.OrdinalIgnoreCase));
  }

  private string Json(string body) {
    try {
      using JsonDocument doc = JsonDocument.Parse(body, new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
      using MemoryStream stream = new();
      using (Utf8JsonWriter writer = new(stream)) {
        WriteRedacted(doc.RootElement, writer);
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    } catch (JsonException) {
      // The body isn't valid JSON, which is often exactly what you're trying to debug, so log it as it came in, but with
      // anything that looks like a sensitive property redacted, as we can't rely on the parser to find them for us
      return _jsonFallbackRegex is null
        ? body
        : _jsonFallbackRegex.Replace(body, m => $"{m.Groups[1].Value}\"{_options.RedactionPlaceholder}\"");
    }
  }

  private void WriteRedacted(JsonElement element, Utf8JsonWriter writer) {
    switch (element.ValueKind) {
      case JsonValueKind.Object:
        writer.WriteStartObject();
        foreach (JsonProperty property in element.EnumerateObject()) {
          if (_redactedFields.Contains(property.Name)) {
            writer.WriteString(property.Name, _options.RedactionPlaceholder);
          } else {
            writer.WritePropertyName(property.Name);
            WriteRedacted(property.Value, writer);
          }
        }
        writer.WriteEndObject();
        break;
      case JsonValueKind.Array:
        writer.WriteStartArray();
        foreach (JsonElement item in element.EnumerateArray()) {
          WriteRedacted(item, writer);
        }
        writer.WriteEndArray();
        break;
      default:
        element.WriteTo(writer);
        break;
    }
  }

  private string FormEncoded(string value) =>
    value
      .Split('&')
      .Select(pair => {
        int index = pair.IndexOf('=');
        if (index < 0) {
          return pair;
        }
        string name = Uri.UnescapeDataString(pair[..index].Replace("+", " "));
        return _redactedFields.Contains(name)
          ? $"{pair[..index]}={_options.RedactionPlaceholder}"
          : pair;
      })
      .JoinStr("&");

  private string Truncate(string body) =>
    _options.MaxBodyLength > 0 && body.Length > _options.MaxBodyLength
      ? $"{body[.._options.MaxBodyLength]}... (truncated to {_options.MaxBodyLength} characters)"
      : body;

  private static bool IsJson(string? contentType) =>
    contentType is not null
    && (contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
      || contentType.Contains("+json", StringComparison.OrdinalIgnoreCase));

  private static bool IsFormEncoded(string? contentType) =>
    contentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;
}