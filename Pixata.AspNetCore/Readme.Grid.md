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
