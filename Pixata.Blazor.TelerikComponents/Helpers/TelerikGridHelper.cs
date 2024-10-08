using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class TelerikGridHelper {
  public static async Task<TelerikGridFilterResults<T>> GetData<T>(this GridReadEventArgs args, DbContext context, string tableName, string defaultColumnForSort, ListSortDirection defaultSort = ListSortDirection.Ascending) where T : class =>
    await GetData<T>(args, context, tableName, defaultColumnForSort, new List<CompositeFilterDescriptor>().AsReadOnly(), defaultSort);

  public static async Task<TelerikGridFilterResults<T>> GetData<T>(this GridReadEventArgs args, DbContext context, string tableName, string defaultColumnForSort, IEnumerable<CompositeFilterDescriptor> extraFilters, ListSortDirection defaultSort = ListSortDirection.Ascending) where T : class {
    List<SqlParameter> values = new();

    // Set up SQL for filtering
    string sqlFilters = "";
    string sqlFilterConjunction = "";
    int n = 0;

    // First handle the grid's built-in filters
    foreach (CompositeFilterDescriptor cfd in args.Request.Filters.Cast<CompositeFilterDescriptor>().Union(extraFilters)) {
      if (cfd.LogicalOperator == FilterCompositionLogicalOperator.And) {
        foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
          AddValue(values, fd.Member, fd.Operator, fd.Value, n);
          sqlFilters += AddSql(fd.Member, fd.Operator, n, sqlFilterConjunction);
          sqlFilterConjunction = " and";
          n++;
        }
      } else {
        sqlFilters += $"{sqlFilterConjunction} (";
        string thisLogicalOperator = "";
        foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
          AddValue(values, fd.Member, fd.Operator, fd.Value, n);
          sqlFilters += AddSql(fd.Member, fd.Operator, n, thisLogicalOperator);
          thisLogicalOperator = " or";
          sqlFilterConjunction = " and";
          n++;
        }
        sqlFilters += ") ";
      }
    }

    if (!string.IsNullOrWhiteSpace(sqlFilters)) {
      sqlFilters = $" where {sqlFilters}";
    }

    // Paging
    values.Add(new("@Skip", args.Request.Skip));
    values.Add(new("@PageSize", args.Request.PageSize));

    // SQL for sorting
    string sqlSort = $" order by {defaultColumnForSort} {(defaultSort == ListSortDirection.Ascending ? "asc" : "desc")}";
    if (args.Request.Sorts.Any()) {
      SortDescriptor sortDescriptor = (args.Request.Sorts.First())!;
      sqlSort = $" order by {sortDescriptor.Member} " + (sortDescriptor.SortDirection == ListSortDirection.Ascending ? "asc" : "desc");
    }

    // Assemble the final SQL
    string sql = $"select * from {tableName}{sqlFilters} {sqlSort} offset (@Skip) rows fetch next (@PageSize) rows only";

    // Dump the SQL and values
    //Console.WriteLine("\n");
    //Console.WriteLine("Parameters");
    //foreach (SqlParameter pair in values) {
    //  Console.WriteLine($"  {pair.ParameterName}: {pair.Value}");
    //}
    //Console.WriteLine(sql);

    // Get the data and the total number of rows that match the filters
    args.Data = await context.Set<T>().FromSqlRaw(sql, values.ToArray()).ToListAsync();
    values.Remove(values.Single(v => v.ParameterName == "@Skip"));
    values.Remove(values.Single(v => v.ParameterName == "@PageSize"));
    int matchingRows = await context.Database.SqlQueryRaw<int>($"select count(*) as Value from {tableName}{sqlFilters}", values.ToArray()).SingleAsync();
    args.Total = matchingRows;

    // Return the filter SQL in case the calling code wants to use it (eg to show some totals)
    return new(matchingRows, sqlFilters, values.ToArray());
  }

  private static void AddValue(List<SqlParameter> parameters, string member, FilterOperator op, object value, int n) =>
    parameters.Add(new($"@{member}{n}", op switch {
      FilterOperator.Contains => $"%{value}%",
      FilterOperator.StartsWith => $"{value}%",
      FilterOperator.EndsWith => $"%{value}",
      _ => value
    }));

  private static string AddSql(string member, FilterOperator op, int n, string sqlFilterConjunction) =>
    $"{sqlFilterConjunction} {member}" + op switch {
      FilterOperator.IsEqualTo => $"=@{member}{n}",
      FilterOperator.IsNotEqualTo => $"<>@{member}{n}",
      FilterOperator.Contains => $" like @{member}{n}",
      FilterOperator.StartsWith => $" like @{member}{n}",
      FilterOperator.EndsWith => $" like @{member}{n}",
      FilterOperator.IsGreaterThan => $">@{member}{n}",
      FilterOperator.IsGreaterThanOrEqualTo => $">=@{member}{n}",
      FilterOperator.IsLessThan => $"<@{member}{n}",
      FilterOperator.IsLessThanOrEqualTo => $"<=@{member}{n}",
      _ => throw new Exception($"Unknown operator: {op}")
    };
}

public record TelerikGridFilterOptions(string Member, object Value, FilterOperator Operator);

public record TelerikGridFilterResults<T>(int MatchingRows, string SqlFilters, SqlParameter[] Parameters);