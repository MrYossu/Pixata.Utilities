using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Pixata.Extensions;

namespace Pixata.Email;

/// <summary>
/// What <see cref="PixataGraphEmailService"/> and <see cref="PixataGraphMailReader"/> have in common: the mailbox's address, sending a request
/// with the token, and turning Graph's errors into an <see cref="ApiResponse{T}"/>
/// </summary>
internal static class GraphRequests {
  // The mailbox's address, eg https://graph.microsoft.com/v1.0/users/accounts%40spriggs.com/, which the paths are relative to
  internal static string MailboxUrl(GraphEmailSettings settings) {
    string mailbox = string.IsNullOrWhiteSpace(settings.UserId) ? "me" : $"users/{Uri.EscapeDataString(settings.UserId)}";
    string baseUrl = settings.BaseUrl.EndsWith('/') ? settings.BaseUrl : $"{settings.BaseUrl}/";
    return $"{baseUrl}{mailbox}/";
  }

  internal static ApiResponse<T>? NoToken<T>(GraphEmailSettings settings) =>
    string.IsNullOrWhiteSpace(settings.AccessToken)
      ? new(ApiResponseStates.Failure, Message: $"{nameof(GraphEmailSettings)}.{nameof(GraphEmailSettings.AccessToken)} hasn't been set")
      : null;

  /// <summary>
  /// Sends a request to Graph with the token, and returns the JSON it sends back (null if there isn't any)
  /// </summary>
  /// <param name="url">The full address, or one relative to the mailbox, such as <c>messages/{id}/send</c></param>
  /// <param name="notFound">Whether a 404 gives <see cref="ApiResponseStates.NotFound"/>, as it does when reading a message that has gone. It's a
  /// <see cref="ApiResponseStates.Failure"/> otherwise, as it was for sending before the reader was added</param>
  internal static async Task<ApiResponse<JsonNode?>> Send(HttpClient httpClient, GraphEmailSettings settings, HttpMethod method, string url, JsonNode? body = null,
                                                          bool notFound = false, CancellationToken cancellationToken = default,
                                                          Action<HttpRequestMessage>? configure = null) {
    using HttpRequestMessage request = Request(settings, method, url);
    if (body is not null) {
      request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
    } else if (method == HttpMethod.Post) {
      // A POST without a body still needs a Content-Length of 0, eg to send a draft
      request.Content = new ByteArrayContent([]);
    }
    configure?.Invoke(request);
    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode) {
      return new(State(response.StatusCode, notFound), Message: await ErrorMessage(response));
    }
    string json = await response.Content.ReadAsStringAsync(cancellationToken);
    return new(ApiResponseStates.Success, string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json));
  }

  internal static HttpRequestMessage Request(GraphEmailSettings settings, HttpMethod method, string url) {
    HttpRequestMessage request = new(method, Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url : $"{MailboxUrl(settings)}{url}");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
    return request;
  }

  internal static ApiResponse<T> Failed<T>(ApiResponse<JsonNode?> response) =>
    new(response.State, Message: response.Message);

  // What an exception while talking to Graph means
  internal static ApiResponse<T> Caught<T>(Exception ex) =>
    ex is HttpRequestException or TaskCanceledException
      // TaskCanceledException is what HttpClient throws when it times out
      ? new(ApiResponseStates.HttpFailure, Message: $"({ex.GetType().Name}) {ex.Message}")
      : new(ApiResponseStates.Failure, Message: $"({ex.GetType().Name}) {ex.Message}");

  internal static ApiResponseStates State(HttpStatusCode status, bool notFound = false) =>
    status switch {
      HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ApiResponseStates.Unauthorised,
      HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable => ApiResponseStates.ServiceUnavailable,
      HttpStatusCode.NotFound when notFound => ApiResponseStates.NotFound,
      _ => ApiResponseStates.Failure
    };

  // Graph's errors look like {"error": {"code": "ErrorAccessDenied", "message": "Access is denied..."}}
  internal static async Task<string> ErrorMessage(HttpResponseMessage response) {
    string body = await response.Content.ReadAsStringAsync();
    string message = $"({(int)response.StatusCode} {response.ReasonPhrase}) {body}";
    try {
      if (JsonNode.Parse(body)?["error"] is JsonObject error) {
        message = $"({(int)response.StatusCode} {error["code"]?.GetValue<string>()}) {error["message"]?.GetValue<string>()}";
      }
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
      // Not Graph's usual error, so the body is used as it is
    }
    return message;
  }
}
