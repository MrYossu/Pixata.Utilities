using System.Globalization;
using System.Text;

namespace Pixata.Pdf.Documents;

/// <summary>
/// Reads an RTF document, as written by Word, WordPad or LibreOffice, into paragraphs, lists, tables and pictures. Headers,
/// footers, footnotes, comments and drawn shapes are left out. Tables inside tables have their cells' text put in the outer cell
/// </summary>
internal sealed class RtfReader {
  private readonly string _rtf;
  private int _position;
  private readonly List<DocBlock> _blocks = [];
  private State _state = new();
  private readonly Stack<State> _stack = [];

  // The document's tables of fonts, colours and paragraph styles
  private readonly Dictionary<int, int> _fontCodePages = [];
  private readonly HashSet<int> _symbolFonts = [];
  private readonly Dictionary<int, string> _fontNames = [];
  private readonly List<(byte Red, byte Green, byte Blue)?> _colours = [];
  private readonly Dictionary<int, string> _styleNames = [];
  private int _defaultCodePage = 1252;
  private int _defaultFont = -1;

  // What's being read
  private DocParagraph _paragraph = new();
  private string? _listMarker;
  private int _skip;
  private (int Red, int Green, int Blue) _colour = (0, 0, 0);
  private bool _colourGiven;
  private readonly StringBuilder _text = new();
  private int _styleNumber;

  // The last paragraph read, for spacing that's left out between paragraphs of the same style
  private DocParagraph? _lastParagraph;
  private int _lastStyle;
  private bool _lastContextual;

  // The table being read
  private DocTable? _table;
  private DocRow _row = new();
  private DocCell _cell = new();
  private readonly List<int> _cellEdges = [];
  private int _rowLeft;
  private bool _rowBorders;

  private RtfReader(string rtf) {
    _rtf = rtf;
  }

  public static List<DocBlock> Read(byte[] file) {
    try {
      return new RtfReader(Encoding.Latin1.GetString(file)).Parse();
    }
    catch (Exception ex) when (ex is not OutOfMemoryException and not DocumentToPdfException) {
      throw new DocumentToPdfException($"The RTF document couldn't be read: {ex.Message}", ex);
    }
  }

  private enum Destination {
    Text,
    Skip,
    FontTable,
    ColourTable,
    StyleSheet,
    ListText,
    Picture
  }

  // What a group can change, which is put back when the group ends
  private sealed record State {
    public Destination Destination { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool Strike { get; init; }
    public bool Hidden { get; init; }
    public int FontSize { get; init; }
    public int Colour { get; init; }
    public int Font { get; init; } = -1;
    public int UnicodeSkip { get; init; } = 1;
    public DocAlignment Alignment { get; init; }
    public int LeftIndent { get; init; }
    public int FirstIndent { get; init; }
    public int SpaceBefore { get; init; }
    public int SpaceAfter { get; init; }
    public double LineSpacing { get; init; } = 1;
    public bool InTable { get; init; }
    public int Style { get; init; }
    public int OutlineLevel { get; init; } = -1;
    public bool RightToLeft { get; init; }
    public bool List { get; init; }
    public bool Contextual { get; init; }

    // The picture being read
    public PictureState? Picture { get; init; }
  }

  private sealed class PictureState {
    public string Kind { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int GoalWidth { get; set; }
    public int GoalHeight { get; set; }
    public int ScaleX { get; set; } = 100;
    public int ScaleY { get; set; } = 100;
    public StringBuilder Hex { get; } = new();
    public List<byte> Binary { get; } = [];
  }

  private State ResetCharacter(State state) =>
    state with { Bold = false, Italic = false, Underline = false, Strike = false, Hidden = false, FontSize = 0, Colour = 0, Font = _defaultFont };

  private static State ResetParagraph(State state) =>
    state with {
      Alignment = DocAlignment.Left, LeftIndent = 0, FirstIndent = 0, SpaceBefore = 0, SpaceAfter = 0, LineSpacing = 1, InTable = false,
      Style = 0, OutlineLevel = -1, RightToLeft = false, List = false, Contextual = false
    };

