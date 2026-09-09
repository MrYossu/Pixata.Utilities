# Pixata.Blazor.TelerikComponents [![Pixata.Blazor.TelerikComponents Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor.TelerikComponents)](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Blazor.TelerikComponents/MailOldSchool.png "Pixata") 

This package complements the [Pixata.Blazor package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor), and adds components that rely on the Telerik components for Blazor. These were split off into their own package to enable those without a licence for Telerik to use the other components.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/) is available for this project.

>Note that as of version 12.2.0, the major and minor package versions will correspond to the version of the Telerik.Blazor package that is required. The patch version will be used for updates to this package. The build number will indicate my own internal versioning. Thus, version 12.2.0 is based on the Telerik Blazor package version 12.2.x, where x is my own incremental build number.

## The components

### TelerikGridWithState
Saving and restoring state is a very useful feature, as it allows the grid to return to its previous state when the user left the page. Handling this manually is not major, but painful.

I wrote the `TelerikGridWithState` component as almost drop-in replacement for the standard `TelerikGrid`, but preserves the grid state in local storage. If you use virtualisation, then the current skip and take will also be restored.

All you need to do is set a value for the `StorageKey` parameter and you're done. 

In the unlikely event that you need to handle `OnStateInit` or `OnStateChanged` events, you can do so by using the `OnStateInitPre`, `OnStateInitPost`, `OnStateChangedPre`, and `OnStateChangedPost` event handlers. These allow you to run your own code before or after the component's state handling.

#### Repairing restored filters
Before version 12.3.18, restoring a saved state could bring the page down. Telerik serialises `FilterDescriptor.Value` as a bare JSON value, and doesn't serialise `MemberType` at all. On the way back, the value is therefore given the narrowest CLR type that will hold it, rather than the type of the member being filtered. An `enum` or an `int` comes back as a `short`, a `double` comes back as a `decimal`, and a `Guid` or a `TimeSpan` comes back as a `string`.

As the Telerik filter editors cast the value to the member's own type, rendering the filter cell for a restored filter threw an exception (eg `Unable to cast object of type 'System.Int16' to type 'System.Int32'`), which killed the circuit and left the page stuck on its loading indicator. The confusing part was that this only happened for users who had previously filtered that grid, as local storage is per browser and per origin. The same page would work perfectly well locally, or for a colleague on a different machine, which makes it look like an environment or network problem rather than a bug.

The component now repairs the state as it loads it, converting each filter value back to the type of the member it filters, and restoring the `MemberType` that was lost. Filters on members that no longer exist on `TItem` (eg a column renamed or removed since the state was saved) are dropped, as they would throw when the grid reads its data.

If you handle grid state yourself, the same repair is available as `TelerikGridStateHelper.RepairFilterDescriptors(state)`.

#### Extension method to improve the performance of the Telerik Blazor grid
>**Moved.** As of version 12.3.18, `TelerikGridHelper` (`args.GetData<T>()`), along with `TelerikGridFilterResults` and `TelerikGridFilterOptions`, has moved to the new [Pixata.AspNetCore.Telerik package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Telerik), where it is documented. It queries a database, so it needed EF Core, `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.Data.SqlClient`, which meant every client-side app using these components downloaded a SQL Server driver it could never use. This package no longer references any of them.
>
>If you use it, reference `Pixata.AspNetCore.Telerik` from your server project and change `using Pixata.Blazor.TelerikComponents.Helpers;` to `using Pixata.AspNetCore.Telerik.Helpers;`. Nothing else changes.

### TelerikGridBoolFilter
Whilst row filtering on Telerik grids is, in general, pretty brilliant, the one thing that lets it down (in my opinion anyway, feel free to disagree) is the way the controls on `bool` columns are handled. By default, a `bool` column's row filters will show a dropdown for the value, and a button to clear the filter. Apart from the fact that this takes up far more space than the column needs, the button is superfluous, as the dropdown has an "(All)" entry, which isn't even selected by default. Also, the dropdown values are "is true" and "is false", which is not the way the average user thinks.

