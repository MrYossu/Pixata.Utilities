namespace Pixata.Blazor.Components;

/// <summary>
/// What pixata-dialogs.js needs to know about a dialog or window. Serialised with camel-cased names
/// </summary>
/// <param name="Kind"><c>dialog</c> or <c>window</c></param>
/// <param name="Modal">Whether the focus is kept inside the layer</param>
/// <param name="FocusSelector">The element to focus when the layer opens. If null, see <paramref name="FocusFirst"/></param>
/// <param name="FocusFirst">Focus the first focusable element in the layer when it opens, rather than the layer itself</param>
/// <param name="Draggable">Whether a window can be moved by its titlebar</param>
/// <param name="Resizable">Whether a window can be resized by its edges and corners</param>
/// <param name="ContainmentSelector">The element a window is kept inside, rather than the viewport</param>
internal sealed record PixataLayerOptions(string Kind, bool Modal, string? FocusSelector, bool FocusFirst, bool Draggable, bool Resizable, string? ContainmentSelector);
