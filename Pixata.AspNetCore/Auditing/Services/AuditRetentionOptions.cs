namespace Pixata.AspNetCore.Auditing.Services;

public class AuditRetentionOptions {
  /// <summary>
  /// The period after which audit entries will be automatically deleted.
  /// When null (default), audit entries are retained forever.
  /// </summary>
  public TimeSpan? RetentionPeriod { get; set; }

  /// <summary>
  /// How often the retention cleanup runs. Defaults to once per day.
  /// </summary>
  public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}