  private List<DocBlock> Parse() {
    while (_position < _rtf.Length) {
      char c = _rtf[_position];
      switch (c) {
        case '{':
          _position++;
          _stack.Push(_state);
          _skip = 0;
          // {\* starts a part that a reader can skip if it doesn't know it. The only one wanted is a picture
          if (Peek(@"\*")) {
            _position += 2;
            string word = PeekWord();
            if (word is not "shppict") {
              _state = _state with { Destination = Destination.Skip };
            }
          }
          break;
        case '}':
          _position++;
          EndGroup();
          _skip = 0;
          break;
        case '\\':
          ControlWord();
          break;
        case '\r' or '\n':
          _position++;
          break;
        default:
          _position++;
          Character(c, false);
          break;
      }
    }
    EndParagraph(false);
    CloseTable();
    return _blocks;
  }

  private bool Peek(string text) =>
    string.CompareOrdinal(_rtf, _position, text, 0, text.Length) == 0;

  private string PeekWord() {
    int start = _position + 1;
    int end = start;
    while (end < _rtf.Length && char.IsAsciiLetter(_rtf[end])) {
      end++;
    }
    return _position < _rtf.Length && _rtf[_position] == '\\' ? _rtf[start..end] : "";
  }

  private void EndGroup() {
    State ending = _state;
    _state = _stack.Count > 0 ? _stack.Pop() : new();
    switch (ending.Destination) {
      case Destination.Picture when _state.Destination != Destination.Picture && ending.Picture is { } picture:
        AddPicture(picture);
        break;
      case Destination.ListText when _state.Destination != Destination.ListText:
        _listMarker = Marker(_text.ToString());
        _text.Clear();
        break;
      case Destination.StyleSheet when _state.Destination == Destination.StyleSheet:
        // The end of a style's definition, whose name is its text
        string name = _text.ToString().Trim().TrimEnd(';').Trim();
        if (name.Length > 0) {
          _styleNames[_styleNumber] = name;
        }
        _text.Clear();
        _styleNumber = 0;
        break;
    }
  }

  // A list's bullet or number, as the document wrote it before the item. Bullets in the Symbol font are private-use characters
  private static string? Marker(string text) {
    string marker = text.Replace('\t', ' ').Trim();
    if (marker.Length == 0) {
      return null;
    }
    return marker.Length == 1 && (marker[0] is >= '\uf000' and <= '\uf0ff' || marker[0] == '·') ? marker[0] switch {
      '\uf06f' => "◦",
      '\uf0a7' => "▪",
      _ => "•"
    } : marker;
  }

