# Pixata.AspNetCore.Telerik [![Pixata.AspNetCore.Telerik Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Telerik)](https://www.nuget.org/packages/Pixata.AspNetCore.Telerik/)

The server-side companion to the [Pixata.Blazor.TelerikComponents package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents).

This package exists so that `Pixata.Blazor.TelerikComponents` doesn't have to. The grid data helper below talks to a database, so it needs EF Core, `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.Data.SqlClient`. Those used to sit in the component package, which meant every client-side app that wanted a Telerik date picker also downloaded a SQL Server driver it could never use. The helper now lives here, and the component package is free of them.

Install this one in your **server** project, and `Pixata.Blazor.TelerikComponents` wherever your components live.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore.Telerik/) is available for this project.

## Extension method to improve the performance of the Telerik Blazor grid
Whilst the Telerik Blazor grid does an amazing job, it has its limitations. One of these is the way it computes aggregates. For large tables, this can be slow.

I had a play with Dapper, which improved matters significantly, and then found out that I could do the same with pure EF Core, without any extra packages. I wrapped up the code into an extension method for `GridReadEventArgs`, which you can call from the `OnRead` event of the grid. In the project that motivated this code, I managed to reduce the time taken for the grid to load from over 10 seconds to around 400 milliseconds 😎.

There is a [sample repo](https://github.com/MrYossu/TelerikGridWithFromSql) that shows the method in usage, and [a blog post that explain the usage](https://www.pixata.co.uk/2024/10/09/using-the-ef-core-fromsqlraw-method-in-a-telerik-blazor-grid/) (with far too many anecdotes and rambling). If you are bored, that post links to two previous posts that detail my journey to this extension method.

Note that as from version 2.0.0 of the `Pixata.Blazor.TelerikComponents` package, the method allows you to query a table-valued function as well as a table or view.

### Filtering DateTime values by date only
When using row filtering, the Telerik grid will, by default, filter `DateTime` values by their full value, including the time. This is often not what you want, as users will typically want to filter by date only. To make this easier, the extension method allows you to specify that a particular `DateTime` column should be filtered by date only. For example...

```csharp
private Task LoadData(GridReadEventArgs args) {
  TelerikFilterHelper.RemoveTime(args, nameof(MyEntity.DateCreated), nameof(MyEntity.DateDeleted));
  // Call the extension method as before
}
```

Note that `TelerikFilterHelper` stays in the `Pixata.Blazor.TelerikComponents` package, as it doesn't touch the database.

## Breaking changes
### Moved from Pixata.Blazor.TelerikComponents
`TelerikGridHelper` (and with it `TelerikGridFilterResults` and `TelerikGridFilterOptions`) moved here from `Pixata.Blazor.TelerikComponents` v12.3.17. If you use `args.GetData<T>()`, add a reference to this package in your server project and change the using from...

```csharp
using Pixata.Blazor.TelerikComponents.Helpers;
```

...to...

```csharp
using Pixata.AspNetCore.Telerik.Helpers;
```

The method signatures and behaviour are unchanged.

### Removal of the generic parameter from TelerikGridFilterResults
As of version 2.0.0 of `Pixata.Blazor.TelerikComponents`, the generic parameter was removed from `TelerikGridFilterResults` (as it wasn't needed), so you will need to update your code if you capture this as an explicit type. For example, change this:

```csharp
TelerikGridFilterResults<MyType> data = await args.GetData<MyType>(/* args go here */)
```

...to this...

```csharp
TelerikGridFilterResults data = await args.GetData<MyType>(/* args go here */)
```

Not a major issue, but worth noting.

## Warning
As with the rest of these packages, use at your own risk!
