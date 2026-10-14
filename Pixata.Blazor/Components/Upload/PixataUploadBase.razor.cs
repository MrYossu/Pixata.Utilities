using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// What <see cref="PixataFileSelect"/> and <see cref="PixataUpload"/> have in common: the select button, dragging and
/// dropping, validation, and the file list with its keyboard handling
/// </summary>
/// <typeparam name="TFileInfo">The type of the files in the list</typeparam>
public abstract partial class PixataUploadBase<TFileInfo> : ComponentBase, IAsyncDisposable where TFileInfo : PixataFileInfoBase {
  private readonly List<TFileInfo> _files = [];
  private readonly Dictionary<string, PendingRequest> _requests = [];
  private readonly string _listId = $"pixata-upload-files-{Guid.NewGuid():N}";
  private ElementReference _root;
  private ElementReference _input;
  private ElementReference _button;
  private DotNetObjectReference<PixataUploadCallbacks>? _dotNet;
  private int _jsId;
  private string _jsOptions = "";
  private bool _filesLoaded;
  private int? _focusAfterRender;
  private bool _disposed;

  [Inject]
  private IJSRuntime JS { get; set; } = null!;

  /// <summary>
  /// When false, the buttons are disabled, and files can't be picked or dropped. The file list can still be read. Default true
  /// </summary>
  [Parameter]
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// When true (the default), the user can pick or drop several files at once. When false, picking a file replaces the one
  /// in the list
  /// </summary>
  [Parameter]
  public bool Multiple { get; set; } = true;

  /// <summary>
  /// The kinds of file the browser's file dialog offers, eg <c>image/*</c> or <c>.pdf,.docx</c>. This only filters the
  /// dialog; use <see cref="AllowedExtensions"/> to reject files. On phones, <c>image/*</c> (or <c>video/*</c>) lets the user
  /// take a photo (or video) with the camera
  /// </summary>
  [Parameter]
  public string? Accept { get; set; }

  /// <summary>
  /// The file extensions (eg <c>.pdf</c>) that are allowed. Files with any other extension are shown with a validation
  /// message, and aren't uploaded. Empty or null allows any extension. The comparison ignores case
  /// </summary>
  [Parameter]
  public IEnumerable<string>? AllowedExtensions { get; set; }

  /// <summary>
  /// The smallest file allowed, in bytes
  /// </summary>
  [Parameter]
  public long? MinFileSize { get; set; }

  /// <summary>
  /// The largest file allowed, in bytes
  /// </summary>
  [Parameter]
  public long? MaxFileSize { get; set; }

  /// <summary>
  /// The id of the (hidden) file input, so that clicking a <c>&lt;label for="..."&gt;</c> opens the file dialog
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// The file input's <c>capture</c> attribute. On phones, with <see cref="Accept"/> set to <c>image/*</c>, <c>user</c> opens
  /// the front camera and <c>environment</c> the back camera, rather than offering a choice
  /// </summary>
  [Parameter]
  public string? Capture { get; set; }

  /// <summary>
  /// The id of a <see cref="PixataDropZone"/> (or any element) that files can be dropped on, as well as on the component
  /// </summary>
  [Parameter]
  public string? DropZoneId { get; set; }

  /// <summary>
  /// The id of an element that describes the select button, for screen readers
  /// </summary>
  [Parameter]
  public string? AriaDescribedBy { get; set; }

  /// <summary>
  /// The id of an element that labels the select button, for screen readers
  /// </summary>
  [Parameter]
  public string? AriaLabelledBy { get; set; }

  /// <summary>
  /// Files to show in the list when the component is first rendered, eg ones the user uploaded earlier. Changing it later has
  /// no effect
  /// </summary>
  [Parameter]
  public IEnumerable<TFileInfo>? Files { get; set; }

  /// <summary>
  /// Replaces the whole of each item in the file list, including the actions and progress bar. The context's <c>File</c>
  /// is the file
  /// </summary>
  [Parameter]
  public RenderFragment<PixataFileTemplateContext<TFileInfo>>? FileTemplate { get; set; }

