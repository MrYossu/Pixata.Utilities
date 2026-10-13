namespace Pixata.Blazor.Components;

/// <summary>
/// The text that <see cref="PixataFileSelect"/> and <see cref="PixataUpload"/> show. Pass your own to the <c>Messages</c>
/// parameter to translate or reword it
/// </summary>
public class PixataUploadMessages {
  public string SelectFiles { get; set; } = "Select files...";
  public string FileSelectDropZoneHint { get; set; } = "Drag and drop files here";
  public string UploadDropZoneHint { get; set; } = "Drag and drop files here to upload";
  public string InvalidFileExtension { get; set; } = "File type not allowed.";
  public string InvalidMaxFileSize { get; set; } = "File size too large.";
  public string InvalidMinFileSize { get; set; } = "File size too small.";
  public string Remove { get; set; } = "Remove";
  public string Cancel { get; set; } = "Cancel";
  public string Retry { get; set; } = "Retry";
  public string Pause { get; set; } = "Pause";
  public string Resume { get; set; } = "Resume";
  public string Clear { get; set; } = "Clear";
  public string Upload { get; set; } = "Upload";
  public string StatusUploading { get; set; } = "Uploading...";
  public string StatusDone { get; set; } = "Done";
  public string FileUploaded { get; set; } = "File successfully uploaded.";
  public string FileFailed { get; set; } = "File failed to upload.";

  /// <summary>
  /// Read by screen readers on the select button when there are files in the list. <c>{0}</c> is the number of files
  /// </summary>
  public string FilesAriaLabel { get; set; } = "{0} file(s) in the list. Press Arrow Down to move through them.";

  /// <summary>
  /// The units for file sizes, starting with kilobytes
  /// </summary>
  public string[] SizeUnits { get; set; } = ["KB", "MB", "GB", "TB"];

  internal static PixataUploadMessages Default { get; } = new();
}
