# Pixata.AspNetCore.Pdf.WkHtmlToPdf [![Pixata.AspNetCore.Pdf.WkHtmlToPdf Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore.Pdf.WkHtmlToPdf)](https://www.nuget.org/packages/Pixata.AspNetCore.Pdf.WkHtmlToPdf/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore.Pdf.WkHtmlToPdf/ordinateuroff.png "Pixata")

## Obsolete

>This package exists so that apps that used the PDF generation in Pixata.AspNetCore before v2.0.0 can upgrade without changing their templates straight away. wkhtmltopdf was archived in January 2023 and gets no more fixes, security fixes included. It is built on an old QtWebKit, with known server-side request forgery and local file read problems when it renders HTML that contains anything user-supplied. Move to [Pixata.AspNetCore.Pdf.Telerik](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Telerik) or [Pixata.AspNetCore.Pdf.Playwright](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Playwright) as soon as you can. This package will be removed in a future release.

>**Version 1.0.1** updates the `[Obsolete]` messages to mention Pixata.AspNetCore.Pdf.Playwright as well as Pixata.AspNetCore.Pdf.Telerik. No other changes.

A PDF converter for the `DocumentTemplateHelper` in [Pixata.AspNetCore](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore), using wkhtmltopdf (through Haukcode.WkHtmlToPdfDotNet). This is the converter that Pixata.AspNetCore used before v2.0.0.

## Setup

In `Program.cs`...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>();
builder.Services.AddPixataWkHtmlToPdf();
```

This registers the wkhtmltopdf `IConverter` (as a factory, so the native library isn't loaded until a PDF is generated), and a `PdfConverterInterface` that uses it. Both are marked `[Obsolete]`, so you'll get a warning when you call it.

Before v2.0.0, PDFs were always A4Plus. `PdfOptions` now defaults to A4, so pass `new PdfOptions { PaperSize = PdfPaperSize.A4Plus }` to `CreatePdfFromTemplate` if you need the old size.
