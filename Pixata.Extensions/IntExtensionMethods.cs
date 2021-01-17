namespace Pixata.Extensions {
  public static class IntExtensionMethods {
    /// <summary>
    /// Returns the ordinal suffix for a number, eg "st" for 1, "nd" for 2, "rd" for 3 and so on. Useful for formatting dates
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static string OrdinalSuffix(this int n) =>
      n switch {
        11 or 12 or 13 => "th",
        _ when n.ToString().EndsWith("1") => "st",
        _ when n.ToString().EndsWith("2") => "nd",
        _ when n.ToString().EndsWith("3") => "rd",
        _ => "th"
      };
  }
}