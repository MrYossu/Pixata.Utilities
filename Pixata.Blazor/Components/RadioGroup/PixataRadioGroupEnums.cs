namespace Pixata.Blazor.Components;

/// <summary>
/// How the radio buttons in a <see cref="PixataRadioGroup{TItem, TValue}"/> are laid out
/// </summary>
public enum PixataRadioGroupLayout {
  /// <summary>
  /// In a row, which wraps onto more lines when it runs out of room
  /// </summary>
  Horizontal,

  /// <summary>
  /// One above the other
  /// </summary>
  Vertical
}

/// <summary>
/// Where each label goes in a <see cref="PixataRadioGroup{TItem, TValue}"/>
/// </summary>
public enum PixataRadioGroupLabelPosition {
  /// <summary>
  /// Before the radio button
  /// </summary>
  Before,

  /// <summary>
  /// After the radio button
  /// </summary>
  After
}
