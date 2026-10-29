namespace Pixata.Blazor.Components;

/// <summary>
/// The icons the components use, which you can also pass to any <c>Icon</c> parameter, or render with <see cref="PixataIcon"/>
/// </summary>
public static partial class PixataSvgIcons {
  public static readonly PixataSvgIcon ChevronDown = new("chevron-down", "<path d=\"M6 9l6 6 6-6\"/>");
  public static readonly PixataSvgIcon ChevronUp = new("chevron-up", "<path d=\"M6 15l6-6 6 6\"/>");
  public static readonly PixataSvgIcon ChevronLeft = new("chevron-left", "<path d=\"M15 6l-6 6 6 6\"/>");
  public static readonly PixataSvgIcon ChevronRight = new("chevron-right", "<path d=\"M9 6l6 6-6 6\"/>");
  public static readonly PixataSvgIcon CaretDown = new("caret-down", "<path d=\"M7 10h10l-5 6z\" fill=\"currentColor\" stroke=\"none\"/>");
  public static readonly PixataSvgIcon CaretUp = new("caret-up", "<path d=\"M7 14h10l-5-6z\" fill=\"currentColor\" stroke=\"none\"/>");
  public static readonly PixataSvgIcon CaretLeft = new("caret-left", "<path d=\"M14 7v10l-6-5z\" fill=\"currentColor\" stroke=\"none\"/>");
  public static readonly PixataSvgIcon CaretRight = new("caret-right", "<path d=\"M10 7v10l6-5z\" fill=\"currentColor\" stroke=\"none\"/>");
  public static readonly PixataSvgIcon X = new("x", "<path d=\"M6 6l12 12M18 6L6 18\"/>");
  public static readonly PixataSvgIcon XCircle = new("x-circle", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M9 9l6 6M15 9l-6 6\"/>");
  public static readonly PixataSvgIcon Check = new("check", "<path d=\"M5 12.5l4.5 4.5L19 7.5\"/>");
  public static readonly PixataSvgIcon Plus = new("plus", "<path d=\"M12 5v14M5 12h14\"/>");
  public static readonly PixataSvgIcon Minus = new("minus", "<path d=\"M5 12h14\"/>");
  public static readonly PixataSvgIcon Search = new("search", "<circle cx=\"11\" cy=\"11\" r=\"6\"/><path d=\"M20 20l-4.5-4.5\"/>");
  public static readonly PixataSvgIcon MoreHorizontal = new("more-horizontal", "<circle cx=\"5\" cy=\"12\" r=\"1.5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"12\" cy=\"12\" r=\"1.5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"19\" cy=\"12\" r=\"1.5\" fill=\"currentColor\" stroke=\"none\"/>");
  public static readonly PixataSvgIcon MoreVertical = new("more-vertical", "<circle cx=\"12\" cy=\"5\" r=\"1.5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"12\" cy=\"12\" r=\"1.5\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"12\" cy=\"19\" r=\"1.5\" fill=\"currentColor\" stroke=\"none\"/>");
  public static readonly PixataSvgIcon WindowMinimise = new("window-minimise", "<path d=\"M6 17h12\"/>");
  public static readonly PixataSvgIcon WindowMaximise = new("window-maximise", "<rect x=\"5\" y=\"5\" width=\"14\" height=\"14\" rx=\"1\"/>");
  public static readonly PixataSvgIcon WindowRestore = new("window-restore", "<rect x=\"5\" y=\"9\" width=\"10\" height=\"10\" rx=\"1\"/><path d=\"M9 9V6a1 1 0 0 1 1-1h8a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1h-3\"/>");
  public static readonly PixataSvgIcon Upload = new("upload", "<path d=\"M12 16V4M7 9l5-5 5 5M5 20h14\"/>");
  public static readonly PixataSvgIcon Download = new("download", "<path d=\"M12 4v12M7 11l5 5 5-5M5 20h14\"/>");
  public static readonly PixataSvgIcon File = new("file", "<path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"/><path d=\"M14 3v5h5\"/>");
  public static readonly PixataSvgIcon Trash = new("trash", "<path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V4h6v3\"/>");
  public static readonly PixataSvgIcon Pencil = new("pencil", "<path d=\"M4 20h4L19 9l-4-4L4 16z\"/><path d=\"M13 7l4 4\"/>");
  public static readonly PixataSvgIcon Refresh = new("refresh", "<path d=\"M20 12a8 8 0 1 1-2.3-5.7\"/><path d=\"M20 4v5h-5\"/>");
  public static readonly PixataSvgIcon Calendar = new("calendar", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"16\" rx=\"2\"/><path d=\"M3 10h18M8 3v4M16 3v4\"/>");
  public static readonly PixataSvgIcon Clock = new("clock", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 3\"/>");
  public static readonly PixataSvgIcon Info = new("info", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 11v6M12 7.5v.5\"/>");
  public static readonly PixataSvgIcon Warning = new("warning", "<path d=\"M12 3l10 18H2z\"/><path d=\"M12 10v5M12 18v.5\"/>");
  public static readonly PixataSvgIcon Exclamation = new("exclamation", "<path d=\"M12 4v10M12 19.5v.5\"/>");
  public static readonly PixataSvgIcon Bell = new("bell", "<path d=\"M6 16v-5a6 6 0 0 1 12 0v5l2 2H4z\"/><path d=\"M10 20.5a2 2 0 0 0 4 0M12 3v2\"/>");
  public static readonly PixataSvgIcon Cancel = new("cancel", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M5.6 5.6l12.8 12.8\"/>");
  public static readonly PixataSvgIcon Eye = new("eye", "<path d=\"M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>");
  public static readonly PixataSvgIcon EyeOff = new("eye-off", "<path d=\"M10.6 5.1Q11.3 5 12 5c6.4 0 10 7 10 7a17.6 17.6 0 0 1-3.2 4.2M6.6 6.6C3.7 8.5 2 12 2 12s3.6 7 10 7a9.7 9.7 0 0 0 5.4-1.6\"/><path d=\"M9.9 9.9a3 3 0 0 0 4.2 4.2M3 3l18 18\"/>");
}
