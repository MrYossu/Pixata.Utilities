using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// What the user has typed into a date input, held one segment (day, month and so on) at a time, as Telerik's DateInput does.
/// This is plain C#, with no Blazor or JavaScript, so the typing rules can be tested on their own. The component turns key
/// presses into calls to <see cref="Key"/>, and shows <see cref="Text"/> with <see cref="Selection"/> highlighted
/// </summary>
internal sealed class DateInputModel {
  internal enum SegmentType {
    Literal,
    Day,
    Month,
    Year,
    Hour,
    Minute,
    Second,
    AmPm,
    Weekday
  }

  internal sealed record Segment(SegmentType Type, string Format);

  private readonly List<Segment> _segments;
  private readonly int[] _editable;
  private readonly CultureInfo _culture;
  private readonly bool _twelveHour;
  private int? _day;
  private int? _month;
  private int? _year;
  private int? _hour;
  private int? _minute;
  private int? _second;
  private bool? _pm;
  private DateTime _base = DateTime.Today;
  private int _current;
  private string _typed = "";
  // True when the last key filled a segment and moved on to the next one by itself
  private bool _autoSwitched;

  public DateInputModel(string? format, CultureInfo? culture = null) {
    _culture = culture ?? CultureInfo.CurrentCulture;
    _segments = Parse(string.IsNullOrWhiteSpace(format) ? "d" : format, _culture);
    _editable = [.. _segments.Select((s, i) => (s, i)).Where(x => x.s.Type is not (SegmentType.Literal or SegmentType.Weekday)).Select(x => x.i)];
    _twelveHour = _segments.Any(s => s.Type == SegmentType.Hour && s.Format[0] == 'h');
  }

  /// <summary>
  /// When true (the default), the selection moves to the next segment once the current one can't take another digit
  /// </summary>
  public bool AutoSwitchParts { get; set; } = true;

  /// <summary>
  /// Two-digit years up to this are in the 2000s, and those above it are in the 1900s. Defaults to 68, as in Telerik
  /// </summary>
  public int TwoDigitYearMax { get; set; } = 68;

  public DateTime Min { get; set; } = new(1900, 1, 1);

  public DateTime Max { get; set; } = new(2099, 12, 31, 23, 59, 59);

  public IReadOnlyList<Segment> Segments =>
    _segments;

  /// <summary>
  /// True if the format only has numeric segments, so a numeric keyboard is enough on a phone
  /// </summary>
  public bool IsNumeric =>
    !_segments.Any(s => s.Type == SegmentType.AmPm || (s.Type == SegmentType.Month && s.Format.Length > 2));

  public bool IsEmpty =>
    _editable.All(i => PartValue(_segments[i].Type) is null);

  public bool IsComplete =>
    _editable.All(i => PartValue(_segments[i].Type) is not null);

  /// <summary>
  /// The date, if every segment has been filled in and they make a real date. Parts the format doesn't show (such as the
  /// time, for a date-only format) come from the last value set, or are midnight today
  /// </summary>
  public DateTime? Value {
    get {
      if (!IsComplete) {
        return null;
      }
      int year = _year ?? _base.Year;
      int month = _month ?? _base.Month;
      int day = _day ?? _base.Day;
      if (year is < 1 or > 9999 || day > DateTime.DaysInMonth(year, month)) {
        return null;
      }
      int hour = HasType(SegmentType.Hour) ? Hour24(_hour!.Value, _pm ?? _base.Hour >= 12) : _base.Hour;
      if (!HasType(SegmentType.Hour) && HasType(SegmentType.AmPm)) {
        hour = _base.Hour % 12 + (_pm == true ? 12 : 0);
      }
      return new DateTime(year, month, day, hour, _minute ?? _base.Minute, _second ?? _base.Second, _base.Kind);
    }
  }

  /// <summary>
  /// False if the user has typed something that isn't a complete date in range. An empty input counts as valid
  /// </summary>
  public bool IsValid =>
    IsEmpty || (Value is DateTime value && value >= Min && value <= Max);

  public string Text =>
    string.Concat(_segments.Select(Render));

