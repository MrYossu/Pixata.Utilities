using System.Reflection;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;
using AddedFont = UglyToad.PdfPig.Writer.PdfDocumentBuilder.AddedFont;

namespace Pixata.Pdf.Documents;

/// <summary>
/// Lays a document out on PDF pages: wraps paragraphs into lines, sizes tables and pictures, and starts a new page when one is
/// full. It's done in two steps. Measuring turns the blocks into items of known heights (a line of text, a gap, a picture, a table
/// row), and placing puts the items on pages, splitting a table row that doesn't fit between them
/// </summary>
internal sealed class DocumentLayout {
  private const double LineHeightFactor = 1.2;
  private const double AscentFactor = .93;
  private const double TabStop = 36;
  private const double CellPadding = 4;
  private const double ListIndent = 18;
  private const double MarkerGap = 6;
  private const double PixelsToPoints = .75;

  private readonly PdfDocumentBuilder _builder;
  private readonly FontSet _fonts;
  private readonly double _pageWidth;
  private readonly double _pageHeight;
  private readonly double _margin;
  private readonly double _fontSize;
  private PdfPageBuilder _page = null!;
  private double _y;
  private bool _pageEmpty;
  private readonly Dictionary<(AddedFont Font, double Size, string Text), double> _widths = [];
  private readonly Dictionary<(AddedFont Font, char Character), bool> _known = [];

  private DocumentLayout(PdfDocumentBuilder builder, DocumentToPdfOptions options) {
    _builder = builder;
    _fonts = FontSet.Add(builder, options);
    _pageWidth = options.PageWidthMm * 72 / 25.4;
    _pageHeight = options.PageHeightMm * 72 / 25.4;
    _margin = Math.Min(options.MarginMm * 72 / 25.4, Math.Min(_pageWidth, _pageHeight) / 4);
    _fontSize = options.FontSize > 0 ? options.FontSize : 11;
  }

  private double ContentWidth => _pageWidth - 2 * _margin;
  private double ContentHeight => _pageHeight - 2 * _margin;
  private double Bottom => _margin;

  /// <summary>
  /// Lays the blocks out, and returns the PDF
  /// </summary>
  public static byte[] Render(List<DocBlock> blocks, DocumentToPdfOptions options) {
    using PdfDocumentBuilder builder = new();
    DocumentLayout layout = new(builder, options);
    layout.NewPage();
    layout.Place(layout.Measure(blocks, layout.ContentWidth, layout.ContentHeight));
    return builder.Build();
  }

  private void NewPage() {
    _page = _builder.AddPage(_pageWidth, _pageHeight);
    _y = _pageHeight - _margin;
    _pageEmpty = true;
  }

  // ---------- Placing ----------

  private void Place(List<Item> items) {
    for (int index = 0; index < items.Count; index++) {
      Item item = items[index];
      if (item is PageBreak) {
        NewPage();
        continue;
      }
      if (item is Gap gap) {
        // Space at the top of a page is left out, as a word processor does
        if (!_pageEmpty) {
          _y -= gap.Height;
        }
        continue;
      }
      double needed = item.Height;
      if (item.KeepWithNext && items.Skip(index + 1).FirstOrDefault(next => next is not Gap) is { } next and not PageBreak) {
        needed += items.Skip(index + 1).TakeWhile(following => following is Gap).Sum(following => following.Height) + MinimumHeight(next);
      }
      if (_y - needed < Bottom && !_pageEmpty) {
        // A table row is split across the pages if some of each of its cells fits. Anything else starts a new page
        if (item is Row row && _y - Bottom >= row.MinimumHeight) {
          (Row first, Row? rest) = row.Split(_y - Bottom, false);
          Draw(first);
          NewPage();
          if (rest is not null) {
            items[index] = rest;
            index--;
          }
          continue;
        }
        NewPage();
      }
      // A row taller than a whole page is split however it can be
      if (item is Row tall && _y - tall.Height < Bottom) {
        (Row first, Row? rest) = tall.Split(_y - Bottom, true);
        Draw(first);
        if (rest is not null) {
          NewPage();
          items[index] = rest;
          index--;
        }
        continue;
      }
      Draw(item);
    }
  }

