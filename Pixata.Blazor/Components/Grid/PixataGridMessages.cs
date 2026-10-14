using System.Globalization;
using Pixata.Extensions.Grid;

namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// Every piece of text the grid shows itself, in English by default. To translate them, create one with the text you want,
/// such as <c>new PixataGridMessages { NoRecords = "אין נתונים" }</c>, and either register it as a singleton
/// (<c>services.AddSingleton(messages)</c>), so every grid uses it, or pass it to a grid's <c>Messages</c> parameter. The
/// properties ending in "Format" are format strings, with {0} and so on for the values put into them
/// </summary>
public class PixataGridMessages {
  /// <summary>
  /// The English text, used when nothing else has been given
  /// </summary>
  public static PixataGridMessages Default { get; } = new();

  // ---------- Rows ----------

  public string NoRecords { get; init; } = "No records available.";
  public string ReadFailed { get; init; } = "The data couldn't be loaded";
  public string Loading { get; init; } = "Loading";
  public string Toolbar { get; init; } = "Grid toolbar";
  public string SelectAllRows { get; init; } = "Select all rows";
  public string SelectRow { get; init; } = "Select a row";
  public string Details { get; init; } = "Details";
  public string DragRow { get; init; } = "Drag row";
  public string Expand { get; init; } = "Expand";
  public string Collapse { get; init; } = "Collapse";

  // ---------- Sorting ----------

  public string SortedAscending { get; init; } = "Sorted in ascending order";
  public string SortedDescending { get; init; } = "Sorted in descending order";

  // ---------- Editing ----------

  public string Edit { get; init; } = "Edit";
  public string AddNewRecord { get; init; } = "Add new record";
  public string Update { get; init; } = "Update";
  public string Cancel { get; init; } = "Cancel";
  public string Close { get; init; } = "Close";
  public string Delete { get; init; } = "Delete";
  public string ConfirmDelete { get; init; } = "Are you sure you want to delete this record?";
  public string Ok { get; init; } = "OK";
  public string IncreaseValue { get; init; } = "Increase value";
  public string DecreaseValue { get; init; } = "Decrease value";

  // ---------- Paging ----------

  public string PagerFirstPage { get; init; } = "Go to the first page";
  public string PagerPreviousPage { get; init; } = "Go to the previous page";
  public string PagerNextPage { get; init; } = "Go to the next page";
  public string PagerLastPage { get; init; } = "Go to the last page";
  public string PagerPage { get; init; } = "Page";
  public string PagerOf { get; init; } = "of";
  public string PagerMorePages { get; init; } = "More pages";
  public string PagerItemsPerPage { get; init; } = "items per page";
  public string PagerAll { get; init; } = "All";
  public string PagerSelectPage { get; init; } = "Select a page";
  public string PagerSelectPageSize { get; init; } = "Select a page size";

  /// <summary>
  /// "1 - 10 of 91 items": the first and last items on the page, and the total
  /// </summary>
  public string PagerInfoFormat { get; init; } = "{0} - {1} of {2} items";

  /// <summary>
  /// The pager's label: the page and the number of pages
  /// </summary>
  public string PagerNavigationFormat { get; init; } = "Page navigation, page {0} of {1}";

  // ---------- Filtering ----------

  public string Filter { get; init; } = "Filter";
  public string Clear { get; init; } = "Clear";
  public string And { get; init; } = "And";
  public string Or { get; init; } = "Or";
  public string Search { get; init; } = "Search";
  public string SearchPlaceholder { get; init; } = "Search...";
  public string SelectAll { get; init; } = "Select All";
  public string FilterAll { get; init; } = "(All)";
  public string IsTrue { get; init; } = "is true";
  public string IsFalse { get; init; } = "is false";
  public string SelectValue { get; init; } = "-Select value-";
  public string FilterOperators { get; init; } = "Filter Operators list";

  /// <summary>
  /// The label of a column's filter menu button: the column's title
  /// </summary>
  public string FilterMenuButtonFormat { get; init; } = "{0} column filter menu settings";

  /// <summary>
  /// The label of a column's filter editor: the column's title
  /// </summary>
  public string FilterEditorFormat { get; init; } = "{0} Filter";

  /// <summary>
  /// The label of a column's filter menu: the column's title
  /// </summary>
  public string FilterMenuFormat { get; init; } = "{0} Filter Menu";

