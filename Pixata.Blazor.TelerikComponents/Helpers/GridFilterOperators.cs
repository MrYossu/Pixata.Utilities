using System.Collections.Generic;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class GridFilterOperators {
  public static List<FilterListOperator> StringOperators { get; set; } = [
    new() { Operator = FilterOperator.Contains, Text = "contains" },
    new() { Operator = FilterOperator.StartsWith, Text = "starts with" },
    new() { Operator = FilterOperator.EndsWith, Text = "ends with" }
  ];

  public static List<FilterListOperator> NumericOperators { get; set; } = [
    new() { Operator = FilterOperator.IsEqualTo, Text = "equals" },
    new() { Operator = FilterOperator.IsLessThan, Text = "less than" },
    new() { Operator = FilterOperator.IsGreaterThan, Text = "more than" }
  ];

  public static List<FilterListOperator> DateOperators { get; set; } = [
    new() { Operator = FilterOperator.IsLessThan, Text = "before" },
    new() { Operator = FilterOperator.IsEqualTo, Text = "on" },
    new() { Operator = FilterOperator.IsGreaterThanOrEqualTo, Text = "on or after" }
  ];
}