  private void ControlWord() {
    _position++;
    if (_position >= _rtf.Length) {
      return;
    }
    char symbol = _rtf[_position];
    if (!char.IsAsciiLetter(symbol)) {
      _position++;
      switch (symbol) {
        case '\'':
          if (_position + 2 <= _rtf.Length && byte.TryParse(_rtf.AsSpan(_position, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value)) {
            _position += 2;
            Character((char)value, true);
          }
          break;
        case '~':
          Text("\u00a0");
          break;
        case '_':
          Text("\u2011");
          break;
        case '\\' or '{' or '}':
          Character(symbol, false);
          break;
        case '\r' or '\n':
          // A backslash before a line break is a paragraph break, as \par is
          EndParagraph(true);
          break;
      }
      return;
    }
    int start = _position;
    while (_position < _rtf.Length && char.IsAsciiLetter(_rtf[_position])) {
      _position++;
    }
    string word = _rtf[start.._position];
    int? parameter = null;
    int numberStart = _position;
    if (_position < _rtf.Length && (_rtf[_position] == '-' || char.IsAsciiDigit(_rtf[_position]))) {
      _position++;
      while (_position < _rtf.Length && char.IsAsciiDigit(_rtf[_position])) {
        _position++;
      }
      if (int.TryParse(_rtf.AsSpan(numberStart, _position - numberStart), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)) {
        parameter = number;
      }
    }
    // A space after a control word is part of it
    if (_position < _rtf.Length && _rtf[_position] == ' ') {
      _position++;
    }
    Word(word, parameter);
  }

  private void Word(string word, int? parameter) {
    int value = parameter ?? 1;
    bool on = parameter is null or not 0;
    // The parts of the document that aren't its text
    switch (word) {
      case "fonttbl":
        _state = _state with { Destination = Destination.FontTable };
        return;
      case "colortbl":
        _state = _state with { Destination = Destination.ColourTable };
        _colours.Clear();
        return;
      case "stylesheet":
        _state = _state with { Destination = Destination.StyleSheet };
        return;
      case "listtext" or "pntext":
        _state = _state with { Destination = Destination.ListText };
        _text.Clear();
        return;
      case "pict":
        _state = _state with { Destination = Destination.Picture, Picture = new() };
        return;
      case "info" or "header" or "headerl" or "headerr" or "headerf" or "footer" or "footerl" or "footerr" or "footerf" or "footnote"
        or "annotation" or "nonshppict" or "fldinst" or "listtable" or "listoverridetable" or "revtbl" or "rsidtbl" or "xmlnstbl" or "pn"
        or "object" or "shp" or "shpinst" or "datafield" or "themedata" or "colorschememapping" or "latentstyles" or "datastore"
        or "mmathPr" or "generator" or "bkmkstart" or "bkmkend" or "xe" or "tc" or "txe" or "pgdsctbl" or "filetbl" or "docvar":
        _state = _state with { Destination = Destination.Skip };
        return;
    }
    switch (_state.Destination) {
      case Destination.Skip:
        return;
      case Destination.FontTable:
        FontTableWord(word, value);
        return;
      case Destination.ColourTable:
        switch (word) {
          case "red":
            _colour = (value, _colour.Green, _colour.Blue);
            _colourGiven = true;
            break;
          case "green":
            _colour = (_colour.Red, value, _colour.Blue);
            _colourGiven = true;
            break;
          case "blue":
            _colour = (_colour.Red, _colour.Green, value);
            _colourGiven = true;
            break;
        }
        return;
      case Destination.StyleSheet:
        if (word is "s" or "cs" or "ds" or "ts") {
          _styleNumber = word == "s" ? value : -1;
        }
        return;
      case Destination.Picture:
        PictureWord(word, value);
        return;
    }
    switch (word) {
      // The document
      case "ansicpg":
        _defaultCodePage = value;
        break;
      case "deff":
        _defaultFont = value;
        _state = _state with { Font = value };
        break;
      case "uc":
        _state = _state with { UnicodeSkip = Math.Max(0, value) };
        break;
      case "u":
        int code = value < 0 ? value + 65536 : value;
        // Bullets and Greek letters in the Symbol font come as private-use characters, which are only kept as they are for a list's
        // marker, which is worked out when it's ended
        Text(code is >= 0xf020 and <= 0xf0ff && _state.Destination != Destination.ListText && _symbolFonts.Contains(_state.Font)
          ? Symbols.Map(code, _fontNames.GetValueOrDefault(_state.Font)) : ((char)code).ToString());
        _skip = _state.UnicodeSkip;
        break;
      // Characters
      case "plain":
        _state = ResetCharacter(_state);
        break;
      case "b":
        _state = _state with { Bold = on };
        break;
      case "i":
        _state = _state with { Italic = on };
        break;
      case "ul" or "uld" or "uldash" or "uldashd" or "uldashdd" or "uldb" or "ulhwave" or "ulldash" or "ulth" or "ulthd" or "ulthdash"
        or "ulthdashd" or "ulthdashdd" or "ulthldash" or "ululdbwave" or "ulw" or "ulwave":
        _state = _state with { Underline = on };
        break;
      case "ulnone":
        _state = _state with { Underline = false };
        break;
      case "strike" or "striked":
        _state = _state with { Strike = on };
        break;
      case "v":
        _state = _state with { Hidden = on };
        break;
      case "fs":
        _state = _state with { FontSize = value };
        break;
      case "cf":
        _state = _state with { Colour = value };
        break;
      case "f":
        _state = _state with { Font = value };
        break;
      // Special characters
      case "line" or "column":
        if (_state.Destination == Destination.Text) {
          _paragraph.Inlines.Add(new DocLineBreak());
        }
        break;
      case "tab":
        if (_state.Destination == Destination.Text) {
          _paragraph.Inlines.Add(new DocTab(CurrentStyle()));
        } else {
          Text("\t");
        }
        break;
      case "emdash":
        Text("—");
        break;
      case "endash":
        Text("–");
        break;
      case "bullet":
        Text("•");
        break;
      case "lquote":
        Text("‘");
        break;
      case "rquote":
        Text("’");
        break;
      case "ldblquote":
        Text("“");
        break;
      case "rdblquote":
        Text("”");
        break;
      case "emspace" or "enspace" or "qmspace":
        Text(" ");
        break;
      // Paragraphs
      case "par":
        EndParagraph(true);
        break;
      case "sect":
        EndParagraph(false);
        break;
      case "page":
        EndParagraph(false);
        AddBlock(new DocPageBreak());
        break;
      case "pagebb":
        if (!_state.InTable) {
          EndParagraph(false);
          AddBlock(new DocPageBreak());
        }
        break;
      case "pard":
        _state = ResetParagraph(_state);
        break;
      case "ql":
        _state = _state with { Alignment = DocAlignment.Left };
        break;
      case "qc":
        _state = _state with { Alignment = DocAlignment.Centre };
        break;
      case "qr":
        _state = _state with { Alignment = DocAlignment.Right };
        break;
      case "qj" or "qd":
        _state = _state with { Alignment = DocAlignment.Justify };
        break;
      case "li" or "lin":
        _state = _state with { LeftIndent = value };
        break;
      case "fi":
        _state = _state with { FirstIndent = value };
        break;
      case "sb":
        _state = _state with { SpaceBefore = value };
        break;
      case "sa":
        _state = _state with { SpaceAfter = value };
        break;
      case "sl":
        _state = _state with { LineSpacing = value > 0 ? value / 240.0 : 1 };
        break;
      case "slmult":
        if (value == 0) {
          _state = _state with { LineSpacing = 1 };
        }
        break;
      case "s":
        _state = _state with { Style = value };
        break;
      case "outlinelevel":
        _state = _state with { OutlineLevel = value };
        break;
      case "contextualspace":
        _state = _state with { Contextual = true };
        break;
      case "rtlpar":
        _state = _state with { RightToLeft = true };
        break;
      case "ltrpar":
        _state = _state with { RightToLeft = false };
        break;
      case "ls" or "ilvl" or "pnlvlblt" or "pnlvlbody":
        _state = _state with { List = true };
        break;
      // Tables
      case "intbl":
        _state = _state with { InTable = true };
        break;
      case "trowd":
        _cellEdges.Clear();
        _rowLeft = 0;
        _rowBorders = false;
        break;
      case "trleft":
        _rowLeft = value;
        break;
      case "cellx":
        _cellEdges.Add(value);
        break;
      case "brdrs" or "brdrth" or "brdrdb" or "brdrdot" or "brdrdash" or "brdrsh":
        _rowBorders = true;
        break;
      case "cell":
        EndCell();
        break;
      case "nestcell" or "nestrow":
        EndParagraph(false);
        break;
      case "row":
        EndRow();
        break;
    }
  }

  private void FontTableWord(string word, int value) {
    switch (word) {
      case "f":
        _state = _state with { Font = value };
        _text.Clear();
        break;
      case "fcharset":
        if (value == 2) {
          _symbolFonts.Add(_state.Font);
        } else if (CodePage(value) is int codePage) {
          _fontCodePages[_state.Font] = codePage;
        }
        break;
    }
  }

  private static int? CodePage(int charset) =>
    charset switch {
      0 => 1252,
      77 => 10000,
      128 => 932,
      129 => 949,
      134 => 936,
      136 => 950,
      161 => 1253,
      162 => 1254,
      163 => 1258,
      177 => 1255,
      178 => 1256,
      186 => 1257,
      204 => 1251,
      222 => 874,
      238 => 1250,
      _ => null
    };

  private void PictureWord(string word, int value) {
    PictureState picture = _state.Picture!;
    switch (word) {
      case "pngblip":
        picture.Kind = "png";
        break;
      case "jpegblip":
        picture.Kind = "jpeg";
        break;
      case "emfblip" or "wmetafile" or "macpict" or "dibitmap" or "wbitmap" or "pmmetafile":
        picture.Kind = word;
        break;
      case "picw":
        picture.Width = value;
        break;
      case "pich":
        picture.Height = value;
        break;
      case "picwgoal":
        picture.GoalWidth = value;
        break;
      case "pichgoal":
        picture.GoalHeight = value;
        break;
      case "picscalex":
        picture.ScaleX = value;
        break;
      case "picscaley":
        picture.ScaleY = value;
        break;
      case "bin":
        // The picture's bytes as they are, rather than in hexadecimal
        for (int index = 0; index < value && _position < _rtf.Length; index++) {
          picture.Binary.Add((byte)_rtf[_position++]);
        }
        break;
    }
  }

  private void AddPicture(PictureState picture) {
    if (picture.Kind is not ("png" or "jpeg")) {
      return;
    }
    byte[] data = picture.Binary.Count > 0 ? [.. picture.Binary] : Hex(picture.Hex.ToString());
    if (Images.Read(data) is not { } image) {
      return;
    }
    if (picture.GoalWidth > 0 && picture.GoalHeight > 0) {
      image.Width = picture.GoalWidth / 20.0 * picture.ScaleX / 100;
      image.Height = picture.GoalHeight / 20.0 * picture.ScaleY / 100;
    }
    image.Alignment = _state.Alignment;
    // A picture is a block of its own, so the text before it in its paragraph goes first
    if (_paragraph.Inlines.Count > 0) {
      EndParagraph(false);
    }
    AddBlock(image);
  }

  private static byte[] Hex(string hex) {
    List<byte> bytes = new(hex.Length / 2);
    int high = -1;
    foreach (char c in hex) {
      int digit = c switch {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
      };
      if (digit < 0) {
        continue;
      }
      if (high < 0) {
        high = digit;
      } else {
        bytes.Add((byte)(high << 4 | digit));
        high = -1;
      }
    }
    return [.. bytes];
  }

  // A character of text. One from \'hh is a byte in the font's code page, as is one typed in the RTF above 127
  private void Character(char c, bool fromHex) {
    switch (_state.Destination) {
      case Destination.Skip:
        return;
      case Destination.Picture:
        _state.Picture!.Hex.Append(c);
        return;
      case Destination.FontTable:
        if (c == ';') {
          string fontName = _text.ToString().Trim();
          _fontNames[_state.Font] = fontName;
          if (Symbols.IsSymbolFont(fontName)) {
            _symbolFonts.Add(_state.Font);
          }
          _text.Clear();
        } else if (c >= ' ') {
          _text.Append(c);
        }
        return;
      case Destination.ColourTable:
        if (c == ';') {
          _colours.Add(_colourGiven ? ((byte)_colour.Red, (byte)_colour.Green, (byte)_colour.Blue) : null);
          _colour = (0, 0, 0);
          _colourGiven = false;
        }
        return;
    }
    if (_skip > 0) {
      _skip--;
      return;
    }
    if (c < ' ' && !fromHex) {
      return;
    }
    string text;
    if (_symbolFonts.Contains(_state.Font) && c > ' ') {
      text = Symbols.Map(c, _fontNames.GetValueOrDefault(_state.Font));
    } else if (c >= 128 || fromHex) {
      int codePage = _fontCodePages.TryGetValue(_state.Font, out int fontCodePage) ? fontCodePage : _defaultCodePage;
      Encoding encoding = PlainTextReader.GetEncoding(codePage) ?? PlainTextReader.Windows1252;
      text = encoding.GetString([(byte)c]);
    } else {
      text = c.ToString();
    }
    Text(text);
  }

  private void Text(string text) {
    switch (_state.Destination) {
      case Destination.Skip or Destination.Picture or Destination.FontTable or Destination.ColourTable:
        return;
      case Destination.ListText or Destination.StyleSheet:
        _text.Append(text);
        return;
    }
    if (_state.Hidden) {
      return;
    }
    DocStyle style = CurrentStyle();
    List<DocInline> inlines = _paragraph.Inlines;
    if (inlines.Count > 0 && inlines[^1] is DocText last && last.Style == style) {
      inlines[^1] = new DocText(last.Text + text, style);
    } else {
      inlines.Add(new DocText(text, style));
    }
  }

  private DocStyle CurrentStyle() =>
    new(_state.Bold, _state.Italic, _state.Underline, _state.Strike, _state.FontSize > 0 ? _state.FontSize / 2.0 : null,
        _state.Colour > 0 && _state.Colour < _colours.Count ? _colours[_state.Colour] is (0, 0, 0) ? null : _colours[_state.Colour] : null);

  // ---------- Paragraphs and tables ----------

  private void EndParagraph(bool always) {
    DocParagraph paragraph = _paragraph;
    _paragraph = new();
    string? marker = _listMarker;
    _listMarker = null;
    if (!always && paragraph.Inlines.Count == 0) {
      return;
    }
    paragraph.Alignment = _state.Alignment;
    paragraph.RightToLeft = _state.RightToLeft ? true : null;
    paragraph.Indent = Math.Max(0, _state.LeftIndent / 20.0);
    paragraph.FirstLineIndent = marker is null ? _state.FirstIndent / 20.0 : 0;
    paragraph.SpaceBefore = _state.SpaceBefore / 20.0;
    paragraph.SpaceAfter = _state.SpaceAfter / 20.0;
    paragraph.LineSpacing = _state.LineSpacing;
    paragraph.ListMarker = _state.List || marker is not null ? marker : null;
    paragraph.ListMarkerStyle = (paragraph.Inlines.OfType<DocText>().FirstOrDefault()?.Style ?? default) with { Underline = false };
    paragraph.HeadingLevel = _state.OutlineLevel is >= 0 and <= 5 ? _state.OutlineLevel + 1 : HeadingLevel(_styleNames.GetValueOrDefault(_state.Style));
    // Spacing between paragraphs of the same style can be left out, as it is between the items of a list
    if (_state.Contextual && _lastParagraph is { } last && _lastContextual && _lastStyle == _state.Style) {
      last.SpaceAfter = 0;
      paragraph.SpaceBefore = 0;
    }
    AddBlock(paragraph);
    _lastParagraph = paragraph;
    _lastStyle = _state.Style;
    _lastContextual = _state.Contextual;
  }

  internal static int HeadingLevel(string? styleName) {
    if (styleName is null) {
      return 0;
    }
    string name = styleName.Trim().ToLowerInvariant();
    if (name is "title") {
      return 1;
    }
    return name.StartsWith("heading ", StringComparison.Ordinal) && int.TryParse(name[8..], out int level) && level is >= 1 and <= 6 ? level : 0;
  }

  private void AddBlock(DocBlock block) {
    _lastParagraph = null;
    if (_state.InTable && _table is not null) {
      _cell.Blocks.Add(block);
      return;
    }
    if (_state.InTable) {
      _table = new();
      _cell.Blocks.Add(block);
      return;
    }
    CloseTable();
    _blocks.Add(block);
  }

  private void EndCell() {
    if (_paragraph.Inlines.Count > 0 || _cell.Blocks.Count == 0) {
      bool inTable = _state.InTable;
      _state = _state with { InTable = true };
      EndParagraph(false);
      _state = _state with { InTable = inTable };
    }
    _table ??= new();
    _row.Cells.Add(_cell);
    _cell = new();
  }

  private void EndRow() {
    _lastParagraph = null;
    _table ??= new();
    if (_cell.Blocks.Count > 0) {
      _row.Cells.Add(_cell);
      _cell = new();
    }
    if (_row.Cells.Count > 0) {
      _table.Rows.Add(_row);
      _table.Borders |= _rowBorders;
      if (_table.ColumnWidths is null && _cellEdges.Count == _row.Cells.Count) {
        List<double> widths = [];
        int left = _rowLeft;
        foreach (int edge in _cellEdges) {
          widths.Add(Math.Max(1, edge - left) / 20.0);
          left = edge;
        }
        _table.ColumnWidths = widths;
      }
    }
    _row = new();
  }

  private void CloseTable() {
    if (_table is null) {
      return;
    }
    if (_row.Cells.Count > 0 || _cell.Blocks.Count > 0) {
      EndRow();
    }
    DocTable table = _table;
    _table = null;
    if (table.Rows.Count > 0) {
      _blocks.Add(table);
    }
  }
}
