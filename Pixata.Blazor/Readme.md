# Pixata.Blazor [![Pixata.Blazor Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor)](https://www.nuget.org/packages/Pixata.Blazor/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Blazor/avion.png "Pixata")

Like many developers, I find myself writing the same Blazor components over and over again. I have gathered some of those components together in one place, and am making them available for anyone else who might find them useful.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for these components.

I sometimes waffle on about these components [in my blog](https://www.pixata.co.uk/tag/blazor/). No-one reads it, but it satisfies my deeply buried desire to be a writer. If you read the blog, you'll realize why the desire is still deeply buried!

There is a [complementary package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents), which contains additional components for those who have a subscription to Telerik.

## Sample project

I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Sample) to the repository, and use that to try out and demonstrate the components. It doesn't contain samples for all the components yet, but I hope to add more over time. It is deployed at [test.pixata.co.uk](https://test.pixata.co.uk).

## Documentation

The documentation is split over the following pages...

| Page | What's in it |
| --- | --- |
| [Styling](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Styling.md) | The stylesheet the components ship with, how to fit it to your own theme, and what changed when the Bootstrap dependency was dropped |
| [Components](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Components.md) | `VirtualiseWithState`, `SitePageTitle`, `IdentityInspector`, `HebrewDatePicker`, `ListOrGridChooser` and the `CommonComponentBase` base class |
| [Containers](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Containers.md) | `HtmlRaw`, `Busy`, `Loader`, `Confirm`, `Inform`, `MessageView`, `Expander` and `DumpCollection` |
| [ApiResponseView](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.ApiResponseView.md) | The container that renders an `ApiResponse<T>`, handling loading, errors and expired sessions for you |
| [Forms](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Forms.md) | The form layout components and the complete `FormRowXxx` rows |
| [Notifications](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Notifications.md) | Toast-style notifications, and how to have API errors reported through them |
| [Audit viewer](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.AuditViewer.md) | The component for browsing the audit trail recorded by Pixata.AspNetCore |
| [Helpers and services](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Helpers.md) | `PersistentStateHelper`, `TemplateHelper`, `MessageBroker`, `PasswordOptionsHelper`, `ScrollStateService`, `PixataBaseClientService` and `TryGetQueryString()` |
| [Payload encryption](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Encryption.md) | Client-side setup for encrypting API traffic between a WASM app and an ASP.NET Core server |
| [Declarative control flow](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.ControlFlow.md) | `If`, `Switch` and `ForEach`, which replace the Razor `@if`, `@switch` and `@foreach` statements |

## Registering dependencies

Some components in this package require services to be registered in the DI container. To make this easier, you can use the `AddPixataBlazor` extension method in your `Program.cs` file...

```csharp
builder.Services.AddPixataBlazor();
```

This registers the following services (all from this package)...

| Service | What needs it |
| --- | --- |
| `MessageBrokerInstance` | The [`MessageBroker`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Helpers.md#messagebroker), which lets components send messages to each other without knowing about each other |
| `NotificationHelper` | The [notifications](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Notifications.md) ([sample page](https://test.pixata.co.uk/Notifications)) |
| `PasswordOptionsHelper` | Showing the user your Identity password requirements |
| `PersistentStateHelper<T>` | Avoiding hitting the database twice when a page loads. Used by the [`ApiResponseView`](https://test.pixata.co.uk/ApiResponseViewRegular), but can be used independently |
| `ScrollStateService` | The `VirtualiseWithState` component |
| `TemplateHelper` | Building templates for grids and other templated components ([sample page](https://test.pixata.co.uk/TelerikGrid)) |

>`PersistentStateHelper<T>` does the same job as the [`[PersistentState]` attribute](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#declarative-model-for-persisting-state-from-components-and-services) added in .NET 10. This one was written around .NET 8, and is still useful for projects targetting .NET versions before 10.

It also registers [Blazored.LocalStorage](https://github.com/Blazored/LocalStorage), which `ScrollStateService` needs. This package has always referenced Blazored.LocalStorage, but until v2.34.0 it left you to register it, so unless you knew to add `builder.Services.AddBlazoredLocalStorage()` yourself, your app fell over at startup when the container was validated. If you have already registered it (with your own options, for example), your registration is left alone.

Note that you need to do this in any `Program.cs` file, so if you have a mixed rendering mode (both server-side and client-side), you'll need to call `AddPixataBlazor` in both `Program.cs` files.

It is a good idea to add this line **after** your own service registrations, as it checks for duplicate registrations. Therefore, if you have already registered any of the services, you will see a message in your console...

>A service of type TemplateHelper has already been registered

This isn't actually a problem, but removing the duplicate registration will keep the code file a bit cleaner.

The audit viewer and the payload encryption have their own registration methods, as most apps don't want them. See the pages listed above.

## Styling

Don't forget to add the stylesheet, or the components will render unstyled...

```html
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

See [Styling](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.Styling.md) for what's in it, and how to fit it to your own theme.
