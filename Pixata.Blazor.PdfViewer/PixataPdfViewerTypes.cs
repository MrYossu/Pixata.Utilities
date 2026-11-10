using System;
using System.Collections.Generic;
using System.Globalization;
using Pixata.Blazor.Components;
using Pixata.Extensions.Pdf;

namespace Pixata.Blazor.PdfViewer;

/// <summary>
/// How big a <see cref="PixataPdfViewer"/> shows the pages: fitted to the viewer's width (<see cref="FitWidth"/>, the
/// default), each page fitted inside the viewer (<see cref="FitPage"/>), or a scale where 1 is 100%. A <c>decimal</c> or
/// <c>double</c> converts to a scale, so <c>Zoom="1.5m"</c> shows the pages at 150%
/// </summary>
public readonly record struct PixataPdfViewerZoom {
  private PixataPdfViewerZoom(string? value) =>
    _value = value;

  private readonly string? _value;

  /// <summary>
  /// The value PDF.js uses: <c>page-width</c>, <c>page-fit</c> or a scale
  /// </summary>
  internal string Value =>
    _value ?? FitWidthValue;

  private const string FitWidthValue = "page-width";
  private const string FitPageValue = "page-fit";

  /// <summary>
  /// Each page is as wide as the viewer
  /// </summary>
  public static PixataPdfViewerZoom FitWidth { get; } = new(FitWidthValue);

  /// <summary>
  /// Each page fits inside the viewer, so a whole page can be seen at once
  /// </summary>
  public static PixataPdfViewerZoom FitPage { get; } = new(FitPageValue);

  /// <summary>
  /// The pages at a scale, where 1 is 100%. It's kept between 0.1 and 25
  /// </summary>
  public static PixataPdfViewerZoom Scale(decimal scale) =>
    new(Math.Round(Math.Clamp(scale, 0.1m, 25m), 2).ToString("0.##", CultureInfo.InvariantCulture));

  /// <summary>
  /// Whether this fits the pages to the viewer, rather than being a scale
  /// </summary>
  public bool IsFit =>
    Value is FitWidthValue or FitPageValue;

  /// <summary>
  /// The scale, where 1 is 100%, or null if this fits the pages to the viewer
  /// </summary>
  public decimal? ScaleValue =>
    IsFit ? null : decimal.Parse(Value, CultureInfo.InvariantCulture);

  internal static PixataPdfViewerZoom FromValue(string value) =>
    new(value);

  /// <summary>
  /// A scale, where 1 is 100%, so <c>Zoom="1.5m"</c> shows the pages at 150%
  /// </summary>
  public static implicit operator PixataPdfViewerZoom(decimal scale) =>
    Scale(scale);

  /// <summary>
  /// A scale, where 1 is 100%
  /// </summary>
  public static implicit operator PixataPdfViewerZoom(double scale) =>
    Scale((decimal)scale);

  public bool Equals(PixataPdfViewerZoom other) =>
    Value == other.Value;

  public override int GetHashCode() =>
    Value.GetHashCode();

  /// <summary>
  /// <c>page-width</c>, <c>page-fit</c> or the scale
  /// </summary>
  public override string ToString() =>
    Value;
}

/// <summary>
/// Why a <see cref="PixataPdfViewer"/> couldn't show a PDF
/// </summary>
public enum PixataPdfViewerErrorKind {
  /// <summary>
  /// The PDF needs a password to open it
  /// </summary>
  PasswordProtected,

  /// <summary>
  /// The file isn't a PDF, or is damaged
  /// </summary>
  NotAPdf,

  /// <summary>
  /// There's nothing at the <c>Url</c>
  /// </summary>
  NotFound,

  /// <summary>
  /// Anything else, such as a network failure or a <c>Url</c> on another site that doesn't allow it to be fetched
  /// </summary>
  Other
}

/// <summary>
/// Given to <see cref="PixataPdfViewer.OnError"/> when a PDF can't be shown
/// </summary>
public class PixataPdfViewerErrorEventArgs {
  /// <summary>
  /// Why it couldn't be shown
  /// </summary>
  public PixataPdfViewerErrorKind Kind { get; init; }

