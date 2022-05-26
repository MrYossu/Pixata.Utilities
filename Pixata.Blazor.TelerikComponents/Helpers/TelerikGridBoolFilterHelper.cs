using System.Threading.Tasks;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers {
  public class TelerikGridBoolFilterHelper {
    public FilterCellTemplateContext BoolFilterContext { get; set; } = null!;
    public bool? IsTrue { get; set; }

    public async Task ClearBoolFilter() {
      IsTrue = null;
      await BoolFilterContext.ClearFilterAsync();
    }

    public async Task SetupFilter() {
      if (!IsTrue.HasValue) {
        await BoolFilterContext.ClearFilterAsync();
      } else {
        FilterDescriptor filterRule = BoolFilterContext.FilterDescriptor.FilterDescriptors[0] as FilterDescriptor;
        filterRule.Value = IsTrue.Value;
        filterRule.Operator = FilterOperator.IsEqualTo;
        await BoolFilterContext.FilterAsync();
      }
    }
  }
}