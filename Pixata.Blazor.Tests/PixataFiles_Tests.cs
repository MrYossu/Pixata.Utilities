using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixata.Blazor.Components;
using Pixata.Blazor.Extensions;
using Pixata.Extensions;

namespace Pixata.Blazor.Tests;

[TestClass]
public class PixataFiles_Tests {
  private BunitContext _ctx = null!;

  [TestInitialize]
  public void Initialise() {
    _ctx = new();
    _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
  }

  [TestCleanup]
  public void Cleanup() =>
    _ctx.Dispose();

  [TestMethod]
  public async Task SaveFile_WithBytes_SendsThemToTheBrowser() {
    byte[] content = [1, 2, 3, 4];

    ApiResponse<Yunit> response = await PixataFiles.SaveFile(_ctx.JSInterop.JSRuntime, "Invoice 1.pdf", "application/pdf", content);

    Assert.AreEqual(ApiResponseStates.Success, response.State);
    JSRuntimeInvocation download = _ctx.JSInterop.Invocations["downloadFile"].Single();
    Assert.AreEqual("Invoice 1.pdf", download.Arguments[0]);
    Assert.AreEqual("application/pdf", download.Arguments[1]);
    DotNetStreamReference reference = (DotNetStreamReference)download.Arguments[2]!;
    CollectionAssert.AreEqual(content, ((MemoryStream)reference.Stream).ToArray());
  }

  [TestMethod]
  public async Task SaveFile_WithAStream_LeavesItOpen() {
    using MemoryStream content = new([5, 6, 7]);

    ApiResponse<Yunit> response = await PixataFiles.SaveFile(_ctx.JSInterop.JSRuntime, "Data.csv", "text/csv", content);

    Assert.AreEqual(ApiResponseStates.Success, response.State);
    DotNetStreamReference reference = (DotNetStreamReference)_ctx.JSInterop.Invocations["downloadFile"].Single().Arguments[2]!;
    Assert.AreSame(content, reference.Stream);
    Assert.IsTrue(reference.LeaveOpen);
    Assert.IsTrue(content.CanRead);
  }

  [TestMethod]
  public async Task PrintHtml_SendsTheDocumentToTheBrowser() {
    const string html = "<!DOCTYPE html><html><body>Invoice</body></html>";

    ApiResponse<Yunit> response = await PixataFiles.PrintHtml(_ctx.JSInterop.JSRuntime, html);

    Assert.AreEqual(ApiResponseStates.Success, response.State);
    Assert.AreEqual(html, _ctx.JSInterop.Invocations["printHtml"].Single().Arguments[0]);
  }

  [TestMethod]
  public async Task PrintHtml_WhenTheBrowserHasGone_ReturnsSuccess() {
    _ctx.JSInterop.SetupModule(PixataBlazorScript.Path).SetupVoid("printHtml", _ => true).SetException(new JSDisconnectedException("The circuit has gone"));

    ApiResponse<Yunit> response = await PixataFiles.PrintHtml(_ctx.JSInterop.JSRuntime, "<html></html>");

    Assert.AreEqual(ApiResponseStates.Success, response.State);
  }

  [TestMethod]
  public async Task SaveFile_WhenTheBrowserFails_ReturnsTheError() {
    _ctx.JSInterop.SetupModule(PixataBlazorScript.Path).SetupVoid("downloadFile", _ => true).SetException(new JSException("Not allowed"));

    ApiResponse<Yunit> response = await PixataFiles.SaveFile(_ctx.JSInterop.JSRuntime, "a.txt", "text/plain", [1]);

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    Assert.AreEqual("(JSException) Not allowed", response.Message);
  }

  [TestMethod]
  public async Task PrintHtml_WhenTheModuleCantBeLoaded_ReturnsTheError() {
    ApiResponse<Yunit> response = await PixataFiles.PrintHtml(new FailingImport(), "<html></html>");

    Assert.AreEqual(ApiResponseStates.Failure, response.State);
    Assert.AreEqual("(JSException) Failed to fetch dynamically imported module", response.Message);
  }

  // bUnit won't let a test make the import of a module fail
  private class FailingImport : IJSRuntime {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromException<TValue>(new JSException("Failed to fetch dynamically imported module"));

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      InvokeAsync<TValue>(identifier, args);
  }
}
