namespace Pixata.AspNetCore.Extensions;

public class RequestLoggingOptions {
  /// <summary>
  /// Requests whose path contains any of these strings are not logged at all
  /// </summary>
  public string[] IgnoredPaths { get; set; } = ["_framework", "_blazor", "_content", ".well-known"];

  /// <summary>
  /// Whether to log the request headers. Sensitive headers are redacted (see <see cref="RedactedHeaders"/>)
  /// </summary>
  public bool LogHeaders { get; set; } = true;

  /// <summary>
  /// Whether to log the request body. Sensitive fields are redacted (see <see cref="RedactedFields"/>)
  /// </summary>
  public bool LogBody { get; set; } = true;

  /// <summary>
  /// The names of headers whose values should be replaced with <see cref="RedactionPlaceholder"/>. Matching is case-insensitive
  /// </summary>
  public HashSet<string> RedactedHeaders { get; set; } = [.. DefaultRedactedHeaders];

  /// <summary>
  /// The names of query string parameters, form fields and JSON properties whose values should be replaced with <see cref="RedactionPlaceholder"/>. Matching is case-insensitive
  /// </summary>
  public HashSet<string> RedactedFields { get; set; } = [.. DefaultRedactedFields];

  /// <summary>
  /// Content types whose bodies can safely be written to the log. A body whose content type doesn't start with any of these is not logged. Matching is case-insensitive
  /// </summary>
  public string[] LoggedBodyContentTypes { get; set; } = ["application/json", "application/problem+json", "text/plain", "application/xml", "text/xml", "application/x-www-form-urlencoded"];

  /// <summary>
  /// The maximum number of characters of the body to log. Anything longer is truncated. Set to zero or less for no limit
  /// </summary>
  public int MaxBodyLength { get; set; } = 4096;

  /// <summary>
  /// The text that replaces the value of anything that is redacted
  /// </summary>
  public string RedactionPlaceholder { get; set; } = "(redacted)";

  /// <summary>
  /// The headers that are redacted unless you supply your own <see cref="RedactedHeaders"/>
  /// </summary>
  public static readonly string[] DefaultRedactedHeaders = [
    "Authorization",
    "Proxy-Authorization",
    "Cookie",
    "Set-Cookie",
    "X-Api-Key",
    "Api-Key",
    "X-Auth-Token",
    "X-Access-Token",
    "X-CSRF-Token",
    "X-XSRF-Token",
    "RequestVerificationToken"
  ];

  /// <summary>
  /// The query string parameters, form fields and JSON properties that are redacted unless you supply your own <see cref="RedactedFields"/>
  /// </summary>
  public static readonly string[] DefaultRedactedFields = [
    "password",
    "newPassword",
    "oldPassword",
    "currentPassword",
    "confirmPassword",
    "token",
    "accessToken",
    "access_token",
    "refreshToken",
    "refresh_token",
    "idToken",
    "id_token",
    "secret",
    "clientSecret",
    "client_secret",
    "apiKey",
    "api_key",
    "authorization",
    "creditCard",
    "cardNumber",
    "cvv",
    "cvc",
    "pin",
    "ssn"
  ];
}