  /// <summary>
  /// Replaces the file's name and size (or message) in each item in the file list, keeping the icon, progress bar and
  /// actions. The context's <c>File</c> is the file
  /// </summary>
  [Parameter]
  public RenderFragment<PixataFileTemplateContext<TFileInfo>>? FileInfoTemplate { get; set; }

  /// <summary>
  /// The content of the select button, in place of "Select files..."
  /// </summary>
  [Parameter]
  public RenderFragment? SelectFilesButtonTemplate { get; set; }

  /// <summary>
  /// The text the component shows, so you can translate or reword it
  /// </summary>
  [Parameter]
  public PixataUploadMessages? Messages { get; set; }

  /// <summary>
  /// Extra CSS classes for the component's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Any other attributes are rendered on the component's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  /// <summary>
  /// The files in the list
  /// </summary>
  public IReadOnlyList<TFileInfo> CurrentFiles =>
    _files;

  /// <summary>
  /// Opens the browser's file dialog, as if the user had clicked the select button. Browsers only allow this in response to
  /// something the user did, such as clicking a button
  /// </summary>
  public async Task OpenSelectFilesDialog() {
    if (_jsId != 0 && Enabled) {
      await PixataUploadJs.InvokeVoid(JS, "openDialog", _jsId);
    }
  }

  /// <summary>
  /// Removes a file from the list (raising <c>OnRemove</c> first)
  /// </summary>
  /// <param name="fileId">The file's <see cref="PixataFileInfoBase.Id"/></param>
  public abstract Task RemoveFile(string fileId);

  /// <summary>
  /// Removes every file from the list, without raising any events
  /// </summary>
  public virtual async Task ClearFiles() {
    List<TFileInfo> files = [.. _files];
    _files.Clear();
    await ForgetFiles(files);
    StateHasChanged();
  }

  /// <summary>
  /// The text to use: <see cref="Messages"/>, or the defaults
  /// </summary>
  protected PixataUploadMessages Text =>
    Messages ?? PixataUploadMessages.Default;

  /// <summary>
  /// The hint shown next to the select button
  /// </summary>
  protected abstract string DropZoneHint { get; }

  /// <summary>
  /// The class that identifies the derived component on the outer element, eg <c>pixata-blazor-fileselect</c>
  /// </summary>
  protected virtual string? ComponentClass => null;

  /// <summary>
  /// Rendered (invisibly) at the start of the component, eg to cascade the component to settings components
  /// </summary>
  protected virtual RenderFragment? SettingsContent => null;

  /// <summary>
  /// Rendered next to the select button, in place of the drop hint, eg the upload status
  /// </summary>
  protected virtual RenderFragment? HeaderStatus => null;

  /// <summary>
  /// Rendered under the file list, eg the Clear and Upload buttons
  /// </summary>
  protected virtual RenderFragment? Footer => null;

  /// <summary>
  /// The buttons shown at the end of a file in the list. By default, a remove button
  /// </summary>
  protected virtual RenderFragment FileActions(TFileInfo file) =>
    ActionButton(PixataSvgIcons.X, Text.Remove, "pixata-blazor-upload-remove", () => RemoveFile(file.Id));

  /// <summary>
  /// The progress shown in the bar along the bottom of a file, or null for no bar
  /// </summary>
  protected virtual int? FileProgress(TFileInfo file) =>
    null;

  /// <summary>
  /// The message shown under a file's name in place of its size, eg a validation message
  /// </summary>
  protected virtual (string Text, string Class)? FileMessage(TFileInfo file) =>
    ValidationMessage(file) is { } message ? (message, "pixata-blazor-upload-file-validation-message") : null;

  /// <summary>
  /// Extra classes for a file in the list, describing its state
  /// </summary>
  protected virtual string? FileStateClass(TFileInfo file) =>
    file.IsValid ? null : "pixata-blazor-upload-file-invalid";

