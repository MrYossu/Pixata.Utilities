using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class TelerikGridHelper {
  public static Task<TelerikGridFilterResults> GetData<T>(this GridReadEventArgs args, DbContext context, string tableName, string defaultColumnForSort, ListSortDirection defaultSort = ListSortDirection.Ascending, SqlParameter[]? functionParameters = null) where T : class =>
    GetData<T>(args, context, tableName, defaultColumnForSort, [], defaultSort, functionParameters);

  public static async Task<TelerikGridFilterResults> GetData<T>(this GridReadEventArgs args, DbContext context, string tableName, string defaultColumnForSort, IEnumerable<CompositeFilterDescriptor> extraFilters, ListSortDirection defaultSort = ListSortDirection.Ascending, SqlParameter[]? functionParameters = null) where T : class {
    // Validate identifiers that will be embedded directly into SQL text
    string sanitisedTableNameOrFunction = functionParameters is null
      ? ValidateSqlIdentifier(tableName, nameof(tableName))
      : ValidateSqlFunctionIdentifier(tableName, nameof(tableName));
    string sanitisedDefaultColumnForSort = ValidateSqlIdentifier(defaultColumnForSort, nameof(defaultColumnForSort));

    List<SqlParameter> values = [];

    // If calling a TVF, normalise function parameters into internally-named parameters to avoid collisions
    string sourceFragment;
    if (functionParameters is not null) {
      List<string> internalNames = [];
      for (int i = 0; i < functionParameters.Length; i++) {
        SqlParameter orig = functionParameters[i];
        string internalName = $"@__fn{i}";
        internalNames.Add(internalName);
        SqlParameter p = new(internalName, orig.Value ?? DBNull.Value) {
          Direction = orig.Direction,
          Size = orig.Size
        };
        // Copy common type info if available
        try {
          p.SqlDbType = orig.SqlDbType;
        } catch {
          // No, we don't normally swallow exceptions, but SqlDbType is not always set
        }
        try {
          p.DbType = orig.DbType;
        } catch {
          // See previous comment
        }
        values.Add(p);
      }
      sourceFragment = sanitisedTableNameOrFunction + "(" + string.Join(", ", internalNames) + ")";
    } else {
      sourceFragment = sanitisedTableNameOrFunction;
    }

    // Set up SQL for filtering
    string sqlFilters = "";
    string sqlFilterConjunction = "";
    int n = 0;

    // First handle the grid's built-in filters
    foreach (CompositeFilterDescriptor cfd in (args.Request.Filters ?? []).Cast<CompositeFilterDescriptor>().Union(extraFilters)) {
      if (cfd.LogicalOperator == FilterCompositionLogicalOperator.And) {
        foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
          ValidateSqlIdentifier(fd.Member, "filter member");
          AddValue(values, fd.Member, fd.Operator, fd.Value, n);
          sqlFilters += AddSql(fd.Member, fd.Operator, n, sqlFilterConjunction);
          sqlFilterConjunction = " and";
          n++;
        }
      } else {
        sqlFilters += $"{sqlFilterConjunction} (";
        string thisLogicalOperator = "";
        foreach (FilterDescriptor fd in cfd.FilterDescriptors.Cast<FilterDescriptor>()) {
          ValidateSqlIdentifier(fd.Member, "filter member");
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

    // Only apply paging if PageSize > 0
    bool usePaging = (args.Request?.PageSize ?? 0) > 0;
    if (usePaging) {
      values.Add(new("@Skip", args.Request!.Skip));
      values.Add(new("@PageSize", args.Request.PageSize));
    }

    // SQL for sorting
    string sqlSort = $" order by {sanitisedDefaultColumnForSort} {(defaultSort == ListSortDirection.Ascending ? "asc" : "desc")}";
    if ((args.Request!.Sorts ?? []).Any()) {
      SortDescriptor sortDescriptor = (args.Request.Sorts ?? []).First()!;
      string firstSortMember = ValidateSqlIdentifier(sortDescriptor.Member, "sort member");
      sqlSort = $" order by {firstSortMember} " + (sortDescriptor.SortDirection == ListSortDirection.Ascending ? "asc" : "desc");
      foreach (SortDescriptor sd in (args.Request.Sorts ?? []).Skip(1)) {
        string m = ValidateSqlIdentifier(sd.Member, "sort member");
        sqlSort += $", {m} {(sd.SortDirection == ListSortDirection.Ascending ? "asc" : "desc")}";
      }
    }

    // Assemble the final SQL
    string paginationClause = usePaging ? " offset (@Skip) rows fetch next (@PageSize) rows only" : "";
    string sql = $"select * from {sourceFragment}{sqlFilters} {sqlSort}{paginationClause}";

    // Get the data and the total number of rows that match the filters
    args.Data = await context.Set<T>().FromSqlRaw(sql, values.Cast<object>().ToArray()).ToListAsync();

    // Prepare for count - remove paging parameters if they were added
    if (usePaging) {
      values.Remove(values.Single(v => v.ParameterName == "@Skip"));
      values.Remove(values.Single(v => v.ParameterName == "@PageSize"));
    }

    int matchingRows;
    // Use a DbCommand to execute a parameterised scalar query safely
    DbConnection conn = context.Database.GetDbConnection();
    try {
      if (conn.State != System.Data.ConnectionState.Open) {
        await conn.OpenAsync();
      }
      await using DbCommand cmd = conn.CreateCommand();
      cmd.CommandText = $"select count(*) as Value from {sourceFragment}{sqlFilters}";
      foreach (SqlParameter p in values) {
        cmd.Parameters.Add(p);
      }
      object result = await cmd.ExecuteScalarAsync() ?? 0;
      matchingRows = Convert.ToInt32(result);
    } finally {
      try {
        await conn.CloseAsync();
      } catch {
        // No, we don't normally swallow exceptions, but if we can't close the connection, there's not much we can do about it
      }
    }

    args.Total = matchingRows;

    // Return the filter SQL in case the calling code wants to use it (say, to show some totals)
    return new(matchingRows, sqlFilters, values.ToArray());
  }

  private static void AddValue(List<SqlParameter> parameters, string member, FilterOperator op, object value, int n) {
    if (op != FilterOperator.IsNull && op != FilterOperator.IsNotNull) {
      parameters.Add(new($"@{member}{n}", op switch {
        FilterOperator.Contains => $"%{value}%",
        FilterOperator.StartsWith => $"{value}%",
        FilterOperator.EndsWith => $"%{value}",
        _ => value
      }));
    }
  }

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
      FilterOperator.IsNull => " is null",
      FilterOperator.IsNotNull => " is not null",
      _ => throw new Exception($"Unknown operator: {op}")
    };

  private static string ValidateSqlIdentifier(string id, string paramName) {
    if (string.IsNullOrWhiteSpace(id)) {
      throw new ArgumentException($"{paramName} must be provided and non-empty.", paramName);
    }
    // Only allow letters, numbers and underscore, and must start with a letter or underscore
    return !Regex.IsMatch(id, "^[A-Za-z_][A-Za-z0-9_]*$")
      ? throw new ArgumentException($"Invalid SQL identifier for {paramName}: {id}", paramName)
      : id;
  }

  private static string ValidateSqlFunctionIdentifier(string id, string paramName) {
    if (string.IsNullOrWhiteSpace(id)) {
      throw new ArgumentException($"{paramName} must be provided and non-empty", paramName);
    }
    // Allow either 'name' or 'schema.name'
    string[] parts = id.Split('.');
    switch (parts.Length) {
      case 1:
        return ValidateSqlIdentifier(parts[0], paramName);
      case 2: {
          string schema = ValidateSqlIdentifier(parts[0], paramName + " (schema)");
          string name = ValidateSqlIdentifier(parts[1], paramName + " (function name)");
          return schema + "." + name;
        }
      default:
        throw new ArgumentException($"Invalid SQL function identifier for {paramName}: {id}", paramName);
    }
  }
}

public record TelerikGridFilterOptions(string Member, object Value, FilterOperator Operator);

public record TelerikGridFilterResults(int MatchingRows, string SqlFilters, SqlParameter[] Parameters);