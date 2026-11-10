using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pixata.Extensions.Pdf;

/// <summary>
/// The kinds of mark-up that can be added to a PDF, by <c>PixataPdfViewer</c> in Pixata.Blazor.PdfViewer, and drawn into it by
/// <c>PdfMarkupFlattener</c> in Pixata.Pdf
/// </summary>
public enum PdfMarkupType {
  /// <summary>
  /// A note pinned to a point, at <see cref="PdfMarkupItem.Position"/>, with its text in <see cref="PdfMarkupItem.Text"/>
  /// </summary>
  Note,

  /// <summary>
  /// Highlighted text, or an area, made of the rectangles in <see cref="PdfMarkupItem.Rects"/>
  /// </summary>
  Highlight,

  /// <summary>
  /// Text that's struck through, made of the rectangles in <see cref="PdfMarkupItem.Rects"/>, with a line through the middle
  /// of each
  /// </summary>
  StrikeThrough,

  /// <summary>
  /// Freehand drawing, made of the lines in <see cref="PdfMarkupItem.Strokes"/>
  /// </summary>
  Ink,

  /// <summary>
  /// Text written on the page, such as a corrected figure, with its top left corner at <see cref="PdfMarkupItem.Position"/>
  /// </summary>
  Text
}

/// <summary>
/// A point on a page of a PDF, in points (1/72 of an inch), in the page's own coordinates. These are the coordinates a PDF
/// uses for its annotations: from the bottom left of the page, with y going up, before the page is rotated by its
/// <c>/Rotate</c> entry
/// </summary>
/// <param name="X">How far across the page it is</param>
/// <param name="Y">How far up the page it is</param>
public readonly record struct PdfMarkupPoint(double X, double Y);

/// <summary>
/// A rectangle on a page of a PDF, in the same coordinates as <see cref="PdfMarkupPoint"/>
/// </summary>
/// <param name="Left">The x of its left edge</param>
/// <param name="Bottom">The y of its bottom edge</param>
/// <param name="Right">The x of its right edge</param>
/// <param name="Top">The y of its top edge</param>
public readonly record struct PdfMarkupRect(double Left, double Bottom, double Right, double Top) {
  /// <summary>
  /// How wide it is
  /// </summary>
  [JsonIgnore]
  public double Width =>
    Right - Left;

  /// <summary>
  /// How tall it is
  /// </summary>
  [JsonIgnore]
  public double Height =>
    Top - Bottom;
}

/// <summary>
/// One piece of mark-up on a PDF, such as a note or a highlight, with who added it and when. Mark-up is kept apart from the
/// PDF, so the original file is never changed. Store a list of these with the PDF, as JSON (see <see cref="PdfMarkup"/>), and
/// give them back to the viewer the next time the PDF is shown
/// </summary>
public class PdfMarkupItem {
  /// <summary>
  /// Identifies the item, so that it can be changed or deleted
  /// </summary>
  public string Id { get; set; } = Guid.NewGuid().ToString("N");

  /// <summary>
  /// What kind of mark-up it is, which says which of the other properties are used
  /// </summary>
  public PdfMarkupType Type { get; set; }

  /// <summary>
  /// The page it's on, counting from 1
  /// </summary>
  public int Page { get; set; } = 1;

  /// <summary>
  /// The name of the person who added it, shown with it
  /// </summary>
  public string Author { get; set; } = "";

  /// <summary>
  /// The id of the person who added it, such as their user id, if you give the viewer one. When it's set, it's this rather
  /// than <see cref="Author"/> that says whose mark-up it is, so two people with the same name can't change each other's
  /// </summary>
  public string? AuthorId { get; set; }

  /// <summary>
  /// When it was added, in UTC
  /// </summary>
  public DateTime Created { get; set; }

  /// <summary>
  /// When its text was last changed, in UTC, or null if it hasn't been
  /// </summary>
  public DateTime? Modified { get; set; }

  /// <summary>
  /// Its colour, as a CSS hex colour such as <c>#ffd400</c>
  /// </summary>
  public string Colour { get; set; } = PdfMarkup.DefaultHighlightColour;

  /// <summary>
  /// For a <see cref="PdfMarkupType.Note"/>, the point it's pinned to. For <see cref="PdfMarkupType.Text"/>, the top left corner
  /// of the text, as the page is normally shown
  /// </summary>
  public PdfMarkupPoint? Position { get; set; }

  /// <summary>
  /// For a <see cref="PdfMarkupType.Highlight"/> or a <see cref="PdfMarkupType.StrikeThrough"/>, the rectangles it covers,
  /// usually one for each line of text
  /// </summary>
  public List<PdfMarkupRect> Rects { get; set; } = [];

  /// <summary>
  /// For <see cref="PdfMarkupType.Ink"/>, the lines drawn, each a list of points
  /// </summary>
  public List<List<PdfMarkupPoint>> Strokes { get; set; } = [];

  /// <summary>
  /// For <see cref="PdfMarkupType.Ink"/>, how thick the lines are, in points
  /// </summary>
  public double LineWidth { get; set; } = 2;

  /// <summary>
  /// For <see cref="PdfMarkupType.Text"/>, the size of the text, in points
  /// </summary>
  public double FontSize { get; set; } = 12;

  /// <summary>
  /// For a <see cref="PdfMarkupType.Note"/>, the note. For <see cref="PdfMarkupType.Text"/>, the text written on the page,
  /// which can have more than one line. For a <see cref="PdfMarkupType.Highlight"/> or a
  /// <see cref="PdfMarkupType.StrikeThrough"/>, the text it covers, if it was made by selecting text
  /// </summary>
  public string Text { get; set; } = "";

  /// <summary>
  /// A copy of the item, with copies of its lists, so that changing one doesn't change the other
  /// </summary>
  public PdfMarkupItem Clone() {
    PdfMarkupItem copy = (PdfMarkupItem)MemberwiseClone();
    copy.Rects = [.. Rects];
    copy.Strokes = [.. Strokes.Select(stroke => new List<PdfMarkupPoint>(stroke))];
    return copy;
  }
}

/// <summary>
/// Helpers for lists of <see cref="PdfMarkupItem"/>
/// </summary>
public static class PdfMarkup {
  /// <summary>
  /// The colour of a highlight, unless another is chosen
  /// </summary>
  public const string DefaultHighlightColour = "#ffd400";

  /// <summary>
  /// The colour of the other kinds of mark-up, unless another is chosen
  /// </summary>
  public const string DefaultColour = "#d32f2f";

  /// <summary>
  /// The JSON options <see cref="ToJson"/> and <see cref="FromJson"/> use: camel case names, kinds of mark-up as strings, and no
  /// nulls
  /// </summary>
  public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) {
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  /// <summary>
  /// The mark-up as JSON, to store with the PDF
  /// </summary>
  public static string ToJson(IEnumerable<PdfMarkupItem> markup) =>
    JsonSerializer.Serialize(markup, JsonOptions);

  /// <summary>
  /// Mark-up stored as JSON by <see cref="ToJson"/>. Null or blank JSON gives an empty list
  /// </summary>
  public static List<PdfMarkupItem> FromJson(string? json) =>
    string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<PdfMarkupItem>>(json, JsonOptions) ?? [];
}
