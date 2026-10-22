namespace Pixata.Blazor.Components;

/// <summary>
/// Whether the user can select the items of a <see cref="PixataTreeView"/> by clicking them
/// </summary>
public enum PixataTreeViewSelectionMode {
  /// <summary>
  /// Items can't be selected (the default)
  /// </summary>
  None,

  /// <summary>
  /// One item at a time can be selected
  /// </summary>
  Single,

  /// <summary>
  /// Any number of items can be selected, with Ctrl+click (or Cmd+click) and Shift+click
  /// </summary>
  Multiple
}

/// <summary>
/// Whether the items of a <see cref="PixataTreeView"/> have checkboxes
/// </summary>
public enum PixataTreeViewCheckBoxMode {
  /// <summary>
  /// No checkboxes (the default)
  /// </summary>
  None,

  /// <summary>
  /// Each item has a checkbox, and checking one unchecks the others
  /// </summary>
  Single,

  /// <summary>
  /// Each item has a checkbox, and any number can be checked
  /// </summary>
  Multiple
}

/// <summary>
/// Where a dragged item was dropped, relative to the item it was dropped on
/// </summary>
public enum PixataTreeViewDropPosition {
  /// <summary>
  /// On the item, so the dragged item should become one of its children
  /// </summary>
  Over,

  /// <summary>
  /// Just before the item, as its previous sibling
  /// </summary>
  Before,

  /// <summary>
  /// Just after the item, as its next sibling
  /// </summary>
  After
}
