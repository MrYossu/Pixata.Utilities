namespace Pixata.AspNetCore.Upload;

/// <summary>
/// Options for <see cref="UploadRequestExtensions.ReceiveUpload"/>
/// </summary>
public class UploadReceiveOptions {
  /// <summary>
  /// The form field that holds the file (or the chunk). Default <c>files</c>
  /// </summary>
  public string SaveField { get; set; } = "files";

  /// <summary>
  /// The form field that holds the JSON metadata sent with each chunk of a chunked upload. Default <c>chunkMetadata</c>
  /// </summary>
  public string MetadataField { get; set; } = "chunkMetadata";

  /// <summary>
  /// The file extensions (with the dot, eg <c>.pdf</c>) that are accepted. Empty (the default) accepts any extension. The
  /// comparison ignores case
  /// </summary>
  public List<string> AllowedExtensions { get; set; } = [];

  /// <summary>
  /// The largest file accepted, in bytes. Null (the default) means no limit, other than the ones the server imposes on the
  /// request size
  /// </summary>
  public long? MaxFileSize { get; set; }

  /// <summary>
  /// The smallest file accepted, in bytes. Null (the default) means no limit
  /// </summary>
  public long? MinFileSize { get; set; }

  /// <summary>
  /// When true (the default), a file with the same name as one already in the folder replaces it. When false, the new file
  /// is saved with a number added to its name, eg <c>report (1).pdf</c>
  /// </summary>
  public bool Overwrite { get; set; } = true;

  /// <summary>
  /// Chooses the name the file is saved under, given the (sanitised) name it was uploaded with. Whatever this returns is
  /// sanitised again, so it can't point outside the folder. When null, the uploaded name is used
  /// </summary>
  public Func<string, string>? FileName { get; set; }
}
