using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.AspNetCore.Auditing.Services;

public class AuditService : AuditServiceInterface {
  private readonly DbContext? _context;
  private readonly Func<DbContext>? _createContext;

  /// <summary>
  /// Reads the audit trail through the given context, which is owned by the caller
  /// </summary>
  public AuditService(DbContext context) =>
    _context = context;

  /// <summary>
  /// Creates a context for each read, and disposes of it afterwards. <c>AddAuditing&lt;TContext&gt;()</c> uses this when the app registers an
  /// <c>IDbContextFactory&lt;TContext&gt;</c>, as Microsoft recommend for Blazor Server, where a scoped context would live as long as the
  /// browser tab
  /// </summary>
  public AuditService(Func<DbContext> createContext) =>
    _createContext = createContext;

  public async Task<List<Audit>> GetAuditHistory(string entityType, string entityId) =>
    await Read(context => context.Set<Audit>()
      .AsNoTracking()
      .Where(a => a.EntityType == entityType && a.EntityId == entityId)
      .OrderByDescending(a => a.ChangedAt)
      .ToListAsync());

  public async Task<List<string>> GetAllAuditedEntityTypes() =>
    await Read(context => context.Set<Audit>()
      .Select(a => a.EntityType)
      .Distinct()
      .OrderBy(t => t)
      .ToListAsync());

  /// <summary>
  /// The IDs of the entities of the given type that have been audited. When every ID is a whole number, they are in numerical order (1, 2,
  /// 10 rather than 1, 10, 2), otherwise in the database's text order
  /// </summary>
  public async Task<List<string>> GetDistinctEntityIds(string entityType) {
    List<string> ids = await Read(context => context.Set<Audit>()
      .Where(a => a.EntityType == entityType)
      .Select(a => a.EntityId)
      .Distinct()
      .OrderBy(id => id)
      .ToListAsync());
    // The IDs are stored as text, so whole numbers come back from the database as 1, 10, 11, 2...
    return ids.All(id => long.TryParse(id, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
      ? ids.OrderBy(id => long.Parse(id, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)).ToList()
      : ids;
  }

  public async Task<List<Audit>> GetAuditsByEntityTypes(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user) {
    List<string> types = entityTypes.ToList();
    return await Read(context => {
      IQueryable<Audit> query = context.Set<Audit>().AsNoTracking().Where(a => types.Contains(a.EntityType));
      if (fromDate.HasValue) {
        query = query.Where(a => a.ChangedAt >= fromDate.Value);
      }
      if (toDate.HasValue) {
        query = query.Where(a => a.ChangedAt <= toDate.Value);
      }
      if (!string.IsNullOrWhiteSpace(user)) {
        query = query.Where(a => a.ChangedBy == user);
      }
      return query.OrderByDescending(a => a.ChangedAt).ToListAsync();
    });
  }

  private async Task<T> Read<T>(Func<DbContext, Task<T>> query) {
    if (_context is not null) {
      return await query(_context);
    }
    await using DbContext context = _createContext!();
    return await query(context);
  }
}
