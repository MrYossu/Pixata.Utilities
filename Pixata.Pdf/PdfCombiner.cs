using Pixata.Extensions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace Pixata.Pdf;

/// <summary>
/// Combines several PDFs into one, such as a cover page made with <c>DocumentTemplateHelper.CreatePdfFromTemplate</c> followed by a
/// document the user uploaded. Each page keeps its own size and orientation
/// </summary>
public static class PdfCombiner {
  /// <summary>
  /// Combines the PDFs, in the order given, into one
  /// </summary>
  /// <param name="pdfs">The PDFs' contents</param>
  /// <returns>The combined PDF. If one of the PDFs can't be read, such as one that's damaged, or protected with a password, the response is
  /// a <see cref="ApiResponseStates.Failure"/> that says which one (counting from 1) and why</returns>
  public static ApiResponse<byte[]> Combine(IEnumerable<byte[]> pdfs) {
    List<byte[]> documents = [.. pdfs];
    if (documents.Count == 0) {
      return new(ApiResponseStates.Failure, Message: "There are no PDFs to combine");
    }
    using PdfDocumentBuilder builder = new();
    for (int index = 0; index < documents.Count; index++) {
      try {
        using PdfDocument document = PdfDocument.Open(documents[index]);
        for (int page = 1; page <= document.NumberOfPages; page++) {
          builder.AddPage(document, page);
        }
      }
      catch (Exception ex) when (ex is not OutOfMemoryException) {
        return new(ApiResponseStates.Failure, Message: $"PDF {index + 1} of {documents.Count} couldn't be read: {ex.Message}");
      }
    }
    try {
      return new(ApiResponseStates.Success, builder.Build());
    }
    catch (Exception ex) when (ex is not OutOfMemoryException) {
      return new(ApiResponseStates.Failure, Message: $"The combined PDF couldn't be written: {ex.Message}");
    }
  }

  /// <summary>
  /// Combines the PDFs, in the order given, into one
  /// </summary>
  /// <param name="pdfs">The PDFs' contents</param>
  /// <returns>The combined PDF, or a <see cref="ApiResponseStates.Failure"/> that says which PDF couldn't be read and why</returns>
  public static ApiResponse<byte[]> Combine(params byte[][] pdfs) =>
    Combine((IEnumerable<byte[]>)pdfs);

  /// <summary>
  /// Combines the PDFs, in the order given, into one. Each stream is read from its current position to the end, and isn't closed
  /// </summary>
  /// <param name="pdfs">Streams with the PDFs' contents, such as uploaded files, or files opened from disk</param>
  /// <param name="cancellationToken">Cancels reading the streams</param>
  /// <returns>The combined PDF, or a <see cref="ApiResponseStates.Failure"/> that says which PDF couldn't be read and why</returns>
  public static async Task<ApiResponse<byte[]>> Combine(IEnumerable<Stream> pdfs, CancellationToken cancellationToken = default) {
    // PdfPig needs the whole of each document, as a PDF's index is at its end
    List<byte[]> documents = [];
    foreach (Stream pdf in pdfs) {
      using MemoryStream copy = new();
      await pdf.CopyToAsync(copy, cancellationToken);
      documents.Add(copy.ToArray());
    }
    return Combine(documents);
  }
}
