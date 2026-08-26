# Pixata.Blazor.TelerikComponents [![Pixata.Blazor.TelerikComponents Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor.TelerikComponents)](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Blazor.TelerikComponents/MailOldSchool.png "Pixata") 

This package complements the [Pixata.Blazor package](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/), and adds components that rely on the Telerik components for Blazor. These were split off into their own package to enable those without a licence for Telerik to use the other components.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/) is available for this project.

>Note that as of version 12.2.0, the major and minor package versions will correspond to the version of the Telerik.Blazor package that is required. The patch version will be used for updates to this package. The build number will indicate my own internal versioning. Thus, version 12.2.0 is based on the Telerik Blazor package version 12.2.x, where x is my own incremental build number.

## The components

## TelerikGrid with automatic save and restore of state
Saving and restoring state is a very useful feature, as it allows the grid to return to its previous state when the user left the page. Handling this manually is not major, but painful.

I wrote the `TelerikGridWithState` component as almost drop-in replacement for the standard `TelerikGrid`, but preserves the grid state in local storage. If you use virtualisation, then the current skip and take will also be restored.

All you need to do is set a value for the `StorageKey` parameter and you're done. 

In the unlikely event that you need to handle `OnStateInit` or `OnStateChanged` events, you can do so by using the `OnStateInitPre`, `OnStateInitPost`, `OnStateChangedPre`, and `OnStateChangedPost` event handlers. These allow you to run your own code before or after the component's state handling.

### Extension method to improve the performance of the Telerik Blazor grid
>**Moved.** As of version 12.3.18, `TelerikGridHelper` (`args.GetData<T>()`), along with `TelerikGridFilterResults` and `TelerikGridFilterOptions`, has moved to the new [Pixata.AspNetCore.Telerik package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Telerik), where it is documented. It queries a database, so it needed EF Core, `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.Data.SqlClient`, which meant every client-side app using these components downloaded a SQL Server driver it could never use. This package no longer references any of them.
>
>If you use it, reference `Pixata.AspNetCore.Telerik` from your server project and change `using Pixata.Blazor.TelerikComponents.Helpers;` to `using Pixata.AspNetCore.Telerik.Helpers;`. Nothing else changes.

## TelerikGridBoolFilter
Whilst row filtering on Telerik grids is, in general, pretty brilliant, the one thing that lets it down (in my opinion anyway, feel free to disagree) is the way the controls on `bool` columns are handled. By default, a `bool` column's row filters will show a dropdown for the value, and a button to clear the filter. Apart from the fact that this takes up far more space than the column needs, the button is superfluous, as the dropdown has an "(All)" entry, which isn't even selected by default. Also, the dropdown values are "is true" and "is false", which is not the way the average user thinks.

So, I decided something neater was required. The `TelerikGridBoolFilter` component is an easy way to filter `bool` columns. Usage is very simple...

```xml
<GridColumn Field="@nameof(WeatherForecast.Rain)"
            Width="100px"
            Title="Rain?"
            FilterCellTemplate="@TelerikGridBoolFilter.Filter()" />
```

This is based on the standard Blazor sample page that shows weather forecasts, to which I added a `bool` property called `Rain`. The column header now just shows a checkbox, which by default is in the indeterminate state, meaning show all, but can be checked or unchecked to filter the column.

## LocalisationHelper
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
These were writen to make it quicker to create forms in Blazor. They follow the same layout as the ones in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor), which was originally built with Bootstrap's grid and form classes. As of v12.3.17 they no longer need Bootstrap, but they do need the stylesheet that comes with Pixata.Blazor, so add this to your `_Host.cshtml`, `App.razor` or `index.html`...

```html
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

See [the styling section of the Pixata.Blazor readme](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor#styling) for what's in it and how to fit it to your own theme.

If you look at the [form page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FormSample.razor) on the sample web site you can see the usage.)

## Warning
The package relies on the Telerik.Blazor Nuget package. If you don't have a subscription with Telerik, you can get a 30-day trial version from them.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Test) to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project, but should hopefully be expanded to include sample usage of the components.

>Note that the sample web site is not fully working, and as of 17th Sept '24 isn't being updated when the code changes. I would like to sort this out at some point, but don't have the time right now, so don't hold your breath!
