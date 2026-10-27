using System;
using Microsoft.AspNetCore.Components;
using Pixata.Extensions;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column for text, whose filter offers "contains", "starts with" and "ends with", starting with "contains"
/// </summary>
public class PixataTextGridColumn<TItem> : PixataGridColumn<TItem> {
  public PixataTextGridColumn() {
    FilterOperators = PixataGridFilterOperators.StringOperators;
    DefaultFilterOperator = GridFilterOperator.Contains;
    ShowFilterClearButton = false;
  }
}

/// <summary>
/// A column for numbers, whose filter offers "equals", "less than" and "more than"
/// </summary>
public class PixataNumericGridColumn<TItem> : PixataGridColumn<TItem> {
  public PixataNumericGridColumn() {
    FilterOperators = PixataGridFilterOperators.NumericOperators;
    ShowFilterClearButton = false;
  }
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
    ShowFilterClearButton = false;
  }

  /// <summary>
  /// Set to false to leave out the "(All)" entry at the top of the filter's drop-down, which clears the filter. Defaults to
  /// true. The entry's text comes from the grid's <see cref="PixataGridMessages.FilterAll"/>
  /// </summary>
  [Parameter]
  public bool AddAllEntry { get; set; } = true;

  /// <summary>
  /// How values whose names are several words, such as <c>AwaitingSignature</c>, are shown, in the cells and in the filter's
  /// drop-down. Defaults to <see cref="PixataEnumDisplayStyle.LowerCamelCase"/> ("Awaiting signature")
  /// </summary>
  [Parameter]
  public PixataEnumDisplayStyle DisplayStyle { get; set; } = PixataEnumDisplayStyle.LowerCamelCase;

  internal override string EnumText(Enum value) =>
    DisplayStyle switch {
      PixataEnumDisplayStyle.LowerCamelCase => value.ToString().SplitCamelCase(),
      PixataEnumDisplayStyle.UpperCamelCase => value.ToString().SplitCamelCase(false),
      _ => value.ToString()
    };
}

/// <summary>
/// How a <see cref="PixataEnumGridColumn{TItem}"/> shows values whose names are several words
/// </summary>
public enum PixataEnumDisplayStyle {
  /// <summary>
  /// The name as it is, such as "AwaitingSignature"
  /// </summary>
  NotSplit,

  /// <summary>
  /// Split into words, with only the first capitalised, such as "Awaiting signature"
  /// </summary>
  LowerCamelCase,

  /// <summary>
  /// Split into words, each capitalised, such as "Awaiting Signature"
  /// </summary>
  UpperCamelCase
}
