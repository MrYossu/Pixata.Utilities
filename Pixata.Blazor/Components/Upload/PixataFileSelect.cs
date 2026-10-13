using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// Lets the user pick files (with the select button, or by dragging them onto the component) and hands them to your code,
/// which reads them through each file's <see cref="PixataFileSelectFileInfo.Stream"/>. Nothing is sent anywhere unless your
/// code does it. Use <see cref="PixataUpload"/> to have the browser post the files to an endpoint instead
/// </summary>
public class PixataFileSelect : PixataUploadBase<PixataFileSelectFileInfo> {
  /// <summary>
  /// Raised when the user picks or drops files, before they are added to the list. Read the files' contents through their
  /// <see cref="PixataFileSelectFileInfo.Stream"/>. Files that failed validation are included, so check
  /// <see cref="PixataFileInfoBase.IsValid"/>. Set <c>IsCancelled</c> to stop the files being added, or take files out of
  /// <c>Files</c> to leave just those out
  /// </summary>
  [Parameter]
  public EventCallback<PixataFileSelectEventArgs> OnSelect { get; set; }

  /// <summary>
  /// Raised when the user removes a file from the list. Set <c>IsCancelled</c> to keep it
  /// </summary>
  [Parameter]
  public EventCallback<PixataFileSelectEventArgs> OnRemove { get; set; }

  /// <inheritdoc />
  public override async Task RemoveFile(string fileId) {
    if (FindFile(fileId) is not { } file) {
      return;
    }
    PixataFileSelectEventArgs args = new() { Files = [file] };
    await OnRemove.InvokeAsync(args);
    if (!args.IsCancelled) {
      await RemoveFromList(file);
    }
  }

  protected override string DropZoneHint =>
    Text.FileSelectDropZoneHint;

  protected override string ComponentClass =>
    "pixata-blazor-fileselect";

  protected override PixataFileSelectFileInfo CreateFileInfo() =>
    new();

  protected override async Task<bool> RaiseSelect(List<PixataFileSelectFileInfo> files) {
    PixataFileSelectEventArgs args = new() { Files = files };
    await OnSelect.InvokeAsync(args);
    return !args.IsCancelled;
  }
}
