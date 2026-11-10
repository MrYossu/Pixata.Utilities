using System.Globalization;
using System.IO.Compression;
using System.Text;
using Pixata.Extensions;
using Pixata.Extensions.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Graphics.Operations.General;
using UglyToad.PdfPig.Graphics.Operations.PathConstruction;
using UglyToad.PdfPig.Graphics.Operations.PathPainting;
using UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState;
using UglyToad.PdfPig.Writer;

namespace Pixata.Pdf;

/// <summary>
/// How <see cref="PdfMarkupFlattener"/> draws the mark-up
/// </summary>
public class PdfMarkupFlattenOptions {
  /// <summary>
  /// Adds pages at the end listing the notes, with who added each and when. Each note is shown on its page as a numbered
  /// circle, and listed with that number. Defaults to true. When it's false, the notes are left out altogether
  /// </summary>
  public bool NotesPage { get; set; } = true;

  /// <summary>
  /// The heading of the notes pages
  /// </summary>
  public string NotesTitle { get; set; } = "Notes";

  /// <summary>
  /// How a note's page is shown on the notes pages. {0} is the page number
  /// </summary>
  public string NotePageFormat { get; set; } = "Page {0}";

  /// <summary>
  /// The time zone the notes' dates are shown in. Defaults to the server's
  /// </summary>
  public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

  /// <summary>
  /// How the notes' dates are shown, using the current culture
  /// </summary>
  public string DateFormat { get; set; } = "d MMM yyyy HH:mm";

  /// <summary>
  /// How much a highlight hides the page under it, from 0 (not at all) to 1 (completely). Defaults to 0.4
  /// </summary>
  public double HighlightOpacity { get; set; } = 0.4;

  /// <summary>
  /// A TrueType font (the contents of a .ttf file) for text and notes. Without one, Helvetica is used, which only has Western
  /// European letters, so any others (such as Hebrew, Greek or Chinese) are shown as question marks. The font is embedded in
  /// the PDF, so check that its licence allows that
  /// </summary>
  public byte[]? Font { get; set; }
}

/// <summary>
/// Draws mark-up (notes, highlights, strike-through, drawing and text, as added with <c>PixataPdfViewer</c>) into a copy of a
/// PDF, so it can be archived or sent on with the mark-up in it. The original PDF isn't changed
/// </summary>
public static class PdfMarkupFlattener {
  private const double LineHeight = 1.2;
  private const double NoteDiameter = 14;

  /// <summary>
  /// Draws the mark-up into a copy of the PDF
  /// </summary>
  /// <param name="pdf">The PDF's contents</param>
  /// <param name="markup">The mark-up. Items on pages the PDF doesn't have are left out</param>
  /// <param name="options">How it's drawn</param>
  /// <returns>The PDF with the mark-up drawn in, or a <see cref="ApiResponseStates.Failure"/> that says why it couldn't be made,
  /// such as the PDF being damaged, or protected with a password</returns>
  public static ApiResponse<byte[]> Flatten(byte[] pdf, IEnumerable<PdfMarkupItem> markup, PdfMarkupFlattenOptions? options = null) {
    options ??= new();
    List<PdfMarkupItem> items = [.. markup];
    try {
      using PdfDocument document = PdfDocument.Open(pdf);
      using PdfDocumentBuilder builder = new();
      Fonts fonts = options.Font is { } font
        ? new(builder.AddTrueTypeFont(font), builder.AddTrueTypeFont(font), false)
        : new(builder.AddStandard14Font(Standard14Font.Helvetica), builder.AddStandard14Font(Standard14Font.HelveticaBold), true);
      Dictionary<string, PdfPageBuilder.AddedImage> highlights = [];
      List<(int Number, PdfMarkupItem Item)> notes = [];
      ILookup<int, PdfMarkupItem> pages = items.ToLookup(item => item.Page);
      for (int number = 1; number <= document.NumberOfPages; number++) {
        PdfPageBuilder page = builder.AddPage(document, number);
        int rotation = document.GetPage(number).Rotation.Value;
        // Highlights go first, so the other mark-up is drawn over them
        foreach (PdfMarkupItem item in pages[number].OrderBy(item => item.Type == PdfMarkupType.Highlight ? 0 : 1)) {
          switch (item.Type) {
            case PdfMarkupType.Highlight:
              DrawHighlight(page, item, highlights, options.HighlightOpacity);
              break;
            case PdfMarkupType.StrikeThrough:
              DrawStrikeThrough(page, item, rotation);
              break;
            case PdfMarkupType.Ink:
              DrawInk(page, item);
              break;
            case PdfMarkupType.Text:
              DrawText(page, item, rotation, fonts);
              break;
            case PdfMarkupType.Note when options.NotesPage && item.Position is not null:
              notes.Add((notes.Count + 1, item));
              DrawNoteMarker(page, item, notes.Count, rotation, fonts);
              break;
          }
        }
      }
      if (notes.Count > 0) {
        AddNotesPages(builder, notes, options, fonts);
      }
      return new(ApiResponseStates.Success, builder.Build());
    }
    catch (Exception ex) when (ex is not OutOfMemoryException) {
      return new(ApiResponseStates.Failure, Message: $"The mark-up couldn't be drawn into the PDF: {ex.Message}");
    }
  }

