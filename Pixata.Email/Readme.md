# Pixata.Email [![Pixata.Email Nuget package](https://img.shields.io/nuget/v/Pixata.Email)](https://www.nuget.org/packages/Pixata.Email/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Email/Icon/mail.png "Pixata") 

An email service for use in .NET Core projects. Backed by Mailkit, it eases the effort needed to add email facilities to your project.

A [Nuget package](https://www.nuget.org/packages/Pixata.Email/) is available for this project.

# Breaking change
>As from version 2.0.0, this package does not use LanguageExt, but returns an `ApiResponse` from the [Pixata.Extensions package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions) to indicate success or failure. In most cases, the only change you'll need to make to your code is to add brackets around the `await _emailService.SendEmailAsync(...)` line, see the usage section below.
>
>If your code captures the return value from `SendEmailAsync` in a local variable, then you will need to add a `using` statement for `Pixata.Email` and change the type of the variable from `TryAsync<Unit>` to `ApiResponse<Yunit>` (unless you use `var` in which case the compiler will correctly infer the return type).

## Setup

First thing you need to do is add your SMTP server and "From" details to your application. There are a few ways to do this, the most simple of which is to use `appSettings.json`...

```json
  "Smtp": {
    "Server": "your.smtp.server",
    "Port": 999,
    "UseSsl": "true",
    "UserName": "your.username",
    "Password": "your.password",
    "FromEmail": "jim@spriggs.com",
    "FromName": "Jim Spriggs"
  }
```

Unless your name is Jim Spriggs, you'll need to change these to you own name and server settings!

Then add the following lines...

```c#
// .NET5 - Goes in Startup.cs
SmtpSettings smtpSettings = Configuration.GetSection("Smtp").Get<SmtpSettings>();
services.AddSingleton(smtpSettings);
services.AddTransient<PixataEmailService>();

// .NET6+ - Goes in Program.cs
SmtpSettings smtpSettings = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>();
builder.Services.AddSingleton(smtpSettings);
builder.Services.AddTransient<PixataEmailService>();
```

This allows you to inject an instance of `PixataEmailService` into your code as usual, and it will have the settings baked in for you.

If you store your mail settings in secrets, environmental variables, etc, then you'll need to retrieve them from there and set up an instance of `SmtpSettings` from that.

If you are into interfaces, you can register that instead...

```c#
services.AddTransient<PixataEmailServiceInterface, PixataEmailService>();
```

### Allowing multiple email servers or accounts
>Note that this feature is only available from v1.2.4 onwards

If you need to be able to send emails from multiple servers or accounts, you can set up a default `SmtpSettings` as above, and then when you want to send from a different server or account, just change the `SmtpSettings` property on the service to use the appropriate settings...

```c#
SmtpSettings myStmpSettings = // Pick them up from wherever you store them
_emailService.SmtpSettings = myStmpSettings;
```

## Usage

As from version 2.0.0, the service uses an `ApiResponse` from the [Pixata.Extensions package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions) to indicate success or failure. Previous versions used LanguageExt, which has now been removed from this package.

>If you are upgrading from a LanguageExt version, and are using code like that shown below, then you will need to wrap the first line in brackets (as shown below). Other than that, the code should work as before.

There are two overloads of the `SendEmail` method. Easiest to use is a simple one that just takes the recipient's email address, the subject and the HTML body...

```c#
(await _emailService.SendEmailAsync("billy@shears.com", "Hello from Jim Spriggs", htmlBody)))
  .Match(_ => /* code on success */, ex => /* code on failure */);
```

If you want more control over what is sent and how, the second overload takes an `EmailParameters` object. The various constructors allow you to specify more detail, as well as adding multiple recipients. You can also add attachments, which are tuples of the form `(string FileName, string MimeType, byte[] Data)`.

See [the `EmailParameters` code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Email/EmailParameters.cs) for more details.