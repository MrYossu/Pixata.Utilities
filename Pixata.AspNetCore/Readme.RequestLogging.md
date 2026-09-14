# Request logging middleware

>Part of [Pixata.AspNetCore](Readme.md)

When writing API endpoints, it can be hard to debug 400 errors, which are often caused by incorrect or mismatched paths, or invalid data in the request. You often don't get much clue as to what actually happened.

To help with this, this package includes a piece of middleware that will log every incoming request to the app (subject to configuration choices, see below). This will log the path, the query string, the headers and the body of the request. This can be very helpful for debugging, as it allows you to see exactly what was sent to the server.

## Setup

Register the options in your `Program.cs` file...

```csharp
builder.Services.AddRequestLogging();
```

This will use the default configuration, which is to include the request headers and body in the logging, and to ignore any request whose path contains any of...

- "_framework"
- "_blazor"
- "_content"
- ".well-known"

You then need to add the middleware to the pipeline...

```csharp
app.UseRequestLogging();
```

This should be after any authentication/authorisation middleware, but before any endpoint mapping.

## Redaction

Logging whole requests is only useful if you can leave the logging on, and you can't do that if the log fills up with authentication cookies and passwords. Anyone who can read your logs would then be able to impersonate your users.

Since v1.9.0, the middleware redacts anything that looks sensitive before it writes to the log...

- **Headers** - the value of any header listed in `RedactedHeaders` is replaced. By default that's `Authorization`, `Proxy-Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`, `Api-Key`, `X-Auth-Token`, `X-Access-Token`, `X-CSRF-Token`, `X-XSRF-Token` and `RequestVerificationToken`
- **Query string parameters, form fields and JSON properties** - the value of anything whose name is listed in `RedactedFields` is replaced. By default that covers the usual suspects (`password`, `newPassword`, `token`, `accessToken`, `refreshToken`, `secret`, `clientSecret`, `apiKey`, `cardNumber`, `cvv` and friends). JSON is redacted at any depth, including inside arrays
- **Bodies of unexpected content types** aren't logged at all. Only the content types listed in `LoggedBodyContentTypes` (JSON, XML, plain text and form data) are written to the log, so a file upload no longer ends up as several megabytes of binary in your log file
- **Long bodies are truncated** to `MaxBodyLength` characters (4096 by default). Only that much of the body is read, so a large upload isn't pulled into memory just to be thrown away

Matching of header and field names is case-insensitive.

If the body claims to be JSON but doesn't parse (which is often exactly the sort of thing you're trying to debug), it's logged as it came in, but with anything that looks like a sensitive property redacted.

Redaction is a safety net, not a guarantee. If your app posts sensitive data in a field with a name I haven't thought of, add it to `RedactedFields`.

## Options

You can override any of the options as follows...

```csharp
builder.Services.AddRequestLogging(o => {
  o.IgnoredPaths = ["_framework", "health"]; // Or whatever you want to ignore
  o.LogBody = false;
  o.LogHeaders = false;
  o.RedactedHeaders.Add("X-My-Custom-Auth-Header");
  o.RedactedFields.Add("mothersMaidenName");
  o.MaxBodyLength = 1024;
  o.RedactionPlaceholder = "***";
});
```

| Option | Default | What it does |
| --- | --- | --- |
| `IgnoredPaths` | `["_framework", "_blazor", "_content", ".well-known"]` | Requests whose path contains any of these strings are not logged at all. Set it to `[]` to log everything |
| `LogHeaders` | `true` | Whether to log the request headers |
| `LogBody` | `true` | Whether to log the request body |
| `RedactedHeaders` | `DefaultRedactedHeaders` | Headers whose values are replaced with the placeholder |
| `RedactedFields` | `DefaultRedactedFields` | Query string parameters, form fields and JSON properties whose values are replaced with the placeholder |
| `LoggedBodyContentTypes` | JSON, problem+JSON, plain text, XML and form data | Bodies whose content type doesn't start with one of these are not logged |
| `MaxBodyLength` | `4096` | Characters of the body to log. Zero or less means no limit |
| `RedactionPlaceholder` | `"(redacted)"` | The text that replaces a redacted value |

The defaults are exposed as `RequestLoggingOptions.DefaultRedactedHeaders` and `RequestLoggingOptions.DefaultRedactedFields`, so you can build your own list from them if you'd rather replace the sets than add to them. Bear in mind that if you do replace them, anything you leave out is no longer redacted.
