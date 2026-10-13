namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// The size of a <see cref="PixataGrid{TItem}"/>, which sets the padding around the cells. Like Telerik's grid, there is no large size
/// </summary>
public enum PixataGridSize {
  Small,
  Medium
}

/// <summary>
/// Whether the user can sort by one column at a time, or several
/// </summary>
public enum PixataGridSortMode {
  Single,
  Multiple
}

/// <summary>
/// The horizontal alignment of a column's data cells
/// </summary>
public enum PixataGridTextAlign {
  Left,
  Center,
  Right
}
