using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A group of radio buttons, one for each item in <see cref="Data"/>, for choosing one value
/// </summary>
/// <typeparam name="TItem">The type of the items in <see cref="Data"/></typeparam>
/// <typeparam name="TValue">The type of the value. For model data, this is the type of the <see cref="ValueField"/> property; for primitive data (a list of strings, say), it is the type of the items</typeparam>
public partial class PixataRadioGroup<TItem, TValue> {
  [Inject]
  private IJSRuntime Js { get; set; } = null!;

  /// <summary>
  /// The items to show, one radio button each. These can be primitives (strings, numbers, enum values...), in which case each
  /// item is both the text and the value, or models, in which case <see cref="TextField"/> and <see cref="ValueField"/> name
  /// the properties to use
  /// </summary>
  [Parameter]
  public IEnumerable<TItem>? Data { get; set; }

  /// <summary>
  /// The property of a model item that holds its text. Defaults to <c>Text</c>. Ignored for primitive data. Nested
  /// properties can be given with dots, eg <c>Address.City</c>
  /// </summary>
  [Parameter]
  public string TextField { get; set; } = "Text";

  /// <summary>
  /// The property of a model item that holds its value. Defaults to <c>Value</c>. Ignored for primitive data
  /// </summary>
  [Parameter]
  public string ValueField { get; set; } = "Value";

  /// <summary>
  /// The (bool) property of a model item that says whether the item is disabled. Optional
  /// </summary>
  [Parameter]
  public string? DisabledField { get; set; }

  /// <summary>
  /// Renders the label of each item, in place of its text. The item is the context
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? ItemTemplate { get; set; }

  /// <summary>
  /// Whether the radio buttons are stacked (<see cref="PixataRadioGroupLayout.Vertical"/>, the default) or in a row that
  /// wraps when it runs out of room (<see cref="PixataRadioGroupLayout.Horizontal"/>)
  /// </summary>
  [Parameter]
  public PixataRadioGroupLayout Layout { get; set; } = PixataRadioGroupLayout.Vertical;

  /// <summary>
  /// Whether each label comes before or after (the default) its radio button
  /// </summary>
  [Parameter]
  public PixataRadioGroupLabelPosition LabelPosition { get; set; } = PixataRadioGroupLabelPosition.After;

  /// <summary>
  /// The size of the radio buttons: <c>sm</c>, <c>md</c> (the default) or <c>lg</c>. See <see cref="PixataThemeConstants.Size"/>
  /// </summary>
  [Parameter]
  public string? Size { get; set; }

  /// <summary>
  /// The <c>name</c> shared by the radio buttons. A unique one is generated if you don't set it
  /// </summary>
  [Parameter]
  public string? Name { get; set; }

  /// <summary>
  /// The <c>tabindex</c> of the radio buttons
  /// </summary>
  [Parameter]
  public int? TabIndex { get; set; }

  [Parameter]
  public string? AriaLabel { get; set; }

  [Parameter]
  public string? AriaLabelledBy { get; set; }

  [Parameter]
  public string? AriaDescribedBy { get; set; }

  /// <summary>
  /// Raised when the user picks an item, with the new value
  /// </summary>
  [Parameter]
  public EventCallback<object> OnChange { get; set; }

  private static readonly bool IsPrimitive = IsPrimitiveType(typeof(TItem));

  private readonly string _generatedId = $"pixata-blazor-radiogroup-{Guid.NewGuid():N}";
  private ElementReference _element;

  private string GroupId =>
    string.IsNullOrWhiteSpace(Id) ? _generatedId : Id;

  private string GroupName =>
    string.IsNullOrWhiteSpace(Name) ? _generatedId : Name;

  private string SizeOrDefault =>
    string.IsNullOrWhiteSpace(Size) ? PixataThemeConstants.Size.Medium : Size;

  private string CssClass =>
    PixataCss.Classes("pixata-blazor-radiogroup",
      Layout == PixataRadioGroupLayout.Horizontal ? "pixata-blazor-radiogroup-horizontal" : "pixata-blazor-radiogroup-vertical",
      LabelPosition == PixataRadioGroupLabelPosition.Before ? "pixata-blazor-radiogroup-label-before" : null,
      PixataCss.Disabled(Enabled),
      InvalidClass,
      Class);

  /// <summary>
  /// Moves the focus to the radio button for the item at <paramref name="index"/> (counting from 0) in <see cref="Data"/>
  /// </summary>
  public Task FocusItemAsync(int index) =>
    PixataJs.InvokeVoid(Js, "focusSelector", _element, $"#{GroupId}-{index}");

  private TValue? GetValue(TItem item) =>
    PixataFieldReader.GetValue<TValue>(item, IsPrimitive ? null : ValueField);

  private string GetText(TItem item) =>
    PixataFieldReader.GetText(item, IsPrimitive ? null : TextField);

  private bool IsSelected(TItem item) =>
    EqualityComparer<TValue>.Default.Equals(GetValue(item)!, Value);

  private bool IsItemDisabled(TItem item) =>
    !IsPrimitive && !string.IsNullOrWhiteSpace(DisabledField) && PixataFieldReader.GetValue(item, DisabledField) is true;

  private async Task Select(TItem item) {
    if (!Enabled || IsItemDisabled(item)) {
      return;
    }
    TValue value = GetValue(item)!;
    if (await UpdateValue(value)) {
      await OnChange.InvokeAsync(value);
    }
  }

  private static bool IsPrimitiveType(Type type) {
    Type actual = Nullable.GetUnderlyingType(type) ?? type;
    return actual.IsPrimitive
      || actual.IsEnum
      || actual == typeof(string)
      || actual == typeof(decimal)
      || actual == typeof(DateTime)
      || actual == typeof(DateTimeOffset)
      || actual == typeof(DateOnly)
      || actual == typeof(TimeOnly)
      || actual == typeof(TimeSpan)
      || actual == typeof(Guid);
  }
}