  private void Draw(Item item) {
    item.Draw(this, _margin, _y);
    _y -= item.Height;
    _pageEmpty = false;
  }

  private static double MinimumHeight(Item item) =>
    item is Row row ? row.MinimumHeight : item.Height;

  // ---------- Measuring ----------

  private List<Item> Measure(List<DocBlock> blocks, double width, double maxHeight) {
    List<Item> items = [];
    foreach (DocBlock block in blocks) {
      switch (block) {
        case DocParagraph paragraph:
          MeasureParagraph(paragraph, width, items);
          break;
        case DocTable table:
          MeasureTable(table, width, maxHeight, items);
          break;
        case DocImage image:
          MeasureImage(image, width, maxHeight, items);
          break;
        case DocRule:
          AddGap(items, _fontSize * .5);
          items.Add(new Rule(width));
          AddGap(items, _fontSize * .5);
          break;
        case DocPageBreak:
          items.Add(new PageBreak());
          break;
      }
    }
    return items;
  }

  // Space between blocks collapses, as margins do in HTML, so a paragraph's space after and the next one's space before
  // aren't both added
  private static void AddGap(List<Item> items, double height) {
    if (height <= 0) {
      return;
    }
    if (items.Count > 0 && items[^1] is Gap last) {
      items[^1] = new Gap(Math.Max(last.Height, height));
      return;
    }
    items.Add(new Gap(height));
  }

  private void MeasureImage(DocImage image, double width, double maxHeight, List<Item> items) {
    double naturalWidth = image.Width ?? (image.Height is { } givenHeight ? givenHeight * image.PixelWidth / image.PixelHeight : image.PixelWidth * PixelsToPoints);
    double naturalHeight = image.Height ?? naturalWidth * image.PixelHeight / image.PixelWidth;
    double scale = Math.Min(1, Math.Min(width / naturalWidth, maxHeight / naturalHeight));
    double pictureWidth = naturalWidth * scale;
    double pictureHeight = naturalHeight * scale;
    double x = image.Alignment switch {
      DocAlignment.Centre => (width - pictureWidth) / 2,
      DocAlignment.Right => width - pictureWidth,
      _ => 0
    };
    items.Add(new Picture(image, x, pictureWidth, pictureHeight));
    AddGap(items, _fontSize * .4);
  }

  private void MeasureTable(DocTable table, double width, double maxHeight, List<Item> items) {
    int columns = table.Rows.Count == 0 ? 0 : table.Rows.Max(row => row.Cells.Sum(cell => Math.Max(1, cell.ColumnSpan)));
    if (columns == 0) {
      return;
    }
    List<double> widths;
    if (table.ColumnWidths is { } given && given.Count >= columns && given.Take(columns).All(w => w > 0)) {
      widths = [.. given.Take(columns)];
      double total = widths.Sum();
      if (total > width || table.RelativeWidths) {
        widths = [.. widths.Select(w => w * width / total)];
      }
    } else {
      widths = [.. Enumerable.Repeat(width / columns, columns)];
    }
    AddGap(items, _fontSize * .3);
    foreach (DocRow docRow in table.Rows) {
      List<Cell> cells = [];
      int column = 0;
      foreach (DocCell docCell in docRow.Cells) {
        if (column >= columns) {
          break;
        }
        int span = Math.Min(Math.Max(1, docCell.ColumnSpan), columns - column);
        double x = widths.Take(column).Sum();
        double cellWidth = widths.Skip(column).Take(span).Sum();
        List<Item> content = Measure(docCell.Blocks, Math.Max(1, cellWidth - 2 * CellPadding), maxHeight - 2 * CellPadding);
        // Space before the first paragraph in a cell, and after the last, is the cell's padding instead
        while (content.Count > 0 && content[0] is Gap) {
          content.RemoveAt(0);
        }
        while (content.Count > 0 && content[^1] is Gap) {
          content.RemoveAt(content.Count - 1);
        }
        cells.Add(new(x, cellWidth, content));
        column += span;
      }
      items.Add(new Row(cells, table.Borders));
    }
    AddGap(items, _fontSize * .6);
  }

