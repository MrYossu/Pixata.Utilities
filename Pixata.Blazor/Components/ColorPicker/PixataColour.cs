using System;
using System.Globalization;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// A colour, as red, green and blue from 0 to 255 and an opacity from 0 to 1, read from and written as the CSS colours that
/// <see cref="PixataColorPicker"/> and the editor's colour tools use
/// </summary>
internal readonly record struct PixataColour(int R, int G, int B, double A = 1) {
  /// <summary>
  /// Reads a hex colour (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c>) or an
  /// <c>rgb()</c> or <c>rgba()</c> one (with commas or spaces, and the opacity after a comma or a slash, as a number or a
  /// percentage). Returns null for anything else
  /// </summary>
  public static PixataColour? Parse(string? text) {
    if (string.IsNullOrWhiteSpace(text)) {
      return null;
    }
    string value = text.Trim().ToLowerInvariant();
    if (value.StartsWith("rgb")) {
      return ParseRgb(value);
    }
    if (!value.StartsWith('#')) {
      return null;
    }
    string digits = value[1..];
    if (digits.Length is not (3 or 4 or 6 or 8) || !digits.All(Uri.IsHexDigit)) {
      return null;
    }
    if (digits.Length <= 4) {
      digits = string.Concat(digits.Select(c => $"{c}{c}"));
    }
    int Channel(int index) => int.Parse(digits.AsSpan(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    return new(Channel(0), Channel(1), Channel(2), digits.Length == 8 ? Math.Round(Channel(3) / 255d, 2) : 1);
  }

  private static PixataColour? ParseRgb(string value) {
    int open = value.IndexOf('(');
    int close = value.IndexOf(')');
    if (open < 0 || close < open) {
      return null;
    }
    string[] parts = value[(open + 1)..close].Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length is < 3 or > 4) {
      return null;
    }
    int[] channels = new int[3];
    for (int i = 0; i < 3; i++) {
      if (Number(parts[i], 255) is not double channel) {
        return null;
      }
      channels[i] = (int)Math.Round(Math.Clamp(channel, 0, 255));
    }
    double alpha = 1;
    if (parts.Length == 4) {
      if (Number(parts[3], 1) is not double a) {
        return null;
      }
      alpha = Math.Round(Math.Clamp(a, 0, 1), 2);
    }
    return new(channels[0], channels[1], channels[2], alpha);
  }

  // A number, or a percentage of the full scale
  private static double? Number(string text, double scale) {
    bool percent = text.EndsWith('%');
    return double.TryParse(percent ? text[..^1] : text, NumberStyles.Float, CultureInfo.InvariantCulture, out double n)
      ? percent ? n / 100 * scale : n
      : null;
  }

  /// <summary>
  /// <c>#rrggbb</c>, or <c>#rrggbbaa</c> if it isn't opaque
  /// </summary>
  public string ToHex() =>
    A >= 1 ? $"#{R:x2}{G:x2}{B:x2}" : $"#{R:x2}{G:x2}{B:x2}{(int)Math.Round(A * 255):x2}";

  /// <summary>
  /// <c>rgb(r, g, b)</c>, or <c>rgba(r, g, b, a)</c> if it isn't opaque
  /// </summary>
  public string ToRgb() =>
    A >= 1 ? $"rgb({R}, {G}, {B})" : $"rgba({R}, {G}, {B}, {A.ToString("0.##", CultureInfo.InvariantCulture)})";

  public string Format(PixataColorFormat format) =>
    format == PixataColorFormat.Hex ? ToHex() : ToRgb();

  /// <summary>
  /// The same colour, fully opaque
  /// </summary>
  public PixataColour Opaque() =>
    this with { A = 1 };

  /// <summary>
  /// The hue (0 to 360), saturation (0 to 1) and value, or brightness (0 to 1)
  /// </summary>
  public (double H, double S, double V) ToHsv() {
    double r = R / 255d;
    double g = G / 255d;
    double b = B / 255d;
    double max = Math.Max(r, Math.Max(g, b));
    double min = Math.Min(r, Math.Min(g, b));
    double delta = max - min;
    double h = 0;
    if (delta > 0) {
      h = max == r ? 60 * ((g - b) / delta % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
    }
    if (h < 0) {
      h += 360;
    }
    return (h, max == 0 ? 0 : delta / max, max);
  }

  public static PixataColour FromHsv(double h, double s, double v, double a = 1) {
    h = (h % 360 + 360) % 360;
    s = Math.Clamp(s, 0, 1);
    v = Math.Clamp(v, 0, 1);
    double c = v * s;
    double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
    double m = v - c;
    (double r, double g, double b) = (h / 60) switch {
      < 1 => (c, x, 0d),
      < 2 => (x, c, 0d),
      < 3 => (0d, c, x),
      < 4 => (0d, x, c),
      < 5 => (x, 0d, c),
      _ => (c, 0d, x)
    };
    return new((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255), Math.Round(Math.Clamp(a, 0, 1), 2));
  }
}
