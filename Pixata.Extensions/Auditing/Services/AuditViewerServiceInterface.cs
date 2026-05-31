using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pixata.Extensions.Auditing.Models;

namespace Pixata.Extensions.Auditing.Services;

public interface AuditViewerServiceInterface {
  Task<List<EntityTypeMetadata>> GetEntityTypes();
  Task<List<string>> GetDistinctEntityIds(string entityType);
  Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId);
  Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId, DateTime? fromDate, DateTime? toDate, string? user, AuditOperation? operation);
  Task<List<AuditEntryViewModel>> GetAllAuditEntries(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user);
}
