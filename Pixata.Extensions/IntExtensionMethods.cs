namespace Pixata.Extensions {
  public static class IntExtensionMethods {
    /// <summary>
    /// Returns the ordinal suffix for a number, eg "st" for 1, "nd" for 2, "rd" for 3 and so on. Useful for formatting dates
    /// </summary>
    /// <param name="n">An integer</param>
    /// <param name="includeNumber">If true (9)default) then includes the number in the return value, eg "9th". If false, returns just the ordinal suffix, eg "th"</param>
    /// <returns>The ordinal suffix for the integer, optionally with the integer itself</returns>
    public static string OrdinalSuffix(this int n, bool includeNumber = true) =>
      (includeNumber ? n : "") + n switch {
        11 or 12 or 13 => "th",
        _ when n.ToString().EndsWith("1") => "st",
        _ when n.ToString().EndsWith("2") => "nd",
        _ when n.ToString().EndsWith("3") => "rd",
        _ => "th"
      };
  }
}