# Pixata.AspNetCore [![Pixata.AspNetCore Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore)](https://www.nuget.org/packages/Pixata.AspNetCore/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore/ConnectionReseau.png "Pixata")

Server-side helpers for ASP.NET Core apps - entity auditing, payload encryption, request logging, endpoint validation, document generation and route dumping.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore/) is available for this project.

## Breaking changes in v2.0.0

- **No PDF engine.** This package no longer references wkhtmltopdf (which was archived in 2023, and is unsafe with user-supplied HTML), so apps that don't generate PDFs no longer deploy its native binary. `DocumentTemplateHelper` now converts through a `PdfConverterInterface`, which comes from a separate package. Add [Pixata.AspNetCore.Pdf.Telerik](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Telerik) and call `AddPixataTelerikPdf()`, or, if you need to keep wkhtmltopdf for now, add the obsolete [Pixata.AspNetCore.Pdf.WkHtmlToPdf](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.WkHtmlToPdf) and call `AddPixataWkHtmlToPdf()`. The wkhtmltopdf `IConverter` is no longer registered, and `RegisterPdfConverter` is obsolete and does nothing. See [generating documents and PDFs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md).
- **`DocumentTemplateHelper` no longer needs a request.** Set the new `DocumentBaseUrl` option to generate documents from interactive Blazor Server components or background services. Without it, the helper uses the current request as before, and throws a clear `InvalidOperationException` when there isn't one, rather than a `NullReferenceException`. Its constructor has changed, which only matters if you construct it yourself.
- **Auditing.** `AddAuditing<TContext>()` now takes an `Action<AuditingOptions>` (which inherits from `AuditRetentionOptions`, so existing lambdas still compile), and `AuditingInterceptor`'s constructor takes the options and an `IServiceProvider`. Synchronous `SaveChanges()` calls are now audited, users in interactive Blazor Server pages are now recorded rather than `"System"`, and added entities are saved in a transaction with their audit rows. See [auditing entities](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Auditing.md).

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
| [Generating documents and PDFs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md) | `DocumentTemplateHelper`, which renders a Blazor component to HTML or (with a PDF converter package) to a PDF |
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
  o.RegisterDocumentTemplateHelper = false;
});
```

The options are...

| Option | Default | What it registers |
| --- | --- | --- |
| `RegisterDocumentTemplateHelper` | `true` | `DocumentTemplateHelper`, along with the `HtmlRenderer` and `IHttpContextAccessor` that it needs |
| `DocumentBaseUrl` | `""` | Not a registration, but the base URL `DocumentTemplateHelper` passes to templates. When empty, it uses the current request, which doesn't exist in interactive Blazor Server components or background services |
| `RegisterValidation` | `true` | Your FluentValidation validators (from the assembly containing the type you pass in) and the `ValidationEndpointFilter` |

`RegisterPdfConverter` is obsolete as of v2.0.0, and does nothing. This package no longer includes a PDF engine, see [breaking changes in v2.0.0](#breaking-changes-in-v200).

If you don't use the validation filter, there is a non-generic overload, which registers everything apart from the validation services...

```csharp
builder.Services.AddPixataAspNetCore(o => o.DocumentBaseUrl = "https://www.example.com");
```

Note that `AddPixataAspNetCore` does not register the auditing, encryption or request logging services. Those have their own registration methods, as most apps only want some of them. See the pages listed above.

If you want to use the [route dump feature](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.RouteDumping.md) then you'll also need the following...

```csharp
app.MapPixataAspNetCoreApiEndpoints();
```
