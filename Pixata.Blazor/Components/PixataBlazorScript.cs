namespace Pixata.Blazor.Components;

/// <summary>
/// The paths of the package's JavaScript modules. Each includes a value that changes with each build of this package, so
/// browsers don't keep using an old copy of a module after the package is upgraded
/// </summary>
internal static class PixataBlazorScript {
  private static readonly string Version = typeof(PixataBlazorScript).Assembly.ManifestModule.ModuleVersionId.ToString("N");

  /// <summary>
  /// The shared module, pixata-blazor.js
  /// </summary>
  public static readonly string Path = Versioned("pixata-blazor.js");

  /// <summary>
  /// The path of one of the package's static files, eg <c>pixata-dialogs.js</c> or <c>Components/Chart/PixataChart.razor.js</c>,
  /// with the build's version
  /// </summary>
  public static string Versioned(string file) =>
    $"./_content/Pixata.Blazor/{file}?v={Version}";
}
