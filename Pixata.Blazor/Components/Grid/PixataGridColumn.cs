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

  /// <summary>
  /// Set to false to stop the user filtering by this column when the grid has a <c>FilterMode</c>. Defaults to true
  /// </summary>
  [Parameter]
  public bool Filterable { get; set; } = true;

  /// <summary>
  /// Whether the column's filter menu has two conditions (the default) or a checkbox for each value. Only used when the
  /// grid's <c>FilterMode</c> is <c>FilterMenu</c>
  /// </summary>
  [Parameter]
  public PixataGridFilterMenuType FilterMenuType { get; set; } = PixataGridFilterMenuType.Menu;

  /// <summary>
  /// The operator the filter starts with. Defaults to Contains for text, and Is equal to for everything else
  /// </summary>
  [Parameter]
  public GridFilterOperator? DefaultFilterOperator { get; set; }

  /// <summary>
  /// Your own content for the column's cell in the filter row
  /// </summary>
  [Parameter]
  public RenderFragment<PixataGridFilterTemplateContext>? FilterCellTemplate { get; set; }

  /// <summary>
  /// Your own content for the column's filter menu, in place of the conditions
  /// </summary>
  [Parameter]
  public RenderFragment<PixataGridFilterTemplateContext>? FilterMenuTemplate { get; set; }

  /// <summary>
  /// The content of each data cell, in place of the formatted value. The context is the row's item. A column with a template
  /// doesn't need a <see cref="Field"/>, but if it has one, the column can still be sorted and filtered by it
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? Template { get; set; }

  /// <summary>
  /// The content of the header cell, in place of the title. Clicking it still sorts the column
  /// </summary>
  [Parameter]
  public RenderFragment? HeaderTemplate { get; set; }

  /// <summary>
  /// Called as each cell is rendered, so you can set a CSS class on it based on the item or the value
  /// </summary>
  [Parameter]
  public Action<PixataGridCellRenderEventArgs<TItem>>? OnCellRender { get; set; }

  private string? _resolvedField;
  private Func<TItem, object?>? _getter;
  private string? _attributeTitle;
  private string? _attributeFormat;
  private Type? _fieldType;

  /// <summary>
  /// The type of the field, or null for a column with no field
  /// </summary>
  internal Type? FieldType {
    get {
      Resolve();
      return _fieldType;
    }
  }

  internal PixataGrid<TItem>.FilterKind FilterKind =>
    FieldType is null ? PixataGrid<TItem>.FilterKind.None : PixataGrid<TItem>.KindOf(FieldType);

  internal bool IsNullable =>
    FieldType is not null && (!FieldType.IsValueType || Nullable.GetUnderlyingType(FieldType) is not null);

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

  internal object? CellValue(TItem item) {
    Resolve();
    return _getter?.Invoke(item);
  }

  // The cell's classes, including any set by OnCellRender
  internal string CellClassFor(TItem item) {
    if (OnCellRender is null) {
      return CellClass;
    }
    PixataGridCellRenderEventArgs<TItem> args = new(item, CellValue(item));
    OnCellRender(args);
    return Css.Join(CellClass, args.Class);
  }

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
    _fieldType = null;
    if (string.IsNullOrWhiteSpace(Field)) {
      return;
    }
    _fieldType = FieldAccessor.FieldType(typeof(TItem), Field);
    _getter = FieldAccessor.Getter<TItem>(Field);
    MemberInfo member = FieldAccessor.Member(typeof(TItem), Field);
    _attributeTitle = member.GetCustomAttribute<DisplayAttribute>()?.GetName();
    _attributeFormat = member.GetCustomAttribute<DisplayFormatAttribute>()?.DataFormatString;
  }

  protected override void OnParametersSet() =>
    // Done here as well as lazily, so that a bad field name throws as soon as the column is created, not part way through rendering
    Resolve();
}
