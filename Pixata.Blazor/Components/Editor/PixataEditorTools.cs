using System.Collections.Generic;

namespace Pixata.Blazor.Components;

// The built-in toolbar tools. Each is an ordinary tool with its command, title and icon filled in, so you can change any of
// them, eg new PixataEditorBold { Title = "Gras" }

public class PixataEditorBold : PixataEditorToggleButtonTool {
  public PixataEditorBold() {
    CommandName = PixataEditorCommands.Bold;
    Title = "Bold";
    Icon = PixataSvgIcons.Bold;
  }
}

public class PixataEditorItalic : PixataEditorToggleButtonTool {
  public PixataEditorItalic() {
    CommandName = PixataEditorCommands.Italic;
    Title = "Italic";
    Icon = PixataSvgIcons.Italic;
  }
}

public class PixataEditorUnderline : PixataEditorToggleButtonTool {
  public PixataEditorUnderline() {
    CommandName = PixataEditorCommands.Underline;
    Title = "Underline";
    Icon = PixataSvgIcons.Underline;
  }
}

public class PixataEditorStrikethrough : PixataEditorToggleButtonTool {
  public PixataEditorStrikethrough() {
    CommandName = PixataEditorCommands.Strikethrough;
    Title = "Strikethrough";
    Icon = PixataSvgIcons.Strikethrough;
  }
}

public class PixataEditorSubScript : PixataEditorToggleButtonTool {
  public PixataEditorSubScript() {
    CommandName = PixataEditorCommands.SubScript;
    Title = "Subscript";
    Icon = PixataSvgIcons.Subscript;
  }
}

public class PixataEditorSuperScript : PixataEditorToggleButtonTool {
  public PixataEditorSuperScript() {
    CommandName = PixataEditorCommands.SuperScript;
    Title = "Superscript";
    Icon = PixataSvgIcons.Superscript;
  }
}

public class PixataEditorAlignLeft : PixataEditorToggleButtonTool {
  public PixataEditorAlignLeft() {
    CommandName = PixataEditorCommands.AlignLeft;
    Title = "Align left";
    Icon = PixataSvgIcons.AlignLeft;
  }
}

public class PixataEditorAlignCenter : PixataEditorToggleButtonTool {
  public PixataEditorAlignCenter() {
    CommandName = PixataEditorCommands.AlignCenter;
    Title = "Centre";
    Icon = PixataSvgIcons.AlignCentre;
  }
}

public class PixataEditorAlignRight : PixataEditorToggleButtonTool {
  public PixataEditorAlignRight() {
    CommandName = PixataEditorCommands.AlignRight;
    Title = "Align right";
    Icon = PixataSvgIcons.AlignRight;
  }
}

public class PixataEditorAlignJustify : PixataEditorToggleButtonTool {
  public PixataEditorAlignJustify() {
    CommandName = PixataEditorCommands.AlignJustify;
    Title = "Justify";
    Icon = PixataSvgIcons.AlignJustify;
  }
}

public class PixataEditorUnorderedList : PixataEditorToggleButtonTool {
  public PixataEditorUnorderedList() {
    CommandName = PixataEditorCommands.UnorderedList;
    Title = "Bulleted list";
    Icon = PixataSvgIcons.ListBulleted;
  }
}

public class PixataEditorOrderedList : PixataEditorToggleButtonTool {
  public PixataEditorOrderedList() {
    CommandName = PixataEditorCommands.OrderedList;
    Title = "Numbered list";
    Icon = PixataSvgIcons.ListNumbered;
  }
}

public class PixataEditorIndent : PixataEditorButtonTool {
  public PixataEditorIndent() {
    CommandName = PixataEditorCommands.Indent;
    Title = "Indent";
    Icon = PixataSvgIcons.IndentIncrease;
  }
}

public class PixataEditorOutdent : PixataEditorButtonTool {
  public PixataEditorOutdent() {
    CommandName = PixataEditorCommands.Outdent;
    Title = "Outdent";
    Icon = PixataSvgIcons.IndentDecrease;
  }
}

public class PixataEditorUndo : PixataEditorButtonTool {
  public PixataEditorUndo() {
    CommandName = PixataEditorCommands.Undo;
    Title = "Undo";
    Icon = PixataSvgIcons.Undo;
  }
}

public class PixataEditorRedo : PixataEditorButtonTool {
  public PixataEditorRedo() {
    CommandName = PixataEditorCommands.Redo;
    Title = "Redo";
    Icon = PixataSvgIcons.Redo;
  }
}

