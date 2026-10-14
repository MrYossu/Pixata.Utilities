namespace Pixata.Blazor.Components;

/// <summary>
/// The base for the arguments to <see cref="PixataEditor.ExecuteAsync"/>
/// </summary>
public abstract class PixataEditorToolCommandArgsBase {
  /// <summary>
  /// The command to run, one of the <see cref="PixataEditorCommands"/>
  /// </summary>
  public string CommandName { get; set; } = "";

  /// <summary>
  /// Whether the editor takes the focus after running the command. True by default
  /// </summary>
  public bool ShouldFocusView { get; set; } = true;
}

/// <summary>
/// Runs a command that needs no value, eg <see cref="PixataEditorCommands.Bold"/> or <see cref="PixataEditorCommands.Undo"/>
/// </summary>
public class PixataEditorToolCommandArgs : PixataEditorToolCommandArgsBase {
  public PixataEditorToolCommandArgs() {
  }

  public PixataEditorToolCommandArgs(string commandName) =>
    CommandName = commandName;
}

/// <summary>
/// Runs a command that takes a value, eg <see cref="PixataEditorCommands.Format"/> with <c>h2</c>, or
/// <see cref="PixataEditorCommands.ForeColor"/> with <c>#ff0000</c>
/// </summary>
public class PixataEditorFormatCommandArgs : PixataEditorToolCommandArgsBase {
  public PixataEditorFormatCommandArgs() {
  }

  public PixataEditorFormatCommandArgs(string commandName, string value) {
    CommandName = commandName;
    Value = value;
  }

  public string Value { get; set; } = "";
}

/// <summary>
/// Sets (<see cref="PixataEditorCommands.ViewHtml"/>) or inserts (<see cref="PixataEditorCommands.InsertHtml"/>) HTML. The
/// HTML is sanitised before it reaches the document
/// </summary>
public class PixataEditorHtmlCommandArgs : PixataEditorToolCommandArgsBase {
  public PixataEditorHtmlCommandArgs() {
  }

  public PixataEditorHtmlCommandArgs(string commandName, string value) {
    CommandName = commandName;
    Value = value;
  }

  public PixataEditorHtmlCommandArgs(string commandName, string value, bool inline) : this(commandName, value) =>
    Inline = inline;

  public string Value { get; set; } = "";

  /// <summary>
  /// For <see cref="PixataEditorCommands.InsertHtml"/>: true to insert the HTML inline, at the cursor, rather than as one or
  /// more blocks (paragraphs, lists...), which split the paragraph holding the cursor
  /// </summary>
  public bool Inline { get; set; }
}

/// <summary>
/// Turns the selection into a link, or inserts one at the cursor. Only relative, http, https, mailto and tel addresses are
/// accepted
/// </summary>
public class PixataEditorLinkCommandArgs : PixataEditorToolCommandArgsBase {
  public PixataEditorLinkCommandArgs() =>
    CommandName = PixataEditorCommands.CreateLink;

  public PixataEditorLinkCommandArgs(string href, string text) : this() {
    Href = href;
    Text = text;
  }

  public PixataEditorLinkCommandArgs(string href, string text, string title) : this(href, text) =>
    Title = title;

  public PixataEditorLinkCommandArgs(string href, string text, string target, string title) : this(href, text, title) =>
    Target = target;

  public string Href { get; set; } = "";

  /// <summary>
  /// The link's text. If it is empty, the selected text becomes the link, or the address is used when nothing is selected
  /// </summary>
  public string Text { get; set; } = "";

  /// <summary>
  /// <c>_blank</c> to open the link in a new window, or <c>_self</c>, <c>_parent</c> or <c>_top</c>
  /// </summary>
  public string Target { get; set; } = "";

  public string Title { get; set; } = "";
}

/// <summary>
/// Inserts an image. Only relative, http and https addresses, and base64 <c>data:image/...</c> URLs, are accepted
/// </summary>
public class PixataEditorImageCommandArgs : PixataEditorToolCommandArgsBase {
  public PixataEditorImageCommandArgs() =>
    CommandName = PixataEditorCommands.InsertImage;

  public PixataEditorImageCommandArgs(string src, string alt) : this() {
    Src = src;
    Alt = alt;
  }

  public PixataEditorImageCommandArgs(string src, string alt, string width, string height) : this(src, alt) {
    Width = width;
    Height = height;
  }

  public string Src { get; set; } = "";
  public string Alt { get; set; } = "";

  /// <summary>
  /// The width in pixels (eg <c>200</c> or <c>200px</c>) or as a percentage (<c>50%</c>)
  /// </summary>
  public string Width { get; set; } = "";

  /// <summary>
  /// The height in pixels (eg <c>200</c> or <c>200px</c>) or as a percentage (<c>50%</c>)
  /// </summary>
  public string Height { get; set; } = "";
}

/// <summary>
/// Inserts an empty table with the given number of rows and columns (at most 50 rows and 20 columns)
/// </summary>
public class PixataEditorTableCommandArgs : PixataEditorToolCommandArgsBase {
  public PixataEditorTableCommandArgs() =>
    CommandName = PixataEditorCommands.InsertTable;

  public PixataEditorTableCommandArgs(int rows, int cols) : this() {
    Rows = rows;
    Cols = cols;
  }

  public int Rows { get; set; }
  public int Cols { get; set; }
}
