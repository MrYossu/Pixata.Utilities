using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Pixata.Extensions.Encryption;

namespace Pixata.AspNetCore.Encryption;

public static class HandshakeEndpoint {
  public static IEndpointRouteBuilder MapEncryptionHandshake(this IEndpointRouteBuilder endpoints) {
    endpoints.MapPost("/api/encryption/handshake", (
      HandshakeRequest request,
      SessionKeyStore keyStore,
      IOptions<EncryptionOptions> options) => {
      byte[] clientPublicKeyBytes = Convert.FromBase64String(request.PublicKey);

      using ECDiffieHellman serverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
      ECParameters serverParams = serverEcdh.ExportParameters(false);
      byte[] serverPublicKey = new byte[65];
      serverPublicKey[0] = 0x04;
      serverParams.Q.X!.CopyTo(serverPublicKey, 1);
      serverParams.Q.Y!.CopyTo(serverPublicKey, 33);

      byte[] x = clientPublicKeyBytes.AsSpan(1, 32).ToArray();
      byte[] y = clientPublicKeyBytes.AsSpan(33, 32).ToArray();
      using ECDiffieHellman clientEcdh = ECDiffieHellman.Create(new ECParameters {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = x, Y = y }
      });

      byte[] rawSecret = serverEcdh.DeriveRawSecretAgreement(clientEcdh.PublicKey);

      byte[] info = Encoding.UTF8.GetBytes("EncodeData-" + options.Value.SiteId);
      byte[] aesKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, rawSecret, 32, Array.Empty<byte>(), info);

      string sessionId = Guid.NewGuid().ToString();
      keyStore.Store(sessionId, aesKey);

      return Results.Ok(new HandshakeResponse {
        PublicKey = Convert.ToBase64String(serverPublicKey),
        SessionId = sessionId
      });
    });

    return endpoints;
  }
}
