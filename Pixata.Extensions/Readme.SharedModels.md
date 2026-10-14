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
| `NoAuditAttribute` | Applied to an entity class to keep it out of the audit trail, or (as of v2.21.0) to a property to have its value written as `Audit.HiddenValue` (`"(hidden)"`) instead of the real value. Used as `[NoAudit]` |

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

## Grid requests

The `PixataGrid` component in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md) describes the data it wants with a `GridRequest`. The types are here, rather than in Pixata.Blazor, so a WASM client can send a request to an API and the server can apply it to a query using the same types. The grid is a work in progress, so the request may gain more as the grid does.

Namespace `Pixata.Extensions.Grid`...

| Type | What it is |
| --- | --- |
| `GridRequest` | `Page` (1-based), `PageSize` (zero means no paging), `Skip` (the number of items to skip, in place of the page, for virtual scrolling), `Sorts` (a list of `GridSortDescriptor`s in priority order), `Filters` (a list of `GridCompositeFilterDescriptor`s, all of which must pass), `Search` (the search box's filter, if any), `Groups` (a list of `GridGroupDescriptor`s, outermost first) and `Aggregates` (a list of `GridAggregateDescriptor`s) |
| `GridFilterDescriptor` | `Member`, `Operator` (a `GridFilterOperator`) and `Value`. A value that has been through JSON arrives as a `JsonElement`, and is converted to the field's type when the filter is applied |
| `GridCompositeFilterDescriptor` | `Logic` (`And` or `Or`) and `Filters`, a list of `GridFilterDescriptor`s, such as the two conditions in a column's filter menu |
| `GridSortDescriptor` | `Member` (the field, such as "Address.City") and `SortDirection` (a `ListSortDirection`) |
| `GridGroupDescriptor` | `Member` (the field to group by) and `SortDirection` (the order of the groups, ascending by default) |
| `GridAggregateDescriptor` | `Member` and `Aggregate`, a `GridAggregateType` (`Count`, `Sum`, `Average`, `Min` or `Max`) |
| `GridResult<T>` | `Data` (the requested page of items) and `Total` (the number of items before paging). If the request asked for them, `Groups` (a list of `GridGroup<T>`s) and `Aggregates` (a `GridFieldAggregates` for each field, keyed by field) |
| `GridGroup<T>` | `Member`, `Key` (the value the group's items share), `Count` (the number of items in the whole group, not just on the page), `Items` (the group's items on the page, for the innermost level), `Subgroups` (for the other levels) and `Aggregates` (for the whole group) |
| `GridFieldAggregates` | `Count`, `Sum`, `Average`, `Min` and `Max` for one field, each null if it wasn't asked for. `Min`, `Max` and `Average` of no items are null |
| `GridQueryExtensions` | `ToGridResult(request)` on an `IQueryable<T>` or `IEnumerable<T>`, which filters, sorts (by the groups first), counts, pages, and then works out the groups and aggregates. The group counts and aggregates are separate queries, so on EF Core they run in the database. `ApplySorts(sorts)` and `ApplyFilters(filters)` do just the sorting or filtering. Text comparisons ignore case (using `ToLower()`, which EF Core can translate). These only use standard LINQ, so on an EF Core query they are translated to SQL |
| `FieldAccessor` | Reads a field from an object given its path as a string, such as "Address.City", returning null if anything along the path is null. `Getter<T>(field)` returns a compiled function, cached per type and path. `Lambda(type, field)` builds the equivalent expression (such as `item => item.Address.City`) for use in a query, checking for nulls along the way. `Member()` and `FieldType()` describe what's at the end of the path. A path that doesn't match public properties or fields (it is case sensitive) throws an `ArgumentException` |
