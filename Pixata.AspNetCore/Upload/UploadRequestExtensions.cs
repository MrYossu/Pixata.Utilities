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
    IFormCollection form = await request.ReadFormAsync(cancellationToken);
    IReadOnlyList<IFormFile> files = form.Files.GetFiles(options.SaveField);
    if (files.Count == 0) {
      return UploadReceiveResult.Failed($"The request has no files in the '{options.SaveField}' field");
    }
    Directory.CreateDirectory(folder);
    string metadataJson = form[options.MetadataField].ToString();
    if (!string.IsNullOrWhiteSpace(metadataJson)) {
      return await ReceiveChunk(files[0], metadataJson, folder, options, cancellationToken);
    }
    List<ReceivedFile> saved = [];
    foreach (IFormFile file in files) {
      string originalName = Sanitise(file.FileName);
      string? error = Validate(originalName, file.Length, options);
      if (error is not null) {
        return UploadReceiveResult.Failed(error);
      }
      // Written to a temporary file and then renamed, so that two requests saving files with the same name at the same time
      // don't trip over each other, and nobody sees a half-written file
      string temporaryPath = Path.Combine(folder, $"{Guid.NewGuid():N}{PartExtension}");
      try {
        await using (FileStream target = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
          await file.CopyToAsync(target, cancellationToken);
        }
        string path = TargetPath(folder, originalName, options);
        File.Move(temporaryPath, path, true);
        saved.Add(new(Path.GetFileName(path), originalName, path, file.Length, file.ContentType ?? ""));
      }
      catch (IOException ex) {
        File.Delete(temporaryPath);
        return UploadReceiveResult.Failed($"The file couldn't be saved: {ex.Message}");
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
        File.Delete(path);
        deleted++;
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
    if (chunk is null || chunk.TotalChunks < 1 || chunk.ChunkIndex < 0 || chunk.ChunkIndex >= chunk.TotalChunks) {
      return UploadReceiveResult.Failed("The chunk metadata isn't valid");
    }
    string fileId = SafeId().Replace(chunk.FileId, "");
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
    catch (IOException ex) {
      return UploadReceiveResult.Failed($"The chunk couldn't be saved: {ex.Message}", chunk);
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
    string path = TargetPath(folder, originalName, options);
    try {
      File.Move(partPath, path, true);
    }
    catch (IOException ex) {
      return UploadReceiveResult.Failed($"The file couldn't be saved: {ex.Message}", chunk);
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

  private static string TargetPath(string folder, string originalName, UploadReceiveOptions options) {
    string name = options.FileName is null ? originalName : Sanitise(options.FileName(originalName));
    string path = Path.Combine(folder, name);
    if (!options.Overwrite) {
      string stem = Path.GetFileNameWithoutExtension(name);
      string extension = Path.GetExtension(name);
      for (int n = 1; File.Exists(path); n++) {
        path = Path.Combine(folder, $"{stem} ({n}){extension}");
      }
    }
    if (!IsInside(folder, path)) {
      throw new InvalidOperationException($"The file name '{name}' points outside the upload folder");
    }
    return path;
  }

  /// <summary>
  /// Reduces a name sent by the browser to a plain file name: no folders, no characters the file system rejects, never empty,
  /// and never ending in <c>.part</c> (so it can't be mistaken for an unfinished chunked upload)
  /// </summary>
  public static string Sanitise(string fileName) {
    string name = fileName.Replace('\\', '/').Split('/').Last();
    char[] invalid = Path.GetInvalidFileNameChars();
    name = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
    if (name.Length == 0) {
      return "file";
    }
    return name.EndsWith(PartExtension, StringComparison.OrdinalIgnoreCase) ? $"{name}_" : name;
  }

  private static bool IsInside(string folder, string path) {
    string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    return Path.GetFullPath(path).StartsWith(root, StringComparison.Ordinal);
  }

  [GeneratedRegex("[^A-Za-z0-9_-]")]
  private static partial Regex SafeId();
}