  /// <summary>
  /// Handles the keys that the derived component uses on a file (other than moving around the list and Delete)
  /// </summary>
  protected virtual Task FileKey(TFileInfo file, string key) =>
    Task.CompletedTask;

  /// <summary>
  /// Creates the info for a file the user picked or dropped
  /// </summary>
  protected abstract TFileInfo CreateFileInfo();

  /// <summary>
  /// Raises the derived component's OnSelect event
  /// </summary>
  /// <returns>False if the handler cancelled the event</returns>
  protected abstract Task<bool> RaiseSelect(List<TFileInfo> files);

  /// <summary>
  /// Called when files have been added to the list (eg to start uploading them)
  /// </summary>
  protected virtual Task FilesAdded(List<TFileInfo> files) =>
    Task.CompletedTask;

  /// <summary>
  /// Prepares a file passed in through the <c>Files</c> parameter
  /// </summary>
  protected virtual void PreloadFile(TFileInfo file) {
  }

  /// <summary>
  /// The validation message for a file, or null if it is valid
  /// </summary>
  protected string? ValidationMessage(TFileInfo file) =>
    file.InvalidExtension ? Text.InvalidFileExtension
    : file.InvalidMaxFileSize ? Text.InvalidMaxFileSize
    : file.InvalidMinFileSize ? Text.InvalidMinFileSize
    : null;

  /// <summary>
  /// Formats a file size in KB or bigger, eg <c>0.57 KB</c> or <c>2.00 MB</c>
  /// </summary>
  protected string FormatSize(long size) {
    string[] units = Text.SizeUnits.Length > 0 ? Text.SizeUnits : PixataUploadMessages.Default.SizeUnits;
    double value = size / 1024d;
    int unit = 0;
    while (value >= 1024 && unit < units.Length - 1) {
      value /= 1024;
      unit++;
    }
    return $"{value.ToString("0.00", CultureInfo.CurrentCulture)} {units[unit]}";
  }

  /// <summary>
  /// Finds a file in the list by its id
  /// </summary>
  protected TFileInfo? FindFile(string fileId) =>
    _files.FirstOrDefault(f => f.Id == fileId);

  /// <summary>
  /// Takes files out of the list, and tells the browser it can let go of them
  /// </summary>
  protected async Task RemoveFromList(params TFileInfo[] files) {
    foreach (TFileInfo file in files) {
      _files.Remove(file);
    }
    await ForgetFiles(files);
    StateHasChanged();
  }

  private async Task ForgetFiles(IEnumerable<TFileInfo> files) {
    string[] ids = files.Select(f => f.BrowserFileId).OfType<string>().ToArray();
    if (_jsId != 0 && ids.Length > 0) {
      await PixataUploadJs.InvokeVoid(JS, "forgetFiles", _jsId, ids);
    }
  }

  /// <summary>
  /// Has the browser send a request, and waits for it to finish
  /// </summary>
  /// <param name="request">The request</param>
  /// <param name="onProgress">Called as an upload progresses, with the percentage sent</param>
  /// <param name="requestId">The id to give the request, so it can be paused, resumed or cancelled</param>
  private protected async Task<PixataUploadBrowserResult> Send(PixataUploadBrowserRequest request, Func<int, Task>? onProgress, string requestId) {
    if (_disposed) {
      // The browser has forgotten the component, so it would never say the request had finished
      return PixataUploadBrowserResult.CancelledResult;
    }
    if (_jsId == 0) {
      return new() { StatusText = "The component isn't ready yet" };
    }
    PendingRequest pending = new(onProgress);
    _requests[requestId] = pending;
    try {
      await PixataUploadJs.InvokeVoid(JS, "send", _jsId, requestId, request);
    }
    catch (JSException ex) {
      // The browser wouldn't send it, eg because a header name or the URL isn't valid
      _requests.Remove(requestId);
      return new() { StatusText = ex.Message };
    }
    return await pending.Completion.Task;
  }

  /// <summary>
  /// Calls a function in pixata-upload.js for this component
  /// </summary>
  private protected Task CallBrowser(string identifier, params object?[] args) =>
    _jsId == 0 ? Task.CompletedTask : PixataUploadJs.InvokeVoid(JS, identifier, [_jsId, .. args]);

