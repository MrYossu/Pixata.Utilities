# Pixata.AspNetCore.Pdf.Telerik [![Pixata.AspNetCore.Pdf.Telerik Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Pdf.Telerik)](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Telerik/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore.Pdf.Telerik/ConnectionReseau.png "Pixata")

A PDF converter for the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore), using [Telerik Document Processing](https://www.telerik.com/document-processing-libraries). It turns the HTML rendered from your Blazor template into a PDF, without a browser or a native PDF engine.

A [Nuget package](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.Telerik/) is available for this project.

## Setup

In `Program.cs`...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>(o => o.DocumentBaseUrl = "https://www.example.com");
builder.Services.AddPixataTelerikPdf();
```

Then use `DocumentTemplateHelper.CreatePdfFromTemplate` as described in [generating documents and PDFs](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.AspNetCore/Readme.Documents.md).

Telerik Document Processing needs a Telerik licence, in the same way as Telerik UI for Blazor. If you already use the Telerik components, your licence covers it.

## Writing templates for this converter

Telerik's HTML import is not a browser. It handles text formatting, tables, lists, images and simple CSS well, but it doesn't support flexbox, grid or positioning. Lay templates out with tables, and keep CSS simple.

## Images and style sheets

Images and linked style sheets are loaded over HTTP, but only from the site in `PdfOptions.BaseUrl` (which `DocumentTemplateHelper` sets for you from `DocumentBaseUrl` or the current request). Relative URLs are resolved against it. Anything else (another site, an internal address) is replaced with a blank image. This means that user-supplied content in a template, such as a client's notes on an invoice, can't make your server fetch arbitrary URLs.

Images are decoded with Telerik's `ImageUtils`, which uses SkiaSharp. The Linux native library is included, so this works on Linux hosts (including Alpine) as well as Windows and macOS. `AddPixataTelerikPdf()` sets `FixedExtensibilityManager.ImagePropertiesResolver` and `JpegImageConverter`, unless your app has already set them.
