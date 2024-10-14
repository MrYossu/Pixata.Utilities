using Telerik.Blazor.Components;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public class NumericGridColumn : GridColumn {
  public NumericGridColumn() =>
    FilterOperators = GridFilterOperators.NumericOperators;
}