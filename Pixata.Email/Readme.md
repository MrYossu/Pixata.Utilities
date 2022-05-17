# Pixata.Email [![Pixata.Email Nuget package](https://img.shields.io/nuget/v/Pixata.Email)](https://www.nuget.org/packages/Pixata.Email/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Email/Icon/mail.png "Pixata") 

An email service for use in .NET Core projects. Backed by Mailkit, it eases the effort needed to add email facilities to your project.

A [Nuget package](https://www.nuget.org/packages/Pixata.Email/) is available for this project.

## Setup

First thing you need to do is add your SMTP server and "From" details to appSettings.json...

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

// .NET6 - Goes in Program.cs
SmtpSettings smtpSettings = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>();
builder.Services.AddSingleton(smtpSettings);
builder.Services.AddTransient<PixataEmailService>();
```

This allows you to inject an instance of `PixataEmailService` into your code as usual, and it will have the settings baked in for you.

If you are into interfaces, you can tell it to inject one of those instead...

```c#
services.AddTransient<PixataEmailServiceInterface, PixataEmailService>();
```

## Usage

As with most of the code I write, this service uses the rather excellent [LanguageExt](https://github.com/louthy/language-ext/) Nuget package. This allows you to handle both the happy path and sad path with ease.

There are two overloads of the `SendEmail` method. Easiest to use is a simple one that just takes the recipient's email address, the subject and the HTML body...

```c#
await _emailService.SendEmailAsync("billy@shears.com", "Hello from Jim Spriggs", htmlBody))
  .Match(_ => /* code on success */, ex => /* code on failure */);
```

If you want more control over what is sent and how, the second overload takes an `EmailParameters` object. The various constructors allow you to specify more detail, as well as adding multiple recipients. You can also add attachments, which are tuples of the form `(string FileName, string MimeType, byte[] Data)`.

See [the `EmailParameters` code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Email/EmailParameters.cs) for more details.