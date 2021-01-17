using System.Text.RegularExpressions;

namespace Pixata.Extensions {
  public static class StringExtensionMethods {
    /// <summary>
    /// Splits a CamelCase string into "Camel Case"
    /// </summary>
    /// <param name="str">The input string</param>
    /// <returns>The input string split into space-separated words</returns>
    public static string SplitCamelCase(this string str) =>
      Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
  }
}