public class PixataEditorCreateLink : PixataEditorDialogTool {
  public PixataEditorCreateLink() {
    CommandName = PixataEditorCommands.CreateLink;
    Title = "Insert link";
    Icon = PixataSvgIcons.InsertLink;
  }
}

public class PixataEditorUnlink : PixataEditorButtonTool {
  public PixataEditorUnlink() {
    CommandName = PixataEditorCommands.Unlink;
    Title = "Remove link";
    Icon = PixataSvgIcons.RemoveLink;
  }
}

public class PixataEditorInsertImage : PixataEditorDialogTool {
  public PixataEditorInsertImage() {
    CommandName = PixataEditorCommands.InsertImage;
    Title = "Insert image";
    Icon = PixataSvgIcons.InsertImage;
  }
}

public class PixataEditorViewHtml : PixataEditorDialogTool {
  public PixataEditorViewHtml() {
    CommandName = PixataEditorCommands.ViewHtml;
    Title = "View HTML";
    Icon = PixataSvgIcons.HtmlSource;
  }
}

public class PixataEditorCleanFormatting : PixataEditorButtonTool {
  public PixataEditorCleanFormatting() {
    CommandName = PixataEditorCommands.CleanFormatting;
    Title = "Clear formatting";
    Icon = PixataSvgIcons.ClearFormatting;
  }
}

/// <summary>
/// Opens a grid for choosing the size of a new table
/// </summary>
public class PixataEditorInsertTable : PixataEditorButtonTool {
  public PixataEditorInsertTable() {
    CommandName = PixataEditorCommands.InsertTable;
    Title = "Insert table";
    Icon = PixataSvgIcons.InsertTable;
  }

  /// <summary>
  /// The number of rows in the grid
  /// </summary>
  public int Rows { get; set; } = 8;

  /// <summary>
  /// The number of columns in the grid
  /// </summary>
  public int Columns { get; set; } = 8;
}

public class PixataEditorAddColumnBefore : PixataEditorButtonTool {
  public PixataEditorAddColumnBefore() {
    CommandName = PixataEditorCommands.AddColumnBefore;
    Title = "Add column before";
    Icon = PixataSvgIcons.TableColumnAddBefore;
  }
}

public class PixataEditorAddColumnAfter : PixataEditorButtonTool {
  public PixataEditorAddColumnAfter() {
    CommandName = PixataEditorCommands.AddColumnAfter;
    Title = "Add column after";
    Icon = PixataSvgIcons.TableColumnAddAfter;
  }
}

public class PixataEditorAddRowBefore : PixataEditorButtonTool {
  public PixataEditorAddRowBefore() {
    CommandName = PixataEditorCommands.AddRowBefore;
    Title = "Add row above";
    Icon = PixataSvgIcons.TableRowAddBefore;
  }
}

public class PixataEditorAddRowAfter : PixataEditorButtonTool {
  public PixataEditorAddRowAfter() {
    CommandName = PixataEditorCommands.AddRowAfter;
    Title = "Add row below";
    Icon = PixataSvgIcons.TableRowAddAfter;
  }
}

public class PixataEditorDeleteColumn : PixataEditorButtonTool {
  public PixataEditorDeleteColumn() {
    CommandName = PixataEditorCommands.DeleteColumn;
    Title = "Delete column";
    Icon = PixataSvgIcons.TableColumnDelete;
  }
}

public class PixataEditorDeleteRow : PixataEditorButtonTool {
  public PixataEditorDeleteRow() {
    CommandName = PixataEditorCommands.DeleteRow;
    Title = "Delete row";
    Icon = PixataSvgIcons.TableRowDelete;
  }
}

public class PixataEditorDeleteTable : PixataEditorButtonTool {
  public PixataEditorDeleteTable() {
    CommandName = PixataEditorCommands.DeleteTable;
    Title = "Delete table";
    Icon = PixataSvgIcons.TableDelete;
  }
}

public class PixataEditorMergeCells : PixataEditorButtonTool {
  public PixataEditorMergeCells() {
    CommandName = PixataEditorCommands.MergeCells;
    Title = "Merge cells";
    Icon = PixataSvgIcons.TableCellsMerge;
  }
}

public class PixataEditorSplitCell : PixataEditorButtonTool {
  public PixataEditorSplitCell() {
    CommandName = PixataEditorCommands.SplitCell;
    Title = "Split cell";
    Icon = PixataSvgIcons.TableCellSplit;
  }
}

