using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// Something that can go in a <see cref="PixataEditor"/>'s toolbar: a button, a drop-down list, a colour picker, a group of
/// buttons or a custom tool. Pass a list of them to <see cref="PixataEditor.Tools"/>
/// </summary>
public abstract class PixataEditorTool {
  private protected PixataEditorTool() {
  }
}

/// <summary>
/// A toolbar button that runs a command when clicked. Use it directly for a command that has no built-in tool (eg
/// <see cref="PixataEditorCommands.SelectAll"/>), or use one of the built-in tools such as <see cref="PixataEditorUndo"/>
/// </summary>
public class PixataEditorButtonTool : PixataEditorTool {
  /// <summary>
  /// The command the button runs, one of the <see cref="PixataEditorCommands"/>
  /// </summary>
  public string CommandName { get; set; } = "";

  /// <summary>
  /// The button's tooltip, which is also its accessible name
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// A <see cref="PixataSvgIcon"/>, or the CSS class(es) of an icon font
  /// </summary>
  public object? Icon { get; set; }

  /// <summary>
  /// Extra CSS classes for the button
  /// </summary>
  public string? Class { get; set; }

  /// <summary>
  /// Whether the button can be used. The editor also disables tools that can't do anything at the moment (eg Undo when there
  /// is nothing to undo)
  /// </summary>
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Content to show in the button instead of the icon
  /// </summary>
  public RenderFragment? ChildContent { get; set; }
}

/// <summary>
/// A toolbar button that is shown pressed when its command applies to the selection, eg Bold when the selected text is bold
/// </summary>
public class PixataEditorToggleButtonTool : PixataEditorButtonTool {
}

/// <summary>
/// A toolbar button that opens a dialog: the link dialog for <see cref="PixataEditorCommands.CreateLink"/>, the image dialog
/// for <see cref="PixataEditorCommands.InsertImage"/>, and the HTML source dialog for
/// <see cref="PixataEditorCommands.ViewHtml"/>
/// </summary>
public class PixataEditorDialogTool : PixataEditorButtonTool {
  /// <summary>
  /// The dialog's title. Defaults to the button's <see cref="PixataEditorButtonTool.Title"/>
  /// </summary>
  public string? DialogTitle { get; set; }

  /// <summary>
  /// The dialog's width, as a CSS length. It never gets wider than the screen
  /// </summary>
  public string? DialogWidth { get; set; }
}

/// <summary>
/// A toolbar drop-down list whose items are passed to its command as the value, eg the paragraph and heading formats
/// </summary>
public class PixataEditorDropDownListTool : PixataEditorTool {
  /// <summary>
  /// The command run with the chosen item's value, one of the <see cref="PixataEditorCommands"/>
  /// </summary>
  public string CommandName { get; set; } = "";

  /// <summary>
  /// The tooltip, which is also the accessible name
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// The items in the list
  /// </summary>
  public List<PixataEditorDropDownListItem> Data { get; set; } = [];

  /// <summary>
  /// The text shown when the selection doesn't match any item
  /// </summary>
  public string? DefaultText { get; set; }

  /// <summary>
  /// The width of the drop-down, as a CSS length
  /// </summary>
  public string? Width { get; set; }

  /// <summary>
  /// The width of the list that drops down, as a CSS length. It is at least as wide as the drop-down
  /// </summary>
  public string? PopupWidth { get; set; }

  /// <summary>
  /// The height of the list that drops down, as a CSS length. By default it is at most 200px, and scrolls
  /// </summary>
  public string? PopupHeight { get; set; }

  public string? Class { get; set; }

  public bool Enabled { get; set; } = true;
}

/// <summary>
/// A toolbar colour picker, which shows a palette and runs its command with the chosen colour
/// </summary>
public class PixataEditorColorTool : PixataEditorTool {
  /// <summary>
  /// The command run with the chosen colour, <see cref="PixataEditorCommands.ForeColor"/> or
  /// <see cref="PixataEditorCommands.BackgroundColor"/>
  /// </summary>
  public string CommandName { get; set; } = "";

  /// <summary>
  /// The tooltip, which is also the accessible name
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// A <see cref="PixataSvgIcon"/>, or the CSS class(es) of an icon font
  /// </summary>
  public object? Icon { get; set; }

  /// <summary>
  /// The colours in the palette, as CSS colours. They are laid out in rows of <see cref="Columns"/>
  /// </summary>
  public IEnumerable<string> Colors { get; set; } = PixataEditorColorTool.DefaultColors;

  /// <summary>
  /// The number of colours in each row of the palette
  /// </summary>
  public int Columns { get; set; } = 10;

  /// <summary>
  /// The colour shown in the picker before the selection says otherwise
  /// </summary>
  public string? Value { get; set; }

  public string? Class { get; set; }

  public bool Enabled { get; set; } = true;

