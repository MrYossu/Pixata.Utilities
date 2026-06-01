using Microsoft.EntityFrameworkCore;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

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

  public async Task<List<string>> GetDistinctEntityIds(string entityType) =>
    await context.Set<Audit>()
      .Where(a => a.EntityType == entityType)
      .Select(a => a.EntityId)
      .Distinct()
      .OrderBy(id => id)
      .ToListAsync();

  public async Task<List<Audit>> GetAuditsByEntityTypes(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user) {
    List<string> types = entityTypes.ToList();
    IQueryable<Audit> query = context.Set<Audit>().Where(a => types.Contains(a.EntityType));
    if (fromDate.HasValue) {
      query = query.Where(a => a.ChangedAt >= fromDate.Value);
    }
    if (toDate.HasValue) {
      query = query.Where(a => a.ChangedAt <= toDate.Value);
    }
    if (!string.IsNullOrWhiteSpace(user)) {
      query = query.Where(a => a.ChangedBy == user);
    }
    return await query.OrderByDescending(a => a.ChangedAt).ToListAsync();
  }
}