using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Pixata.AspNetCore.Upload;

/// <summary>
/// Receives the files that a browser posts to an endpoint (eg from the Pixata.Blazor <c>PixataUpload</c> component), saving
/// them to a folder, putting chunked uploads back together, and deleting files when asked to
/// </summary>
public static partial class UploadRequestExtensions {
  /// <summary>
  /// The extension added to a file while its chunks are arriving
  /// </summary>
  public const string PartExtension = ".part";

  private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

  // The characters that Windows doesn't allow in a file name. They are replaced on every platform, so a name is saved the
  // same way wherever the server runs
  private const string InvalidNameChars = "<>:\"/\\|?*";

  // Names that Windows treats as devices rather than files, with or without an extension
  private static readonly HashSet<string> ReservedNames = new([
    "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "CLOCK$", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³",
    .. Enumerable.Range(0, 10).SelectMany(n => new[] { $"COM{n}", $"LPT{n}" })
  ], StringComparer.OrdinalIgnoreCase);

  // The longest name (in UTF-8 bytes) a file is saved under. File systems allow 255, and this leaves room for " (1)"
  private const int MaxNameBytes = 200;

  /// <summary>
  /// Saves the file(s) posted in the request to <paramref name="folder"/>. A plain upload is saved straight away. A chunk of a
  /// chunked upload is written to a <c>.part</c> file, which is renamed to the file's own name when the last chunk arrives.
  /// File names are sanitised, so a request can't write outside the folder
  /// </summary>
  /// <param name="request">The request</param>
  /// <param name="folder">The folder to save to. It is created if it doesn't exist. Don't use a folder the web server serves files from, unless you want anyone to be able to download what is uploaded</param>
  /// <param name="configure">Sets the options, eg the form field names and the extensions and sizes allowed</param>
  /// <param name="cancellationToken">Cancels reading the request</param>
  /// <returns>The files saved, or why the request was rejected</returns>
  public static async Task<UploadReceiveResult> ReceiveUpload(this HttpRequest request, string folder, Action<UploadReceiveOptions>? configure = null, CancellationToken cancellationToken = default) {
    UploadReceiveOptions options = new();
    configure?.Invoke(options);
    if (!request.HasFormContentType) {
      return UploadReceiveResult.Failed("The request isn't a form post");
    }
    IFormCollection form;
    try {
      form = await request.ReadFormAsync(cancellationToken);
    }
    catch (InvalidDataException) {
      // The form is bigger than the FormOptions limits allow
      return UploadReceiveResult.Failed("The request is too big, or has too many parts");
    }
    IReadOnlyList<IFormFile> files = form.Files.GetFiles(options.SaveField);
    if (files.Count == 0) {
      return UploadReceiveResult.Failed($"The request has no files in the '{options.SaveField}' field");
    }
    Directory.CreateDirectory(folder);
    string metadataJson = form[options.MetadataField].ToString();
    if (!string.IsNullOrWhiteSpace(metadataJson)) {
      return await ReceiveChunk(files[0], metadataJson, folder, options, cancellationToken);
    }
    // Every file is checked before any is saved, so a request with one bad file doesn't leave the others behind
    foreach (IFormFile file in files) {
      if (Validate(Sanitise(file.FileName), file.Length, options) is { } error) {
        return UploadReceiveResult.Failed(error);
      }
    }
    List<ReceivedFile> saved = [];
    foreach (IFormFile file in files) {
      string originalName = Sanitise(file.FileName);
      // Written to a temporary file and then renamed, so that two requests saving files with the same name at the same time
      // don't trip over each other, and nobody sees a half-written file
      string temporaryPath = Path.Combine(folder, $"{Guid.NewGuid():N}{PartExtension}");
      try {
        await using (FileStream target = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
          await file.CopyToAsync(target, cancellationToken);
        }
        string path = MoveIntoPlace(temporaryPath, folder, TargetName(originalName, options), options.Overwrite);
        saved.Add(new(Path.GetFileName(path), originalName, path, file.Length, file.ContentType ?? ""));
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) {
        // The message isn't passed on, as it can include the server's folder names
        return new() { Error = "The file couldn't be saved", Exception = ex, Files = saved };
      }
      finally {
        // Only still there if something went wrong
        File.Delete(temporaryPath);
      }
    }
    return new() { Files = saved };
  }

