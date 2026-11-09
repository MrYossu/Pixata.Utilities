# Serving data to PixataGrid

>Part of [Pixata.AspNetCore](Readme.md)

[PixataGrid](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md) (in Pixata.Blazor) can get its data through its `OnRead` event, which hands you a `GridRequest` describing the page, sorting, filtering, grouping and aggregates it wants. The request types, and a synchronous `ToGridResult()` that applies one to a query, are in [Pixata.Extensions](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.SharedModels.md#grid-requests).

This package adds `ToGridResultAsync()`, namespace `Pixata.AspNetCore.Grid`, which does the same for an EF Core query using EF's async methods, so the count and the page are fetched without blocking a thread...

```csharp
GridResult<Person> result = await db.People.OrderBy(p => p.Id).ToGridResultAsync(request, cancellationToken);
```

The filtering and sorting are expression trees, so EF Core translates them to SQL, and only the requested page comes back from the database. On a query that isn't from EF Core, it falls back to the synchronous version. If the request asks for groups or aggregates, they take a few more queries (one for each group's count and aggregates, and one for each aggregate over all the data). These are run synchronously, as they are small.

>Order the query by something (such as the key) before calling it. Otherwise, when the user hasn't sorted the grid, the database is free to return the rows in any order, and EF Core will warn about using `Skip` and `Take` without an `OrderBy`. A sort the user chooses in the grid replaces this order.

In a Blazor Server app, you can call this in the grid's `OnRead` directly. In a WASM app, POST the `GridRequest` to a minimal API endpoint that calls it and returns the `GridResult<T>`. Both types serialise with `System.Text.Json` as they are.

## Dates, time zones and PostgreSQL

The dates the user picks in the grid's filters have a `Kind` of `Unspecified`. SQL Server compares them with a `datetime2` column as they are, but Npgsql (PostgreSQL) refuses to compare them with a `timestamp with time zone` column, and the query throws. Pass a `GridQueryOptions` (in Pixata.Extensions) to say what the dates are...

```csharp
// The dates are already UTC, so they are just marked as such
GridResult<Order> result = await db.Orders.OrderBy(o => o.Id).ToGridResultAsync(request, GridQueryOptions.Utc, cancellationToken);

// The dates are in the user's time zone, so a filter for "on 21/10/2026" in London starts at 23:00 UTC on the 20th
TimeZoneInfo london = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
GridResult<Order> result = await db.Orders.OrderBy(o => o.Id).ToGridResultAsync(request, GridQueryOptions.FromTimeZone(london), cancellationToken);
```

The second is also worth doing on SQL Server if you store times in UTC but your users filter by their local dates. For a filter that ignores the time (see `IgnoreTime`), the start of the day and of the next day are converted, so the filter covers the user's whole day. For anything else, set `ConvertDateTime` to your own `Func<DateTime, DateTime>`.
