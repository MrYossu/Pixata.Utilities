# Generating documents and PDFs

>Part of [Pixata.AspNetCore](Readme.md)

I often find myself generating documents, either for conversion to PDF, or for emailing. This has always been a painful process, so I decided that a helper was needed. The `DocumentTemplateHelper` class contains two methods, one for generating HTML from a Blazor component, and another for generating a PDF from a Blazor component.

## Registering the services

The easiest way is to let the package do it...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>();
```

This registers `DocumentTemplateHelper` along with the `HtmlRenderer`, the `IHttpContextAccessor` and the wkhtmltopdf `IConverter` that it needs. See [registering services](Readme.md#registering-services) for how to opt out of the parts you don't want.

If you'd rather do it yourself, then you need the following in `Program.cs`...

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HtmlRenderer>();
builder.Services.AddScoped<DocumentTemplateHelper>();
```

...plus an `IConverter` of your own. Note that you only need this if you registered the services yourself. As of v1.9.0, `AddPixataAspNetCore` registers the `IHttpContextAccessor` for you.

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
```
