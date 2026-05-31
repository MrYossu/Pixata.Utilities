using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pixata.Extensions.Auditing.Models;

namespace Pixata.Extensions.Auditing.Services;

public interface AuditServiceInterface {
  Task<List<Audit>> GetAuditHistory(string entityType, string entityId);
  Task<List<string>> GetAllAuditedEntityTypes();
  Task<List<string>> GetDistinctEntityIds(string entityType);
  Task<List<Audit>> GetAuditsByEntityTypes(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user);
}