  /// <summary>
  /// PDF.js's description of the problem, for a log
  /// </summary>
  public string Detail { get; init; } = "";
}

/// <summary>
/// Given to <see cref="PixataPdfViewer.OnLoaded"/> when a PDF has been opened
/// </summary>
public class PixataPdfViewerLoadedEventArgs {
  /// <summary>
  /// How many pages the PDF has
  /// </summary>
  public int PageCount { get; init; }
}

/// <summary>
/// Given to <see cref="PixataPdfViewer.OnDownload"/> before the PDF is downloaded
/// </summary>
public class PixataPdfViewerDownloadEventArgs {
  /// <summary>
  /// The name the file is saved as, which can be changed
  /// </summary>
  public string FileName { get; set; } = "";

  /// <summary>
  /// Set to true to stop the download, if you want to do something else instead
  /// </summary>
  public bool IsCancelled { get; set; }
}

/// <summary>
/// The text a <see cref="PixataPdfViewer"/> shows, for translating. Give a viewer your own with <c>Messages</c>
/// </summary>
public class PixataPdfViewerMessages {
  /// <summary>
  /// The English text, used when nothing else has been given
  /// </summary>
  public static PixataPdfViewerMessages Default { get; } = new();

  /// <summary>
  /// The accessible name of the toolbar
  /// </summary>
  public string Toolbar { get; init; } = "PDF viewer";

  /// <summary>
  /// The accessible name of the area the pages are shown in
  /// </summary>
  public string Pages { get; init; } = "Pages";

  /// <summary>
  /// The tooltip and accessible name of the previous page button
  /// </summary>
  public string PreviousPage { get; init; } = "Previous page";

  /// <summary>
  /// The tooltip and accessible name of the next page button
  /// </summary>
  public string NextPage { get; init; } = "Next page";

  /// <summary>
  /// The accessible name of the page number box
  /// </summary>
  public string PageNumber { get; init; } = "Page";

  /// <summary>
  /// After the page number box. {0} is the number of pages
  /// </summary>
  public string PageCountFormat { get; init; } = "of {0}";

  /// <summary>
  /// The tooltip and accessible name of the zoom out button
  /// </summary>
  public string ZoomOut { get; init; } = "Zoom out";

  /// <summary>
  /// The tooltip and accessible name of the zoom in button
  /// </summary>
  public string ZoomIn { get; init; } = "Zoom in";

  /// <summary>
  /// The accessible name of the zoom drop-down
  /// </summary>
  public string Zoom { get; init; } = "Zoom";

  /// <summary>
  /// The zoom drop-down's option that fits the pages to the viewer's width
  /// </summary>
  public string FitWidth { get; init; } = "Fit width";

  /// <summary>
  /// The zoom drop-down's option that fits each page inside the viewer
  /// </summary>
  public string FitPage { get; init; } = "Fit page";

  /// <summary>
  /// The tooltip and accessible name of the rotate anticlockwise button
  /// </summary>
  public string RotateAnticlockwise { get; init; } = "Rotate anticlockwise";

  /// <summary>
  /// The tooltip and accessible name of the rotate clockwise button
  /// </summary>
  public string RotateClockwise { get; init; } = "Rotate clockwise";

  /// <summary>
  /// The placeholder and accessible name of the search box
  /// </summary>
  public string Search { get; init; } = "Search";

  /// <summary>
  /// The tooltip and accessible name of the button that shows the previous search match
  /// </summary>
  public string PreviousMatch { get; init; } = "Previous match";

  /// <summary>
  /// The tooltip and accessible name of the button that shows the next search match
  /// </summary>
  public string NextMatch { get; init; } = "Next match";

  /// <summary>
  /// The number of matches. {0} is the one shown and {1} is how many there are
  /// </summary>
  public string MatchesFormat { get; init; } = "{0} of {1}";

