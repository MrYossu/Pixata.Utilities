# Payload encryption

>Part of [Pixata.Blazor](Readme.md)

This package includes client-side support for transparent ECDH + AES-256-GCM payload encryption between Blazor WASM and an ASP.NET Core server. All HTTP request and response bodies are encrypted so that data appears as binary blobs in the browser's Network tab.

The encryption uses the browser's SubtleCrypto API via JS interop, so it only works for components running in **WebAssembly**, not server-side rendering. This is by design: when code runs server-side, API calls go directly from server to server and never appear in the browser's Network tab, so there is nothing to hide.

For a full explanation of how the encryption works, the server-side setup, and important .NET 10 caveats, see the [encryption documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Encryption.md) in the Pixata.AspNetCore package.

## Client setup

The `crypto-interop.js` file is included as a static web asset in this package and is loaded automatically via `IJSRuntime` when the encryption initialises. No manual `<script>` tag is needed in either `index.html` or `App.razor`.

### Pure WASM app (standalone or ASP.NET Core hosted)

This is the pattern where your WASM project has an `index.html` in `wwwroot` and a single `Program.cs`. Register the encrypted HTTP client in `Program.cs`...

```csharp
using Pixata.Blazor.Encryption;

builder.Services.AddEncryptedHttpClient<IMyService, MyHttpService>(
    options => options.SiteId = "my-unique-site-id",
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
```

Or use the shorthand...

```csharp
builder.Services.AddEncryptedHttpClient<IMyService, MyHttpService>(
    "my-unique-site-id",
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
```

If you are using the ASP.NET Core hosted pattern (where a server project hosts the WASM app), the server project's `Program.cs` will already have `app.UseBlazorFrameworkFiles()` and `app.MapFallbackToFile("index.html")`. The encryption server-side setup goes in that same server `Program.cs`.

### Hybrid Blazor Web App (.NET 8+)

This is the pattern where you have a server project (with `App.razor` instead of `index.html`) and a separate `.Client` project for WebAssembly components. There are two `Program.cs` files.

Register the encrypted HTTP client in the **client project's** `Program.cs` only...

```csharp
using Pixata.Blazor.Encryption;

builder.Services.AddEncryptedHttpClient<IMyService, MyHttpService>(
    options => options.SiteId = "my-unique-site-id",
    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
```

`builder.HostEnvironment.BaseAddress` resolves to the origin the WASM app was served from, so it works correctly in all environments (localhost during development, your production domain after deployment) without any URL changes.

Do **not** register the encrypted HTTP client in the server project's `Program.cs`. The encryption relies on the browser's SubtleCrypto API and will not work in server-side rendering. Components that need encrypted API calls should use `@rendermode InteractiveWebAssembly` (or `InteractiveAuto`, which falls back to WebAssembly after the WASM runtime is downloaded).

The encryption server-side setup (`AddEncryptionServer`, `UseEncryptionMiddleware`, `MapEncryptionHandshake`) goes in the **server project's** `Program.cs` as described in the [Pixata.AspNetCore encryption documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Encryption.md).

## What gets registered

The `SiteId` **must match** the value used on the server.

`AddEncryptedHttpClient` registers...

- `SubtleCryptoEncryptor` as the `PayloadEncryptorInterface` (handles JS interop with the browser's SubtleCrypto API)
- `EncryptingHandler` as a `DelegatingHandler` on the `HttpClient` (transparently encrypts and decrypts request and response bodies)
- A separate `"EncryptionHandshake"` named `HttpClient` for the initial key exchange

Your service class (`MyHttpService` in the examples above) uses `HttpClient` as normal - it never sees any encryption. The `EncryptingHandler` performs the ECDH handshake on the first API call and encrypts and decrypts all traffic after that.
