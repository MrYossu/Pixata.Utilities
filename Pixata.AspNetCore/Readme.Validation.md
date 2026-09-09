# Validation endpoint filter

>Part of [Pixata.AspNetCore](Readme.md)

When using FluentValidation in Blazor server-side, the chances of anyone bypassing your validation are small enough that they can be ignored for most cases. However, when running in client-side (WASM), validation is handled in the WASM, and the data is then sent to the server via API endpoints. This means that anyone can modify the request, or write a script to mimic it, and bypass your validation.

The obvious (and correct) solution to this is to validate your incoming models on the server before doing anything with the data. Generally, this is a bit of a pain, as it involves duplicating validation code.

To avoid this, you can add the `ValidationEndpointFilter` to your API endpoints. This will run the same validation as in the client, but on the server, so if anyone tries to bypass the client-side validation, they will be stopped by the server-side validation. This allows you to protect your endpoints without adding much extra code.

## Registering the services

The validation extension requires services to be registered in the DI container. To make this easier, you can use the `AddPixataAspNetCore` extension method in your `Program.cs` file...

```csharp
builder.Services.AddPixataAspNetCore<ContactModel>();
```

...where `ContactModel` is any model. It is used here to point the framework to the assembly containing your models. See [registering services](Readme.md#registering-services) for the other options.

## Basic usage

Once you've registered the dependencies, you just add the `AddValidationEndpointFilter` extension method to any API endpoints that need validation.

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

If there were any validation errors, then the filter will return an `ApiResponse` with a `State` of `ApiResponseStates.Failure` and a `Message` containing a formatted string of the validation errors. By default, the errors are formatted as a comma-delimited string of the form `$"{e.PropertyName}: {e.ErrorMessage}"`, which would produce something like `"Validation errors - Name: Required, Email: Invalid"`. This can be overridden as explained below.

## Options

The filter has two optional parameters.

You can pass in a `Func<ValidationFailure, string>` to format the validation errors. For example, if you wanted to include the error code in the message, you could do something like this...

```csharp
app.MapPost(RoutesHelper.ApiContact, async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model))
    .AddEndpointFilter(new ValidationEndpointFilter(err => $"({err.ErrorCode}) {err.ErrorMessage} for {err.PropertyName}"));
```

This would produce a message of the form `"Validation errors - (NotEmptyValidator) Required for Name, (EmailValidator) Invalid for Email"`.

By default, the filter will pick up a validator for any class in the assembly you specified when registering (with the `AddValidatorsFromAssemblyContaining` method, see above). You may wish to restrict this further, and specify that only classes within a certain namespace should be validated. You can do this as follows...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model))
    .AddEndpointFilter(new ValidationEndpointFilter(nameSpace: typeof(ContactModel).Namespace));
```

This is not a common scenario, but is included for those odd cases.
