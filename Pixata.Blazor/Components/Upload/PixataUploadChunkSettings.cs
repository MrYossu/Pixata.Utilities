using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// Has a <see cref="PixataUpload"/> send each file in chunks, one request per chunk, so that large files can get past a
/// server's request size limit, a failed chunk can be sent again, and uploads can be paused and resumed. Put it inside the
/// upload's <c>UploadSettings</c>. Each chunk carries JSON metadata (the file's id, name, size and content type, and the
/// chunk's index, the number of chunks and its offset) that the server uses to put the file back together
/// </summary>
public class PixataUploadChunkSettings : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataUpload? Upload { get; set; }

  /// <summary>
  /// When false, files are sent whole, as if this component wasn't there. Default true
  /// </summary>
  [Parameter]
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// The size of each chunk, in bytes. Default 1 MB
  /// </summary>
  [Parameter]
  public long Size { get; set; } = 1024 * 1024;

  /// <summary>
  /// How long to wait before sending a failed chunk again, in milliseconds. Default 100
  /// </summary>
  [Parameter]
  public int AutoRetryAfter { get; set; } = 100;

  /// <summary>
  /// How many times a failed chunk is sent again before the upload is marked as failed. Default 1
  /// </summary>
  [Parameter]
  public int MaxAutoRetries { get; set; } = 1;

  /// <summary>
  /// When true (the default), the user can pause and resume uploads
  /// </summary>
  [Parameter]
  public bool Resumable { get; set; } = true;

  /// <summary>
  /// The form field the chunk's JSON metadata is sent in. Default <c>chunkMetadata</c>
  /// </summary>
  [Parameter]
  public string MetadataField { get; set; } = "chunkMetadata";

  protected override void OnInitialized() {
    if (Upload is null) {
      throw new InvalidOperationException($"{nameof(PixataUploadChunkSettings)} has to be inside the UploadSettings of a {nameof(PixataUpload)}");
    }
    Upload.ChunkSettings = this;
  }

  public void Dispose() {
    if (Upload is not null && ReferenceEquals(Upload.ChunkSettings, this)) {
      Upload.ChunkSettings = null;
    }
    GC.SuppressFinalize(this);
  }
}
