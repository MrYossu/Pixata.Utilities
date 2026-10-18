using System.Collections.Generic;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// An operator offered in a column's filter, with the text shown for it. See <see cref="PixataGridColumn{TItem}.FilterOperators"/>
/// </summary>
public class PixataGridFilterListOperator {
  public GridFilterOperator Operator { get; set; }
  public string Text { get; set; } = "";
}

/// <summary>
/// Short lists of filter operators, for a column's <see cref="PixataGridColumn{TItem}.FilterOperators"/>. These are used by
/// <see cref="PixataGridTextColumn{TItem}"/>, <see cref="PixataGridNumericColumn{TItem}"/> and
/// <see cref="PixataGridDateColumn{TItem}"/>, and can be replaced at startup to change the operators (or their text) everywhere
/// </summary>
public static class PixataGridFilterOperators {
  public static List<PixataGridFilterListOperator> StringOperators { get; set; } = [
    new() { Operator = GridFilterOperator.Contains, Text = "contains" },
    new() { Operator = GridFilterOperator.StartsWith, Text = "starts with" },
    new() { Operator = GridFilterOperator.EndsWith, Text = "ends with" }
  ];

  public static List<PixataGridFilterListOperator> NumericOperators { get; set; } = [
    new() { Operator = GridFilterOperator.IsEqualTo, Text = "equals" },
    new() { Operator = GridFilterOperator.IsLessThan, Text = "less than" },
    new() { Operator = GridFilterOperator.IsGreaterThan, Text = "more than" }
  ];

  public static List<PixataGridFilterListOperator> DateOperators { get; set; } = [
    new() { Operator = GridFilterOperator.IsLessThan, Text = "before" },
    new() { Operator = GridFilterOperator.IsEqualTo, Text = "on" },
    new() { Operator = GridFilterOperator.IsGreaterThanOrEqualTo, Text = "on or after" }
  ];
}
