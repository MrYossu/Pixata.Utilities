using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataFileSelect"/> and <see cref="PixataUpload"/> know about a file in their list
/// </summary>
public abstract class PixataFileInfoBase {
  /// <summary>
  /// Identifies the file. Files the user picks get a generated id; for files you pass in through the <c>Files</c> parameter,
  /// set it yourself (one is generated if you don't). Pass it to methods such as <c>RemoveFile()</c>
  /// </summary>
  public string Id { get; set; } = "";

  /// <summary>
  /// The file name, including the extension for files the user picks
  /// </summary>
  public string Name { get; set; } = "";

  /// <summary>
  /// The size of the file, in bytes
  /// </summary>
  public long Size { get; set; }

  /// <summary>
  /// The file's extension, including the dot, eg <c>.pdf</c>
  /// </summary>
  public string Extension { get; set; } = "";

  /// <summary>
  /// The content (MIME) type the browser gave for the file, eg <c>image/png</c>. Empty if the browser didn't know
  /// </summary>
  public string ContentType { get; set; } = "";

  /// <summary>
  /// When the file was last changed, if the browser said
  /// </summary>
  public DateTimeOffset? LastModified { get; set; }

  /// <summary>
  /// True if <c>AllowedExtensions</c> is set and doesn't include the file's extension
  /// </summary>
  public bool InvalidExtension { get; set; }

  /// <summary>
  /// True if the file is smaller than <c>MinFileSize</c>
  /// </summary>
  public bool InvalidMinFileSize { get; set; }

  /// <summary>
  /// True if the file is larger than <c>MaxFileSize</c>
  /// </summary>
  public bool InvalidMaxFileSize { get; set; }

  /// <summary>
  /// True if the file passed validation (its extension and size are allowed)
  /// </summary>
  public bool IsValid =>
    !InvalidExtension && !InvalidMinFileSize && !InvalidMaxFileSize;

  /// <summary>
  /// The id the browser side knows the file by, or null for files passed in through the <c>Files</c> parameter
  /// </summary>
  internal string? BrowserFileId { get; set; }
}

/// <summary>
/// A file in a <see cref="PixataFileSelect"/>, with access to its contents
/// </summary>
public class PixataFileSelectFileInfo : PixataFileInfoBase {
  private Func<CancellationToken, ValueTask<IJSStreamReference>>? _getFile;
  private Stream? _stream;

  /// <summary>
  /// The file's contents, read from the browser as you go. It is null for files passed in through the <c>Files</c> parameter.
  /// The stream has to be read asynchronously (eg with <c>CopyToAsync()</c> or <c>ReadAsync()</c>), and can only be read once.
  /// Call <see cref="OpenReadStream"/> if you need to read the file again. Read the file before it is removed from the list,
  /// as the browser lets go of it then
  /// </summary>
  public Stream? Stream =>
    _getFile is null ? null : _stream ??= OpenReadStream(Size);

  /// <summary>
  /// Opens a new stream to read the file's contents from the browser. Like <see cref="Stream"/>, it has to be read
  /// asynchronously
  /// </summary>
  /// <param name="maxAllowedSize">The largest file you're prepared to read, in bytes. Defaults to 500 KB</param>
  /// <param name="cancellationToken">Cancels opening the file</param>
  /// <exception cref="IOException">The file is larger than <paramref name="maxAllowedSize"/></exception>
  /// <exception cref="InvalidOperationException">The file was passed in through the <c>Files</c> parameter, so the browser doesn't have it</exception>
  public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
    if (_getFile is null) {
      throw new InvalidOperationException($"The browser doesn't have the contents of '{Name}', as it was passed in through the Files parameter rather than picked by the user");
    }
    if (Size > maxAllowedSize) {
      throw new IOException($"'{Name}' is {Size} bytes, which is more than the {maxAllowedSize} bytes allowed");
    }
    return new PixataFileStream(Size, maxAllowedSize, _getFile, cancellationToken);
  }

  internal void SetSource(Func<CancellationToken, ValueTask<IJSStreamReference>> getFile) =>
    _getFile = getFile;
}

/// <summary>
/// A file in a <see cref="PixataUpload"/>, with its upload status and progress
/// </summary>
public class PixataUploadFileInfo : PixataFileInfoBase {
  /// <summary>
  /// How much of the file has been uploaded, as a percentage (0 to 100)
  /// </summary>
  public int Progress { get; set; }

  /// <summary>
  /// Where the file is in the upload process. Files you pass in through the <c>Files</c> parameter are treated as already on
  /// the server, so are shown as <see cref="PixataUploadFileStatus.Blank"/> unless you set <see cref="PixataUploadFileStatus.Uploaded"/>
  /// or <see cref="PixataUploadFileStatus.Failed"/>
  /// </summary>
  public PixataUploadFileStatus Status { get; set; } = PixataUploadFileStatus.Blank;

  /// <summary>
  /// The browser-side request uploading the file, while it is being uploaded
  /// </summary>
  internal string? RequestId { get; set; }
}

/// <summary>
/// Where a file is in the upload process
/// </summary>
public enum PixataUploadFileStatus {
  /// <summary>
  /// Picked by the user, but not uploaded yet
  /// </summary>
  Selected,

  /// <summary>
  /// Uploaded successfully
  /// </summary>
  Uploaded,

  /// <summary>
  /// Being uploaded
  /// </summary>
  Uploading,

  /// <summary>
  /// The upload failed (the server returned an error, or couldn't be reached)
  /// </summary>
  Failed,

  /// <summary>
  /// Passed in through the <c>Files</c> parameter, so already on the server
  /// </summary>
  Blank,

  /// <summary>
  /// A chunked upload that the user has paused
  /// </summary>
  Paused
}

/// <summary>
/// The kind of request that succeeded or failed
/// </summary>
public enum PixataUploadOperationType {
  Upload,
  Remove
}
