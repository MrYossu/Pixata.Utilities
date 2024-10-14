using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public class TextGridColumn : GridColumn {
  public TextGridColumn() {
    FilterOperators = GridFilterOperators.StringOperators;
    DefaultFilterOperator = FilterOperator.Contains;
  }
}