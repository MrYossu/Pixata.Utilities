namespace Pixata.Blazor.Components;

/// <summary>
/// Where a <see cref="PixataTabStrip"/> puts its tabs, relative to the content
/// </summary>
public enum PixataTabPosition {
  /// <summary>
  /// The tabs are above the content (the default)
  /// </summary>
  Top,

  /// <summary>
  /// The tabs are below the content
  /// </summary>
  Bottom,

  /// <summary>
  /// The tabs are in a column to the left of the content
  /// </summary>
  Left,

  /// <summary>
  /// The tabs are in a column to the right of the content
  /// </summary>
  Right
}

/// <summary>
/// How a <see cref="PixataTabStrip"/> lines its tabs up along the tab list
/// </summary>
public enum PixataTabStripTabAlignment {
  /// <summary>
  /// The tabs are packed at the start of the list (the default)
  /// </summary>
  Start,

  /// <summary>
  /// The tabs are packed at the end of the list
  /// </summary>
  End,

  /// <summary>
  /// The tabs are packed in the middle of the list
  /// </summary>
  Center,

  /// <summary>
  /// The tabs are spread along the list, with equal gaps between them
  /// </summary>
  Justify,

  /// <summary>
  /// The tabs are stretched to share the whole length of the list equally
  /// </summary>
  Stretched
}

/// <summary>
/// Where a scrollable <see cref="PixataTabStrip"/> puts its scroll buttons
/// </summary>
public enum PixataTabStripScrollButtonsPosition {
  /// <summary>
  /// One button at each end of the tab list (the default)
  /// </summary>
  Split,

  /// <summary>
  /// Both buttons before the tab list
  /// </summary>
  Start,

  /// <summary>
  /// Both buttons after the tab list
  /// </summary>
  End
}

/// <summary>
/// When a scrollable <see cref="PixataTabStrip"/> shows its scroll buttons
/// </summary>
public enum PixataTabStripScrollButtonsVisibility {
  /// <summary>
  /// Only when the tabs don't fit (the default)
  /// </summary>
  Auto,

  /// <summary>
  /// Always. A button is disabled when there is nothing more to scroll in its direction
  /// </summary>
  Visible,

  /// <summary>
  /// Never. The tabs can still be scrolled by swiping, or with the keyboard
  /// </summary>
  Hidden
}

/// <summary>
/// What a <see cref="PixataTabStrip"/> does when its tabs don't fit
/// </summary>
public enum PixataTabStripOverflowMode {
  /// <summary>
  /// The tabs wrap on to another line (the default)
  /// </summary>
  None,

  /// <summary>
  /// The tabs stay on one line and scroll, the same as setting <see cref="PixataTabStrip.Scrollable"/>
  /// </summary>
  Scroll
}
