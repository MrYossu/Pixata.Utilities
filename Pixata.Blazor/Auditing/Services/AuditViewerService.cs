using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pixata.Extensions.Auditing.Attributes;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.Blazor.Auditing.Services;

public class AuditViewerService(AuditServiceInterface auditService, DbContext context) : AuditViewerServiceInterface {
  public async Task<List<EntityTypeMetadata>> GetEntityTypes() {
    List<EntityTypeMetadata> contextTypes = GetEntityTypesFromContext(context.GetType());
    List<string> auditedEntityTypes = await auditService.GetAllAuditedEntityTypes();

    HashSet<string> existingNames = contextTypes.Select(et => et.FullName).ToHashSet();
    string auditTypeName = typeof(Audit).FullName ?? typeof(Audit).Name;

    foreach (string entityType in auditedEntityTypes) {
      if (entityType != auditTypeName && !existingNames.Contains(entityType)) {
        string shortName = entityType.Contains('.') ? entityType[(entityType.LastIndexOf('.') + 1)..] : entityType;
        contextTypes.Add(new EntityTypeMetadata { FullName = entityType, ShortName = shortName });
      }
    }

    return contextTypes.OrderBy(et => et.ShortName).ToList();
  }

  public async Task<List<string>> GetDistinctEntityIds(string entityType) =>
    await auditService.GetDistinctEntityIds(entityType);

  public async Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId) {
    List<Audit> audits = await auditService.GetAuditHistory(entityType, entityId);
    return audits.Select(MapToViewModel).ToList();
  }

  public async Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId, DateTime? fromDate, DateTime? toDate, string? user, AuditOperation? operation) {
    List<Audit> audits = await auditService.GetAuditHistory(entityType, entityId);

    IEnumerable<Audit> filtered = audits;
    if (fromDate.HasValue) {
      filtered = filtered.Where(audit => audit.ChangedAt >= fromDate.Value);
    }
    if (toDate.HasValue) {
      filtered = filtered.Where(audit => audit.ChangedAt <= toDate.Value);
    }
    if (!string.IsNullOrWhiteSpace(user)) {
      filtered = filtered.Where(audit => audit.ChangedBy == user);
    }
    if (operation.HasValue) {
      filtered = filtered.Where(audit => audit.Operation == operation.Value);
    }

    return filtered.Select(MapToViewModel).ToList();
  }

  public static List<EntityTypeMetadata> GetEntityTypesFromContext(Type contextType) {
    List<EntityTypeMetadata> result = [];
    PropertyInfo[] properties = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (PropertyInfo property in properties) {
      Type propertyType = property.PropertyType;
      if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(DbSet<>)) {
        Type entityType = propertyType.GetGenericArguments()[0];
        if (entityType == typeof(Audit)) {
          continue;
        }
        if (entityType.GetCustomAttribute<NoAuditAttribute>() is not null) {
          continue;
        }
        result.Add(new EntityTypeMetadata {
          FullName = entityType.FullName ?? entityType.Name,
          ShortName = entityType.Name
        });
      }
    }

    return result.OrderBy(et => et.ShortName).ToList();
  }

  public async Task<List<AuditEntryViewModel>> GetAllAuditEntries(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user) {
    List<Audit> audits = await auditService.GetAuditsByEntityTypes(entityTypes, fromDate, toDate, user);
    return audits.Select(MapToViewModel).ToList();
  }

  private static AuditEntryViewModel MapToViewModel(Audit audit) {
    Dictionary<string, object?> properties = [];
    if (!string.IsNullOrWhiteSpace(audit.FullSnapshot)) {
      JsonSerializerOptions options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };
      properties = JsonSerializer.Deserialize<Dictionary<string, object?>>(audit.FullSnapshot, options) ?? [];
    }

    List<PropertyChange> changedProperties = [];
    if (!string.IsNullOrWhiteSpace(audit.ChangedProperties)) {
      JsonSerializerOptions options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };
      Dictionary<string, JsonElement>? changes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(audit.ChangedProperties, options);
      if (changes is not null) {
        foreach (KeyValuePair<string, JsonElement> change in changes) {
          if (change.Value.ValueKind == JsonValueKind.Array) {
            JsonElement[] values = [.. change.Value.EnumerateArray()];
            changedProperties.Add(new PropertyChange {
              PropertyName = change.Key,
              OldValue = values.Length > 0 ? values[0].ToString() : null,
              NewValue = values.Length > 1 ? values[1].ToString() : null
            });
          }
        }
      }
    }

    return new AuditEntryViewModel {
      Id = audit.Id,
      EntityType = audit.EntityType,
      EntityId = audit.EntityId,
      Operation = audit.Operation,
      ChangedBy = audit.ChangedBy,
      ChangedAt = audit.ChangedAt,
      Properties = properties,
      ChangedProperties = changedProperties
    };
  }
}
