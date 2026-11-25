using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Pixata.Pdf.Documents;

/// <summary>
/// Reads an HTML page into paragraphs, lists, tables and pictures. It follows the page's structure and the simpler inline styles
/// (bold, italic, underline, size, colour and alignment), not its CSS layout, so a page laid out with CSS comes out as a plain
/// document. Pictures are only shown if they're in the page itself, as <c>data:</c> URLs, as there's nowhere to fetch others from
/// </summary>
internal static partial class HtmlReader {
  public static List<DocBlock> Read(byte[] file, double baseSize = 11) {
    string html = PlainTextReader.Decode(file, Charset(file));
    IHtmlDocument document = new HtmlParser().ParseDocument(html);
    List<DocBlock> blocks = [];
    if (document.Body is { } body) {
      Flow flow = new(blocks, baseSize);
      Settings settings = new() { RightToLeft = Direction(document.DocumentElement) ?? Direction(body) };
      flow.Settings = settings;
      Children(body, flow, default);
      flow.Flush();
    }
    return blocks;
  }

  // The charset a page names in its first 1024 bytes, as browsers look for it
  private static string? Charset(byte[] file) {
    string start = Encoding.ASCII.GetString(file, 0, Math.Min(file.Length, 1024));
    Match match = CharsetRegex().Match(start);
    return match.Success ? match.Groups[1].Value : null;
  }

  [GeneratedRegex(@"<meta[^>]+charset\s*=\s*[""']?([\w-]+)", RegexOptions.IgnoreCase)]
  private static partial Regex CharsetRegex();

  /// <summary>
  /// How the paragraphs made in a block look
  /// </summary>
  private sealed record Settings {
    public DocAlignment Alignment { get; init; }
    public int Heading { get; init; }
    public double? FontSize { get; init; }
    public double Indent { get; init; }
    public bool Pre { get; init; }
    public string? Marker { get; init; }
    public DocStyle MarkerStyle { get; init; }
    public int ListDepth { get; init; }
    public bool? RightToLeft { get; init; }
    public double? SpaceAfter { get; init; }
  }

  // Where the text goes: the blocks being filled (the document's, or a table cell's), and the paragraph being written
  private sealed class Flow(List<DocBlock> blocks, double baseSize) {
    public List<DocBlock> Blocks { get; } = blocks;
    public double BaseSize { get; } = baseSize;
    public Settings Settings { get; set; } = new();
    private DocParagraph? _paragraph;
    private bool _lastWasSpace = true;

    public DocParagraph Paragraph {
      get {
        if (_paragraph is null) {
          _paragraph = new() {
            Alignment = Settings.Alignment,
            HeadingLevel = Settings.Heading,
            FontSize = Settings.FontSize,
            Indent = Settings.Indent,
            ListMarker = Settings.Marker,
            ListMarkerStyle = Settings.MarkerStyle,
            RightToLeft = Settings.RightToLeft,
            SpaceAfter = Settings.SpaceAfter
          };
          // Only the first paragraph of a list item has its bullet
          Settings = Settings with { Marker = null };
          Blocks.Add(_paragraph);
          _lastWasSpace = true;
        }
        return _paragraph;
      }
    }

    public void Text(string text, DocStyle style) {
      StringBuilder collapsed = new();
      if (Settings.Pre) {
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int index = 0; index < lines.Length; index++) {
          if (index > 0) {
            Add(collapsed, style);
            Paragraph.Inlines.Add(new DocLineBreak());
          }
          collapsed.Append(lines[index].Replace('\t', ' '));
        }
        Add(collapsed, style);
        return;
      }
      foreach (char c in text) {
        if (c is ' ' or '\t' or '\r' or '\n' or '\f') {
          if (!_lastWasSpace) {
            collapsed.Append(' ');
            _lastWasSpace = true;
          }
          continue;
        }
        collapsed.Append(c);
        _lastWasSpace = false;
      }
      Add(collapsed, style);
    }

    private void Add(StringBuilder text, DocStyle style) {
      if (text.Length == 0) {
        return;
      }
      List<DocInline> inlines = Paragraph.Inlines;
      if (inlines.Count > 0 && inlines[^1] is DocText last && last.Style == style) {
        inlines[^1] = new DocText(last.Text + text, style);
      } else {
        inlines.Add(new DocText(text.ToString(), style));
      }
      text.Clear();
    }

    public void LineBreak() {
      Paragraph.Inlines.Add(new DocLineBreak());
      _lastWasSpace = true;
    }

    public void Flush() {
      _paragraph = null;
      _lastWasSpace = true;
    }

