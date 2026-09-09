# Route dumping

>Part of [Pixata.AspNetCore](Readme.md)

When writing API endpoints, it can be hard to keep track of all the routes you have defined, and what they are. To help with this, this package adds an endpoint that lists every route in your app.

Add the following to your `Program.cs`...

```csharp
app.MapPixataAspNetCoreApiEndpoints();
```

...and then navigate to `/dump-routes`, which returns the routes as plain text, one per line.

By default, it ignores routes that start with any of `"/_blazor"`, `"/_framework"` or `"/_content"`, as these are not usually of interest. You can override this by passing an array of routes to ignore...

```csharp
app.MapPixataAspNetCoreApiEndpoints(["/_blazor", "/_framework", "/_content", "/hello"]);
```

Note that you need to include the default ones if you want to ignore them. If you pass in an empty array, then all routes will be dumped.

Also note that any routes that **start with** any of the specified routes will be ignored. So if you specify `"/hello"`, then `"/hello-world"` will also be ignored.

>As this lists every endpoint in your app, you probably don't want it available to the public. Either only call `MapPixataAspNetCoreApiEndpoints()` in development, or put your app behind authentication before you deploy it.
