using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// Lets the user pick files (with the select button, or by dragging them onto the component), and posts them from the
/// browser to <see cref="SaveUrl"/>, showing the progress of each one. Uploads can be cancelled, retried, sent in chunks, and
/// (when chunked) paused and resumed. Use <see cref="PixataFileSelect"/> to have the files handed to your code instead
/// </summary>
public class PixataUpload : PixataUploadBase<PixataUploadFileInfo> {
  private readonly HashSet<string> _removing = [];

  /// <summary>
  /// The URL the files are posted to, one request per file (or per chunk), as multipart form data with the file in the
  /// <see cref="SaveField"/> field. A 2xx response means success; anything else means the upload failed
  /// </summary>
  [Parameter]
  public string? SaveUrl { get; set; }

  /// <summary>
  /// The URL that a remove request is posted to when the user removes a file that has been uploaded, with the file's name in
  /// the <see cref="RemoveField"/> field. When null, removing a file just takes it out of the list
  /// </summary>
  [Parameter]
  public string? RemoveUrl { get; set; }

  /// <summary>
  /// The form field the file is sent in. Default <c>files</c>
  /// </summary>
  [Parameter]
  public string SaveField { get; set; } = "files";

  /// <summary>
  /// The form field the file name is sent in when removing a file. Default <c>files</c>
  /// </summary>
  [Parameter]
  public string RemoveField { get; set; } = "files";

  /// <summary>
  /// When true, cookies and authorisation headers are sent with requests to another domain (if the server's CORS policy
  /// allows credentials)
  /// </summary>
  [Parameter]
  public bool WithCredentials { get; set; }

  /// <summary>
  /// When true (the default), files are uploaded as soon as they are picked. When false, the user clicks the Upload button
  /// (or you call <see cref="UploadFiles"/>)
  /// </summary>
  [Parameter]
  public bool AutoUpload { get; set; } = true;

  /// <summary>
  /// Holds settings components, eg <see cref="PixataUploadChunkSettings"/> to upload files in chunks
  /// </summary>
  [Parameter]
  public RenderFragment? UploadSettings { get; set; }

  /// <summary>
  /// Raised when the user picks or drops files, before they are added to the list. Files that failed validation are
  /// included. Set <c>IsCancelled</c> to stop the files being added, or take files out of <c>Files</c> to leave just those out
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadSelectEventArgs> OnSelect { get; set; }

  /// <summary>
  /// Raised just before a file is uploaded. Add headers and form fields to the request through <c>RequestHeaders</c> and
  /// <c>RequestData</c>, or set <c>IsCancelled</c> to stop the upload (the file stays in the list, waiting to be uploaded)
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadEventArgs> OnUpload { get; set; }

  /// <summary>
  /// Raised when the user removes a file, before the remove request (if any) is sent. Add headers and form fields to the
  /// request, or set <c>IsCancelled</c> to keep the file
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadEventArgs> OnRemove { get; set; }

  /// <summary>
  /// Raised as a file uploads
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadProgressEventArgs> OnProgress { get; set; }

  /// <summary>
  /// Raised when the server accepts an upload or remove request. <c>Operation</c> says which, and <c>Request</c> holds the
  /// server's response
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadSuccessEventArgs> OnSuccess { get; set; }

  /// <summary>
  /// Raised when an upload or remove request fails. <c>Operation</c> says which, and <c>Request</c> holds the server's
  /// response
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadErrorEventArgs> OnError { get; set; }

  /// <summary>
  /// Raised when the user cancels an upload. Set <c>IsCancelled</c> to let the upload carry on
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadCancelEventArgs> OnCancel { get; set; }

  /// <summary>
  /// Raised when the user clicks the Clear button. Set <c>IsCancelled</c> to keep the files
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadClearEventArgs> OnClear { get; set; }

  /// <summary>
  /// Raised when the user pauses a chunked upload. Set <c>IsCancelled</c> to carry on uploading
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadPauseEventArgs> OnPause { get; set; }

  /// <summary>
  /// Raised when the user resumes a paused chunked upload. Set <c>IsCancelled</c> to leave it paused
  /// </summary>
  [Parameter]
  public EventCallback<PixataUploadResumeEventArgs> OnResume { get; set; }

  /// <summary>
  /// The chunk settings, set by a <see cref="PixataUploadChunkSettings"/> inside <see cref="UploadSettings"/>
  /// </summary>
  internal PixataUploadChunkSettings? ChunkSettings { get; set; }

  private bool Chunked =>
    ChunkSettings is { Enabled: true, Size: > 0 };

  private bool CanPause =>
    Chunked && ChunkSettings is { Resumable: true };