    public void Add(DocBlock block) {
      Flush();
      Blocks.Add(block);
    }
  }

  private static readonly HashSet<string> Skipped = ["script", "style", "head", "title", "template", "noscript", "meta", "link", "iframe", "object", "embed",
                                                    "svg", "math", "canvas", "video", "audio", "input", "select", "textarea", "button", "map"];

  private static readonly HashSet<string> BlockTags = ["p", "div", "blockquote", "pre", "section", "article", "header", "footer", "main", "nav", "aside",
                                                      "address", "figure", "figcaption", "center", "form", "fieldset", "legend", "dl", "dt", "dd", "details",
                                                      "summary", "li", "caption", "h1", "h2", "h3", "h4", "h5", "h6", "body", "html"];

  private static void Children(INode parent, Flow flow, DocStyle style) {
    foreach (INode node in parent.ChildNodes) {
      Node(node, flow, style);
    }
  }

  private static void Node(INode node, Flow flow, DocStyle style) {
    if (node is IText text) {
      flow.Text(text.Data, style);
      return;
    }
    if (node is not IElement element) {
      return;
    }
    string tag = element.LocalName;
    Dictionary<string, string> css = Css(element.GetAttribute("style"));
    if (Skipped.Contains(tag) || css.GetValueOrDefault("display") == "none" || element.HasAttribute("hidden")) {
      return;
    }
    DocStyle inner = Style(element, tag, css, style, flow);
    if (PageBreak(css, "before") && flow.Blocks.Count > 0) {
      flow.Add(new DocPageBreak());
    }
    Element(element, tag, css, flow, inner);
    if (PageBreak(css, "after")) {
      flow.Add(new DocPageBreak());
    }
  }

  // page-break-before: always, or break-before: page, as Word and LibreOffice write when saving a document as a web page
  private static bool PageBreak(Dictionary<string, string> css, string side) =>
    (css.GetValueOrDefault($"page-break-{side}") ?? css.GetValueOrDefault($"break-{side}")) is "always" or "page" or "left" or "right" or "recto" or "verso";

  private static void Element(IElement element, string tag, Dictionary<string, string> css, Flow flow, DocStyle inner) {
    switch (tag) {
      case "br":
        flow.LineBreak();
        return;
      case "img":
        if (Image(element, css) is { } image) {
          flow.Add(image);
        }
        return;
      case "hr":
        flow.Add(new DocRule());
        return;
      case "table":
        Table(element, flow, inner);
        return;
      case "ul" or "ol" or "menu":
        List(element, flow, inner);
        return;
    }
    if (BlockTags.Contains(tag) || css.GetValueOrDefault("display") is "block" or "flex" or "grid" or "list-item") {
      Block(element, tag, css, flow, inner);
      return;
    }
    Children(element, flow, inner);
  }

  private static void Block(IElement element, string tag, Dictionary<string, string> css, Flow flow, DocStyle style) {
    flow.Flush();
    Settings saved = flow.Settings;
    Settings settings = saved;
    if (Alignment(element, css) is { } alignment) {
      settings = settings with { Alignment = alignment };
    } else if (tag == "center") {
      settings = settings with { Alignment = DocAlignment.Centre };
    }
    if (Direction(element) is { } rightToLeft) {
      settings = settings with { RightToLeft = rightToLeft };
    }
    if (tag.Length == 2 && tag[0] == 'h' && tag[1] is >= '1' and <= '6') {
      int level = tag[1] - '0';
      settings = settings with { Heading = level, FontSize = HeadingSize(level, flow.BaseSize) };
      style = style with { Bold = true };
    } else if (tag is "blockquote" or "dd") {
      settings = settings with { Indent = settings.Indent + 24 };
    } else if (tag == "pre") {
      settings = settings with { Pre = true };
    }
    if (tag is "div" or "li" or "dt" or "dd" or "section" or "article" or "header" or "footer" or "main" or "nav" or "aside") {
      // These usually hold lines of their own, not paragraphs, so they don't add space after
      settings = settings with { SpaceAfter = settings.SpaceAfter ?? (tag == "li" ? null : 0) };
    }
    if (tag == "p" || settings.Heading > 0) {
      settings = settings with { SpaceAfter = null };
    }
    flow.Settings = settings;
    Children(element, flow, style);
    flow.Flush();
    // A list item's bullet is used up by its first paragraph, which mustn't change the settings of what comes after it
    flow.Settings = saved;
  }

  private static double HeadingSize(int level, double baseSize) =>
    baseSize * level switch {
      1 => 2,
      2 => 1.5,
      3 => 1.17,
      4 => 1,
      5 => .83,
      _ => .67
    };

  private static readonly string[] Bullets = ["•", "◦", "▪"];

  private static void List(IElement list, Flow flow, DocStyle style) {
    flow.Flush();
    bool ordered = list.LocalName == "ol";
    int number = int.TryParse(list.GetAttribute("start"), out int start) ? start : 1;
    string type = list.GetAttribute("type") ?? Css(list.GetAttribute("style")).GetValueOrDefault("list-style-type") ?? (ordered ? "1" : "disc");
    Settings saved = flow.Settings;
    int depth = saved.ListDepth;
    foreach (INode child in list.ChildNodes) {
      if (child is IElement { LocalName: "li" } item) {
        if (int.TryParse(item.GetAttribute("value"), out int value)) {
          number = value;
        }
        string? marker = type == "none" ? null : ordered ? $"{Number(number++, type)}." : Bullets[depth % Bullets.Length];
        flow.Settings = saved with { Indent = saved.Indent + 18, Marker = marker, MarkerStyle = style, ListDepth = depth + 1, SpaceAfter = null };
        Block(item, "li", Css(item.GetAttribute("style")), flow, Style(item, "li", Css(item.GetAttribute("style")), style, flow));
        flow.Settings = saved;
      } else if (child is IElement { LocalName: "ul" or "ol" } nested) {
        flow.Settings = saved with { Indent = saved.Indent + 18, ListDepth = depth + 1 };
        List(nested, flow, style);
        flow.Settings = saved;
      } else {
        Node(child, flow, style);
      }
    }
    flow.Flush();
    flow.Settings = saved;
  }

  private static string Number(int number, string type) =>
    type switch {
      "a" or "lower-alpha" or "lower-latin" => Letters(number).ToLowerInvariant(),
      "A" or "upper-alpha" or "upper-latin" => Letters(number),
      "i" or "lower-roman" => Roman(number).ToLowerInvariant(),
      "I" or "upper-roman" => Roman(number),
      _ => number.ToString(CultureInfo.InvariantCulture)
    };

  internal static string Letters(int number) {
    StringBuilder letters = new();
    while (number > 0) {
      number--;
      letters.Insert(0, (char)('A' + number % 26));
      number /= 26;
    }
    return letters.ToString();
  }

  internal static string Roman(int number) {
    if (number is <= 0 or >= 4000) {
      return number.ToString(CultureInfo.InvariantCulture);
    }
    (int Value, string Numeral)[] numerals = [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];
    StringBuilder roman = new();
    foreach ((int value, string numeral) in numerals) {
      while (number >= value) {
        roman.Append(numeral);
        number -= value;
      }
    }
    return roman.ToString();
  }

  private static void Table(IElement element, Flow flow, DocStyle style) {
    flow.Flush();
    Settings saved = flow.Settings;
    if (element.Children.FirstOrDefault(child => child.LocalName == "caption") is { } caption) {
      flow.Settings = saved with { Alignment = DocAlignment.Centre, Marker = null };
      Children(caption, flow, style);
      flow.Flush();
    }
    Dictionary<string, string> tableCss = Css(element.GetAttribute("style"));
    DocTable table = new() {
      Borders = element.GetAttribute("border") is { } border && border != "0" || HasBorder(tableCss)
    };
    List<IElement> rows = [.. element.Children.SelectMany(IEnumerable<IElement> (child) => child.LocalName is "thead" or "tbody" or "tfoot" ? child.Children : [child])
                                     .Where(child => child.LocalName == "tr")];
    foreach (IElement tr in rows) {
      DocRow row = new();
      foreach (IElement td in tr.Children.Where(child => child.LocalName is "td" or "th")) {
        Dictionary<string, string> cellCss = Css(td.GetAttribute("style"));
        table.Borders |= HasBorder(cellCss);
        DocCell cell = new() { ColumnSpan = int.TryParse(td.GetAttribute("colspan"), out int span) && span > 1 ? Math.Min(span, 50) : 1 };
        Flow cellFlow = new(cell.Blocks, flow.BaseSize) {
          Settings = new() {
            Alignment = Alignment(td, cellCss) ?? Alignment(tr, Css(tr.GetAttribute("style"))) ?? DocAlignment.Left,
            RightToLeft = saved.RightToLeft,
            SpaceAfter = 0
          }
        };
        DocStyle cellStyle = Style(td, td.LocalName, cellCss, td.LocalName == "th" ? style with { Bold = true } : style, flow);
        Children(td, cellFlow, cellStyle);
        cellFlow.Flush();
        row.Cells.Add(cell);
      }
      if (row.Cells.Count > 0) {
        table.Rows.Add(row);
      }
    }
    // Column widths, if the first row gives them all
    if (rows.FirstOrDefault()?.Children.Where(child => child.LocalName is "td" or "th").ToList() is { Count: > 0 } firstCells
        && firstCells.All(cell => !cell.HasAttribute("colspan"))) {
      List<(double Value, bool Percent)?> widths = [.. firstCells.Select(cell => Length(cell.GetAttribute("width") ?? Css(cell.GetAttribute("style")).GetValueOrDefault("width")))];
      if (widths.All(width => width is not null) && widths.Select(width => width!.Value.Percent).Distinct().Count() == 1) {
        table.ColumnWidths = [.. widths.Select(width => width!.Value.Value)];
        table.RelativeWidths = widths[0]!.Value.Percent;
      }
    }
    flow.Settings = saved;
    if (table.Rows.Count > 0) {
      flow.Add(table);
    }
  }

  private static bool HasBorder(Dictionary<string, string> css) =>
    css.Any(pair => pair.Key.StartsWith("border", StringComparison.Ordinal) && !pair.Key.Contains("radius") && !pair.Key.Contains("collapse")
                    && !pair.Key.Contains("spacing") && !pair.Key.Contains("color") && !pair.Key.Contains("style")
                    && !pair.Value.Contains("none") && !pair.Value.Contains("hidden") && !Regex.IsMatch(pair.Value, @"^0(px|pt)?(\s|$)"));

  // A length in points, or a percentage
  private static (double Value, bool Percent)? Length(string? value) {
    if (string.IsNullOrWhiteSpace(value)) {
      return null;
    }
    Match match = Regex.Match(value.Trim(), @"^([\d.]+)\s*(px|pt|%|cm|mm|in)?$");
    if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || number <= 0) {
      return null;
    }
    return match.Groups[2].Value switch {
      "%" => (number, true),
      "pt" => (number, false),
      "cm" => (number * 72 / 2.54, false),
      "mm" => (number * 72 / 25.4, false),
      "in" => (number * 72, false),
      _ => (number * .75, false)
    };
  }

  private static DocImage? Image(IElement element, Dictionary<string, string> css) {
    string source = element.GetAttribute("src") ?? "";
    Match match = Regex.Match(source, @"^data:image/[\w.+-]+;base64,(.*)$", RegexOptions.Singleline);
    if (!match.Success) {
      return null;
    }
    byte[] data;
    try {
      data = Convert.FromBase64String(Regex.Replace(match.Groups[1].Value, @"\s", ""));
    }
    catch (FormatException) {
      return null;
    }
    if (Images.Read(data) is not { } image) {
      return null;
    }
    if (Length(element.GetAttribute("width") ?? css.GetValueOrDefault("width")) is (double width, false)) {
      image.Width = width;
    }
    if (Length(element.GetAttribute("height") ?? css.GetValueOrDefault("height")) is (double height, false)) {
      image.Height = height;
    }
    return image;
  }

  // ---------- Styles ----------

  private static DocStyle Style(IElement element, string tag, Dictionary<string, string> css, DocStyle style, Flow flow) {
    style = tag switch {
      "b" or "strong" => style with { Bold = true },
      "i" or "em" or "cite" or "var" or "dfn" or "address" => style with { Italic = true },
      "u" or "ins" => style with { Underline = true },
      "s" or "strike" or "del" => style with { Strike = true },
      "small" => style with { Size = (style.Size ?? flow.BaseSize) * .83 },
      "big" => style with { Size = (style.Size ?? flow.BaseSize) * 1.2 },
      "sub" or "sup" => style with { Size = (style.Size ?? flow.BaseSize) * .75 },
      "font" => Font(element, style),
      _ => style
    };
    if (css.TryGetValue("font-weight", out string? weight)) {
      style = style with { Bold = weight is "bold" or "bolder" || int.TryParse(weight, out int number) && number >= 600 };
    }
    if (css.TryGetValue("font-style", out string? fontStyle)) {
      style = style with { Italic = fontStyle is "italic" or "oblique" };
    }
    if ((css.GetValueOrDefault("text-decoration") ?? css.GetValueOrDefault("text-decoration-line")) is { } decoration) {
      style = style with { Underline = decoration.Contains("underline"), Strike = decoration.Contains("line-through") };
    }
    if (css.TryGetValue("font-size", out string? size) && FontSize(size, style.Size ?? flow.BaseSize, flow.BaseSize) is { } points) {
      style = style with { Size = points };
    }
    if (css.TryGetValue("color", out string? colour)) {
      style = style with { Colour = Colour(colour) };
    }
    return style;
  }

  private static DocStyle Font(IElement element, DocStyle style) {
    if (element.GetAttribute("color") is { } colour) {
      style = style with { Colour = Colour(colour) };
    }
    return style;
  }

  private static double? FontSize(string value, double current, double baseSize) {
    value = value.Trim();
    double? keyword = value switch {
      "xx-small" => .6,
      "x-small" => .75,
      "small" => .89,
      "medium" => 1,
      "large" => 1.2,
      "x-large" => 1.5,
      "xx-large" => 2,
      _ => null
    };
    if (keyword is { } factor) {
      return baseSize * factor;
    }
    if (value is "smaller") {
      return current * .83;
    }
    if (value is "larger") {
      return current * 1.2;
    }
    Match match = Regex.Match(value, @"^([\d.]+)\s*(px|pt|em|rem|%)$");
    if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || number <= 0) {
      return null;
    }
    double points = match.Groups[2].Value switch {
      "pt" => number,
      "px" => number * .75,
      "em" => number * current,
      "rem" => number * baseSize,
      _ => number * current / 100
    };
    return Math.Clamp(points, 4, 96);
  }

  private static readonly Dictionary<string, (byte, byte, byte)> NamedColours = new(StringComparer.OrdinalIgnoreCase) {
    ["red"] = (255, 0, 0), ["green"] = (0, 128, 0), ["blue"] = (0, 0, 255), ["navy"] = (0, 0, 128), ["maroon"] = (128, 0, 0),
    ["purple"] = (128, 0, 128), ["teal"] = (0, 128, 128), ["olive"] = (128, 128, 0), ["gray"] = (128, 128, 128), ["grey"] = (128, 128, 128),
    ["silver"] = (192, 192, 192), ["orange"] = (255, 165, 0), ["darkblue"] = (0, 0, 139), ["darkred"] = (139, 0, 0), ["darkgreen"] = (0, 100, 0),
    ["white"] = (255, 255, 255)
  };

  internal static (byte Red, byte Green, byte Blue)? Colour(string value) {
    value = value.Trim();
    if (NamedColours.TryGetValue(value, out (byte, byte, byte) named)) {
      return named;
    }
    string hex = value.TrimStart('#');
    if (value.StartsWith('#') && hex.Length == 3) {
      hex = string.Concat(hex.Select(c => $"{c}{c}"));
    }
    if (value.StartsWith('#') && hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) {
      return Visible(((byte)(rgb >> 16), (byte)(rgb >> 8 & 255), (byte)(rgb & 255)));
    }
    Match match = Regex.Match(value, @"^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)");
    if (match.Success) {
      return Visible(((byte)Math.Min(255, int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)),
                      (byte)Math.Min(255, int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)),
                      (byte)Math.Min(255, int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture))));
    }
    return null;
  }

  // Black is the default, so it isn't set
  private static (byte Red, byte Green, byte Blue)? Visible((byte Red, byte Green, byte Blue) colour) =>
    colour is (0, 0, 0) ? null : colour;

  private static DocAlignment? Alignment(IElement element, Dictionary<string, string> css) =>
    (css.GetValueOrDefault("text-align") ?? element.GetAttribute("align"))?.ToLowerInvariant() switch {
      "center" or "middle" => DocAlignment.Centre,
      "right" or "end" => DocAlignment.Right,
      "justify" => DocAlignment.Justify,
      "left" or "start" => DocAlignment.Left,
      _ => null
    };

  private static bool? Direction(IElement? element) =>
    (element?.GetAttribute("dir") ?? Css(element?.GetAttribute("style")).GetValueOrDefault("direction"))?.ToLowerInvariant() switch {
      "rtl" => true,
      "ltr" => false,
      _ => null
    };

  // The element's inline style, as lower-case property names and values
  private static Dictionary<string, string> Css(string? style) {
    Dictionary<string, string> css = [];
    if (string.IsNullOrWhiteSpace(style)) {
      return css;
    }
    foreach (string declaration in style.Split(';')) {
      int colon = declaration.IndexOf(':');
      if (colon > 0) {
        css[declaration[..colon].Trim().ToLowerInvariant()] = declaration[(colon + 1)..].Replace("!important", "").Trim().ToLowerInvariant();
      }
    }
    return css;
  }
}
