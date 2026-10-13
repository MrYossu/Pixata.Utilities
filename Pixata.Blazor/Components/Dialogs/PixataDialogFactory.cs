using System;
using System.Threading.Tasks;

namespace Pixata.Blazor.Components;

/// <summary>
/// Shows the predefined alert, confirm and prompt dialogs from code, and lets you await the user's answer...
/// <code>
/// @inject PixataDialogFactory Dialogs
///
/// if (await Dialogs.ConfirmAsync("Delete this customer?", "Delete")) {
///   ...
/// }
/// </code>
/// <c>AddPixataBlazor()</c> registers it, and it needs a <see cref="PixataDialogHost"/> in your layout to show the dialogs.
/// The button text comes from the app's <see cref="PixataDialogTexts"/>, unless you pass your own
/// </summary>
public class PixataDialogFactory(PixataDialogTexts texts) {
  internal const string MissingHostMessage = "There is no PixataDialogHost to show the dialog. Add <PixataDialogHost /> to your layout (eg MainLayout.razor), "
    + "somewhere that is rendered interactively, so that PixataDialogFactory can show alert, confirm and prompt dialogs.";

  private PixataDialogHost? _host;

  /// <summary>
  /// Shows a message with an OK button, and completes when the user closes it
  /// </summary>
  /// <param name="text">The message</param>
  /// <param name="title">The title. <see cref="PixataDialogTexts.AlertTitle"/> if not given, and no titlebar if that's blank</param>
  /// <param name="okButtonText">The text of the OK button. <see cref="PixataDialogTexts.Ok"/> if not given</param>
  /// <exception cref="InvalidOperationException">There's no <see cref="PixataDialogHost"/> on the page</exception>
  public async Task AlertAsync(string text, string? title = null, string? okButtonText = null) =>
    await Show(new(PixataPredefinedDialogKind.Alert, text, title ?? texts.AlertTitle, okButtonText ?? texts.Ok, null, null));

  /// <summary>
  /// Asks the user to confirm something with OK and Cancel buttons
  /// </summary>
  /// <param name="text">The question</param>
  /// <param name="title">The title. <see cref="PixataDialogTexts.ConfirmTitle"/> if not given, and no titlebar if that's blank</param>
  /// <param name="okButtonText">The text of the OK button, eg <c>Yes</c>. <see cref="PixataDialogTexts.Ok"/> if not given</param>
  /// <param name="cancelButtonText">The text of the Cancel button, eg <c>No</c>. <see cref="PixataDialogTexts.Cancel"/> if not given</param>
  /// <returns>True if the user clicked OK, false if they clicked Cancel or pressed Esc</returns>
  /// <exception cref="InvalidOperationException">There's no <see cref="PixataDialogHost"/> on the page</exception>
  public async Task<bool> ConfirmAsync(string text, string? title = null, string? okButtonText = null, string? cancelButtonText = null) =>
    (await Show(new(PixataPredefinedDialogKind.Confirm, text, title ?? texts.ConfirmTitle, okButtonText ?? texts.Ok, cancelButtonText ?? texts.Cancel, null))).Accepted;

  /// <summary>
  /// Asks the user to type something, with OK and Cancel buttons
  /// </summary>
  /// <param name="text">The question</param>
  /// <param name="title">The title. <see cref="PixataDialogTexts.PromptTitle"/> if not given, and no titlebar if that's blank</param>
  /// <param name="defaultText">The text the text box starts with. It is selected, so the user can type over it</param>
  /// <param name="okButtonText">The text of the OK button. <see cref="PixataDialogTexts.Ok"/> if not given</param>
  /// <param name="cancelButtonText">The text of the Cancel button. <see cref="PixataDialogTexts.Cancel"/> if not given</param>
  /// <returns>What the user typed if they clicked OK (or pressed Enter), or null if they clicked Cancel or pressed Esc</returns>
  /// <exception cref="InvalidOperationException">There's no <see cref="PixataDialogHost"/> on the page</exception>
  public async Task<string?> PromptAsync(string text, string? title = null, string? defaultText = null, string? okButtonText = null, string? cancelButtonText = null) {
    PixataPredefinedDialogResult result = await Show(new(PixataPredefinedDialogKind.Prompt, text, title ?? texts.PromptTitle, okButtonText ?? texts.Ok, cancelButtonText ?? texts.Cancel, defaultText ?? ""));
    return result.Accepted ? result.Value ?? "" : null;
  }

  internal void Attach(PixataDialogHost host) =>
    _host = host;

  internal void Detach(PixataDialogHost host) {
    if (ReferenceEquals(_host, host)) {
      _host = null;
    }
  }

  private Task<PixataPredefinedDialogResult> Show(PixataPredefinedDialog dialog) =>
    (_host ?? throw new InvalidOperationException(MissingHostMessage)).Show(dialog);
}

internal enum PixataPredefinedDialogKind {
  Alert,
  Confirm,
  Prompt
}

internal readonly record struct PixataPredefinedDialogResult(bool Accepted, string? Value);

/// <summary>
/// One alert, confirm or prompt dialog waiting for the user
/// </summary>
internal sealed class PixataPredefinedDialog(PixataPredefinedDialogKind kind, string text, string title, string okButtonText, string? cancelButtonText, string? value) {
  public PixataPredefinedDialogKind Kind { get; } = kind;

  public string Text { get; } = text;

  public string Title { get; } = title;

  public string OkButtonText { get; } = okButtonText;

  public string? CancelButtonText { get; } = cancelButtonText;

  /// <summary>
  /// What's in a prompt's text box
  /// </summary>
  public string? Value { get; set; } = value;

  public TaskCompletionSource<PixataPredefinedDialogResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
