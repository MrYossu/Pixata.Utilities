namespace Pixata.Extensions {
  public static class IntExtensionMethods {
    public static string OrdinalSuffix(this int n) =>
      n switch {
        1 or 21 or 31 => "st",
        2 or 22 => "nd",
        3 or 23 => "rd",
        _ => "th"
      };
  }
}