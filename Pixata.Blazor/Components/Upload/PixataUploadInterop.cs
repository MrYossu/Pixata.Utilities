using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A file the user picked or dropped, as described by pixata-upload.js
/// </summary>
internal sealed class PixataUploadBrowserFile {
  public string Id { get; set; } = "";
  public string Name { get; set; } = "";
  public long Size { get; set; }
  public string ContentType { get; set; } = "";
  public double LastModified { get; set; }
}

/// <summary>
/// A request for pixata-upload.js to send: an upload (<see cref="FileId"/> set) or a remove (<see cref="Values"/> set)
/// </summary>
internal sealed class PixataUploadBrowserRequest {
  public string Url { get; set; } = "";
  public string Field { get; set; } = "";
  public string? FileId { get; set; }
  public string? UploadId { get; set; }
  public List<string>? Values { get; set; }
  public Dictionary<string, string> Headers { get; set; } = [];
  public Dictionary<string, string> Data { get; set; } = [];
  public bool WithCredentials { get; set; }
  public PixataUploadBrowserChunk? Chunk { get; set; }
}

internal sealed class PixataUploadBrowserChunk {
  public long Size { get; set; }
  public int AutoRetryAfter { get; set; }
  public int MaxAutoRetries { get; set; }
  public string MetadataField { get; set; } = "";
}

/// <summary>
/// How a request sent by pixata-upload.js turned out
/// </summary>
internal sealed class PixataUploadBrowserResult {
  public int Status { get; set; }
  public string StatusText { get; set; } = "";
  public string ResponseType { get; set; } = "";
  public string ResponseText { get; set; } = "";
  public bool Success { get; set; }
  public bool Cancelled { get; set; }

  public PixataUploadHttpRequest ToRequest() =>
    new() { Status = Status, StatusText = StatusText, ResponseType = ResponseType, ResponseText = ResponseText };

  public static PixataUploadBrowserResult CancelledResult { get; } = new() { StatusText = "Cancelled", Cancelled = true };
}

/// <summary>
/// The methods pixata-upload.js calls. They are on this class rather than on the components, so they aren't part of the
/// components' public API
/// </summary>
internal sealed class PixataUploadCallbacks(
  Func<PixataUploadBrowserFile[], Task> filesSelected,
  Func<string, int, Task> requestProgress,
  Func<string, PixataUploadBrowserResult, Task> requestComplete) {
  [JSInvokable]
  public Task OnFilesSelected(PixataUploadBrowserFile[] files) =>
    filesSelected(files);

  [JSInvokable]
  public Task OnRequestProgress(string requestId, int percent) =>
    requestProgress(requestId, percent);

  [JSInvokable]
  public Task OnRequestComplete(string requestId, PixataUploadBrowserResult result) =>
    requestComplete(requestId, result);
}
