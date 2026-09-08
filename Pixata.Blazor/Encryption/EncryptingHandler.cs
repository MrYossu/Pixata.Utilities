using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Pixata.Extensions.Encryption;

namespace Pixata.Blazor.Encryption;

public class EncryptingHandler(PayloadEncryptorInterface encryptor, IOptions<EncryptionOptions> options) : DelegatingHandler {
  private readonly EncryptionOptions _options = options.Value;

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, CancellationToken cancellationToken) {
    if (IsHandshakeRequest(request)) {
      return await base.SendAsync(request, cancellationToken);
    }

    if (!encryptor.IsReady) {
      string baseUri = request.RequestUri!.GetLeftPart(UriPartial.Authority);
      await encryptor.Initialise(baseUri + _options.HandshakePath, _options.SiteId);
    }

    request.Headers.TryAddWithoutValidation(_options.SessionHeader, encryptor.SessionId);

    if (request.Content is not null) {
      byte[] plaintext = await request.Content.ReadAsByteArrayAsync(cancellationToken);
      if (plaintext.Length > 0) {
        byte[] encrypted = await encryptor.Encrypt(plaintext);
        request.Content = new ByteArrayContent(encrypted);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(_options.EncryptedContentType);
      }
    }

    HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

    if (response.Content.Headers.ContentType?.MediaType == _options.EncryptedContentType) {
      byte[] encryptedResponse = await response.Content.ReadAsByteArrayAsync(cancellationToken);
      if (encryptedResponse.Length > 0) {
        byte[] decrypted = await encryptor.Decrypt(encryptedResponse);
        response.Content = new ByteArrayContent(decrypted);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      }
    }

    return response;
  }

  private bool IsHandshakeRequest(HttpRequestMessage request) =>
    request.RequestUri?.AbsolutePath.Equals(_options.HandshakePath, StringComparison.OrdinalIgnoreCase) == true;
}
