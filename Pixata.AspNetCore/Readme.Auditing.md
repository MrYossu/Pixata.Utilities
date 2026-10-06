# Auditing entities

>Part of [Pixata.AspNetCore](Readme.md)

When investigating bug reports from customers, I often find that the issue is nothing to do with my code, it's that they have changed something in the database, and I need to find out what they changed, and when.

Adding auditing manually can be done, but means you end up writing the same intrusive code in every app. To combat this, this package includes an entity auditing system that automatically captures all entity changes via an EF Core `SaveChangesInterceptor`. It stores full snapshots and property-level diffs for every create, update and delete operation.

The feature spans three packages...

| Package | What it contributes |
| --- | --- |
| `Pixata.Extensions` | The `Audit` entity, the view models, the `[NoAudit]` attribute and the service interfaces |
| `Pixata.AspNetCore` | The EF Core interceptor, the services, the retention background service and the API endpoints (this package) |
| `Pixata.Blazor` | The `AuditViewer` component for browsing the audit trail |

For the viewer itself, see the [audit viewer documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.AuditViewer.md) in the Pixata.Blazor package.

## Setup

**1. Register auditing services in `Program.cs`:**

```csharp
builder.Services.AddAuditing<MyDbContext>();
```

**2. Add the interceptor to your DbContext registration:**

```csharp
builder.Services.AddDbContext<MyDbContext>((serviceProvider, options) =>
  options.UseSqlServer(connectionString)
         .AddAuditingInterceptor(serviceProvider));
```

**3. Add the `Audit` DbSet to your DbContext:**

```csharp
using Pixata.Extensions.Auditing.Models;

public class MyDbContext : DbContext {
  public DbSet<Audit> Audits { get; set; }
  // ... other DbSets
}
```

**4. Create and run an EF Core migration:**

```sh
dotnet ef migrations add AddAuditing
dotnet ef database update
```

That is all you need for auditing information to be recorded when entities are added, modified or deleted.

## Data model

Each audit entry stores...

- **EntityType** - fully qualified type name
- **EntityId** - JSON-serialised primary key (handles composite keys)
- **Operation** - Created, Updated or Deleted
- **ChangedBy** - username from Identity or custom identifier
- **ChangedAt** - UTC timestamp (the viewer can show it in another time zone)
- **FullSnapshot** - complete JSON of the entity at that point in time
- **ChangedProperties** - JSON of changed properties only (null for Create/Delete), stored as `{ "PropertyName": [oldValue, newValue] }`

## Opting out of auditing

Entities can opt out of auditing by applying the `[NoAudit]` attribute...

```csharp
using Pixata.Extensions.Auditing.Attributes;

[NoAudit]
public class SensitiveEntity {
  // This entity will not be audited
}
```

### Leaving individual properties out

If an entity should be audited, but has a property whose value mustn't be copied into the audit table (a password hash, an API token, bank details), put `[NoAudit]` on the property...

```csharp
public class Customer {
  public int Id { get; set; }
  public string Name { get; set; } = "";

  [NoAudit]
  public string? ApiToken { get; set; }
}
```

The property is still listed in the snapshot, and in the changed properties when its value changes, but its value is written as `"(hidden)"` (the `Audit.HiddenValue` constant), so you can see that it changed, but not what to. Note that a value converter (eg one that encrypts the column) doesn't help here, as the interceptor reads the model's value, not the stored one.

For types you can't decorate, such as ASP.NET Core Identity's user, use the options instead...

```csharp
builder.Services.AddAuditing<MyDbContext>(options => {
  options.ExcludeProperty<IdentityUser>(u => u.PasswordHash)
         .ExcludeProperty<IdentityUser>(u => u.SecurityStamp);
});
```

This applies to the type you name, and any type derived from it. There's also an overload that takes a property name, for shadow properties. Property-level exclusions were added in v2.0.0.

## Custom user identification

The interceptor works out who made the change in this order...

1. The `UserIdentifier` property of `AuditUserContextInterface`, if you've set it (see below)
2. `HttpContext.User.Identity.Name`, if there is an `HttpContext` with a named user
3. The user from Blazor's `AuthenticationStateProvider`, if one is registered. Inside an interactive Blazor Server circuit there is no usable `HttpContext`, so this is where the user comes from in most Blazor Server apps. Prior to v2.0.0, those changes were all recorded as `"System"`
4. `"System"`

