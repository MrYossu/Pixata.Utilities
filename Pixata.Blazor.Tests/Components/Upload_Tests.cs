using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;

namespace Pixata.Blazor.Tests.Components;

[TestClass]
public class Upload_Tests {
  private const string SaveUrl = "/api/save";
  private const string RemoveUrl = "/api/remove";

  /// <summary>
  /// A bUnit context with pixata-upload.js set up to hand out an instance id, so the tests can stand in for the browser by
  /// calling the callbacks the component gave the script
  /// </summary>
  private sealed class Browser : IDisposable {
    private int _nextFileId = 1;

    public BunitContext Context { get; } = PixataTestContext.Create();
    public BunitJSModuleInterop Upload { get; }
    public BunitJSModuleInterop Shared { get; }

    public Browser() {
      Upload = Context.JSInterop.SetupModule(PixataUploadJs.ModulePath);
      Upload.Mode = JSRuntimeMode.Loose;
      Upload.Setup<int>("init", _ => true).SetResult(7);
      Shared = Context.JSInterop.SetupModule(PixataJs.ModulePath);
      Shared.Mode = JSRuntimeMode.Loose;
    }

    public PixataUploadCallbacks Callbacks =>
      ((DotNetObjectReference<PixataUploadCallbacks>)Upload.Invocations["init"].Last().Arguments[3]!).Value;

    public IReadOnlyList<JSRuntimeInvocation> Calls(string identifier) =>
      Upload.Invocations[identifier];

    public Task Pick<T>(IRenderedComponent<T> cut, params (string Name, long Size)[] files) where T : IComponent =>
      cut.InvokeAsync(() => Callbacks.OnFilesSelected(files.Select(f => new PixataUploadBrowserFile {
        Id = $"b{_nextFileId++}",
        Name = f.Name,
        Size = f.Size,
        ContentType = f.Name.EndsWith(".png") ? "image/png" : "text/plain",
        LastModified = 1700000000000
      }).ToArray()));

    /// <summary>
    /// The requests the component has asked the browser to send, as (request id, request)
    /// </summary>
    public List<(string Id, PixataUploadBrowserRequest Request)> Requests =>
      Calls("send").Select(c => ((string)c.Arguments[1]!, (PixataUploadBrowserRequest)c.Arguments[2]!)).ToList();

    public Task Complete<T>(IRenderedComponent<T> cut, string requestId, int status) where T : IComponent =>
      cut.InvokeAsync(() => Callbacks.OnRequestComplete(requestId, new() {
        Status = status,
        StatusText = status == 200 ? "OK" : "Internal Server Error",
        ResponseType = "text",
        ResponseText = status == 200 ? "saved" : "oops",
        Success = status is >= 200 and < 300
      }));

    public Task Progress<T>(IRenderedComponent<T> cut, string requestId, int percent) where T : IComponent =>
      cut.InvokeAsync(() => Callbacks.OnRequestProgress(requestId, percent));

    public void Dispose() =>
      Context.Dispose();
  }

  private sealed class FakeStreamReference(byte[] content) : IJSStreamReference {
    public long Length => content.Length;
    public long? MaxAllowedSize { get; private set; }
    public bool Disposed { get; private set; }

    public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) {
      MaxAllowedSize = maxAllowedSize;
      return ValueTask.FromResult<Stream>(new MemoryStream(content));
    }

