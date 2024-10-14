using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public class EnumGridColumn : GridColumn {
  public EnumGridColumn() {
    DefaultFilterOperator = FilterOperator.IsEqualTo;
    ShowFilterCellButtons = false;
  }
}