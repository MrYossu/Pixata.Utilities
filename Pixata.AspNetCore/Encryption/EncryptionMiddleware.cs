using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Pixata.Extensions.Encryption;

namespace Pixata.AspNetCore.Encryption;

public class EncryptionMiddleware(RequestDelegate next, SessionKeyStore keyStore, IOptions<EncryptionOptions> options) {
  private readonly EncryptionOptions _options = options.Value;

  public async Task InvokeAsync(HttpContext context) {
    if (IsHandshakePath(context.Request.Path)) {
      await next(context);
      return;
    }

    string? sessionId = context.Request.Headers[_options.SessionHeader].FirstOrDefault();
    if (sessionId is null) {
      await next(context);
      return;
    }

    byte[]? key = keyStore.Get(sessionId);
    if (key is null) {
      context.Response.StatusCode = 401;
      return;
    }

    if (context.Request.ContentType?.StartsWith(_options.EncryptedContentType) == true) {
      using MemoryStream ms = new();
      await context.Request.Body.CopyToAsync(ms);
      byte[] encrypted = ms.ToArray();

      if (encrypted.Length > 0) {
        byte[] decrypted = AesGcmEncryptor.Decrypt(encrypted, key);
        context.Request.Body = new MemoryStream(decrypted);
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = decrypted.Length;
      }
    }

    Stream originalBody = context.Response.Body;
    using MemoryStream responseBuffer = new();
    context.Response.Body = responseBuffer;

    await next(context);

    responseBuffer.Seek(0, SeekOrigin.Begin);
    byte[] responseBytes = responseBuffer.ToArray();

    if (responseBytes.Length > 0) {
      byte[] encryptedResponse = AesGcmEncryptor.Encrypt(responseBytes, key);
      context.Response.Body = originalBody;
      context.Response.ContentType = _options.EncryptedContentType;
      context.Response.ContentLength = encryptedResponse.Length;
      context.Response.Headers.Remove("Transfer-Encoding");
      await context.Response.Body.WriteAsync(encryptedResponse);
    } else {
      context.Response.Body = originalBody;
    }
  }

  private bool IsHandshakePath(PathString path) =>
    path.Equals(_options.HandshakePath, StringComparison.OrdinalIgnoreCase);
}