  public string IsEqualTo { get; init; } = "Is equal to";
  public string IsNotEqualTo { get; init; } = "Is not equal to";
  public string StartsWith { get; init; } = "Starts with";
  public string Contains { get; init; } = "Contains";
  public string DoesNotContain { get; init; } = "Does not contain";
  public string EndsWith { get; init; } = "Ends with";
  public string IsNull { get; init; } = "Is null";
  public string IsNotNull { get; init; } = "Is not null";
  public string IsEmpty { get; init; } = "Is empty";
  public string IsNotEmpty { get; init; } = "Is not empty";
  public string HasNoValue { get; init; } = "Has no value";
  public string HasValue { get; init; } = "Has value";
  public string IsLessThan { get; init; } = "Is less than";
  public string IsLessThanOrEqualTo { get; init; } = "Is less than or equal to";
  public string IsGreaterThan { get; init; } = "Is greater than";
  public string IsGreaterThanOrEqualTo { get; init; } = "Is greater than or equal to";
  public string IsBefore { get; init; } = "Is before";
  public string IsBeforeOrEqualTo { get; init; } = "Is before or equal to";
  public string IsAfter { get; init; } = "Is after";
  public string IsAfterOrEqualTo { get; init; } = "Is after or equal to";

  // ---------- Grouping ----------

  public string GroupPanelEmpty { get; init; } = "Drag a column header and drop it here to group by that column";
  public string GroupPanelLabel { get; init; } = "Grouped by";
  public string Remove { get; init; } = "Remove";

  // ---------- The column menu ----------

  public string ColumnMenu { get; init; } = "Column menu";
  public string SortAscending { get; init; } = "Sort Ascending";
  public string SortDescending { get; init; } = "Sort Descending";
  public string Columns { get; init; } = "Columns";
  public string GroupColumn { get; init; } = "Group column";
  public string UngroupColumn { get; init; } = "Ungroup column";
  public string SetColumnPosition { get; init; } = "Set column position";
  public string Lock { get; init; } = "Lock";
  public string Unlock { get; init; } = "Unlock";
  public string MovePrevious { get; init; } = "Move Previous";
  public string MoveNext { get; init; } = "Move Next";
  public string Apply { get; init; } = "Apply";
  public string Reset { get; init; } = "Reset";

  public string PagerInfo(int first, int last, int total) =>
    string.Format(CultureInfo.CurrentCulture, PagerInfoFormat, first, last, total);

  public string PagerNavigation(int page, int pageCount) =>
    string.Format(CultureInfo.CurrentCulture, PagerNavigationFormat, page, pageCount);

  public string FilterMenuButton(string title) =>
    string.Format(CultureInfo.CurrentCulture, FilterMenuButtonFormat, title);

  public string FilterEditor(string title) =>
    string.Format(CultureInfo.CurrentCulture, FilterEditorFormat, title);

  public string FilterMenu(string title) =>
    string.Format(CultureInfo.CurrentCulture, FilterMenuFormat, title);

  // A filter operator's name. Dates have before and after rather than less than and greater than
  internal string Operator(GridFilterOperator op, bool date) =>
    op switch {
      GridFilterOperator.IsEqualTo => IsEqualTo,
      GridFilterOperator.IsNotEqualTo => IsNotEqualTo,
      GridFilterOperator.StartsWith => StartsWith,
      GridFilterOperator.Contains => Contains,
      GridFilterOperator.DoesNotContain => DoesNotContain,
      GridFilterOperator.EndsWith => EndsWith,
      GridFilterOperator.IsNull => IsNull,
      GridFilterOperator.IsNotNull => IsNotNull,
      GridFilterOperator.IsEmpty => IsEmpty,
      GridFilterOperator.IsNotEmpty => IsNotEmpty,
      GridFilterOperator.IsNullOrEmpty => HasNoValue,
      GridFilterOperator.IsNotNullOrEmpty => HasValue,
      GridFilterOperator.IsLessThan => date ? IsBefore : IsLessThan,
      GridFilterOperator.IsLessThanOrEqualTo => date ? IsBeforeOrEqualTo : IsLessThanOrEqualTo,
      GridFilterOperator.IsGreaterThan => date ? IsAfter : IsGreaterThan,
      GridFilterOperator.IsGreaterThanOrEqualTo => date ? IsAfterOrEqualTo : IsGreaterThanOrEqualTo,
      _ => op.ToString()
    };
}
