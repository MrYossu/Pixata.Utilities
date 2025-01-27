using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Pixata.Blazor.Containers;

// Interface to access Switch functionality without knowing the type
public interface SwitchInterface {
  bool CanRender();
}

public class Switch<T> : ComponentBase, SwitchInterface {
  [Parameter]
  public T Variable { get; set; }

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  private bool _anyValueMet;

  internal void MarkValueMet() =>
    _anyValueMet = true;

  public bool CanRender() => !_anyValueMet;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    builder.OpenComponent<CascadingValue<Switch<T>>>(0);
    builder.AddAttribute(1, "Value", this);
    builder.AddAttribute(2, "IsFixed", false);
    builder.AddAttribute(3, "ChildContent", ChildContent);
    builder.CloseComponent();
  }

  protected override void OnParametersSet() =>
    _anyValueMet = false;
}

public class Case<T> : ComponentBase {
  [CascadingParameter]
  private Switch<T> Parent { get; set; } = null!;

  [Parameter]
  public T Equals { get; set; }

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Equals(Equals, Parent.Variable) && Parent.CanRender()) {
      Parent.MarkValueMet();
      builder.AddContent(0, ChildContent);
    }
  }
}

public class Default : ComponentBase {
  [CascadingParameter]
  private object Parent { get; set; } = null!;

  [Parameter]
  public RenderFragment ChildContent { get; set; } = null!;

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Parent is SwitchInterface switchParent && switchParent.CanRender()) {
      builder.AddContent(0, ChildContent);
    }
  }
}