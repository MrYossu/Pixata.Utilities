using System.Net;
using System.Text;

namespace Pixata.Email.Tests;

/// <summary>
/// A stand-in for Graph that answers each request with whatever <see cref="Respond"/> says, and keeps the requests, with their bodies, which are
/// gone once the request is disposed
/// </summary>
public class ScriptedGraph : HttpMessageHandler {
  public record Call(HttpMethod Method, string Url, string? Authorization, string? ContentRange, string? Prefer, byte[] Body) {
    public string Text => Encoding.UTF8.GetString(Body);
  }

  public List<Call> Calls { get; } = [];

  /// <summary>
  /// Given the method and the URL (unescaped), the status and body to send back. By default, everything succeeds with no body
  /// </summary>
  public Func<HttpMethod, string, (HttpStatusCode Status, string Body)> Respond { get; set; } = (_, _) => (HttpStatusCode.OK, "");

  /// <summary>
  /// The bytes to send back for a request whose URL ends in $value, as Graph does for an attachment's content
  /// </summary>
  public byte[] Content { get; set; } = [];

  public bool ContentStreamDisposed { get; private set; }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
    byte[] body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
    string url = Uri.UnescapeDataString(request.RequestUri!.ToString());
    Calls.Add(new(request.Method, url, request.Headers.Authorization?.ToString(), request.Content?.Headers.ContentRange?.ToString(),
      request.Headers.TryGetValues("Prefer", out IEnumerable<string>? prefer) ? string.Join(",", prefer) : null, body));
    (HttpStatusCode status, string responseBody) = Respond(request.Method, url);
    if (url.EndsWith("/$value") && (int)status < 300) {
      return new(status) { Content = new StreamContent(new WatchedStream(Content, () => ContentStreamDisposed = true)) };
    }
    return new(status) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") };
  }

  private class WatchedStream(byte[] data, Action disposed) : MemoryStream(data) {
    protected override void Dispose(bool disposing) {
      disposed();
      base.Dispose(disposing);
    }
  }
}
