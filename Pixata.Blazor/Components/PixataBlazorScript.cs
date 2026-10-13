namespace Pixata.Blazor.Components;

/// <summary>
/// The path of the JavaScript module used by the Telerik-style components. It includes a value that changes with each build
/// of this package, so browsers don't keep using an old copy of the module after the package is upgraded
/// </summary>
internal static class PixataBlazorScript {
  public static readonly string Path =
    $"./_content/Pixata.Blazor/pixata-blazor.js?v={typeof(PixataBlazorScript).Assembly.ManifestModule.ModuleVersionId:N}";
}
