using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class TelerikGridHelper {
  public static async Task<(int, string, Dictionary<string, object>)> GetData<T>(this GridReadEventArgs args, string connectionString, string tableName, string defaultColumnForSort, ListSortDirection defaultSort = ListSortDirection.Ascending) =>
    await GetData<T>(args, connectionString, tableName, defaultColumnForSort, new List<TelerikGridFilterOptions>(), defaultSort);

  /// <summary>
  /// Extension method to the Telerik GridReadEventArgs object that loads the grid data using Dapper
  /// </summary>
  /// <typeparam name="T">The type of the grid data</typeparam>
  /// <param name="args">The GridReadEventArgs object</param>
  /// <param name="connectionString">Connection string to the SQL Server database</param>
  /// <param name="tableName">The table name to be queried for the data</param>
  /// <param name="defaultColumnForSort">If sorting is not specified in the grid, this column will be used by default</param>
  /// <param name="extraFilter">A collection of extra filter specifications. Useful if you roll your own custom filter</param>
  /// <param name="defaultSort">The default sort direction (if the user does not set one in the grid). Defaults to ascending</param>
  /// <returns>A 3-tuple containing total number of rows matching the current filtering, the SQL for the filtering (useful if you want to do any extra queries using the same filters), and a collection of data values used by the SQL</returns>
  public static async Task<(int, string, Dictionary<string, object>)> GetData<T>(this GridReadEventArgs args, string connectionString, string tableName, string defaultColumnForSort, IEnumerable<TelerikGridFilterOptions> extraFilter, ListSortDirection defaultSort = ListSortDirection.Ascending) {
    // Create a connection to the database
    await using SqlConnection connection = new(connectionString);
    // Create a dictionary that will hold the data for filtering and paging
    Dictionary<string, object> values = new();

    // Set up SQL for filtering
    string sqlFilters = "";
    string sqlFilterConjunction = "";
    int n = 0;

    // First handle the grid's built-in filters
    Console.WriteLine();
    foreach (CompositeFilterDescriptor cfd in args.Request.Filters.Cast<CompositeFilterDescriptor>()) {
      foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
        AddValue(values, fd.Member, fd.Operator, fd.Value, n);
        sqlFilters += AddSql(fd.Member, fd.Operator, n, sqlFilterConjunction);
        sqlFilterConjunction = " and";
        n++;
      }
    }

    // Now add in any extra filters
    foreach (TelerikGridFilterOptions option in extraFilter) {
      AddValue(values, option.Member, option.Operator, option.Value, n);
      sqlFilters += AddSql(option.Member, option.Operator, n, sqlFilterConjunction);
      sqlFilterConjunction = " and";
      n++;
    }

    if (!string.IsNullOrWhiteSpace(sqlFilters)) {
      sqlFilters = $" where {sqlFilters}";
    }

    // Paging
    values.Add("Skip", args.Request.Skip);
    values.Add("PageSize", args.Request.PageSize);

    // SQL for sorting
    string sqlSort = $" order by {defaultColumnForSort} {(defaultSort == ListSortDirection.Ascending ? "asc" : "desc")}";
    if (args.Request.Sorts.Any()) {
      SortDescriptor sortDescriptor = (args.Request.Sorts.First())!;
      sqlSort = $" order by {sortDescriptor.Member} " + (sortDescriptor.SortDirection == ListSortDirection.Ascending ? "asc" : "desc");
    }

    // Assemble the final SQL
    string sql = $"select * from {tableName}{sqlFilters} {sqlSort} offset (@Skip) rows fetch next (@PageSize) rows only";

    // Dump the SQL and values
    //foreach (KeyValuePair<string, object> pair in values) {
    //  Console.WriteLine($"  {pair.Key}: {pair.Value}");
    //}
    //Console.WriteLine(sql);

    // Get the data and the total number of rows that match the filters
    int matchingRows = await connection.ExecuteScalarAsync<int>($"select count(*) from {tableName}{sqlFilters}", values);
    args.Data = await connection.QueryAsync<T>(sql, values);
    args.Total = matchingRows;

    // Return the filter SQL in case the calling code wants to use it (eg to show some totals)
    values.Remove("Skip");
    values.Remove("PageSize");
    return (matchingRows, sqlFilters, values);
  }

  private static void AddValue(Dictionary<string, object> values, string member, FilterOperator op, object value, int n) =>
    values.Add($"@{member}{n}", op == FilterOperator.Contains
      ? $"%{value}%"
      : value);

  private static string AddSql(string member, FilterOperator op, int n, string sqlFilterConjunction) =>
    $"{sqlFilterConjunction} {member}" + op switch {
      FilterOperator.IsEqualTo => $"=@{member}{n}",
      FilterOperator.IsNotEqualTo => $"<>@{member}{n}",
      FilterOperator.Contains => $" like @{member}{n}",
      FilterOperator.IsGreaterThan => $">@{member}{n}",
      FilterOperator.IsGreaterThanOrEqualTo => $">=@{member}{n}",
      FilterOperator.IsLessThan => $"<@{member}{n}",
      FilterOperator.IsLessThanOrEqualTo => $"<=@{member}{n}",
      _ => throw new Exception($"Unknown operator: {op}")
    };

  public static async Task<(string, Dictionary<string, object>)> GetDataOrig<T>(this GridReadEventArgs args, string connectionString, string tableName, string defaultColumnForSort, string defaultSortDirection = "asc") {
    // Create a connection to the database
    await using SqlConnection connection = new(connectionString);
    // Create a dictionary that will hold the data for filtering and paging
    Dictionary<string, object> values = new();

    // Set up SQL for filtering
    string sqlFilters = "";
    string sqlFilterConjunction = "";
    foreach (CompositeFilterDescriptor cfd in args.Request.Filters.Cast<CompositeFilterDescriptor>()) {
      foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
        if (!values.ContainsKey("@{fd.Member}")) {
          values.Add($"@{fd.Member}", fd.Operator == FilterOperator.Contains
            ? $"%{fd.Value}%"
            : fd.Value);
        }
        sqlFilters += $"{sqlFilterConjunction} {fd.Member}" + fd.Operator switch {
          FilterOperator.IsEqualTo => $"=@{fd.Member}",
          FilterOperator.Contains => $" like @{fd.Member}",
          FilterOperator.IsGreaterThan => $">@{fd.Member}",
          FilterOperator.IsGreaterThanOrEqualTo => $">=@{fd.Member}",
          FilterOperator.IsLessThan => $"<@{fd.Member}",
          FilterOperator.IsLessThanOrEqualTo => $"<=@{fd.Member}",
          _ => throw new Exception($"Unknown operator: {fd.Operator}")
        };
        sqlFilterConjunction = " and";
      }
    }
    if (!string.IsNullOrWhiteSpace(sqlFilters)) {
      sqlFilters = $" where {sqlFilters}";
    }

    // Paging
    values.Add("Skip", args.Request.Skip);
    values.Add("PageSize", args.Request.PageSize);

    // SQL for sorting
    string sqlSort = $" order by {defaultColumnForSort} {defaultSortDirection}";
    if (args.Request.Sorts.Any()) {
      SortDescriptor sortDescriptor = (args.Request.Sorts.First())!;
      sqlSort = $" order by {sortDescriptor.Member} " + (sortDescriptor.SortDirection == ListSortDirection.Ascending ? "asc" : "desc");
    }

    // Get the data and the total number of rows that match the filters
    args.Data = await connection.QueryAsync<T>($"select * from {tableName}{sqlFilters} {sqlSort} offset (@Skip) rows fetch next (@PageSize) rows only", values);
    args.Total = await connection.ExecuteScalarAsync<int>($"select count(*) from {tableName}{sqlFilters}", values);

    // Return the filter SQL in case the calling code wants to use it (eg to show some totals)
    values.Remove("Skip");
    values.Remove("PageSize");
    return (sqlFilters, values);
  }
}

public record TelerikGridFilterOptions(string Member, object Value, FilterOperator Operator);