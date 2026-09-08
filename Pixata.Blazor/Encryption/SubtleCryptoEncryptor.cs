using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Pixata.Extensions.Encryption;

namespace Pixata.Blazor.Encryption;

public class SubtleCryptoEncryptor(IJSRuntime js, IHttpClientFactory httpClientFactory) : PayloadEncryptorInterface, IAsyncDisposable {
  private readonly SemaphoreSlim _initLock = new(1, 1);
  private IJSObjectReference? _module;

  public bool IsReady => SessionId is not null;
  public string? SessionId { get; private set; }

  public async Task Initialise(string handshakeUrl, string siteId) {
    if (IsReady) {
      return;
    }

    await _initLock.WaitAsync();
    try {
      if (IsReady) {
        return;
      }

      _module = await js.InvokeAsync<IJSObjectReference>(
        "import", "./_content/Pixata.Blazor/crypto-interop.js");

      byte[] clientPublicKey = await _module.InvokeAsync<byte[]>("generateKeyPair");

      HttpClient handshakeClient = httpClientFactory.CreateClient("EncryptionHandshake");
      HttpResponseMessage response = await handshakeClient.PostAsJsonAsync(handshakeUrl, new HandshakeRequest {
        PublicKey = Convert.ToBase64String(clientPublicKey)
      });
      response.EnsureSuccessStatusCode();

      HandshakeResponse handshake = await response.Content.ReadFromJsonAsync<HandshakeResponse>()
                      ?? throw new InvalidOperationException("Empty handshake response");

      byte[] serverPublicKey = Convert.FromBase64String(handshake.PublicKey);
      await _module.InvokeVoidAsync("deriveSessionKey", serverPublicKey, siteId);

      SessionId = handshake.SessionId;
    } finally {
      _initLock.Release();
    }
  }

  public async Task<byte[]> Encrypt(byte[] plaintext) {
    if (_module is null) {
      throw new InvalidOperationException("Not initialised");
    }
    return await _module.InvokeAsync<byte[]>("encrypt", plaintext);
  }

  public async Task<byte[]> Decrypt(byte[] ciphertext) {
    if (_module is null) {
      throw new InvalidOperationException("Not initialised");
    }
    return await _module.InvokeAsync<byte[]>("decrypt", ciphertext);
  }

  public async ValueTask DisposeAsync() {
    if (_module is not null) {
      await _module.DisposeAsync();
    }
    _initLock.Dispose();
  }
}
