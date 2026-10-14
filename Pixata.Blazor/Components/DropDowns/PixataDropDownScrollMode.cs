namespace Pixata.Blazor.Components;

/// <summary>
/// How a drop-down renders its list
/// </summary>
public enum PixataDropDownScrollMode {
  /// <summary>
  /// Every item is rendered, and the list scrolls normally. Best for lists of up to a few hundred items. This is the default
  /// </summary>
  Scrollable,

  /// <summary>
  /// Only the items that can be seen (plus a few either side) are rendered, so very long lists stay fast. Set
  /// <c>ItemHeight</c> to the height of an item, and with <c>OnRead</c> the data is requested a page (<c>PageSize</c> items) at a
  /// time as the user scrolls
  /// </summary>
  Virtual
}
