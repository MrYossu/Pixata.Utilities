# Shared models

>Part of [Pixata.Extensions](Readme.md)

As well as the extension methods, this package holds the types that are shared between the client-side and server-side Pixata packages. They live here because this package has no dependency on EF Core, ASP.NET Core or anything else that a Blazor WASM app can't reference, so both halves of a feature can use the same types.

You will rarely need to use these directly. They are documented here so that you know where they are and why the package contains them.

## Auditing

The auditing feature spans three packages. The models and interfaces are here, the EF Core interceptor and services are in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Auditing.md), and the viewer components are in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.AuditViewer.md).

Namespace `Pixata.Extensions.Auditing.Models`...

| Type | What it is |
| --- | --- |
| `Audit` | The audit record itself, and the entity you add to your `DbContext` as `DbSet<Audit>`. Holds the entity type, the JSON-serialised key, the operation, who changed it and when, a full JSON snapshot, and the changed properties |
| `AuditOperation` | An `enum` of `Created`, `Updated` and `Deleted` |
| `AuditEntryViewModel` | An audit record with its JSON unpacked into a property dictionary and a list of `PropertyChange`s, which is what the viewer binds to |
| `PropertyChange` | The name of a property, its old value and its new value |
| `EntityTypeMetadata` | The full and short names of an audited entity type, used to populate the viewer's entity type dropdown |

Namespace `Pixata.Extensions.Auditing.Attributes`...

| Type | What it is |
| --- | --- |
| `NoAuditAttribute` | Applied to an entity class to keep it out of the audit trail. Used as `[NoAudit]` |

Namespace `Pixata.Extensions.Auditing.Services`...

| Type | What it is |
| --- | --- |
| `AuditServiceInterface` | Reads raw `Audit` records. Implemented by `AuditService` in Pixata.AspNetCore |
| `AuditViewerServiceInterface` | Supplies the viewer with entity types, entity Ids and filtered history. Implemented server-side by `AuditViewerService` (Pixata.AspNetCore, talks to a `DbContext`) and client-side by `AuditViewerHttpService` (Pixata.Blazor, talks to the audit API) |

That last pair is the reason these interfaces are here. The `AuditViewer` component only ever refers to `AuditViewerServiceInterface`, so the same component works in a server-side app and in a WASM one, without the Blazor package needing EF Core.

## Payload encryption

The encryption feature is likewise split, with the browser-side encryptor in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Encryption.md) and the middleware in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Encryption.md). Both ends need to agree on the options and on the shape of the handshake, so those types live here.

Namespace `Pixata.Extensions.Encryption`...

| Type | What it is |
| --- | --- |
| `EncryptionOptions` | `SiteId`, `HandshakePath`, `SessionHeader` and `EncryptedContentType`. These must match on the client and the server |
| `HandshakeRequest` | The client's ECDH public key, sent to the handshake endpoint |
| `HandshakeResponse` | The server's ECDH public key and the session Id that identifies the derived key |

## File uploads

`UploadFileDto` is a record of `(string JoinGuid, string FileName, byte[] Buffer)`, used to send a file from a Blazor client to an API endpoint. `PixataBaseClientService` in Pixata.Blazor has a `PostBson()` method that posts one of these as BSON, which avoids the base64 inflation you get from sending a byte array as JSON.
