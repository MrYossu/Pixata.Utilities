using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components.Grid;

internal static class Css {
  public static string Join(params string?[] classes) =>
    string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)));
}

/// <summary>
/// Renders its content after the components before it. The grid uses it so that the table is rendered after the columns
/// have registered themselves, which means the columns are there on the first render, including when prerendering. It's
/// only public because Razor can't use internal components
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class PixataGridDefer : ComponentBase {
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  protected override void BuildRenderTree(RenderTreeBuilder builder) =>
    builder.AddContent(0, ChildContent);
}
