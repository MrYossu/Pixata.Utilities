using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using V = DocumentFormat.OpenXml.Vml;

namespace Pixata.Pdf.Documents;

/// <summary>
/// Reads a Word document (.docx, and its templates and macro-enabled forms) into paragraphs, lists, tables and pictures, with the
/// formatting its styles and numbering give. Headers, footers, footnotes, comments and deleted text are left out, and text boxes
/// come after the paragraph they're anchored to
/// </summary>
internal sealed class WordReader {
  private readonly MainDocumentPart _main;
  private readonly Dictionary<string, Style> _styles = [];
  private readonly OpenXmlElement? _defaultRun;
  private readonly OpenXmlElement? _defaultParagraph;
  private readonly string? _defaultParagraphStyle;
  private readonly Dictionary<int, NumberingInstance> _numbers = [];
  private readonly Dictionary<int, AbstractNum> _abstractNumbers = [];

  // The current number at each level of each list, by its abstract numbering, as lists that share one carry on from each other
  private readonly Dictionary<int, int?[]> _counters = [];
  private readonly HashSet<(int Number, int Level)> _started = [];

  // The last paragraph read, for spacing that's left out between paragraphs of the same style
  private DocParagraph? _lastParagraph;
  private string? _lastStyle;
  private bool _lastContextual;

  private WordReader(MainDocumentPart main) {
    _main = main;
    if (main.StyleDefinitionsPart?.Styles is { } styles) {
      foreach (Style style in styles.Elements<Style>()) {
        if (style.StyleId?.Value is { } id) {
          _styles.TryAdd(id, style);
          if (_defaultParagraphStyle is null && style.Type?.Value == StyleValues.Paragraph && style.Default?.Value == true) {
            _defaultParagraphStyle = id;
          }
        }
      }
      _defaultRun = styles.DocDefaults?.RunPropertiesDefault?.RunPropertiesBaseStyle;
      _defaultParagraph = styles.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle;
    }
    if (main.NumberingDefinitionsPart?.Numbering is { } numbering) {
      foreach (NumberingInstance number in numbering.Elements<NumberingInstance>()) {
        if (number.NumberID?.Value is int id) {
          _numbers.TryAdd(id, number);
        }
      }
      foreach (AbstractNum abstractNumber in numbering.Elements<AbstractNum>()) {
        if (abstractNumber.AbstractNumberId?.Value is int id) {
          _abstractNumbers.TryAdd(id, abstractNumber);
        }
      }
    }
  }

  public static List<DocBlock> Read(byte[] file) {
    try {
      using MemoryStream stream = new(file, false);
      using WordprocessingDocument document = WordprocessingDocument.Open(stream, false);
      if (document.MainDocumentPart?.Document?.Body is not { } body) {
        throw new DocumentToPdfException("The Word document has no text in it");
      }
      WordReader reader = new(document.MainDocumentPart);
      List<DocBlock> blocks = [];
      reader.Blocks(body, blocks, []);
      return blocks;
    }
    catch (Exception ex) when (ex is not OutOfMemoryException and not DocumentToPdfException) {
      throw new DocumentToPdfException($"The Word document couldn't be read. It may be damaged. {ex.Message}", ex);
    }
  }

  // ---------- Formatting ----------

  private readonly record struct RunFormat(bool Bold, bool Italic, bool Underline, bool Strike, bool Hidden, bool Caps, double? Size,
                                           (byte Red, byte Green, byte Blue)? Colour, bool Raised, string? Font) {
    public DocStyle Style => new(Bold, Italic, Underline, Strike, Size is { } size && Raised ? size * .65 : Raised ? 11 * .65 : Size, Colour);
  }

  private sealed record ParagraphFormat {
    public DocAlignment Alignment { get; init; }
    public double Left { get; init; }
    public double FirstLine { get; init; }
    public double? Before { get; init; }
    public double? After { get; init; }
    public double? LineMultiple { get; init; }
    public double? LinePoints { get; init; }
    public bool LineAtLeast { get; init; }
    public bool RightToLeft { get; init; }
    public int? Number { get; init; }
    public int? Level { get; init; }
    public int OutlineLevel { get; init; } = -1;
    public bool Contextual { get; init; }
    public bool PageBreakBefore { get; init; }
  }