  /// <summary>
  /// Draws the mark-up into a copy of the PDF. The stream is read from its current position to the end, and isn't closed
  /// </summary>
  /// <param name="pdf">A stream with the PDF's contents</param>
  /// <param name="markup">The mark-up. Items on pages the PDF doesn't have are left out</param>
  /// <param name="options">How it's drawn</param>
  /// <param name="cancellationToken">Cancels reading the stream</param>
  /// <returns>The PDF with the mark-up drawn in, or a <see cref="ApiResponseStates.Failure"/> that says why it couldn't be made</returns>
  public static async Task<ApiResponse<byte[]>> Flatten(Stream pdf, IEnumerable<PdfMarkupItem> markup, PdfMarkupFlattenOptions? options = null,
    CancellationToken cancellationToken = default) {
    using MemoryStream copy = new();
    await pdf.CopyToAsync(copy, cancellationToken);
    return Flatten(copy.ToArray(), markup, options);
  }

  // The fonts, whether they're PdfPig's built-in Helvetica, and whether they can show each character they've been asked to
  internal sealed record Fonts(PdfDocumentBuilder.AddedFont Regular, PdfDocumentBuilder.AddedFont Bold, bool BuiltIn) {
    public Dictionary<(PdfDocumentBuilder.AddedFont Font, char Character), bool> Known { get; } = [];
  }

  // ---------- Each kind of mark-up ----------

  // A highlight is a translucent image, as PdfPig can't set the opacity of what it draws, and a PNG's transparency is kept
  private static void DrawHighlight(PdfPageBuilder page, PdfMarkupItem item, Dictionary<string, PdfPageBuilder.AddedImage> images, double opacity) {
    foreach (PdfMarkupRect rect in item.Rects.Where(r => r.Width > 0 && r.Height > 0)) {
      PdfRectangle area = new(rect.Left, rect.Bottom, rect.Right, rect.Top);
      if (images.TryGetValue(item.Colour, out PdfPageBuilder.AddedImage? image)) {
        page.AddImage(image, area);
      } else {
        (byte red, byte green, byte blue) = Colour(item.Colour);
        images[item.Colour] = page.AddPng(Png(red, green, blue, (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255)), area);
      }
    }
  }

  // The line goes through the middle of each rectangle, along its longer side, which is the way the text runs. For a
  // rectangle that's nearly square, such as one letter, it goes the way text runs as the page is normally shown
  private static void DrawStrikeThrough(PdfPageBuilder page, PdfMarkupItem item, int rotation) {
    foreach (PdfMarkupRect rect in item.Rects.Where(r => r.Width > 0 && r.Height > 0)) {
      bool across = StrikesAcross(rect, rotation);
      double thickness = Math.Max(1, (across ? rect.Height : rect.Width) * .08);
      PdfMarkupPoint[] line = across
        ? [new(rect.Left, (rect.Bottom + rect.Top) / 2), new(rect.Right, (rect.Bottom + rect.Top) / 2)]
        : [new((rect.Left + rect.Right) / 2, rect.Bottom), new((rect.Left + rect.Right) / 2, rect.Top)];
      Stroke(page, [line], item.Colour, thickness, round: false);
    }
  }

