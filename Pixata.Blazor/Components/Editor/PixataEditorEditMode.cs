namespace Pixata.Blazor.Components;

/// <summary>
/// How <see cref="PixataEditor"/> holds the document being edited
/// </summary>
public enum PixataEditorEditMode {
  /// <summary>
  /// The document is in an iframe, so the page's styles don't reach it. The editor's content styles are copied into the
  /// iframe for you. This is the default
  /// </summary>
  Iframe,

  /// <summary>
  /// The document is in a div in the page, so it picks up the page's styles
  /// </summary>
  Div
}