  /// <summary>
  /// Uploads every valid file that is waiting to be uploaded. The task finishes when they have all finished
  /// </summary>
  /// <exception cref="InvalidOperationException"><see cref="SaveUrl"/> isn't set</exception>
  public Task UploadFiles() =>
    Task.WhenAll(CurrentFiles.Where(f => f is { Status: PixataUploadFileStatus.Selected, IsValid: true }).ToList().Select(UploadFile));

  /// <summary>
  /// Uploads a file that failed again
  /// </summary>
  public async Task RetryFile(string fileId) {
    if (FindFile(fileId) is { Status: PixataUploadFileStatus.Failed } file) {
      await UploadFile(file);
    }
  }

  /// <summary>
  /// Cancels a file's upload (raising <c>OnCancel</c> first), and takes it out of the list
  /// </summary>
  public async Task CancelFile(string fileId) {
    if (FindFile(fileId) is not { } file) {
      return;
    }
    if (file is { Status: PixataUploadFileStatus.Uploading or PixataUploadFileStatus.Paused, RequestId: { } requestId }) {
      PixataUploadCancelEventArgs args = new() { Files = [file] };
      await OnCancel.InvokeAsync(args);
      if (args.IsCancelled) {
        return;
      }
      await CallBrowser("cancel", requestId);
    }
    await RemoveFromList(file);
  }

  /// <summary>
  /// Pauses a chunked upload (raising <c>OnPause</c> first). Only works when the <see cref="PixataUploadChunkSettings"/> are
  /// resumable
  /// </summary>
  public async Task PauseFile(string fileId) {
    if (!CanPause || FindFile(fileId) is not { Status: PixataUploadFileStatus.Uploading, RequestId: { } requestId } file) {
      return;
    }
    PixataUploadPauseEventArgs args = new() { Files = [file] };
    await OnPause.InvokeAsync(args);
    if (args.IsCancelled || file.Status != PixataUploadFileStatus.Uploading) {
      return;
    }
    file.Status = PixataUploadFileStatus.Paused;
    await CallBrowser("pause", requestId);
    StateHasChanged();
  }

  /// <summary>
  /// Resumes a paused upload (raising <c>OnResume</c> first)
  /// </summary>
  public async Task ResumeFile(string fileId) {
    if (FindFile(fileId) is not { Status: PixataUploadFileStatus.Paused, RequestId: { } requestId } file) {
      return;
    }
    PixataUploadResumeEventArgs args = new() { Files = [file] };
    await OnResume.InvokeAsync(args);
    if (args.IsCancelled || file.Status != PixataUploadFileStatus.Paused) {
      return;
    }
    file.Status = PixataUploadFileStatus.Uploading;
    await CallBrowser("resume", requestId);
    StateHasChanged();
  }

  /// <summary>
  /// Removes a file (raising <c>OnRemove</c> first). If it has been uploaded and <see cref="RemoveUrl"/> is set, a remove
  /// request is sent, and the file is only taken out of the list if that succeeds. A file that is uploading is cancelled
  /// </summary>
  public override async Task RemoveFile(string fileId) {
    if (FindFile(fileId) is not { } file || _removing.Contains(file.Id)) {
      return;
    }
    if (file.Status is PixataUploadFileStatus.Uploading or PixataUploadFileStatus.Paused) {
      await CancelFile(fileId);
      return;
    }
    PixataUploadEventArgs args = new() { Files = [file] };
    await OnRemove.InvokeAsync(args);
    if (args.IsCancelled) {
      return;
    }
    if (file.Status is not (PixataUploadFileStatus.Uploaded or PixataUploadFileStatus.Blank) || string.IsNullOrWhiteSpace(RemoveUrl)) {
      await RemoveFromList(file);
      return;
    }
    _removing.Add(file.Id);
    PixataUploadBrowserResult result;
    try {
      result = await Send(new() {
        Url = RemoveUrl,
        Field = RemoveField,
        Values = [file.Name],
        Headers = Strings(args.RequestHeaders),
        Data = Strings(args.RequestData),
        WithCredentials = WithCredentials
      }, null, NewId());
    }
    finally {
      _removing.Remove(file.Id);
    }
    if (result.Cancelled) {
      return;
    }
    if (!result.Success) {
      await OnError.InvokeAsync(new() { Files = [file], Operation = PixataUploadOperationType.Remove, Request = result.ToRequest() });
      return;
    }
    await RemoveFromList(file);
    await OnSuccess.InvokeAsync(new() { Files = [file], Operation = PixataUploadOperationType.Remove, Request = result.ToRequest() });
  }

  /// <summary>
  /// Removes every file from the list, cancelling any uploads, without raising any events or sending remove requests
  /// </summary>
  public override async Task ClearFiles() {
    foreach (PixataUploadFileInfo file in CurrentFiles.Where(f => f.RequestId is not null).ToList()) {
      await CallBrowser("cancel", file.RequestId);
    }
    await base.ClearFiles();
  }

