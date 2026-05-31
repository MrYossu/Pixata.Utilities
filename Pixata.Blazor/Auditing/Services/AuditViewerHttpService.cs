using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Pixata.Extensions.Auditing.Models;
using Pixata.Extensions.Auditing.Services;

namespace Pixata.Blazor.Auditing.Services;

public class AuditViewerHttpService(HttpClient httpClient) : AuditViewerServiceInterface {
  public async Task<List<EntityTypeMetadata>> GetEntityTypes() =>
    await httpClient.GetFromJsonAsync<List<EntityTypeMetadata>>("entity-types") ?? [];

  public async Task<List<string>> GetDistinctEntityIds(string entityType) =>
    await httpClient.GetFromJsonAsync<List<string>>($"entity-ids?entityType={Uri.EscapeDataString(entityType)}") ?? [];

  public async Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId) =>
    await httpClient.GetFromJsonAsync<List<AuditEntryViewModel>>($"history?entityType={Uri.EscapeDataString(entityType)}&entityId={Uri.EscapeDataString(entityId)}") ?? [];

  public async Task<List<AuditEntryViewModel>> GetAuditHistory(string entityType, string entityId, DateTime? fromDate, DateTime? toDate, string? user, AuditOperation? operation) {
    List<string> queryParts = [
      $"entityType={Uri.EscapeDataString(entityType)}",
      $"entityId={Uri.EscapeDataString(entityId)}"
    ];
    if (fromDate.HasValue) {
      queryParts.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
    }
    if (toDate.HasValue) {
      queryParts.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
    }
    if (!string.IsNullOrWhiteSpace(user)) {
      queryParts.Add($"user={Uri.EscapeDataString(user)}");
    }
    if (operation.HasValue) {
      queryParts.Add($"operation={Uri.EscapeDataString(operation.Value.ToString())}");
    }
    string url = $"history?{string.Join("&", queryParts)}";
    return await httpClient.GetFromJsonAsync<List<AuditEntryViewModel>>(url) ?? [];
  }

  public async Task<List<AuditEntryViewModel>> GetAllAuditEntries(IEnumerable<string> entityTypes, DateTime? fromDate, DateTime? toDate, string? user) {
    List<string> queryParts = entityTypes.Select(et => $"entityType={Uri.EscapeDataString(et)}").ToList();
    if (fromDate.HasValue) {
      queryParts.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
    }
    if (toDate.HasValue) {
      queryParts.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
    }
    if (!string.IsNullOrWhiteSpace(user)) {
      queryParts.Add($"user={Uri.EscapeDataString(user)}");
    }
    string url = $"all-entries?{string.Join("&", queryParts)}";
    return await httpClient.GetFromJsonAsync<List<AuditEntryViewModel>>(url) ?? [];
  }
}
