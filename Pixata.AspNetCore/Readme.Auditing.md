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
- **ChangedAt** - UTC timestamp
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

## Custom user identification

By default, the auditing system identifies users via `HttpContext.User.Identity.Name`, falling back to `"System"` if no user is available. You can override this by injecting `AuditUserContextInterface` and setting the `UserIdentifier` property...

```csharp
public class SprocketController(AuditUserContextInterface auditContext) : ControllerBase {
  [AllowAnonymous]
  public async Task<IActionResult> NotifyFromSprocket() {
    auditContext.UserIdentifier = "SprocketNotificationEndpoint";
    // Any changes saved in this request will use this identifier
  }
}
```

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

By default, audit entries are retained forever. You can configure automatic cleanup by specifying a retention period...

```csharp
builder.Services.AddAuditing<MyDbContext>(options => {
  options.RetentionPeriod = TimeSpan.FromDays(90); // Delete entries older than 90 days
  options.CleanupInterval = TimeSpan.FromHours(6); // Check every 6 hours (default: daily)
});
```

When a retention period is set, a background service runs periodically and deletes audit entries older than the configured period. If you don't set one, the background service isn't registered at all.
