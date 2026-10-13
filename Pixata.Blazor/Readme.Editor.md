# Editor

>Part of [Pixata.Blazor](Readme.md)

`PixataEditor` is a rich text editor whose value is HTML. It has a configurable toolbar (paragraph formats, fonts, colours, alignment, lists, links, images, tables, undo and redo, and a view of the HTML), works with or without an `EditForm`, cleans up what is pasted into it, and sanitises its HTML both when it is given a value and when it emits one. It works on touch screens and at phone widths, and can be used entirely from the keyboard.

```razor
<PixataEditor @bind-Value="_notes" />

@code {
  private string _notes = "<p>Hello <strong>world</strong></p>";
}
```

There is a [live demo](https://test.pixata.co.uk/EditorSample) on the sample site, and you can see [the source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/EditorSample.razor).

## Setting up

Add `@using Pixata.Blazor.Components` to your `_Imports.razor`, and the Pixata stylesheet to your host page or `App.razor`, if you haven't already...

```html
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

Nothing needs registering. The editor loads its script (`_content/Pixata.Blazor/pixata-editor.js`) itself the first time it is shown. It doesn't use anything server-only, so it works in Blazor Server, WebAssembly and Auto. While the page is being prerendered, the (sanitised) content is shown read-only in the editor's place, and the editor takes over once the page is interactive.

## Parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Value` / `ValueChanged` | `string` | | The HTML. Use `@bind-Value` for two-way binding |
| `Tools` | `IEnumerable<PixataEditorTool>?` | `PixataEditorToolSets.Default` | The tools in the toolbar, in order (see [Tools](#tools)). An empty list hides the toolbar |
| `EditMode` | `PixataEditorEditMode` | `Iframe` | `Iframe` puts the document in an iframe, so the page's styles don't affect it. `Div` puts it in a div, so it picks up the page's styles |
| `DebounceDelay` | `int` | `100` | How long (in milliseconds) after the user stops typing the value is updated. It is also updated straight away when the editor loses the focus |
| `ReadOnly` | `bool` | `false` | The content can be read, selected and copied but not changed, and the tools are disabled |
| `Enabled` | `bool` | `true` | When false, the editor can't be used at all |
| `Height` | `string?` | `"250px"` | The height of the whole editor, toolbar included, as a CSS length. The document scrolls inside it |
| `Width` | `string?` | | The width, as a CSS length. By default the editor fills its container |
| `Placeholder` | `string?` | | Text shown while the document is empty |
| `Adaptive` | `bool` | `false` | Keeps the toolbar on one line, moving the tools that don't fit into a menu at its end. When false, the toolbar wraps onto more lines |
| `SpellCheck` | `string?` | | The document's `spellcheck` attribute: `"true"`, `"false"`, or null to leave it to the browser |
| `AriaLabel` | `string?` | `"Editor"` | The document's accessible name |
| `AriaLabelledBy` | `string?` | | The id(s) of the element(s) that label the document, eg a `<label>` |
| `AriaDescribedBy` | `string?` | | The id(s) of the element(s) that describe the document, eg a hint |
| `EditorCustomTools` | `RenderFragment?` | | The content of your custom tools (see [Custom tools](#custom-tools)) |
| `EditorSettings` | `RenderFragment?` | | Settings for the editor, ie a `PixataEditorPasteSettings` (see [Pasting](#pasting)) |
| `Id` | `string?` | | The id of the document (in `Div` mode) or of the iframe holding it. To label the editor, use `AriaLabelledBy` rather than `<label for>` |
| `Class` | `string?` | | Extra CSS classes for the editor's outer element. Any other attributes you add go on that element too |

## Events

| Event | Argument | Raised when |
| --- | --- | --- |
| `ValueChanged` | `string` | The HTML changes, after `DebounceDelay` |
| `OnChange` | `string` | The editor loses the focus, if the value changed while it had it |
| `OnBlur` | | The document loses the focus |

## Methods

Get a reference to the editor with `@ref` to call these.

| Method | Description |
| --- | --- |
| `ExecuteAsync(PixataEditorToolCommandArgsBase args)` | Runs a command, as if the user had used a tool (see below) |
| `FocusAsync()` | Puts the focus in the document |

```razor
<PixataEditor @ref="_editor" @bind-Value="_html" />
<button @onclick="AddSignature">Sign</button>

@code {
  private PixataEditor _editor = null!;
  private string _html = "";

  private Task AddSignature() =>
    _editor.ExecuteAsync(new PixataEditorHtmlCommandArgs(PixataEditorCommands.InsertHtml, "<p>Kind regards,<br>Fred</p>"));
}
```

The commands are constants in `PixataEditorCommands`, and each takes one of these argument classes...

| Arguments | Commands |
| --- | --- |
| `PixataEditorToolCommandArgs(command)` | `Bold`, `Italic`, `Underline`, `Strikethrough`, `SubScript`, `SuperScript`, `AlignLeft`, `AlignCenter`, `AlignRight`, `AlignJustify`, `UnorderedList`, `OrderedList`, `Indent`, `Outdent`, `Unlink`, `AddColumnBefore`, `AddColumnAfter`, `AddRowBefore`, `AddRowAfter`, `DeleteColumn`, `DeleteRow`, `DeleteTable`, `MergeCells`, `SplitCell`, `CleanFormatting`, `Undo`, `Redo`, `SelectAll` |
| `PixataEditorFormatCommandArgs(command, value)` | `Format` (`p` or `h1` to `h6`), `FontFamily` and `FontSize` (CSS values), `ForeColor` and `BackgroundColor` (CSS colours). For the last four, `""` removes the formatting |
| `PixataEditorHtmlCommandArgs(command, html, inline)` | `InsertHtml` inserts HTML at the cursor (set `inline` to insert it within the current paragraph), `ViewHtml` replaces the whole document. Unlike setting `Value`, both can be undone |
| `PixataEditorLinkCommandArgs(href, text, target, title)` | `CreateLink`. With no text, the selection becomes the link |
| `PixataEditorImageCommandArgs(src, alt, width, height)` | `InsertImage` |
| `PixataEditorTableCommandArgs(rows, cols)` | `InsertTable` |

Every argument class has `ShouldFocusView` (true by default), which says whether the document takes the focus after the command has run. Any HTML, link address or style value passed to a command is checked and sanitised in the same way as the value.

## Tools

`PixataEditorToolSets.Default` holds the everyday tools: bold, italic and underline, the paragraph format, alignment, lists and indentation, links and an image. `PixataEditorToolSets.All` adds undo and redo, strikethrough, subscript and superscript, the font, its size and colours, tables, clearing the formatting, and viewing the HTML. Each returns a new list, so you can change it without affecting other editors.

```razor
<PixataEditor @bind-Value="_html" Tools="PixataEditorToolSets.All" />
```

To choose your own tools, pass a list of them. Buttons that belong together go in a `PixataEditorButtonGroup`, which draws them with shared borders...

```razor
<PixataEditor @bind-Value="_html" Tools="_tools" />

@code {
  private readonly List<PixataEditorTool> _tools = [
    new PixataEditorButtonGroup(new PixataEditorBold(), new PixataEditorItalic(), new PixataEditorUnderline()),
    new PixataEditorFormat { Data = [new("Paragraph", "p"), new("Title", "h2"), new("Subtitle", "h3")] },
    new PixataEditorForeColor { Colors = ["#000000", "#c00000", "#0070c0"], Columns = 3 },
    new PixataEditorButtonGroup(new PixataEditorUndo(), new PixataEditorRedo()),
    new PixataEditorButtonTool { CommandName = PixataEditorCommands.SelectAll, Title = "Select all", Icon = "fas fa-object-group" }
  ];
}
```

These are the built-in tools...

| Group | Tools |
| --- | --- |
| Text | `PixataEditorBold`, `PixataEditorItalic`, `PixataEditorUnderline`, `PixataEditorStrikethrough`, `PixataEditorSubScript`, `PixataEditorSuperScript`, `PixataEditorCleanFormatting` |
| Drop-down lists | `PixataEditorFormat`, `PixataEditorFontFamily`, `PixataEditorFontSize` |
| Colours | `PixataEditorForeColor`, `PixataEditorBackgroundColor` |
| Paragraphs | `PixataEditorAlignLeft`, `PixataEditorAlignCenter`, `PixataEditorAlignRight`, `PixataEditorAlignJustify`, `PixataEditorUnorderedList`, `PixataEditorOrderedList`, `PixataEditorIndent`, `PixataEditorOutdent` |
| Links and images | `PixataEditorCreateLink`, `PixataEditorUnlink`, `PixataEditorInsertImage` |
| Tables | `PixataEditorInsertTable`, `PixataEditorAddColumnBefore`, `PixataEditorAddColumnAfter`, `PixataEditorAddRowBefore`, `PixataEditorAddRowAfter`, `PixataEditorDeleteColumn`, `PixataEditorDeleteRow`, `PixataEditorDeleteTable`, `PixataEditorMergeCells`, `PixataEditorSplitCell` |
| Other | `PixataEditorUndo`, `PixataEditorRedo`, `PixataEditorViewHtml` |

Every tool's properties can be changed, eg `new PixataEditorBold { Title = "Gras" }`. These are the kinds of tool, if you want to make your own...

| Class | Properties |
| --- | --- |
| `PixataEditorButtonTool` | `CommandName`, `Title` (the tooltip and accessible name), `Icon` (a `PixataSvgIcon`, or the CSS classes of an icon font), `ChildContent` (shown instead of the icon), `Class`, `Enabled` |
| `PixataEditorToggleButtonTool` | A button that is shown pressed when its command applies to the selection, eg bold text |
| `PixataEditorDialogTool` | A button that opens the link, image or HTML dialog. Adds `DialogTitle` and `DialogWidth` |
| `PixataEditorDropDownListTool` | `CommandName`, `Title`, `Data` (a list of `PixataEditorDropDownListItem(text, value)`; `PixataEditorDropDownListToolItems` has the built-in lists), `DefaultText` (shown when the selection doesn't match an item), `Width`, `PopupWidth`, `PopupHeight`, `Class`, `Enabled` |
| `PixataEditorColorTool` | `CommandName`, `Title`, `Icon`, `Colors` (CSS colours), `Columns`, `Value` (the colour shown before one is chosen), `Class`, `Enabled` |
| `PixataEditorInsertTable` | Adds `Rows` and `Columns`, the size of the grid you choose the table's size from (8 by 8 by default) |
| `PixataEditorButtonGroup` | `Tools`, `Enabled`, `Class` |

The editor disables tools that can't do anything at the moment (eg Undo when there is nothing to undo, or the table tools when the cursor isn't in a table), and shows the pressed state, format, font, size and colours of the selection.

## Custom tools

To put something of your own in the toolbar, add a `PixataEditorCustomTool` with a name to the `Tools` list, and put a `PixataEditorCustomToolTemplate` with the same name, holding its content, in `EditorCustomTools`. The content can be anything, and usually calls `ExecuteAsync()`...

```razor
<PixataEditor @ref="_editor" @bind-Value="_html" Tools="_tools">
  <EditorCustomTools>
    <PixataEditorCustomToolTemplate Name="InsertDate">
      <button type="button" class="pixata-blazor-button pixata-blazor-button-md pixata-blazor-rounded-md pixata-blazor-button-solid pixata-blazor-button-base"
              @onclick="InsertDate">Insert date</button>
    </PixataEditorCustomToolTemplate>
  </EditorCustomTools>
</PixataEditor>

@code {
  private PixataEditor _editor = null!;
  private string _html = "";

  private readonly List<PixataEditorTool> _tools = [
    new PixataEditorButtonGroup(new PixataEditorBold(), new PixataEditorItalic()),
    new PixataEditorCustomTool("InsertDate")
  ];

  private Task InsertDate() =>
    _editor.ExecuteAsync(new PixataEditorHtmlCommandArgs(PixataEditorCommands.InsertHtml, DateTime.Today.ToLongDateString(), true));
}
```

Buttons, inputs and links in a custom tool join the toolbar's keyboard navigation automatically.

## Pasting

What is pasted is cleaned before it reaches the document. Put a `PixataEditorPasteSettings` in `EditorSettings` to control how...

```razor
<PixataEditor @bind-Value="_html">
  <EditorSettings>
    <PixataEditorPasteSettings RemoveAttributes="@(["style"])" StripTags="@(["span"])" />
  </EditorSettings>
</PixataEditor>
```

| Setting | Default | Description |
| --- | --- | --- |
| `ConvertMsLists` | `true` | Turns the list paragraphs Microsoft Word pastes into real bulleted and numbered lists |
| `RemoveMsClasses` | `true` | Removes the `Mso...` classes Microsoft Office adds |
| `RemoveMsStyles` | `true` | Removes the `mso-...` style properties Microsoft Office adds |
| `RemoveHtmlComments` | `false` | Removes HTML comments (they never reach the document anyway) |
| `RemoveAllAttributes` | `false` | Removes every attribute, including link addresses and image sources |
| `RemoveAttributes` | | The attributes to remove, eg `["style"]` to drop pasted colours, fonts and alignment |
| `StripTags` | | The tags to remove, keeping their content, eg `["span"]` |

Whatever these settings say, pasted HTML is also sanitised, so nothing unsafe gets in.

## Sanitising

The editor makes sure no script can get into or out of it...

- In the browser, the document can only hold the elements, attributes and styles the editor knows about (paragraphs, headings, lists, tables, links, images, bold, italic, colours and so on), so anything else in a value, a command or a paste (`<script>`, `<iframe>`, `onclick` and other event handlers, `style` blocks, forms and so on) is dropped as it is read. Link and image addresses are only kept if they are relative or use `http`, `https`, `mailto` or `tel` (images can also be base64 `data:image` URLs), and style values can't hold `url()`, `expression()`, escapes or anything else that could load or run something.
- In .NET, the value you give the editor and the value it emits both go through `PixataHtmlSanitiser`, which applies the same rules. This matters in Blazor Server, where the HTML comes from the browser and can't be trusted.

The value is sanitised as the editor emits it, so if you give it unsafe HTML, the bound value only changes (to the sanitised HTML) once the user edits it. You can use `PixataHtmlSanitiser` yourself, eg to sanitise HTML on the server before saving it, or before showing it with a `MarkupString`...

```razor
<div>@((MarkupString)PixataHtmlSanitiser.Sanitise(comment.Html))</div>
```

| Method | Description |
| --- | --- |
| `PixataHtmlSanitiser.Sanitise(string? html)` | Returns safe HTML, keeping the formatting and removing everything else. Unknown elements are removed but their text is kept, and `script`, `style`, `iframe`, `object`, `svg` and the like are removed with their content |
| `PixataHtmlSanitiser.SafeUrl(string? url, bool allowDataImage = false)` | Returns the URL if it is safe in a link (or, with `allowDataImage`, an image), otherwise null |
| `PixataHtmlSanitiser.SafeCssValue(string? value)` | Returns the CSS value if it is safe in a `style` attribute, otherwise null |

## Edit modes

In `Iframe` mode (the default) the document is in an iframe, so your page's styles can't change how the content looks. The editor copies its own content styles (every CSS rule that mentions `pixata-blazor-editor-document`) and the `--pixata-blazor-*` custom properties they use into the iframe, and uses your page's font, so the content looks the same in both modes. In `Div` mode the document is part of the page and picks up its styles.

## Keyboard and accessibility

The toolbar is a single tab stop with `role="toolbar"`: the left and right arrow keys move between the tools, Home and End go to the first and last, and Esc goes back to the document. Alt+F10 in the document moves to the toolbar. The format, font and size tools are combo boxes (the up and down arrow keys or Alt+Down open the list, Enter chooses), and the colour and table tools open grids you move around with the arrow keys. The dialogs are modal, keep the focus inside them, and close with Esc.

| Keys | In the document |
| --- | --- |
| Ctrl+B, Ctrl+I, Ctrl+U | Bold, italic, underline |
| Ctrl+Z, Ctrl+Y (or Ctrl+Shift+Z) | Undo, redo |
| Shift+Enter | A line break within the paragraph |
| Tab, Shift+Tab | In a list, indent or outdent the item. In a table, go to the next or previous cell |
| Ctrl+], Ctrl+[ | Indent or outdent a list item |
| Alt+F10 | Move to the toolbar |

(On a Mac, use Cmd for Ctrl.)

On touch screens, the tools, list items and swatches get bigger targets, the drop-downs stay inside the screen, and the dialogs fit the width of a phone. On a narrow screen, either let the toolbar wrap (the default) or set `Adaptive="true"` to keep it to one line.

## Restyling

The editor uses the shared `--pixata-blazor-*` custom properties (see [Styling](Readme.Styling.md)), plus these, which you can override on `:root` or on any container...

| Property | Default | Used for |
| --- | --- | --- |
| `--pixata-blazor-editor-bg` | the input background | The document's background |
| `--pixata-blazor-editor-toolbar-bg` | the alternative surface colour | The toolbar's background |
| `--pixata-blazor-editor-content-padding` | `.5rem .75rem` | The space around the document's content |
| `--pixata-blazor-editor-link-color` | `#0058e9` | Links in the document |
| `--pixata-blazor-editor-table-border-color` | `#c6c6c6` | Table cell borders and horizontal rules |
| `--pixata-blazor-editor-selected-cell-bg` | a light blue | Selected table cells |
| `--pixata-blazor-editor-selected-node-outline` | `2px solid #8cb2f7` | A selected image |
| `--pixata-blazor-editor-code-bg` | `#f5f5f5` | Code blocks |
| `--pixata-blazor-editor-quote-border-color` | `#d6d6d6` | Block quotes |
| `--pixata-blazor-editor-dialog-width` | `400px` | The link and image dialogs |

The main classes are...

| Class | Element |
| --- | --- |
| `pixata-blazor-editor` | The outer element, with `pixata-blazor-editor-iframe-mode` or `pixata-blazor-editor-div-mode`, and `pixata-blazor-editor-readonly`, `pixata-blazor-disabled` and `pixata-blazor-invalid` for those states |
| `pixata-blazor-editor-toolbar` | The toolbar (with `pixata-blazor-editor-toolbar-adaptive` when `Adaptive` is set), holding a `pixata-blazor-editor-toolbar-item` for each tool |
| `pixata-blazor-editor-tool` | Every toolbar button, with `pixata-blazor-editor-tool-<command>`, eg `pixata-blazor-editor-tool-bold` |
| `pixata-blazor-editor-dropdown` | The format, font and size drop-downs |
| `pixata-blazor-editor-colour-tool`, `pixata-blazor-editor-palette` | The colour tools and their palettes |
| `pixata-blazor-editor-table-picker` | The grid for choosing the size of a new table |
| `pixata-blazor-editor-toolbar-overflow`, `pixata-blazor-editor-overflow-tools` | The overflow menu's button and its tools |
| `pixata-blazor-editor-content` | The area holding the document |
| `pixata-blazor-editor-document` | The document itself. Style the content with rules that mention this class, eg `.pixata-blazor-editor-document h1 { color: navy; }`, and they apply in both edit modes |
| `pixata-blazor-editor-placeholder` | The empty paragraph showing the placeholder |
| `pixata-blazor-editor-dialog` | The dialogs, with `pixata-blazor-editor-dialog-link`, `-image` or `-html` |

## How it works

The editing itself is done in the browser by [ProseMirror](https://prosemirror.net/), an open-source (MIT) toolkit for rich text editors, bundled with the editor's own script into `wwwroot/pixata-editor.js`. Its schema is what limits the document to safe content. The source of the script is in [build/pixata-editor](https://github.com/MrYossu/Pixata.Utilities/tree/master/build/pixata-editor), which also explains how to rebuild it.
