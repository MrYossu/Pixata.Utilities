# Pixata.AspNetCore [![Pixata.AspNetCore Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore)](https://www.nuget.org/packages/Pixata.AspNetCore/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore/ConnectionReseau.png "Pixata")

Server-side helpers for ASP.NET Core apps - entity auditing, payload encryption, request logging, endpoint validation, document generation, receiving file uploads and route dumping.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore/) is available for this project.

>**Version 2.1.1** lists [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright) alongside Pixata.AspNetCore.Pdf.Telerik in the message `DocumentTemplateHelper` throws when no PDF converter is registered, and in the `RegisterPdfConverter` obsolete message. No other changes.

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
| [Serving data to PixataGrid](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Grid.md) | `ToGridResultAsync()`, which applies a grid's paging, sorting and filtering to an EF Core query in the database |
| [Route dumping](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.RouteDumping.md) | An endpoint that lists every route in your app |
| [Receiving uploads](#receiving-uploads) | Saving files posted to an endpoint (eg by the Pixata.Blazor `PixataUpload` component), including chunked uploads, below |

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

## Receiving uploads

`ReceiveUpload()` is an extension method on `HttpRequest` that saves the files posted to an endpoint. It was written for the Pixata.Blazor [`PixataUpload` component](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.FileUpload.md), but works with any form post. It needs no registration...

```csharp
string uploadFolder = Path.Combine(builder.Environment.ContentRootPath, "Uploads");

app.MapPost(RoutesHelper.UploadSave, async (HttpRequest request) => {
  UploadReceiveResult result = await request.ReceiveUpload(uploadFolder, o => {
    o.AllowedExtensions = [".pdf", ".docx"];
    o.MaxFileSize = 20 * 1024 * 1024;
  });
  return result.Success ? Results.Ok(result.Files.Select(f => f.FileName)) : Results.BadRequest(result.Error);
});
```

It saves each file in the `files` form field to the folder (creating it if need be), and...

- **Sanitises the file names**, so a request can't write outside the folder, and the name is safe on any server: it removes any folders, replaces characters that Windows doesn't allow (on every platform, so a name is saved the same way wherever you run), control characters and invisible formatting characters (such as the ones that reverse text, used to disguise `fdp.exe` as `exe.pdf`), puts an underscore in front of names that Windows reserves for devices (`CON`, `nul.txt`, `COM1.pdf` and so on), and shortens names over 200 bytes, keeping the extension. `UploadRequestExtensions.Sanitise()` is public, if you want to do the same elsewhere.
- **Checks the extension and size again**, as the browser's checks can be bypassed. A rejected file isn't saved, and `Error` says why. If a request holds several files, they are all checked before any is saved.
- **Writes each file under a temporary name first**, and renames it when it's complete, so nobody sees a half-written file, and two requests saving files with the same name don't trip over each other (with `Overwrite` off, each gets its own name, even if they arrive at the same moment).
- **Puts chunked uploads back together.** If the request has a `chunkMetadata` field (the JSON that `PixataUpload` sends with each chunk), the chunk is written to a `.part` file at its offset, and the file gets its real name when the last chunk arrives. A chunk sent twice overwrites itself rather than being added twice. `Files` is empty until the last chunk, and `Complete` is true once the file is saved.

Return a 2xx response when `Success` is true (for every chunk, not just the last), and a 400 otherwise, so the component shows the file as uploaded or failed.

The options are...

| Option | Default | Description |
| --- | --- | --- |
| `SaveField` | `files` | The form field that holds the file (or chunk) |
| `MetadataField` | `chunkMetadata` | The form field that holds a chunk's JSON metadata |
| `AllowedExtensions` | empty | The extensions accepted (the dot is optional, and case is ignored). Empty accepts any |
| `MaxFileSize`, `MinFileSize` | null | The largest and smallest files accepted, in bytes. Null means no limit, other than the server's limit on request size, which doesn't limit a chunked upload, as each chunk is a separate request. So set `MaxFileSize` if you accept chunked uploads |
| `Overwrite` | `true` | When false, a file with the same name as one already there is saved as eg `report (1).pdf` |
| `FileName` | null | A `Func<string, string>` that chooses the name to save under, given the uploaded name (eg to add a GUID). Its result is sanitised too |

The result has `Success`, `Complete`, `Error`, `Chunk` (the chunk's metadata, for a chunked upload) and `Files`, each of which is a `ReceivedFile` with the `FileName` it was saved under, the `OriginalFileName`, the full `Path`, the `Size` and the `ContentType`. If a file couldn't be written (eg the disk is full), `Error` just says so, as it's usually sent back to the browser, and `Exception` holds the exception for you to log.

To delete files when the user removes them, point the component's `RemoveUrl` at an endpoint that calls `RemoveUploads()`. It deletes the files named in the `files` field from the folder (again, sanitising the names), and returns the names it deleted...

```csharp
app.MapPost(RoutesHelper.UploadRemove, async (HttpRequest request) =>
  Results.Ok(await request.RemoveUploads(uploadFolder)));
```

The component sends the name the file had on the user's machine, so `RemoveUploads()` only suits a folder where files are saved under that name. If you set `FileName`, or turn `Overwrite` off, the file may have been saved under another name (and the one sent may belong to another upload), so keep track of the names yourself (eg send the saved name back from the save endpoint, and look it up in the remove endpoint). Bear in mind too that anyone who can post to the remove endpoint can delete any file in the folder whose name they can guess, so protect it, or use a folder per user.

A chunked upload that is cancelled, or abandoned when the user closes the page, leaves a `.part` file behind. Call `UploadRequestExtensions.DeleteAbandonedChunks(uploadFolder, TimeSpan.FromHours(1))` now and then (eg from a background service) to delete the ones that haven't been written to for a while.

A few things to bear in mind...

- Don't save uploads under `wwwroot`, unless you want anyone to be able to download them.
- Your endpoint is a normal endpoint, so protect it with `RequireAuthorization()` as you would any other. The browser sends its cookies with the upload.
- Kestrel limits a request to about 30 MB by default. Either upload in chunks (which keeps every request small), or raise the limit for the endpoint with `[RequestSizeLimit]` or `.WithMetadata(new RequestSizeLimitAttribute(...))`.
- `ReceiveUpload()` reads the form itself, so the endpoint doesn't bind any form parameters, and isn't subject to ASP.NET Core's antiforgery check. If your endpoint also binds `IFormFile` or `[FromForm]` parameters and you use `app.UseAntiforgery()`, either send the antiforgery token as a header from `OnUpload`, or call `.DisableAntiforgery()` on the endpoint.
