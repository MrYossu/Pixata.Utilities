using System.Collections.Generic;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class GridFilterOperators {
  public static List<FilterListOperator> StringOperators { get; set; } = [
    new FilterListOperator { Operator = FilterOperator.Contains, Text = "contains" },
    new FilterListOperator { Operator = FilterOperator.StartsWith, Text = "starts with" },
    new FilterListOperator { Operator = FilterOperator.EndsWith, Text = "ends with" }
  ];

  public static List<FilterListOperator> NumericOperators { get; set; } = [
    new FilterListOperator { Operator = FilterOperator.IsEqualTo, Text = "equals" },
    new FilterListOperator { Operator = FilterOperator.IsLessThan, Text = "less than" },
    new FilterListOperator { Operator = FilterOperator.IsGreaterThan, Text = "more than" }
  ];

  public static List<FilterListOperator> DateOperators { get; set; } = [
    new FilterListOperator { Operator = FilterOperator.IsLessThan, Text = "before" },
    new FilterListOperator { Operator = FilterOperator.IsEqualTo, Text = "on" },
    new FilterListOperator { Operator = FilterOperator.IsGreaterThanOrEqualTo, Text = "on or after" }
  ];
}