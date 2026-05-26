namespace Pixata.Blazor.Auditing.Models;

public class PropertyChange {
  public string PropertyName { get; set; } = "";
  public string? OldValue { get; set; }
  public string? NewValue { get; set; }
}
