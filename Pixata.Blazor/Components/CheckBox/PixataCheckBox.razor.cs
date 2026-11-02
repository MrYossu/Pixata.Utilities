using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Pixata.Blazor.Components;

/// <summary>
/// A checkbox for a <c>bool</c> or <c>bool?</c> value, with the same parameters as Telerik's CheckBox
/// </summary>
/// <typeparam name="TValue"><c>bool</c> or <c>bool?</c></typeparam>
public partial class PixataCheckBox<TValue> {
  private static readonly bool IsSupportedType = typeof(TValue) == typeof(bool) || typeof(TValue) == typeof(bool?);

  /// <summary>
  /// Shows the checkbox as indeterminate (a dash), whatever its value. Cleared when the user clicks it. Can be bound with
  /// <c>@bind-Indeterminate</c>
  /// </summary>
  [Parameter]
  public bool Indeterminate { get; set; }

  [Parameter]
  public EventCallback<bool> IndeterminateChanged { get; set; }

  /// <summary>
  /// <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// <c>none</c>, <c>sm</c>, <c>md</c> (the default), <c>lg</c> or <c>full</c>. See <see cref="PixataThemeConstants.Rounded"/>
  /// </summary>
  [Parameter]
  public string? Rounded { get; set; }

  /// <summary>
  /// The form field name. When set, the checkbox is a native <c>&lt;input type="checkbox" value="true"&gt;</c> rather than a
  /// button, so it is posted with the form (as <c>true</c> when ticked, and not at all when not), and can be toggled in a
  /// statically rendered page. Matches <c>name="..."</c> in markup
  /// </summary>
  [Parameter]
  public string? Name { get; set; }

  [Parameter]
  public int? TabIndex { get; set; }

  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public string? AriaLabelledBy { get; set; }

  [Parameter]
  public string? AriaDescribedBy { get; set; }

  /// <summary>
  /// Raised when the user changes the checkbox, after <c>ValueChanged</c>, with the new value (a <c>bool</c>)
  /// </summary>
  [Parameter]
  public EventCallback<object> OnChange { get; set; }

  private bool IsNative =>
    !string.IsNullOrWhiteSpace(Name);

  private bool IsChecked =>
    Value is true;

  // A null bool? is shown as indeterminate, as Telerik's is
  private bool IsIndeterminate =>
    Indeterminate || Value is null;

  private string CssClass =>
    PixataCss.Classes("pixata-blazor-checkbox",
      $"pixata-blazor-checkbox-{(string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size)}",
      PixataCss.Rounded(Rounded),
      IsIndeterminate ? "pixata-blazor-checkbox-indeterminate" : IsChecked ? "pixata-blazor-checkbox-checked" : null,
      PixataCss.Disabled(Enabled),
      InvalidClass,
      Class);

  protected override void OnParametersSet() {
    if (!IsSupportedType) {
      throw new InvalidOperationException($"PixataCheckBox only works with bool and bool? values, not {typeof(TValue).Name}");
    }
    base.OnParametersSet();
  }

  private async Task Toggle() {
    if (!Enabled) {
      return;
    }
    // An indeterminate checkbox becomes checked, as a native one does
    bool wasIndeterminate = IsIndeterminate;
    bool newValue = wasIndeterminate || !IsChecked;
    if (Indeterminate) {
      Indeterminate = false;
      await IndeterminateChanged.InvokeAsync(false);
    }
    bool changed = await UpdateValue((TValue)(object)newValue);
    if (changed || wasIndeterminate) {
      await OnChange.InvokeAsync(newValue);
    }
  }
}
