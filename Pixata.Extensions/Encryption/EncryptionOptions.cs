namespace Pixata.Extensions.Encryption;

public class EncryptionOptions {
  public string SiteId { get; set; } = "default";
  public string HandshakePath { get; set; } = "/api/encryption/handshake";
  public string SessionHeader { get; set; } = "X-Encryption-Session";
  public string EncryptedContentType { get; set; } = "application/octet-stream";
}
