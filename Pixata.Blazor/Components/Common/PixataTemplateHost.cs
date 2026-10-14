using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Components;

/// <summary>
/// Renders a template that a child component holds (eg a tab's header template) where its parent wants it. A parent renders
/// before its children are given their new parameters, so a template it rendered itself would be the one the child had before.
/// This reads the template when it renders instead, and the child calls <see cref="Refresh"/> when it is given a new one, so the
/// template is redrawn without the whole parent having to render again
/// </summary>
internal sealed class PixataTemplateHost : ComponentBase, IDisposable {
  private RenderFragment? _rendered;
  private bool _disposed;

  /// <summary>
  /// Gets the template to render
  /// </summary>
  [Parameter]
  public Func<RenderFragment?> Template { get; set; } = () => null;

  /// <summary>
  /// A fragment that renders a host for <paramref name="template"/>, and passes the host to <paramref name="capture"/> so the
  /// child can refresh it
  /// </summary>
  internal static RenderFragment For(Func<RenderFragment?> template, Action<PixataTemplateHost> capture) =>
    builder => {
      builder.OpenComponent<PixataTemplateHost>(0);
      builder.AddAttribute(1, nameof(Template), template);
      builder.AddComponentReferenceCapture(2, host => capture((PixataTemplateHost)host));
      builder.CloseComponent();
    };

  /// <summary>
  /// Redraws the template, if it has changed since it was last drawn
  /// </summary>
  internal void Refresh() {
    if (!_disposed && !ReferenceEquals(Template(), _rendered)) {
      StateHasChanged();
    }
  }

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    _rendered = Template();
    builder.AddContent(0, _rendered);
  }

  public void Dispose() =>
    _disposed = true;
}