  /// <summary>
  /// Shown next to the search box when the text isn't in the PDF
  /// </summary>
  public string NoMatches { get; init; } = "No matches";

  /// <summary>
  /// The tooltip and accessible name of the download button
  /// </summary>
  public string Download { get; init; } = "Download";

  /// <summary>
  /// The tooltip and accessible name of the print button
  /// </summary>
  public string Print { get; init; } = "Print";

  /// <summary>
  /// Shown while a PDF is loading
  /// </summary>
  public string Loading { get; init; } = "Loading...";

  /// <summary>
  /// Shown when there is no PDF to show
  /// </summary>
  public string NoDocument { get; init; } = "No document";

  /// <summary>
  /// Shown when the PDF needs a password, which the viewer can't ask for yet
  /// </summary>
  public string PasswordProtected { get; init; } = "This PDF is protected by a password, so it can't be shown";

  /// <summary>
  /// Shown when the file isn't a PDF, or is damaged
  /// </summary>
  public string NotAPdf { get; init; } = "This file isn't a PDF, or it's damaged";

  /// <summary>
  /// Shown when there's nothing at the <c>Url</c>
  /// </summary>
  public string NotFound { get; init; } = "The PDF couldn't be found";

  /// <summary>
  /// Shown when the PDF can't be loaded for any other reason, such as a network failure
  /// </summary>
  public string LoadFailed { get; init; } = "The PDF couldn't be loaded";

  /// <summary>
  /// The accessible name of the group of mark-up tools on the toolbar
  /// </summary>
  public string MarkupTools { get; init; } = "Mark-up";

  /// <summary>
  /// The tooltip and accessible name of the tool that adds a note where the page is clicked
  /// </summary>
  public string AddNote { get; init; } = "Add a note";

  /// <summary>
  /// The tooltip and accessible name of the tool that highlights the text selected, or an area dragged out
  /// </summary>
  public string Highlight { get; init; } = "Highlight";

  /// <summary>
  /// The tooltip and accessible name of the tool that strikes through the text selected, or an area dragged out
  /// </summary>
  public string StrikeThrough { get; init; } = "Strike through";

  /// <summary>
  /// The tooltip and accessible name of the tool that draws freehand
  /// </summary>
  public string Draw { get; init; } = "Draw";

  /// <summary>
  /// The tooltip and accessible name of the tool that writes text where the page is clicked
  /// </summary>
  public string AddText { get; init; } = "Add text";

  /// <summary>
  /// The tooltip and accessible name of the colour picker for the mark-up tools
  /// </summary>
  public string MarkupColour { get; init; } = "Colour";

  /// <summary>
  /// The accessible name of the pop-up for a note that's being added
  /// </summary>
  public string NewNote { get; init; } = "New note";

  /// <summary>
  /// The accessible name of the box a note is written in
  /// </summary>
  public string Note { get; init; } = "Note";

  /// <summary>
  /// The placeholder of the box a note is written in
  /// </summary>
  public string NotePlaceholder { get; init; } = "Write a note";

  /// <summary>
  /// The accessible name of the box text is written in on the page
  /// </summary>
  public string Text { get; init; } = "Text";

  /// <summary>
  /// The placeholder of the box text is written in on the page
  /// </summary>
  public string TextPlaceholder { get; init; } = "Type here";

  /// <summary>
  /// The button in a note's pop-up that deletes it, or the other mark-up shown
  /// </summary>
  public string DeleteMarkup { get; init; } = "Delete";

  /// <summary>
  /// The button that closes a note's pop-up, saving what's been written
  /// </summary>
  public string CloseMarkup { get; init; } = "Done";

  internal string PageCount(int count) =>
    string.Format(CultureInfo.CurrentCulture, PageCountFormat, count);

  internal string Matches(int current, int total) =>
    string.Format(CultureInfo.CurrentCulture, MatchesFormat, current, total);

