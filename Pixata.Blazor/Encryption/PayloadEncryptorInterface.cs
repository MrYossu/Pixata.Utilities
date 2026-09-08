using System.Threading.Tasks;

namespace Pixata.Blazor.Encryption;

public interface PayloadEncryptorInterface {
  bool IsReady { get; }
  string? SessionId { get; }
  Task Initialise(string handshakeUrl, string siteId);
  Task<byte[]> Encrypt(byte[] plaintext);
  Task<byte[]> Decrypt(byte[] ciphertext);
}
