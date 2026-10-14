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

/// <summary>
/// Whether the grid's popups adapt to small screens
/// </summary>
public enum PixataGridAdaptiveMode {
  /// <summary>
  /// The popups are the same on every screen
  /// </summary>
  None,

  /// <summary>
  /// On small screens (768px wide or less), the filter menu, column menu and popup editor fill the screen
  /// </summary>
  Auto
}
