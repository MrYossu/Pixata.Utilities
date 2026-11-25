using System.Text;

namespace Pixata.Pdf.Documents;

/// <summary>
/// Turns a document into a PDF: Word (<c>.docx</c>, and its templates and macro-enabled forms), RTF, HTML, text, or a picture
/// (PNG or JPEG), such as a CV uploaded in whatever format the person had. It keeps paragraphs, headings, bold, italic and
/// underlined text, lists, tables and pictures, and lays them out on A4 pages (or whatever size the options give). It's
/// written in .NET alone, with no Chromium, Office or other native dependency, so it runs on ordinary hosting
/// </summary>
public static class DocumentToPdf {
  /// <summary>
  /// The file extensions that can be converted, in lower case, with their dots
  /// </summary>
  public static IReadOnlyList<string> Extensions { get; } = [".docx", ".docm", ".dotx", ".dotm", ".rtf", ".html", ".htm", ".txt", ".png", ".jpg", ".jpeg", ".pdf"];

  /// <summary>
  /// Whether a file with this name (or extension, such as <c>.docx</c> or <c>docx</c>) can be converted
  /// </summary>
  public static bool CanConvert(string fileName) =>
    Extensions.Contains(Extension(fileName));

  /// <summary>
  /// Turns the file into a PDF
  /// </summary>
  /// <param name="file">The file's contents</param>
  /// <param name="fileName">The file's name, such as <c>CV.docx</c>, or just its extension, such as <c>.docx</c> or <c>docx</c>. What's
  /// in the file counts for more, so a Word document saved as RTF is read as RTF, whatever it's called</param>
  /// <param name="options">The page size, margins and fonts</param>
  /// <returns>The PDF. A PDF is returned as it is</returns>
  /// <exception cref="DocumentToPdfException">The file is empty, is a kind that can't be converted (such as a Word 97-2003
  /// <c>.doc</c>), isn't what its name says, or is damaged</exception>
  /// <exception cref="ArgumentOutOfRangeException">The options give a page with no width or height, or a margin less than 0</exception>
  public static byte[] Convert(byte[] file, string fileName, DocumentToPdfOptions? options = null) {
    ArgumentNullException.ThrowIfNull(file);
    if (file.Length == 0) {
      throw new DocumentToPdfException("The file is empty");
    }
    options ??= new();
    if (!(options.PageWidthMm > 0) || !(options.PageHeightMm > 0)) {
      throw new ArgumentOutOfRangeException(nameof(options), "The page's width and height must be more than 0");
    }
    if (!(options.MarginMm >= 0)) {
      throw new ArgumentOutOfRangeException(nameof(options), "The margin can't be less than 0");
    }
    string extension = Extension(fileName ?? "");
    if (StartsWith(file, "%PDF-"u8)) {
      return file;
    }
    List<DocBlock> blocks = Read(file, extension);
    try {
      return DocumentLayout.Render(blocks, options);
    }
    catch (Exception ex) when (ex is not OutOfMemoryException and not DocumentToPdfException) {
      throw new DocumentToPdfException($"The PDF couldn't be made: {ex.Message}", ex);
    }
  }

  /// <summary>
  /// Turns the file into a PDF, as <see cref="Convert(byte[], string, DocumentToPdfOptions?)"/> does. The stream is read from its
  /// current position to the end, and isn't closed
  /// </summary>
  public static async Task<byte[]> Convert(Stream file, string fileName, DocumentToPdfOptions? options = null, CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(file);
    using MemoryStream copy = new();
    await file.CopyToAsync(copy, cancellationToken);
    return Convert(copy.ToArray(), fileName, options);
  }

  private static string Extension(string fileName) {
    string trimmed = fileName.Trim();
    int dot = trimmed.LastIndexOf('.');
    return $".{(dot >= 0 ? trimmed[(dot + 1)..] : trimmed).ToLowerInvariant()}";
  }

  private static readonly string[] WordExtensions = [".docx", ".docm", ".dotx", ".dotm"];

  private static List<DocBlock> Read(byte[] file, string extension) {
    // What's in the file is checked first, as files get renamed, and Word saves RTF and web pages as .doc if asked
    if (Images.Read(file) is { } image) {
      image.Alignment = DocAlignment.Centre;
      return [image];
    }
    if (Images.IsPng(file) || Images.IsJpeg(file) || extension is ".png" or ".jpg" or ".jpeg") {
      throw new DocumentToPdfException("The picture couldn't be read. It may be damaged, or not really a PNG or JPEG");
    }
    if (IsRtf(file)) {
      return RtfReader.Read(file);
    }
    bool isZip = StartsWith(file, [0x50, 0x4b, 0x03, 0x04]);
    if (isZip && (WordExtensions.Contains(extension) || extension == ".doc")) {
      return WordReader.Read(file);
    }
    if (StartsWith(file, [0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1])) {
      // Word keeps a document with a password in the same kind of file as a Word 97-2003 document
      throw new DocumentToPdfException(WordExtensions.Contains(extension)
        ? "The Word document is protected with a password, so it can't be read"
        : extension is ".doc" or ".dot"
          ? "Word 97-2003 documents (.doc) can't be converted. Open it in Word and save it as a Word document (.docx)"
          : $"{Describe(extension)} can't be converted");
    }
    return extension switch {
      ".html" or ".htm" => HtmlReader.Read(file),
      ".txt" => PlainTextReader.Read(file),
      ".rtf" => throw new DocumentToPdfException("The file isn't an RTF document, as it doesn't start with {\\rtf"),
      ".docx" or ".docm" or ".dotx" or ".dotm" => throw new DocumentToPdfException("The file isn't a Word document. It may be damaged, or have the wrong name"),
      // A web page saved as .doc
      ".doc" when LooksLikeHtml(file) => HtmlReader.Read(file),
      ".doc" or ".dot" => throw new DocumentToPdfException("The file isn't a Word document that can be read"),
      _ => throw new DocumentToPdfException($"{Describe(extension)} can't be converted. Only {string.Join(", ", Extensions.SkipLast(1))} and {Extensions[^1]} files can")
    };
  }

  private static string Describe(string extension) =>
    extension == "." ? "Files without an extension" : $"{extension} files";

  private static bool StartsWith(byte[] file, ReadOnlySpan<byte> start) =>
    file.AsSpan().StartsWith(start);

  // RTF starts with {\rtf, perhaps after a byte order mark or white space
  private static bool IsRtf(byte[] file) {
    string start = Encoding.ASCII.GetString(file, 0, Math.Min(file.Length, 16)).TrimStart('\ufeff', ' ', '\r', '\n', '\t', '?');
    return start.StartsWith(@"{\rtf", StringComparison.Ordinal);
  }

  private static bool LooksLikeHtml(byte[] file) {
    string start = Encoding.UTF8.GetString(file, 0, Math.Min(file.Length, 1024)).TrimStart('\ufeff', ' ', '\r', '\n', '\t');
    return start.StartsWith('<');
  }
}