    public ValueTask DisposeAsync() {
      Disposed = true;
      return ValueTask.CompletedTask;
    }
  }

  private static string[] Names(IRenderedComponent<PixataFileSelect> cut) =>
    cut.FindAll("li .pixata-blazor-upload-file-name").Select(e => e.TextContent).ToArray();

  private static string[] Names(IRenderedComponent<PixataUpload> cut) =>
    cut.FindAll("li .pixata-blazor-upload-file-name").Select(e => e.TextContent).ToArray();

  // ---------- Rendering ----------

  [TestMethod]
  public void FileSelect_Renders_ButtonInputAndHint() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.Id, "picker")
      .Add(c => c.Accept, "image/*")
      .Add(c => c.Capture, "environment")
      .Add(c => c.Class, "mine"));
    IElement root = cut.Find("div.pixata-blazor-upload");
    Assert.IsTrue(root.ClassList.Contains("pixata-blazor-fileselect"));
    Assert.IsTrue(root.ClassList.Contains("mine"));
    Assert.AreEqual("Select files...", cut.Find("button.pixata-blazor-upload-button").TextContent.Trim());
    IElement input = cut.Find("input[type=file]");
    Assert.AreEqual("picker", input.Id);
    Assert.AreEqual("image/*", input.GetAttribute("accept"));
    Assert.AreEqual("environment", input.GetAttribute("capture"));
    Assert.IsTrue(input.HasAttribute("multiple"));
    Assert.AreEqual("-1", input.GetAttribute("tabindex"));
    Assert.AreEqual("Drag and drop files here", cut.Find(".pixata-blazor-upload-dropzone-hint").TextContent);
    Assert.IsTrue(cut.Find("ul.pixata-blazor-upload-files").HasAttribute("hidden"));
  }

  [TestMethod]
  public void Upload_Renders_ItsOwnHint() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p.Add(c => c.SaveUrl, SaveUrl));
    Assert.AreEqual("Drag and drop files here to upload", cut.Find(".pixata-blazor-upload-dropzone-hint").TextContent);
    Assert.IsFalse(cut.Find("div.pixata-blazor-upload").ClassList.Contains("pixata-blazor-fileselect"));
  }

  [TestMethod]
  public void Init_IsPassedTheOptions() {
    using Browser browser = new();
    browser.Context.Render<PixataFileSelect>(p => p.Add(c => c.Multiple, false).Add(c => c.DropZoneId, "zone"));
    JSRuntimeInvocation init = browser.Calls("init").Single();
    string options = System.Text.Json.JsonSerializer.Serialize(init.Arguments[4]);
    StringAssert.Contains(options, "\"multiple\":false");
    StringAssert.Contains(options, "\"dropZoneId\":\"zone\"");
  }

  [TestMethod]
  public void Init_ChangedParameters_UpdateTheBrowser() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    cut.Render(p => p.Add(c => c.Enabled, false));
    Assert.AreEqual(1, browser.Calls("update").Count);
  }

  [TestMethod]
  public void Disabled_DisablesTheButtonAndInput() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p.Add(c => c.Enabled, false));
    Assert.IsTrue(cut.Find("button.pixata-blazor-upload-button").HasAttribute("disabled"));
    Assert.IsTrue(cut.Find("input[type=file]").HasAttribute("disabled"));
    Assert.IsTrue(cut.Find("div.pixata-blazor-upload").ClassList.Contains("pixata-blazor-upload-disabled"));
  }

  [TestMethod]
  public void SelectFilesButtonTemplate_ReplacesTheText() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.SelectFilesButtonTemplate, "<b>Choose</b>"));
    Assert.AreEqual("Choose", cut.Find("button.pixata-blazor-upload-button b").TextContent);
  }

  [TestMethod]
  public void Messages_ReplaceTheText() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.Messages, new PixataUploadMessages { SelectFiles = "Dewis ffeiliau...", FileSelectDropZoneHint = "Llusgwch ffeiliau yma" }));
    Assert.AreEqual("Dewis ffeiliau...", cut.Find("button.pixata-blazor-upload-button").TextContent.Trim());
    Assert.AreEqual("Llusgwch ffeiliau yma", cut.Find(".pixata-blazor-upload-dropzone-hint").TextContent);
  }

  // ---------- Selecting and validating ----------

  [TestMethod]
  public async Task FileSelect_PickingFiles_AddsThemAndRaisesOnSelect() {
    using Browser browser = new();
    List<PixataFileSelectFileInfo> selected = [];
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnSelect, (PixataFileSelectEventArgs e) => selected.AddRange(e.Files)));
    await browser.Pick(cut, ("photo.png", 2048), ("notes.txt", 580));
    CollectionAssert.AreEqual(new[] { "photo.png", "notes.txt" }, Names(cut));
    CollectionAssert.AreEqual(new[] { "photo.png", "notes.txt" }, selected.Select(f => f.Name).ToArray());
    PixataFileSelectFileInfo photo = selected[0];
    Assert.AreEqual(".png", photo.Extension);
    Assert.AreEqual("image/png", photo.ContentType);
    Assert.AreEqual(2048, photo.Size);
    Assert.IsTrue(photo.IsValid);
    Assert.IsNotNull(photo.LastModified);
    Assert.IsFalse(string.IsNullOrEmpty(photo.Id));
    CollectionAssert.AreEqual(new[] { "2.00 KB", "0.57 KB" }, cut.FindAll(".pixata-blazor-upload-file-size").Select(e => e.TextContent).ToArray());
    Assert.IsFalse(cut.Find("ul.pixata-blazor-upload-files").HasAttribute("hidden"));
  }

  [TestMethod]
  public async Task Validation_FlagsAndExplainsInvalidFiles() {
    using Browser browser = new();
    List<PixataFileSelectFileInfo> selected = [];
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.AllowedExtensions, ["png", ".TXT"])
      .Add(c => c.MinFileSize, 10)
      .Add(c => c.MaxFileSize, 1000)
      .Add(c => c.OnSelect, (PixataFileSelectEventArgs e) => selected.AddRange(e.Files)));
    await browser.Pick(cut, ("ok.PNG", 100), ("setup.exe", 100), ("big.txt", 5000), ("tiny.txt", 5));
    Assert.IsTrue(selected[0].IsValid);
    Assert.IsTrue(selected[1].InvalidExtension);
    Assert.IsTrue(selected[2].InvalidMaxFileSize);
    Assert.IsTrue(selected[3].InvalidMinFileSize);
    CollectionAssert.AreEqual(new[] { "File type not allowed.", "File size too large.", "File size too small." },
      cut.FindAll(".pixata-blazor-upload-file-validation-message").Select(e => e.TextContent).ToArray());
    Assert.AreEqual(3, cut.FindAll("li.pixata-blazor-upload-file-invalid").Count);
  }

  [TestMethod]
  public async Task OnSelect_Cancelled_AddsNothingAndForgetsTheFiles() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnSelect, (PixataFileSelectEventArgs e) => e.IsCancelled = true));
    await browser.Pick(cut, ("photo.png", 2048));
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
    Assert.AreEqual(1, browser.Calls("forgetFiles").Count);
  }

  [TestMethod]
  public async Task OnSelect_TakingAFileOut_LeavesJustThatOneOut() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnSelect, (PixataFileSelectEventArgs e) => e.Files.RemoveAll(f => f.Name == "skip.txt")));
    await browser.Pick(cut, ("keep.txt", 10), ("skip.txt", 10));
    CollectionAssert.AreEqual(new[] { "keep.txt" }, Names(cut));
  }

  [TestMethod]
  public async Task SingleFile_ANewFileReplacesTheOldOne() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p.Add(c => c.Multiple, false));
    Assert.IsFalse(cut.Find("input[type=file]").HasAttribute("multiple"));
    await browser.Pick(cut, ("first.txt", 10));
    await browser.Pick(cut, ("second.txt", 10), ("third.txt", 10));
    CollectionAssert.AreEqual(new[] { "second.txt" }, Names(cut));
  }

  [TestMethod]
  public async Task SingleFile_OnSelectLeavingEveryFileOut_KeepsTheOldOne() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.Multiple, false)
      .Add(c => c.OnSelect, (PixataFileSelectEventArgs e) => e.Files.RemoveAll(f => f.Name.StartsWith("reject"))));
    await browser.Pick(cut, ("first.txt", 10));
    await browser.Pick(cut, ("reject.txt", 10));
    CollectionAssert.AreEqual(new[] { "first.txt" }, Names(cut));
    Assert.AreEqual(0, browser.Calls("forgetFiles").Count(c => ((string[])c.Arguments[1]!).Contains("b1")));
  }

  [TestMethod]
  public async Task Stream_ReadsTheFileFromTheBrowser() {
    using Browser browser = new();
    FakeStreamReference reference = new(Encoding.UTF8.GetBytes("Hello"));
    browser.Upload.Setup<IJSStreamReference>("getFile", _ => true).SetResult(reference);
    string content = "";
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnSelect, async (PixataFileSelectEventArgs e) => {
        using MemoryStream copy = new();
        await e.Files[0].Stream!.CopyToAsync(copy);
        content = Encoding.UTF8.GetString(copy.ToArray());
      }));
    await browser.Pick(cut, ("hello.txt", 5));
    Assert.AreEqual("Hello", content);
    Assert.AreEqual(5, reference.MaxAllowedSize);
    JSRuntimeInvocation getFile = browser.Calls("getFile").Single();
    Assert.AreEqual(7, getFile.Arguments[0]);
    Assert.AreEqual("b1", getFile.Arguments[1]);
  }

  [TestMethod]
  public async Task Stream_ReadToTheEnd_LetsGoOfTheBrowserFile() {
    using Browser browser = new();
    FakeStreamReference reference = new(Encoding.UTF8.GetBytes("Hello"));
    browser.Upload.Setup<IJSStreamReference>("getFile", _ => true).SetResult(reference);
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnSelect, async (PixataFileSelectEventArgs e) => {
        // Read without disposing the stream, as most code does
        using MemoryStream copy = new();
        await e.Files[0].Stream!.CopyToAsync(copy);
      }));
    await browser.Pick(cut, ("hello.txt", 5));
    // Otherwise the browser holds on to the file (and the server to the stream) until the user leaves the app
    Assert.IsTrue(reference.Disposed);
    Assert.AreEqual(0, await cut.Instance.CurrentFiles[0].Stream!.ReadAsync(new byte[10]));
    Assert.AreEqual(1, browser.Calls("getFile").Count);
  }

  [TestMethod]
  public async Task Stream_ReadAfterDisposing_Throws() {
    using Browser browser = new();
    browser.Upload.Setup<IJSStreamReference>("getFile", _ => true).SetResult(new FakeStreamReference(Encoding.UTF8.GetBytes("Hello")));
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("hello.txt", 5));
    Stream stream = cut.Instance.CurrentFiles[0].OpenReadStream();
    await stream.DisposeAsync();
    await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () => await stream.ReadAsync(new byte[10]));
    Assert.AreEqual(0, browser.Calls("getFile").Count);
  }

  [TestMethod]
  public async Task Stream_CanOnlyBeReadAsynchronously() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("hello.txt", 5));
    Stream stream = cut.Instance.CurrentFiles[0].Stream!;
    Assert.IsFalse(stream.CanSeek);
    Assert.AreEqual(5, stream.Length);
    Assert.ThrowsException<NotSupportedException>(() => stream.ReadByte());
  }

  [TestMethod]
  public async Task OpenReadStream_TooBig_Throws() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("big.pdf", 600000));
    Assert.ThrowsException<IOException>(() => cut.Instance.CurrentFiles[0].OpenReadStream());
    Assert.IsNotNull(cut.Instance.CurrentFiles[0].OpenReadStream(1000000));
  }

  [TestMethod]
  public void Files_AreShownAndHaveNoStream() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.Files, [new PixataFileSelectFileInfo { Name = "Report.pdf", Size = 2 * 1024 * 1024 }]));
    PixataFileSelectFileInfo file = cut.Instance.CurrentFiles.Single();
    Assert.AreEqual(".pdf", file.Extension);
    Assert.IsFalse(string.IsNullOrEmpty(file.Id));
    Assert.IsNull(file.Stream);
    Assert.ThrowsException<InvalidOperationException>(() => file.OpenReadStream());
    Assert.AreEqual("2.00 MB", cut.Find(".pixata-blazor-upload-file-size").TextContent);
    Assert.IsNotNull(cut.Find("li .pixata-blazor-svg-icon-file-pdf"));
  }

  [TestMethod]
  public void Files_OnlyLoadsTheFirstTime() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.Files, [new PixataFileSelectFileInfo { Name = "a.pdf" }]));
    cut.Render(p => p.Add(c => c.Files, [new PixataFileSelectFileInfo { Name = "b.pdf" }]));
    CollectionAssert.AreEqual(new[] { "a.pdf" }, Names(cut));
  }

  // ---------- Templates ----------

  [TestMethod]
  public async Task FileInfoTemplate_ReplacesTheNameAndSize() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.FileInfoTemplate, context => $"<em>{context.File.Name}!</em>"));
    await browser.Pick(cut, ("photo.png", 10));
    Assert.AreEqual("photo.png!", cut.Find("li .pixata-blazor-upload-file-info em").TextContent);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-file-name").Count);
    Assert.IsNotNull(cut.Find("li .pixata-blazor-upload-remove"));
  }

  [TestMethod]
  public async Task FileTemplate_ReplacesTheWholeItem() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.FileTemplate, context => $"<span class=\"custom\">{context.File.Name}</span>"));
    await browser.Pick(cut, ("photo.png", 10));
    Assert.AreEqual("photo.png", cut.Find("li.pixata-blazor-upload-file span.custom").TextContent);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-remove").Count);
  }

  // ---------- Removing ----------

  [TestMethod]
  public async Task FileSelect_RemoveButton_RaisesOnRemoveAndRemoves() {
    using Browser browser = new();
    List<string> removed = [];
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnRemove, (PixataFileSelectEventArgs e) => removed.AddRange(e.Files.Select(f => f.Name))));
    await browser.Pick(cut, ("a.txt", 10), ("b.txt", 10));
    await cut.FindAll(".pixata-blazor-upload-remove")[0].ClickAsync(new());
    CollectionAssert.AreEqual(new[] { "a.txt" }, removed);
    CollectionAssert.AreEqual(new[] { "b.txt" }, Names(cut));
    Assert.AreEqual("b1", ((string[])browser.Calls("forgetFiles").Last().Arguments[1]!).Single());
  }

  [TestMethod]
  public async Task FileSelect_OnRemoveCancelled_KeepsTheFile() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>(p => p
      .Add(c => c.OnRemove, (PixataFileSelectEventArgs e) => e.IsCancelled = true));
    await browser.Pick(cut, ("a.txt", 10));
    await cut.Find(".pixata-blazor-upload-remove").ClickAsync(new());
    CollectionAssert.AreEqual(new[] { "a.txt" }, Names(cut));
  }

  [TestMethod]
  public async Task ClearFiles_EmptiesTheList() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("a.txt", 10), ("b.txt", 10));
    await cut.InvokeAsync(() => cut.Instance.ClearFiles());
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
    Assert.IsTrue(cut.Find("ul.pixata-blazor-upload-files").HasAttribute("hidden"));
  }

  [TestMethod]
  public async Task OpenSelectFilesDialog_AsksTheBrowser() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await cut.InvokeAsync(() => cut.Instance.OpenSelectFilesDialog());
    Assert.AreEqual(7, browser.Calls("openDialog").Single().Arguments[0]);
  }

  // ---------- Keyboard and ARIA ----------

  [TestMethod]
  public async Task Aria_ListAndButtonDescribeTheFiles() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    IElement button = cut.Find("button.pixata-blazor-upload-button");
    Assert.AreEqual("false", button.GetAttribute("aria-expanded"));
    Assert.IsNull(button.GetAttribute("aria-label"));
    await browser.Pick(cut, ("a.txt", 10), ("b.exe", 10));
    button = cut.Find("button.pixata-blazor-upload-button");
    IElement list = cut.Find("ul.pixata-blazor-upload-files");
    Assert.AreEqual("list", list.GetAttribute("role"));
    Assert.AreEqual(list.Id, button.GetAttribute("aria-controls"));
    Assert.AreEqual("true", button.GetAttribute("aria-expanded"));
    StringAssert.Contains(button.GetAttribute("aria-label"), "2 file(s)");
    IElement item = cut.FindAll("li")[0];
    Assert.AreEqual("listitem", item.GetAttribute("role"));
    Assert.AreEqual("-1", item.GetAttribute("tabindex"));
    Assert.AreEqual("a.txt, 0.01 KB", item.GetAttribute("aria-label"));
    Assert.AreEqual("-1", cut.Find(".pixata-blazor-upload-remove").GetAttribute("tabindex"));
    Assert.AreEqual("Remove", cut.Find(".pixata-blazor-upload-remove").GetAttribute("aria-label"));
  }

  [TestMethod]
  public async Task Keyboard_ArrowsMoveThroughTheList() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("a.txt", 10), ("b.txt", 10), ("c.txt", 10));
    cut.Find("button.pixata-blazor-upload-button").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    cut.FindAll("li")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
    cut.FindAll("li")[1].KeyDown(new KeyboardEventArgs { Key = "End" });
    cut.FindAll("li")[2].KeyDown(new KeyboardEventArgs { Key = "Home" });
    cut.FindAll("li")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
    string[] selectors = browser.Shared.Invocations["focusSelector"].Select(i => (string)i.Arguments[1]!).ToArray();
    CollectionAssert.AreEqual(new[] { "li[data-index='0']", "li[data-index='1']", "li[data-index='2']", "li[data-index='0']" }, selectors);
    Assert.AreEqual(1, browser.Shared.Invocations["focusElement"].Count);
  }

  [TestMethod]
  public async Task Keyboard_DeleteRemovesTheFileAndFocusesTheNext() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("a.txt", 10), ("b.txt", 10));
    await cut.FindAll("li")[0].KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });
    CollectionAssert.AreEqual(new[] { "b.txt" }, Names(cut));
    Assert.AreEqual("li[data-index='0']", (string)browser.Shared.Invocations["focusSelector"].Last().Arguments[1]!);
    await cut.FindAll("li")[0].KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
    Assert.AreEqual(1, browser.Shared.Invocations["focusElement"].Count);
  }

  [TestMethod]
  public async Task Keyboard_DeleteDoesNothingWhenDisabled() {
    using Browser browser = new();
    IRenderedComponent<PixataFileSelect> cut = browser.Context.Render<PixataFileSelect>();
    await browser.Pick(cut, ("a.txt", 10));
    cut.Render(p => p.Add(c => c.Enabled, false));
    await cut.Find("li").KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });
    Assert.AreEqual(1, cut.Instance.CurrentFiles.Count);
  }

  // ---------- Uploading ----------

  [TestMethod]
  public async Task Upload_PickingFiles_UploadsThemWithTheHeadersAndDataFromOnUpload() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.SaveField, "attachments")
      .Add(c => c.WithCredentials, true)
      .Add(c => c.OnUpload, (PixataUploadEventArgs e) => {
        e.RequestHeaders["Authorization"] = "Bearer token";
        e.RequestData["recordId"] = 42;
      }));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    PixataUploadBrowserRequest request = browser.Requests[0].Request;
    Assert.AreEqual(SaveUrl, request.Url);
    Assert.AreEqual("attachments", request.Field);
    Assert.AreEqual("b1", request.FileId);
    Assert.IsTrue(request.WithCredentials);
    Assert.AreEqual("Bearer token", request.Headers["Authorization"]);
    Assert.AreEqual("42", request.Data["recordId"]);
    Assert.IsNull(request.Chunk);
    Assert.AreEqual(PixataUploadFileStatus.Uploading, cut.Instance.CurrentFiles[0].Status);
    Assert.AreEqual("Uploading...", cut.Find(".pixata-blazor-upload-status").TextContent.Trim());
    Assert.IsNotNull(cut.Find("li.pixata-blazor-upload-file-progress .pixata-blazor-upload-cancel"));
  }

  [TestMethod]
  public async Task Upload_Progress_UpdatesTheBarAndRaisesOnProgress() {
    using Browser browser = new();
    List<int> progress = [];
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnProgress, (PixataUploadProgressEventArgs e) => progress.Add(e.Progress)));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await browser.Progress(cut, browser.Requests[0].Id, 40);
    CollectionAssert.AreEqual(new[] { 40 }, progress);
    IElement bar = cut.Find(".pixata-blazor-upload-progressbar");
    Assert.AreEqual("progressbar", bar.GetAttribute("role"));
    Assert.AreEqual("40", bar.GetAttribute("aria-valuenow"));
    StringAssert.Contains(cut.Find(".pixata-blazor-upload-progressbar-value").GetAttribute("style"), "width:40%");
    Assert.AreEqual("40%", cut.Find(".pixata-blazor-upload-pct").TextContent);
  }

  [TestMethod]
  public async Task Upload_Success_MarksTheFileUploaded() {
    using Browser browser = new();
    PixataUploadSuccessEventArgs? success = null;
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnSuccess, (PixataUploadSuccessEventArgs e) => success = e));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await browser.Complete(cut, browser.Requests[0].Id, 200);
    cut.WaitForAssertion(() => Assert.IsNotNull(success));
    Assert.AreEqual(PixataUploadOperationType.Upload, success!.Operation);
    Assert.AreEqual(200, success.Request.Status);
    Assert.AreEqual("saved", success.Request.ResponseText);
    PixataUploadFileInfo file = cut.Instance.CurrentFiles[0];
    Assert.AreEqual(PixataUploadFileStatus.Uploaded, file.Status);
    Assert.AreEqual(100, file.Progress);
    Assert.IsNotNull(cut.Find("li.pixata-blazor-upload-file-success"));
    Assert.AreEqual("File successfully uploaded.", cut.Find(".pixata-blazor-upload-file-validation-message").TextContent);
    Assert.AreEqual("Done", cut.Find(".pixata-blazor-upload-status-done").TextContent.Trim());
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-progressbar").Count);
  }

  [TestMethod]
  public async Task Upload_Failure_MarksTheFileFailedAndRetryUploadsItAgain() {
    using Browser browser = new();
    List<PixataUploadErrorEventArgs> errors = [];
    int uploads = 0;
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnUpload, (PixataUploadEventArgs _) => uploads++)
      .Add(c => c.OnError, (PixataUploadErrorEventArgs e) => errors.Add(e)));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await browser.Complete(cut, browser.Requests[0].Id, 500);
    cut.WaitForAssertion(() => Assert.AreEqual(1, errors.Count));
    Assert.AreEqual(500, errors[0].Request.Status);
    Assert.AreEqual(PixataUploadFileStatus.Failed, cut.Instance.CurrentFiles[0].Status);
    Assert.AreEqual("File failed to upload.", cut.Find("li.pixata-blazor-upload-file-error .pixata-blazor-upload-file-validation-message").TextContent);
    Assert.IsNotNull(cut.Find(".pixata-blazor-upload-status-failed"));
    // Retrying waits for the new upload to finish, so the click isn't awaited here
    _ = cut.Find(".pixata-blazor-upload-retry").ClickAsync(new());
    cut.WaitForAssertion(() => Assert.AreEqual(2, browser.Requests.Count));
    Assert.AreEqual(2, uploads);
    Assert.AreNotEqual(browser.Requests[0].Id, browser.Requests[1].Id);
  }

  [TestMethod]
  public async Task Upload_KeyboardEnter_RetriesAFailedFile() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p.Add(c => c.SaveUrl, SaveUrl));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await browser.Complete(cut, browser.Requests[0].Id, 500);
    cut.WaitForAssertion(() => Assert.AreEqual(PixataUploadFileStatus.Failed, cut.Instance.CurrentFiles[0].Status));
    _ = cut.Find("li").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
    cut.WaitForAssertion(() => Assert.AreEqual(2, browser.Requests.Count));
  }

  [TestMethod]
  public async Task Upload_OnUploadCancelled_DoesNotSend() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnUpload, (PixataUploadEventArgs e) => e.IsCancelled = true));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(PixataUploadFileStatus.Selected, cut.Instance.CurrentFiles[0].Status));
    Assert.AreEqual(0, browser.Requests.Count);
  }

  [TestMethod]
  public async Task Upload_InvalidFiles_AreNotUploaded() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.AllowedExtensions, [".png"]));
    await browser.Pick(cut, ("photo.png", 2048), ("setup.exe", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    Assert.AreEqual("b1", browser.Requests[0].Request.FileId);
    Assert.AreEqual(PixataUploadFileStatus.Selected, cut.Instance.CurrentFiles[1].Status);
    Assert.AreEqual("File type not allowed.", cut.Find("li.pixata-blazor-upload-file-invalid .pixata-blazor-upload-file-validation-message").TextContent);
  }

  [TestMethod]
  public async Task Upload_NotAutomatic_WaitsForTheUploadButton() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.AutoUpload, false));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-actions-bar").Count);
    await browser.Pick(cut, ("a.txt", 10), ("b.txt", 10));
    Assert.AreEqual(0, browser.Requests.Count);
    IElement upload = cut.Find(".pixata-blazor-upload-actions-bar .pixata-blazor-upload-selected");
    Assert.AreEqual("Upload", upload.TextContent.Trim());
    Assert.IsTrue(upload.ClassList.Contains("pixata-blazor-button-primary"));
    _ = upload.ClickAsync(new());
    cut.WaitForAssertion(() => Assert.AreEqual(2, browser.Requests.Count));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-actions-bar").Count);
  }

  [TestMethod]
  public async Task Upload_ClearButton_RaisesOnClearAndClears() {
    using Browser browser = new();
    int cleared = 0;
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.AutoUpload, false)
      .Add(c => c.OnClear, (PixataUploadClearEventArgs e) => cleared = e.Files.Count));
    await browser.Pick(cut, ("a.txt", 10), ("b.txt", 10));
    await cut.Find(".pixata-blazor-upload-clear-selected").ClickAsync(new());
    Assert.AreEqual(2, cleared);
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
  }

  [TestMethod]
  public async Task Upload_OnClearCancelled_KeepsTheFiles() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.AutoUpload, false)
      .Add(c => c.OnClear, (PixataUploadClearEventArgs e) => e.IsCancelled = true));
    await browser.Pick(cut, ("a.txt", 10));
    await cut.Find(".pixata-blazor-upload-clear-selected").ClickAsync(new());
    Assert.AreEqual(1, cut.Instance.CurrentFiles.Count);
  }

  [TestMethod]
  public async Task Upload_WithoutASaveUrl_Throws() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p.Add(c => c.AutoUpload, false));
    await browser.Pick(cut, ("a.txt", 10));
    await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => cut.InvokeAsync(() => cut.Instance.UploadFiles()));
  }

  // ---------- Cancelling, pausing and resuming ----------

  [TestMethod]
  public async Task Upload_Cancel_RaisesOnCancelStopsTheRequestAndRemovesTheFile() {
    using Browser browser = new();
    int cancelled = 0;
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnCancel, (PixataUploadCancelEventArgs _) => cancelled++));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await cut.Find(".pixata-blazor-upload-cancel").ClickAsync(new());
    Assert.AreEqual(1, cancelled);
    Assert.AreEqual(browser.Requests[0].Id, browser.Calls("cancel").Single().Arguments[1]);
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
  }

  [TestMethod]
  public async Task Upload_KeyboardEscape_Cancels() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p.Add(c => c.SaveUrl, SaveUrl));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await cut.Find("li").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
  }

  [TestMethod]
  public async Task Upload_OnCancelCancelled_CarriesOn() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnCancel, (PixataUploadCancelEventArgs e) => e.IsCancelled = true));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await cut.Find(".pixata-blazor-upload-cancel").ClickAsync(new());
    Assert.AreEqual(0, browser.Calls("cancel").Count);
    Assert.AreEqual(PixataUploadFileStatus.Uploading, cut.Instance.CurrentFiles[0].Status);
  }

  [TestMethod]
  public async Task Upload_ChunkSettings_AreSentAndAllowPausingAndResuming() {
    using Browser browser = new();
    List<string> events = [];
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnPause, (PixataUploadPauseEventArgs _) => events.Add("pause"))
      .Add(c => c.OnResume, (PixataUploadResumeEventArgs _) => events.Add("resume"))
      .Add(c => c.UploadSettings, settings => {
        settings.OpenComponent<PixataUploadChunkSettings>(0);
        settings.AddComponentParameter(1, nameof(PixataUploadChunkSettings.Size), 65536L);
        settings.AddComponentParameter(2, nameof(PixataUploadChunkSettings.MaxAutoRetries), 3);
        settings.CloseComponent();
      }));
    await browser.Pick(cut, ("big.pdf", 600000));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    (string requestId, PixataUploadBrowserRequest request) = browser.Requests[0];
    Assert.AreEqual(65536, request.Chunk!.Size);
    Assert.AreEqual(3, request.Chunk.MaxAutoRetries);
    Assert.AreEqual(100, request.Chunk.AutoRetryAfter);
    Assert.AreEqual("chunkMetadata", request.Chunk.MetadataField);
    Assert.AreEqual(requestId, request.UploadId);

    await cut.Find(".pixata-blazor-upload-pause").ClickAsync(new());
    Assert.AreEqual(PixataUploadFileStatus.Paused, cut.Instance.CurrentFiles[0].Status);
    Assert.AreEqual(requestId, browser.Calls("pause").Single().Arguments[1]);
    Assert.IsNotNull(cut.Find("li.pixata-blazor-upload-file-paused"));

    await cut.Find(".pixata-blazor-upload-resume").ClickAsync(new());
    Assert.AreEqual(PixataUploadFileStatus.Uploading, cut.Instance.CurrentFiles[0].Status);
    Assert.AreEqual(requestId, browser.Calls("resume").Single().Arguments[1]);
    CollectionAssert.AreEqual(new[] { "pause", "resume" }, events);
  }

  [TestMethod]
  public async Task Upload_WithoutChunks_CannotBePaused() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p.Add(c => c.SaveUrl, SaveUrl));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-pause").Count);
    await cut.InvokeAsync(() => cut.Instance.PauseFile(cut.Instance.CurrentFiles[0].Id));
    Assert.AreEqual(PixataUploadFileStatus.Uploading, cut.Instance.CurrentFiles[0].Status);
  }

  [TestMethod]
  public void ChunkSettings_OutsideAnUpload_Throws() {
    using Browser browser = new();
    Assert.ThrowsException<InvalidOperationException>(() => browser.Context.Render<PixataUploadChunkSettings>());
  }

  // ---------- Removing uploaded files ----------

  [TestMethod]
  public async Task Upload_RemovingAnUploadedFile_PostsToTheRemoveUrl() {
    using Browser browser = new();
    List<string> log = [];
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.RemoveUrl, RemoveUrl)
      .Add(c => c.RemoveField, "names")
      .Add(c => c.OnRemove, (PixataUploadEventArgs e) => {
        e.RequestData["folder"] = "invoices";
        log.Add("remove");
      })
      .Add(c => c.OnSuccess, (PixataUploadSuccessEventArgs e) => log.Add($"success {e.Operation}")));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await browser.Complete(cut, browser.Requests[0].Id, 200);
    cut.WaitForAssertion(() => Assert.AreEqual(PixataUploadFileStatus.Uploaded, cut.Instance.CurrentFiles[0].Status));

    Task click = cut.Find(".pixata-blazor-upload-remove").ClickAsync(new());
    cut.WaitForAssertion(() => Assert.AreEqual(2, browser.Requests.Count));
    (string removeId, PixataUploadBrowserRequest remove) = browser.Requests[1];
    Assert.AreEqual(RemoveUrl, remove.Url);
    Assert.AreEqual("names", remove.Field);
    CollectionAssert.AreEqual(new[] { "photo.png" }, remove.Values);
    Assert.IsNull(remove.FileId);
    Assert.AreEqual("invoices", remove.Data["folder"]);
    Assert.AreEqual(1, cut.Instance.CurrentFiles.Count);
    await browser.Complete(cut, removeId, 200);
    await click;
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
    CollectionAssert.AreEqual(new[] { "success Upload", "remove", "success Remove" }, log);
  }

  [TestMethod]
  public async Task Upload_RemoveFailing_KeepsTheFileAndRaisesOnError() {
    using Browser browser = new();
    PixataUploadErrorEventArgs? error = null;
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.RemoveUrl, RemoveUrl)
      .Add(c => c.Files, [new PixataUploadFileInfo { Id = "old", Name = "Old.pdf", Size = 100 }])
      .Add(c => c.OnError, (PixataUploadErrorEventArgs e) => error = e));
    Task click = cut.Find(".pixata-blazor-upload-remove").ClickAsync(new());
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    await browser.Complete(cut, browser.Requests[0].Id, 500);
    await click;
    Assert.AreEqual(PixataUploadOperationType.Remove, error!.Operation);
    Assert.AreEqual(1, cut.Instance.CurrentFiles.Count);
  }

  [TestMethod]
  public async Task Upload_RemovingAFileNotUploaded_SendsNothing() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.RemoveUrl, RemoveUrl)
      .Add(c => c.AutoUpload, false));
    await browser.Pick(cut, ("a.txt", 10));
    await cut.Find(".pixata-blazor-upload-remove").ClickAsync(new());
    Assert.AreEqual(0, browser.Requests.Count);
    Assert.AreEqual(0, cut.Instance.CurrentFiles.Count);
  }

  [TestMethod]
  public void Upload_Files_AreTreatedAsAlreadyOnTheServer() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.Files, [
        new PixataUploadFileInfo { Name = "a.pdf", Status = PixataUploadFileStatus.Selected },
        new PixataUploadFileInfo { Name = "b.pdf", Status = PixataUploadFileStatus.Uploaded },
        new PixataUploadFileInfo { Name = "c.pdf", Status = PixataUploadFileStatus.Uploading }
      ]));
    CollectionAssert.AreEqual(new[] { PixataUploadFileStatus.Blank, PixataUploadFileStatus.Uploaded, PixataUploadFileStatus.Blank },
      cut.Instance.CurrentFiles.Select(f => f.Status).ToArray());
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-upload-actions-bar").Count);
  }

  [TestMethod]
  public async Task Upload_Dispose_TellsTheBrowser() {
    using Browser browser = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p.Add(c => c.SaveUrl, SaveUrl));
    await cut.Instance.DisposeAsync();
    Assert.AreEqual(7, browser.Calls("dispose").Single().Arguments[0]);
  }

  [TestMethod]
  public async Task Upload_DisposedWhileOnUploadRuns_SendsNothingAndFinishes() {
    using Browser browser = new();
    TaskCompletionSource release = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.AutoUpload, false)
      .Add(c => c.OnUpload, async (PixataUploadEventArgs _) => await release.Task));
    await browser.Pick(cut, ("photo.png", 2048));
    Task uploading = Task.CompletedTask;
    await cut.InvokeAsync(() => {
      uploading = cut.Instance.UploadFiles();
    });
    await cut.Instance.DisposeAsync();
    release.SetResult();
    // The browser has forgotten the component, so it would never answer a request sent now
    Assert.AreSame(uploading, await Task.WhenAny(uploading, Task.Delay(2000)));
    Assert.AreEqual(0, browser.Calls("send").Count);
  }

  [TestMethod]
  public async Task Upload_BrowserRefusingTheRequest_MarksTheFileFailed() {
    using Browser browser = new();
    // eg a header name with a space in it, or a SaveUrl that isn't a valid URL
    browser.Upload.SetupVoid("send", _ => true).SetException(new JSException("'bad header' is not a valid HTTP header field name"));
    List<PixataUploadErrorEventArgs> errors = [];
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnError, (PixataUploadErrorEventArgs e) => errors.Add(e)));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(PixataUploadFileStatus.Failed, cut.Instance.CurrentFiles[0].Status));
    Assert.AreEqual(1, errors.Count);
    Assert.AreEqual(0, errors[0].Request.Status);
    StringAssert.Contains(errors[0].Request.StatusText, "bad header");
    Assert.IsNotNull(cut.Find(".pixata-blazor-upload-retry"));
  }

  [TestMethod]
  public async Task Upload_FinishingWhileOnCancelRuns_KeepsTheUploadedFile() {
    using Browser browser = new();
    TaskCompletionSource release = new();
    IRenderedComponent<PixataUpload> cut = browser.Context.Render<PixataUpload>(p => p
      .Add(c => c.SaveUrl, SaveUrl)
      .Add(c => c.OnCancel, async (PixataUploadCancelEventArgs _) => await release.Task));
    await browser.Pick(cut, ("photo.png", 2048));
    cut.WaitForAssertion(() => Assert.AreEqual(1, browser.Requests.Count));
    Task cancelling = Task.CompletedTask;
    await cut.InvokeAsync(() => {
      cancelling = cut.Instance.CancelFile(cut.Instance.CurrentFiles[0].Id);
    });
    await browser.Complete(cut, browser.Requests[0].Id, 200);
    release.SetResult();
    await cancelling;
    // It's on the server now, so dropping it from the list would leave it there with no way to remove it
    Assert.AreEqual(1, cut.Instance.CurrentFiles.Count);
    Assert.AreEqual(PixataUploadFileStatus.Uploaded, cut.Instance.CurrentFiles[0].Status);
    Assert.AreEqual(0, browser.Calls("cancel").Count);
  }

  // ---------- PixataDropZone ----------

  [TestMethod]
  public void DropZone_RendersTheHintNoteAndSettings() {
    using Browser browser = new();
    IRenderedComponent<PixataDropZone> cut = browser.Context.Render<PixataDropZone>(p => p
      .Add(c => c.Id, "zone")
      .Add(c => c.NoteText, "Images only")
      .Add(c => c.Height, "150px")
      .Add(c => c.DragOverClass, "glow")
      .Add(c => c.Multiple, false));
    IElement zone = cut.Find("#zone");
    Assert.IsTrue(zone.ClassList.Contains("pixata-blazor-external-dropzone"));
    Assert.AreEqual("height:150px;", zone.GetAttribute("style"));
    Assert.AreEqual("glow", zone.GetAttribute("data-pixata-dragover-class"));
    Assert.AreEqual("false", zone.GetAttribute("data-pixata-multiple"));
    Assert.IsNull(zone.GetAttribute("data-pixata-disabled"));
    Assert.AreEqual("Drag and drop files here to upload", cut.Find(".pixata-blazor-dropzone-hint").TextContent);
    Assert.AreEqual("Images only", cut.Find(".pixata-blazor-dropzone-note").TextContent);
    Assert.IsNotNull(cut.Find(".pixata-blazor-dropzone-icon svg"));
  }

  [TestMethod]
  public void DropZone_DisabledAndTemplated() {
    using Browser browser = new();
    IRenderedComponent<PixataDropZone> cut = browser.Context.Render<PixataDropZone>(p => p
      .Add(c => c.Id, "zone")
      .Add(c => c.Enabled, false)
      .Add(c => c.Template, "<p class=\"mine\">Drop it</p>"));
    Assert.AreEqual("true", cut.Find("#zone").GetAttribute("data-pixata-disabled"));
    Assert.AreEqual("Drop it", cut.Find("p.mine").TextContent);
    Assert.AreEqual(0, cut.FindAll(".pixata-blazor-dropzone-hint").Count);
  }

  // ---------- Icons ----------

  [TestMethod]
  public void ForFile_PicksAnIconByExtension() {
    Assert.AreSame(PixataSvgIcons.FileImage, PixataSvgIcons.ForFile(".JPG"));
    Assert.AreSame(PixataSvgIcons.FilePdf, PixataSvgIcons.ForFile(".pdf"));
    Assert.AreSame(PixataSvgIcons.File, PixataSvgIcons.ForFile(".xyz"));
    Assert.AreSame(PixataSvgIcons.File, PixataSvgIcons.ForFile(null));
  }
}