So, I decided something neater was required. The `TelerikGridBoolFilter` component is an easy way to filter `bool` columns. Usage is very simple...

```xml
<GridColumn Field="@nameof(WeatherForecast.Rain)"
            Width="100px"
            Title="Rain?"
            FilterCellTemplate="@TelerikGridBoolFilter.Filter()" />
```

This is based on the standard Blazor sample page that shows weather forecasts, to which I added a `bool` property called `Rain`. The column header now just shows a checkbox, which by default is in the indeterminate state, meaning show all, but can be checked or unchecked to filter the column.

### LocalisationHelper
The Telerik Blazor components supply a default text for many situations. For example, if a grid does not contain any data, then it will show the rather geeky message "No records to display". Overriding these messages with your own text is not hard, but is now even easier with the `LocalisationHelper` class in this package.

Basic usage is as simple as adding the following line to any <code>Program.cs</code> file in your app..

```csharp
builder.Services.AddSingleton(typeof(ITelerikStringLocalizer), typeof(LocalisationHelper));
```

However, that will only override two of the messages (the ones that annoy me the most)...

- "No records to display" in grids now shows "Sorry, nothing matched your filters. Please widen your search criteria"
- When binding a dropdown (or any component that allows filtering) to an <code>enum</code>, the first entry in the dropdown is changed from "Select a value" to "All" which I think is more sensible. It's also shorter, which is an advantage when your values are short.

However, adding your own messages, or modifying my choice of the above is easy...

```csharp
LocalisationHelper.Values["DatePicker_Open"] = "Open Sesame";
builder.Services.AddSingleton(typeof(ITelerikStringLocalizer), typeof(LocalisationHelper));
```

As the class is static, you only need to do this once.

