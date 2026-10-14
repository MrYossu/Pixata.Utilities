using System;
using System.Collections.Generic;

namespace Pixata.Blazor.Components;

public static partial class PixataSvgIcons {
  private const string FileOutline = "<path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"/><path d=\"M14 3v5h5\"/>";

  public static readonly PixataSvgIcon FileImage = new("file-image", $"{FileOutline}<circle cx=\"10\" cy=\"11.5\" r=\"1.25\"/><path d=\"M7.5 18l3-3.5 2 2 1.5-1.5 2.5 3z\"/>");
  public static readonly PixataSvgIcon FileVideo = new("file-video", $"{FileOutline}<path d=\"M10 11.5v5.5l4.5-2.75z\"/>");
  public static readonly PixataSvgIcon FileAudio = new("file-audio", $"{FileOutline}<circle cx=\"10\" cy=\"16.5\" r=\"1.5\"/><path d=\"M11.5 16.5V11l3 1\"/>");
  public static readonly PixataSvgIcon FileArchive = new("file-archive", $"{FileOutline}<path d=\"M10 3v2M10 7v2M10 11v2\"/><rect x=\"8.5\" y=\"14\" width=\"3\" height=\"4\" rx=\".5\"/>");
  public static readonly PixataSvgIcon FilePdf = new("file-pdf", $"{FileOutline}<path d=\"M7.5 18c2.5-1.5 4.5-4.5 4.5-6.5 0-1-1-1-1 0 0 2.5 2.5 5.5 5.5 5.5\"/>");
  public static readonly PixataSvgIcon FileText = new("file-text", $"{FileOutline}<path d=\"M8 12h8M8 15h8M8 18h5\"/>");
  public static readonly PixataSvgIcon FileData = new("file-data", $"{FileOutline}<path d=\"M8 11h8v7H8zM8 14.5h8M12 11v7\"/>");
  public static readonly PixataSvgIcon FilePresentation = new("file-presentation", $"{FileOutline}<rect x=\"7.5\" y=\"11\" width=\"9\" height=\"5.5\" rx=\".5\"/><path d=\"M12 16.5v2\"/>");
  public static readonly PixataSvgIcon FileCode = new("file-code", $"{FileOutline}<path d=\"M10 12l-2 2.5 2 2.5M14 12l2 2.5-2 2.5\"/>");
  public static readonly PixataSvgIcon Pause = new("pause", "<path d=\"M9 6v12M15 6v12\"/>");
  public static readonly PixataSvgIcon Play = new("play", "<path d=\"M8 5.5v13l10-6.5z\"/>");

  private static readonly Dictionary<string, PixataSvgIcon> FileIcons = new(StringComparer.OrdinalIgnoreCase) {
    [".jpg"] = FileImage, [".jpeg"] = FileImage, [".png"] = FileImage, [".gif"] = FileImage, [".bmp"] = FileImage,
    [".svg"] = FileImage, [".webp"] = FileImage, [".tif"] = FileImage, [".tiff"] = FileImage, [".ico"] = FileImage,
    [".heic"] = FileImage, [".avif"] = FileImage,
    [".mp4"] = FileVideo, [".mov"] = FileVideo, [".avi"] = FileVideo, [".mkv"] = FileVideo, [".webm"] = FileVideo,
    [".wmv"] = FileVideo, [".m4v"] = FileVideo, [".mpg"] = FileVideo, [".mpeg"] = FileVideo,
    [".mp3"] = FileAudio, [".wav"] = FileAudio, [".ogg"] = FileAudio, [".flac"] = FileAudio, [".aac"] = FileAudio,
    [".m4a"] = FileAudio, [".wma"] = FileAudio,
    [".zip"] = FileArchive, [".rar"] = FileArchive, [".7z"] = FileArchive, [".tar"] = FileArchive, [".gz"] = FileArchive,
    [".bz2"] = FileArchive,
    [".pdf"] = FilePdf,
    [".txt"] = FileText, [".doc"] = FileText, [".docx"] = FileText, [".rtf"] = FileText, [".odt"] = FileText, [".md"] = FileText,
    [".xls"] = FileData, [".xlsx"] = FileData, [".csv"] = FileData, [".ods"] = FileData,
    [".ppt"] = FilePresentation, [".pptx"] = FilePresentation, [".odp"] = FilePresentation, [".key"] = FilePresentation,
    [".html"] = FileCode, [".htm"] = FileCode, [".css"] = FileCode, [".js"] = FileCode, [".ts"] = FileCode, [".cs"] = FileCode,
    [".razor"] = FileCode, [".json"] = FileCode, [".xml"] = FileCode, [".py"] = FileCode, [".java"] = FileCode, [".sql"] = FileCode
  };

  /// <summary>
  /// The icon for a file with the given extension (eg <c>.pdf</c>), or <see cref="File"/> if there isn't a specific one
  /// </summary>
  public static PixataSvgIcon ForFile(string? extension) =>
    extension is not null && FileIcons.TryGetValue(extension, out PixataSvgIcon? icon) ? icon : File;
}
