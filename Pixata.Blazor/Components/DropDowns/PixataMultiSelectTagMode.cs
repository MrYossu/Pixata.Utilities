namespace Pixata.Blazor.Components;

/// <summary>
/// How <see cref="PixataMultiSelect{TItem, TValue}"/> shows the selected items
/// </summary>
public enum PixataMultiSelectTagMode {
  /// <summary>
  /// One removable tag per selected item (see <c>MaxAllowedTags</c> to limit how many are shown). This is the default
  /// </summary>
  Multiple,

  /// <summary>
  /// A single tag summarising the selection, eg "3 items selected"
  /// </summary>
  Single
}
