using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Pixata.Blazor.Components;

/// <summary>
/// Makes HTML safe to render, keeping the formatting a rich text editor produces (paragraphs, headings, lists, tables, links,
/// images, bold, italic, colours and so on) and removing everything that could run script or load something unexpected.
/// <see cref="PixataEditor"/> uses it on the value it is given and on the value it emits, and you can use it on any HTML you
/// display with a <see cref="Microsoft.AspNetCore.Components.MarkupString"/>
/// </summary>
/// <remarks>
/// It works on an allow-list: the input is read, and new HTML is written out containing only the allowed elements, with only
/// the allowed attributes, and with all text and attribute values encoded. So <c>script</c>, <c>style</c>, <c>iframe</c>,
/// <c>object</c>, <c>svg</c>, forms and so on are removed with their content, unknown elements are removed but their text
/// is kept, event handler attributes (<c>onclick</c> and so on), <c>id</c> and <c>class</c> are dropped, links and images
/// are only kept with relative, http, https, mailto or tel addresses (and, for images, base64 <c>data:image</c> URLs), and
/// only a few style properties are kept, with values that can't hold a URL or an expression. Comments are removed
/// </remarks>
public static partial class PixataHtmlSanitiser {
  private static readonly HashSet<string> AllowedElements = new(StringComparer.Ordinal) {
    "a", "b", "blockquote", "br", "caption", "code", "col", "colgroup", "del", "div", "em", "font", "h1", "h2", "h3", "h4",
    "h5", "h6", "hr", "i", "img", "ins", "li", "mark", "ol", "p", "pre", "s", "small", "span", "strike", "strong", "sub",
    "sup", "table", "tbody", "td", "tfoot", "th", "thead", "tr", "u", "ul"
  };

  // Elements whose content is removed along with them
  private static readonly HashSet<string> RemovedWithContent = new(StringComparer.Ordinal) {
    "applet", "audio", "button", "canvas", "datalist", "dialog", "frameset", "head", "iframe", "map", "math", "noembed",
    "noframes", "noscript", "object", "plaintext", "script", "select", "style", "svg", "template", "textarea", "title",
    "video", "xmp"
  };

  // Elements whose content isn't markup, so it is skipped as text up to the matching end tag
  private static readonly HashSet<string> RawText = new(StringComparer.Ordinal) {
    "iframe", "noembed", "noframes", "noscript", "plaintext", "script", "style", "textarea", "title", "xmp"
  };

  private static readonly HashSet<string> VoidElements = new(StringComparer.Ordinal) {
    "area", "base", "br", "col", "embed", "frame", "hr", "img", "input", "keygen", "link", "meta", "param", "source",
    "track", "wbr"
  };

  private static readonly HashSet<string> AllowedStyles = new(StringComparer.Ordinal) {
    "background-color", "color", "font-family", "font-size", "font-style", "font-weight", "height", "margin-left",
    "text-align", "text-decoration", "text-decoration-line", "vertical-align", "width"
  };

  private static readonly HashSet<string> LinkTargets = new(StringComparer.Ordinal) { "_blank", "_self", "_parent", "_top" };

  /// <summary>
  /// Returns a safe version of <paramref name="html"/>, or <c>""</c> if it is null or empty
  /// </summary>
  public static string Sanitise(string? html) {
    if (string.IsNullOrEmpty(html)) {
      return "";
    }
    Writer writer = new(html.Length);
    int i = 0;
    while (i < html.Length) {
      int lt = html.IndexOf('<', i);
      if (lt < 0) {
        writer.Text(html[i..]);
        break;
      }
      if (lt > i) {
        writer.Text(html[i..lt]);
      }
      i = lt;
      char next = i + 1 < html.Length ? html[i + 1] : '\0';
      if (string.CompareOrdinal(html, i, "<!--", 0, 4) == 0) {
        i = CommentEnd(html, i);
      } else if (next is '!' or '?') {
        i = After(html, i, '>');
      } else if (next == '/') {
        if (i + 2 < html.Length && char.IsAsciiLetter(html[i + 2])) {
          string name = ReadName(html, i + 2, out int end);
          i = After(html, end, '>');
          writer.EndTag(name);
        } else {
          i = After(html, i, '>');
        }
      } else if (char.IsAsciiLetter(next)) {
        string name = ReadName(html, i + 1, out int end);
        List<KeyValuePair<string, string>> attributes = ReadAttributes(html, end, out i);
        if (RawText.Contains(name)) {
          i = SkipRawText(html, i, name);
          continue;
        }
        writer.StartTag(name, attributes);
      } else {
        writer.Text("<");
        i++;
      }
    }
    return writer.Finish();
  }

