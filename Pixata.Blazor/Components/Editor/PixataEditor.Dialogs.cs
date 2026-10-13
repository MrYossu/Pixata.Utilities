using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

// The dialogs opened by the Insert link, Insert image and View HTML tools
public partial class PixataEditor {
  private enum EditorDialog {
    None,
    Link,
    Image,
    Html
  }

  private EditorDialog _dialog;
  private string _dialogTitle = "";
  private string? _dialogWidth;
  private string? _dialogError;
  private bool _dialogOpened;
  private int _dialogTrap;
  private ElementReference _dialogElement;
  private string _linkHref = "";
  private string _linkText = "";
  private string _linkTitle = "";
  private bool _linkNewWindow;
  private string _imageSrc = "";
  private string _imageAlt = "";
  private string _imageWidth = "";
  private string _imageHeight = "";
  private string _html = "";

  private string DialogTitleId => $"{_id}-dialog-title";
  private string DialogErrorId => $"{_id}-dialog-error";

  private string FieldId(string name) =>
    $"{_id}-{name}";

  private string? DialogStyle =>
    string.IsNullOrWhiteSpace(_dialogWidth) ? null : $"width: {_dialogWidth};";

  private string DialogSubmitText =>
    _dialog == EditorDialog.Html ? "Update" : "Insert";

  private bool DialogCanSubmit =>
    _dialog switch {
      EditorDialog.Link => !string.IsNullOrWhiteSpace(_linkHref),
      EditorDialog.Image => !string.IsNullOrWhiteSpace(_imageSrc),
      _ => true
    };

  private void OpenDialog(EditorDialog dialog, PixataEditorDialogTool tool, string fallbackTitle) {
    _dialog = dialog;
    _dialogTitle = tool.DialogTitle ?? tool.Title ?? fallbackTitle;
    _dialogWidth = tool.DialogWidth;
    _dialogError = null;
    _dialogOpened = true;
  }

  private async Task OpenLinkDialog(PixataEditorDialogTool tool) {
    PixataEditorLinkInfo info = await Invoke("getLinkInfo", new PixataEditorLinkInfo(), _handle);
    _linkHref = info.Href;
    _linkText = info.Text;
    _linkTitle = info.Title;
    _linkNewWindow = info.Target == "_blank";
    OpenDialog(EditorDialog.Link, tool, "Insert link");
  }

  private async Task OpenImageDialog(PixataEditorDialogTool tool) {
    PixataEditorImageInfo info = await Invoke("getImageInfo", new PixataEditorImageInfo(), _handle);
    _imageSrc = info.Src;
    _imageAlt = info.Alt;
    _imageWidth = info.Width;
    _imageHeight = info.Height;
    OpenDialog(EditorDialog.Image, tool, "Insert image");
  }

  private async Task OpenHtmlDialog(PixataEditorDialogTool tool) {
    _html = _handle == 0 ? _sanitisedValue : await Invoke("getHtml", _sanitisedValue, _handle, true);
    OpenDialog(EditorDialog.Html, tool, "View HTML");
  }

  private async Task CloseDialog() {
    _dialog = EditorDialog.None;
    _dialogError = null;
    if (_dialogTrap != 0) {
      int trap = _dialogTrap;
      _dialogTrap = 0;
      await InvokeVoid("releaseFocus", trap);
    }
    _focusEditor = true;
  }

  private void FieldInput(Action<string> set, string? value) {
    set(value ?? "");
    _dialogError = null;
  }

  private void DialogKeyDown(KeyboardEventArgs e) {
    if (e.Key == "Escape") {
      _ = CloseDialog();
    }
  }

  private async Task SubmitDialog() {
    switch (_dialog) {
      case EditorDialog.Link: {
        string? href = PixataHtmlSanitiser.SafeUrl(_linkHref);
        if (href is null) {
          _dialogError = "That web address can't be used. Use a web (http or https), email (mailto) or phone (tel) address.";
          return;
        }
        await CloseDialog();
        await ExecuteAsync(new PixataEditorLinkCommandArgs(href, _linkText.Trim(), _linkNewWindow ? "_blank" : "", _linkTitle.Trim()));
        break;
      }
      case EditorDialog.Image: {
        string? src = PixataHtmlSanitiser.SafeUrl(_imageSrc, true);
        if (src is null) {
          _dialogError = "That web address can't be used for an image. Use a web (http or https) address.";
          return;
        }
        await CloseDialog();
        await ExecuteAsync(new PixataEditorImageCommandArgs(src, _imageAlt.Trim(), _imageWidth.Trim(), _imageHeight.Trim()));
        break;
      }
      case EditorDialog.Html:
        await CloseDialog();
        await ExecuteAsync(new PixataEditorHtmlCommandArgs(PixataEditorCommands.ViewHtml, _html));
        break;
    }
  }
}

internal sealed class PixataEditorLinkInfo {
  public string Href { get; set; } = "";
  public string Text { get; set; } = "";
  public string Title { get; set; } = "";
  public string Target { get; set; } = "";
}

internal sealed class PixataEditorImageInfo {
  public string Src { get; set; } = "";
  public string Alt { get; set; } = "";
  public string Width { get; set; } = "";
  public string Height { get; set; } = "";
}