  private void MeasureParagraph(DocParagraph paragraph, double width, List<Item> items) {
    double size = paragraph.FontSize ?? _fontSize;
    bool heading = paragraph.HeadingLevel > 0;
    AddGap(items, paragraph.SpaceBefore ?? (heading ? size * .7 : 0));
    bool rightToLeft = paragraph.RightToLeft ?? RightToLeft(paragraph);
    double indent = Math.Max(0, paragraph.Indent);
    string? marker = paragraph.ListMarker is { Length: > 0 } listMarker ? listMarker : null;
    if (marker is not null && indent < ListIndent) {
      indent = ListIndent;
    }
    indent = Math.Min(indent, width * .6);
    double firstIndent = Math.Clamp(paragraph.FirstLineIndent, -indent, width * .5);
    List<List<Piece>> lines = Wrap(Tokens(paragraph, size, rightToLeft), width - indent, firstIndent, out List<bool> explicitEnds);
    double lineSpacing = paragraph.LineSpacing > 0 ? paragraph.LineSpacing : 1;
    DocAlignment alignment = paragraph.Alignment == DocAlignment.Left && rightToLeft ? DocAlignment.Right : paragraph.Alignment;
    for (int index = 0; index < lines.Count; index++) {
      List<Piece> pieces = lines[index];
      double lineStart = indent + (index == 0 ? firstIndent : 0);
      double available = width - lineStart;
      if (rightToLeft || pieces.Any(piece => piece.RightToLeft)) {
        pieces = Reorder(pieces, rightToLeft);
      }
      double lineWidth = pieces.Count == 0 ? 0 : pieces[^1].X + pieces[^1].Width;
      double largest = pieces.Count == 0 ? size : pieces.Max(piece => piece.Size);
      double offset = alignment switch {
        DocAlignment.Centre => (available - lineWidth) / 2,
        DocAlignment.Right => available - lineWidth,
        _ => 0
      };
      List<Piece> placed = [];
      if (alignment == DocAlignment.Justify && index < lines.Count - 1 && !explicitEnds[index]) {
        int gaps = pieces.Count(piece => piece.SpaceBefore);
        double extra = gaps == 0 ? 0 : (available - lineWidth) / gaps;
        int gap = 0;
        foreach (Piece piece in pieces) {
          if (piece.SpaceBefore) {
            gap++;
          }
          placed.Add(piece with { X = lineStart + piece.X + gap * extra });
        }
      } else {
        placed.AddRange(pieces.Select(piece => piece with { X = lineStart + offset + Math.Max(0, piece.X) }));
      }
      if (index == 0 && marker is not null) {
        DocStyle markerStyle = paragraph.ListMarkerStyle;
        AddedFont font = _fonts.Get(markerStyle);
        double markerSize = markerStyle.Size ?? (placed.Count > 0 ? placed[0].Size : size);
        string text = Printable(marker, font);
        double markerWidth = Width(text, markerSize, font);
        double markerX = rightToLeft ? width - indent + MarkerGap : Math.Max(0, indent - MarkerGap - markerWidth);
        placed.Add(new(text, font, markerSize, markerX, markerWidth, markerStyle, false, false));
      }
      // Extra line spacing is shared above and below the text
      double lineHeight = largest * LineHeightFactor * lineSpacing;
      Line line = new(placed, largest * AscentFactor + (lineHeight - largest * LineHeightFactor) / 2, lineHeight) {
        KeepWithNext = heading && index == lines.Count - 1
      };
      items.Add(line);
    }
    AddGap(items, paragraph.SpaceAfter ?? (heading ? size * .3 : marker is not null ? size * .15 : size * .5));
  }

