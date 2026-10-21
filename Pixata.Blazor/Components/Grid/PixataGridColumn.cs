using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
  /// An identifier for the column, for the methods that take one, such as <c>AutoFitColumn</c>
  /// </summary>
  [Parameter]
  public string? Id { get; set; }

  /// <summary>
  /// Columns to show under this one, which then has a header spanning theirs and no cells of its own
  /// </summary>
  [Parameter]
  public RenderFragment? Columns { get; set; }

  /// <summary>
  /// Whether the user can resize this column, when the grid is <c>Resizable</c>. Defaults to true
  /// </summary>
  [Parameter]
  public bool Resizable { get; set; } = true;

  /// <summary>
  /// The narrowest the user can make the column, in pixels
  /// </summary>
  [Parameter]
  public double? MinResizableWidth { get; set; }

  /// <summary>
  /// The widest the user can make the column, in pixels
  /// </summary>
  [Parameter]
  public double? MaxResizableWidth { get; set; }

  /// <summary>
  /// Whether the user can move this column, when the grid is <c>Reorderable</c>. Defaults to true
  /// </summary>
  [Parameter]
  public bool Reorderable { get; set; } = true;

  /// <summary>
  /// Freezes the column, so it stays put when the grid scrolls sideways. It needs a <see cref="Width"/> in pixels
  /// </summary>
  [Parameter]
  public bool Locked { get; set; }

  /// <summary>
  /// Whether the user can freeze and unfreeze the column from the column menu. Defaults to true
  /// </summary>
  [Parameter]
  public bool Lockable { get; set; } = true;

  /// <summary>
  /// Whether the column has a column menu, when the grid's <c>ShowColumnMenu</c> is set. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowColumnMenu { get; set; } = true;

  /// <summary>
  /// Whether the column is listed in the column menu's column chooser, to be shown or hidden. Defaults to true
  /// </summary>
  [Parameter]
  public bool VisibleInColumnChooser { get; set; } = true;

  [CascadingParameter(Name = ParentCascadeName)]
  internal PixataGridColumn<TItem>? ParentColumn { get; set; }

  internal const string ParentCascadeName = "PixataGridParentColumn";

  // What the user has changed, which takes the place of the parameters
  internal string? RuntimeWidth { get; set; }
  internal bool? RuntimeVisible { get; set; }
  internal bool? RuntimeLocked { get; set; }
  internal int? RuntimeOrder { get; set; }

  // The columns inside this one, if it has Columns
  internal List<PixataGridColumn<TItem>> Children { get; } = [];

  // Identifies the column in the page, for the JavaScript
  internal string Key { get; } = Guid.NewGuid().ToString("N")[..8];

  // Set by the grid as it renders, for a frozen column
  internal string? StickyStyle { get; set; }
  internal double? StickyLeft { get; set; }
  internal double? StickyRight { get; set; }

  internal bool IsGroupColumn =>
    Columns is not null;

  internal string? CurrentWidth =>
    RuntimeWidth ?? Width;

  // A frozen column, or one inside a frozen column
  internal bool IsLocked =>
    RuntimeLocked ?? (Locked || ParentColumn?.IsLocked == true);

  internal string? LockedClass =>
    StickyStyle is null ? null : "pixata-blazor-grid-locked";

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
  /// For a <see cref="DateTime"/> field, makes the filter compare the date only, ignoring the time, so "on 21/10/2026" finds
  /// the items at any time that day. It is true by default for a <see cref="PixataDateGridColumn{TItem}"/>, and false for
  /// other columns. See <see cref="GridFilterDescriptor.IgnoreTime"/>
  /// </summary>
  [Parameter]
  public bool IgnoreTime { get; set; }

  /// <summary>
  /// The operators offered in the column's filter row and filter menu, in place of the defaults for the field's type, such
  /// as <see cref="PixataGridFilterOperators.StringOperators"/>. The filter starts with
  /// <see cref="DefaultFilterOperator"/> if set, otherwise the usual default if it's in the list, otherwise the first one
  /// </summary>
  [Parameter]
  public List<PixataGridFilterListOperator>? FilterOperators { get; set; }

  /// <summary>
  /// Set to false to hide the operator drop-down and the clear button in the column's filter row cell, leaving just the
  /// editor. Defaults to true
  /// </summary>
  [Parameter]
  public bool ShowFilterCellButtons { get; set; } = true;

  /// <summary>
  /// Set to false to hide just the clear button in the column's filter row cell, keeping the operator drop-down. Defaults
  /// to true. Has no effect if <see cref="ShowFilterCellButtons"/> is false
  /// </summary>
  [Parameter]
  public bool ShowFilterClearButton { get; set; } = true;

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
  /// The content of each data cell, in place of the formatted value. The context is the row's item, as an <see cref="object"/>
  /// (as in Telerik's grid), so templates built by the <c>TemplateHelper</c> can be used. A column with a template doesn't need
  /// a <see cref="Field"/>, but if it has one, the column can still be sorted and filtered by it
  /// </summary>
  [Parameter]
  public RenderFragment<object>? Template { get; set; }

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

  /// <summary>
  /// Set to false to stop the user editing this column. Defaults to true, but a property with no public setter can't be
  /// edited anyway
  /// </summary>
  [Parameter]
  public bool Editable { get; set; } = true;

  /// <summary>
  /// The editor to use, if not the default for the field's type
  /// </summary>
  [Parameter]
  public PixataGridEditorType? EditorType { get; set; }

  /// <summary>
  /// Your own editor. The context is the copy of the item being edited, so bind your editor to its property
  /// </summary>
  [Parameter]
  public RenderFragment<TItem>? EditorTemplate { get; set; }

  /// <summary>
  /// Whether the user can group by this column, when the grid is <c>Groupable</c>. Defaults to true
  /// </summary>
  [Parameter]
  public bool Groupable { get; set; } = true;

  /// <summary>
  /// The content of the header row of each group, when the data is grouped by this column, in place of "Title: value"
  /// </summary>
  [Parameter]
  public RenderFragment<PixataGridGroupTemplateContext>? GroupHeaderTemplate { get; set; }

  /// <summary>
  /// The content of this column's cell in the footer row of each group, usually showing aggregates. The aggregates must be
  /// asked for in the grid's <c>GridAggregates</c>
  /// </summary>
  [Parameter]
  public RenderFragment<PixataGridGroupTemplateContext>? GroupFooterTemplate { get; set; }

  /// <summary>
  /// The content of this column's cell in the grid's footer, usually showing aggregates over all the data. The aggregates
  /// must be asked for in the grid's <c>GridAggregates</c>
  /// </summary>
  [Parameter]
  public RenderFragment<PixataGridAggregateTemplateContext>? FooterTemplate { get; set; }

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
    RuntimeVisible ?? Visible ?? true;

  /// <summary>
  /// The title shown in the column's header
  /// </summary>
  public string HeaderText {
    get {
      Resolve();
      return Title ?? _attributeTitle ?? Field?.Split('.')[^1].SplitCamelCase() ?? "";
    }
  }

  internal string? WidthStyle =>
    string.IsNullOrWhiteSpace(CurrentWidth) ? null : $"width: {CurrentWidth}";

  internal string CellClass =>
    Css.Join("pixata-blazor-grid-td", TextAlign switch {
      PixataGridTextAlign.Left => "pixata-blazor-grid-text-left",
      PixataGridTextAlign.Center => "pixata-blazor-grid-text-center",
      PixataGridTextAlign.Right => "pixata-blazor-grid-text-right",
      _ => null
    }, LockedClass, Class);

  internal string HeaderCellClass =>
    Css.Join("pixata-blazor-grid-th", LockedClass, HeaderClass);

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

  // The format for the values: DisplayFormat, or the [DisplayFormat] attribute
  internal string? Format {
    get {
      Resolve();
      return DisplayFormat ?? _attributeFormat;
    }
  }

  internal string CellText(TItem item) {
    Resolve();
    return FormatValue(_getter?.Invoke(item));
  }

  // A value of this column's field, formatted as the cells are
  internal string FormatValue(object? value) {
    Resolve();
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

  // A column with Columns renders them, so they can register with it. Other columns render nothing
  protected override void BuildRenderTree(RenderTreeBuilder builder) {
    if (Columns is null) {
      return;
    }
    builder.OpenComponent<CascadingValue<PixataGridColumn<TItem>>>(0);
    builder.AddComponentParameter(1, "Name", ParentCascadeName);
    builder.AddComponentParameter(2, "Value", this);
    builder.AddComponentParameter(3, "IsFixed", true);
    builder.AddComponentParameter(4, "ChildContent", Columns);
    builder.CloseComponent();
  }

  protected override bool ShouldRender() =>
    Columns is not null;
}
