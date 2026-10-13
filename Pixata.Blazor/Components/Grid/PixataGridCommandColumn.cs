using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column of buttons, such as Edit and Delete, for each row. Put <see cref="PixataGridCommandButton{TItem}"/>s inside it
/// </summary>
public class PixataGridCommandColumn<TItem> : PixataGridColumn<TItem> {
  /// <summary>
  /// The buttons
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  protected override void OnParametersSet() {
#pragma warning disable BL0005 // These are the column's own parameters
    Sortable = false;
    Filterable = false;
    Editable = false;
#pragma warning restore BL0005
    base.OnParametersSet();
  }
}