  protected override string DropZoneHint =>
    Text.UploadDropZoneHint;

  protected override RenderFragment SettingsContent =>
    builder => {
      builder.OpenComponent<CascadingValue<PixataUpload>>(0);
      builder.AddComponentParameter(1, nameof(CascadingValue<PixataUpload>.Value), this);
      builder.AddComponentParameter(2, nameof(CascadingValue<PixataUpload>.IsFixed), true);
      builder.AddComponentParameter(3, nameof(CascadingValue<PixataUpload>.ChildContent), UploadSettings);
      builder.CloseComponent();
    };

  protected override RenderFragment? HeaderStatus {
    get {
      if (CurrentFiles.Any(f => f.Status is PixataUploadFileStatus.Uploading or PixataUploadFileStatus.Paused)) {
        return StatusContent(PixataSvgIcons.Upload, Text.StatusUploading, "pixata-blazor-upload-status-uploading");
      }
      if (CurrentFiles.Any(f => f.Status == PixataUploadFileStatus.Failed)) {
        return StatusContent(PixataSvgIcons.Warning, Text.StatusDone, "pixata-blazor-upload-status-failed");
      }
      return CurrentFiles.Any(f => f.Status == PixataUploadFileStatus.Uploaded)
        ? StatusContent(PixataSvgIcons.Check, Text.StatusDone, "pixata-blazor-upload-status-done")
        : null;
    }
  }

  protected override RenderFragment? Footer =>
    AutoUpload || !CurrentFiles.Any(f => f is { Status: PixataUploadFileStatus.Selected, IsValid: true })
      ? null
      : ActionsBar(builder => {
        builder.AddContent(0, FooterButton(Text.Upload, PixataThemeConstants.Button.ThemeColor.Primary, "pixata-blazor-upload-selected", UploadFiles));
        builder.AddContent(1, FooterButton(Text.Clear, PixataThemeConstants.Button.ThemeColor.Base, "pixata-blazor-upload-clear-selected", ClearClicked));
      });

  protected override RenderFragment FileActions(PixataUploadFileInfo file) =>
    builder => {
      switch (file.Status) {
        case PixataUploadFileStatus.Uploading:
        case PixataUploadFileStatus.Paused:
          builder.OpenElement(0, "span");
          builder.AddAttribute(1, "class", "pixata-blazor-upload-pct");
          builder.AddContent(2, $"{file.Progress}%");
          builder.CloseElement();
          if (CanPause) {
            builder.AddContent(3, file.Status == PixataUploadFileStatus.Paused
              ? ActionButton(PixataSvgIcons.Play, Text.Resume, "pixata-blazor-upload-resume", () => ResumeFile(file.Id))
              : ActionButton(PixataSvgIcons.Pause, Text.Pause, "pixata-blazor-upload-pause", () => PauseFile(file.Id)));
          }
          builder.AddContent(4, ActionButton(PixataSvgIcons.Cancel, Text.Cancel, "pixata-blazor-upload-cancel", () => CancelFile(file.Id)));
          break;
        case PixataUploadFileStatus.Failed when file.IsValid:
          builder.AddContent(5, ActionButton(PixataSvgIcons.Refresh, Text.Retry, "pixata-blazor-upload-retry", () => RetryFile(file.Id)));
          builder.AddContent(6, ActionButton(PixataSvgIcons.X, Text.Remove, "pixata-blazor-upload-remove", () => RemoveFile(file.Id)));
          break;
        default:
          builder.AddContent(7, ActionButton(PixataSvgIcons.X, Text.Remove, "pixata-blazor-upload-remove", () => RemoveFile(file.Id)));
          break;
      }
    };

  protected override int? FileProgress(PixataUploadFileInfo file) =>
    file.Status is PixataUploadFileStatus.Uploading or PixataUploadFileStatus.Paused ? file.Progress : null;

  protected override (string Text, string Class)? FileMessage(PixataUploadFileInfo file) =>
    ValidationMessage(file) is { } message ? (message, "pixata-blazor-upload-file-validation-message")
    : file.Status == PixataUploadFileStatus.Uploaded ? (Text.FileUploaded, "pixata-blazor-upload-file-validation-message")
    : file.Status == PixataUploadFileStatus.Failed ? (Text.FileFailed, "pixata-blazor-upload-file-validation-message")
    : null;

  protected override string? FileStateClass(PixataUploadFileInfo file) =>
    !file.IsValid ? "pixata-blazor-upload-file-invalid"
    : file.Status switch {
      PixataUploadFileStatus.Uploading => "pixata-blazor-upload-file-progress",
      PixataUploadFileStatus.Paused => "pixata-blazor-upload-file-progress pixata-blazor-upload-file-paused",
      PixataUploadFileStatus.Uploaded => "pixata-blazor-upload-file-success",
      PixataUploadFileStatus.Failed => "pixata-blazor-upload-file-error",
      _ => null
    };

