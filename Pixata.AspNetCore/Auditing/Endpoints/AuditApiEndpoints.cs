using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.AspNetCore.Auditing.Endpoints;

public static class AuditApiEndpoints {
  /// <summary>
  /// Maps the audit viewer API endpoints under the given route prefix.
  /// Returns a <see cref="RouteGroupBuilder"/> so the caller can chain
  /// additional configuration such as <c>.RequireAuthorization()</c>.
  /// </summary>
  /// <example>
  /// app.MapAuditApi("/api/audit").RequireAuthorization();
  /// </example>
  public static RouteGroupBuilder MapAuditApi(this IEndpointRouteBuilder app, string prefix) {
    RouteGroupBuilder group = app.MapGroup(prefix);

    group.MapGet("entity-types", async (AuditViewerServiceInterface service) =>
      Results.Ok(await service.GetEntityTypes()));

    group.MapGet("entity-ids", async (string entityType, AuditViewerServiceInterface service) =>
      Results.Ok(await service.GetDistinctEntityIds(entityType)));

    group.MapGet("history", async (
      string entityType,
      string entityId,
      DateTime? fromDate,
      DateTime? toDate,
      string? user,
      AuditOperation? operation,
      AuditViewerServiceInterface service) =>
      Results.Ok(await service.GetAuditHistory(entityType, entityId, fromDate, toDate, user, operation)));

    return group;
  }
}