  internal static bool StrikesAcross(PdfMarkupRect rect, int rotation) =>
    rect.Width > rect.Height * 1.2 || (rect.Height <= rect.Width * 1.2 && rotation % 180 == 0);

  private static void DrawInk(PdfPageBuilder page, PdfMarkupItem item) =>
    Stroke(page, item.Strokes, item.Colour, item.LineWidth, round: true);

  private static void DrawText(PdfPageBuilder page, PdfMarkupItem item, int rotation, Fonts fonts) {
    if (item.Position is not PdfMarkupPoint position || string.IsNullOrWhiteSpace(item.Text)) {
      return;
    }
    double size = item.FontSize > 0 ? item.FontSize : 12;
    string[] lines = item.Text.ReplaceLineEndings("\n").Split('\n');
    List<IGraphicsStateOperation> operations = page.CurrentStream.Operations;
    operations.Add(Push.Value);
    operations.Add(Upright(position, rotation));
    (byte red, byte green, byte blue) = Colour(item.Colour);
    page.SetTextAndFillColor(red, green, blue);
    for (int index = 0; index < lines.Length; index++) {
      string line = Printable(page, lines[index], fonts.Regular, fonts);
      if (line.Length > 0) {
        page.AddText(line, size, new(0, -size * (1 + index * LineHeight)), fonts.Regular);
      }
    }
    page.ResetColor();
    operations.Add(Pop.Value);
  }

  private static void DrawNoteMarker(PdfPageBuilder page, PdfMarkupItem item, int number, int rotation, Fonts fonts) {
    PdfMarkupPoint position = item.Position!.Value;
    List<IGraphicsStateOperation> operations = page.CurrentStream.Operations;
    operations.Add(Push.Value);
    operations.Add(Upright(position, rotation));
    (byte red, byte green, byte blue) = Colour(item.Colour);
    page.SetTextAndFillColor(red, green, blue);
    page.SetStrokeColor(red, green, blue);
    page.DrawCircle(new(0, 0), NoteDiameter, 0, fill: true);
    string label = number.ToString(CultureInfo.InvariantCulture);
    double size = label.Length > 2 ? 6 : 8;
    double width = TextWidth(page, label, size, fonts.Bold);
    page.SetTextAndFillColor(255, 255, 255);
    page.AddText(label, size, new(-width / 2, -size * .35), fonts.Bold);
    page.ResetColor();
    operations.Add(Pop.Value);
  }

  // ---------- The notes pages ----------