  /// <summary>
  /// The 60 colours in the palette by default: a column of greys, then nine hues, each from a light tint down to a dark shade
  /// </summary>
  public static IReadOnlyList<string> DefaultColors { get; } = [
    "#ffffff", "#fcd9d9", "#fce9d9", "#fcf6d9", "#d9fcd9", "#d9fcf5", "#d9f0fc", "#d9e2fc", "#e8d9fc", "#fcd9f0",
    "#d9d9d9", "#f5a3a3", "#f5c9a3", "#f5e7a3", "#a3f5a3", "#a3f5e4", "#a3daf5", "#a3b8f5", "#c5a3f5", "#f5a3da",
    "#a6a6a6", "#ed5e5e", "#eda15e", "#edd55e", "#5eed5e", "#5eedd1", "#5ebeed", "#5e82ed", "#9a5eed", "#ed5ebe",
    "#7f7f7f", "#e61919", "#e67919", "#e6c419", "#19e619", "#19e6bd", "#19a1e6", "#194ce6", "#6e19e6", "#e619a1",
    "#404040", "#a51212", "#a55712", "#a58d12", "#12a512", "#12a588", "#1274a5", "#1237a5", "#5012a5", "#a51274",
    "#000000", "#6e0c0c", "#6e3a0c", "#6e5e0c", "#0c6e0c", "#0c6e5b", "#0c4e6e", "#0c256e", "#350c6e", "#6e0c4e"
  ];
}

/// <summary>
/// Buttons shown together, sharing their borders, eg Bold, Italic and Underline
/// </summary>
public class PixataEditorButtonGroup : PixataEditorTool {
  public PixataEditorButtonGroup() {
  }

  public PixataEditorButtonGroup(params PixataEditorButtonTool[] tools) =>
    Tools = tools.ToList();

  public List<PixataEditorButtonTool> Tools { get; set; } = [];

  /// <summary>
  /// When false, every button in the group is disabled
  /// </summary>
  public bool Enabled { get; set; } = true;

  public string? Class { get; set; }
}

/// <summary>
/// Puts a tool of your own in the toolbar. Its content comes from the <see cref="PixataEditorCustomToolTemplate"/> with the
/// same <see cref="Name"/> in the editor's <see cref="PixataEditor.EditorCustomTools"/>
/// </summary>
public class PixataEditorCustomTool : PixataEditorTool {
  public PixataEditorCustomTool() {
  }

  public PixataEditorCustomTool(string name) =>
    Name = name;

  public string Name { get; set; } = "";
}

/// <summary>
/// An item in a <see cref="PixataEditorDropDownListTool"/>
/// </summary>
public class PixataEditorDropDownListItem {
  public PixataEditorDropDownListItem() {
  }

  public PixataEditorDropDownListItem(string text, string value) {
    Text = text;
    Value = value;
  }

  /// <summary>
  /// What the user sees
  /// </summary>
  public string Text { get; set; } = "";

  /// <summary>
  /// What is passed to the tool's command
  /// </summary>
  public string Value { get; set; } = "";
}

/// <summary>
/// The items the built-in drop-down tools show. Each property returns a new list, so you can change it without affecting
/// other editors
/// </summary>
public static class PixataEditorDropDownListToolItems {
  /// <summary>
  /// Paragraph and Heading 1 to Heading 6
  /// </summary>
  public static List<PixataEditorDropDownListItem> FormatItems => [
    new("Paragraph", "p"),
    new("Heading 1", "h1"),
    new("Heading 2", "h2"),
    new("Heading 3", "h3"),
    new("Heading 4", "h4"),
    new("Heading 5", "h5"),
    new("Heading 6", "h6")
  ];

  /// <summary>
  /// Common font families, each with fallbacks
  /// </summary>
  public static List<PixataEditorDropDownListItem> FontFamilyItems => [
    new("Arial", "Arial, \"Helvetica Neue\", Helvetica, sans-serif"),
    new("Courier New", "\"Courier New\", Courier, \"Lucida Sans Typewriter\", \"Lucida Typewriter\", monospace"),
    new("Georgia", "Georgia, Times, \"Times New Roman\", serif"),
    new("Impact", "Impact, Haettenschweiler, \"Arial Narrow Bold\", sans-serif"),
    new("Lucida Console", "\"Lucida Console\", Monaco, monospace"),
    new("Tahoma", "Tahoma, Verdana, Segoe, sans-serif"),
    new("Times New Roman", "\"Times New Roman\", Times, Baskerville, Georgia, serif"),
    new("Trebuchet MS", "\"Trebuchet MS\", \"Lucida Grande\", \"Lucida Sans Unicode\", \"Lucida Sans\", Tahoma, sans-serif"),
    new("Verdana", "Verdana, Geneva, sans-serif")
  ];

  /// <summary>
  /// The seven CSS size keywords, from xx-small (about 8pt) to xx-large (about 36pt)
  /// </summary>
  public static List<PixataEditorDropDownListItem> FontSizeItems => [
    new("1 (8pt)", "xx-small"),
    new("2 (10pt)", "x-small"),
    new("3 (12pt)", "small"),
    new("4 (14pt)", "medium"),
    new("5 (18pt)", "large"),
    new("6 (24pt)", "x-large"),
    new("7 (36pt)", "xx-large")
  ];
}
