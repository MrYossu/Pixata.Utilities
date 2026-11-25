using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Exceptions;

namespace Pixata.Pdf;

/// <summary>
/// Reads the text of a PDF, such as to check in a test that a document your app made says what it should: that a quote shows its
/// customer, lines and totals, or that a long report runs onto a second page
/// </summary>
public static class PdfText {
  /// <summary>
  /// Reads the text of each page, in order. On each page, the lines are in order from the top, separated by new lines (<c>\n</c>), and
  /// the words on a line are in order from the left, separated by spaces, so the cells of a table's row come out on one line
  /// </summary>
  /// <param name="pdf">The PDF's contents</param>
  /// <returns>The text of each page, the first page's first. A page with no text gives <c>""</c></returns>
  /// <exception cref="PdfTextException">The data isn't a PDF, is damaged, or needs a password to open</exception>
  public static List<string> Read(byte[] pdf) {
    using PdfDocument document = Open(pdf);
    return [.. document.GetPages().Select(PageText)];
  }

  /// <summary>
  /// Reads the text of each page, in order, as <see cref="Read(byte[])"/> does. The stream is read from its current position to the
  /// end, and isn't closed
  /// </summary>
  /// <param name="pdf">A stream with the PDF's contents</param>
  /// <param name="cancellationToken">Cancels reading the stream</param>
  /// <returns>The text of each page, the first page's first</returns>
  /// <exception cref="PdfTextException">The data isn't a PDF, is damaged, or needs a password to open</exception>
  public static async Task<List<string>> Read(Stream pdf, CancellationToken cancellationToken = default) =>
    Read(await ReadAll(pdf, cancellationToken));

  /// <summary>
  /// The number of pages in the PDF
  /// </summary>
  /// <param name="pdf">The PDF's contents</param>
  /// <exception cref="PdfTextException">The data isn't a PDF, is damaged, or needs a password to open</exception>
  public static int PageCount(byte[] pdf) {
    using PdfDocument document = Open(pdf);
    return document.NumberOfPages;
  }

  /// <summary>
  /// The number of pages in the PDF. The stream is read from its current position to the end, and isn't closed
  /// </summary>
  /// <param name="pdf">A stream with the PDF's contents</param>
  /// <param name="cancellationToken">Cancels reading the stream</param>
  /// <exception cref="PdfTextException">The data isn't a PDF, is damaged, or needs a password to open</exception>
  public static async Task<int> PageCount(Stream pdf, CancellationToken cancellationToken = default) =>
    PageCount(await ReadAll(pdf, cancellationToken));

  // A PDF starts with %PDF-, though readers allow some rubbish before it, as long as it's in the first 1024 bytes
  private static readonly byte[] Header = "%PDF-"u8.ToArray();

  private static PdfDocument Open(byte[] pdf) {
    ArgumentNullException.ThrowIfNull(pdf);
    if (pdf.AsSpan(0, Math.Min(pdf.Length, 1024)).IndexOf(Header) < 0) {
      throw new PdfTextException(pdf.Length == 0 ? "The PDF is empty" : "The data isn't a PDF, as it doesn't start with %PDF-");
    }
    try {
      PdfDocument document = PdfDocument.Open(pdf);
      // PdfPig only reads the pages when asked, so a damaged document is found here rather than half way through reading it
      _ = document.NumberOfPages;
      return document;
    }
    catch (PdfDocumentEncryptedException ex) {
      throw new PdfTextException("The PDF is encrypted, and needs a password to open", ex);
    }
    catch (Exception ex) when (ex is not OutOfMemoryException) {
      throw new PdfTextException($"The PDF couldn't be read: {ex.Message}", ex);
    }
  }

  private static async Task<byte[]> ReadAll(Stream pdf, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(pdf);
    // PdfPig needs the whole document, as a PDF's index is at its end
    using MemoryStream copy = new();
    await pdf.CopyToAsync(copy, cancellationToken);
    return copy.ToArray();
  }

  // The page's words, gathered into lines by their baselines. A PDF draws text wherever it likes, in any order (Chromium draws a table
  // a column at a time, for example), so the words are put back in the order they're read, rather than the order they're drawn
  private static string PageText(Page page) {
    List<Word> words = [.. page.GetWords().Where(word => !string.IsNullOrWhiteSpace(word.Text))];
    List<List<Word>> lines = [];
    List<Word>? line = null;
    double lineBaseline = 0;
    foreach (Word word in words.OrderByDescending(Baseline).ThenBy(word => word.BoundingBox.Left)) {
      double baseline = Baseline(word);
      // Words on the same line can sit a little apart, such as text in different fonts, so a word whose baseline is within a third of its
      // height of the line's joins it
      if (line is null || lineBaseline - baseline > Math.Max(1, Height(word) / 3)) {
        line = [];
        lines.Add(line);
        lineBaseline = baseline;
      }
      line.Add(word);
    }
    StringBuilder text = new();
    foreach (List<Word> lineWords in lines) {
      if (text.Length > 0) {
        text.Append('\n');
      }
      text.AppendJoin(' ', lineWords.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text));
    }
    return text.ToString();
  }

  private static double Baseline(Word word) =>
    word.Letters.Count > 0 ? word.Letters[0].StartBaseLine.Y : word.BoundingBox.Bottom;

  private static double Height(Word word) =>
    word.Letters.Count > 0 ? Math.Max(word.Letters[0].PointSize, word.BoundingBox.Height) : word.BoundingBox.Height;
}

/// <summary>
/// Thrown by <see cref="PdfText"/> when the data isn't a PDF, is damaged, or needs a password to open
/// </summary>
public class PdfTextException(string message, Exception? innerException = null) : Exception(message, innerException);
