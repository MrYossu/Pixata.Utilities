using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Icons for the grid's buttons, drawn as SVG so there's no icon font to load
/// </summary>
public enum PixataGridIcon {
  None,
  Plus,
  Pencil,
  Trash,
  Save,
  Cancel,
  Check,
  X,
  Filter,
  FilterClear,
  Search,
  Download,
  FileCsv,
  FileExcel,
  CaretDown,
  CaretRight,
  CaretLeft,
  XCircle,
  SortUp,
  SortDown,
  MoreVertical,
  Columns,
  ChevronDown,
  ChevronUp,
  Group,
  ColumnPosition,
  Lock,
  Unlock,
  Reset
}

internal static class PixataGridIcons {
  public static RenderFragment? For(PixataGridIcon icon) =>
    Path(icon) is string path
      ? builder => {
          builder.OpenElement(0, "svg");
          builder.AddAttribute(1, "viewBox", "0 0 16 16");
          builder.AddAttribute(2, "aria-hidden", "true");
          builder.OpenElement(3, "path");
          builder.AddAttribute(4, "d", path);
          builder.CloseElement();
          builder.CloseElement();
        }
      : null;

  private static string? Path(PixataGridIcon icon) => icon switch {
    PixataGridIcon.Plus => "M7.5 2h1v5.5H14v1H8.5V14h-1V8.5H2v-1h5.5z",
    PixataGridIcon.Pencil => "M11.3 1.6 14.4 4.7 5.3 13.8 1.5 14.5l.7-3.8zm0 1.4-7.9 7.9-.4 2.1 2.1-.4 7.9-7.9z",
    PixataGridIcon.Trash => "M6 1h4v1h4v1H2V2h4zM3 4h1v10h8V4h1v11H3zm3 1h1v7H6zm3 0h1v7H9z",
    PixataGridIcon.Save => "M2 2h9.5L14 4.5V14H2zm1 1v10h10V5l-2-2v3H5V3zm3 0v2h4V3zm2 5a2 2 0 1 1 0 4 2 2 0 0 1 0-4",
    PixataGridIcon.Cancel => "M8 1a7 7 0 1 1 0 14A7 7 0 0 1 8 1m0 1a6 6 0 0 0-4.7 9.7L11.7 3.3A6 6 0 0 0 8 2m4.7 2.3-8.4 8.4A6 6 0 0 0 12.7 4.3",
    PixataGridIcon.Check => "m13.6 3.6.8.8L6 12.8 1.6 8.4l.8-.8L6 11.2z",
    PixataGridIcon.X => "m8 7.3 3.6-3.6.7.7L8.7 8l3.6 3.6-.7.7L8 8.7l-3.6 3.6-.7-.7L7.3 8 3.7 4.4l.7-.7z",
    PixataGridIcon.Filter => "M1 2h14l-5.5 6.5V14l-3-2V8.5z",
    PixataGridIcon.FilterClear => "M1 2h12L8.5 7.5V13l-2-1.3V7.5zm9.6 7.4 1.9 1.9 1.9-1.9.7.7-1.9 1.9 1.9 1.9-.7.7-1.9-1.9-1.9 1.9-.7-.7 1.9-1.9-1.9-1.9z",
    PixataGridIcon.Search => "M6.5 1a5.5 5.5 0 0 1 4.4 8.8l4 4-.7.7-4-4A5.5 5.5 0 1 1 6.5 1m0 1a4.5 4.5 0 1 0 0 9 4.5 4.5 0 0 0 0-9",
    PixataGridIcon.Download => "M7.5 1h1v8.3l3-3 .7.7L8 11.2 3.8 7l.7-.7 3 3zM2 13h12v1H2z",
    PixataGridIcon.FileCsv => "M3 1h7l3 3v11H3zm1 1v12h8V5H9V2zm1 6h6v1H5zm0 2h6v1H5zm0 2h4v1H5z",
    PixataGridIcon.FileExcel => "M3 1h7l3 3v11H3zm1 1v12h8V5H9V2zm1.3 5h1.2L8 9.2 9.5 7h1.2l-2.1 3 2.1 3H9.5L8 10.8 6.5 13H5.3l2.1-3z",
    PixataGridIcon.CaretDown => "M4 6h8l-4 5z",
    PixataGridIcon.CaretRight => "M6 4v8l5-4z",
    PixataGridIcon.CaretLeft => "M10 4v8L5 8z",
    PixataGridIcon.SortUp => "M7 6v7h1V6h3L7.5 2 4 6z",
    PixataGridIcon.SortDown => "M7 10V3h1v7h3l-3.5 4L4 10z",
    PixataGridIcon.MoreVertical => "M7.5 2a1.25 1.25 0 1 1 0 2.5 1.25 1.25 0 0 1 0-2.5m0 4.75a1.25 1.25 0 1 1 0 2.5 1.25 1.25 0 0 1 0-2.5m0 4.75a1.25 1.25 0 1 1 0 2.5 1.25 1.25 0 0 1 0-2.5",
    PixataGridIcon.Columns => "M1 2h14v12H1zm1 1v10h3.5V3zm4.5 0v10h3V3zm4 0v10H14V3z",
    PixataGridIcon.ChevronDown => "m3.4 5.6 4.6 4.6 4.6-4.6.7.7L8 11.6 2.7 6.3z",
    PixataGridIcon.ChevronUp => "m3.4 10.4 4.6-4.6 4.6 4.6.7-.7L8 4.4 2.7 9.7z",
    PixataGridIcon.Group => "M1 2h9v3H1zm3 4h11v3H4zm0 4h11v3H4zM1 6h1v8H1z",
    PixataGridIcon.ColumnPosition => "M1 2h4v12H1zm1 1v10h2V3zm9-1h4v12h-4zm1 1v10h2V3zM6 7.5h3V6l2.5 2L9 10V8.5H6z",
    PixataGridIcon.Lock => "M8 1a3.5 3.5 0 0 1 3.5 3.5V7H13v8H3V7h1.5V4.5A3.5 3.5 0 0 1 8 1m0 1a2.5 2.5 0 0 0-2.5 2.5V7h5V4.5A2.5 2.5 0 0 0 8 2M4 8v6h8V8z",
    PixataGridIcon.Unlock => "M8 1a3.5 3.5 0 0 1 3.5 3.5h-1a2.5 2.5 0 0 0-5 0V7H13v8H3V7h1.5V4.5A3.5 3.5 0 0 1 8 1M4 8v6h8V8z",
    PixataGridIcon.Reset => "M8 2a6 6 0 1 1-6 6h1a5 5 0 1 0 1.5-3.6L6.5 6H2V1.5l1.8 1.8A6 6 0 0 1 8 2",
    // The cross is cut out of the circle, with fill-rule evenodd set in the CSS
    PixataGridIcon.XCircle => "M8 1a7 7 0 1 1 0 14A7 7 0 0 1 8 1zm2.6 3.3L8 6.9 5.4 4.3 4.3 5.4 6.9 8l-2.6 2.6 1.1 1.1L8 9.1l2.6 2.6 1.1-1.1L9.1 8l2.6-2.6z",
    _ => null
  };
}
