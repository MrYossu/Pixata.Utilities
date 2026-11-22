# Validation

>Part of [Pixata.Blazor](Readme.md)

## Satisfies, a safe replacement for FluentValidation's Must and Custom

### The problem

If a Blazor WebAssembly app validates its forms with [FluentValidation](https://docs.fluentvalidation.net/) (through [Blazilla](https://github.com/loresoft/Blazilla) or similar), **don't use FluentValidation's `Must` or `Custom` in any validator the client uses.**

In a published (Release) WebAssembly app, just constructing a validator that uses either of them stops the .NET runtime dead. The browser's console shows a Mono assertion, with the details stripped out of the Release runtime...

```
[MONO] * Assertion at /__w/1/s/src/runtime/src/mono/mono/metadata/loader.c:1826, condition `<disabled>' not met
```

As the validator is created when the form is first drawn, and nothing runs after the assertion, the page just sits on its loading indicator. There are no failed requests in the network tab, and no other errors in the console.

What makes this so easy to miss is that it only happens in a Release build. Running the app locally (a Debug build) works perfectly, so you only find out once the site has been deployed.

This was found with FluentValidation 12.2 on .NET 10.0.14. Testing showed that...

- Every overload of `Must`, on properties of any type, and `Custom` crash it
- FluentValidation's built-in rules (`NotEmpty`, `NotEqual`, `GreaterThan`, `EmailAddress`, `Matches` and so on), `When`, `Unless`, `WithMessage` and rule sets are all fine
- It isn't caused by trimming. The app crashes the same way with trimming turned off
- A rule written as a class of its own, deriving from FluentValidation's `PropertyValidator<T, TProperty>`, works

The exact cause inside the runtime isn't known, as the Release runtime doesn't say which method it failed on.

### The fix

`Satisfies` (in the `Pixata.Blazor.Validation` namespace) does the same job as `Must`, but is a validator class of its own, so it works in a published app. It has two overloads, one taking the property's value, and one taking the object being validated as well...

```csharp
using Pixata.Blazor.Validation;

public class PosterDtoValidator : AbstractValidator<PosterDto> {
  public PosterDtoValidator() {
    // In place of .Must(id => id > 0)
    RuleFor(p => p.SedraId).Satisfies(id => id > 0).WithMessage("Sedra is required");
    // In place of .Must((poster, start) => start < poster.EndDate)
    RuleFor(p => p.StartDate).Satisfies((poster, start) => start < poster.EndDate).WithMessage("Start date must be before end date");
    // Works with When, Unless and the other rule options, as Must does
    RuleFor(p => p.Image).Satisfies(image => image.Length > 0).When(p => p.Id == 0).WithMessage("Image is required");
  }
}
```

Without a `WithMessage`, the message is the same as `Must`'s: "The specified condition was not met for '{PropertyName}'".

To replace `Custom`, which can add different messages for different failures, use `Satisfies` with a `WithMessage` that works out the message. For example, with a method that returns what's wrong with a telephone number, or `""` if nothing is...

```csharp
RuleFor(c => c.Telephone)
  .Satisfies(telephone => TelephoneError(telephone) == "")
  .WithMessage((_, telephone) => TelephoneError(telephone));
```

### Checking a form

As the crash only happens in a Release build, check a form that has a validator by publishing the app and running the published copy...

```
dotnet publish MyProject.Server -c Release -o publish
```

Then run `publish\MyProject.Server.exe` and open the form. If the page sits on its loading indicator, look in the browser's console for the assertion.
