using System.Collections.Generic;

namespace Pixata.Blazor.Components;

/// <summary>
/// The arguments for <see cref="PixataFileSelect"/>'s <c>OnSelect</c> and <c>OnRemove</c> events
/// </summary>
public class PixataFileSelectEventArgs {
  /// <summary>
  /// The files being selected or removed. Files that failed validation are included, so check
  /// <see cref="PixataFileInfoBase.IsValid"/> (or <c>InvalidExtension</c> and the others) before using one
  /// </summary>
  public List<PixataFileSelectFileInfo> Files { get; set; } = [];

  /// <summary>
  /// Set to true to stop the files being added to (or removed from) the list
  /// </summary>
  public bool IsCancelled { get; set; }
}

/// <summary>
/// The base for the arguments of <see cref="PixataUpload"/>'s events
/// </summary>
public class PixataUploadEventArgsBase {
  /// <summary>
  /// The files the event is about
  /// </summary>
  public List<PixataUploadFileInfo> Files { get; set; } = [];
}

/// <summary>
/// The arguments for an event that can be cancelled
/// </summary>
public class PixataUploadCancelableEventArgs : PixataUploadEventArgsBase {
  /// <summary>
  /// Set to true to stop the action going ahead
  /// </summary>
  public bool IsCancelled { get; set; }
}

/// <summary>
/// The arguments for <c>OnSelect</c>, raised when the user picks or drops files. Files that failed validation are included.
/// Cancelling the event stops the files being added to the list
/// </summary>
public class PixataUploadSelectEventArgs : PixataUploadCancelableEventArgs;

/// <summary>
/// The arguments for <c>OnUpload</c> and <c>OnRemove</c>, raised just before a request is sent to the save or remove URL.
/// Add headers (eg an authorisation token) and form fields to send with the request, or cancel it
/// </summary>
public class PixataUploadEventArgs : PixataUploadCancelableEventArgs {
  /// <summary>
  /// Headers to send with the request, eg <c>RequestHeaders["Authorization"] = $"Bearer {token}"</c>
  /// </summary>
  public Dictionary<string, object> RequestHeaders { get; set; } = [];

  /// <summary>
  /// Extra form fields to send with the request. The values are sent as strings
  /// </summary>
  public Dictionary<string, object> RequestData { get; set; } = [];
}

/// <summary>
/// The arguments for <c>OnProgress</c>, raised as a file uploads
/// </summary>
public class PixataUploadProgressEventArgs : PixataUploadEventArgsBase {
  /// <summary>
  /// How much of the file has been uploaded, as a percentage (0 to 100)
  /// </summary>
  public int Progress { get; set; }
}

/// <summary>
/// The arguments for <c>OnCancel</c>, raised when the user cancels an upload. Cancelling the event lets the upload carry on
/// </summary>
public class PixataUploadCancelEventArgs : PixataUploadCancelableEventArgs;

/// <summary>
/// The arguments for <c>OnClear</c>, raised when the user clicks the Clear button. Cancelling the event keeps the files
/// </summary>
public class PixataUploadClearEventArgs : PixataUploadCancelableEventArgs;

/// <summary>
/// The arguments for <c>OnPause</c>, raised when the user pauses a chunked upload
/// </summary>
public class PixataUploadPauseEventArgs : PixataUploadCancelableEventArgs;

/// <summary>
/// The arguments for <c>OnResume</c>, raised when the user resumes a paused chunked upload
/// </summary>
public class PixataUploadResumeEventArgs : PixataUploadCancelableEventArgs;

/// <summary>
/// The base for the arguments of <c>OnSuccess</c> and <c>OnError</c>
/// </summary>
public class PixataUploadRequestResultEventArgs : PixataUploadEventArgsBase {
  /// <summary>
  /// Whether the request was an upload or a remove
  /// </summary>
  public PixataUploadOperationType Operation { get; set; }

  /// <summary>
  /// The server's response
  /// </summary>
  public PixataUploadHttpRequest Request { get; set; } = new();
}

/// <summary>
/// The arguments for <c>OnSuccess</c>, raised when the server accepts an upload or remove request
/// </summary>
public class PixataUploadSuccessEventArgs : PixataUploadRequestResultEventArgs;

/// <summary>
/// The arguments for <c>OnError</c>, raised when an upload or remove request fails
/// </summary>
public class PixataUploadErrorEventArgs : PixataUploadRequestResultEventArgs;

/// <summary>
/// The server's response to an upload or remove request
/// </summary>
public class PixataUploadHttpRequest {
  /// <summary>
  /// The HTTP status code, eg 200, or 0 if the server couldn't be reached
  /// </summary>
  public int Status { get; set; }

  public string StatusText { get; set; } = "";

  public string ResponseType { get; set; } = "";

  /// <summary>
  /// The body of the response
  /// </summary>
  public string ResponseText { get; set; } = "";
}

/// <summary>
/// The context for the <c>FileTemplate</c> and <c>FileInfoTemplate</c> of <see cref="PixataFileSelect"/> and
/// <see cref="PixataUpload"/>
/// </summary>
public class PixataFileTemplateContext<TFileInfo>(TFileInfo file) where TFileInfo : PixataFileInfoBase {
  /// <summary>
  /// The file being rendered
  /// </summary>
  public TFileInfo File { get; } = file;
}
