# Payload encryption (ECDH + AES-256-GCM)

>Part of [Pixata.AspNetCore](Readme.md)

This package (together with `Pixata.Blazor` and `Pixata.Extensions`) provides transparent encryption of all HTTP request and response bodies between a Blazor WASM client and an ASP.NET Core server. The data in the browser's Network tab appears as binary blobs instead of readable JSON.

This is **obfuscation**, not a replacement for TLS. It prevents casual inspection of API traffic by the app's own users (eg reading prices, internal IDs, or other business data from the Network tab). A sufficiently motivated attacker who controls the browser can still intercept data by modifying the JS interop module or setting breakpoints.

## How it works

1. On the first API call, the Blazor client generates an ephemeral ECDH P-256 key pair (via the browser's SubtleCrypto API) and sends its public key to a handshake endpoint on the server.
2. The server generates its own ECDH key pair (via .NET `ECDiffieHellman`), derives a shared secret, then uses HKDF-SHA256 to produce an AES-256 session key. It returns its public key and a session ID.
3. The client derives the same AES-256 key using SubtleCrypto.
4. All subsequent requests and responses are encrypted with AES-256-GCM. The wire format is `[12-byte nonce][ciphertext][16-byte auth tag]` sent as `application/octet-stream`.

Each session gets a unique key pair. Each deployment can use a different site ID, so even with identical ECDH outputs, different sites derive different keys.

## Server setup (this package)

**1. Register encryption services in `Program.cs`:**

```csharp
using Pixata.AspNetCore.Encryption;

builder.Services.AddEncryptionServer(options =>
    options.SiteId = "my-unique-site-id");
```

Or use the shorthand...

```csharp
builder.Services.AddEncryptionServer("my-unique-site-id");
```

**2. Add middleware and endpoints (order matters):**

```csharp
app.UseStaticFiles();
app.UseEncryptionMiddleware();
app.MapEncryptionHandshake();
// If you're using .NET 10, see the note below

// Your API endpoints go here
app.MapMyEndpoints();
```

The encryption middleware must come **before** your API endpoints but **after** `UseStaticFiles()`. The handshake endpoint is mapped at `/api/encryption/handshake` by default.

## Client setup (Pixata.Blazor)

See the [client-side encryption documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Encryption.md) in the Pixata.Blazor package.

## .NET 10: POST/PUT endpoint caveat

In .NET 10, the `RequestDelegateFactory` checks `Content-Type` before the middleware has a chance to decrypt the body and restore `application/json`. This means that POST and PUT endpoints using automatic body binding (eg `(ProductDto product)` as a parameter) will return **415 Unsupported Media Type**.

The fix is to use `HttpContext` and read the body manually...

```csharp
// Instead of this (breaks with encryption middleware):
app.MapPost("/api/products", async (ProductDto product, ProductServiceInterface service) =>
  Results.Ok(await service.Create(product)));

// Do this:
app.MapPost("/api/products", async (HttpContext context, ProductServiceInterface service) => {
  ProductDto? product = await context.Request.ReadFromJsonAsync<ProductDto>();
  if (product is null) {
    return Results.BadRequest();
  }
  ProductDto created = await service.Create(product);
  return Results.Created($"/api/products/{created.Id}", created);
});
```

GET and DELETE endpoints (which have no request body) are unaffected.

## .NET 10: Static web asset fingerprinting

.NET 10 adds fingerprint hashes to static web asset filenames (eg `crypto-interop.a1b2c3d4e5.js`). Blazor's `IJSRuntime` imports the original path, so the JS module used by the encryption won't load.

Add this middleware **before** `UseStaticFiles()` to strip the fingerprints...

```csharp
app.UseRclFingerprintFallback();
app.UseStaticFiles();
```

This is included in `Pixata.AspNetCore.Encryption` as an extension method. It only affects paths under `/_content/` and only strips fingerprint-shaped segments, so it won't interfere with other static files.

## Configuration options

All options have sensible defaults. The full set...

| Option | Default | Description |
| --- | --- | --- |
| `SiteId` | `"default"` | Per-deployment identifier used in HKDF key derivation |
| `HandshakePath` | `"/api/encryption/handshake"` | Path for the key exchange endpoint |
| `SessionHeader` | `"X-Encryption-Session"` | Header name carrying the session ID |
| `EncryptedContentType` | `"application/octet-stream"` | Content-Type used for encrypted payloads |

These live in `EncryptionOptions` in Pixata.Extensions, as both ends need them. The `SiteId` **must match** between server and client. All other options must also match if you change them from defaults.

## Session key storage

Session keys are stored in an in-memory `ConcurrentDictionary`. In a multi-instance deployment, you will need sticky sessions or a distributed cache to ensure a client always hits the server that holds its session key.

## Packages involved

| Package | What it contributes |
| --- | --- |
| `Pixata.Extensions` | `EncryptionOptions`, `HandshakeRequest`, `HandshakeResponse` (shared between client and server) |
| `Pixata.Blazor` | `SubtleCryptoEncryptor` (JS interop), `EncryptingHandler` (DelegatingHandler), `AddEncryptedHttpClient` extension, `crypto-interop.js` |
| `Pixata.AspNetCore` | `EncryptionMiddleware`, `HandshakeEndpoint`, `AesGcmEncryptor`, `SessionKeyStore`, `AddEncryptionServer`/`UseEncryptionMiddleware`/`MapEncryptionHandshake`/`UseRclFingerprintFallback` extensions |
