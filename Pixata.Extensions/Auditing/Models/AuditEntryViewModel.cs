using System;
using System.Collections.Generic;

namespace Pixata.Extensions.Auditing.Models;

public class AuditEntryViewModel {
  public long Id { get; set; }
  public AuditOperation Operation { get; set; }
  public string ChangedBy { get; set; } = "";
  public DateTime ChangedAt { get; set; }
  public Dictionary<string, object?> Properties { get; set; } = [];
  public List<PropertyChange> ChangedProperties { get; set; } = [];
}
