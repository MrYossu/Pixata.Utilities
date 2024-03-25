using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pixata.Extensions {
  public static class StringExtensionMethods {
    /// <summary>
    /// Does the same as string.Join, but as an extension method, so it can be chained
    /// </summary>
    /// <param name="strs">An IEnumerable of strings to be joined</param>
    /// <param name="separator">An optional parameter that specifies the separator to use. Defaults to ", "</param>
    /// <returns>A string containing the joined input strings</returns>
    public static string JoinStr(this IEnumerable<string> strs, string separator = ", ") =>
      string.Join(separator, strs);

    /// <summary>
    /// Splits a CamelCase string into "Camel Case"
    /// </summary>
    /// <param name="str">The input string</param>
    /// <param name="toLower">An optional bool that specifies whether the second and subsequent words in the returned string should be lower case. Default is true</param>
    /// <returns>The input string split into space-separated words</returns>
    public static string SplitCamelCase(this string str, bool toLower = true) {
      if (string.IsNullOrWhiteSpace(str)) {
        return "";
      }
      string s = Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
      return toLower ? s[0].ToString().ToUpper() + s.Substring(1).ToLower() : s;
    }

    /// <summary>
    /// Splits an enum member name using camel case as the rule. Splits CamelCase into "Camel Case"
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    /// <param name="e">The enum value</param>
    /// <param name="toLower">An optional bool that specifies whether the second and subsequent words in the returned string should be lower case. Default is true</param>
    /// <returns>A space-separated string containing the enum member name split by camel case</returns>
    /// <exception cref="ArgumentException">Thrown if the input is not an enum</exception>
    public static string SplitEnumCamelCase<T>(this T e, bool toLower = true) where T : struct, IConvertible =>
      !typeof(T).IsEnum
        ? throw new ArgumentException("T must be an enumerated type")
        : SplitCamelCase(e.ToString(), toLower);

    /// <summary>
    /// Splits an enum member value (assumed to be an int) using camel case as the rule. Splits CamelCase into "Camel Case"
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    /// <param name="n">The int value of the enum value</param>
    /// <param name="toLower">An optional bool that specifies whether the second and subsequent words in the returned string should be lower case. Default is true</param>
    /// <returns>A space-separated string containing the enum member value split by camel case</returns>
    /// <exception cref="ArgumentException">ArgumentException thrown if the generic type is not an enum, or ArgumentOutOfRangeException if the int value does not correspond to an enum member value</exception>
    public static string SplitEnumValueCamelCase<T>(this int n, bool toLower = true) where T : struct, IConvertible =>
      !typeof(T).IsEnum
        ? throw new ArgumentException("T must be an enumerated type")
        : !Enum.IsDefined(typeof(T), n)
          ? throw new ArgumentOutOfRangeException($"{n} is not a valid value for the {typeof(T).Name} enum")
          : SplitCamelCase(((T)Enum.Parse(typeof(T), n.ToString())).ToString(), toLower);

    /// <summary>
    /// Returns the first line of a multi-line string. Useful for getting the first line of someone's address
    /// </summary>
    /// <param name="str">The multi-line string</param>
    /// <returns>The first line of the input, assuming Environment.NewLine or \n is the line delimiter</returns>
    public static string FirstLine(this string str) =>
      str.IndexOf(Environment.NewLine) > 0
        ? str.Substring(0, str.IndexOf(Environment.NewLine))
        : str.IndexOf("\n") > 0
          ? str.Substring(0, str.IndexOf("\n"))
          : str;

    /// <summary>
    /// Returns all but the first line of a multi-line string. Useful for getting the second and subsequent line(s) of someone's address
    /// </summary>
    /// <param name="str">The multi-line string</param>
    /// <returns>The second and subsequent line(s) of the input, assuming Environment.NewLine or \n is the line delimiter</returns>
    public static IEnumerable<string> OtherLines(this string str) =>
      str.IndexOf(Environment.NewLine) > 0
        ? str.Split(Environment.NewLine).Skip(1)
        : str.IndexOf("\n") > 0
          ? str.Split("\n").Skip(1)
          : new List<string>();

    /// <summary>
    /// Removes diacritics (such as ð, â and ý) from letters, replacing them with their (hopefully) nearest Latin equivalents
    /// </summary>
    /// <param name="text">The string containing diacritics</param>
    /// <returns>The string with diacritics replaced</returns>
    public static string RemoveDiacritics(this string text) =>
      string.Concat(text.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark))
        .Normalize(NormalizationForm.FormC)
        // For some reason, the character ð isn't converted, so we have to do that one manually. I don't know how often these are used, but for the (really only a) few extra milliseconds it adds to the execution time, it will make the users happy :)
        // Actually, it won't make them sad, which it would if it didn't work, but it amounts to the same thing!
        .Replace("ð", "o");

    /// <summary>
    /// Replaces the first character of each word with upper case e.g. This is an example - This Is An Example
    /// </summary>
    /// <param name="text">The string on which to capitalise</param>
    /// <returns>The string capitalised</returns>
    public static string ToTitleCase(this string text) =>
      CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
  }
}