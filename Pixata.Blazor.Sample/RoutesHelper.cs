namespace Pixata.Blazor.Sample;

/// <summary>
/// The routes of the sample's API endpoints, so they aren't hard-coded in both Program.cs and the pages that call them
/// </summary>
public static class RoutesHelper {
  /// <summary>
  /// Receives the files posted by the upload samples. Add <c>?delay=500</c> to have it wait (in milliseconds, up to 2000)
  /// before answering, so you can see the progress and cancel or pause an upload
  /// </summary>
  public const string UploadSave = "/api/upload/save";

  /// <summary>
  /// Deletes files saved by <see cref="UploadSave"/>
  /// </summary>
  public const string UploadRemove = "/api/upload/remove";

  /// <summary>
  /// Always answers with a 500, to show what happens when an upload fails
  /// </summary>
  public const string UploadFail = "/api/upload/fail";

  public static string UploadSaveWithDelay(int milliseconds) =>
    $"{UploadSave}?delay={milliseconds}";
}
