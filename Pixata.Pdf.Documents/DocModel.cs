namespace Pixata.Pdf.Documents;

// What a document is read into, whatever its format, before it's laid out on PDF pages. It only has what the PDF will show:
// paragraphs of styled text, lists, tables, pictures and page breaks

internal abstract class DocBlock;

internal enum DocAlignment {
  Left,
  Centre,
  Right,
  Justify
}

/// <summary>
/// How a piece of text looks. A null size means the paragraph's
/// </summary>
internal readonly record struct DocStyle(bool Bold = false, bool Italic = false, bool Underline = false, bool Strike = false, double? Size = null,
                                         (byte Red, byte Green, byte Blue)? Colour = null);

internal abstract class DocInline;

internal sealed class DocText(string text, DocStyle style) : DocInline {
  public string Text { get; } = text;
  public DocStyle Style { get; } = style;
}

internal sealed class DocLineBreak : DocInline;

internal sealed class DocTab(DocStyle style) : DocInline {
  public DocStyle Style { get; } = style;
}

internal sealed class DocParagraph : DocBlock {
  public List<DocInline> Inlines { get; } = [];

  /// <summary>
  /// 0 for an ordinary paragraph, 1 to 6 for a heading. Only used for the sizes of headings whose format doesn't give one, and to
  /// keep a heading with what follows it
  /// </summary>
  public int HeadingLevel { get; set; }

  public DocAlignment Alignment { get; set; }

  /// <summary>
  /// Whether the paragraph reads from the right, such as Hebrew. Null works it out from the text
  /// </summary>
  public bool? RightToLeft { get; set; }

  /// <summary>
  /// The left indent, and the first line's, in points. A list item's marker goes in the first line's indent
  /// </summary>
  public double Indent { get; set; }
  public double FirstLineIndent { get; set; }

  /// <summary>
  /// A list item's bullet or number, such as "•" or "2."
  /// </summary>
  public string? ListMarker { get; set; }
  public DocStyle ListMarkerStyle { get; set; }

  /// <summary>
  /// The size of text that doesn't give its own, in points. Null for the document's default (or the heading's)
  /// </summary>
  public double? FontSize { get; set; }

  /// <summary>
  /// The space before and after the paragraph, in points. Null for the defaults
  /// </summary>
  public double? SpaceBefore { get; set; }
  public double? SpaceAfter { get; set; }

  /// <summary>
  /// The line height as a multiple of the usual (1.2 times the font size)
  /// </summary>
  public double LineSpacing { get; set; } = 1;

  public bool IsEmpty => Inlines.All(inline => inline is DocText { Text.Length: 0 });
}

internal sealed class DocTable : DocBlock {
  public List<DocRow> Rows { get; } = [];

  /// <summary>
  /// The columns' widths in points, if the document gives them. They're made narrower if they don't fit the page
  /// </summary>
  public List<double>? ColumnWidths { get; set; }

  /// <summary>
  /// Whether <see cref="ColumnWidths"/> are only in proportion to each other, such as percentages, so the table fills the space
  /// </summary>
  public bool RelativeWidths { get; set; }

  public bool Borders { get; set; }
}

internal sealed class DocRow {
  public List<DocCell> Cells { get; } = [];
}

internal sealed class DocCell {
  public List<DocBlock> Blocks { get; } = [];
  public int ColumnSpan { get; set; } = 1;
}

internal sealed class DocImage(byte[] data, ImageFormat format, int pixelWidth, int pixelHeight) : DocBlock {
  public byte[] Data { get; } = data;
  public ImageFormat Format { get; } = format;
  public int PixelWidth { get; } = pixelWidth;
  public int PixelHeight { get; } = pixelHeight;

  /// <summary>
  /// The size the document gives it, in points. Null for its size at 96 dots per inch
  /// </summary>
  public double? Width { get; set; }
  public double? Height { get; set; }

  public DocAlignment Alignment { get; set; }
}

internal sealed class DocRule : DocBlock;

internal sealed class DocPageBreak : DocBlock;
