# Pixata.Blazor.Grid.Excel

Excel export for `PixataGrid`, the data grid in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md). It's a separate package because it uses [ClosedXML](https://github.com/ClosedXML/ClosedXML), which would otherwise add weight to every app that uses Pixata.Blazor, including WASM apps that don't need it.

>**Version 1.0.1** has no changes of its own. It depends on Pixata.Blazor 5.5.0, so adding this package on its own brings in a Pixata.Blazor (and Pixata.Extensions) whose grid works. Version 1.0.0 depended on Pixata.Blazor 5.0.0, which depended on a Pixata.Extensions without the grid types, so the grid threw a `TypeLoadException`.

## Using it

Add the package, add `@using Pixata.Blazor.Grid.Excel` (in the page or `_Imports.razor`), and put a `PixataGridExcelExport` in the grid's `GridExport`. An `ExcelExport` command button then saves the grid as an Excel file...

```razor
<PixataGrid Data="People" Pageable="true">
  <GridToolBarTemplate>
    <PixataGridCommandButton Command="ExcelExport" Icon="PixataGridIcon.FileExcel">Export to Excel</PixataGridCommandButton>
  </GridToolBarTemplate>
  <GridExport>
    <PixataGridExcelExport FileName="people" AllPages="true" />
  </GridExport>
  <GridColumns>...</GridColumns>
</PixataGrid>
```

You can also export from code, with these extension methods on the grid...

| Method | What it does |
| --- | --- |
| `SaveAsExcelFile()` | Exports the grid and has the browser save the file |
| `ExportToExcel()` | Exports the grid and returns the file as a `MemoryStream`, or null if `OnBeforeExport` cancelled it |

## Settings

| Parameter | Type | What it does |
| --- | --- | --- |
| `FileName` | `string` | The file name, without ".xlsx". Defaults to "Grid" |
| `SheetName` | `string` | The name of the worksheet. Defaults to "Sheet1" |
| `AllPages` | `bool` | Exports all the rows (filtered and sorted as in the grid), rather than just the page shown. With `OnRead`, the handler is called with a request for all the rows |
| `OnBeforeExport` | `EventCallback<PixataGridBeforeExportEventArgs<TItem>>` | Fires before the export, with the `Columns` and `Data`, which you can change. Set `IsCancelled` to stop it |
| `OnAfterExport` | `EventCallback<PixataGridAfterExportEventArgs>` | Fires after the export, with the file as a `Stream` |

## What's exported

The columns shown in the grid that have a `Field`, in the order shown, with their titles as the first row. Command, checkbox and drag columns aren't exported.

- Numbers, dates and true/false values are stored as such, so they can be worked with in Excel. The column's `DisplayFormat` (or `[DisplayFormat]` attribute) is turned into an Excel format where it can be: `C`, `N`, `F`, `P` and `D` (with or without a number of decimal places), the standard date formats, custom date formats such as "dd MMM yyyy", and custom number formats such as "#,##0.00". Dates without a format use the current culture's short date format
- Everything else is stored as text, formatted with the column's `DisplayFormat` if it has one
- A column with a `Width` in pixels is that wide in Excel. Other columns are sized to fit their content

`ExcelFormats.For(displayFormat, culture)` does the format conversion, if you need it yourself.