  /// <summary>
  /// The start and end of the selected segment in <see cref="Text"/>
  /// </summary>
  public (int Start, int End) Selection {
    get {
      int start = 0;
      for (int i = 0; i < _segments.Count; i++) {
        int length = Render(_segments[i]).Length;
        if (_editable.Length > 0 && i == _editable[_current]) {
          return (start, start + length);
        }
        start += length;
      }
      return (0, 0);
    }
  }

  public void SetValue(DateTime? value) {
    _typed = "";
    _autoSwitched = false;
    if (value is not DateTime date) {
      _day = _month = _year = _hour = _minute = _second = null;
      _pm = null;
      _base = DateTime.Today;
      return;
    }
    _base = date;
    _day = date.Day;
    _month = date.Month;
    _year = date.Year;
    _hour = _twelveHour ? (date.Hour % 12 == 0 ? 12 : date.Hour % 12) : date.Hour;
    _minute = date.Minute;
    _second = date.Second;
    _pm = date.Hour >= 12;
    // Only the parts the format shows count as filled in, so a time-only format doesn't need a date typed, and vice versa
    foreach (SegmentType type in Enum.GetValues<SegmentType>().Where(t => !HasType(t))) {
      SetPart(type, null);
    }
  }

  public void Clear() =>
    SetValue(null);

  /// <summary>
  /// Selects the first segment
  /// </summary>
  public void SelectFirst() {
    _current = 0;
    _typed = "";
    _autoSwitched = false;
  }

  /// <summary>
  /// Selects the segment that the caret is in, for when the user clicks in the input
  /// </summary>
  public void SelectAt(int caret) {
    int start = 0;
    int best = 0;
    for (int i = 0; i < _segments.Count; i++) {
      int length = Render(_segments[i]).Length;
      int editableIndex = Array.IndexOf(_editable, i);
      if (editableIndex >= 0) {
        if (caret >= start) {
          best = editableIndex;
        }
        if (caret <= start + length) {
          break;
        }
      }
      start += length;
    }
    _current = best;
    _typed = "";
    _autoSwitched = false;
  }

  /// <summary>
  /// Handles a key press. The key is the browser's <c>KeyboardEvent.key</c>, so "ArrowUp", "Backspace", "7" and so on
  /// </summary>
  /// <returns>True if the key was handled, in which case the browser shouldn't act on it</returns>
  public bool Key(string key) {
    if (_editable.Length == 0) {
      return false;
    }
    bool autoSwitched = _autoSwitched;
    _autoSwitched = false;
    switch (key) {
      case "ArrowLeft":
        Move(-1);
        return true;
      case "ArrowRight":
        Move(1);
        return true;
      case "Home":
        _current = 0;
        _typed = "";
        return true;
      case "End":
        _current = _editable.Length - 1;
        _typed = "";
        return true;
      case "ArrowUp":
        Step(1);
        return true;
      case "ArrowDown":
        Step(-1);
        return true;
      case "Backspace" or "Delete":
        SetPart(CurrentSegment.Type, null);
        _typed = "";
        return true;
    }
    if (key.Length != 1) {
      return false;
    }
    char c = key[0];
    if (char.IsDigit(c)) {
      TypeDigit(c);
    } else if (char.IsLetter(c)) {
      TypeLetter(c);
    } else if (autoSwitched) {
      // The segment before has already moved on by itself, so the separator is ignored, or "01/10/2026" would skip the month.
      // It stays ignored for the rest of a separator of more than one character, such as ", "
      _autoSwitched = true;
    } else {
      // A separator moves on to the next segment, so "1/2/2026" can be typed without using the arrow keys
      Move(1);
    }
    return true;
  }

  /// <summary>
  /// Takes a whole date, such as one pasted in, if it can be read in the input's format or the culture's usual ones
  /// </summary>
  /// <returns>True if the text was a date</returns>
  public bool Paste(string text) {
    string format = string.Concat(_segments.Select(s => s.Type == SegmentType.Literal ? $"'{s.Format}'" : s.Format));
    if (DateTime.TryParseExact(text.Trim(), format, _culture, DateTimeStyles.AllowWhiteSpaces, out DateTime date)
        || DateTime.TryParse(text.Trim(), _culture, DateTimeStyles.AllowWhiteSpaces, out date)) {
      SetValue(date);
      return true;
    }
    return false;
  }

  private Segment CurrentSegment =>
    _segments[_editable[_current]];

  private void Move(int delta) {
    _current = Math.Clamp(_current + delta, 0, _editable.Length - 1);
    _typed = "";
  }

