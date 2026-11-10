namespace Pixata.Pdf;

/// <summary>
/// A simple form of the Unicode bidirectional algorithm, enough for a line of Hebrew or Arabic text with numbers or English
/// words in it. A PDF's text is drawn left to right, so text that's typed right to left has to be put into the order it's seen
/// in first
/// </summary>
internal static class Bidi {
  private static bool IsRightToLeft(char c) =>
    c is >= '\u0590' and <= '\u08ff' or >= '\ufb1d' and <= '\ufdff' or >= '\ufe70' and <= '\ufeff';

  private static bool IsLeftToRight(char c) =>
    char.IsLetterOrDigit(c) && !IsRightToLeft(c);

  // A bracket in right-to-left text faces the other way
  private static char Mirror(char c) =>
    c switch {
      '(' => ')',
      ')' => '(',
      '[' => ']',
      ']' => '[',
      '{' => '}',
      '}' => '{',
      '<' => '>',
      '>' => '<',
      _ => c
    };

  /// <summary>
  /// The line in the order its characters are drawn, from left to right. Its direction is that of its first letter, so a line
  /// that starts with a Hebrew word reads from the right
  /// </summary>
  public static string Visual(string line) {
    if (!line.Any(IsRightToLeft)) {
      return line;
    }
    bool rightToLeft = line.FirstOrDefault(c => IsRightToLeft(c) || IsLeftToRight(c)) is char first && IsRightToLeft(first);
    // Each character's direction. Spaces and punctuation take the direction of the letters either side of them if those agree,
    // and the line's direction if they don't
    bool?[] strong = [.. line.Select(c => IsRightToLeft(c) ? true : IsLeftToRight(c) ? false : (bool?)null)];
    bool[] direction = new bool[line.Length];
    for (int index = 0; index < line.Length; index++) {
      if (strong[index] is bool known) {
        direction[index] = known;
        continue;
      }
      bool? before = strong[..index].LastOrDefault(d => d is not null);
      bool? after = strong[(index + 1)..].FirstOrDefault(d => d is not null);
      direction[index] = before is not null && before == after ? before.Value : rightToLeft;
    }
    // Runs of characters going the same way. Right-to-left runs are reversed, and so is the order of the runs if the line
    // reads from the right
    List<string> runs = [];
    int start = 0;
    for (int index = 1; index <= line.Length; index++) {
      if (index == line.Length || direction[index] != direction[start]) {
        string run = line[start..index];
        runs.Add(direction[start] ? new([.. run.Reverse().Select(Mirror)]) : run);
        start = index;
      }
    }
    if (rightToLeft) {
      runs.Reverse();
    }
    return string.Concat(runs);
  }
}