  private static void AddNotesPages(PdfDocumentBuilder builder, List<(int Number, PdfMarkupItem Item)> notes, PdfMarkupFlattenOptions options, Fonts fonts) {
    const double margin = 50;
    const double size = 10;
    const double numberWidth = 24;
    PdfPageBuilder page = builder.AddPage(PageSize.A4);
    double width = page.PageSize.Width - margin * 2;
    double y = page.PageSize.Height - margin - 16;
    page.AddText(Printable(page, options.NotesTitle, fonts.Bold, fonts), 16, new(margin, y), fonts.Bold);
    y -= 16 * LineHeight + 6;
    foreach ((int number, PdfMarkupItem note) in notes) {
      // A note that wasn't given the time it was added has no date
      string created = note.Created == default
        ? ""
        : TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(note.Created, DateTimeKind.Utc), options.TimeZone).ToString(options.DateFormat, CultureInfo.CurrentCulture);
      string heading = string.Join("  –  ", new[] {
        string.Format(CultureInfo.CurrentCulture, options.NotePageFormat, note.Page),
        note.Author,
        created
      }.Where(part => !string.IsNullOrWhiteSpace(part)));
      List<string> lines = [.. note.Text.ReplaceLineEndings("\n").Split('\n').SelectMany(line => Wrap(page, Printable(page, line, fonts.Regular, fonts), size, width - numberWidth, fonts.Regular))];
      double height = size * LineHeight * (lines.Count + 1);
      if (y - height < margin) {
        page = builder.AddPage(PageSize.A4);
        y = page.PageSize.Height - margin - size;
      }
      (byte red, byte green, byte blue) = Colour(note.Colour);
      page.SetTextAndFillColor(red, green, blue);
      page.SetStrokeColor(red, green, blue);
      page.DrawCircle(new(margin + NoteDiameter / 2, y + size * .35), NoteDiameter, 0, fill: true);
      string label = number.ToString(CultureInfo.InvariantCulture);
      double labelSize = label.Length > 2 ? 6 : 8;
      page.SetTextAndFillColor(255, 255, 255);
      page.AddText(label, labelSize, new(margin + NoteDiameter / 2 - TextWidth(page, label, labelSize, fonts.Bold) / 2, y + size * .35 - labelSize * .35), fonts.Bold);
      page.ResetColor();
      page.AddText(Printable(page, heading, fonts.Bold, fonts), size, new(margin + numberWidth, y), fonts.Bold);
      foreach (string line in lines) {
        y -= size * LineHeight;
        if (y < margin) {
          page = builder.AddPage(PageSize.A4);
          y = page.PageSize.Height - margin - size;
        }
        if (line.Length > 0) {
          page.AddText(line, size, new(margin + numberWidth, y), fonts.Regular);
        }
      }
      y -= size * LineHeight + 8;
    }
  }

  private static IEnumerable<string> Wrap(PdfPageBuilder page, string text, double size, double width, PdfDocumentBuilder.AddedFont font) {
    if (text.Length == 0) {
      yield return "";
      yield break;
    }
    string line = "";
    foreach (string word in text.Split(' ')) {
      string candidate = line.Length == 0 ? word : $"{line} {word}";
      if (TextWidth(page, candidate, size, font) <= width) {
        line = candidate;
        continue;
      }
      if (line.Length > 0) {
        yield return line;
      }
      // A word too long for a line is broken where it has to be
      line = word;
      while (line.Length > 1 && TextWidth(page, line, size, font) > width) {
        int fits = line.Length - 1;
        while (fits > 1 && TextWidth(page, line[..fits], size, font) > width) {
          fits--;
        }
        yield return line[..fits];
        line = line[fits..];
      }
    }
    yield return line;
  }

  // ---------- Helpers ----------

  private static double TextWidth(PdfPageBuilder page, string text, double size, PdfDocumentBuilder.AddedFont font) {
    if (text.Length == 0) {
      return 0;
    }
    IReadOnlyList<Letter> letters = page.MeasureText(text, size, new(0, 0), font);
    return letters.Count == 0 ? 0 : letters[^1].EndBaseLine.X - letters[0].StartBaseLine.X;
  }

  // Turns what's drawn after it so that it's upright as the page is normally shown, with (0, 0) at the point given and y
  // going up. A page with /Rotate is shown turned clockwise, so the text is turned the other way in the page's coordinates
  private static ModifyCurrentTransformationMatrix Upright(PdfMarkupPoint origin, int rotation) {
    double radians = rotation * Math.PI / 180;
    double cos = Math.Round(Math.Cos(radians), 10);
    double sin = Math.Round(Math.Sin(radians), 10);
    return new([cos, sin, -sin, cos, origin.X, origin.Y]);
  }

  private static void Stroke(PdfPageBuilder page, IEnumerable<IReadOnlyList<PdfMarkupPoint>> lines, string colour, double width, bool round) {
    List<IGraphicsStateOperation> operations = page.CurrentStream.Operations;
    (byte red, byte green, byte blue) = Colour(colour);
    operations.Add(Push.Value);
    operations.Add(new SetStrokeColorDeviceRgb(red / 255.0, green / 255.0, blue / 255.0));
    operations.Add(new SetLineWidth(width > 0 ? width : 1));
    operations.Add(new SetLineCap(round ? 1 : 0));
    operations.Add(new SetLineJoin(round ? 1 : 0));
    bool any = false;
    foreach (IReadOnlyList<PdfMarkupPoint> line in lines.Where(l => l.Count > 0)) {
      operations.Add(new BeginNewSubpath(line[0].X, line[0].Y));
      // A single point is drawn as a dot, which needs a line, however short, to have round ends drawn on
      IEnumerable<PdfMarkupPoint> rest = line.Count == 1 ? [new(line[0].X + .01, line[0].Y)] : line.Skip(1);
      foreach (PdfMarkupPoint point in rest) {
        operations.Add(new AppendStraightLineSegment(point.X, point.Y));
      }
      any = true;
    }
    if (any) {
      operations.Add(StrokePath.Value);
    }
    operations.Add(Pop.Value);
  }

  internal static (byte Red, byte Green, byte Blue) Colour(string? colour) {
    string hex = (colour ?? "").Trim().TrimStart('#');
    if (hex.Length == 3) {
      hex = string.Concat(hex.Select(c => $"{c}{c}"));
    }
    return hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)
      ? ((byte)(value >> 16), (byte)(value >> 8 & 255), (byte)(value & 255))
      : ((byte)211, (byte)47, (byte)47);
  }

  // The characters, other than ASCII ones, that PdfPig draws correctly in its built-in Helvetica. It writes others, such as
  // accented letters, with the wrong codes, so é comes out as Ø
  private static readonly HashSet<char> Helvetica = [.. "£¥ßæøœ–—‘’“”•…"];

  // The text as it can be drawn: tabs as spaces, without other control characters, and with any character the font can't
  // show as a question mark. With the built-in Helvetica, accented letters lose their accents, so café comes out as cafe
  internal static string Printable(PdfPageBuilder page, string text, PdfDocumentBuilder.AddedFont font, Fonts fonts) {
    StringBuilder printable = new(text.Length);
    foreach (char c in text.Replace('\t', ' ')) {
      if (char.IsControl(c)) {
        continue;
      }
      if (fonts.BuiltIn) {
        printable.Append(c is >= ' ' and <= '~' || Helvetica.Contains(c) ? c : WithoutAccent(c));
        continue;
      }
      if (!fonts.Known.TryGetValue((font, c), out bool known)) {
        try {
          page.MeasureText(c.ToString(), 10, new(0, 0), font);
          known = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or ArgumentException or KeyNotFoundException) {
          known = false;
        }
        fonts.Known[(font, c)] = known;
      }
      printable.Append(known ? c : '?');
    }
    return fonts.BuiltIn ? printable.ToString() : Bidi.Visual(printable.ToString());
  }

  private static char WithoutAccent(char c) {
    string plain = new([.. c.ToString().Normalize(NormalizationForm.FormD).Where(part => CharUnicodeInfo.GetUnicodeCategory(part) != UnicodeCategory.NonSpacingMark)]);
    return plain.Length == 1 && plain[0] is >= ' ' and <= '~' ? plain[0] : '?';
  }

  // A 1×1 PNG of one colour, with the transparency given
  private static byte[] Png(byte red, byte green, byte blue, byte alpha) {
    using MemoryStream png = new();
    png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
    Chunk(png, "IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0]);
    using MemoryStream compressed = new();
    using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true)) {
      zlib.Write([0, red, green, blue, alpha]);
    }
    Chunk(png, "IDAT", compressed.ToArray());
    Chunk(png, "IEND", []);
    return png.ToArray();
  }

  private static void Chunk(Stream png, string type, byte[] data) {
    byte[] typeBytes = Encoding.ASCII.GetBytes(type);
    png.Write(BigEndian((uint)data.Length));
    png.Write(typeBytes);
    png.Write(data);
    png.Write(BigEndian(Crc([.. typeBytes, .. data])));
  }

  private static byte[] BigEndian(uint value) =>
    [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

  private static uint Crc(byte[] bytes) {
    uint crc = 0xffffffff;
    foreach (byte b in bytes) {
      crc ^= b;
      for (int bit = 0; bit < 8; bit++) {
        crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
      }
    }
    return crc ^ 0xffffffff;
  }
}
