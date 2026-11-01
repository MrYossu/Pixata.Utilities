using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Pixata.Blazor.Components;
using Pixata.Extensions;

namespace Pixata.Blazor.Extensions;

/// <summary>
/// Has the browser save a file that the app has made, or print an HTML document, without leaving the page. Uses the package's
/// shared JavaScript module, so there's nothing to register or add to the page
/// </summary>
public static class PixataFiles {
  /// <summary>
  /// Has the browser save a file, as if the user had downloaded it
  /// </summary>
  /// <param name="js">The page's <see cref="IJSRuntime"/></param>
  /// <param name="fileName">The name the browser suggests for the file, eg <c>Invoice 1234.pdf</c></param>
  /// <param name="contentType">The file's MIME type, eg <c>application/pdf</c></param>
  /// <param name="content">The file</param>
  /// <returns>Success, or a failure with the reason the file couldn't be saved. If the user has closed the page, there's no one to tell, so
  /// this returns success</returns>
  public static async Task<ApiResponse<Yunit>> SaveFile(IJSRuntime js, string fileName, string contentType, byte[] content) {
    using MemoryStream stream = new(content, false);
    return await SaveFile(js, fileName, contentType, stream);
  }

  /// <summary>
  /// Has the browser save a file, as if the user had downloaded it. The stream is read from its current position, and is left open
  /// </summary>
  /// <param name="js">The page's <see cref="IJSRuntime"/></param>
  /// <param name="fileName">The name the browser suggests for the file, eg <c>Invoice 1234.pdf</c></param>
  /// <param name="contentType">The file's MIME type, eg <c>application/pdf</c></param>
  /// <param name="content">The file. Rewind a <see cref="MemoryStream"/> you have just written to, or the file will be empty</param>
  /// <returns>Success, or a failure with the reason the file couldn't be saved. If the user has closed the page, there's no one to tell, so
  /// this returns success</returns>
  public static Task<ApiResponse<Yunit>> SaveFile(IJSRuntime js, string fileName, string contentType, Stream content) =>
    Call(js, async module => {
      using DotNetStreamReference reference = new(content, leaveOpen: true);
      await module.InvokeVoidAsync("downloadFile", fileName, contentType, reference);
    });

  /// <summary>
  /// Prints an HTML document, such as one made by <c>DocumentTemplateHelper.CreateHtmlFromTemplate</c> in Pixata.AspNetCore, without
  /// leaving the page. The document is put in a hidden frame, where scripts can't run, and the browser's print dialog is opened for it.
  /// Relative URLs in it (for images and style sheets) are relative to the page's address
  /// </summary>
  /// <param name="js">The page's <see cref="IJSRuntime"/></param>
  /// <param name="html">The whole document, including the <c>html</c>, <c>head</c> and <c>body</c> elements</param>
  /// <returns>Success once the document has been handed to the browser (which doesn't say whether the user went on to print it), or a
  /// failure with the reason it couldn't be. If the user has closed the page, there's no one to tell, so this returns success</returns>
  public static Task<ApiResponse<Yunit>> PrintHtml(IJSRuntime js, string html) =>
    Call(js, module => module.InvokeVoidAsync("printHtml", html).AsTask());

  private static async Task<ApiResponse<Yunit>> Call(IJSRuntime js, Func<IJSObjectReference, Task> call) {
    try {
      await call(await PixataJs.Module(js));
      return new(ApiResponseStates.Success);
    }
    catch (JSDisconnectedException) {
      // The user has closed the page
      return new(ApiResponseStates.Success);
    }
    catch (Exception ex) {
      // Reported rather than thrown, as an exception that escapes an event handler ends a Blazor Server circuit
      return new(ApiResponseStates.Failure, Message: $"({ex.GetType().Name}) {ex.Message}");
    }
  }
}
