using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Pixata.Blazor.Components;

/// <summary>
/// The base for the components that edit a value (the drop-downs, the switch, the radio group and so on). It provides the
/// two-way bindable <c>Value</c>, and works inside an <see cref="EditForm"/> (when bound with <c>@bind-Value</c>, or given a
/// <c>ValueExpression</c>), telling the form when the value changes and exposing <see cref="IsInvalid"/> so the component can
/// show its invalid state. Unlike <see cref="InputBase{TValue}"/>, it doesn't need an <see cref="EditForm"/> to work
/// </summary>
/// <remarks>Derived components that override <see cref="OnParametersSet"/> must call the base method</remarks>
public abstract class PixataInputBase<TValue> : ComponentBase, IDisposable {
  [CascadingParameter]
  private EditContext? CascadedEditContext { get; set; }

  /// <summary>
  /// The value. Use <c>@bind-Value</c> for two-way binding
  /// </summary>
  [Parameter]
  public TValue Value { get; set; } = default!;

  [Parameter]
  public EventCallback<TValue> ValueChanged { get; set; }

  /// <summary>
  /// Identifies the bound field, so validation works inside an <see cref="EditForm"/>. Set for you by <c>@bind-Value</c>
  /// </summary>
  [Parameter]
  public Expression<Func<TValue>>? ValueExpression { get; set; }

  /// <summary>
  /// When false, the component is greyed out and can't be used
  /// </summary>
  [Parameter]
  public bool Enabled { get; set; } = true;

  /// <summary>
  /// The id rendered on the focusable element, so a <c>&lt;label for="..."&gt;</c> can point at it
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Extra CSS classes for the component's outer element
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Any other attributes (eg <c>title</c>, <c>aria-label</c>, <c>data-*</c>) are rendered on the component's outer element
  /// </summary>
  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  /// <summary>
  /// The <see cref="EditForm"/>'s context, if the component is inside one
  /// </summary>
  protected EditContext? EditContext { get; private set; }

  /// <summary>
  /// The bound field, if the component is inside an <see cref="EditForm"/> and has a <see cref="ValueExpression"/>
  /// </summary>
  protected FieldIdentifier? FieldIdentifier { get; private set; }

  /// <summary>
  /// True if the form has validation messages for the bound field
  /// </summary>
  protected bool IsInvalid =>
    EditContext is not null && FieldIdentifier is { } identifier && EditContext.GetValidationMessages(identifier).Any();

  /// <summary>
  /// <c>pixata-blazor-invalid</c> when <see cref="IsInvalid"/> is true
  /// </summary>
  protected string? InvalidClass =>
    IsInvalid ? "pixata-blazor-invalid" : null;

  protected override void OnParametersSet() {
    if (!ReferenceEquals(CascadedEditContext, EditContext)) {
      if (EditContext is not null) {
        EditContext.OnValidationStateChanged -= ValidationStateChanged;
      }
      EditContext = CascadedEditContext;
      if (EditContext is not null) {
        EditContext.OnValidationStateChanged += ValidationStateChanged;
      }
    }
    FieldIdentifier = ValueExpression is not null && EditContext is not null
      ? Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(ValueExpression)
      : null;
  }

  /// <summary>
  /// Sets <see cref="Value"/>, raises <see cref="ValueChanged"/> and tells the <see cref="EditForm"/> (if any) that the field has
  /// changed, so it is validated. Does nothing if the value hasn't changed
  /// </summary>
  /// <returns>True if the value changed</returns>
  protected async Task<bool> UpdateValue(TValue value) {
    if (EqualityComparer<TValue>.Default.Equals(Value, value)) {
      return false;
    }
    Value = value;
    await ValueChanged.InvokeAsync(value);
    if (EditContext is not null && FieldIdentifier is { } identifier) {
      EditContext.NotifyFieldChanged(identifier);
    }
    return true;
  }

  private void ValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) =>
    InvokeAsync(StateHasChanged);

  protected virtual void Dispose(bool disposing) {
    if (disposing && EditContext is not null) {
      EditContext.OnValidationStateChanged -= ValidationStateChanged;
    }
  }

  public void Dispose() {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}
