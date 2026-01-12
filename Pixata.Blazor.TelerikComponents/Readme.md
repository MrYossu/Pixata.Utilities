# Pixata.Blazor.TelerikComponents [![Pixata.Blazor.TelerikComponents Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor.TelerikComponents)](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor.TelerikComponents/Icon/mail%20old%20school.png "Pixata") 

This package complements the [Pixata.Blazor package](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/), and adds components that rely on the Telerik components for Blazor. These were split off into their own package to enable those without a licence for Telerik to use the other components.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/) is available for this project.

>Note that as of version 12.2.0, the major and minor package versions will correspond to the version of the Telerik.Blazor package that is required. The patch version will be used for updates to this package. The build number will indicate my own internal versioning. Thus, version 12.2.0 is based on the Telerik Blazor package version 12.2.x, where x is my own nicremental build number.

## The components
### Form components
These were writen to make it quicker to create forms in Blazor. They are all very much based around Bootstrap, which I was using heavily when I wrote these components. If you look at the [form page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FormSample.razor) on the sample web site you can see the usage.)

### Extension method to improve the performance of the Telerik Blazor grid
Whilst the Telerik Blazor grid does an amazing job, it has its limitations. One of these is the way it computes aggregates. For large tables, this can be slow.

I had a play with Dapper, which improved matters significantly, and then found out that I could do the same with pure EF Core, without any extra packages. I wrapped up the code into an extension method for `GridReadEventArgs`, which you can call from the `OnRead` event of the grid. In the project that motivated this code, I managed to reduce the time taken for the grid to load from over 10 seconds to around 400 milliseconds 😎.

There is a [sample repo](https://github.com/MrYossu/TelerikGridWithFromSql) that shows the method in usage, and [a blog post that explain the usage](https://www.pixata.co.uk/2024/10/08/hmm-maybe-ef-core-isnt-so-bad-after-all/) (with far too many anecdotes and rambling). If you are bored, that post links to two previous posts that detail my journey to this extension method.

Note that as from version 2.0.0 of the package, the method allows you to query a table-valued function as well as a table or view.

#### Breaking change
As of version 2.0.0, the generic parameter has been removed from `TelerikGridFilterResults` (as it wasn't needed), so you will need to update your code if you capture this as an explicit type. For example, change this:

```csharp
TelerikGridFilterResults<MyType> data = await args.GetData<MyType>(/* args go here */)
```

...to this...

```csharp
TelerikGridFilterResults data = await args.GetData<MyType>(/* args go here */)
```

Not a major issue, but worth noting.

## Warning
The package relies on the Telerik.Blazor Nuget package. If you don't have a subscription with Telerik, you can get a 30-day trial version from them.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Test) to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project, but should hopefully be expanded to include sample usage of the components.

>Note that the sample web site is not fully working, and as of 17th Sept '24 isn't being updated when the code changes. I would like to sort this out at some point, but don't have the time right now, so don't hold your breath!
