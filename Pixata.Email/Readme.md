# Pixata.Email [![Pixata.Email Nuget package](https://img.shields.io/nuget/v/Pixata.Email)](https://www.nuget.org/packages/Pixata.Email/)

![Pixata](https://raw.githubusercontent.com/MrYossu/Pixata.Utilities/master/Pixata.Email/mail.png "Pixata")

An email service for use in .NET Core projects. Backed by Mailkit, it eases the effort needed to add email facilities to your project.

A [Nuget package](https://www.nuget.org/packages/Pixata.Email/) is available for this project.

>**Version 3.4.0** adds `PixataGraphMailReader`, which reads the messages in a Microsoft 365 mailbox or shared mailbox with Microsoft Graph, such as the invoices that suppliers email to a shared address. It gets the messages in a folder, oldest first (optionally only unread ones, or those since a date), their attachments as streams, and can mark a message as read or move it to a folder such as "Processed". See [reading a mailbox with Microsoft Graph](#reading-a-mailbox-with-microsoft-graph) below (#142). `PixataGraphEmailService` can now also send attachments over 3MB, up to Microsoft 365's limit for the mailbox. An email too big for `sendMail` is created as a draft, its attachments are uploaded, and the draft is sent, which needs the `Mail.ReadWrite` permission as well as `Mail.Send`. Smaller emails are sent as before. See [sending with Microsoft Graph](#sending-with-microsoft-graph) below (#154)

>**Version 3.3.0** can send through Microsoft 365 with OAuth, as Microsoft is retiring user name and password logins for SMTP. `PixataGraphEmailService` sends with Microsoft Graph, which works even where SMTP logins are turned off for the mailbox, and saves the emails in Sent Items. `SmtpSettings.AccessToken` logs in to an SMTP server with OAuth (XOAUTH2) instead of the password, for Microsoft 365 or Gmail. Both services implement the new `PixataEmailSenderInterface`, so your app can choose between them. See [Microsoft 365, Gmail and OAuth](#microsoft-365-gmail-and-oauth) below (#128)

>**Version 3.2.0** works with SMTP servers that don't need a login, such as an internal relay that allows your app's IP address, or a local test server such as smtp4dev or Papercut. The service no longer logs in when `UserName` is empty, or when the server doesn't offer logins (which used to fail with "The SMTP server does not support authentication"). See [servers that don't need a login](#servers-that-dont-need-a-login) below (#127)

>**Version 3.1.0** adds `SendEmailsAsync`, which sends several emails over one connection to the SMTP server, which is much quicker than sending them one by one. See [sending several emails at once](#sending-several-emails-at-once) below.

>**Version 3.0.1** references a patched version of `System.Security.Cryptography.Pkcs`, as the one pulled in transitively had known vulnerabilities ([GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx), [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7)). No code changes.

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
    "UserName": "your.username", // Optional, see below
    "Password": "your.password", // Optional, see below
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

### Servers that don't need a login

>Note that this feature is only available from v3.2.0 onwards

Some SMTP servers don't take a user name and password, such as an internal relay that accepts email from your app's IP address, or a local test server such as [smtp4dev](https://github.com/rnwood/smtp4dev) or [Papercut](https://github.com/ChangemakerStudios/Papercut-SMTP). For those, leave out `UserName` and `Password` (or set them to `""`)...

```json
  "Smtp": {
    "Server": "localhost",
    "Port": 25,
    "SocketOptions": "None", // A local test server doesn't usually have TLS
    "FromEmail": "jim@spriggs.com",
    "FromName": "Jim Spriggs"
  }
```

The service only logs in when `UserName` has a value *and* the server offers logins (it lists AUTH in its reply to EHLO). So if your users enter their own mail server's settings, a user name for a server that doesn't take one doesn't stop the email being sent. If the server does need a login and you didn't give one, it rejects the email, and the failure message is the server's reason, eg `"(SmtpCommandException) 5.7.1 Client was not authenticated"`.

Note that a server that only offers logins over an encrypted connection won't offer them if `SocketOptions` is `None`, so the service won't log in. Use `StartTls` or `SslOnConnect` (the defaults) for those.

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

`PixataEmailSenderInterface` (from v3.3.0) has just the methods that don't depend on how the email is sent, and is implemented by both `PixataEmailService` and [`PixataGraphEmailService`](#sending-with-microsoft-graph), so use that one if your app can send either way.

### Allowing multiple email servers or accounts

>Note that this feature is only available from v1.2.4 onwards

If you need to be able to send emails from multiple servers or accounts, you can set up a default `SmtpSettings` as above, and then when you want to send from a different server or account, just change the `SmtpSettings` property on the service to use the appropriate settings...

```c#
SmtpSettings myStmpSettings = // Pick them up from wherever you store them
_emailService.SmtpSettings = myStmpSettings;
```

## Usage

There are three overloads of the `SendEmailAsync` method, plus `SendEmailsAsync` for [sending several emails at once](#sending-several-emails-at-once). Easiest to use is a simple one that just takes the recipient's email address, the subject and the HTML body...

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

### Sending several emails at once

>Note that this feature is only available from v3.1.0 onwards

Each `SendEmailAsync` overload opens a new connection to the SMTP server, secures it, logs in, sends the email and disconnects. That's fine for one email, but if you're sending a batch (statements to a list of customers, for example), the connecting and logging in can take longer than the sending. `SendEmailsAsync` takes a collection of `EmailParameters`, and sends them all over one connection...

```c#
List<EmailParameters> emails = customers
  .Select(c => new EmailParameters("Your statement", StatementBody(c), c.Email, c.Name))
  .ToList();
List<ApiResponse<Yunit>> responses = await _emailService.SendEmailsAsync(emails);
```

You get back one `ApiResponse<Yunit>` per email, in the same order as the emails you passed in, so you can match them up to see which ones failed...

```c#
foreach ((EmailParameters email, ApiResponse<Yunit> response) in emails.Zip(responses)) {
  response.Match(_ => { }, error => Console.WriteLine($"Couldn't send to {email.Recipients.First()}: {error}"));
}
```

A few things to note...

- An email that fails doesn't stop the rest being sent. If the server dropped the connection when it failed, the service connects again for the next email.
- If the service can't connect or log in at all, every email fails with that error. As it tries again for each one, this can take a while if the server isn't responding.
- It uses the socket options from your settings, and prefixes the failure messages with the exception type, as `SendEmailAsync(EmailParameters)` does.
- The emails are sent one after the other, not in parallel, so this won't exceed the sending rate of providers that limit it, such as Amazon SES. It's quicker because each email doesn't need its own connection, not because the emails are sent any faster.

It's a separate method, rather than another `SendEmailAsync` overload, because an overload taking a collection would make existing calls such as `SendEmailAsync(new("Hello", body, "billy@shears.com"))` ambiguous.

## Microsoft 365, Gmail and OAuth

>Note that this feature is only available from v3.3.0 onwards

Microsoft is [retiring basic authentication for SMTP AUTH](https://techcommunity.microsoft.com/blog/exchange/exchange-online-to-retire-basic-auth-for-client-submission-smtp/4114750) (logging in to `smtp.office365.com` with a user name and password) in Exchange Online, so a `UserName` and `Password` will stop working for Microsoft 365 mailboxes. Pixata.Email has two ways to log in with an OAuth access token instead...

| | [Microsoft Graph](#sending-with-microsoft-graph) | [SMTP with OAuth](#logging-in-to-smtp-with-oauth) |
| --- | --- | --- |
| Use | `PixataGraphEmailService` | `PixataEmailService`, with `SmtpSettings.AccessToken` |
| Works with | Microsoft 365 | Microsoft 365, Gmail, and any other server that takes XOAUTH2 |
| Needs SMTP AUTH turned on for the mailbox | No | Yes, which many Microsoft 365 tenants turn off |
| Saves the email in Sent Items | Yes (you can turn this off) | No |
| Largest email | The mailbox's limit (35MB for Microsoft 365 unless an administrator changes it). Over about 3MB of attachments needs `Mail.ReadWrite` as well | The server's limit (eg 35MB for Microsoft 365) |

For Microsoft 365, Graph is usually the better choice.

Either way, getting the token is up to your app. That means having the user sign in and consent (or an administrator consent for the whole organisation), keeping the refresh token, and getting a new access token when the old one expires, which is after about an hour. [MSAL](https://learn.microsoft.com/entra/msal/dotnet/) (`Microsoft.Identity.Client`) or [Microsoft.Identity.Web](https://learn.microsoft.com/entra/msal/dotnet/microsoft-identity-web/) will do this for you. Pixata.Email just takes the token.

### Sending with Microsoft Graph

`PixataGraphEmailService` sends with Graph's [`sendMail`](https://learn.microsoft.com/graph/api/user-sendmail). It needs a token for Microsoft Graph with the `Mail.Send` permission, which can be either...

- **Delegated**, so the app sends as a user who has signed in and consented. Ask for `Mail.Send` and `offline_access` (for a refresh token, so the app can carry on sending after the user has gone). The email is sent from that user's mailbox.
- **Application**, so the app sends as itself, from any mailbox in the organisation, once an administrator has consented. Ask for `https://graph.microsoft.com/.default` with the client credentials flow, and set `UserId` to the mailbox to send from. You can limit which mailboxes the app can use with an [application access policy](https://learn.microsoft.com/graph/auth-limit-mailbox-access).

Register it with an `HttpClient`...

```c#
builder.Services.AddHttpClient<PixataGraphEmailService>();
```

...and give it the token before you send...

```c#
string token = await _tokens.GetGraphToken(company); // Your code, eg with MSAL
_graphEmailService.Settings = new() { AccessToken = token };
ApiResponse<Yunit> response = await _graphEmailService.SendEmailAsync(emailParameters);
if (response.State == ApiResponseStates.Unauthorised) {
  // The token has expired or been revoked (or doesn't have permission, see the message), so get a new one, or ask the user to connect their
  // mailbox again
}
```

You can also pass a `GraphEmailSettings` to the constructor, or register one, but as the token changes, it's usually easiest to set `Settings` before sending. Set it to a new `GraphEmailSettings`, as above, rather than changing the properties of one that other users of your app might share.

| `GraphEmailSettings` | Default | What it does |
| --- | --- | --- |
| `AccessToken` | `""` | The access token. Without one, sending fails without calling Graph |
| `UserId` | `""` | The mailbox to send from, as the user's id or user principal name (usually their email address), for an application token. Leave empty to send from the signed-in user's mailbox (`/me`) |
| `SaveToSentItems` | `true` | Whether a copy of each email is saved in Sent Items |
| `BaseUrl` | `https://graph.microsoft.com/v1.0/` | Only change this for a [national cloud](https://learn.microsoft.com/graph/deployments) |

It takes the same `EmailParameters` as `PixataEmailService`, and has the same `SendEmailAsync` and `SendEmailsAsync` methods (but not the overload that takes `SecureSocketOptions`, which only means anything for SMTP). A few things are different...

- **The email is from the mailbox**, so there are no `FromEmail` and `FromName` settings. If you set `EmailParameters.From`, the mailbox needs permission to send as (or on behalf of) that address.
- **Large attachments need the `Mail.ReadWrite` permission.** `sendMail` takes requests of up to 4MB, which is about 3MB of attachments, as they are base64 encoded. An email bigger than that is created as a draft in the mailbox, each attachment is added (through an [upload session](https://learn.microsoft.com/graph/outlook-large-attachments), in chunks, if it's 3MB or more), and the draft is sent. That needs `Mail.ReadWrite` (delegated or application, as for `Mail.Send`) to create the draft. Without it, sending a large email gives `Unauthorised`, with a message that says so. If anything fails along the way, the draft is deleted. Each attachment can be up to 150MB, but the whole email must be within the mailbox's limit, which is 35MB by default in Microsoft 365. With `SaveToSentItems` turned off, the draft is marked to be deleted once it's sent (with the `PidTagDeleteAfterSubmit` property), as sending a draft otherwise always saves it.
- **Failures have more specific states.** A token that has expired or been revoked (401), or that doesn't allow the email to be sent (403, eg one without `Mail.Send`, or a `From` address the mailbox can't send as), gives `Unauthorised`, Graph throttling the app or being busy (429 or 503) gives `ServiceUnavailable`, and not being able to reach Graph gives `HttpFailure`. Anything else is a `Failure`. The message is Graph's, with the status and its error code, eg `"(401 InvalidAuthenticationToken) Lifetime validation failed, the token is expired."`.
- **`SendEmailsAsync` sends one request per email**, as Graph takes them separately, so it's no quicker than sending them one by one. It's there so that you can swap between the two services.

### Logging in to SMTP with OAuth

To log in to an SMTP server with OAuth (XOAUTH2) instead of a password, set `AccessToken` in the `SmtpSettings`, with `UserName` the mailbox's email address. `Password` is ignored when there's a token...

```c#
_emailService.SmtpSettings = new() {
  Server = "smtp.office365.com",
  Port = 587,
  UserName = "jim@spriggs.com",
  AccessToken = token, // Your code, eg with MSAL
  FromEmail = "jim@spriggs.com",
  FromName = "Jim Spriggs"
};
```

The token needs the right scope for the server...

| Server | Scope |
| --- | --- |
| Microsoft 365 (`smtp.office365.com`) | `https://outlook.office.com/SMTP.Send` (with `offline_access` for a refresh token). The mailbox also needs [SMTP AUTH turned on](https://learn.microsoft.com/exchange/clients-and-mobile-in-exchange-online/authenticated-client-smtp-submission) |
| Gmail (`smtp.gmail.com`) | `https://mail.google.com/` |

If the token has expired, the server refuses the login, and the failure message is an `AuthenticationException` with the server's reason. Get a new token and send again.

### Choosing between them

If your app lets each customer use their own mail server or their own Microsoft 365 mailbox, use `PixataEmailSenderInterface`, which both services implement...

```c#
PixataEmailSenderInterface sender = company.UsesMicrosoft365
  ? new PixataGraphEmailService(_httpClientFactory.CreateClient(), new() { AccessToken = await _tokens.GetGraphToken(company) })
  : new PixataEmailService(company.SmtpSettings);
ApiResponse<Yunit> response = await sender.SendEmailAsync(emailParameters);
```

### Reading a mailbox with Microsoft Graph

`PixataGraphMailReader` reads the messages in a Microsoft 365 mailbox, eg to take in the invoices that suppliers email to a shared address. It takes the same `GraphEmailSettings` as `PixataGraphEmailService`, and getting the token is up to your app in the same way. The token needs...

- `Mail.Read` to read the messages and their attachments, or `Mail.ReadWrite` to mark them as read, move them, or create folders.
- For a shared mailbox with a **delegated** token, `Mail.Read.Shared` (or `Mail.ReadWrite.Shared`), with `UserId` set to the shared mailbox, which the user needs access to.
- With an **application** token (client credentials), `Mail.Read` or `Mail.ReadWrite` as application permissions, and `UserId` set to the mailbox. As that lets the app read every mailbox in the organisation, limit it to the ones it needs with an [application access policy](https://learn.microsoft.com/graph/auth-limit-mailbox-access).

Register it with an `HttpClient`...

```c#
builder.Services.AddHttpClient<PixataGraphMailReader>();
```

...then give it the token, and read the messages. This takes in the unread messages in the shared mailbox's inbox, saves their PDFs, and moves each one to a "Processed" folder, so it isn't picked up again...

```c#
_mailReader.Settings = new() { AccessToken = token, UserId = "invoices@spriggs.com" };
ApiResponse<string> processed = await _mailReader.GetFolderId("Inbox/Processed", create: true);
ApiResponse<List<ReceivedEmail>> messages = await _mailReader.GetMessages(new() { UnreadOnly = true });
foreach (ReceivedEmail message in messages.Data ?? []) {
  ApiResponse<List<ReceivedAttachment>> attachments = await _mailReader.GetAttachments(message.Id);
  foreach (ReceivedAttachment attachment in (attachments.Data ?? []).Where(a => a.ContentType == "application/pdf")) {
    ApiResponse<Stream> content = await _mailReader.OpenAttachment(message.Id, attachment.Id);
    if (content) {
      await using Stream pdf = content.Data!;
      await _invoices.Import(message.From, attachment.Name, pdf); // Your code
    }
  }
  await _mailReader.Move(message.Id, processed.Data!);
}
```

| Method | What it does |
| --- | --- |
| `GetMessages(query)` | Gets the messages in a folder, oldest first. See `GraphMailQuery` below |
| `GetAttachments(messageId, includeInline)` | Gets the details of a message's attachments (`Id`, `Name`, `ContentType`, `Size`, `IsInline` and `Kind`), without their content. Inline ones, such as images in a signature, are left out unless you ask for them |
| `OpenAttachment(messageId, attachmentId)` | Opens an attachment's content as a stream, read from Graph as you read it, so a large attachment isn't held in memory. Dispose of it when you've finished. For an attached email (`Kind` is `Item`), the content is the email as a .eml file. A link to a file in OneDrive or SharePoint (`Kind` is `Reference`) has no content |
| `MarkAsRead(messageId, isRead)` | Marks a message as read (or unread, with `false`) |
| `Move(messageId, folder)` | Moves a message to a folder, given its id or a well-known name such as `archive` or `deleteditems`. A message's id changes when it's moved, so this returns the new one |
| `GetFolderId(path, create)` | Gets a folder's id from its path, such as "Inbox/Processed". The first part can be a well-known name, such as `inbox`, which works whatever the mailbox's language. With `create: true`, it creates the folders that don't exist |

| `GraphMailQuery` | Default | What it does |
| --- | --- | --- |
| `Folder` | `inbox` | The folder to read, as a well-known name or a folder's id (use `GetFolderId()` to get one from its path) |
| `UnreadOnly` | `false` | Only gets messages that haven't been read |
| `Since` | `null` | Only gets messages received at or after this time |
| `MaxCount` | `100` | The most messages to get. As they're oldest first, you get the rest next time, once you've dealt with these |
| `BodyAsText` | `false` | Gets the body as plain text, which Graph makes from the HTML |
| `IncludeHeaders` | `false` | Gets the internet headers as well, in `Headers`. `Delivered-To` among them says which address a message was delivered to, when it was sent to a group or by BCC, so isn't in `To` or `Cc` |

Each `ReceivedEmail` has the message's `Id`, `InternetMessageId`, `Subject`, `From`, `Sender`, `To`, `Cc`, `ReplyTo` (as MimeKit `MailboxAddress`es, so you can pass them to an `EmailParameters` to reply), `ReceivedAt`, `Body` (with `BodyIsHtml`), `IsRead`, `HasAttachments`, `ConversationId` and `Headers`. A sender inside the organisation sometimes has an Exchange address (such as `/O=EXCHANGELABS/...`) rather than an email address, in which case the `MailboxAddress` has the name and no address.

The failures are as for sending, with one more: a message, attachment or folder that doesn't exist (eg a message that has been moved, so its id has changed) gives `NotFound`.

If your mail isn't in Microsoft 365, MailKit's `ImapClient` (which this package already depends on) reads any IMAP server, and can log in with OAuth too.

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
