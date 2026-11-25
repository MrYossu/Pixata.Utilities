namespace Pixata.Pdf.Documents;

/// <summary>
/// Turns characters in the Symbol and Wingdings fonts into the Unicode characters they look like. Word writes bullets and Greek
/// letters in those fonts as ordinary letters or private-use characters (U+F000 to U+F0FF), which other fonts don't draw as they
/// should, so "·" in Symbol is a bullet, and "§" in Wingdings is a square bullet
/// </summary>
internal static class Symbols {
  private const string GreekLower = "αβχδεφγηιϕκλμνοπθρστυϖωξψζ";
  private const string GreekUpper = "ΑΒΧΔΕΦΓΗΙϑΚΛΜΝΟΠΘΡΣΤΥςΩΞΨΖ";

  public static bool IsSymbolFont(string? font) =>
    font is not null && (font.StartsWith("Symbol", StringComparison.OrdinalIgnoreCase) || font.StartsWith("Wingdings", StringComparison.OrdinalIgnoreCase)
                         || font.StartsWith("Webdings", StringComparison.OrdinalIgnoreCase));

  /// <summary>
  /// The Unicode text for a character in a symbol font
  /// </summary>
  /// <param name="code">The character, either as the font has it (0 to 255) or in the private-use area</param>
  /// <param name="font">The font's name, if it's known</param>
  public static string Map(int code, string? font) {
    if (code is >= 0xf000 and <= 0xf0ff) {
      code -= 0xf000;
    }
    bool wingdings = font is not null && font.StartsWith("Wingdings", StringComparison.OrdinalIgnoreCase);
    if (wingdings) {
      return code switch {
        0x9f or 0x6c or 0x9e => "•",
        0xa7 or 0x6e => "▪",
        0xa8 or 0x6f or 0x71 => "□",
        0xfc => "✓",
        0xfb or 0xfd => "✗",
        0xd8 or 0xe0 or 0xe8 => "➢",
        0x76 => "❖",
        _ => "•"
      };
    }
    return code switch {
      >= 'a' and <= 'z' => GreekLower[code - 'a'].ToString(),
      >= 'A' and <= 'Z' => GreekUpper[code - 'A'].ToString(),
      0xb7 => "•",
      0xb0 => "°",
      0xb1 => "±",
      0xa3 => "≤",
      0xb3 => "≥",
      0xb4 => "×",
      0xb8 => "÷",
      0xb9 => "≠",
      0xbb => "≈",
      0xa5 => "∞",
      0xae => "→",
      0xac => "←",
      0xad => "↑",
      0xaf => "↓",
      0xd6 => "√",
      0xd2 or 0xe2 => "®",
      0xd3 or 0xe3 => "©",
      0xd4 or 0xe4 => "™",
      0xa8 => "♣",
      0xa9 => "♦",
      0xaa => "♥",
      0xab => "♠",
      >= 0x20 and < 0x7f => ((char)code).ToString(),
      _ => "•"
    };
  }
}
