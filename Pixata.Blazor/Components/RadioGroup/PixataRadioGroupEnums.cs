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

/// <summary>
/// How the picked radio button in a <see cref="PixataRadioGroup{TItem, TValue}"/> is drawn
/// </summary>
public enum PixataRadioGroupCheckedStyle {
  /// <summary>
  /// A solid dot in the middle of the radio button
  /// </summary>
  Filled,

  /// <summary>
  /// The radio button filled with a thick ring, around a hollow (white) middle
  /// </summary>
  Ring
}