  internal string Error(PixataPdfViewerErrorKind kind) =>
    kind switch {
      PixataPdfViewerErrorKind.PasswordProtected => PasswordProtected,
      PixataPdfViewerErrorKind.NotAPdf => NotAPdf,
      PixataPdfViewerErrorKind.NotFound => NotFound,
      _ => LoadFailed
    };
}

/// <summary>
/// The mark-up tools on a <see cref="PixataPdfViewer"/>'s toolbar
/// </summary>
public enum PixataPdfViewerMarkupTool {
  /// <summary>
  /// No tool, so the pages can be scrolled and their text selected, and clicking mark-up shows it
  /// </summary>
  None,

  /// <summary>
  /// Clicking the page adds a note there
  /// </summary>
  Note,

  /// <summary>
  /// Selecting text highlights it. Dragging where there's no text, such as on a scanned page, highlights the area
  /// </summary>
  Highlight,

  /// <summary>
  /// Selecting text strikes it through. Dragging where there's no text strikes through the area
  /// </summary>
  StrikeThrough,

  /// <summary>
  /// Dragging draws freehand
  /// </summary>
  Ink,

  /// <summary>
  /// Clicking the page writes text there
  /// </summary>
  Text
}

/// <summary>
/// What happened to a piece of mark-up
/// </summary>
public enum PixataPdfViewerMarkupChange {
  /// <summary>
  /// The user added it
  /// </summary>
  Added,

  /// <summary>
  /// The user changed its text
  /// </summary>
  Changed,

  /// <summary>
  /// The user deleted it
  /// </summary>
  Deleted
}

/// <summary>
/// Given to <see cref="PixataPdfViewer.OnMarkupChanged"/> when the user adds, changes or deletes mark-up
/// </summary>
public class PixataPdfViewerMarkupChangedEventArgs {
  /// <summary>
  /// The item. For one that's been deleted, it's as it was
  /// </summary>
  public PdfMarkupItem Item { get; init; } = null!;

  /// <summary>
  /// What happened to it
  /// </summary>
  public PixataPdfViewerMarkupChange Change { get; init; }

  /// <summary>
  /// All the mark-up, now
  /// </summary>
  public IReadOnlyList<PdfMarkupItem> Markup { get; init; } = [];
}

/// <summary>
/// The icons on the viewer's toolbar that Pixata.Blazor doesn't have
/// </summary>
internal static class PixataPdfViewerIcons {
  public static readonly PixataSvgIcon ZoomIn = new("zoom-in", "<circle cx=\"11\" cy=\"11\" r=\"6\"/><path d=\"M20 20l-4.5-4.5M8.5 11h5M11 8.5v5\"/>");
  public static readonly PixataSvgIcon ZoomOut = new("zoom-out", "<circle cx=\"11\" cy=\"11\" r=\"6\"/><path d=\"M20 20l-4.5-4.5M8.5 11h5\"/>");
  public static readonly PixataSvgIcon RotateClockwise = new("rotate-clockwise", "<path d=\"M19 12a7 7 0 1 1-2.05-4.95\"/><path d=\"M19 4v4h-4\"/>");
  public static readonly PixataSvgIcon RotateAnticlockwise = new("rotate-anticlockwise", "<path d=\"M5 12a7 7 0 1 0 2.05-4.95\"/><path d=\"M5 4v4h4\"/>");
  public static readonly PixataSvgIcon Note = new("note", "<path d=\"M5 5h14a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1h-8l-4 3v-3H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1z\"/><path d=\"M8 9h8M8 12h5\"/>");
  public static readonly PixataSvgIcon Highlight = new("highlight", "<path d=\"M13.5 4.5l6 6L11 19H5v-6z\"/><path d=\"M3 21h18\"/>");
  public static readonly PixataSvgIcon Text = new("text", "<path d=\"M5 7V4h14v3M12 4v16M9 20h6\"/>");
  public static readonly PixataSvgIcon Print = new("print", "<path d=\"M7 9V4h10v5\"/><rect x=\"3\" y=\"9\" width=\"18\" height=\"8\" rx=\"2\"/><path d=\"M7 14h10v6H7z\"/>");
}
