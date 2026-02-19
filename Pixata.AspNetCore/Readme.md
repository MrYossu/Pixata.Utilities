# Pixata.AspNetCore [![Pixata.AspNetCore Nuget package](https://img.shields.io/nuget/v/Pixata.AspNetCore)](https://www.nuget.org/packages/Pixata.AspNetCore/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.AspNetCore/Icon/ConnectionReseau.png "Pixata") 

## Registering dependencies
The validation extension in this package requires services to be registered in the DI container. To make this easier, you can use the `AddPixataAspNetCore` extension method in your `Program.cs` file:

```csharp
builder.Services.AddPixataAspNetCore();
```

Note: As the extension in this package is only designed to be used in server-side projects, you should only add this line in the `Program.cs` file of your server-side project. If you have a WASM project, adding a reference to this package will cause errors.

## ValidationEndpointFilter
When using fluent validation in Blazor server-side, the chances of anyone bypassing your validation are small enough that they can be ignored for most cases. However, when running in client-side (WASM), validation is handled in the WASM, and the data is then sent to the servier via API endpoints. This means that anyone can modify the request, or write a script to mimic it, and bypass your validation.

To avoid this, you can add the `ValidationEndpointFilter` to your API endpoints. This will run the same validation as in the client, but on the server, so if anyone tries to bypass the client-side validation, they will be stopped by the server-side validation. This allows you to protect your endpoints without adding much extra code.

Basic usage is very simple. First you need to add fluent validation, register the filter and tell it where to find your validators (in the server project's `Program.cs`)...

```csharp
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ContactModelValidator>();
builder.Services.AddSingleton<ValidationEndpointFilter>();
```

`ContactModelValidator` can be any model, it is used here to point the framework to the assembly containing your models.

Then you can use the `AddEndpointFilter` extension method to add the filter to any API endpoints that need validation...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model))
    .AddEndpointFilter(new ValidationEndpointFilter());
```

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