using Pixata.AspNetCore.Auditing.Models;

namespace Pixata.AspNetCore.Auditing.Services;

public interface AuditServiceInterface {
  Task<List<Audit>> GetAuditHistory(string entityType, string entityId);
  Task<List<string>> GetAllAuditedEntityTypes();
}
