using System.Collections.Generic;

namespace Pixata.Extensions.Grid;

/// <summary>
/// The comparisons a grid filter can make. The names match Telerik's <c>FilterOperator</c>
/// </summary>
public enum GridFilterOperator {
  IsEqualTo,
  IsNotEqualTo,
  IsLessThan,
  IsLessThanOrEqualTo,
  IsGreaterThan,
  IsGreaterThanOrEqualTo,
  StartsWith,
  EndsWith,
  Contains,
  DoesNotContain,
  /// <summary>
  /// The value is one of a list. The descriptor's value must be a collection
  /// </summary>
  IsContainedIn,
  IsNull,
  IsNotNull,
  IsEmpty,
  IsNotEmpty,
  IsNullOrEmpty,
  IsNotNullOrEmpty
}

/// <summary>
/// How the filters in a <see cref="GridCompositeFilterDescriptor"/> are combined
/// </summary>
public enum GridFilterLogic {
  And,
  Or
}

/// <summary>
/// Filters a grid's data on one field. It isn't called <c>FilterDescriptor</c>, so that it doesn't clash with Telerik's in
/// code that uses both
/// </summary>
/// <param name="Member">The field, such as "Name" or "Address.City"</param>
/// <param name="Operator">The comparison</param>
/// <param name="Value">What to compare with. Not needed for operators such as <see cref="GridFilterOperator.IsNull"/>.
/// After a trip through JSON this will be a <c>JsonElement</c>, which is converted to the field's type when the filter is
/// applied</param>
public record GridFilterDescriptor(string Member, GridFilterOperator Operator, object? Value = null) {
  /// <summary>
  /// For a <see cref="System.DateTime"/> field, compares the date only, ignoring the time on both the field and the value. So
  /// "is equal to 21/10/2026" matches any time on that day, and "is greater than 21/10/2026" starts on the next day. It is
  /// done with a range rather than by taking the date part of the field, so EF Core can still use an index. It has no effect
  /// on other types
  /// </summary>
  public bool IgnoreTime { get; init; }
}

/// <summary>
/// A group of filters combined with AND or OR, such as the two conditions in a column's filter menu
/// </summary>
public record GridCompositeFilterDescriptor(GridFilterLogic Logic, List<GridFilterDescriptor> Filters) {
  public GridCompositeFilterDescriptor() : this(GridFilterLogic.And, []) {
  }
}