  /// <summary>
  /// Runs work that isn't awaited (eg an upload started by picking a file), passing any exception to the error boundary
  /// </summary>
  protected async Task RunInBackground(Func<Task> work) {
    try {
      await work();
    }
    catch (Exception ex) {
      await DispatchExceptionAsync(ex);
    }
  }

  protected override void OnParametersSet() {
    if (!_filesLoaded) {
      _filesLoaded = true;
      foreach (TFileInfo file in Files ?? []) {
        if (string.IsNullOrWhiteSpace(file.Id)) {
          file.Id = NewId();
        }
        if (string.IsNullOrWhiteSpace(file.Extension)) {
          file.Extension = Path.GetExtension(file.Name);
        }
        PreloadFile(file);
        _files.Add(file);
      }
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
      PixataUploadCallbacks callbacks = new(OnFilesSelected, OnRequestProgress, OnRequestComplete);
      _dotNet = DotNetObjectReference.Create(callbacks);
      _jsOptions = JsOptionsKey;
      _jsId = await PixataUploadJs.Invoke(JS, "init", 0, _root, _input, _button, _dotNet, JsOptions);
      if (_disposed && _jsId != 0) {
        await PixataUploadJs.InvokeVoid(JS, "dispose", _jsId);
      }
    } else if (_jsId != 0 && JsOptionsKey != _jsOptions) {
      _jsOptions = JsOptionsKey;
      await CallBrowser("update", JsOptions);
    }
    if (_focusAfterRender is int index) {
      _focusAfterRender = null;
      await Focus(index);
    }
  }

  private object JsOptions =>
    new { enabled = Enabled, multiple = Multiple, dropZoneId = DropZoneId };

  private string JsOptionsKey =>
    $"{Enabled}|{Multiple}|{DropZoneId}";

  // Called by pixata-upload.js when the user picks or drops files
  internal async Task OnFilesSelected(PixataUploadBrowserFile[] browserFiles) {
    List<TFileInfo> picked = browserFiles.Select(Create).ToList();
    if (picked.Count == 0) {
      return;
    }
    // The handler can take files out of the list it's given, to stop them being added
    List<TFileInfo> files = [.. picked];
    if (!await RaiseSelect(files)) {
      await ForgetFiles(picked);
      return;
    }
    await ForgetFiles(picked.Except(files));
    if (files.Count == 0) {
      return;
    }
    if (!Multiple) {
      await ClearFiles();
      files = files.Take(1).ToList();
    }
    _files.AddRange(files);
    StateHasChanged();
    await FilesAdded(files);
  }

  private TFileInfo Create(PixataUploadBrowserFile browserFile) {
    TFileInfo file = CreateFileInfo();
    file.Id = NewId();
    file.BrowserFileId = browserFile.Id;
    file.Name = browserFile.Name;
    file.Size = browserFile.Size;
    file.Extension = Path.GetExtension(browserFile.Name);
    file.ContentType = browserFile.ContentType;
    file.LastModified = browserFile.LastModified > 0 ? DateTimeOffset.FromUnixTimeMilliseconds((long)browserFile.LastModified) : null;
    Validate(file);
    if (file is PixataFileSelectFileInfo selectFile) {
      string browserFileId = browserFile.Id;
      selectFile.SetSource(token => OpenBrowserFile(browserFileId, token));
    }
    return file;
  }

  private async ValueTask<IJSStreamReference> OpenBrowserFile(string browserFileId, System.Threading.CancellationToken cancellationToken) {
    IJSObjectReference module = await PixataUploadJs.Module(JS);
    return await module.InvokeAsync<IJSStreamReference>("getFile", cancellationToken, _jsId, browserFileId);
  }