public class PixataEditorFormat : PixataEditorDropDownListTool {
  public PixataEditorFormat() {
    CommandName = PixataEditorCommands.Format;
    Title = "Format";
    DefaultText = "Format";
    Data = PixataEditorDropDownListToolItems.FormatItems;
  }
}

public class PixataEditorFontFamily : PixataEditorDropDownListTool {
  public PixataEditorFontFamily() {
    CommandName = PixataEditorCommands.FontFamily;
    Title = "Font";
    DefaultText = "Font";
    Data = PixataEditorDropDownListToolItems.FontFamilyItems;
  }
}

public class PixataEditorFontSize : PixataEditorDropDownListTool {
  public PixataEditorFontSize() {
    CommandName = PixataEditorCommands.FontSize;
    Title = "Font size";
    DefaultText = "Font size";
    Data = PixataEditorDropDownListToolItems.FontSizeItems;
  }
}

public class PixataEditorForeColor : PixataEditorColorTool {
  public PixataEditorForeColor() {
    CommandName = PixataEditorCommands.ForeColor;
    Title = "Text colour";
    Icon = PixataSvgIcons.TextColour;
  }
}

public class PixataEditorBackgroundColor : PixataEditorColorTool {
  public PixataEditorBackgroundColor() {
    CommandName = PixataEditorCommands.BackgroundColor;
    Title = "Background colour";
    Icon = PixataSvgIcons.BackgroundColour;
  }
}

/// <summary>
/// Ready-made toolbars for <see cref="PixataEditor.Tools"/>. Each property returns a new list, so you can add to it, remove
/// from it or reorder it without affecting other editors
/// </summary>
public static class PixataEditorToolSets {
  /// <summary>
  /// The everyday tools: bold, italic and underline, the paragraph format, alignment, lists and indentation, links and
  /// images. This is what the editor shows if you don't set <see cref="PixataEditor.Tools"/>
  /// </summary>
  public static List<PixataEditorTool> Default => [
    new PixataEditorButtonGroup(new PixataEditorBold(), new PixataEditorItalic(), new PixataEditorUnderline()),
    new PixataEditorFormat(),
    new PixataEditorButtonGroup(new PixataEditorAlignLeft(), new PixataEditorAlignCenter(), new PixataEditorAlignRight(), new PixataEditorAlignJustify()),
    new PixataEditorButtonGroup(new PixataEditorUnorderedList(), new PixataEditorOrderedList(), new PixataEditorIndent(), new PixataEditorOutdent()),
    new PixataEditorButtonGroup(new PixataEditorCreateLink(), new PixataEditorUnlink()),
    new PixataEditorInsertImage()
  ];

  /// <summary>
  /// Every built-in tool: the above plus undo and redo, strikethrough, subscript and superscript, the font, its size and
  /// colours, tables, clearing the formatting and viewing the HTML
  /// </summary>
  public static List<PixataEditorTool> All => [
    new PixataEditorButtonGroup(new PixataEditorUndo(), new PixataEditorRedo()),
    new PixataEditorButtonGroup(new PixataEditorBold(), new PixataEditorItalic(), new PixataEditorUnderline(), new PixataEditorStrikethrough()),
    new PixataEditorButtonGroup(new PixataEditorSubScript(), new PixataEditorSuperScript()),
    new PixataEditorFormat(),
    new PixataEditorFontFamily(),
    new PixataEditorFontSize(),
    new PixataEditorForeColor(),
    new PixataEditorBackgroundColor(),
    new PixataEditorButtonGroup(new PixataEditorAlignLeft(), new PixataEditorAlignCenter(), new PixataEditorAlignRight(), new PixataEditorAlignJustify()),
    new PixataEditorButtonGroup(new PixataEditorUnorderedList(), new PixataEditorOrderedList(), new PixataEditorIndent(), new PixataEditorOutdent()),
    new PixataEditorButtonGroup(new PixataEditorCreateLink(), new PixataEditorUnlink()),
    new PixataEditorInsertImage(),
    new PixataEditorInsertTable(),
    new PixataEditorButtonGroup(new PixataEditorAddColumnBefore(), new PixataEditorAddColumnAfter(), new PixataEditorAddRowBefore(), new PixataEditorAddRowAfter()),
    new PixataEditorButtonGroup(new PixataEditorDeleteColumn(), new PixataEditorDeleteRow(), new PixataEditorDeleteTable()),
    new PixataEditorButtonGroup(new PixataEditorMergeCells(), new PixataEditorSplitCell()),
    new PixataEditorCleanFormatting(),
    new PixataEditorViewHtml()
  ];
}
