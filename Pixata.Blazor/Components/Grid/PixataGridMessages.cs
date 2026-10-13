namespace Pixata.Blazor.Components.Grid;

/// <summary>
/// The text the grid shows itself, matching Telerik's English messages. Kept in one place so they can be made replaceable for
/// localisation
/// </summary>
internal static class PixataGridMessages {
  public const string NoRecords = "No records available.";
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

  public static string PagerInfo(int first, int last, int total) =>
    $"{first} - {last} of {total} items";

  public static string PagerNavigation(int page, int pageCount) =>
    $"Page navigation, page {page} of {pageCount}";
}
