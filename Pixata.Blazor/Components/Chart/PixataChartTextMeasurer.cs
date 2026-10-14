using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pixata.Blazor.Components;

/// <summary>
/// Knows how wide the chart's text is, so the layout can leave room for it. Until the browser has measured a piece of text
/// (or when there is no browser, as when prerendering), the width is estimated from the characters, and the text is noted
/// so the chart can ask the browser to measure it after it renders
/// </summary>
internal sealed partial class PixataChartTextMeasurer {
  public const string AxisLabel = "pixata-blazor-chart-axis-label";
  public const string AxisTitle = "pixata-blazor-chart-axis-title";
  public const string Label = "pixata-blazor-chart-label";

  private static readonly Dictionary<string, double> DefaultFontSizes = new() {
    [AxisLabel] = 12,
    [AxisTitle] = 14,
    [Label] = 12
  };

  private readonly Dictionary<string, double> _fontSizes = [];
  private readonly Dictionary<(string Style, string Text), double> _widths = [];
  private readonly HashSet<(string Style, string Text)> _missing = [];

  /// <summary>
  /// The text the layout needed but which hasn't been measured yet, as (style key, text) pairs. The style key is the CSS
  /// class, followed by <c>|</c> and the CSS font, if the text has one of its own
  /// </summary>
  public IReadOnlyCollection<(string Style, string Text)> Missing => _missing;

  /// <summary>
  /// Whether the browser has measured anything yet
  /// </summary>
  public bool HasMeasurements => _widths.Count > 0;

  public static string StyleKey(string cssClass, string? font) =>
    string.IsNullOrWhiteSpace(font) ? cssClass : $"{cssClass}|{font}";

  public double FontSize(string cssClass, string? font = null) {
    if (_fontSizes.TryGetValue(StyleKey(cssClass, font), out double measured)) {
      return measured;
    }
    if (!string.IsNullOrWhiteSpace(font)) {
      Match size = FontSizeInFont().Match(font);
      if (size.Success) {
        double value = double.Parse(size.Groups[1].Value, CultureInfo.InvariantCulture);
        return size.Groups[2].Value == "px" ? value : value * 16;
      }
    }
    return DefaultFontSizes.TryGetValue(cssClass, out double fallback) ? fallback : 12;
  }

  public double LineHeight(string cssClass, string? font = null) =>
    Math.Ceiling(FontSize(cssClass, font) * 1.25);

  /// <summary>
  /// The width of <paramref name="text"/> in pixels
  /// </summary>
  /// <param name="record">Whether to note the text for measuring if it hasn't been measured yet</param>
  public double Width(string text, string cssClass, string? font = null, bool record = true) {
    if (text == "") {
      return 0;
    }
    string style = StyleKey(cssClass, font);
    if (_widths.TryGetValue((style, text), out double width)) {
      return width;
    }
    if (record) {
      _missing.Add((style, text));
    }
    return Estimate(text, FontSize(cssClass, font));
  }

  /// <summary>
  /// The width of the widest line of a multi-line label
  /// </summary>
  public double Width(IEnumerable<string> lines, string cssClass, string? font = null) =>
    lines.Select(l => Width(l, cssClass, font)).DefaultIfEmpty(0).Max();

  /// <summary>
  /// Stores the widths the browser measured, returning true if any of them is new
  /// </summary>
  public bool Store(string style, double fontSize, IReadOnlyList<string> texts, IReadOnlyList<double> widths) {
    bool changed = !_fontSizes.TryGetValue(style, out double previous) || Math.Abs(previous - fontSize) > .01;
    _fontSizes[style] = fontSize;
    for (int i = 0; i < texts.Count && i < widths.Count; i++) {
      changed |= !_widths.TryGetValue((style, texts[i]), out double old) || Math.Abs(old - widths[i]) > .5;
      _widths[(style, texts[i])] = widths[i];
      _missing.Remove((style, texts[i]));
    }
    return changed;
  }

  /// <summary>
  /// Forgets the measurements, eg when the page's fonts have finished loading
  /// </summary>
  public void Clear() {
    _widths.Clear();
    _fontSizes.Clear();
    _missing.Clear();
  }

  public void ClearMissing() =>
    _missing.Clear();

  /// <summary>
  /// A rough width for text in a typical sans-serif font, used until the browser has measured it
  /// </summary>
  public static double Estimate(string text, double fontSize) {
    double ems = 0;
    foreach (char c in text) {
      ems += c switch {
        ' ' => .28,
        '.' or ',' or ':' or ';' or '\'' or '!' or '|' or 'i' or 'j' or 'l' or 'I' => .28,
        'f' or 't' or 'r' or '(' or ')' or '-' => .36,
        'm' or 'w' or 'M' or 'W' or '%' or '@' => .85,
        >= '0' and <= '9' => .57,
        >= 'A' and <= 'Z' => .66,
        _ => .54
      };
    }
    return ems * fontSize;
  }

  /// <summary>
  /// Shortens text to fit <paramref name="maxWidth"/>, ending it with an ellipsis
  /// </summary>
  public static string Truncate(string text, double width, double maxWidth) {
    if (width <= maxWidth || text.Length <= 1) {
      return text;
    }
    int keep = Math.Max(1, (int)Math.Floor(text.Length * maxWidth / width) - 1);
    return keep >= text.Length ? text : $"{text[..keep].TrimEnd()}…";
  }

  [GeneratedRegex(@"(\d+(?:\.\d+)?)(px|rem|em)")]
  private static partial Regex FontSizeInFont();
}
