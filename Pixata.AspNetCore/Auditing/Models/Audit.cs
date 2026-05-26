namespace Pixata.AspNetCore.Auditing.Models;

public class Audit {
  public long Id { get; set; }
  public string EntityType { get; set; } = "";
  public string EntityId { get; set; } = "";
  public AuditOperation Operation { get; set; }
  public string ChangedBy { get; set; } = "";
  public DateTime ChangedAt { get; set; }
  public string FullSnapshot { get; set; } = "";
  public string? ChangedProperties { get; set; }
}
