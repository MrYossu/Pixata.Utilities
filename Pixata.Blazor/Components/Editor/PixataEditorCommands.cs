namespace Pixata.Blazor.Components;

/// <summary>
/// The names of the commands <see cref="PixataEditor"/> understands, for <see cref="PixataEditor.ExecuteAsync"/> and the
/// <c>CommandName</c> of your own tools
/// </summary>
public static class PixataEditorCommands {
  public const string Undo = "undo";
  public const string Redo = "redo";
  public const string Bold = "bold";
  public const string Italic = "italic";
  public const string Underline = "underline";
  public const string Strikethrough = "strikethrough";
  public const string SubScript = "subscript";
  public const string SuperScript = "superscript";

  /// <summary>
  /// Takes a <see cref="PixataEditorFormatCommandArgs"/> whose value is <c>p</c> or <c>h1</c> to <c>h6</c>
  /// </summary>
  public const string Format = "format";

  /// <summary>
  /// Takes a <see cref="PixataEditorFormatCommandArgs"/> whose value is a CSS font-family, or "" to remove it
  /// </summary>
  public const string FontFamily = "fontFamily";

  /// <summary>
  /// Takes a <see cref="PixataEditorFormatCommandArgs"/> whose value is a CSS font-size, or "" to remove it
  /// </summary>
  public const string FontSize = "fontSize";

  /// <summary>
  /// Takes a <see cref="PixataEditorFormatCommandArgs"/> whose value is a CSS colour, or "" to remove it
  /// </summary>
  public const string ForeColor = "foreColor";

  /// <summary>
  /// Takes a <see cref="PixataEditorFormatCommandArgs"/> whose value is a CSS colour, or "" to remove it
  /// </summary>
  public const string BackgroundColor = "backColor";

  public const string AlignLeft = "alignLeft";
  public const string AlignCenter = "alignCenter";
  public const string AlignRight = "alignRight";
  public const string AlignJustify = "alignJustify";
  public const string UnorderedList = "insertUnorderedList";
  public const string OrderedList = "insertOrderedList";
  public const string Indent = "indent";
  public const string Outdent = "outdent";

  /// <summary>
  /// Takes a <see cref="PixataEditorLinkCommandArgs"/>
  /// </summary>
  public const string CreateLink = "createLink";

  public const string Unlink = "unlink";

  /// <summary>
  /// Takes a <see cref="PixataEditorImageCommandArgs"/>
  /// </summary>
  public const string InsertImage = "insertImage";

  /// <summary>
  /// Takes a <see cref="PixataEditorTableCommandArgs"/>
  /// </summary>
  public const string InsertTable = "insertTable";

  public const string AddColumnBefore = "addColumnBefore";
  public const string AddColumnAfter = "addColumnAfter";
  public const string AddRowBefore = "addRowBefore";
  public const string AddRowAfter = "addRowAfter";
  public const string DeleteColumn = "deleteColumn";
  public const string DeleteRow = "deleteRow";
  public const string DeleteTable = "deleteTable";
  public const string MergeCells = "mergeCells";
  public const string SplitCell = "splitCell";

  /// <summary>
  /// Replaces the whole document. Takes a <see cref="PixataEditorHtmlCommandArgs"/>. This is the command the View HTML tool
  /// runs when you update the HTML, and, unlike setting <c>Value</c>, it can be undone
  /// </summary>
  public const string ViewHtml = "setHtml";

  /// <summary>
  /// Inserts HTML at the cursor. Takes a <see cref="PixataEditorHtmlCommandArgs"/>
  /// </summary>
  public const string InsertHtml = "insertHtml";

  /// <summary>
  /// Removes the inline formatting (but not links), alignment and indentation from the selection, or from the paragraph
  /// holding the cursor
  /// </summary>
  public const string CleanFormatting = "cleanFormatting";

  public const string SelectAll = "selectAll";
}
