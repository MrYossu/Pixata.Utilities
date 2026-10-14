namespace Pixata.Blazor.Components;

public static partial class PixataSvgIcons {
  /// <summary>
  /// A circular arrow, shown on the occurrences of a recurring appointment
  /// </summary>
  public static readonly PixataSvgIcon Repeat = new("repeat", "<path d=\"M19 12a7 7 0 1 1-2.05-4.95\"/><path d=\"M18 3.5v4h-4\"/>");

  /// <summary>
  /// A crossed-out circular arrow, shown on an occurrence of a recurring appointment that has been changed
  /// </summary>
  public static readonly PixataSvgIcon RepeatException = new("repeat-exception", "<path d=\"M19 12a7 7 0 1 1-2.05-4.95\"/><path d=\"M18 3.5v4h-4\"/><path d=\"M4 4l16 16\"/>");
}