  /// <summary>
  /// Returns the URL if it is relative or uses http, https, mailto or tel (or, when <paramref name="allowDataImage"/> is
  /// true, is a base64 <c>data:image</c> URL for a raster image), otherwise null
  /// </summary>
  public static string? SafeUrl(string? url, bool allowDataImage = false) {
    if (url is null) {
      return null;
    }
    string trimmed = url.Trim();
    if (trimmed == "") {
      return null;
    }
    string compact = new(trimmed.Where(c => c > ' ' && (c < '\u007f' || c > '\u009f')).ToArray());
    Match scheme = SchemeRegex().Match(compact);
    if (!scheme.Success) {
      return trimmed;
    }
    string name = scheme.Groups[1].Value.ToLowerInvariant();
    if (name is "http" or "https" or "mailto" or "tel") {
      return trimmed;
    }
    return allowDataImage && name == "data" && DataImageRegex().IsMatch(compact) ? compact : null;
  }

  /// <summary>
  /// Returns the CSS value if it is safe in a style attribute (no <c>url()</c>, <c>expression()</c>, escapes, comments or
  /// anything that could end the declaration, and no functions other than the colour ones), otherwise null
  /// </summary>
  public static string? SafeCssValue(string? value) {
    if (value is null) {
      return null;
    }
    string text = value.Trim();
    if (text == "" || text.Length > 200 || UnsafeCssRegex().IsMatch(text) || !CssCharactersRegex().IsMatch(text)) {
      return null;
    }
    foreach (Match function in CssFunctionRegex().Matches(text)) {
      if (!ColourFunctionRegex().IsMatch(function.Value)) {
        return null;
      }
    }
    return text;
  }

  // Reading

  private static string ReadName(string html, int start, out int end) {
    end = start;
    while (end < html.Length && !char.IsWhiteSpace(html[end]) && html[end] != '/' && html[end] != '>') {
      end++;
    }
    return html[start..end].ToLowerInvariant();
  }

