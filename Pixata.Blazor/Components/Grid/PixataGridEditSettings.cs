using System;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// The base for settings components that go in the grid's <c>GridSettings</c>
/// </summary>
public abstract class PixataGridSettingsBase<TItem> : ComponentBase, IDisposable {
  [CascadingParameter]
  internal PixataGrid<TItem>? Grid { get; set; }

  protected override void OnInitialized() {
    if (Grid is null) {
      throw new InvalidOperationException($"{GetType().Name.Split('`')[0]} must be inside the GridSettings of a PixataGrid");
    }
    Register(Grid, true);
  }

  internal abstract void Register(PixataGrid<TItem> grid, bool add);

  protected override bool ShouldRender() =>
    false;

  public void Dispose() {
    if (Grid is not null) {
      Register(Grid, false);
    }
    GC.SuppressFinalize(this);
  }
}

/// <summary>
/// Settings for the popup window used when the grid's <c>EditMode</c> is <c>Popup</c>
/// </summary>
public class PixataGridPopupEditSettings<TItem> : PixataGridSettingsBase<TItem> {
  /// <summary>
  /// The window's title. Defaults to "Edit", or "Add new record" for a new item
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  /// <summary>
  /// The window's width, as a CSS width
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// The window's maximum height, as a CSS height. The form scrolls if it's taller
  /// </summary>
  [Parameter]
  public string? MaxHeight { get; set; }

  /// <summary>
  /// A CSS class for the window
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  internal override void Register(PixataGrid<TItem> grid, bool add) {
    if (add) {
      grid.PopupEditSettings = this;
    } else if (grid.PopupEditSettings == this) {
      grid.PopupEditSettings = null;
    }
  }
}

/// <summary>
/// Where the popup form's buttons go
/// </summary>
public enum PixataGridFormButtonsLayout {
  Start,
  Center,
  End,
  Stretch
}

/// <summary>
/// Settings for the form in the popup edit window
/// </summary>
public class PixataGridPopupEditFormSettings<TItem> : PixataGridSettingsBase<TItem> {
  /// <summary>
  /// The number of columns the fields are laid out in. Defaults to 1
  /// </summary>
  [Parameter]
  public int Columns { get; set; } = 1;

  /// <summary>
  /// Where the Update and Cancel buttons go. Defaults to the start
  /// </summary>
  [Parameter]
  public PixataGridFormButtonsLayout ButtonsLayout { get; set; } = PixataGridFormButtonsLayout.Start;

  /// <summary>
  /// Your own form, in place of the generated one. The context is the item being edited
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? FormTemplate { get; set; }

  /// <summary>
  /// Your own buttons, in place of Update and Cancel. Use command buttons with Save and Cancel in them
  /// </summary>
  [Parameter]
  public RenderFragment? ButtonsTemplate { get; set; }

  internal override void Register(PixataGrid<TItem> grid, bool add) {
    if (add) {
      grid.PopupEditFormSettings = this;
    } else if (grid.PopupEditFormSettings == this) {
      grid.PopupEditFormSettings = null;
    }
  }
}

/// <summary>
/// Settings for validating edited items
/// </summary>
public class PixataGridValidationSettings<TItem> : PixataGridSettingsBase<TItem> {
  /// <summary>
  /// Whether edited items are validated before they're saved. Defaults to true
  /// </summary>
  [Parameter]
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// The validator to use, in place of the default <c>DataAnnotationsValidator</c>, such as
  /// <c>&lt;FluentValidationValidator /&gt;</c>
  /// </summary>
  [Parameter]
  public RenderFragment? ValidatorTemplate { get; set; }

  internal override void Register(PixataGrid<TItem> grid, bool add) {
    if (add) {
      grid.ValidationSettings = this;
    } else if (grid.ValidationSettings == this) {
      grid.ValidationSettings = null;
    }
  }
}