Step 3 needs nothing from you. The provider is resolved if it's there, so apps that don't use Blazor aren't affected. For a synchronous `SaveChanges()`, the provider's state is only used if it's already available, as blocking on it inside a circuit could deadlock. In practice it always is by the time anything is saved.

You can override all of this by injecting `AuditUserContextInterface` and setting the `UserIdentifier` property...

```csharp
public class SprocketController(AuditUserContextInterface auditContext) : ControllerBase {
  [AllowAnonymous]
  public async Task<IActionResult> NotifyFromSprocket() {
    auditContext.UserIdentifier = "SprocketNotificationEndpoint";
    // Any changes saved in this request will use this identifier
  }
}
```

## Synchronous and asynchronous saves

Both `SaveChanges()` and `SaveChangesAsync()` are audited. Prior to v2.0.0, only `SaveChangesAsync()` was, so a synchronous save silently left a hole in the audit trail.

## Transactions

The audit rows for updated and deleted entities are saved with the entities themselves. The rows for added entities can't be, as they need the key the database generates, so they are written by a second save straight after the first.

As of v2.0.0, when there are added entities to audit and no transaction already, the interceptor starts one before the first save, and commits it after the second, so either the entities and their audit rows are all saved, or none of them are. Previously, a failure in the second save left the entities saved with no "Created" row, and threw an exception for a save that had actually succeeded, which invited a retry and a duplicate. Note that if the second save fails, the change tracker will already have marked the entities as saved, so discard the context.

The interceptor doesn't start a transaction when...

- you've already started one (or there's an ambient `TransactionScope`), as the second save then happens inside yours, and it's up to you to commit it;
- the provider isn't relational (eg the in-memory provider), as it doesn't support transactions;
- the context uses a retrying execution strategy (eg `EnableRetryOnFailure()` on SQL Server), as EF Core won't let a transaction be started inside one. If you need the guarantee, wrap the save yourself, as described in [Microsoft's connection resiliency docs](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#execution-strategies-and-transactions).

You can turn it off altogether with `options.UseTransactionForAddedEntities = false`.

## Serving the audit trail to the viewer

How you do this depends on where the viewer component runs.

### Server-side apps

The component can talk to the database directly, so all you need is...

```csharp
builder.Services.AddPixataAuditViewer();
```

You'll need `using Pixata.AspNetCore.Auditing.Extensions;`. This registers `AuditViewerService` as the `AuditViewerServiceInterface` that the component resolves.

>**Moved.** As of Pixata.Blazor v4.0.0, `AddPixataAuditViewer()` and the `AuditViewerService` it registers live in this package rather than in Pixata.Blazor. The service needs a `DbContext`, so keeping it in the Blazor package forced EF Core into every client-side app that used any component from it. The method and its behaviour are otherwise unchanged.

### Mixed-mode and WASM apps

The component runs in the browser, so it needs an API to talk to. In the server project's `Program.cs`, add...

```csharp
app.MapAuditApi("/api/audit");
```

The route can be anything you like.

As this endpoint exposes audit information, you should ensure that it is protected by authentication and authorisation. For example...

```csharp
app.MapAuditApi("/api/audit")
   .RequireAuthorization("AuditViewerPolicy");
```

Then, in the client project's `Program.cs`, add the following (from Pixata.Blazor)...

```csharp
builder.Services.AddAuditViewerHttpService(builder.HostEnvironment.BaseAddress + "api/audit/");
```

...where the route matches the one you used in the server project.

## Retention policy

By default, audit entries are retained forever. You can configure automatic cleanup by specifying a retention period (the lambda's parameter is an `AuditingOptions`, which inherits these from `AuditRetentionOptions`)...

```csharp
builder.Services.AddAuditing<MyDbContext>(options => {
  options.RetentionPeriod = TimeSpan.FromDays(90); // Delete entries older than 90 days
  options.CleanupInterval = TimeSpan.FromHours(6); // Check every 6 hours (default: daily)
});
```

When a retention period is set, a background service runs periodically and deletes audit entries older than the configured period. If you don't set one, the background service isn't registered at all.
