using Microsoft.AspNetCore.Components;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column for text, whose filter offers "contains", "starts with" and "ends with", starting with "contains"
/// </summary>
public class PixataTextGridColumn<TItem> : PixataGridColumn<TItem> {
  public PixataTextGridColumn() {
    FilterOperators = PixataGridFilterOperators.StringOperators;
    DefaultFilterOperator = GridFilterOperator.Contains;
  }
}

/// <summary>
/// A column for numbers, whose filter offers "equals", "less than" and "more than"
/// </summary>
public class PixataNumericGridColumn<TItem> : PixataGridColumn<TItem> {
  public PixataNumericGridColumn() =>
    FilterOperators = PixataGridFilterOperators.NumericOperators;
}

/// <summary>
/// A column for dates, whose filter offers "before", "on" and "on or after". The filter ignores the time, unless
/// <see cref="PixataGridColumn{TItem}.IgnoreTime"/> is set to false
/// </summary>
public class PixataDateGridColumn<TItem> : PixataGridColumn<TItem> {
  public PixataDateGridColumn() {
    FilterOperators = PixataGridFilterOperators.DateOperators;
    IgnoreTime = true;
  }
}

/// <summary>
/// A column for an enum, whose filter row cell is just a drop-down of the values, filtering on equality, with no operator or
/// clear button. The drop-down starts with an "(All)" entry that clears the filter, unless <see cref="AddAllEntry"/> is false
/// </summary>
public class PixataEnumGridColumn<TItem> : PixataGridColumn<TItem> {
  public PixataEnumGridColumn() {
    DefaultFilterOperator = GridFilterOperator.IsEqualTo;
    ShowFilterCellButtons = false;
  }

  /// <summary>
  /// Set to false to leave out the "(All)" entry at the top of the filter's drop-down, which clears the filter. Defaults to
  /// true. The entry's text comes from the grid's <see cref="PixataGridMessages.FilterAll"/>
  /// </summary>
  [Parameter]
  public bool AddAllEntry { get; set; } = true;
}
