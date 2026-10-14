using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// Renders the chart's settings components. It only renders again when the page gives the chart new content, not when the
/// chart redraws itself, as rendering the content creates new parameter values (an inline array, say) that would make the
/// settings ask the chart to redraw again, for ever
/// </summary>
internal sealed class PixataChartSettingsHost : ComponentBase {
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  private RenderFragment? _rendered;

  protected override bool ShouldRender() =>
    !ReferenceEquals(_rendered, ChildContent);

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    _rendered = ChildContent;
    builder.AddContent(0, ChildContent);
  }
}
