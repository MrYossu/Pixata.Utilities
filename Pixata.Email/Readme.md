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
    "UserName": "your.username",
    "Password": "your.password",
    "FromEmail": "jim@spriggs.com",
    "FromName": "Jim Spriggs"
  }
```

Unless your name is Jim Spriggs, you'll need to change these to you own name and server settings!

Then in your `Startup` class, add the following lines...

```c#
SmtpSettings smtpSettings = Configuration.GetSection("Smtp").Get<SmtpSettings>();
services.AddSingleton(smtpSettings);
services.AddTransient<PixataEmailService>();
```

This allows you to inject an instance of `PixataEmailService` into your code as usual, and it will have the settings baked in for you.

If you are into interfaces, you can tell it to inject one of those instead...

```c#
services.AddTransient<PixataEmailServiceInterface, PixataEmailService>();
```

## Usage

As with most of the code I write, this service uses the rather excellent [LanguageExt](https://github.com/louthy/language-ext/) Nuget package. This allows you to handle both the happy path and sad path with ease.

If you look at the code in [the sample page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/SendEmail.razor), you'll see that it (essentially) does this...

```c#
Msg = await _emailService.SendEmailAsync(new(TheContactModel.Subject, TheContactModel.Body, TheContactModel.Email, TheContactModel.Name))
  .Match(_ => "Success", ex => $"Exception: {ex.Message}");
```

`Msg` is a `string` variable that is displayed on the page. 

If you wanted to shorten that line, you could add a method to the `ContactModel` that returned an `EmailParameters` object, and pass that in to `SendEmailAsync` instead.

Obviously, as with any of the LanguageExt classes that supply a `Match` function, you can do much than just return a value...

```c#
await _emailService.SendEmailAsync(new(TheContactModel.Subject, TheContactModel.Body, TheContactModel.Email, TheContactModel.Name))
  .Match(_ => {
    _navMan.NavigateTo("/EmailSent");
  }, ex => {
    _logger.LogError($"Exception sending email: {ex.Message}");
    Msg = "Sorry, your email could not be sent";
  });
```