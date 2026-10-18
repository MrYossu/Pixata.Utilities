using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column for text, whose filter offers "contains", "starts with" and "ends with", starting with "contains"
/// </summary>
public class PixataGridTextColumn<TItem> : PixataGridColumn<TItem> {
  public PixataGridTextColumn() {
    FilterOperators = PixataGridFilterOperators.StringOperators;
    DefaultFilterOperator = GridFilterOperator.Contains;
  }
}

/// <summary>
/// A column for numbers, whose filter offers "equals", "less than" and "more than"
/// </summary>
public class PixataGridNumericColumn<TItem> : PixataGridColumn<TItem> {
  public PixataGridNumericColumn() =>
    FilterOperators = PixataGridFilterOperators.NumericOperators;
}

/// <summary>
/// A column for dates, whose filter offers "before", "on" and "on or after"
/// </summary>
public class PixataGridDateColumn<TItem> : PixataGridColumn<TItem> {
  public PixataGridDateColumn() =>
    FilterOperators = PixataGridFilterOperators.DateOperators;
}

/// <summary>
/// A column for an enum, whose filter row cell is just a drop-down of the values, filtering on equality, with no operator or
/// clear button
/// </summary>
public class PixataGridEnumColumn<TItem> : PixataGridColumn<TItem> {
  public PixataGridEnumColumn() {
    DefaultFilterOperator = GridFilterOperator.IsEqualTo;
    ShowFilterCellButtons = false;
  }
}
