using System.Collections.Concurrent;

namespace Pixata.AspNetCore.Encryption;

public class SessionKeyStore {
  private readonly ConcurrentDictionary<string, byte[]> _keys = new();

  public void Store(string sessionId, byte[] key) =>
    _keys[sessionId] = key;

  public byte[]? Get(string sessionId) =>
    _keys.GetValueOrDefault(sessionId);
}
