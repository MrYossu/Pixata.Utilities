namespace Pixata.AspNetCore.Auditing.Services;

public class AuditUserContext : AuditUserContextInterface {
  public string? UserIdentifier { get; set; }
}