  private static List<KeyValuePair<string, string>> ReadAttributes(string html, int start, out int end) {
    List<KeyValuePair<string, string>> attributes = [];
    // Only the first of a repeated attribute counts. The names seen are kept in a set rather than found by searching the
    // list, so that a tag with thousands of attributes takes linear rather than quadratic time
    HashSet<string> names = new(StringComparer.Ordinal);
    int i = start;
    while (i < html.Length) {
      while (i < html.Length && (char.IsWhiteSpace(html[i]) || html[i] == '/')) {
        i++;
      }
      if (i >= html.Length || html[i] == '>') {
        break;
      }
      int nameStart = i;
      i++;
      while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '/' && html[i] != '>' && html[i] != '=') {
        i++;
      }
      string name = html[nameStart..i].ToLowerInvariant();
      while (i < html.Length && char.IsWhiteSpace(html[i])) {
        i++;
      }
      string value = "";
      if (i < html.Length && html[i] == '=') {
        i++;
        while (i < html.Length && char.IsWhiteSpace(html[i])) {
          i++;
        }
        if (i < html.Length && html[i] is '"' or '\'') {
          char quote = html[i];
          int close = html.IndexOf(quote, i + 1);
          int valueEnd = close < 0 ? html.Length : close;
          value = html[(i + 1)..valueEnd];
          i = close < 0 ? html.Length : close + 1;
        } else {
          int valueStart = i;
          while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '>') {
            i++;
          }
          value = html[valueStart..i];
        }
      }
      if (names.Add(name)) {
        attributes.Add(new(name, WebUtility.HtmlDecode(value)));
      }
    }
    end = i < html.Length ? i + 1 : html.Length;
    return attributes;
  }

  private static int After(string html, int start, char c) {
    int index = html.IndexOf(c, start);
    return index < 0 ? html.Length : index + 1;
  }

  private static int CommentEnd(string html, int start) {
    // <!--> and <!---> are complete (empty) comments
    if (string.CompareOrdinal(html, start, "<!-->", 0, 5) == 0) {
      return start + 5;
    }
    if (string.CompareOrdinal(html, start, "<!--->", 0, 6) == 0) {
      return start + 6;
    }
    int end = html.IndexOf("-->", start + 4, StringComparison.Ordinal);
    return end < 0 ? html.Length : end + 3;
  }

  private static int SkipRawText(string html, int start, string name) {
    if (name == "plaintext") {
      return html.Length;
    }
    int i = start;
    while (true) {
      int close = html.IndexOf("</", i, StringComparison.Ordinal);
      if (close < 0) {
        return html.Length;
      }
      int nameEnd = close + 2 + name.Length;
      if (nameEnd <= html.Length
          && string.Compare(html, close + 2, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0
          && (nameEnd == html.Length || char.IsWhiteSpace(html[nameEnd]) || html[nameEnd] is '/' or '>')) {
        return After(html, nameEnd, '>');
      }
      i = close + 2;
    }
  }

  // Writing

  private sealed class Writer(int capacity) {
    private readonly StringBuilder _output = new(capacity);
    private readonly Stack<string> _open = new();
    // How many of each element are in _open, so an end tag can be checked without searching the stack (which made deeply
    // nested input with many stray end tags take quadratic time)
    private readonly Dictionary<string, int> _openCounts = new(StringComparer.Ordinal);
    private string? _skipping;
    private int _skipDepth;

    public void Text(string text) {
      if (_skipping is null) {
        AppendEncoded(_output, WebUtility.HtmlDecode(text), false);
      }
    }

    public void StartTag(string name, List<KeyValuePair<string, string>> attributes) {
      if (_skipping is not null) {
        if (name == _skipping) {
          _skipDepth++;
        }
        return;
      }
      if (RemovedWithContent.Contains(name) && !VoidElements.Contains(name)) {
        _skipping = name;
        _skipDepth = 1;
        return;
      }
      if (!AllowedElements.Contains(name)) {
        return;
      }
      _output.Append('<').Append(name);
      foreach ((string attribute, string value) in AllowedAttributes(name, attributes)) {
        _output.Append(' ').Append(attribute).Append("=\"");
        AppendEncoded(_output, value, true);
        _output.Append('"');
      }
      _output.Append('>');
      if (!VoidElements.Contains(name)) {
        _open.Push(name);
        _openCounts[name] = _openCounts.GetValueOrDefault(name) + 1;
      }
    }

    public void EndTag(string name) {
      if (_skipping is not null) {
        if (name == _skipping && --_skipDepth == 0) {
          _skipping = null;
        }
        return;
      }
      if (!AllowedElements.Contains(name) || VoidElements.Contains(name) || _openCounts.GetValueOrDefault(name) == 0) {
        return;
      }
      while (_open.Count > 0) {
        string open = _open.Pop();
        _openCounts[open]--;
        _output.Append("</").Append(open).Append('>');
        if (open == name) {
          break;
        }
      }
    }

    public string Finish() {
      while (_open.Count > 0) {
        _output.Append("</").Append(_open.Pop()).Append('>');
      }
      return _output.ToString();
    }
  }

  private static IEnumerable<KeyValuePair<string, string>> AllowedAttributes(string element, List<KeyValuePair<string, string>> attributes) {
    string? target = null;
    foreach ((string name, string value) in attributes) {
      string? kept = name switch {
        "title" or "lang" => value,
        "dir" => value.ToLowerInvariant() is "ltr" or "rtl" or "auto" ? value.ToLowerInvariant() : null,
        "style" => Style(value),
        "href" when element == "a" => SafeUrl(value),
        "target" when element == "a" => LinkTargets.Contains(value) ? value : null,
        "src" when element == "img" => SafeUrl(value, true),
        "alt" when element == "img" => value,
        "width" or "height" when element is "img" or "col" or "td" or "th" or "table" => SizeRegex().IsMatch(value.Trim()) ? value.Trim() : null,
        "colspan" or "rowspan" when element is "td" or "th" => NumberRegex().IsMatch(value.Trim()) ? value.Trim() : null,
        "data-colwidth" when element is "td" or "th" => ColumnWidthsRegex().IsMatch(value.Trim()) ? value.Trim() : null,
        "span" when element is "col" or "colgroup" => NumberRegex().IsMatch(value.Trim()) ? value.Trim() : null,
        "start" when element == "ol" => int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int start) ? start.ToString(CultureInfo.InvariantCulture) : null,
        "type" when element == "ol" => value.Trim() is "1" or "a" or "A" or "i" or "I" ? value.Trim() : null,
        "color" or "face" when element == "font" => SafeCssValue(value),
        "size" when element == "font" => value.Trim() is ['1' or '2' or '3' or '4' or '5' or '6' or '7'] ? value.Trim() : null,
        _ => null
      };
      if (name == "target" && kept is not null) {
        target = kept;
      }
      if (kept is not null) {
        yield return new(name, kept);
      }
    }
    if (target == "_blank") {
      yield return new("rel", "noopener noreferrer");
    }
  }

  // Keeps the allowed style properties with safe values, written as "name: value;" separated by spaces
  private static string? Style(string style) {
    List<string> kept = [];
    foreach (string declaration in style.Split(';')) {
      int colon = declaration.IndexOf(':');
      if (colon <= 0) {
        continue;
      }
      string property = declaration[..colon].Trim().ToLowerInvariant();
      string? value = SafeCssValue(declaration[(colon + 1)..]);
      if (AllowedStyles.Contains(property) && value is not null && kept.All(k => !k.StartsWith($"{property}:", StringComparison.Ordinal))) {
        kept.Add($"{property}: {value};");
      }
    }
    return kept.Count == 0 ? null : string.Join(" ", kept);
  }

  private static void AppendEncoded(StringBuilder output, string text, bool attribute) {
    foreach (char c in text) {
      switch (c) {
        case '&':
          output.Append("&amp;");
          break;
        case '<':
          output.Append("&lt;");
          break;
        case '>':
          output.Append("&gt;");
          break;
        case '"' when attribute:
          output.Append("&quot;");
          break;
        case '\u00a0':
          output.Append("&nbsp;");
          break;
        case '\0':
          break;
        default:
          output.Append(c);
          break;
      }
    }
  }

  [GeneratedRegex("^([a-z][a-z0-9+.-]*):", RegexOptions.IgnoreCase)]
  private static partial Regex SchemeRegex();

  [GeneratedRegex("^data:image/(png|gif|jpe?g|webp|bmp|avif);base64,[a-z0-9+/=]*$", RegexOptions.IgnoreCase)]
  private static partial Regex DataImageRegex();

  [GeneratedRegex(@"[<>\\{};@!]|/\*|\*/|javascript:|expression|url\s*\(|image-set|var\s*\(", RegexOptions.IgnoreCase)]
  private static partial Regex UnsafeCssRegex();

  [GeneratedRegex(@"^[\p{L}\p{N}\s#%.,'""()+-]*$")]
  private static partial Regex CssCharactersRegex();

  [GeneratedRegex(@"[a-z-]+\s*\(", RegexOptions.IgnoreCase)]
  private static partial Regex CssFunctionRegex();

  [GeneratedRegex(@"^(rgba?|hsla?)\s*\($", RegexOptions.IgnoreCase)]
  private static partial Regex ColourFunctionRegex();

  [GeneratedRegex(@"^\d{1,5}(\.\d+)?(px|%)?$")]
  private static partial Regex SizeRegex();

  [GeneratedRegex(@"^\d{1,4}$")]
  private static partial Regex NumberRegex();

  [GeneratedRegex(@"^\d{1,5}(,\d{1,5})*$")]
  private static partial Regex ColumnWidthsRegex();
}