  private static bool RightToLeft(DocParagraph paragraph) {
    foreach (DocText text in paragraph.Inlines.OfType<DocText>()) {
      foreach (char c in text.Text) {
        if (Bidi.IsRightToLeft(c)) {
          return true;
        }
        if (Bidi.IsLeftToRight(c)) {
          return false;
        }
      }
    }
    return false;
  }

  // ---------- Words and lines ----------

  private enum TokenKind {
    Word,
    Space,
    Tab,
    Break
  }

  private sealed record Token(TokenKind Kind, string Text, DocStyle Style, double Size, AddedFont Font, bool RightToLeft);

  private List<Token> Tokens(DocParagraph paragraph, double size, bool rightToLeftParagraph) {
    List<Token> tokens = [];
    foreach (DocInline inline in paragraph.Inlines) {
      switch (inline) {
        case DocLineBreak:
          tokens.Add(new(TokenKind.Break, "", default, size, _fonts.Regular, false));
          break;
        case DocTab tab:
          tokens.Add(new(TokenKind.Tab, "", tab.Style, tab.Style.Size ?? size, _fonts.Get(tab.Style), false));
          break;
        case DocText text:
          AddedFont font = _fonts.Get(text.Style);
          double textSize = text.Style.Size ?? size;
          StringBuilder current = new();
          bool? space = null;
          void Flush() {
            if (current.Length == 0) {
              return;
            }
            string value = current.ToString();
            if (space == true) {
              tokens.Add(new(TokenKind.Space, value, text.Style, textSize, font, false));
            } else {
              string printable = Printable(value, font);
              bool rightToLeft = printable.Any(Bidi.IsRightToLeft);
              tokens.Add(new(TokenKind.Word, rightToLeft || rightToLeftParagraph ? Bidi.Visual(printable) : printable, text.Style, textSize, font, rightToLeft));
            }
            current.Clear();
          }
          foreach (char c in text.Text) {
            bool isSpace = c is ' ' or '\u2002' or '\u2003' or '\u2009' or '\r' or '\n';
            if (space is not null && space != isSpace) {
              Flush();
            }
            space = isSpace;
            current.Append(isSpace ? ' ' : c);
          }
          Flush();
          break;
      }
    }
    return tokens;
  }

  // Greedy line breaking: as many words as fit on each line, breaking a word that's too long for a line on its own
  private List<List<Piece>> Wrap(List<Token> tokens, double width, double firstIndent, out List<bool> explicitEnds) {
    List<List<Piece>> lines = [[]];
    explicitEnds = [];
    double x = 0;
    double pendingSpace = 0;
    bool first = true;
    double Available() => Math.Max(1, width - (first ? firstIndent : 0));
    void NewLine(bool isExplicit, List<bool> ends) {
      ends.Add(isExplicit);
      lines.Add([]);
      x = 0;
      pendingSpace = 0;
      first = false;
    }
    foreach (Token token in tokens) {
      List<Piece> line = lines[^1];
      switch (token.Kind) {
        case TokenKind.Break:
          NewLine(true, explicitEnds);
          break;
        case TokenKind.Space:
          // Spaces at the start of a wrapped line are dropped, but those at the start of the paragraph or after a line break count
          if (line.Count == 0 && !first && explicitEnds.Count > 0 && !explicitEnds[^1]) {
            break;
          }
          pendingSpace += Width(token.Text, token.Size, token.Font);
          break;
        case TokenKind.Tab:
          double stop = (Math.Floor((x + pendingSpace) / TabStop) + 1) * TabStop;
          if (stop > Available() && line.Count > 0) {
            NewLine(false, explicitEnds);
            stop = TabStop;
          }
          x = stop;
          pendingSpace = 0;
          break;
        case TokenKind.Word:
          double wordWidth = Width(token.Text, token.Size, token.Font);
          if (x + pendingSpace + wordWidth > Available() && line.Count > 0) {
            NewLine(false, explicitEnds);
            line = lines[^1];
          }
          string text = token.Text;
          // A word longer than a line is broken where it has to be
          while (wordWidth > Available() && text.Length > 1) {
            int fits = text.Length - 1;
            while (fits > 1 && Width(text[..fits], token.Size, token.Font) > Available() - x) {
              fits--;
            }
            string part = text[..fits];
            line.Add(new(part, token.Font, token.Size, x, Width(part, token.Size, token.Font), token.Style, false, token.RightToLeft));
            NewLine(false, explicitEnds);
            line = lines[^1];
            text = text[fits..];
            wordWidth = Width(text, token.Size, token.Font);
          }
          bool spaceBefore = pendingSpace > 0 && line.Count > 0;
          double start = x + pendingSpace;
          line.Add(new(text, token.Font, token.Size, start, wordWidth, token.Style, spaceBefore, token.RightToLeft));
          x = start + wordWidth;
          pendingSpace = 0;
          break;
      }
    }
    explicitEnds.Add(true);
    return lines;
  }

