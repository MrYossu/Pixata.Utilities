using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Containers;

public class If : ComponentBase {
  [Parameter]
  public bool Condition { get; set; } = true;

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  private bool _hasRendered;

  internal void MarkAsRendered() =>
    _hasRendered = true;

  internal bool CanRender() =>
    !_hasRendered;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    builder.OpenComponent<CascadingValue<If>>(0);
    builder.AddAttribute(1, "Value", this);
    builder.AddAttribute(2, "IsFixed", false);
    builder.AddAttribute(3, "ChildContent", ChildContent);
    builder.CloseComponent();
  }

  protected override void OnParametersSet() =>
    _hasRendered = false;
}

public class Then : ComponentBase {
  [CascadingParameter]
  protected If Parent { get; set; } = null!;

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Parent.Condition && Parent.CanRender()) {
      Parent.MarkAsRendered();
      builder.AddContent(0, ChildContent);
    }
  }
}

public class ElseIf : Then {
  [Parameter]
  public bool Condition { get; set; } = true;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Condition && !Parent.Condition && Parent.CanRender()) {
      Parent.MarkAsRendered();
      builder.AddContent(0, ChildContent);
    }
  }
}

public class Else : ComponentBase {
  [CascadingParameter]
  private If Parent { get; set; } = null!;

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Parent.CanRender()) {
      builder.AddContent(0, ChildContent);
    }
  }
}