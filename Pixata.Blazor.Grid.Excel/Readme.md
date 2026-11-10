# Pixata.Blazor.Grid.Excel

Excel export for `PixataGrid`, the data grid in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Grid.md). It's a separate package because it uses [ClosedXML](https://github.com/ClosedXML/ClosedXML), which would otherwise add weight to every app that uses Pixata.Blazor, including WASM apps that don't need it.

>**Version 1.1.0** writes the grid's aggregates as a row of totals under the data, as Excel formulas, and, with the new `GroupRows` parameter, writes a grouped grid with a heading and totals for each group, in an outline that can be collapsed. Set `Totals="false"` for the sheet as it was. See [totals](#totals) and [groups](#groups). It needs Pixata.Blazor 5.27.0 (#150).

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
| `Totals` | `bool` | Whether to write the grid's aggregates as totals under the data. See [totals](#totals). Defaults to true |
| `GroupRows` | `bool` | Whether, when the grid is grouped, to write a heading and totals for each group. See [groups](#groups). Defaults to false |
| `AggregateLabel` | `Func<GridAggregateType, string>` | The text shown before a total, such as "Average: ". Defaults to "Count: ", "Average: ", "Min: " and "Max: ", with nothing before a sum. Return "" for no text |
| `AllPages` | `bool` | Exports all the rows (filtered and sorted as in the grid), rather than just the page shown. With `OnRead`, the handler is called with a request for all the rows |
| `OnBeforeExport` | `EventCallback<PixataGridBeforeExportEventArgs<TItem>>` | Fires before the export, with the `Columns` and `Data`, which you can change. Set `IsCancelled` to stop it |
| `OnAfterExport` | `EventCallback<PixataGridAfterExportEventArgs>` | Fires after the export, with the file as a `Stream` |

## What's exported

The columns shown in the grid that have a `Field`, in the order shown, with their titles as the first row. Command, checkbox and drag columns aren't exported.

- Numbers, dates and true/false values are stored as such, so they can be worked with in Excel. The column's `DisplayFormat` (or `[DisplayFormat]` attribute) is turned into an Excel format where it can be: `C`, `N`, `F`, `P` and `D` (with or without a number of decimal places), the standard date formats, custom date formats such as "dd MMM yyyy", and custom number formats such as "#,##0.00". Dates without a format use the current culture's short date format
- Everything else is stored as text, formatted with the column's `DisplayFormat` if it has one
- A column with a `Width` in pixels is that wide in Excel. Other columns are sized to fit their content

`ExcelFormats.For(displayFormat, culture)` does the format conversion, if you need it yourself.

## Totals

If the grid has aggregates (in its `GridAggregates`, for its footers), they're written under the data, in the columns they're for, as Excel formulas, such as `=SUM(C2:C40)`. That means they stay right if rows are changed or deleted in Excel. Sum, average, min and max use `SUM`, `AVERAGE`, `MIN` and `MAX`. Count uses `COUNTA`, which counts the cells that aren't empty, so a row whose value is empty isn't counted, where the grid would count it.

```razor
<PixataGrid Data="Invoices">
  <GridAggregates>
    <PixataGridAggregate Field="@nameof(Invoice.Amount)" Aggregate="GridAggregateType.Sum" />
    <PixataGridAggregate Field="@nameof(Invoice.Amount)" Aggregate="GridAggregateType.Average" />
    <PixataGridAggregate Field="@nameof(Invoice.Customer)" Aggregate="GridAggregateType.Count" />
  </GridAggregates>
  <GridExport>
    <PixataGridExcelExport FileName="invoices" AllPages="true" />
  </GridExport>
  ...
</PixataGrid>
```

- The totals are bold, under a line, and formatted like the column, so a sum of amounts shown as "{0:C2}" is shown as currency.
- Each total except a sum has a label before it, such as "Count: 12". The label is part of the cell's number format, so the cell is still a number that other formulas can use. Change the labels (to translate them, say) with `AggregateLabel`.
- A column with more than one aggregate, such as a sum and an average, gets a row for each.
- They're the totals of the rows exported. Without `AllPages="true"`, that's just the page shown, whereas the grid's footer shows the totals of all the rows.
- To change which totals are written, change the columns' `Aggregates` in `OnBeforeExport`. To leave them out, set `Totals="false"`.

## Groups

When the grid is grouped, its rows are exported in group order. By default they're written as a plain table, which can be sorted and filtered in Excel. Set `GroupRows="true"` to write them as the grid shows them...

- Each group starts with a bold heading row, such as "Department: Sales", indented for each level of grouping.
- Each group ends with its totals, if the grid has aggregates (and `Totals` isn't false). These use Excel's `SUBTOTAL`, so the totals of a group, and the totals under all the rows, leave out the subtotals within them, rather than counting them twice. A count in the first column also leaves out the headings within it, which are in that column.
- Each group's rows are grouped in Excel's outline, so they can be collapsed, leaving the heading and totals, with the buttons at the side of the sheet.

`OnBeforeExport` is given the `Groups`, outermost first. Clear them to export a grouped grid as a plain table, whatever `GroupRows` says.