  // Puts a line's words in the order they're seen. In a right-to-left paragraph the line reads from the right, so the order of
  // its words is reversed, apart from runs of left-to-right words (such as an English name), which keep their order. In a
  // left-to-right paragraph, only runs of right-to-left words are reversed. Each word's letters are already in order
  private static List<Piece> Reorder(List<Piece> pieces, bool rightToLeft) {
    if (pieces.Count == 0) {
      return pieces;
    }
    // The space before each word, which stays between the same two words when they swap round
    double[] gaps = new double[pieces.Count];
    for (int index = 0; index < pieces.Count; index++) {
      gaps[index] = index == 0 ? pieces[0].X : pieces[index].X - (pieces[index - 1].X + pieces[index - 1].Width);
    }
    // Runs of words going the same way. A word with no letters (a number or punctuation) goes the way of the run it's in
    List<(List<int> Indices, bool RightToLeft)> runs = [];
    for (int index = 0; index < pieces.Count; index++) {
      bool? runDirection = runs.Count == 0 ? null : runs[^1].RightToLeft;
      bool direction = pieces[index].RightToLeft || (!pieces[index].Text.Any(Bidi.IsLeftToRight) && (runDirection ?? rightToLeft));
      if (runs.Count == 0 || direction != runDirection) {
        runs.Add(([], direction));
      }
      runs[^1].Indices.Add(index);
    }
    foreach ((List<int> indices, bool runIsRightToLeft) in runs) {
      if (runIsRightToLeft) {
        indices.Reverse();
      }
    }
    if (rightToLeft) {
      runs.Reverse();
    }
    List<int> order = [.. runs.SelectMany(run => run.Indices)];
    List<Piece> result = [];
    double x = gaps[0];
    for (int position = 0; position < order.Count; position++) {
      int index = order[position];
      double gap = position == 0 ? 0 : gaps[Math.Max(index, order[position - 1])];
      x += gap;
      result.Add(pieces[index] with { X = x, SpaceBefore = gap > 0 });
      x += pieces[index].Width;
    }
    return result;
  }

  // ---------- Fonts and text ----------

  private double Width(string text, double size, AddedFont font) {
    if (text.Length == 0) {
      return 0;
    }
    if (_widths.TryGetValue((font, size, text), out double width)) {
      return width;
    }
    IReadOnlyList<UglyToad.PdfPig.Content.Letter> letters = _page.MeasureText(text, size, new(0, 0), font);
    width = letters.Count == 0 ? 0 : letters[^1].EndBaseLine.X - letters[0].StartBaseLine.X;
    _widths[(font, size, text)] = width;
    return width;
  }

