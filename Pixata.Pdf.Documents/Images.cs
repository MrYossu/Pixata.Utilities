namespace Pixata.Pdf.Documents;

internal enum ImageFormat {
  Png,
  Jpeg
}

/// <summary>
/// Works out what a picture is, and its size in pixels, from its first bytes. PDFs can hold PNGs and JPEGs as they are, so those are
/// the only kinds that can be used
/// </summary>
internal static class Images {
  public static bool IsPng(ReadOnlySpan<byte> data) =>
    data.Length >= 8 && data[..8].SequenceEqual((ReadOnlySpan<byte>)[137, 80, 78, 71, 13, 10, 26, 10]);

  public static bool IsJpeg(ReadOnlySpan<byte> data) =>
    data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;

  /// <summary>
  /// The picture, if it's a PNG or JPEG whose size can be read, else null
  /// </summary>
  public static DocImage? Read(byte[] data) {
    if (IsPng(data) && data.Length >= 24) {
      int width = BigEndian(data, 16);
      int height = BigEndian(data, 20);
      return width > 0 && height > 0 ? new(data, ImageFormat.Png, width, height) : null;
    }
    if (IsJpeg(data) && JpegSize(data) is (int jpegWidth, int jpegHeight)) {
      return new(data, ImageFormat.Jpeg, jpegWidth, jpegHeight);
    }
    return null;
  }

  private static int BigEndian(byte[] data, int offset) =>
    data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];

  // The size is in the frame header (an SOF marker), which comes after any number of other segments
  private static (int Width, int Height)? JpegSize(byte[] data) {
    int index = 2;
    while (index + 4 <= data.Length) {
      if (data[index] != 0xff) {
        return null;
      }
      byte marker = data[index + 1];
      if (marker == 0xff) {
        index++;
        continue;
      }
      // Markers without a length
      if (marker is 0xd8 or 0x01 or >= 0xd0 and <= 0xd7) {
        index += 2;
        continue;
      }
      int length = data[index + 2] << 8 | data[index + 3];
      // SOF0 to SOF15, apart from DHT (C4), JPG (C8) and DAC (CC)
      if (marker is >= 0xc0 and <= 0xcf and not 0xc4 and not 0xc8 and not 0xcc) {
        if (index + 9 > data.Length) {
          return null;
        }
        int height = data[index + 5] << 8 | data[index + 6];
        int width = data[index + 7] << 8 | data[index + 8];
        return width > 0 && height > 0 ? (width, height) : null;
      }
      if (marker == 0xda || length < 2) {
        return null;
      }
      index += 2 + length;
    }
    return null;
  }
}
