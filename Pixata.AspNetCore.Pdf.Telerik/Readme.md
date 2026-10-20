# Pixata.AspNetCore.Pdf.Telerik [![Pixata.AspNetCore.Pdf.Telerik Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Pdf.Telerik)](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Telerik/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore.Pdf.Telerik/ordinateuroff.png "Pixata")

A PDF converter for the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore), using [Telerik Document Processing](https://www.telerik.com/document-processing-libraries). It turns the HTML rendered from your Blazor template into a PDF, without a browser or a native PDF engine.

>Telerik Document Processing needs a Telerik licence. [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright) is free, and gives much better results, as it prints templates as Chrome would, with full CSS support. It is recommended instead of this package.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Telerik/) is available for this project.

>**Version 1.0.3** changes the package description and this readme to recommend Pixata.AspNetCore.Pdf.Playwright. No code changes.

>**Version 1.0.2** fixes the image at the top of this readme. No code changes.

>**Version 1.0.1** references a patched version of `System.Security.Cryptography.Pkcs`, as the one pulled in transitively had known vulnerabilities ([GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx), [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7)). No code changes.

## Setup

This package is a converter for the `DocumentTemplateHelper` in Pixata.AspNetCore, so you register both. In `Program.cs`...

```csharp
builder.Services.AddPixataAspNetCore<Program>();
builder.Services.AddPixataTelerikPdf();
```

The first line registers `DocumentTemplateHelper` (along with the `HtmlRenderer` and `IHttpContextAccessor` it needs), which renders a Blazor component to HTML, and then hands the HTML to the converter registered by the second line to turn it into a PDF.

The type parameter can be any type in your app. It's only used to find your FluentValidation validators for Pixata.AspNetCore's validation endpoint filter, which is registered at the same time. `Program` is always there, so it's the easiest choice. If you don't use the validation filter, the non-generic `builder.Services.AddPixataAspNetCore()` registers everything else. See [registering services](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.md#registering-services) for how to opt out of the parts you don't want.

### The base URL

Templates are given a `BaseUrl` parameter for linking to images and style sheets on your site, and the converter only loads them from that site (see [images and style sheets](#images-and-style-sheets)). You don't need to set it, as the helper takes it from the scheme and host of the current request, so it is right in development, test and live without any changes. This works when you create PDFs from an endpoint, a controller, a Razor page, or a statically rendered or prerendering Blazor component.

There is no current request inside an interactive Blazor Server component (after the first render), or in a hosted or background service. If you create PDFs from either of those, pick up the base URL from the first request the app gets, by adding this to `Program.cs` after `UseHttpsRedirection()` (and after `UseForwardedHeaders()`, if the app is behind a reverse proxy)...

```csharp
PixataAspNetCoreOptions pixataOptions = app.Services.GetRequiredService<PixataAspNetCoreOptions>();
app.Use(async (HttpContext context, Func<Task> next) => {
  if (pixataOptions.DocumentBaseUrl == "") {
    pixataOptions.DocumentBaseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
  }
  await next();
});
```

An interactive Blazor Server component always follows a request (the one that loaded its page), so this always works there. A background service that creates a PDF before the site has had any requests gets an `InvalidOperationException` saying that the base URL can't be worked out.

As this trusts the `Host` header of the first request, set `AllowedHosts` in the live site's `appsettings.json` (eg `"AllowedHosts": "www.example.com"`), so that a request with a made-up host is rejected rather than becoming the base URL. Your host may already filter requests by host name for you.

Then use `DocumentTemplateHelper.CreatePdfFromTemplate` as described in [generating documents and PDFs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md).

Telerik Document Processing needs a Telerik licence, in the same way as Telerik UI for Blazor. If you already use the Telerik components, your licence covers it.

## Writing templates for this converter

Telerik's HTML import is not a browser. It handles text formatting, tables, lists, images and simple CSS well, but it doesn't support flexbox, grid or positioning. Lay templates out with tables, and keep CSS simple.

## Images and style sheets

Images and linked style sheets are loaded over HTTP, but only from the site in `PdfOptions.BaseUrl` (which `DocumentTemplateHelper` sets for you from `DocumentBaseUrl` or the current request). Relative URLs are resolved against it. Anything else (another site, an internal address) is replaced with a blank image. This means that user-supplied content in a template, such as a client's notes on an invoice, can't make your server fetch arbitrary URLs.

Images are decoded with Telerik's `ImageUtils`, which uses SkiaSharp. The Linux native library is included, so this works on Linux hosts (including Alpine) as well as Windows and macOS. `AddPixataTelerikPdf()` sets `FixedExtensibilityManager.ImagePropertiesResolver` and `JpegImageConverter`, unless your app has already set them.
