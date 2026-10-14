using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base of <see cref="PixataDropDownButtonItem"/> and <see cref="PixataSplitButtonItem"/>: an item in the menu of a
/// <see cref="PixataDropDownButton"/> or <see cref="PixataSplitButton"/>. Its child content is the item's text (or any other
/// markup you want in the item)
/// </summary>
public abstract class PixataMenuButtonItemBase : ComponentBase, IDisposable {
  [CascadingParameter]
  private PixataMenuButtonBase? Owner { get; set; }

  /// <summary>
  /// The item's content, usually its text
  /// </summary>
  [Parameter]
  public RenderFragment? ChildContent { get; set; }

  /// <summary>
  /// An icon shown before the content. A <see cref="PixataSvgIcon"/> (eg one from <see cref="PixataSvgIcons"/>), or the CSS
  /// class(es) of an icon font
  /// </summary>
  [Parameter]
  public object? Icon { get; set; }

  /// <summary>
  /// Raised when the item is clicked, tapped, or chosen with Enter or Space. The menu closes first
  /// </summary>
  [Parameter]
  public EventCallback<MouseEventArgs> OnClick { get; set; }

  /// <summary>
  /// When false, the item is greyed out, can't be chosen, and is skipped by the arrow keys
  /// </summary>
  [Parameter]
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// Extra CSS classes for the item's element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Any other attributes (eg <c>title</c> or <c>data-*</c>) are rendered on the item's element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  internal string ElementId { get; private set; } = "";

  protected override void OnInitialized() {
    if (Owner is null) {
      throw new InvalidOperationException($"{GetType().Name} can only be used inside the items of a PixataDropDownButton or PixataSplitButton");
    }
    ElementId = Owner.Register(this);
  }

  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    builder.OpenElement(0, "li");
    builder.AddMultipleAttributes(1, AdditionalAttributes);
    builder.AddAttribute(2, "id", ElementId);
    builder.AddAttribute(3, "class", PixataCss.Classes("pixata-blazor-list-item", "pixata-blazor-button-menu-item", PixataCss.Disabled(Enabled), Class));
    builder.AddAttribute(4, "role", "menuitem");
    builder.AddAttribute(5, "tabindex", "-1");
    builder.AddAttribute(6, "aria-disabled", Enabled ? null : "true");
    builder.AddAttribute(7, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, args => Owner!.ActivateItem(this, args)));
    builder.AddAttribute(8, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, () => Owner!.ItemFocused(this)));
    builder.OpenComponent<PixataIcon>(9);
    builder.AddComponentParameter(10, nameof(PixataIcon.Icon), Icon);
    builder.AddComponentParameter(11, nameof(PixataIcon.Class), "pixata-blazor-button-menu-item-icon");
    builder.CloseComponent();
    builder.OpenElement(12, "span");
    builder.AddAttribute(13, "class", "pixata-blazor-button-menu-item-text");
    builder.AddContent(14, ChildContent);
    builder.CloseElement();
    builder.CloseElement();
  }

  public void Dispose() {
    Owner?.Unregister(this);
    GC.SuppressFinalize(this);
  }
}
