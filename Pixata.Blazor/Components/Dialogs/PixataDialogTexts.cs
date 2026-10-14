namespace Pixata.Blazor.Components;

/// <summary>
/// The text on the buttons of the predefined dialogs, and the tooltips (and screen reader labels) of the dialog and window
/// titlebar buttons. <c>AddPixataBlazor()</c> registers one with the English defaults. To translate them for the whole app,
/// register your own before calling <c>AddPixataBlazor()</c>, eg built from an <c>IStringLocalizer</c>...
/// <code>
/// builder.Services.AddScoped(sp => {
///   IStringLocalizer&lt;Shared&gt; l = sp.GetRequiredService&lt;IStringLocalizer&lt;Shared&gt;&gt;();
///   return new PixataDialogTexts { Ok = l["OK"], Cancel = l["Cancel"] };
/// });
/// </code>
/// The text of individual predefined dialogs can also be set when they are shown, see <see cref="PixataDialogFactory"/>
/// </summary>
public class PixataDialogTexts {
  /// <summary>
  /// The button that accepts an alert, confirm or prompt dialog
  /// </summary>
  public string Ok { get; set; } = "OK";

  /// <summary>
  /// The button that cancels a confirm or prompt dialog
  /// </summary>
  public string Cancel { get; set; } = "Cancel";

  /// <summary>
  /// The tooltip of the close button in the titlebar of a dialog or window
  /// </summary>
  public string Close { get; set; } = "Close";

  /// <summary>
  /// The tooltip of a window's minimise button
  /// </summary>
  public string Minimise { get; set; } = "Minimise";

  /// <summary>
  /// The tooltip of a window's maximise button
  /// </summary>
  public string Maximise { get; set; } = "Maximise";

  /// <summary>
  /// The tooltip of a minimised or maximised window's restore button
  /// </summary>
  public string Restore { get; set; } = "Restore";

  /// <summary>
  /// The title of an alert dialog shown without one. Blank (the default) means no titlebar
  /// </summary>
  public string AlertTitle { get; set; } = "";

  /// <summary>
  /// The title of a confirm dialog shown without one. Blank (the default) means no titlebar
  /// </summary>
  public string ConfirmTitle { get; set; } = "";

  /// <summary>
  /// The title of a prompt dialog shown without one. Blank (the default) means no titlebar
  /// </summary>
  public string PromptTitle { get; set; } = "";
}
