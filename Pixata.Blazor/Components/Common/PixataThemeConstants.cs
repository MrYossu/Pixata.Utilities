namespace Pixata.Blazor.Components;

/// <summary>
/// The values the components accept for their <c>Size</c>, <c>Rounded</c>, <c>FillMode</c> and <c>ThemeColor</c> parameters. They are
/// plain strings, so you can pass the string itself (<c>Size="lg"</c>) if you prefer
/// </summary>
public static partial class PixataThemeConstants {
  public static class Size {
    public const string Small = "sm";
    public const string Medium = "md";
    public const string Large = "lg";
  }

  public static class Rounded {
    public const string None = "none";
    public const string Small = "sm";
    public const string Medium = "md";
    public const string Large = "lg";
    public const string Full = "full";
  }

  public static class FillMode {
    public const string Solid = "solid";
    public const string Flat = "flat";
    public const string Outline = "outline";
  }

  public static class ThemeColor {
    public const string Base = "base";
    public const string Primary = "primary";
    public const string Secondary = "secondary";
    public const string Tertiary = "tertiary";
    public const string Info = "info";
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Dark = "dark";
    public const string Light = "light";
    public const string Inverse = "inverse";
  }

  public static class Button {
    public static class Size {
      public const string Small = "sm";
      public const string Medium = "md";
      public const string Large = "lg";
    }

    public static class Rounded {
      public const string None = "none";
      public const string Small = "sm";
      public const string Medium = "md";
      public const string Large = "lg";
      public const string Full = "full";
    }

    public static class FillMode {
      public const string Solid = "solid";
      public const string Flat = "flat";
      public const string Outline = "outline";
      public const string Link = "link";
      public const string Clear = "clear";
    }

    public static class ThemeColor {
      public const string Base = "base";
      public const string Primary = "primary";
      public const string Secondary = "secondary";
      public const string Tertiary = "tertiary";
      public const string Info = "info";
      public const string Success = "success";
      public const string Warning = "warning";
      public const string Error = "error";
      public const string Dark = "dark";
      public const string Light = "light";
      public const string Inverse = "inverse";
    }
  }
}