  private void TypeDigit(char digit) {
    Segment segment = CurrentSegment;
    string candidate = _typed + digit;
    // If the digit can't extend what was typed, it starts the segment again, and if it can't do that either, it's ignored
    if (!TryDigits(segment, candidate)) {
      candidate = digit.ToString();
      if (!TryDigits(segment, candidate)) {
        return;
      }
    }
    _typed = candidate;
    if (AutoSwitchParts && SegmentFull(segment, candidate)) {
      Move(1);
      _autoSwitched = true;
    }
  }

  private bool TryDigits(Segment segment, string digits) {
    if (digits.Length > MaxDigits(segment)) {
      return false;
    }
    int n = int.Parse(digits, CultureInfo.InvariantCulture);
    switch (segment.Type) {
      case SegmentType.Day:
        if (n > DaysAvailable(_month, _year)) {
          return false;
        }
        if (n > 0) {
          _day = n;
        }
        return true;
      case SegmentType.Month:
        if (n > 12 || (n > 0 && _day > DaysAvailable(n, _year))) {
          return false;
        }
        if (n > 0) {
          _month = n;
        }
        return true;
      case SegmentType.Year:
        _year = MaxDigits(segment) == 2 ? (n <= TwoDigitYearMax ? 2000 + n : 1900 + n) : n;
        return true;
      case SegmentType.Hour:
        if (n > (_twelveHour ? 12 : 23)) {
          return false;
        }
        if (n > 0 || !_twelveHour) {
          _hour = n;
        }
        return true;
      case SegmentType.Minute or SegmentType.Second:
        if (n > 59) {
          return false;
        }
        SetPart(segment.Type, n);
        return true;
      default:
        return false;
    }
  }

  // A segment is full when it has all its digits, or when another digit would make it too big. So typing 3 in a day stays
  // put (it could be 30 or 31), but typing 4 moves on
  private bool SegmentFull(Segment segment, string digits) {
    if (digits.Length >= MaxDigits(segment)) {
      return true;
    }
    int n = int.Parse(digits, CultureInfo.InvariantCulture);
    int max = segment.Type switch {
      SegmentType.Day => DaysAvailable(_month, _year),
      SegmentType.Month => 12,
      SegmentType.Hour => _twelveHour ? 12 : 23,
      SegmentType.Minute or SegmentType.Second => 59,
      _ => int.MaxValue
    };
    return segment.Type != SegmentType.Year && n * 10 > max;
  }

  private static int MaxDigits(Segment segment) =>
    segment.Type == SegmentType.Year ? (segment.Format.Length <= 2 ? 2 : 4) : 2;

  private void TypeLetter(char letter) {
    Segment segment = CurrentSegment;
    if (segment.Type == SegmentType.AmPm) {
      string am = _culture.DateTimeFormat.AMDesignator;
      string pm = _culture.DateTimeFormat.PMDesignator;
      if (pm.Length > 0 && char.ToUpperInvariant(pm[0]) == char.ToUpperInvariant(letter)) {
        _pm = true;
      } else if (am.Length > 0 && char.ToUpperInvariant(am[0]) == char.ToUpperInvariant(letter)) {
        _pm = false;
      } else {
        return;
      }
      if (AutoSwitchParts) {
        Move(1);
        _autoSwitched = true;
      }
      return;
    }
    if (segment.Type != SegmentType.Month || segment.Format.Length < 3) {
      return;
    }
    // Typing letters picks the month whose name starts with them. Typing the same letter again moves through the months
    // that start with it, so J, J, J gives January, June, July
    string[] names = [.. Enumerable.Range(1, 12).Select(m => MonthName(segment.Format, m))];
    string typed = _typed + letter;
    List<int> matches = Matches(names, typed);
    bool cycling = false;
    if (matches.Count == 0) {
      cycling = typed.Length > 1 && typed.All(ch => char.ToUpperInvariant(ch) == char.ToUpperInvariant(letter));
      if (!cycling) {
        typed = letter.ToString();
      }
      matches = Matches(names, letter.ToString());
      if (matches.Count == 0) {
        return;
      }
    }
    int month = cycling ? matches[(typed.Length - 1) % matches.Count] : matches[0];
    if (_day > DaysAvailable(month, _year)) {
      return;
    }
    _month = month;
    _typed = typed;
    if (AutoSwitchParts && !cycling && matches.Count == 1) {
      Move(1);
      _autoSwitched = true;
    }
  }

