using Microsoft.EntityFrameworkCore;
using Pixata.AspNetCore.Auditing.Models;

namespace Pixata.AspNetCore.Auditing.Services;

public class AuditService(DbContext context) : AuditServiceInterface {
  public async Task<List<Audit>> GetAuditHistory(string entityType, string entityId) =>
    await context.Set<Audit>()
      .Where(a => a.EntityType == entityType && a.EntityId == entityId)
      .OrderByDescending(a => a.ChangedAt)
      .ToListAsync();

  public async Task<List<string>> GetAllAuditedEntityTypes() =>
    await context.Set<Audit>()
      .Select(a => a.EntityType)
      .Distinct()
      .OrderBy(t => t)
      .ToListAsync();
}