  /// <summary>
  /// Deletes the files named in the request from <paramref name="folder"/>. Use this in the endpoint that a component's
  /// remove URL points at. Names are sanitised, so a request can't delete anything outside the folder
  /// </summary>
  /// <param name="request">The request</param>
  /// <param name="folder">The folder the files were saved to</param>
  /// <param name="removeField">The form field that holds the file names. Default <c>files</c></param>
  /// <param name="cancellationToken">Cancels reading the request</param>
  /// <returns>The names of the files that were deleted. A name that wasn't found is skipped</returns>
  public static async Task<List<string>> RemoveUploads(this HttpRequest request, string folder, string removeField = "files", CancellationToken cancellationToken = default) {
    List<string> removed = [];
    if (!request.HasFormContentType) {
      return removed;
    }
    IFormCollection form = await request.ReadFormAsync(cancellationToken);
    StringValues names = form[removeField];
    foreach (string? name in names) {
      if (string.IsNullOrWhiteSpace(name)) {
        continue;
      }
      string path = Path.Combine(folder, Sanitise(name));
      if (IsInside(folder, path) && File.Exists(path)) {
        File.Delete(path);
        removed.Add(Path.GetFileName(path));
      }
    }
    return removed;
  }

  /// <summary>
  /// Deletes the <c>.part</c> files of chunked uploads that were never finished (the user cancelled, or closed the page),
  /// and haven't been written to for <paramref name="olderThan"/>. Call it now and then, eg from a background service
  /// </summary>
  /// <returns>The number of files deleted</returns>
  public static int DeleteAbandonedChunks(string folder, TimeSpan olderThan) {
    if (!Directory.Exists(folder)) {
      return 0;
    }
    DateTime cutOff = DateTime.UtcNow - olderThan;
    int deleted = 0;
    foreach (string path in Directory.EnumerateFiles(folder, $"*{PartExtension}")) {
      if (File.GetLastWriteTimeUtc(path) < cutOff) {
        try {
          File.Delete(path);
          deleted++;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
          // Still in use, so leave it for next time
        }
      }
    }
    return deleted;
  }

  private static async Task<UploadReceiveResult> ReceiveChunk(IFormFile file, string metadataJson, string folder, UploadReceiveOptions options, CancellationToken cancellationToken) {
    UploadChunkMetadata? chunk;
    try {
      chunk = JsonSerializer.Deserialize<UploadChunkMetadata>(metadataJson, JsonOptions);
    }
    catch (JsonException) {
      chunk = null;
    }
    if (chunk is null || chunk.TotalChunks < 1 || chunk.ChunkIndex < 0 || chunk.ChunkIndex >= chunk.TotalChunks || chunk.FileSize < 0 || chunk.Offset < 0) {
      return UploadReceiveResult.Failed("The chunk metadata isn't valid");
    }
    string fileId = SafeId().Replace(chunk.FileId ?? "", "");
    if (fileId.Length == 0 || fileId.Length > 100) {
      return UploadReceiveResult.Failed("The chunk metadata doesn't have a valid file id", chunk);
    }
    string originalName = Sanitise(string.IsNullOrWhiteSpace(chunk.FileName) ? file.FileName : chunk.FileName);
    string? error = Validate(originalName, chunk.FileSize, options);
    if (error is not null) {
      return UploadReceiveResult.Failed(error, chunk);
    }
    string partPath = Path.Combine(folder, $"{fileId}{PartExtension}");
    try {
      await WriteChunk(file, chunk, partPath, cancellationToken);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
      // The message isn't passed on, as it can include the server's folder names
      return new() { Error = "The chunk couldn't be saved", Exception = ex, Chunk = chunk };
    }
    catch (InvalidDataException ex) {
      File.Delete(partPath);
      return UploadReceiveResult.Failed(ex.Message, chunk);
    }
    if (!chunk.IsLast) {
      return new() { Chunk = chunk };
    }
    long size = new FileInfo(partPath).Length;
    if (size != chunk.FileSize) {
      File.Delete(partPath);
      return UploadReceiveResult.Failed("The file is incomplete", chunk);
    }
    string path;
    try {
      path = MoveIntoPlace(partPath, folder, TargetName(originalName, options), options.Overwrite);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) {
      return new() { Error = "The file couldn't be saved", Exception = ex, Chunk = chunk };
    }
    string contentType = string.IsNullOrWhiteSpace(chunk.ContentType) ? file.ContentType ?? "" : chunk.ContentType;
    return new() { Chunk = chunk, Files = [new(Path.GetFileName(path), originalName, path, size, contentType)] };
  }

