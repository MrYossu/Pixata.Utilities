using System.Security.Cryptography;

namespace Pixata.AspNetCore.Encryption;

public static class AesGcmEncryptor {
  private const int NonceSize = 12;
  private const int TagSize = 16;

  public static byte[] Encrypt(byte[] plaintext, byte[] key) {
    byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
    byte[] ciphertext = new byte[plaintext.Length];
    byte[] tag = new byte[TagSize];

    using AesGcm aes = new(key, TagSize);
    aes.Encrypt(nonce, plaintext, ciphertext, tag);

    byte[] result = new byte[NonceSize + ciphertext.Length + TagSize];
    nonce.CopyTo(result, 0);
    ciphertext.CopyTo(result, NonceSize);
    tag.CopyTo(result, NonceSize + ciphertext.Length);
    return result;
  }

  public static byte[] Decrypt(byte[] data, byte[] key) {
    Span<byte> nonce = data.AsSpan(0, NonceSize);
    Span<byte> tag = data.AsSpan(data.Length - TagSize);
    Span<byte> ciphertext = data.AsSpan(NonceSize, data.Length - NonceSize - TagSize);
    byte[] plaintext = new byte[ciphertext.Length];

    using AesGcm aes = new(key, TagSize);
    aes.Decrypt(nonce, ciphertext, tag, plaintext);
    return plaintext;
  }
}
