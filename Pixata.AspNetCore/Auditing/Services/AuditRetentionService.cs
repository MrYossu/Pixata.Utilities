using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pixata.AspNetCore.Auditing.Models;

namespace Pixata.AspNetCore.Auditing.Services;

public class AuditRetentionService(IServiceScopeFactory scopeFactory, AuditRetentionOptions options, ILogger<AuditRetentionService> logger) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    if (!options.RetentionPeriod.HasValue) {
      return;
    }

    while (!stoppingToken.IsCancellationRequested) {
      try {
        await CleanupOldAuditEntries(stoppingToken);
      } catch (Exception ex) {
        logger.LogError(ex, "Error cleaning up old audit entries");
      }

      await Task.Delay(options.CleanupInterval, stoppingToken);
    }
  }

  private async Task CleanupOldAuditEntries(CancellationToken stoppingToken) {
    using IServiceScope scope = scopeFactory.CreateScope();
    DbContext context = scope.ServiceProvider.GetRequiredService<DbContext>();
    DateTime cutoff = DateTime.UtcNow - options.RetentionPeriod!.Value;

    int deleted = await context.Set<Audit>()
      .Where(a => a.ChangedAt < cutoff)
      .ExecuteDeleteAsync(stoppingToken);

    if (deleted > 0) {
      logger.LogInformation("Deleted {Count} audit entries older than {Cutoff}", deleted, cutoff);
    }
  }
}
