# Generating documents and PDFs

>Part of [Pixata.AspNetCore](Readme.md)

I often find myself generating documents, either for conversion to PDF, or for emailing. This has always been a painful process, so I decided that a helper was needed. The `DocumentTemplateHelper` class contains methods for generating HTML from a Blazor component, and for generating a PDF from a Blazor component.

## Registering the services

The easiest way is to let the package do it...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>(o => o.DocumentBaseUrl = "https://www.example.com");
```

This registers `DocumentTemplateHelper` along with the `HtmlRenderer` and `IHttpContextAccessor` that it needs. See [registering services](Readme.md#registering-services) for how to opt out of the parts you don't want.

If you'd rather do it yourself, then you need the following in `Program.cs`...

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HtmlRenderer>();
builder.Services.AddScoped<DocumentTemplateHelper>();
```

In that case, the helper can't see `DocumentBaseUrl`, unless you also register the `PixataAspNetCoreOptions` yourself.

### The base URL

Templates are passed a `BaseUrl` parameter, so that they can link to images and style sheets on your site. If you set `DocumentBaseUrl`, that is what they get. If you don't, the helper uses the scheme and host of the current request.

There is no current request inside an interactive Blazor Server component (after the first render), in a hosted or background service, or in a test. If you generate documents from any of those, set `DocumentBaseUrl`. Without it, the helper throws an `InvalidOperationException` telling you to. Prior to v2.0.0, it threw a `NullReferenceException`.

### PDFs

As of v2.0.0, this package doesn't include a PDF engine, so that apps that only want HTML (or that don't use the helper at all) don't have to deploy one. To create PDFs, add a package that provides a `PdfConverterInterface`...

| Package | Registration | Notes |
| --- | --- | --- |
| [Pixata.AspNetCore.Pdf.Telerik](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.Telerik) | `builder.Services.AddPixataTelerikPdf();` | Recommended. Uses Telerik Document Processing, which is pure .NET and actively maintained. Its CSS support is more limited than a browser's (no flexbox or grid), so keep templates to simple, table-based layout. Needs a Telerik licence |
| [Pixata.AspNetCore.Pdf.WkHtmlToPdf](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.AspNetCore.Pdf.WkHtmlToPdf) | `builder.Services.AddPixataWkHtmlToPdf();` | Obsolete. This is what the package used before v2.0.0. wkhtmltopdf was archived in January 2023, gets no security fixes, and has known server-side request forgery and local file read problems with user-supplied HTML. Only use it while you move your templates over |

You can also register an implementation of your own...

```csharp
builder.Services.AddSingleton<PdfConverterInterface, MyPdfConverter>();
```

If no converter is registered, `CreateHtmlFromTemplate` works as normal, and `CreatePdfFromTemplate` throws an `InvalidOperationException` saying what's missing.

## Writing a template

Create a Blazor component that will be the template for the document you wish to generate. It needs to accept two parameters as follows...

```xml
<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8" />
    <title></title>
    <!-- Any links you need -->
  </head>
  <body>
    <div>
      <h2><img src="@BaseUrl/images/logo.png" width="70" height="70" /> Thank you for contacting us</h2>
      <div>Your message has been received, and we'll get back to you as soon as possible.</div>
      <div>
        <h3>Your message</h3>
        <HtmlRaw Html="@(Model.Message.Replace("\n", "<br/>"))" />
      </div>
      <h3>The Fab Ferret Emporium team</h3>
    </div>
  </body>
</html>
```

```csharp
@code {

  [Parameter]
  public string BaseUrl { get; set; } = "";

  [Parameter]
  public ContactModel Model { get; set; } = null!;

}
```

The `BaseUrl` parameter is populated by the template helper, and allows you to pull in images from your web site, as you can see above. The `Model` parameter is the model that you want to use to populate the template, and can be any class.

## Using the helper

With that in place, you can inject a `DocumentTemplateHelper` into your code, and use it as follows...

```csharp
// Generate HTML for use as an email body...
ContactModel model = new ContactModel { Name = "Billy Shears", Email = "billy@shears.co.uk" };
string html = await documentTemplateHelper
  .CreateHtmlFromTemplate<EmailFromContactPageTemplate>((nameof(EmailFromContactPageTemplate.Model), model));

// Generate PDF for attaching to an email...
InvoiceModel model = new InvoiceModel { /* set properties */ };
byte[] bytes = await documentTemplateHelper
  .CreatePdfFromTemplate<InvoiceTemplate>((nameof(InvoiceTemplate.Model), model));

// ...or with page settings
byte[] landscape = await documentTemplateHelper
  .CreatePdfFromTemplate<InvoiceTemplate>(new PdfOptions { Orientation = PdfOrientation.Landscape, PaperSize = PdfPaperSize.Letter, MarginMm = 15 },
    (nameof(InvoiceTemplate.Model), model));
```

`PdfOptions` defaults to portrait A4 with 10mm margins. Before v2.0.0, PDFs were always A4Plus (225mm x 320mm), so set `PaperSize = PdfPaperSize.A4Plus` if you relied on that.
