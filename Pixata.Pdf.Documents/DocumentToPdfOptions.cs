namespace Pixata.Pdf.Documents;

/// <summary>
/// How <see cref="DocumentToPdf"/> lays out the PDF
/// </summary>
public class DocumentToPdfOptions {
  /// <summary>
  /// The width of the pages, in millimetres. Defaults to 210, which is A4
  /// </summary>
  public double PageWidthMm { get; set; } = 210;

  /// <summary>
  /// The height of the pages, in millimetres. Defaults to 297, which is A4
  /// </summary>
  public double PageHeightMm { get; set; } = 297;

  /// <summary>
  /// The margin on each side of the pages, in millimetres. Defaults to 20
  /// </summary>
  public double MarginMm { get; set; } = 20;

  /// <summary>
  /// The size of text whose document doesn't give one, in points. Defaults to 11
  /// </summary>
  public double FontSize { get; set; } = 11;

  /// <summary>
  /// A TrueType font (the contents of a <c>.ttf</c> file) for the text, in place of Liberation Sans, which comes with the
  /// package. It's embedded in the PDF, so check that its licence allows that
  /// </summary>
  public byte[]? Font { get; set; }

  /// <summary>
  /// The bold, italic and bold italic faces of <see cref="Font"/>. Any that aren't given use <see cref="Font"/> (or the bold face,
  /// for bold italic). They're ignored without <see cref="Font"/>
  /// </summary>
  public byte[]? BoldFont { get; set; }

  /// <inheritdoc cref="BoldFont"/>
  public byte[]? ItalicFont { get; set; }

  /// <inheritdoc cref="BoldFont"/>
  public byte[]? BoldItalicFont { get; set; }
}

/// <summary>
/// Thrown by <see cref="DocumentToPdf"/> when a file can't be turned into a PDF: it's a kind it can't read, isn't what its name
/// says, or is damaged. The message says which, and can be shown to the user
/// </summary>
public class DocumentToPdfException(string message, Exception? innerException = null) : Exception(message, innerException);
