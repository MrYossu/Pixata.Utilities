using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// Controls how <see cref="PixataEditor"/> cleans HTML pasted into it. Put it in the editor's
/// <see cref="PixataEditor.EditorSettings"/>. Whatever these settings say, the pasted HTML is also sanitised, so nothing
/// unsafe gets in
/// </summary>
public class PixataEditorPasteSettings : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataEditor? Editor { get; set; }

  /// <summary>
  /// Turns the list paragraphs Microsoft Word pastes into real bulleted and numbered lists. True by default
  /// </summary>
  [Parameter]
  public bool ConvertMsLists { get; set; } = true;

  /// <summary>
  /// Removes the CSS classes starting with "Mso" that Microsoft Office adds. True by default
  /// </summary>
  [Parameter]
  public bool RemoveMsClasses { get; set; } = true;

  /// <summary>
  /// Removes the style properties starting with "mso-" that Microsoft Office adds. True by default
  /// </summary>
  [Parameter]
  public bool RemoveMsStyles { get; set; } = true;

  /// <summary>
  /// Removes HTML comments. False by default (comments never reach the document, so this only matters to the HTML the
  /// other settings see)
  /// </summary>
  [Parameter]
  public bool RemoveHtmlComments { get; set; }

  /// <summary>
  /// Removes every attribute from the pasted HTML, including link addresses and image sources, so links become plain text
  /// and images are dropped. Takes precedence over the other attribute settings
  /// </summary>
  [Parameter]
  public bool RemoveAllAttributes { get; set; }

  /// <summary>
  /// The attributes to remove from the pasted HTML, eg <c>["style"]</c> to drop pasted colours, fonts and alignment
  /// </summary>
  [Parameter]
  public List<string>? RemoveAttributes { get; set; }

  /// <summary>
  /// The tags to remove from the pasted HTML. Their content is kept, eg <c>["span"]</c> drops pasted colours and fonts but
  /// keeps the text
  /// </summary>
  [Parameter]
  public List<string>? StripTags { get; set; }

  protected override void OnParametersSet() {
    if (Editor is null) {
      throw new InvalidOperationException($"{nameof(PixataEditorPasteSettings)} must be inside the {nameof(PixataEditor.EditorSettings)} of a {nameof(PixataEditor)}");
    }
    Editor.SetPasteSettings(this);
  }

  internal object ToOptions() => new {
    convertMsLists = ConvertMsLists,
    removeMsClasses = RemoveMsClasses,
    removeMsStyles = RemoveMsStyles,
    removeHtmlComments = RemoveHtmlComments,
    removeAllAttributes = RemoveAllAttributes,
    removeAttributes = RemoveAttributes ?? [],
    stripTags = StripTags ?? []
  };

  public void Dispose() {
    Editor?.RemovePasteSettings(this);
    GC.SuppressFinalize(this);
  }
}
