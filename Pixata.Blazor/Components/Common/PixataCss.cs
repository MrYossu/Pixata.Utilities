using System.Linq;

namespace Pixata.Blazor.Components;

/// <summary>
/// Builds the CSS class lists the components render. Every class is prefixed with <c>pixata-blazor-</c>, and the colours,
/// sizes and spacing behind them come from the <c>--pixata-blazor-*</c> custom properties in pixata.css
/// </summary>
public static class PixataCss {
  /// <summary>
  /// Joins the classes that aren't null or blank with a space
  /// </summary>
  public static string Classes(params string?[] classes) =>
    string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c!.Trim()));

  /// <summary>
  /// The classes for a button, eg <c>pixata-blazor-button pixata-blazor-button-md pixata-blazor-rounded-md pixata-blazor-button-solid pixata-blazor-button-primary</c>
  /// </summary>
  /// <param name="themeColor">One of the <see cref="PixataThemeConstants.Button.ThemeColor"/> values</param>
  /// <param name="fillMode">One of the <see cref="PixataThemeConstants.Button.FillMode"/> values</param>
  /// <param name="rounded">One of the <see cref="PixataThemeConstants.Button.Rounded"/> values</param>
  /// <param name="size">One of the <see cref="PixataThemeConstants.Button.Size"/> values</param>
  /// <param name="iconOnly">True for a button that only holds an icon, so it is square</param>
  public static string Button(string? themeColor, string? fillMode, string? rounded, string? size, bool iconOnly = false) =>
    Classes("pixata-blazor-button",
      $"pixata-blazor-button-{Or(size, PixataThemeConstants.Button.Size.Medium)}",
      Rounded(rounded),
      $"pixata-blazor-button-{Or(fillMode, PixataThemeConstants.Button.FillMode.Solid)}",
      $"pixata-blazor-button-{Or(themeColor, PixataThemeConstants.Button.ThemeColor.Base)}",
      iconOnly ? "pixata-blazor-icon-button" : null);

  /// <summary>
  /// The classes for the wrapper of an input (a text box, a drop-down list and so on), eg <c>pixata-blazor-input pixata-blazor-input-md pixata-blazor-rounded-md pixata-blazor-input-solid</c>
  /// </summary>
  public static string Input(string? size, string? rounded, string? fillMode) =>
    Classes("pixata-blazor-input",
      $"pixata-blazor-input-{Or(size, PixataThemeConstants.Size.Medium)}",
      Rounded(rounded),
      $"pixata-blazor-input-{Or(fillMode, PixataThemeConstants.FillMode.Solid)}");

  /// <summary>
  /// The class for a <see cref="PixataThemeConstants.Rounded"/> value, eg <c>pixata-blazor-rounded-md</c>
  /// </summary>
  public static string Rounded(string? rounded) =>
    $"pixata-blazor-rounded-{Or(rounded, PixataThemeConstants.Rounded.Medium)}";

  /// <summary>
  /// <c>pixata-blazor-disabled</c> when <paramref name="enabled"/> is false
  /// </summary>
  public static string? Disabled(bool enabled) =>
    enabled ? null : "pixata-blazor-disabled";

  private static string Or(string? value, string fallback) =>
    string.IsNullOrWhiteSpace(value) ? fallback : value;
}
