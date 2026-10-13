using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// The content of a custom toolbar tool. Put one in the editor's <see cref="PixataEditor.EditorCustomTools"/> for each
/// <see cref="PixataEditorCustomTool"/> in its <see cref="PixataEditor.Tools"/>, with the same <see cref="Name"/>. The
/// content is shown where the tool is in the toolbar
/// </summary>
public class PixataEditorCustomToolTemplate : ComponentBase {
  [CascadingParameter]
  private PixataEditor? Editor { get; set; }

  // The name of the custom tool being rendered. The editor renders EditorCustomTools once for each custom tool, and each
  // template only shows its content in the place for its own tool
  [CascadingParameter(Name = PixataEditor.CustomToolNameKey)]
  private string? ToolName { get; set; }

  /// <summary>
  /// The name of the <see cref="PixataEditorCustomTool"/> this is the content for
  /// </summary>
  [Parameter, EditorRequired]
  public string Name { get; set; } = "";

  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  protected override void OnParametersSet() {
    if (Editor is null) {
      throw new InvalidOperationException($"{nameof(PixataEditorCustomToolTemplate)} must be inside the {nameof(PixataEditor.EditorCustomTools)} of a {nameof(PixataEditor)}");
    }
  }

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (ToolName is not null && ToolName == Name) {
      builder.AddContent(0, ChildContent);
    }
  }
}