You can see a list of all messages on ([Telerik's web site](https://www.telerik.com/blazor-ui/documentation/api/telerik.blazor.resources.messages)).

### Form components
These were written to make it quicker to create forms in Blazor. They follow the same layout as the ones in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor), which was originally built with Bootstrap's grid and form classes. As of v12.3.17 they no longer need Bootstrap, but they do need the stylesheet that comes with Pixata.Blazor, so add this to your `_Host.cshtml`, `App.razor` or `index.html`...

```html
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

See [the styling section of the Pixata.Blazor readme](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Styling.md) for what's in it and how to fit it to your own theme.

If you look at the [form page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FormSample.razor) on the sample web site you can see the usage.

The components are...

| Component | For |
| --- | --- |
| `FormRowDateTelerik`, `FormRowDateNullableTelerik` | A date, using a `TelerikDatePicker`. Set `ShowTime` to include the time |
| `FormRowTimeTelerik` | A time, using a `TelerikTimePicker` |
| `FormRowHtmlEditor` | Formatted text, using the `TelerikEditor`. Takes `Height` (default 400px) and `Width` (default 100%) |
| `FormRowButtons` | The submit and cancel buttons at the bottom of the form, lined up with the rows above |

Apart from `FormRowButtons`, they take the same parameters as the [rows in Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Forms.md#complete-form-rows) - `PropertyName`, `Value`, `Caption` and `Icon` - and must be inside an `EditForm`.

`FormRowButtons` takes `SubmitCaption` and `CancelCaption` (with `SubmitClass` and `CancelClass` for styling), `CancelUrl` for where the cancel button navigates to, and `ShowCancel` if you only want the one button.

### TelerikHebrewDatePicker

A Telerik-flavoured version of the [`HebrewDatePicker`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Components.md#hebrewdatepicker) in Pixata.Blazor. It binds to a `DateTime` or `DateTime?`, and can show Hebrew dates, Gregorian dates, or both.

```xml
<TelerikHebrewDatePicker @bind-Value="_date"
                         ShowSecondaryDates="true"
                         IncludeShabbosOrYomTov="true" />
```

The parameters worth knowing about...

| Parameter | Default | What it does |
| --- | --- | --- |
| `InputDisplay` | `Hebrew` | Whether the input box shows the Hebrew or the Gregorian date |
| `GregorianPrimary` | `false` | Swaps which calendar the day cells lead with |
| `ShowSecondaryDates` | `false` | Shows the other calendar's date as well, optionally in brackets (`ShowSecondaryDatesInBrackets`) |
| `IncludeShabbosOrYomTov` | `false` | Shades Shabbos and Yom Tov, in `ShabbosOrYomTovBgColour` |
| `IncludeOtherNonWorkDays` | `false` | Also shades Chol Hamoed, Tisha B'av and Purim, in `OtherNonWorkDayBgColour` |
| `IncludeBankHolidays` | `false` | Also shades UK bank holidays, in `BankHolidayBgColour`. `ClashPriority` says which colour wins when a bank holiday falls on a Yom Tov |
| `ShowTooltips` | `false` | Names the day (Yom Tov, bank holiday) on hover |
| `MinDate`, `MaxDate` | 1584 to 2238 | The range the picker allows |
| `Width`, `Class` | `"320px"` | Sizing and styling of the input |
| `OpenOnFocus`, `OpenOnRight`, `AnimationType` | | How and where the calendar pops up |

`ShowDiagnostics` puts the conversion details on screen, which is handy if you think a date is coming out wrong.

## Grid column helpers

Telerik's default filter operators offer far more options than most users need, and the extra ones only get in the way. These subclasses of `GridColumn` are drop-in replacements that cut the list down to the ones people actually use...

| Column | Filter operators |
| --- | --- |
| `TextGridColumn` | contains (the default), starts with, ends with |
| `NumericGridColumn` | equals, less than, more than |
| `DateGridColumn` | before, on, on or after |
| `EnumGridColumn` | equals, with the filter cell buttons hidden, as they aren't needed |

Use them exactly as you would a `GridColumn`...

```xml
<TextGridColumn Field="@nameof(Person.Surname)" Title="Surname" />
<DateGridColumn Field="@nameof(Person.DateOfBirth)" Title="Born" />
```

The operator lists themselves are static properties on `GridFilterOperators` (`StringOperators`, `NumericOperators` and `DateOperators`), so you can change the wording, or the operators, once in `Program.cs` and have every column pick it up.

## TelerikFilterHelper

Building a `CompositeFilterDescriptor` by hand is tedious, so this class does it for you...

```csharp
// Everyone whose surname contains "smith"
CompositeFilterDescriptor filter =
  TelerikFilterHelper.CreateSimple(nameof(Person.Surname), FilterOperator.Contains, "smith");

// Everyone born in 2024
CompositeFilterDescriptor born =
  TelerikFilterHelper.CreateAnd(nameof(Person.DateOfBirth),
    new OperatorValue(FilterOperator.IsGreaterThanOrEqualTo, new DateTime(2024, 1, 1)),
    new OperatorValue(FilterOperator.IsLessThan, new DateTime(2025, 1, 1)));
```

`CreateOr()` does the same with an "or" between the conditions.

`RemoveTime()` fixes the usual "why does filtering on today's date find nothing?" problem you get when a column holds a `DateTime` with a time part. For each member you name, it rewrites an "equals" filter into "on or after that date, and before the next one", so the whole day matches. Call it in your `OnRead` handler before you read the data...

```csharp
private async Task OnRead(GridReadEventArgs args) {
  TelerikFilterHelper.RemoveTime(args, nameof(Person.DateOfBirth));
  // Now read your data
}
```

## Warning
The package relies on the Telerik.Blazor Nuget package. If you don't have a subscription with Telerik, you can get a 30-day trial version from them.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Sample) to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project, but should hopefully be expanded to include sample usage of the components.

>Note that the sample web site is not fully working, and as of 17th Sept '24 isn't being updated when the code changes. I would like to sort this out at some point, but don't have the time right now, so don't hold your breath!
