using System.Globalization;

namespace Pixata.Blazor.Components;

/// <summary>
/// How the text typed into a drop-down is matched against the text of each item when the list is filtered. Matching ignores
/// case
/// </summary>
public enum PixataStringFilterOperator {
  /// <summary>
  /// The item's text starts with the typed text. This is the default
  /// </summary>
  StartsWith,

  /// <summary>
  /// The item's text ends with the typed text
  /// </summary>
  EndsWith,

  /// <summary>
  /// The item's text contains the typed text anywhere
  /// </summary>
  Contains,

  /// <summary>
  /// The typed text contains the item's text
  /// </summary>
  IsContainedIn,

  /// <summary>
  /// The item's text doesn't contain the typed text
  /// </summary>
  DoesNotContain,

  /// <summary>
  /// The item's text is the typed text
  /// </summary>
  IsEqualTo
}

public static class PixataStringFilterOperatorExtensions {
  /// <summary>
  /// Whether <paramref name="text"/> (an item's text) matches <paramref name="filter"/> (what the user typed), ignoring case.
  /// Handy in an <c>OnRead</c> handler that filters a list in memory. An empty filter matches everything
  /// </summary>
  public static bool Matches(this PixataStringFilterOperator filterOperator, string? text, string? filter) {
    if (string.IsNullOrEmpty(filter)) {
      return true;
    }
    text ??= "";
    CompareInfo compare = CultureInfo.CurrentCulture.CompareInfo;
    const CompareOptions options = CompareOptions.IgnoreCase;
    return filterOperator switch {
      PixataStringFilterOperator.EndsWith => compare.IsSuffix(text, filter, options),
      PixataStringFilterOperator.Contains => compare.IndexOf(text, filter, options) >= 0,
      PixataStringFilterOperator.IsContainedIn => compare.IndexOf(filter, text, options) >= 0,
      PixataStringFilterOperator.DoesNotContain => compare.IndexOf(text, filter, options) < 0,
      PixataStringFilterOperator.IsEqualTo => compare.Compare(text, filter, options) == 0,
      _ => compare.IsPrefix(text, filter, options)
    };
  }
}
