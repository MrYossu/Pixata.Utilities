# Pixata.AspNetCore [![Pixata.AspNetCore Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore)](https://www.nuget.org/packages/Pixata.AspNetCore/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore/ConnectionReseau.png "Pixata")

Server-side helpers for ASP.NET Core apps - entity auditing, payload encryption, request logging, endpoint validation, document generation and route dumping.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore/) is available for this project.

## Important

Everything in this package is designed to be used server-side, and it references EF Core and other server-only libraries. Reference it from a server-side project. If you add it to a WASM project, you will get errors.

The client-side halves of the features that have one live in [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor), and the types that both ends share live in [Pixata.Extensions](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions).

## Documentation

The documentation is split over the following pages...

| Page | What's in it |
| --- | --- |
| [Auditing entities](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Auditing.md) | The EF Core interceptor that records every entity change, how to set it up, opting entities out, identifying the user, serving the trail to the viewer, and retention policies |
| [Payload encryption](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Encryption.md) | ECDH + AES-256-GCM encryption of request and response bodies, the server-side setup, and the .NET 10 caveats |
| [Request logging middleware](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.RequestLogging.md) | Logging incoming requests to help debug API calls, with redaction of anything that looks sensitive |
| [Validation endpoint filter](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Validation.md) | Running your FluentValidation validators on the server, so a WASM client can't bypass them |
| [Generating documents and PDFs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md) | `DocumentTemplateHelper`, which renders a Blazor component to HTML or to a PDF |
| [Route dumping](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.RouteDumping.md) | An endpoint that lists every route in your app |

## Registering services

The code in this package requires certain dependencies to be registered in the DI container. In order to make this easier, there is an extension method to add them all. In `Program.cs` add this line...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>();
```

...where `ContactModel` is any type in your project. If you are using the validation filter, then it is used here to point the framework to the assembly containing your models.

This registers everything in the package. If you only want some of it, you can say so...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>(o => {
  o.RegisterPdfConverter = false;
  o.RegisterDocumentTemplateHelper = false;
});
```

The options are...

| Option | Default | What it registers |
| --- | --- | --- |
| `RegisterPdfConverter` | `true` | The wkhtmltopdf `IConverter` used by `DocumentTemplateHelper` to generate PDFs. Set this to `false` if your app generates PDFs some other way (QuestPDF, for example), and you don't want the native wkhtmltopdf library anywhere near your app |
| `RegisterDocumentTemplateHelper` | `true` | `DocumentTemplateHelper`, along with the `HtmlRenderer` and `IHttpContextAccessor` that it needs |
| `RegisterValidation` | `true` | Your FluentValidation validators (from the assembly containing the type you pass in) and the `ValidationEndpointFilter` |

The converter is registered as a factory, so wkhtmltopdf isn't loaded until something actually asks for it. Prior to v1.9.0 it was constructed while services were being registered, which meant that every app referencing this package loaded the native library at startup, even if it never generated a PDF.

As `DocumentTemplateHelper` takes an `IConverter`, setting `RegisterPdfConverter` to `false` whilst leaving `RegisterDocumentTemplateHelper` set to `true` will throw an exception when you register the services, unless you have registered an `IConverter` of your own first. This is deliberate, as it's a lot easier to fix than the error you'd otherwise get when something tries to resolve the helper.

If you don't use the validation filter, there is a non-generic overload, which registers everything apart from the validation services...

```csharp
builder.Services.AddPixataAspNetCore(o => o.RegisterPdfConverter = false);
```

Note that `AddPixataAspNetCore` does not register the auditing, encryption or request logging services. Those have their own registration methods, as most apps only want some of them. See the pages listed above.

If you want to use the [route dump feature](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.RouteDumping.md) then you'll also need the following...

```csharp
app.MapPixataAspNetCoreApiEndpoints();
```
