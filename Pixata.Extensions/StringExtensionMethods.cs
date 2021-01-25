using System.Globalization;
using System.Linq;
using System.Text;
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

    /// <summary>
    /// Removes diacritics (such as ð, â and ý) from letters, replacing them with their (hopefully) nearest Latin equivalents
    /// </summary>
    /// <param name="text">The string containing diacritics</param>
    /// <returns>The string with diacritics replaced</returns>
    public static string RemoveDiacritics(this string text) =>
      string.Concat(text.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark))
        .Normalize(NormalizationForm.FormC)
        .ToLower()
        // For some reason, the character ð isn't converted, so we have to do that one manually. I don't know how often they use it, but for the (really only a) few extra milliseconds it adds to the execution time, it will make the users happy :)
        // Actually, it won't make them sad, which it would if it didn't work, but it amounts to the same thing!
        .Replace("ð", "o");
  }
}