  protected override async Task FileKey(PixataUploadFileInfo file, string key) {
    switch (key) {
      case "Escape" when file.Status is PixataUploadFileStatus.Uploading or PixataUploadFileStatus.Paused:
        await CancelFile(file.Id);
        break;
      case "Enter" when file.Status == PixataUploadFileStatus.Failed:
        await RetryFile(file.Id);
        break;
      case " " when file.Status == PixataUploadFileStatus.Uploading:
        await PauseFile(file.Id);
        break;
      case " " when file.Status == PixataUploadFileStatus.Paused:
        await ResumeFile(file.Id);
        break;
    }
  }

  protected override PixataUploadFileInfo CreateFileInfo() =>
    new() { Status = PixataUploadFileStatus.Selected };

  protected override void PreloadFile(PixataUploadFileInfo file) {
    // A file passed in is on the server already, so it can't be uploading or waiting to be uploaded
    if (file.Status is not (PixataUploadFileStatus.Uploaded or PixataUploadFileStatus.Failed)) {
      file.Status = PixataUploadFileStatus.Blank;
    }
  }

  protected override async Task<bool> RaiseSelect(List<PixataUploadFileInfo> files) {
    PixataUploadSelectEventArgs args = new() { Files = files };
    await OnSelect.InvokeAsync(args);
    return !args.IsCancelled;
  }

  protected override Task FilesAdded(List<PixataUploadFileInfo> files) {
    if (AutoUpload) {
      foreach (PixataUploadFileInfo file in files.Where(f => f.IsValid)) {
        _ = RunInBackground(() => UploadFile(file));
      }
    }
    return Task.CompletedTask;
  }

  private async Task ClearClicked() {
    PixataUploadClearEventArgs args = new() { Files = [.. CurrentFiles] };
    await OnClear.InvokeAsync(args);
    if (!args.IsCancelled) {
      await ClearFiles();
    }
  }

  private async Task UploadFile(PixataUploadFileInfo file) {
    if (file.Status is not (PixataUploadFileStatus.Selected or PixataUploadFileStatus.Failed) || !file.IsValid || file.BrowserFileId is null) {
      return;
    }
    if (string.IsNullOrWhiteSpace(SaveUrl)) {
      throw new InvalidOperationException("PixataUpload needs a SaveUrl to upload the files to");
    }
    PixataUploadFileStatus previous = file.Status;
    // Marked as uploading straight away, so it isn't started twice if UploadFiles() is called again while OnUpload runs
    file.Status = PixataUploadFileStatus.Uploading;
    PixataUploadEventArgs args = new() { Files = [file] };
    await OnUpload.InvokeAsync(args);
    if (args.IsCancelled || !CurrentFiles.Contains(file)) {
      file.Status = previous;
      StateHasChanged();
      return;
    }
    string requestId = NewId();
    file.RequestId = requestId;
    file.Progress = 0;
    StateHasChanged();
    PixataUploadBrowserResult result = await Send(new() {
      Url = SaveUrl,
      Field = SaveField,
      FileId = file.BrowserFileId,
      UploadId = requestId,
      Headers = Strings(args.RequestHeaders),
      Data = Strings(args.RequestData),
      WithCredentials = WithCredentials,
      Chunk = Chunked && ChunkSettings is { } chunk
        ? new() { Size = chunk.Size, AutoRetryAfter = chunk.AutoRetryAfter, MaxAutoRetries = chunk.MaxAutoRetries, MetadataField = chunk.MetadataField }
        : null
    }, progress => Progress(file, progress), requestId);
    file.RequestId = null;
    if (result.Cancelled || !CurrentFiles.Contains(file)) {
      return;
    }
    if (result.Success) {
      file.Status = PixataUploadFileStatus.Uploaded;
      file.Progress = 100;
      StateHasChanged();
      await OnSuccess.InvokeAsync(new() { Files = [file], Operation = PixataUploadOperationType.Upload, Request = result.ToRequest() });
    } else {
      file.Status = PixataUploadFileStatus.Failed;
      StateHasChanged();
      await OnError.InvokeAsync(new() { Files = [file], Operation = PixataUploadOperationType.Upload, Request = result.ToRequest() });
    }
  }

  private async Task Progress(PixataUploadFileInfo file, int progress) {
    if (file.Status is not (PixataUploadFileStatus.Uploading or PixataUploadFileStatus.Paused)) {
      return;
    }
    file.Progress = progress;
    StateHasChanged();
    await OnProgress.InvokeAsync(new() { Files = [file], Progress = progress });
  }

  private static Dictionary<string, string> Strings(Dictionary<string, object> values) =>
    values.ToDictionary(v => v.Key, v => Convert.ToString(v.Value, CultureInfo.InvariantCulture) ?? "");
}
