using System.ComponentModel;

namespace Pixata.Extensions.Grid;

/// <summary>
/// Sorts a grid's data by one field. It isn't called <c>SortDescriptor</c>, so that it doesn't clash with Telerik's in code
/// that uses both
/// </summary>
/// <param name="Member">The field to sort by, such as "Name" or "Address.City"</param>
/// <param name="SortDirection">The direction</param>
public record GridSortDescriptor(string Member, ListSortDirection SortDirection = ListSortDirection.Ascending);