  private static async Task WriteChunk(IFormFile file, UploadChunkMetadata chunk, string partPath, CancellationToken cancellationToken) {
    await using FileStream part = new(partPath, chunk.ChunkIndex == 0 ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    long position = chunk.Offset ?? part.Length;
    if (position > part.Length) {
      throw new InvalidDataException("A chunk is missing");
    }
    // Setting the length drops anything after this chunk's offset, so a chunk that is sent twice isn't added twice
    part.SetLength(position);
    part.Seek(position, SeekOrigin.Begin);
    await file.CopyToAsync(part, cancellationToken);
    if (part.Length > chunk.FileSize) {
      throw new InvalidDataException("The chunks are bigger than the file");
    }
  }

  private static string? Validate(string fileName, long size, UploadReceiveOptions options) {
    string extension = Path.GetExtension(fileName);
    if (options.AllowedExtensions.Count > 0 && !options.AllowedExtensions.Any(e => string.Equals(Dotted(e), extension, StringComparison.OrdinalIgnoreCase))) {
      return "File type not allowed";
    }
    if (options.MaxFileSize is { } max && size > max) {
      return "File size too large";
    }
    if (options.MinFileSize is { } min && size < min) {
      return "File size too small";
    }
    return null;
  }

  private static string Dotted(string extension) =>
    extension.StartsWith('.') ? extension : $".{extension}";

  private static string TargetName(string originalName, UploadReceiveOptions options) =>
    options.FileName is null ? originalName : Sanitise(options.FileName(originalName));

  /// <summary>
  /// Renames a finished file to its name in the folder. When not overwriting, the name is claimed first by creating an empty
  /// file with it (which fails if the name is taken, even by a request doing the same thing at the same time), adding a
  /// number to the name until that succeeds. Checking whether the name is free and then moving the file wouldn't do, as two
  /// requests could both find the name free, and the second would overwrite the first
  /// </summary>
  /// <returns>The path the file was saved to</returns>
  private static string MoveIntoPlace(string source, string folder, string name, bool overwrite) {
    string path = Path.Combine(folder, name);
    if (!IsInside(folder, path)) {
      throw new InvalidOperationException($"The file name '{name}' points outside the upload folder");
    }
    if (!overwrite) {
      string stem = Path.GetFileNameWithoutExtension(name);
      string extension = Path.GetExtension(name);
      for (int n = 1; ; n++) {
        try {
          new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None).Dispose();
          break;
        }
        catch (IOException) when (File.Exists(path)) {
          path = Path.Combine(folder, $"{stem} ({n}){extension}");
        }
      }
    }
    File.Move(source, path, true);
    return path;
  }

  /// <summary>
  /// Reduces a name sent by the browser to a plain file name that is safe to save on any platform: no folders, no characters
  /// that Windows rejects, no control or invisible formatting characters (such as the ones that reverse the text, to disguise
  /// <c>exe.pdf</c> as <c>fdp.exe</c>), not one of the names Windows reserves for devices (eg <c>CON</c> or <c>nul.txt</c>),
  /// at most 200 bytes long (keeping the extension), never empty, and never ending in <c>.part</c> (so it can't be mistaken
  /// for an unfinished chunked upload)
  /// </summary>
  public static string Sanitise(string fileName) {
    string name = fileName.Replace('\\', '/').Split('/').Last();
    StringBuilder cleaned = new(name.Length);
    // Runes rather than chars, so a lone half of a surrogate pair becomes U+FFFD rather than an invalid name
    foreach (Rune rune in name.EnumerateRunes()) {
      cleaned.Append(IsInvalid(rune) ? "_" : rune.ToString());
    }
    name = Shorten(Trimmed(cleaned.ToString()));
    if (name.Length == 0) {
      return "file";
    }
    if (ReservedNames.Contains(name.Split('.')[0].TrimEnd(' '))) {
      name = $"_{name}";
    }
    return name.EndsWith(PartExtension, StringComparison.OrdinalIgnoreCase) ? $"{name}_" : name;
  }

  private static bool IsInvalid(Rune rune) =>
    Rune.IsControl(rune)
    || (rune.IsAscii && InvalidNameChars.Contains((char)rune.Value))
    // Zero-width joiners are formatting characters too, but are part of some scripts and emoji
    || (Rune.GetUnicodeCategory(rune) == UnicodeCategory.Format && rune.Value is not (0x200C or 0x200D));

  // Windows drops dots and spaces from the end of a name, so they're removed here, so the name is the same everywhere
  private static string Trimmed(string name) =>
    name.Trim().TrimEnd('.', ' ');

  private static string Shorten(string name) {
    if (Encoding.UTF8.GetByteCount(name) <= MaxNameBytes) {
      return name;
    }
    string extension = Path.GetExtension(name);
    if (Encoding.UTF8.GetByteCount(extension) > MaxNameBytes / 4) {
      extension = "";
    }
    int budget = MaxNameBytes - Encoding.UTF8.GetByteCount(extension);
    StringBuilder stem = new();
    foreach (Rune rune in name[..^extension.Length].EnumerateRunes()) {
      budget -= rune.Utf8SequenceLength;
      if (budget < 0) {
        break;
      }
      stem.Append(rune.ToString());
    }
    return Trimmed($"{Trimmed(stem.ToString())}{extension}");
  }

  private static bool IsInside(string folder, string path) {
    string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    return Path.GetFullPath(path).StartsWith(root, StringComparison.Ordinal);
  }

  [GeneratedRegex("[^A-Za-z0-9_-]")]
  private static partial Regex SafeId();
}
