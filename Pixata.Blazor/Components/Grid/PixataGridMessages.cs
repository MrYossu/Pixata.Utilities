namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// The text the grid shows itself, matching Telerik's English messages. Kept in one place so they can be made replaceable for
/// localisation
/// </summary>
internal static class PixataGridMessages {
  public const string NoRecords = "No records available.";
  public const string ReadFailed = "The data couldn't be loaded";
  public const string Edit = "Edit";
  public const string AddNewRecord = "Add new record";
  public const string Update = "Update";
  public const string Cancel = "Cancel";
  public const string Close = "Close";
  public const string Delete = "Delete";
  public const string ConfirmDelete = "Are you sure you want to delete this record?";
  public const string Ok = "OK";
  public const string SortedAscending = "Sorted in ascending order";
  public const string SortedDescending = "Sorted in descending order";
  public const string PagerFirstPage = "Go to the first page";
  public const string PagerPreviousPage = "Go to the previous page";
  public const string PagerNextPage = "Go to the next page";
  public const string PagerLastPage = "Go to the last page";
  public const string PagerPage = "Page";
  public const string PagerOf = "of";
  public const string PagerMorePages = "More pages";
  public const string PagerItemsPerPage = "items per page";
  public const string PagerAll = "All";
  public const string PagerSelectPage = "Select a page";
  public const string PagerSelectPageSize = "Select a page size";
  public const string GroupPanelEmpty = "Drag a column header and drop it here to group by that column";
  public const string GroupPanelLabel = "Grouped by";
  public const string Remove = "Remove";
  public const string Expand = "Expand";
  public const string Collapse = "Collapse";
  public const string ColumnMenu = "Column menu";
  public const string SortAscending = "Sort Ascending";
  public const string SortDescending = "Sort Descending";
  public const string Columns = "Columns";
  public const string Filter = "Filter";
  public const string GroupColumn = "Group column";
  public const string UngroupColumn = "Ungroup column";
  public const string SetColumnPosition = "Set column position";
  public const string Lock = "Lock";
  public const string Unlock = "Unlock";
  public const string MovePrevious = "Move Previous";
  public const string MoveNext = "Move Next";
  public const string Apply = "Apply";
  public const string Reset = "Reset";

  public static string PagerInfo(int first, int last, int total) =>
    $"{first} - {last} of {total} items";

  public static string PagerNavigation(int page, int pageCount) =>
    $"Page navigation, page {page} of {pageCount}";
}
