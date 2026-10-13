namespace Pixata.Blazor.Components;

// The icons used by PixataEditor's toolbar
public static partial class PixataSvgIcons {
  public static readonly PixataSvgIcon Bold = new("bold", "<path d=\"M7.5 5h5.5a3.5 3.5 0 0 1 0 7H7.5zM7.5 12h6.5a3.5 3.5 0 0 1 0 7H7.5z\" stroke-width=\"2.5\"/>");
  public static readonly PixataSvgIcon Italic = new("italic", "<path d=\"M11 5h7M6 19h7M15 5l-6 14\"/>");
  public static readonly PixataSvgIcon Underline = new("underline", "<path d=\"M7 4v7a5 5 0 0 0 10 0V4M5 20h14\"/>");
  public static readonly PixataSvgIcon Strikethrough = new("strikethrough", "<path d=\"M4 12h16M16.5 7.5C15.9 5.9 14.2 5 12 5c-2.6 0-4.5 1.3-4.5 3.3 0 1.4.9 2.3 2.4 2.9M7.6 16.2c.6 1.7 2.3 2.8 4.6 2.8 2.7 0 4.6-1.4 4.6-3.4 0-.6-.1-1.1-.4-1.6\"/>");
  public static readonly PixataSvgIcon Subscript = new("subscript", "<path d=\"M4 6l8 10M12 6l-8 10\"/><path d=\"M16.5 15.8c.3-.7 1-1.3 2-1.3 1.2 0 2 .7 2 1.7 0 2-3.9 2.6-3.9 4.8h4\" stroke-width=\"1.6\"/>");
  public static readonly PixataSvgIcon Superscript = new("superscript", "<path d=\"M4 8l8 10M12 8l-8 10\"/><path d=\"M16.5 4.3c.3-.7 1-1.3 2-1.3 1.2 0 2 .7 2 1.7 0 2-3.9 2.6-3.9 4.8h4\" stroke-width=\"1.6\"/>");
  public static readonly PixataSvgIcon AlignLeft = new("align-left", "<path d=\"M4 6h16M4 10h10M4 14h16M4 18h10\"/>");
  public static readonly PixataSvgIcon AlignCentre = new("align-centre", "<path d=\"M4 6h16M7 10h10M4 14h16M7 18h10\"/>");
  public static readonly PixataSvgIcon AlignRight = new("align-right", "<path d=\"M4 6h16M10 10h10M4 14h16M10 18h10\"/>");
  public static readonly PixataSvgIcon AlignJustify = new("align-justify", "<path d=\"M4 6h16M4 10h16M4 14h16M4 18h16\"/>");
  public static readonly PixataSvgIcon ListBulleted = new("list-bulleted", "<circle cx=\"5\" cy=\"7\" r=\"1.4\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"5\" cy=\"12\" r=\"1.4\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"5\" cy=\"17\" r=\"1.4\" fill=\"currentColor\" stroke=\"none\"/><path d=\"M9 7h11M9 12h11M9 17h11\"/>");
  public static readonly PixataSvgIcon ListNumbered = new("list-numbered", "<path d=\"M10 7h10M10 12h10M10 17h10\"/><path d=\"M4 5l1.5-1v5M3.5 9H7M3.6 14.2c.2-.6.8-1 1.5-1 .9 0 1.5.5 1.5 1.3 0 1.5-3 2-3 3.5h3\" stroke-width=\"1.4\"/>");
  public static readonly PixataSvgIcon IndentIncrease = new("indent-increase", "<path d=\"M4 5h16M11 10h9M11 14h9M4 19h16M4 9l3 3-3 3\"/>");
  public static readonly PixataSvgIcon IndentDecrease = new("indent-decrease", "<path d=\"M4 5h16M11 10h9M11 14h9M4 19h16M7 9l-3 3 3 3\"/>");
  public static readonly PixataSvgIcon InsertLink = new("insert-link", "<path d=\"M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1.2 1.2\"/><path d=\"M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1.2-1.2\"/>");
  public static readonly PixataSvgIcon RemoveLink = new("remove-link", "<path d=\"M15.5 12.5l2.2-2.2a3.6 3.6 0 0 0-5.1-5.1l-2.2 2.2M8.5 11.5l-2.2 2.2a3.6 3.6 0 0 0 5.1 5.1l2.2-2.2M4 4l3 3M17 17l3 3\"/>");
  public static readonly PixataSvgIcon InsertImage = new("insert-image", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><circle cx=\"8.5\" cy=\"10\" r=\"1.5\"/><path d=\"M21 16l-5-5-9 8\"/>");
  public static readonly PixataSvgIcon InsertTable = new("insert-table", "<rect x=\"3\" y=\"4\" width=\"18\" height=\"16\" rx=\"1.5\"/><path d=\"M3 9.5h18M3 15h18M9 4v16M15 4v16\"/>");
  public static readonly PixataSvgIcon TableColumnAddBefore = new("table-column-add-before", "<path d=\"M12 4h7a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-7zM12 12h8M3 12h6M6 9v6\"/>");
  public static readonly PixataSvgIcon TableColumnAddAfter = new("table-column-add-after", "<path d=\"M12 4H5a1 1 0 0 0-1 1v14a1 1 0 0 0 1 1h7zM4 12h8M15 12h6M18 9v6\"/>");
  public static readonly PixataSvgIcon TableRowAddBefore = new("table-row-add-before", "<path d=\"M4 12v7a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-7zM12 12v8M12 3v6M9 6h6\"/>");
  public static readonly PixataSvgIcon TableRowAddAfter = new("table-row-add-after", "<path d=\"M4 12V5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v7zM12 4v8M12 15v6M9 18h6\"/>");
  public static readonly PixataSvgIcon TableColumnDelete = new("table-column-delete", "<rect x=\"4\" y=\"4\" width=\"7\" height=\"16\" rx=\"1\"/><path d=\"M15 9.5l5 5M20 9.5l-5 5\"/>");
  public static readonly PixataSvgIcon TableRowDelete = new("table-row-delete", "<rect x=\"4\" y=\"4\" width=\"16\" height=\"7\" rx=\"1\"/><path d=\"M9.5 15l5 5M14.5 15l-5 5\"/>");
  public static readonly PixataSvgIcon TableDelete = new("table-delete", "<rect x=\"3\" y=\"3\" width=\"13\" height=\"13\" rx=\"1\"/><path d=\"M3 8.5h13M8.5 3v13M15 15l6 6M21 15l-6 6\"/>");
  public static readonly PixataSvgIcon TableCellsMerge = new("table-cells-merge", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"1\"/><path d=\"M4 12h5M7 10l2 2-2 2M20 12h-5M17 10l-2 2 2 2\"/>");
  public static readonly PixataSvgIcon TableCellSplit = new("table-cell-split", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"1\"/><path d=\"M12 5v14M10 12H6M8 10l-2 2 2 2M14 12h4M16 10l2 2-2 2\"/>");
  public static readonly PixataSvgIcon HtmlSource = new("html-source", "<path d=\"M8 7l-5 5 5 5M16 7l5 5-5 5M14 4l-4 16\"/>");
  public static readonly PixataSvgIcon Undo = new("undo", "<path d=\"M9 14L4 9l5-5\"/><path d=\"M4 9h10.5a5.5 5.5 0 0 1 0 11H11\"/>");
  public static readonly PixataSvgIcon Redo = new("redo", "<path d=\"M15 14l5-5-5-5\"/><path d=\"M20 9H9.5a5.5 5.5 0 0 0 0 11H13\"/>");
  public static readonly PixataSvgIcon ClearFormatting = new("clear-formatting", "<path d=\"M5 5h11M10.5 5l-3 14M14 14l6 6M20 14l-6 6\"/>");
  public static readonly PixataSvgIcon TextColour = new("text-colour", "<path d=\"M6.5 16L12 3l5.5 13M8.4 11.5h7.2\"/>");
  public static readonly PixataSvgIcon BackgroundColour = new("background-colour", "<path d=\"M12 2.5s-5 5.5-5 8.7a5 5 0 0 0 10 0C17 8 12 2.5 12 2.5z\"/>");
}
