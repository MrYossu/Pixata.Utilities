namespace Pixata.AspNetCore.Upload;

/// <summary>
/// What <see cref="UploadRequestExtensions.ReceiveUpload"/> did with a request
/// </summary>
public class UploadReceiveResult {
  /// <summary>
  /// The files this request completed. For a chunked upload, this is empty until the last chunk arrives
  /// </summary>
  public List<ReceivedFile> Files { get; init; } = [];

  /// <summary>
  /// The chunk's metadata, if the request was one chunk of a chunked upload, otherwise null
  /// </summary>
  public UploadChunkMetadata? Chunk { get; init; }

  /// <summary>
  /// Why the request was rejected, or null if it was accepted. Return a 400 (Bad Request) when this is set, so the
  /// component shows the file as failed
  /// </summary>
  public string? Error { get; init; }

  /// <summary>
  /// True if the request was accepted
  /// </summary>
  public bool Success =>
    Error is null;

  /// <summary>
  /// True if the request was accepted and finished a file: a plain upload, or the last chunk of a chunked one
  /// </summary>
  public bool Complete =>
    Success && (Chunk is null || Chunk.IsLast);

  internal static UploadReceiveResult Failed(string error, UploadChunkMetadata? chunk = null) =>
    new() { Error = error, Chunk = chunk };
}

/// <summary>
/// A file that has been saved
/// </summary>
/// <param name="FileName">The name the file was saved under</param>
/// <param name="OriginalFileName">The name the file was uploaded with</param>
/// <param name="Path">The full path of the saved file</param>
/// <param name="Size">The size of the file, in bytes</param>
/// <param name="ContentType">The content (MIME) type the browser gave for the file</param>
public record ReceivedFile(string FileName, string OriginalFileName, string Path, long Size, string ContentType);

/// <summary>
/// The JSON metadata the browser sends with each chunk of a chunked upload
/// </summary>
public class UploadChunkMetadata {
  /// <summary>
  /// Identifies the file being uploaded, so the chunks can be put back together
  /// </summary>
  public string FileId { get; set; } = "";

  public string FileName { get; set; } = "";

  /// <summary>
  /// The size of the whole file, in bytes
  /// </summary>
  public long FileSize { get; set; }

  public string ContentType { get; set; } = "";

  /// <summary>
  /// The (zero-based) index of this chunk
  /// </summary>
  public int ChunkIndex { get; set; }

  public int TotalChunks { get; set; }

  /// <summary>
  /// Where this chunk starts in the file, in bytes. When it is sent, a chunk that is sent again (because the response to the
  /// first attempt was lost) overwrites itself rather than being added twice
  /// </summary>
  public long? Offset { get; set; }

  /// <summary>
  /// True if this is the last chunk of the file
  /// </summary>
  public bool IsLast =>
    ChunkIndex >= TotalChunks - 1;
}
