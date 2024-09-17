# Pixata.Blazor.TelerikComponents [![Pixata.Blazor.TelerikComponents Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor.TelerikComponents)](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor.TelerikComponents/Icon/mail%20old%20school.png "Pixata") 

This package complements the [Pixata.Blazor package](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/), and adds components that rely on the Telerik components for Blazor. These were split off into their own package to enable those without a licence for Telerik to use the other components.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor.TelerikComponents/) is available for this project.

## The components
### Form components
These were writen to make it quicker to create forms in Blazor. They are all very much based around Bootstrap, which I was using heavily when I wrote these components. If you look at the [form page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FormSample.razor) on the sample web site you can see the usage.)

### Dapper helper method
I had the delights of discovering how much faster your data can load if you use [Dapper](https://github.com/DapperLib/Dapper) instead of EF Core, and wrote an extension method to make it easy to use Dapper with the Telerik Blazor grid. Version 1.9.0 of this package includes a `TelerikGridHelper` class that holds that extension method.

More details and sample code can be found in [this blog pst](https://www.pixata.co.uk/2024/09/09/using-dapper-with-the-telerik-blazor-grid/).

## Warning
The package relies on the Telerik.Blazor Nuget package. If you don't have a subscription with Telerik, you can get a 30-day trial version from them.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Test) to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project, but should hopefully be expanded to include sample usage of the components.

>Note that the sample web site is not fully working, and as of 17th Sept '24 isn't being updated when the code changes. I would like to sort this out at some point, but don't have the time right now, so don't hold your breath!
