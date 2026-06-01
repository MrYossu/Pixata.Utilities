using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pixata.AspNetCore.Auditing.Services;
using Pixata.Extensions.Auditing.Attributes;
using Pixata.Extensions.Auditing.Models;

namespace Pixata.AspNetCore.Auditing.Interceptors;

public class AuditingInterceptor(IHttpContextAccessor httpContextAccessor, AuditUserContextInterface auditUserContext) : SaveChangesInterceptor {
  private static readonly JsonSerializerOptions JsonOptions = new() {
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private record PendingAddedAudit(EntityEntry Entry, string EntityTypeName, string ChangedBy, DateTime ChangedAt);

  private readonly List<PendingAddedAudit> _pendingAddedAudits = [];

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
    if (eventData.Context is null) {
      return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    DbContext context = eventData.Context;
    string changedBy = GetChangedBy();
    DateTime changedAt = DateTime.UtcNow;

    List<EntityEntry> entries = context.ChangeTracker.Entries()
      .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
      .Where(e => e.Entity.GetType() != typeof(Audit))
      .Where(e => !e.Entity.GetType().IsDefined(typeof(NoAuditAttribute), inherit: true))
      .ToList();

    foreach (EntityEntry entry in entries) {
      Type entityType = entry.Entity.GetType();
      string entityTypeName = entityType.FullName ?? entityType.Name;

      if (entry.State == EntityState.Added) {
        // Defer audit creation for Added entities: the database-generated ID is not yet
        // available at this point. The real ID will be resolved after SaveChanges completes.
        _pendingAddedAudits.Add(new PendingAddedAudit(entry, entityTypeName, changedBy, changedAt));
        continue;
      }

      string entityId = GetPrimaryKeyValue(entry);
      AuditOperation operation = entry.State switch {
        EntityState.Modified => AuditOperation.Updated,
        EntityState.Deleted => AuditOperation.Deleted,
        _ => throw new ArgumentOutOfRangeException()
      };

      string fullSnapshot = SerialiseEntity(entry);
      string? changedProperties = operation == AuditOperation.Updated
        ? SerialiseChangedProperties(entry)
        : null;

      Audit audit = new() {
        EntityType = entityTypeName,
        EntityId = entityId,
        Operation = operation,
        ChangedBy = changedBy,
        ChangedAt = changedAt,
        FullSnapshot = fullSnapshot,
        ChangedProperties = changedProperties
      };

      context.Set<Audit>().Add(audit);
    }

    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) {
    if (_pendingAddedAudits.Count > 0 && eventData.Context is not null) {
      DbContext context = eventData.Context;

      foreach (PendingAddedAudit pending in _pendingAddedAudits) {
        // The database has now assigned the real ID, so we can read it from the entry.
        string entityId = GetPrimaryKeyValue(pending.Entry);
        string fullSnapshot = SerialiseEntity(pending.Entry);

        Audit audit = new() {
          EntityType = pending.EntityTypeName,
          EntityId = entityId,
          Operation = AuditOperation.Created,
          ChangedBy = pending.ChangedBy,
          ChangedAt = pending.ChangedAt,
          FullSnapshot = fullSnapshot,
          ChangedProperties = null
        };

        context.Set<Audit>().Add(audit);
      }

      _pendingAddedAudits.Clear();
      await context.SaveChangesAsync(cancellationToken);
    }

    return await base.SavedChangesAsync(eventData, result, cancellationToken);
  }

  public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default) {
    _pendingAddedAudits.Clear();
    return base.SaveChangesFailedAsync(eventData, cancellationToken);
  }

  private string GetChangedBy() {
    if (!string.IsNullOrWhiteSpace(auditUserContext.UserIdentifier)) {
      return auditUserContext.UserIdentifier;
    }

    string? identityName = httpContextAccessor.HttpContext?.User?.Identity?.Name;
    if (!string.IsNullOrWhiteSpace(identityName)) {
      return identityName;
    }

    return "System";
  }

  private static string GetPrimaryKeyValue(EntityEntry entry) {
    Microsoft.EntityFrameworkCore.Metadata.IKey? primaryKey = entry.Metadata.FindPrimaryKey();
    if (primaryKey is null) {
      return "";
    }

    List<object?> keyValues = primaryKey.Properties
      .Select(p => entry.Property(p.Name).CurrentValue)
      .ToList();

    if (keyValues.Count == 1) {
      return keyValues[0]?.ToString() ?? "";
    }

    return JsonSerializer.Serialize(keyValues, JsonOptions);
  }

  private static string SerialiseEntity(EntityEntry entry) {
    Dictionary<string, object?> properties = new();
    foreach (PropertyEntry property in entry.Properties) {
      properties[property.Metadata.Name] = entry.State == EntityState.Deleted
        ? property.OriginalValue
        : property.CurrentValue;
    }

    return JsonSerializer.Serialize(properties, JsonOptions);
  }

  private static string? SerialiseChangedProperties(EntityEntry entry) {
    Dictionary<string, object?[]> changed = new();
    foreach (PropertyEntry property in entry.Properties) {
      if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue)) {
        changed[property.Metadata.Name] = [property.OriginalValue, property.CurrentValue];
      }
    }

    return changed.Count > 0 ? JsonSerializer.Serialize(changed, JsonOptions) : null;
  }
}
