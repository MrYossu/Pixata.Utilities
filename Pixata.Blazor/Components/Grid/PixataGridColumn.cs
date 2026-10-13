using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Pixata.Extensions;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// A column that shows the value of one field of the grid's items
/// </summary>
public class PixataGridColumn<TItem> : PixataGridColumnBase<TItem> {
  /// <summary>
  /// The name of the property or field to show, such as <c>nameof(Person.Name)</c>, or a path to a nested one, such as
  /// "Address.City". This is case sensitive
  /// </summary>
  [Parameter]
  public string? Field { get; set; }

  /// <summary>
  /// The header text. If not set, the grid uses the <c>[Display(Name = "...")]</c> attribute on the field if there is one,
  /// otherwise the field name split into words, so <c>FirstName</c> becomes "First name"
  /// </summary>
  [Parameter]
  public string? Title { get; set; }

  /// <summary>
  /// The width of the column, as a CSS width, such as "120px" or "20%". Columns without a width share the space left over
  /// </summary>
  [Parameter]
  public string? Width { get; set; }

  /// <summary>
  /// A .NET format string for the value, such as "{0:dd MMM yyyy}" or "{0:C2}". If not set, the grid uses the
  /// <c>[DisplayFormat(DataFormatString = "...")]</c> attribute on the field if there is one
  /// </summary>
  [Parameter]
  public string? DisplayFormat { get; set; }

  /// <summary>
  /// Set to false to hide the column. Null is treated as true
  /// </summary>
  [Parameter]
  public bool? Visible { get; set; }

  /// <summary>
  /// The horizontal alignment of the data cells. If not set, they use the page's normal alignment. The header isn't affected;
  /// use <see cref="HeaderClass"/> for that
  /// </summary>
  [Parameter]
  public PixataGridTextAlign? TextAlign { get; set; }

  /// <summary>
  /// A CSS class to add to the column's data cells
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// A CSS class to add to the column's header cell
  /// </summary>
  [Parameter]
  public string? HeaderClass { get; set; }

  /// <summary>
  /// Set to false to stop the user sorting by this column when the grid is <c>Sortable</c>. Defaults to true
  /// </summary>
  [Parameter]
  public bool Sortable { get; set; } = true;

  private string? _resolvedField;
  private Func<TItem, object?>? _getter;
  private string? _attributeTitle;
  private string? _attributeFormat;

  internal bool IsVisible =>
    Visible ?? true;

  internal string HeaderText {
    get {
      Resolve();
      return Title ?? _attributeTitle ?? Field?.Split('.')[^1].SplitCamelCase() ?? "";
    }
  }

  internal string? WidthStyle =>
    string.IsNullOrWhiteSpace(Width) ? null : $"width: {Width}";

  internal string CellClass =>
    Css.Join("pixata-blazor-grid-td", TextAlign switch {
      PixataGridTextAlign.Left => "pixata-blazor-grid-text-left",
      PixataGridTextAlign.Center => "pixata-blazor-grid-text-center",
      PixataGridTextAlign.Right => "pixata-blazor-grid-text-right",
      _ => null
    }, Class);

  internal string HeaderCellClass =>
    Css.Join("pixata-blazor-grid-th", HeaderClass);

  internal string CellText(TItem item) {
    Resolve();
    object? value = _getter?.Invoke(item);
    if (value is null) {
      return "";
    }
    string? format = DisplayFormat ?? _attributeFormat;
    return string.IsNullOrWhiteSpace(format)
      ? Convert.ToString(value, CultureInfo.CurrentCulture) ?? ""
      : string.Format(CultureInfo.CurrentCulture, format, value);
  }

  // Works out the getter and the attribute-based defaults once per field, rather than on every cell
  private void Resolve() {
    if (Field == _resolvedField) {
      return;
    }
    _resolvedField = Field;
    _getter = null;
    _attributeTitle = null;
    _attributeFormat = null;
    if (string.IsNullOrWhiteSpace(Field)) {
      return;
    }
    _getter = FieldAccessor.Getter<TItem>(Field);
    MemberInfo member = FieldAccessor.Member(typeof(TItem), Field);
    _attributeTitle = member.GetCustomAttribute<DisplayAttribute>()?.GetName();
    _attributeFormat = member.GetCustomAttribute<DisplayFormatAttribute>()?.DataFormatString;
  }

  protected override void OnParametersSet() =>
    // Done here as well as lazily, so that a bad field name throws as soon as the column is created, not part way through rendering
    Resolve();
}