  // The text as it can be drawn, with any character the font hasn't got as a question mark
  private string Printable(string text, AddedFont font) {
    StringBuilder printable = new(text.Length);
    foreach (char c in text) {
      if (char.IsControl(c) || c is '\u200b' or '\u200e' or '\u200f' or '\ufeff' or '\u00ad') {
        continue;
      }
      if (!_known.TryGetValue((font, c), out bool known)) {
        try {
          known = _page.MeasureText(c.ToString(), 10, new(0, 0), font).Count > 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException or KeyNotFoundException) {
          known = false;
        }
        _known[(font, c)] = known;
      }
      printable.Append(known ? c : '?');
    }
    return printable.ToString();
  }

  // ---------- Items ----------

  private abstract class Item {
    public abstract double Height { get; }

    /// <summary>
    /// Whether this goes on the same page as the item after it, as a heading does
    /// </summary>
    public bool KeepWithNext { get; init; }

    public abstract void Draw(DocumentLayout layout, double x, double top);
  }

  private sealed class Gap(double height) : Item {
    public override double Height { get; } = height;

    public override void Draw(DocumentLayout layout, double x, double top) {
    }
  }

  private sealed class PageBreak : Item {
    public override double Height => 0;

    public override void Draw(DocumentLayout layout, double x, double top) {
    }
  }

  private sealed record Piece(string Text, AddedFont Font, double Size, double X, double Width, DocStyle Style, bool SpaceBefore, bool RightToLeft);

  private sealed class Line(List<Piece> pieces, double ascent, double height) : Item {
    public override double Height { get; } = height;

    public override void Draw(DocumentLayout layout, double x, double top) {
      // The baseline is the same for every piece, so text of different sizes lines up
      double baseline = top - ascent;
      foreach (Piece piece in pieces) {
        if (piece.Style.Colour is (byte red, byte green, byte blue)) {
          layout._page.SetTextAndFillColor(red, green, blue);
          layout._page.SetStrokeColor(red, green, blue);
        }
        layout._page.AddText(piece.Text, piece.Size, new(x + piece.X, baseline), piece.Font);
        if (piece.Style.Underline) {
          double y = baseline - piece.Size * .12;
          layout._page.DrawLine(new(x + piece.X, y), new(x + piece.X + piece.Width, y), piece.Size * .06);
        }
        if (piece.Style.Strike) {
          double y = baseline + piece.Size * .28;
          layout._page.DrawLine(new(x + piece.X, y), new(x + piece.X + piece.Width, y), piece.Size * .06);
        }
        if (piece.Style.Colour is not null) {
          layout._page.ResetColor();
        }
      }
    }
  }

  private sealed class Picture(DocImage image, double offset, double width, double height) : Item {
    public override double Height { get; } = height;

    public override void Draw(DocumentLayout layout, double x, double top) {
      PdfRectangle area = new(x + offset, top - Height, x + offset + width, top);
      if (image.Format == ImageFormat.Png) {
        layout._page.AddPng(image.Data, area);
      } else {
        layout._page.AddJpeg(image.Data, area);
      }
    }
  }

  private sealed class Rule(double width) : Item {
    public override double Height => 1;

    public override void Draw(DocumentLayout layout, double x, double top) {
      layout._page.SetStrokeColor(160, 160, 160);
      layout._page.DrawLine(new(x, top - .5), new(x + width, top - .5), .75);
      layout._page.ResetColor();
    }
  }

  private sealed record Cell(double X, double Width, List<Item> Items);

  // A table row. Its cells are as tall as its tallest, so their borders line up
  private sealed class Row(List<Cell> cells, bool borders) : Item {
    public List<Cell> Cells { get; } = cells;

    public override double Height { get; } = cells.Count == 0 ? 0 : cells.Max(cell => cell.Items.Sum(item => item.Height)) + 2 * CellPadding;

    /// <summary>
    /// The least that has to fit for some of each cell to go on a page
    /// </summary>
    public double MinimumHeight =>
      Cells.Count == 0 ? 0 : Cells.Max(cell => cell.Items.Count == 0 ? 0 : MinimumHeight(cell.Items[0])) + 2 * CellPadding;