  private void Validate(TFileInfo file) {
    List<string> allowed = (AllowedExtensions ?? []).Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).Select(e => e.StartsWith('.') ? e : $".{e}").ToList();
    file.InvalidExtension = allowed.Count > 0 && !allowed.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
    file.InvalidMinFileSize = MinFileSize is long min && file.Size < min;
    file.InvalidMaxFileSize = MaxFileSize is long max && file.Size > max;
  }

  // Called by pixata-upload.js as an upload progresses
  internal async Task OnRequestProgress(string requestId, int percent) {
    if (_requests.TryGetValue(requestId, out PendingRequest? pending) && pending.OnProgress is not null) {
      await pending.OnProgress(percent);
    }
  }

  // Called by pixata-upload.js when a request has finished
  internal Task OnRequestComplete(string requestId, PixataUploadBrowserResult result) {
    if (_requests.Remove(requestId, out PendingRequest? pending)) {
      pending.Completion.TrySetResult(result);
    }
    return Task.CompletedTask;
  }

  private async Task ButtonKeyDown(KeyboardEventArgs e) {
    if (e.Key == "ArrowDown" && _files.Count > 0) {
      await Focus(0);
    }
  }

  private async Task FileKeyDown(KeyboardEventArgs e, TFileInfo file, int index) {
    switch (e.Key) {
      case "ArrowDown":
        if (index < _files.Count - 1) {
          await Focus(index + 1);
        }
        break;
      case "ArrowUp":
        await Focus(index - 1);
        break;
      case "Home":
        await Focus(0);
        break;
      case "End":
        await Focus(_files.Count - 1);
        break;
      case "Delete":
        if (Enabled) {
          await RemoveFile(file.Id);
          if (!_files.Contains(file)) {
            // The file had the focus, so move it to the next one, or to the button if there are none left
            _focusAfterRender = _files.Count == 0 ? -1 : Math.Min(index, _files.Count - 1);
            StateHasChanged();
          }
        }
        break;
      default:
        if (Enabled) {
          await FileKey(file, e.Key);
        }
        break;
    }
  }

  /// <summary>
  /// Moves the focus to a file in the list, or to the select button if <paramref name="index"/> is negative
  /// </summary>
  private async Task Focus(int index) {
    if (index < 0) {
      await PixataJs.InvokeVoid(JS, "focusElement", _button);
    } else {
      await PixataJs.InvokeVoid(JS, "focusSelector", _root, $"li[data-index='{index}']");
    }
  }

  private string RootClass =>
    PixataCss.Classes("pixata-blazor-upload", ComponentClass, Enabled ? null : "pixata-blazor-upload-disabled", Class);

  private static string ButtonClass =>
    PixataCss.Classes(PixataCss.Button(null, null, null, null), "pixata-blazor-upload-button");

  private string? ButtonAriaLabel =>
    _files.Count == 0 ? null : $"{Text.SelectFiles} {string.Format(CultureInfo.CurrentCulture, Text.FilesAriaLabel, _files.Count)}";

  private string FileClass(TFileInfo file) =>
    PixataCss.Classes("pixata-blazor-upload-file", FileStateClass(file));

  private string FileAriaLabel(TFileInfo file) =>
    FileMessage(file) is { } message ? $"{file.Name}, {message.Text}" : $"{file.Name}, {FormatSize(file.Size)}";

  /// <summary>
  /// A new unique id, for a file or a request
  /// </summary>
  protected static string NewId() =>
    Guid.NewGuid().ToString("N");

  public virtual async ValueTask DisposeAsync() {
    if (_disposed) {
      return;
    }
    _disposed = true;
    foreach (PendingRequest pending in _requests.Values) {
      pending.Completion.TrySetResult(PixataUploadBrowserResult.CancelledResult);
    }
    _requests.Clear();
    if (_jsId != 0) {
      await PixataUploadJs.InvokeVoid(JS, "dispose", _jsId);
    }
    _dotNet?.Dispose();
    GC.SuppressFinalize(this);
  }

  private sealed class PendingRequest(Func<int, Task>? onProgress) {
    public Func<int, Task>? OnProgress { get; } = onProgress;
    public TaskCompletionSource<PixataUploadBrowserResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}