  private static List<int> Matches(string[] names, string typed) =>
    [.. names.Select((n, i) => (n, i)).Where(x => x.n.StartsWith(typed, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.i + 1)];

  // Up and Down change a complete date by one unit of the selected segment, so the 31st plus one day is the 1st of the next
  // month, and January 31st plus one month is February 28th (or 29th). On an incomplete date, they change just that segment,
  // starting from today's value if it's empty
  private void Step(int delta) {
    Segment segment = CurrentSegment;
    _typed = "";
    if (IsComplete && Value is DateTime date) {
      DateTime next = segment.Type switch {
        SegmentType.Day => date.AddDays(delta),
        SegmentType.Month => date.AddMonths(delta),
        SegmentType.Year => date.AddYears(delta),
        SegmentType.Hour => date.AddHours(delta),
        SegmentType.Minute => date.AddMinutes(delta),
        SegmentType.Second => date.AddSeconds(delta),
        _ => date.AddHours(date.Hour < 12 ? 12 : -12)
      };
      if (next >= Min && next <= Max) {
        SetValue(next);
      }
      return;
    }
    DateTime today = DateTime.Now;
    int? current = PartValue(segment.Type);
    switch (segment.Type) {
      case SegmentType.Day:
        _day = current is null ? Math.Min(today.Day, DaysAvailable(_month, _year)) : Wrap(current.Value + delta, 1, DaysAvailable(_month, _year));
        break;
      case SegmentType.Month:
        _month = current is null ? today.Month : Wrap(current.Value + delta, 1, 12);
        break;
      case SegmentType.Year:
        _year = current is null ? today.Year : Math.Clamp(current.Value + delta, 1, 9999);
        break;
      case SegmentType.Hour:
        _hour = current is null
          ? (_twelveHour ? (today.Hour % 12 == 0 ? 12 : today.Hour % 12) : today.Hour)
          : (_twelveHour ? Wrap(current.Value + delta, 1, 12) : Wrap(current.Value + delta, 0, 23));
        break;
      case SegmentType.Minute:
        _minute = current is null ? today.Minute : Wrap(current.Value + delta, 0, 59);
        break;
      case SegmentType.Second:
        _second = current is null ? today.Second : Wrap(current.Value + delta, 0, 59);
        break;
      case SegmentType.AmPm:
        _pm = _pm is null ? today.Hour >= 12 : !_pm;
        break;
    }
  }

  private static int Wrap(int value, int min, int max) =>
    value < min ? max : value > max ? min : value;

  private int DaysAvailable(int? month, int? year) =>
    month is null ? 31 : DateTime.DaysInMonth(year is >= 1 and <= 9999 ? year.Value : 2000, month.Value);

  private static int Hour24(int hour, bool pm) =>
    hour % 12 + (pm ? 12 : 0);

  private bool HasType(SegmentType type) =>
    _segments.Any(s => s.Type == type);

  private int? PartValue(SegmentType type) => type switch {
    SegmentType.Day => _day,
    SegmentType.Month => _month,
    SegmentType.Year => _year,
    SegmentType.Hour => _hour,
    SegmentType.Minute => _minute,
    SegmentType.Second => _second,
    SegmentType.AmPm => _pm is null ? null : _pm.Value ? 1 : 0,
    _ => null
  };

  private void SetPart(SegmentType type, int? value) {
    switch (type) {
      case SegmentType.Day:
        _day = value;
        break;
      case SegmentType.Month:
        _month = value;
        break;
      case SegmentType.Year:
        _year = value;
        break;
      case SegmentType.Hour:
        _hour = value;
        break;
      case SegmentType.Minute:
        _minute = value;
        break;
      case SegmentType.Second:
        _second = value;
        break;
      case SegmentType.AmPm:
        _pm = value is null ? null : value == 1;
        break;
    }
  }

  // An empty segment shows its format specifier, such as "dd" or "MMM", as Telerik's does
  private string Render(Segment segment) {
    if (segment.Type == SegmentType.Literal) {
      return segment.Format;
    }
    if (segment.Type == SegmentType.Weekday) {
      return Value is DateTime date ? date.ToString(segment.Format.Length == 3 ? "ddd" : "dddd", _culture) : segment.Format;
    }
    int? value = PartValue(segment.Type);
    if (value is null) {
      return segment.Format;
    }
    int n = value.Value;
    return segment.Type switch {
      SegmentType.Month when segment.Format.Length > 2 => MonthName(segment.Format, n),
      SegmentType.Year when segment.Format.Length <= 2 => (n % 100).ToString(segment.Format.Length == 2 ? "00" : "0", CultureInfo.InvariantCulture),
      SegmentType.Year => n.ToString(new string('0', segment.Format.Length), CultureInfo.InvariantCulture),
      SegmentType.AmPm => n == 1 ? _culture.DateTimeFormat.PMDesignator : _culture.DateTimeFormat.AMDesignator,
      _ => n.ToString(segment.Format.Length == 2 ? "00" : "0", CultureInfo.InvariantCulture)
    };
  }

  private string MonthName(string format, int month) =>
    format.Length == 3 ? _culture.DateTimeFormat.GetAbbreviatedMonthName(month) : _culture.DateTimeFormat.GetMonthName(month);

  /// <summary>
  /// Splits a .NET date format into segments. A single-letter standard format, such as "d", is first expanded to the
  /// culture's pattern for it
  /// </summary>
  internal static List<Segment> Parse(string format, CultureInfo culture) {
    DateTimeFormatInfo info = culture.DateTimeFormat;
    if (format.Length == 1) {
      format = format[0] switch {
        'd' => info.ShortDatePattern,
        'D' => info.LongDatePattern,
        't' => info.ShortTimePattern,
        'T' => info.LongTimePattern,
        'g' => $"{info.ShortDatePattern} {info.ShortTimePattern}",
        'G' => $"{info.ShortDatePattern} {info.LongTimePattern}",
        'f' => $"{info.LongDatePattern} {info.ShortTimePattern}",
        'F' => info.FullDateTimePattern,
        'M' or 'm' => info.MonthDayPattern,
        'Y' or 'y' => info.YearMonthPattern,
        _ => format
      };
    }
    List<Segment> segments = [];
    void AddLiteral(string text) {
      if (segments.Count > 0 && segments[^1].Type == SegmentType.Literal) {
        segments[^1] = segments[^1] with { Format = segments[^1].Format + text };
      } else {
        segments.Add(new(SegmentType.Literal, text));
      }
    }
    int i = 0;
    while (i < format.Length) {
      char c = format[i];
      int run = 1;
      while (i + run < format.Length && format[i + run] == c) {
        run++;
      }
      switch (c) {
        case 'd':
          segments.Add(new(run <= 2 ? SegmentType.Day : SegmentType.Weekday, new string('d', Math.Min(run, 4))));
          break;
        case 'M':
          segments.Add(new(SegmentType.Month, new string('M', Math.Min(run, 4))));
          break;
        case 'y':
          segments.Add(new(SegmentType.Year, run <= 2 ? new string('y', run) : "yyyy"));
          break;
        case 'H' or 'h':
          segments.Add(new(SegmentType.Hour, new string(c, Math.Min(run, 2))));
          break;
        case 'm':
          segments.Add(new(SegmentType.Minute, new string('m', Math.Min(run, 2))));
          break;
        case 's':
          segments.Add(new(SegmentType.Second, new string('s', Math.Min(run, 2))));
          break;
        case 't':
          segments.Add(new(SegmentType.AmPm, "tt"));
          break;
        case '/':
          AddLiteral(info.DateSeparator);
          run = 1;
          break;
        case ':':
          AddLiteral(info.TimeSeparator);
          run = 1;
          break;
        case '\'' or '"': {
          int end = format.IndexOf(c, i + 1);
          end = end < 0 ? format.Length : end;
          AddLiteral(format[(i + 1)..end]);
          run = end - i + 1;
          break;
        }
        case '\\':
          if (i + 1 < format.Length) {
            AddLiteral(format[i + 1].ToString());
          }
          run = 2;
          break;
        case '%' or 'f' or 'F' or 'z' or 'K' or 'g':
          // Fractions of a second, time zones and eras can't be typed, so they're left out
          break;
        default:
          AddLiteral(new string(c, run));
          break;
      }
      i += run;
    }
    return segments;
  }
}