  private static bool On(OnOffType? element) =>
    element is not null && (element.Val is null || element.Val.Value);

  private static double Twips(string? value) =>
    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double twips) ? twips / 20 : 0;

  private static RunFormat ApplyRun(OpenXmlElement? properties, RunFormat format) {
    if (properties is null) {
      return format;
    }
    if (properties.GetFirstChild<Bold>() is { } bold) {
      format = format with { Bold = On(bold) };
    }
    if (properties.GetFirstChild<Italic>() is { } italic) {
      format = format with { Italic = On(italic) };
    }
    if (properties.GetFirstChild<Underline>() is { } underline) {
      format = format with { Underline = underline.Val is null || underline.Val.Value != UnderlineValues.None };
    }
    if (properties.GetFirstChild<Strike>() is { } strike) {
      format = format with { Strike = On(strike) };
    }
    if (properties.GetFirstChild<DoubleStrike>() is { } doubleStrike) {
      format = format with { Strike = On(doubleStrike) };
    }
    if (properties.GetFirstChild<Vanish>() is { } vanish) {
      format = format with { Hidden = On(vanish) };
    }
    if (properties.GetFirstChild<Caps>() is { } caps) {
      format = format with { Caps = On(caps) };
    }
    if (properties.GetFirstChild<SmallCaps>() is { } smallCaps && On(smallCaps)) {
      format = format with { Caps = true };
    }
    if (properties.GetFirstChild<FontSize>()?.Val?.Value is { } size && double.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out double halfPoints) && halfPoints > 0) {
      format = format with { Size = halfPoints / 2 };
    }
    if (properties.GetFirstChild<Color>()?.Val?.Value is { } colour) {
      format = format with { Colour = Colour(colour) };
    }
    if (properties.GetFirstChild<VerticalTextAlignment>()?.Val is { } position) {
      format = format with { Raised = position.Value == VerticalPositionValues.Superscript || position.Value == VerticalPositionValues.Subscript };
    }
    if (properties.GetFirstChild<RunFonts>() is { } fonts && (fonts.Ascii?.Value ?? fonts.HighAnsi?.Value) is { } font) {
      format = format with { Font = font };
    }
    return format;
  }

  // A colour as Word writes it, such as 365F91. Black and "auto" are the default colour
  private static (byte, byte, byte)? Colour(string value) {
    if (value.Length != 6 || !int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb) || rgb == 0) {
      return null;
    }
    return ((byte)(rgb >> 16), (byte)(rgb >> 8 & 0xff), (byte)(rgb & 0xff));
  }

  private static ParagraphFormat ApplyParagraph(OpenXmlElement? properties, ParagraphFormat format) {
    if (properties is null) {
      return format;
    }
    if (properties.GetFirstChild<Justification>()?.Val is { } justification) {
      JustificationValues value = justification.Value;
      format = format with {
        Alignment = value == JustificationValues.Center ? DocAlignment.Centre
          : value == JustificationValues.Right || value == JustificationValues.End ? DocAlignment.Right
          : value == JustificationValues.Both || value == JustificationValues.Distribute ? DocAlignment.Justify
          : DocAlignment.Left
      };
    }
    if (properties.GetFirstChild<Indentation>() is { } indentation) {
      if ((indentation.Left?.Value ?? indentation.Start?.Value) is { } left) {
        format = format with { Left = Twips(left) };
      }
      if (indentation.Hanging?.Value is { } hanging) {
        format = format with { FirstLine = -Twips(hanging) };
      } else if (indentation.FirstLine?.Value is { } firstLine) {
        format = format with { FirstLine = Twips(firstLine) };
      }
    }
    if (properties.GetFirstChild<SpacingBetweenLines>() is { } spacing) {
      if (spacing.BeforeAutoSpacing is { } beforeAuto && beforeAuto.Value) {
        format = format with { Before = 14 };
      } else if (spacing.Before?.Value is { } before) {
        format = format with { Before = Twips(before) };
      }
      if (spacing.AfterAutoSpacing is { } afterAuto && afterAuto.Value) {
        format = format with { After = 14 };
      } else if (spacing.After?.Value is { } after) {
        format = format with { After = Twips(after) };
      }
      if (spacing.Line?.Value is { } line && Twips(line) > 0) {
        LineSpacingRuleValues? rule = spacing.LineRule?.Value;
        format = rule is null || rule == LineSpacingRuleValues.Auto
          ? format with { LineMultiple = Twips(line) / 12, LinePoints = null }
          : format with { LinePoints = Twips(line), LineAtLeast = rule == LineSpacingRuleValues.AtLeast, LineMultiple = null };
      }
    }
    if (properties.GetFirstChild<BiDi>() is { } bidi) {
      format = format with { RightToLeft = On(bidi) };
    }
    if (properties.GetFirstChild<NumberingProperties>() is { } numbering) {
      if (numbering.NumberingId?.Val?.Value is int number) {
        format = format with { Number = number };
      }
      if (numbering.NumberingLevelReference?.Val?.Value is int level) {
        format = format with { Level = level };
      }
    }
    if (properties.GetFirstChild<OutlineLevel>()?.Val?.Value is int outline) {
      format = format with { OutlineLevel = outline };
    }
    if (properties.GetFirstChild<ContextualSpacing>() is { } contextual) {
      format = format with { Contextual = On(contextual) };
    }
    if (properties.GetFirstChild<PageBreakBefore>() is { } pageBreak) {
      format = format with { PageBreakBefore = On(pageBreak) };
    }
    return format;
  }

  // A style, then the one it's based on, and so on, with the first ancestor first, as that's the order they're applied in
  private List<Style> Chain(string? styleId) {
    List<Style> chain = [];
    HashSet<string> seen = [];
    while (styleId is not null && seen.Add(styleId) && _styles.TryGetValue(styleId, out Style? style)) {
      chain.Insert(0, style);
      styleId = style.BasedOn?.Val?.Value;
    }
    return chain;
  }

  // ---------- Blocks ----------

  private void Blocks(OpenXmlElement container, List<DocBlock> blocks, List<Style> tableStyles) {
    foreach (OpenXmlElement element in container.ChildElements) {
      switch (element) {
        case Paragraph paragraph:
          ReadParagraph(paragraph, blocks, tableStyles);
          break;
        case Table table:
          ReadTable(table, blocks);
          _lastParagraph = null;
          break;
        case AltChunk chunk:
          ReadChunk(chunk, blocks);
          _lastParagraph = null;
          break;
        case AlternateContent alternate:
          if (Chosen(alternate) is { } chosen) {
            Blocks(chosen, blocks, tableStyles);
          }
          break;
        default:
          // Content controls and custom XML hold paragraphs and tables of their own
          if (element.LocalName is "sdt" or "sdtContent" or "customXml" or "ins" or "moveTo") {
            Blocks(element, blocks, tableStyles);
          }
          break;
      }
    }
  }

  // The part of Markup Compatibility content to use: the first choice, as Word writes the newer markup first, else the fallback
  private static OpenXmlElement? Chosen(AlternateContent alternate) =>
    alternate.GetFirstChild<AlternateContentChoice>() is { HasChildren: true } choice ? choice : alternate.GetFirstChild<AlternateContentFallback>();

  // Another document put in this one as it is, such as a web page pasted in by a program
  private void ReadChunk(AltChunk chunk, List<DocBlock> blocks) {
    if (chunk.Id?.Value is not { } id || !_main.TryGetPartById(id, out OpenXmlPart? part)) {
      return;
    }
    byte[] data = PartData(part);
    string type = part.ContentType.ToLowerInvariant();
    try {
      blocks.AddRange(type switch {
        _ when type.Contains("html") => HtmlReader.Read(data),
        _ when type.Contains("rtf") => RtfReader.Read(data),
        "text/plain" => PlainTextReader.Read(data),
        _ when type.Contains("wordprocessingml") => Read(data),
        _ => []
      });
    }
    catch (DocumentToPdfException) {
      // A part that can't be read is left out, rather than losing the rest of the document
    }
  }

  private static byte[] PartData(OpenXmlPart part) {
    using Stream stream = part.GetStream();
    using MemoryStream copy = new();
    stream.CopyTo(copy);
    return copy.ToArray();
  }

  private void ReadParagraph(Paragraph paragraph, List<DocBlock> blocks, List<Style> tableStyles) {
    ParagraphProperties? properties = paragraph.ParagraphProperties;
    string? styleId = properties?.ParagraphStyleId?.Val?.Value ?? _defaultParagraphStyle;
    List<Style> chain = Chain(styleId);

    // The paragraph's formatting comes from the document's defaults, then the table's style, the paragraph's style and those it's
    // based on, its list level, then what's set on the paragraph itself
    ParagraphFormat format = ApplyParagraph(_defaultParagraph, new());
    RunFormat run = ApplyRun(_defaultRun, new());
    foreach (Style style in tableStyles) {
      format = ApplyParagraph(style.StyleParagraphProperties, format);
      run = ApplyRun(style.StyleRunProperties, run);
    }
    foreach (Style style in chain) {
      format = ApplyParagraph(style.StyleParagraphProperties, format);
      run = ApplyRun(style.StyleRunProperties, run);
    }
    ParagraphFormat direct = ApplyParagraph(properties, format);
    string? marker = null;
    RunFormat markerRun = run;
    int number = direct.Number ?? 0;
    if (number > 0 && Level(number, direct.Level ?? 0) is { } level) {
      format = ApplyParagraph(level.PreviousParagraphProperties, format);
      marker = Marker(number, direct.Level ?? 0, level);
      markerRun = ApplyRun(level.NumberingSymbolRunProperties, ApplyRun(properties?.ParagraphMarkRunProperties, run)) with { Underline = false };
    }
    format = ApplyParagraph(properties, format);
    RunFormat mark = ApplyRun(properties?.ParagraphMarkRunProperties, run);

    int heading = format.OutlineLevel is >= 0 and <= 5 ? format.OutlineLevel + 1 : 0;
    for (int index = chain.Count - 1; index >= 0 && heading == 0; index--) {
      heading = RtfReader.HeadingLevel(chain[index].StyleName?.Val?.Value);
    }
    double size = mark.Size ?? 11;
    double lineSpacing = format.LineMultiple ?? (format.LinePoints is { } points
      ? format.LineAtLeast ? Math.Max(1, points / (size * 1.2)) : points / (size * 1.2)
      : 1);

    if (format.PageBreakBefore && blocks.Count > 0) {
      blocks.Add(new DocPageBreak());
      _lastParagraph = null;
    }

    bool first = true;
    DocParagraph Create() {
      DocParagraph created = new() {
        Alignment = format.Alignment,
        RightToLeft = format.RightToLeft ? true : null,
        Indent = Math.Max(0, format.Left),
        FirstLineIndent = marker is null ? format.FirstLine : 0,
        SpaceBefore = first ? format.Before ?? 0 : 0,
        SpaceAfter = format.After ?? 0,
        LineSpacing = lineSpacing > 0 ? lineSpacing : 1,
        FontSize = mark.Size,
        HeadingLevel = heading,
        ListMarker = first ? marker : null,
        ListMarkerStyle = markerRun.Style
      };
      first = false;
      return created;
    }

    ParagraphReader reader = new(this, blocks, Create, run, format.Alignment);
    reader.Inlines(paragraph);
    DocParagraph? last = reader.Finish();

    // Spacing between paragraphs of the same style can be left out, as it is between the items of a list
    if (last is not null) {
      if (reader.Parts == 1 && _lastParagraph is not null && format.Contextual && _lastContextual && _lastStyle == styleId) {
        _lastParagraph.SpaceAfter = 0;
        last.SpaceBefore = 0;
      }
      _lastParagraph = last;
      _lastStyle = styleId;
      _lastContextual = format.Contextual;
    } else {
      _lastParagraph = null;
    }
    foreach (OpenXmlElement textBox in reader.TextBoxes) {
      Blocks(textBox, blocks, []);
    }
  }

  // Reads the text, breaks and pictures in a paragraph. A page break or a picture splits it, as they're blocks of their own
  private sealed class ParagraphReader(WordReader reader, List<DocBlock> blocks, Func<DocParagraph> create, RunFormat paragraphRun, DocAlignment alignment) {
    private DocParagraph _current = create();
    private bool _split;

    public List<OpenXmlElement> TextBoxes { get; } = [];
    public int Parts { get; private set; }

    public void Inlines(OpenXmlElement container) {
      foreach (OpenXmlElement element in container.ChildElements) {
        switch (element) {
          case Run run:
            Run(run);
            break;
          case AlternateContent alternate:
            if (Chosen(alternate) is { } chosen) {
              Inlines(chosen);
            }
            break;
          default:
            switch (element.LocalName) {
              // Links, fields, content controls, inserted text and so on hold runs of their own. Deleted text doesn't count
              case "hyperlink" or "fldSimple" or "sdt" or "sdtContent" or "ins" or "moveTo" or "smartTag" or "customXml" or "bdo" or "dir":
                Inlines(element);
                break;
              case "oMath" or "oMathPara":
                Text(element.InnerText, paragraphRun);
                break;
            }
            break;
        }
      }
    }

    private void Run(Run run) {
      RunFormat format = paragraphRun;
      if (run.RunProperties?.RunStyle?.Val?.Value is { } characterStyle) {
        foreach (Style style in reader.Chain(characterStyle)) {
          format = ApplyRun(style.StyleRunProperties, format);
        }
      }
      format = ApplyRun(run.RunProperties, format);
      if (format.Hidden) {
        return;
      }
      RunContent(run, format);
    }

    private void RunContent(OpenXmlElement container, RunFormat format) {
      foreach (OpenXmlElement element in container.ChildElements) {
        switch (element) {
          case Text text:
            Text(text.Text, format);
            break;
          case TabChar or PositionalTab:
            _current.Inlines.Add(new DocTab(format.Style));
            break;
          case Break lineBreak when lineBreak.Type?.Value == BreakValues.Page:
            PageBreak();
            break;
          case Break or CarriageReturn:
            _current.Inlines.Add(new DocLineBreak());
            break;
          case NoBreakHyphen:
            Text("\u2011", format);
            break;
          case SymbolChar symbol:
            if (int.TryParse(symbol.Char?.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code)) {
              string? font = symbol.Font?.Value;
              Text(Symbols.IsSymbolFont(font) ? Symbols.Map(code, font) : ((char)(code is >= 0xf000 and <= 0xf0ff ? code - 0xf000 : code)).ToString(), format with { Font = null });
            }
            break;
          case Drawing drawing:
            Drawing(drawing);
            break;
          case Picture or EmbeddedObject:
            Vml(element);
            break;
          case AlternateContent alternate:
            if (Chosen(alternate) is { } chosen) {
              RunContent(chosen, format);
            }
            break;
        }
      }
    }

    private void Text(string text, RunFormat format) {
      if (text.Length == 0) {
        return;
      }
      if (Symbols.IsSymbolFont(format.Font)) {
        StringBuilder mapped = new();
        foreach (char c in text) {
          mapped.Append(c is ' ' ? " " : Symbols.Map(c, format.Font));
        }
        text = mapped.ToString();
      }
      if (format.Caps) {
        text = text.ToUpperInvariant();
      }
      DocStyle style = format.Style;
      List<DocInline> inlines = _current.Inlines;
      if (inlines.Count > 0 && inlines[^1] is DocText last && last.Style == style) {
        inlines[^1] = new DocText(last.Text + text, style);
      } else {
        inlines.Add(new DocText(text, style));
      }
    }

    private void PageBreak() {
      Flush();
      blocks.Add(new DocPageBreak());
      _split = true;
    }

    private void AddPicture(DocImage? image) {
      if (image is null) {
        return;
      }
      Flush();
      image.Alignment = alignment;
      blocks.Add(image);
      _split = true;
    }

    // Ends the part of the paragraph before a page break or picture, if it has anything in it
    private void Flush() {
      if (_current.Inlines.Count > 0) {
        blocks.Add(_current);
        Parts++;
        _current = create();
      }
    }

    /// <summary>
    /// Ends the paragraph. An empty paragraph is a blank line, unless it only held a page break or picture
    /// </summary>
    /// <returns>The paragraph's last part, if it ended with text</returns>
    public DocParagraph? Finish() {
      if (_current.Inlines.Count > 0 || !_split) {
        blocks.Add(_current);
        Parts++;
        return _current;
      }
      return null;
    }

    private void Drawing(Drawing drawing) {
      List<A.Blip> blips = [.. drawing.Descendants<A.Blip>()];
      DW.Extent? extent = drawing.Descendants<DW.Extent>().FirstOrDefault();
      foreach (A.Blip blip in blips) {
        DocImage? image = reader.Image(blip.Embed?.Value);
        if (image is not null && blips.Count == 1 && extent?.Cx?.Value is long width && extent.Cy?.Value is long height && width > 0 && height > 0) {
          // English Metric Units, of which there are 12,700 to a point
          image.Width = width / 12700.0;
          image.Height = height / 12700.0;
        }
        AddPicture(image);
      }
      TextBoxes.AddRange(TopLevel<TextBoxContent>(drawing));
    }

    // Pictures in the older drawing markup, such as in documents from Word 2003, and the pictures that stand in for embedded objects
    private void Vml(OpenXmlElement element) {
      foreach (V.ImageData data in element.Descendants<V.ImageData>()) {
        DocImage? image = reader.Image(data.RelationshipId?.Value);
        if (image is not null && data.Ancestors<V.Shape>().FirstOrDefault()?.Style?.Value is { } style) {
          image.Width = VmlLength(style, "width");
          image.Height = VmlLength(style, "height");
        }
        AddPicture(image);
      }
      TextBoxes.AddRange(TopLevel<TextBoxContent>(element));
    }
  }

  // The elements of a kind that aren't inside another of the same kind, such as the text boxes in a drawing, but not those in them
  private static IEnumerable<T> TopLevel<T>(OpenXmlElement root) where T : OpenXmlElement {
    foreach (OpenXmlElement child in root.ChildElements) {
      if (child is T found) {
        yield return found;
      } else {
        foreach (T descendant in TopLevel<T>(child)) {
          yield return descendant;
        }
      }
    }
  }

  // A length in a VML style, such as "width:120pt;height:1.5in"
  private static double? VmlLength(string style, string name) {
    foreach (string declaration in style.Split(';')) {
      int colon = declaration.IndexOf(':');
      if (colon < 0 || !declaration[..colon].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }
      string value = declaration[(colon + 1)..].Trim().ToLowerInvariant();
      int unitStart = value.Length;
      while (unitStart > 0 && char.IsAsciiLetter(value[unitStart - 1])) {
        unitStart--;
      }
      if (!double.TryParse(value[..unitStart], NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || number <= 0) {
        return null;
      }
      return value[unitStart..] switch {
        "pt" or "" => number,
        "in" => number * 72,
        "cm" => number * 72 / 2.54,
        "mm" => number * 72 / 25.4,
        "px" => number * .75,
        "pc" => number * 12,
        _ => null
      };
    }
    return null;
  }

  private DocImage? Image(string? relationshipId) {
    if (relationshipId is null || !_main.TryGetPartById(relationshipId, out OpenXmlPart? part)) {
      return null;
    }
    // Only PNGs and JPEGs can go in a PDF as they are. Other kinds, such as the EMF previews of embedded objects, are left out
    return Images.Read(PartData(part));
  }

  // ---------- Lists ----------

  private Level? Level(int number, int level) {
    if (!_numbers.TryGetValue(number, out NumberingInstance? instance)) {
      return null;
    }
    if (instance.Elements<LevelOverride>().FirstOrDefault(item => item.LevelIndex?.Value == level)?.Level is { } overridden) {
      return overridden;
    }
    return AbstractNumber(instance)?.Elements<Level>().FirstOrDefault(item => item.LevelIndex?.Value == level);
  }

  // A list's definition. One that only links to a numbering style uses the definition that style's list has
  private AbstractNum? AbstractNumber(NumberingInstance instance) {
    if (instance.AbstractNumId?.Val?.Value is not int id || !_abstractNumbers.TryGetValue(id, out AbstractNum? abstractNumber)) {
      return null;
    }
    if (!abstractNumber.Elements<Level>().Any() && abstractNumber.NumberingStyleLink?.Val?.Value is { } styleId
        && _styles.TryGetValue(styleId, out Style? style)
        && style.StyleParagraphProperties?.NumberingProperties?.NumberingId?.Val?.Value is int linked
        && _numbers.TryGetValue(linked, out NumberingInstance? linkedInstance)
        && linkedInstance.AbstractNumId?.Val?.Value is int linkedId && linkedId != id
        && _abstractNumbers.TryGetValue(linkedId, out AbstractNum? linkedNumber)) {
      return linkedNumber;
    }
    return abstractNumber;
  }

  // The item's bullet or number, such as "•", "3." or "2.1"
  private string? Marker(int number, int level, Level definition) {
    NumberingInstance instance = _numbers[number];
    AbstractNum? abstractNumber = AbstractNumber(instance);
    int key = abstractNumber?.AbstractNumberId?.Value ?? -number;
    if (!_counters.TryGetValue(key, out int?[]? counters)) {
      counters = new int?[9];
      _counters[key] = counters;
    }
    level = Math.Clamp(level, 0, 8);
    // A list can restart its numbering, such as a second numbered list that uses the same definition as the first
    if (_started.Add((number, level))
        && instance.Elements<LevelOverride>().FirstOrDefault(item => item.LevelIndex?.Value == level)?.StartOverrideNumberingValue?.Val?.Value is int restart) {
      counters[level] = restart - 1;
    }
    counters[level] = (counters[level] ?? Start(definition) - 1) + 1;
    for (int deeper = level + 1; deeper < counters.Length; deeper++) {
      counters[deeper] = null;
    }

    NumberFormatValues format = definition.NumberingFormat?.Val?.Value ?? NumberFormatValues.Decimal;
    string text = definition.LevelText?.Val?.Value ?? "";
    if (format == NumberFormatValues.None) {
      return null;
    }
    if (format == NumberFormatValues.Bullet) {
      string? font = definition.NumberingSymbolRunProperties?.RunFonts?.Ascii?.Value;
      return Bullet(text, font);
    }
    StringBuilder marker = new();
    for (int index = 0; index < text.Length; index++) {
      if (text[index] == '%' && index + 1 < text.Length && text[index + 1] is >= '1' and <= '9') {
        int referenced = text[index + 1] - '1';
        Level? referencedLevel = referenced == level ? definition : Level(number, referenced);
        int value = counters[referenced] ?? (referencedLevel is null ? 1 : Start(referencedLevel));
        NumberFormatValues referencedFormat = referencedLevel?.NumberingFormat?.Val?.Value ?? NumberFormatValues.Decimal;
        // A legal style list numbers every level in decimals, such as 2.1
        marker.Append(Format(value, definition.IsLegalNumberingStyle is not null && referenced != level ? NumberFormatValues.Decimal : referencedFormat));
        index++;
      } else {
        marker.Append(text[index]);
      }
    }
    string result = marker.ToString().Trim();
    return result.Length > 0 ? result : null;
  }

  private static int Start(Level level) =>
    level.StartNumberingValue?.Val?.Value ?? 0;

  private static string? Bullet(string text, string? font) {
    if (text.Length == 0) {
      return null;
    }
    if (Symbols.IsSymbolFont(font) || text[0] is >= '\uf000' and <= '\uf0ff') {
      return Symbols.Map(text[0], font ?? "Symbol");
    }
    // Word's second level of bullets is an "o" in Courier New
    return text == "o" ? "◦" : text;
  }

  internal static string Format(int value, NumberFormatValues format) {
    if (format == NumberFormatValues.LowerLetter) {
      return Letters(value).ToLowerInvariant();
    }
    if (format == NumberFormatValues.UpperLetter) {
      return Letters(value);
    }
    if (format == NumberFormatValues.LowerRoman) {
      return HtmlReader.Roman(value).ToLowerInvariant();
    }
    if (format == NumberFormatValues.UpperRoman) {
      return HtmlReader.Roman(value);
    }
    if (format == NumberFormatValues.DecimalZero) {
      return value.ToString("00", CultureInfo.InvariantCulture);
    }
    if (format == NumberFormatValues.Hebrew1) {
      return HebrewNumber(value);
    }
    if (format == NumberFormatValues.Hebrew2) {
      return value < 1 ? value.ToString(CultureInfo.InvariantCulture) : HebrewLetters[(value - 1) % HebrewLetters.Length].ToString();
    }
    return value.ToString(CultureInfo.InvariantCulture);
  }

  // Word's letters go a to z, then aa, bb and so on
  private static string Letters(int value) =>
    value < 1 ? value.ToString(CultureInfo.InvariantCulture) : new string((char)('A' + (value - 1) % 26), (value - 1) / 26 + 1);

  private const string HebrewLetters = "אבגדהוזחטיכלמנסעפצקרשת";

  // A number in Hebrew letters, as in gematria, with 15 and 16 written as 9+6 and 9+7, as is usual
  private static string HebrewNumber(int value) {
    if (value is < 1 or > 999) {
      return value.ToString(CultureInfo.InvariantCulture);
    }
    StringBuilder text = new();
    int hundreds = value / 100;
    while (hundreds >= 4) {
      text.Append('ת');
      hundreds -= 4;
    }
    if (hundreds > 0) {
      text.Append("קרש"[hundreds - 1]);
    }
    int rest = value % 100;
    if (rest is 15 or 16) {
      text.Append('ט').Append(rest == 15 ? 'ו' : 'ז');
      return text.ToString();
    }
    if (rest >= 10) {
      text.Append("יכלמנסעפצ"[rest / 10 - 1]);
    }
    if (rest % 10 > 0) {
      text.Append("אבגדהוזחט"[rest % 10 - 1]);
    }
    return text.ToString();
  }

  // ---------- Tables ----------

  private void ReadTable(Table table, List<DocBlock> blocks) {
    DocTable docTable = new();
    TableProperties? properties = table.GetFirstChild<TableProperties>();
    List<Style> styles = Chain(properties?.TableStyle?.Val?.Value);
    if (table.GetFirstChild<TableGrid>() is { } grid) {
      List<double> widths = [.. grid.Elements<GridColumn>().Select(column => Twips(column.Width?.Value))];
      if (widths.Count > 0 && widths.All(width => width > 0)) {
        docTable.ColumnWidths = widths;
      }
    }
    docTable.Borders = HasBorders(properties?.TableBorders) || styles.Any(style => HasBorders(style.StyleTableProperties?.TableBorders))
                       || table.Descendants<TableCellBorders>().Any(HasBorders);
    foreach (TableRow row in Rows(table)) {
      DocRow docRow = new();
      if (row.TableRowProperties?.GetFirstChild<GridBefore>()?.Val?.Value is int before && before > 0) {
        docRow.Cells.Add(new() { ColumnSpan = before });
      }
      foreach (TableCell cell in Cells(row)) {
        DocCell docCell = new() { ColumnSpan = Math.Max(1, cell.TableCellProperties?.GridSpan?.Val?.Value ?? 1) };
        // A cell merged with the one above it is left empty, so the text isn't there twice
        VerticalMerge? merge = cell.TableCellProperties?.VerticalMerge;
        if (merge is null || merge.Val?.Value == MergedCellValues.Restart) {
          _lastParagraph = null;
          Blocks(cell, docCell.Blocks, styles);
        }
        docRow.Cells.Add(docCell);
      }
      if (docRow.Cells.Count > 0) {
        docTable.Rows.Add(docRow);
      }
    }
    if (docTable.Rows.Count > 0) {
      blocks.Add(docTable);
    }
  }

  // The rows of a table, including those in content controls
  private static IEnumerable<TableRow> Rows(OpenXmlElement container) {
    foreach (OpenXmlElement element in container.ChildElements) {
      if (element is TableRow row) {
        yield return row;
      } else if (element.LocalName is "sdt" or "sdtContent" or "customXml") {
        foreach (TableRow inner in Rows(element)) {
          yield return inner;
        }
      }
    }
  }

  private static IEnumerable<TableCell> Cells(OpenXmlElement container) {
    foreach (OpenXmlElement element in container.ChildElements) {
      if (element is TableCell cell) {
        yield return cell;
      } else if (element.LocalName is "sdt" or "sdtContent" or "customXml") {
        foreach (TableCell inner in Cells(element)) {
          yield return inner;
        }
      }
    }
  }

  private static bool HasBorders(OpenXmlElement? borders) =>
    borders is not null && borders.ChildElements.OfType<BorderType>().Any(border => border.Val is { } value && value.Value != BorderValues.None
                                                                                      && value.Value != BorderValues.Nil);
}
