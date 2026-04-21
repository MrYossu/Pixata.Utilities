# Pixata.AspNetCore [![Pixata.AspNetCore Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore)](https://www.nuget.org/packages/Pixata.AspNetCore/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.AspNetCore/ConnectionReseau.png "Pixata") 

## Important
As the validation extension in this package is only designed to be used in server-side projects, you should reference this package in a server-side project. If you have a WASM project, adding a reference to this package will cause errors.

## DocumentTemplateHelper
I often find myuself generating documents, either for conversion to PDF, or for emailing. This has always been a painful process, so I decided that a helper was needed. This class contains two methods, one for generating HTML from a Blazor component, and another for generating a PDF from a Blazor component.

First you need to register a Microsoft dependency and the Pixata template helper in `Program.cs`...

```csharp
builder.Services.AddScoped<HtmlRenderer>();
builder.Services.AddScoped<DocumentTemplateHelper>();
```

Then, you create a Blazor component that will be the template for the document you wish to generate. It needs to accept two parameters as follows...

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

## RequestLoggingMiddleware
When writing API endpoints, it can be hard to debug 400 errors, which are often caused by incorrect or mismatched paths, or invalid data in the request. You often don't get much clue as to what actually happened.

To help with this, I have added a piece of middleware that will log every incoming request to the app (subject to configuration choices, see below). This will log the path, the query string, and the body of the request. This can be very helpful for debugging, as it allows you to see exactly what was sent to the server.

You need to register the middleware in your `Program.cs` file, as follows...
```csharp
builder.Services.AddRequestLogging();
```

This will use the default configuration, which is to include the request body in the logging, and ignore any requests whose path starts with any of...

- "_framework"
- "_blazor"
- "_content"
- ".well-known"

You can override either of these options as follows...
```csharp
builder.Services.AddRequestLogging(o => {
  o.IgnoredPaths = ["_framework", "health"]; // Or whatever you want to ignore
  o.LogBody = false;
});
```

To log all requests, set `o.IgnoredPaths` to an empty array `[]`.

You then need to register the middleware...
```csharp
app.UseRequestLogging();
```

This should be after any authentication/authorisation middleware, but before any endpoint mapping.

## ValidationEndpointFilter
When using fluent validation in Blazor server-side, the chances of anyone bypassing your validation are small enough that they can be ignored for most cases. However, when running in client-side (WASM), validation is handled in the WASM, and the data is then sent to the server via API endpoints. This means that anyone can modify the request, or write a script to mimic it, and bypass your validation.

The obvious (and correct) solution to this is to validate your incoming models on the server before doing anything with the data. Generally, this is a bit of a pain, as it involves duplicating validation code.

To avoid this, you can add the `ValidationEndpointFilter` to your API endpoints. This will run the same validation as in the client, but on the server, so if anyone tries to bypass the client-side validation, they will be stopped by the server-side validation. This allows you to protect your endpoints without adding much extra code.

The validation extension requires services to be registered in the DI container. To make this easier, you can use the `AddPixataAspNetCore` extension method in your `Program.cs` file:

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>();
```

...where `ContactModel` is any model, it is used here to point the framework to the assembly containing your models.

Basic usage is very simple. Once you've registered the dependencies (see above), you just add the `AddEndpointFilter` extension method to any API endpoints that need validation.

You change...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model));
```

...to...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model))
    .AddValidationEndpointFilter();
```

When the endpoint is hit, the appropriate validator will be found and applied to the incoming model.

If there were any validation errors, then the filter will return an `ApiResponse` with a `State` of `ApiResponseStates.Failure` and a `Message` containing a formatted string of the validation errors. By default, the errors are formatted as a comma-delimited string of the form `$"{e.PropertyName}: {e.ErrorMessage}"`, which would produce something like... `"Validation errors - Name: Required, Email: Invalid"`. This can be overriden as explained below.

The filter has two optional parameters.

You can pass in a `Func<ValidationFailure, string>` to format the validation errors. For example, if you wanted to include the error code in the message, you could do something like this...


```csharp
app.MapPost(RoutesHelper.ApiContact, async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model))
    .AddEndpointFilter(new ValidationEndpointFilter(err => $"({err.ErrorCode}) {err.ErrorMessage} for {err.PropertyName}"));
```

This would produce a message of the form `"Validation errors - (NotEmptyValidator) Required for Name, (EmailValidator) Invalid for Email"`.

By default, the filter will pick up any a validator for any class in the assembly you specified when registering (with the `AddValidatorsFromAssemblyContaining` method, see above). You may wish to restrict this further, and specify that only classes within a certain namepsace should be validated. You can do this as follows...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model)).AddEndpointFilter(new ValidationEndpointFilter(nameSpace: typeof(ContactModel).Namespace);
```

This is not a common scenario, but is included for those odd cases.