using Telerik.Blazor.Components;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public class DateGridColumn : GridColumn {
  public DateGridColumn() =>
    FilterOperators = GridFilterOperators.DateOperators;
}