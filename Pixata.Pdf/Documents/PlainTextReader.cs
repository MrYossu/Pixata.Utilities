using System.Text;

namespace Pixata.Pdf.Documents;

/// <summary>
/// Reads a text file: a paragraph for each line, with blank lines kept
/// </summary>
internal static class PlainTextReader {
  public static List<DocBlock> Read(byte[] file) {
    string text = Decode(file, null);
    List<DocBlock> blocks = [];
    foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
      DocParagraph paragraph = new() { SpaceAfter = 0 };
      string[] parts = line.Split('\t');
      for (int index = 0; index < parts.Length; index++) {
        if (index > 0) {
          paragraph.Inlines.Add(new DocTab(default));
        }
        if (parts[index].Length > 0) {
          paragraph.Inlines.Add(new DocText(parts[index], default));
        }
      }
      blocks.Add(paragraph);
    }
    // The line break at the end of the file doesn't make another line
    while (blocks.Count > 1 && blocks[^1] is DocParagraph { Inlines.Count: 0 }) {
      blocks.RemoveAt(blocks.Count - 1);
    }
    return blocks;
  }

  /// <summary>
  /// The file as text: in the encoding its byte order mark gives, else the one named (such as by an HTML page's meta tag), else
  /// UTF-8 if it's valid UTF-8, else Windows-1252, which is what older Windows programs wrote
  /// </summary>
  public static string Decode(byte[] file, string? charset) {
    if (file.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xef, 0xbb, 0xbf])) {
      return Encoding.UTF8.GetString(file, 3, file.Length - 3);
    }
    if (file.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xff, 0xfe])) {
      return Encoding.Unicode.GetString(file, 2, file.Length - 2);
    }
    if (file.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xfe, 0xff])) {
      return Encoding.BigEndianUnicode.GetString(file, 2, file.Length - 2);
    }
    if (charset is not null && GetEncoding(charset) is { } named) {
      return named.GetString(file);
    }
    try {
      return new UTF8Encoding(false, true).GetString(file);
    }
    catch (DecoderFallbackException) {
      return Windows1252.GetString(file);
    }
  }

  public static Encoding Windows1252 { get; } = CodePagesEncodingProvider.Instance.GetEncoding(1252) ?? Encoding.Latin1;

  public static Encoding? GetEncoding(string name) {
    try {
      return CodePagesEncodingProvider.Instance.GetEncoding(name) ?? Encoding.GetEncoding(name);
    }
    catch (ArgumentException) {
      return null;
    }
  }

  public static Encoding? GetEncoding(int codePage) {
    try {
      return CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
    }
    catch (Exception ex) when (ex is ArgumentException or NotSupportedException) {
      return null;
    }
  }
}
