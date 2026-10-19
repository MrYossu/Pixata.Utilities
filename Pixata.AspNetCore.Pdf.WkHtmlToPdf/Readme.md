# Pixata.AspNetCore.Pdf.WkHtmlToPdf [![Pixata.AspNetCore.Pdf.WkHtmlToPdf Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Pdf.WkHtmlToPdf)](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.WkHtmlToPdf/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore.Pdf.WkHtmlToPdf/ordinateuroff.png "Pixata")

## Obsolete

>This package exists so that apps that used the PDF generation in Pixata.AspNetCore before v2.0.0 can upgrade without changing their templates straight away. wkhtmltopdf was archived in January 2023 and does not get any new fixes, security fixes included. It is built on an old QtWebKit, with known server-side request forgery and local file read problems when it renders HTML that contains anything user-supplied. It is recommended to move to [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright) as this provides for a much better level of conversion, and depends on external packages that will be maintained and updated.

>**Version 1.0.2** changes the package description to recommend Pixata.AspNetCore.Pdf.Playwright. No other changes.

>**Version 1.0.1** updates the `[Obsolete]` messages to mention Pixata.AspNetCore.Pdf.Playwright as well as Pixata.AspNetCore.Pdf.Telerik. No other changes.

A PDF converter for the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore), using wkhtmltopdf (through Haukcode.WkHtmlToPdfDotNet). This is the converter that Pixata.AspNetCore used before v2.0.0.

## Setup

This package is a converter for the `DocumentTemplateHelper` in Pixata.AspNetCore, so you register both. In `Program.cs`...

```csharp
builder.Services.AddPixataAspNetCore<Program>();
builder.Services.AddPixataWkHtmlToPdf();
```

The first line registers `DocumentTemplateHelper` (along with the `HtmlRenderer` and `IHttpContextAccessor` it needs), which renders a Blazor component to HTML, and then hands the HTML to the converter registered by the second line to turn it into a PDF.

The type parameter can be any type in your app. It's only used to find your FluentValidation validators for Pixata.AspNetCore's validation endpoint filter, which is registered at the same time. `Program` is always there, so it's the easiest choice. If you don't use the validation filter, the non-generic `builder.Services.AddPixataAspNetCore()` registers everything else. See [registering services](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.md#registering-services) for how to opt out of the parts you don't want.

Templates are given a `BaseUrl` parameter for linking to images and style sheets on your site. You don't need to set it, as the helper takes it from the scheme and host of the current request, so it is right in development, test and live without any changes. If you create PDFs from an interactive Blazor Server component or a background service, where there is no current request, see [the base URL](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright#the-base-url) in the Pixata.AspNetCore.Pdf.Playwright readme for how to pick it up from the first request instead.

The second line registers the wkhtmltopdf `IConverter` (as a factory, so the native library isn't loaded until a PDF is generated), and a `PdfConverterInterface` that uses it. Both are marked `[Obsolete]`, so you'll get a warning when you call it.

Before v2.0.0, PDFs were always A4Plus. `PdfOptions` now defaults to A4, so pass `new PdfOptions { PaperSize = PdfPaperSize.A4Plus }` to `CreatePdfFromTemplate` if you need the old size.
