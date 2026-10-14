# Pixata.Email [![Pixata.Email Nuget package](https://img.shields.io/nuget/v/Pixata.Email)](https://www.nuget.org/packages/Pixata.Email/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Email/mail.png "Pixata")

An email service for use in .NET Core projects. Backed by Mailkit, it eases the effort needed to add email facilities to your project.

A [Nuget package](https://www.nuget.org/packages/Pixata.Email/) is available for this project.

# Breaking changes

>**Version 3.0.0** changes how the connection to the SMTP server is secured. `SmtpSettings` has a new `SocketOptions` property (a MailKit `SecureSocketOptions`), and `UseSsl` is now a `bool?` marked `[Obsolete]`. If you set neither, the service uses implicit TLS on port 465 and *required* STARTTLS on any other port, where it used to try implicit TLS everywhere (which failed on port 587). See [securing the connection](#securing-the-connection) below.

>As from version 2.0.0, this package does not use LanguageExt, but returns an `ApiResponse` from the [Pixata.Extensions package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions) to indicate success or failure. See the comments at the bottom of this document for how this will change your code.

## Setup

First thing you need to do is add your SMTP server and "From" details to your application. There are a few ways to do this, the most simple of which is to use `appSettings.json`...

```json
  "Smtp": {
    "Server": "your.smtp.server",
    "Port": 999,
    "SocketOptions": "StartTls", // Optional, see below
    "UserName": "your.username",
    "Password": "your.password",
    "FromEmail": "jim@spriggs.com",
    "FromName": "Jim Spriggs",
    "ReplyTo": "jim@spriggs.com" // Optional
  }
```

Unless your name is Jim Spriggs, you'll need to change these to you own name and server settings!

### Securing the connection

`SocketOptions` takes any member of MailKit's `SecureSocketOptions` enum (`None`, `Auto`, `SslOnConnect`, `StartTls` or `StartTlsWhenAvailable`). If you leave it out, the service picks one from the port...

| Port | Default |
| --- | --- |
| 465 | `SslOnConnect` (implicit TLS) |
| Anything else (eg 587, the standard submission port used by Microsoft 365, Gmail and most other providers) | `StartTls` (required, so a man in the middle can't strip the encryption and see your credentials) |

If you need an unencrypted connection (a local test server such as Papercut or smtp4dev, for example), set `"SocketOptions": "None"` explicitly.

Prior to version 3.0.0, the connection was controlled by a `UseSsl` flag, which MailKit maps to `SslOnConnect` (true) or `StartTlsWhenAvailable` (false). That meant `true` failed with a handshake error on port 587, and `false` only used STARTTLS if the server offered it. `UseSsl` still works, but is marked `[Obsolete]`, and is ignored if you set `SocketOptions`. If you read `UseSsl` in your own code, note that it is now a `bool?`, and is null unless you set it.

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

There are three overloads of the `SendEmailAsync` method. Easiest to use is a simple one that just takes the recipient's email address, the subject and the HTML body...

```c#
(await _emailService.SendEmailAsync("billy@shears.com", "Hello from Jim Spriggs", htmlBody)))
  .Match(_ => /* code on success */, ex => /* code on failure */);
```

If you want more control over what is sent and how, the second overload takes an `EmailParameters` object. The various constructors allow you to specify more detail, as well as adding multiple recipients. You can also add attachments, which are tuples of the form `(string FileName, string MimeType, byte[] Data)`.

To copy the email to other people, add them to `Cc` or `Bcc` (both `List<MailboxAddress>`, empty by default), or use the `AddCc` and `AddBcc` methods, which parse the addresses the same way as the constructors do...

```c#
EmailParameters parameters = new EmailParameters("Your invoice", htmlBody, "billy@shears.com", "Billy Shears")
  .AddCc("accounts@shears.com")
  .AddBcc("jim@spriggs.com"); // Send me a copy
```

BCC recipients get the email, but don't appear in the headers that the other recipients see. CC and BCC were added in version 3.0.0.

See [the `EmailParameters` code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Email/EmailParameters.cs) for more details.

The third overload takes an `EmailParameters` and a MailKit `SecureSocketOptions`, which overrides the one from your settings for this email only...

```c#
await _emailService.SendEmailAsync(emailParameters, SecureSocketOptions.StartTls);
```

The other two overloads connect using the socket options from your settings (see [securing the connection](#securing-the-connection)), so you rarely need this one.

All three return an `ApiResponse<Yunit>`, so the failure message is the exception's message. The two overloads that use your settings prefix it with the exception type (eg `"(AuthenticationException) Authentication failed"`), as that is usually the useful part when a send fails.

## Breaking change in version 2.0.0

As from version 2.0.0, the service uses an `ApiResponse` from the [Pixata.Extensions package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Extensions) to indicate success or failure. Previous versions used LanguageExt, which has now been removed from this package.

If you are upgrading from a LanguageExt version, then you will need to change the code in the failure lambda. The LanguageExt version passed in a `System.Exception`, whereas the new version passes a `string` containing the error message. For example, you would change...

```c#
(await _emailService.SendEmailAsync("billy@shears.com", "Hello from Jim Spriggs", htmlBody)))
  // Note that ex is an Exception, so we need to use the Message property to get the details
  .Match(_ => /* code on success */, ex => Console.WriteLine(ex.Message));
```

...to...

```c#
(await _emailService.SendEmailAsync("billy@shears.com", "Hello from Jim Spriggs", htmlBody)))
  // Note that we use ex directly, as it is a string, not an Exception, so we don't have a Message property
  .Match(_ => /* code on success */, ex => Console.WriteLine(ex));
```

You will need also to make sure you wrap the first line in brackets (as shown above). This was not necessary before.

If your code captures the return value from `SendEmailAsync` in a local variable, then you will need to add a `using` statement for `Pixata.Email` and change the type of the variable from `TryAsync<Unit>` to `ApiResponse<Yunit>` (unless you use `var` in which case the compiler will correctly infer the return type). However, this is not a common pattern when using this service.