    /// <summary>
    /// The part of the row that fits in the height given, and the rest, which goes on the next page. With force, each cell's first
    /// item goes in the first part even if it doesn't fit, so a row taller than a page still gets drawn
    /// </summary>
    public (Row First, Row? Remainder) Split(double available, bool force) {
      double space = available - 2 * CellPadding;
      List<Cell> first = [];
      List<Cell> rest = [];
      foreach (Cell cell in Cells) {
        List<Item> taken = [];
        List<Item> left = [];
        double used = 0;
        foreach (Item item in cell.Items) {
          if (left.Count == 0 && used + item.Height <= space) {
            taken.Add(item);
            used += item.Height;
          } else if (left.Count == 0 && item is Row nested && space - used >= nested.MinimumHeight) {
            (Row nestedFirst, Row? nestedRest) = nested.Split(space - used, false);
            taken.Add(nestedFirst);
            used += nestedFirst.Height;
            if (nestedRest is not null) {
              left.Add(nestedRest);
            }
          } else if (left.Count == 0 && taken.Count == 0 && force) {
            taken.Add(item);
            used += item.Height;
          } else if (left.Count > 0 || item is not Gap) {
            left.Add(item);
          }
        }
        first.Add(cell with { Items = taken });
        rest.Add(cell with { Items = left });
      }
      return (new Row(first, borders), rest.Any(cell => cell.Items.Count > 0) ? new Row(rest, borders) : null);
    }

    public override void Draw(DocumentLayout layout, double x, double top) {
      foreach (Cell cell in Cells) {
        double y = top - CellPadding;
        foreach (Item item in cell.Items) {
          item.Draw(layout, x + cell.X + CellPadding, y);
          y -= item.Height;
        }
        if (borders) {
          layout._page.SetStrokeColor(96, 96, 96);
          layout._page.DrawRectangle(new(x + cell.X, top - Height), cell.Width, Height, .5, false);
          layout._page.ResetColor();
        }
      }
    }
  }

  // ---------- Fonts ----------

  private sealed record FontSet(AddedFont Regular, AddedFont Bold, AddedFont Italic, AddedFont BoldItalic) {
    public AddedFont Get(DocStyle style) =>
      style switch {
        { Bold: true, Italic: true } => BoldItalic,
        { Bold: true } => Bold,
        { Italic: true } => Italic,
        _ => Regular
      };

    public static FontSet Add(PdfDocumentBuilder builder, DocumentToPdfOptions options) {
      if (options.Font is { } font) {
        AddedFont regular = builder.AddTrueTypeFont(font);
        return new(regular,
                   options.BoldFont is { } bold ? builder.AddTrueTypeFont(bold) : regular,
                   options.ItalicFont is { } italic ? builder.AddTrueTypeFont(italic) : regular,
                   options.BoldItalicFont is { } boldItalic ? builder.AddTrueTypeFont(boldItalic) : options.BoldFont is { } boldOnly ? builder.AddTrueTypeFont(boldOnly) : regular);
      }
      return new(builder.AddTrueTypeFont(Liberation.Value.Regular), builder.AddTrueTypeFont(Liberation.Value.Bold),
                 builder.AddTrueTypeFont(Liberation.Value.Italic), builder.AddTrueTypeFont(Liberation.Value.BoldItalic));
    }

    private static readonly Lazy<(byte[] Regular, byte[] Bold, byte[] Italic, byte[] BoldItalic)> Liberation = new(() =>
      (Resource("Regular"), Resource("Bold"), Resource("Italic"), Resource("BoldItalic")));

    private static byte[] Resource(string face) {
      using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Pixata.Pdf.Documents.Fonts.LiberationSans-{face}.ttf")
                            ?? throw new InvalidOperationException($"The LiberationSans-{face} font is missing from Pixata.Pdf");
      using MemoryStream copy = new();
      stream.CopyTo(copy);
      return copy.ToArray();
    }
  }
}
