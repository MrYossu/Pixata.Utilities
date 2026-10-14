using System;

namespace Pixata.Extensions.Auditing.Models;

public class Audit {
  /// <summary>
  /// The value written in place of a property that has been left out of the audit trail, either with <c>[NoAudit]</c> on the property, or with
  /// <c>ExcludeProperty</c> in the auditing options
  /// </summary>
  public const string HiddenValue = "(hidden)";

  public long Id { get; set; }
  public string EntityType { get; set; } = "";
  public string EntityId { get; set; } = "";
  public AuditOperation Operation { get; set; }
  public string ChangedBy { get; set; } = "";
  public DateTime ChangedAt { get; set; }
  public string FullSnapshot { get; set; } = "";
  public string? ChangedProperties { get; set; }
}
