namespace Pixata.AspNetCore.Auditing.Services;

public interface AuditUserContextInterface {
  string? UserIdentifier { get; set; }
}
