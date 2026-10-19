using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column of buttons, such as Edit and Delete, for each row. Put <see cref="PixataGridCommandButton{TItem}"/>s inside it,
/// along with anything else that needs the row
/// </summary>
public class PixataGridCommandColumn<TItem> : PixataGridColumn<TItem> {
  /// <summary>
  /// The buttons. This is given the row's item, as an <see cref="object"/> (as in Telerik's grid, and as
  /// <see cref="PixataGridColumn{TItem}.Template"/> is), so the buttons can depend on the row, and buttons of your own can act
  /// on it. While the row is being edited, it is given the row as it was before editing started (or the new item, when
  /// adding). Use <c>Context="row"</c> to rename it if a template inside also uses <c>context</c>. To tell whether the row is
  /// being edited, pick up the cascading <see cref="PixataGridRowContext{TItem}"/>
  /// </summary>
  [Parameter]
  public RenderFragment<object>? ChildContent { get; set; }

  protected override void OnParametersSet() {
#pragma warning disable BL0005 // These are the column's own parameters
    Sortable = false;
    Filterable = false;
    Editable = false;
#pragma warning restore BL0005
    base.OnParametersSet();
  }
}
