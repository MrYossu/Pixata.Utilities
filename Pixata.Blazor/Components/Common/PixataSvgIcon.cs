namespace Pixata.Blazor.Components;

/// <summary>
/// An icon drawn as inline SVG, so it needs no icon font. <see cref="Content"/> is the markup that goes inside the
/// <c>&lt;svg&gt;</c> element. By default the shapes are stroked in the current text colour; give a shape
/// <c>fill="currentColor" stroke="none"</c> to have it filled instead. The built-in icons are in <see cref="PixataSvgIcons"/>,
/// and you can create your own
/// </summary>
/// <param name="Name">A name for the icon, rendered as a <c>pixata-blazor-svg-icon-{name}</c> class</param>
/// <param name="Content">The SVG markup for the icon's shapes</param>
/// <param name="ViewBox">The SVG view box the shapes are drawn in</param>
public sealed record PixataSvgIcon(string Name, string Content, string ViewBox = "0 0 24 24");
