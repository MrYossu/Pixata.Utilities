using System.Collections.Generic;
using System.ComponentModel;

namespace Pixata.Extensions.Grid;

/// <summary>
/// Groups a grid's data by one field. Several of these give nested groups, in order
/// </summary>
/// <param name="Member">The field, such as "Department"</param>
/// <param name="SortDirection">The order of the groups</param>
public record GridGroupDescriptor(string Member, ListSortDirection SortDirection = ListSortDirection.Ascending);

/// <summary>
/// The calculations a grid can show in its group and column footers. The names match Telerik's <c>GridAggregateType</c>
/// </summary>
public enum GridAggregateType {
  Count,
  Sum,
  Average,
  Min,
  Max
}

/// <summary>
/// Asks for an aggregate of one field
/// </summary>
public record GridAggregateDescriptor(string Member, GridAggregateType Aggregate);

/// <summary>
/// The aggregates calculated for one field. Only the ones that were asked for are set. After a trip through JSON, the
/// values will be <c>JsonElement</c>s
/// </summary>
public class GridFieldAggregates {
  public object? Count { get; set; }
  public object? Sum { get; set; }
  public object? Average { get; set; }
  public object? Min { get; set; }
  public object? Max { get; set; }
}

/// <summary>
/// One group of a grid's data. The top level groups are in <see cref="GridResult{T}.Groups"/>, and each one either has
/// <see cref="Subgroups"/> (if the data is grouped by more fields) or <see cref="Items"/>
/// </summary>
public class GridGroup<T> {
  /// <summary>
  /// The field the group is for
  /// </summary>
  public string Member { get; set; } = "";

  /// <summary>
  /// The value of the field that the items in the group share
  /// </summary>
  public object? Key { get; set; }

  /// <summary>
  /// The number of items in the whole group, which may be more than are on the current page
  /// </summary>
  public int Count { get; set; }

  /// <summary>
  /// The items in this group on the current page, if this is the innermost level of grouping
  /// </summary>
  public List<T> Items { get; set; } = [];

  public List<GridGroup<T>> Subgroups { get; set; } = [];

  /// <summary>
  /// The aggregates for the whole group, keyed by field
  /// </summary>
  public Dictionary<string, GridFieldAggregates> Aggregates { get; set; } = [];
}
