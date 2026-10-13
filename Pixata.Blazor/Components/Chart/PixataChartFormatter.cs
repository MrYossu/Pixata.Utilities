using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pixata.Blazor.Components;

/// <summary>
/// Formats values and fills in the label templates (<c>#= value #</c> and friends)
/// </summary>
internal static partial class PixataChartFormatter {
  /// <summary>
  /// Formats a value with either a composite format (<c>{0:C}</c>) or a plain one (<c>C</c>), using the current culture
  /// </summary>
  public static string Format(object? value, string? format) {
    if (value is null) {
      return "";
    }
    if (string.IsNullOrEmpty(format)) {
      return value is IFormattable plain ? plain.ToString(null, CultureInfo.CurrentCulture) : value.ToString() ?? "";
    }
    if (format.Contains("{0")) {
      return string.Format(CultureInfo.CurrentCulture, format, value);
    }
    return value is IFormattable formattable ? formattable.ToString(format, CultureInfo.CurrentCulture) : value.ToString() ?? "";
  }

  /// <summary>
  /// Fills in a template such as <c>#= category #: #= format('{0:P0}', percentage) #</c>, using <paramref name="resolve"/> to
  /// look up the names (<c>value</c>, <c>category</c>, <c>dataItem.Name</c> and so on)
  /// </summary>
  public static string ApplyTemplate(string template, Func<string, object?> resolve) {
    StringBuilder result = new();
    int position = 0;
    while (position < template.Length) {
      int start = template.IndexOf("#=", position, StringComparison.Ordinal);
      if (start < 0) {
        result.Append(template, position, template.Length - position);
        break;
      }
      int end = template.IndexOf('#', start + 2);
      if (end < 0) {
        result.Append(template, position, template.Length - position);
        break;
      }
      result.Append(template, position, start - position);
      result.Append(Evaluate(template.Substring(start + 2, end - start - 2).Trim(), resolve));
      position = end + 1;
    }
    return result.ToString();
  }

  private static string Evaluate(string expression, Func<string, object?> resolve) {
    Match call = FormatCall().Match(expression);
    if (call.Success) {
      return Format(resolve(call.Groups["path"].Value.Trim()), call.Groups["format"].Value);
    }
    return Format(resolve(expression), null);
  }

  /// <summary>
  /// Splits label text into lines, at real line breaks and at the two characters <c>\n</c> (which is what a template typed
  /// into a Razor attribute contains)
  /// </summary>
  public static string[] Lines(string text) =>
    text.Replace("\\n", "\n").Replace("\r", "").Split('\n');

  /// <summary>
  /// Converts a data value to a number, or null if it is missing or not a number
  /// </summary>
  public static double? ToDouble(object? value) {
    try {
      double? result = value switch {
        null => null,
        double d => d,
        float f => f,
        decimal m => (double)m,
        int i => i,
        long l => l,
        short s => s,
        byte b => b,
        string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
        string => null,
        IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
        _ => null
      };
      return result is null || double.IsNaN(result.Value) || double.IsInfinity(result.Value) ? null : result;
    }
    catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) {
      return null;
    }
  }

  /// <summary>
  /// Converts a data value to a boolean, for the <c>ExplodeField</c> and <c>VisibleInLegendField</c>
  /// </summary>
  public static bool? ToBool(object? value) =>
    value switch {
      null => null,
      bool b => b,
      string text when bool.TryParse(text, out bool parsed) => parsed,
      _ => ToDouble(value) is double d ? d != 0 : null
    };

  /// <summary>
  /// A number as SVG wants it: invariant culture, no more than two decimal places
  /// </summary>
  public static string Svg(double value) =>
    Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

  // Matches format('{0:P0}', percentage), with or without a prefix such as "something." before format
  [GeneratedRegex(@"^(?:[\w$]+\.)*format\(\s*(?<quote>['""])(?<format>.*?)\k<quote>\s*,\s*(?<path>[\w.$]+)\s*\)$", RegexOptions.IgnoreCase)]
  private static partial Regex FormatCall();
}
