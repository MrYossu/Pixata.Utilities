using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pixata.AspNetCore.Pdf.Playwright.Tests;

[TestClass]
public class BlankImage_Tests {
  // Issue #96: the image used in place of a blocked one was half-transparent blue, so each blocked image showed as a blue box in the PDF
  [TestMethod]
  public void BlankImage_IsOneFullyTransparentPixel() {
    (int width, int height, byte colourType, byte[] pixel) = DecodeSinglePixel(PlaywrightPdfConverter.BlankImage);
    Assert.AreEqual(1, width);
    Assert.AreEqual(1, height);
    byte alpha = colourType switch {
      4 => pixel[1], // Greyscale with alpha
      6 => pixel[3], // RGBA
      _ => throw new AssertFailedException($"PNG colour type {colourType} has no alpha channel, so it can't be transparent")
    };
    Assert.AreEqual(0, alpha);
  }

  // Just enough of a PNG decoder for a 1x1 8-bit image. With one pixel there is no left neighbour or previous row, so every filter type
  // leaves the bytes unchanged, and the filter byte can be skipped
  private static (int width, int height, byte colourType, byte[] pixel) DecodeSinglePixel(byte[] png) {
    int width = 0;
    int height = 0;
    byte colourType = 0;
    using MemoryStream idat = new();
    int position = 8;
    while (position < png.Length) {
      int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position));
      string type = Encoding.ASCII.GetString(png, position + 4, 4);
      ReadOnlySpan<byte> data = png.AsSpan(position + 8, length);
      if (type == "IHDR") {
        width = BinaryPrimitives.ReadInt32BigEndian(data);
        height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
        Assert.AreEqual(8, data[8], "Expected 8 bits per channel");
        colourType = data[9];
      } else if (type == "IDAT") {
        idat.Write(data);
      }
      position += length + 12;
    }
    idat.Position = 0;
    using ZLibStream zlib = new(idat, CompressionMode.Decompress);
    using MemoryStream raw = new();
    zlib.CopyTo(raw);
    return (width, height, colourType, raw.ToArray()[1..]);
  }
}
