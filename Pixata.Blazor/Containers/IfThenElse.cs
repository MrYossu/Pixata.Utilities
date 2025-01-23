using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Containers;

public class If : ComponentBase {
  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  private bool _anyConditionMet;

  internal void MarkConditionMet() =>
    _anyConditionMet = true;

  internal bool CanRender() => !_anyConditionMet;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    builder.OpenComponent<CascadingValue<If>>(0);
    builder.AddAttribute(1, "Value", this);
    builder.AddAttribute(2, "IsFixed", false);
    builder.AddAttribute(3, "ChildContent", ChildContent);
    builder.CloseComponent();
  }

  protected override void OnParametersSet() =>
    _anyConditionMet = false;
}

public class Then : ComponentBase {
  [CascadingParameter]
  private If Parent { get; set; } = null!;

  [Parameter]
  public bool Condition { get; set; } = true;

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Condition && Parent.CanRender()) {
      Parent.MarkConditionMet();
      builder.AddContent(0, ChildContent);
    }
  }
}

public class ElseIf : Then {
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