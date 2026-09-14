# Helpers and services

>Part of [Pixata.Blazor](Readme.md)

The non-visual parts of the package. Most of these are registered for you by [`AddPixataBlazor()`](Readme.md#registering-dependencies).

## Persistent state and caching helper

>**Note:** Starting from .NET 10, there is a built-in way to do this, see [the .NET documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/prerendered-state-persistence?view=aspnetcore-10.0) for more details.

When using the Blazor web app template introduced in .NET 8, you have to deal with avoiding loading the data twice, once when the code is rendered on the server, and once when it is rendered again on the client.

The `PersistentStateHelper<T>` class in this package does that for you. Please see [this blog post](https://www.pixata.co.uk/2024/11/21/loading-data-in-a-blazor-web-app-without-multiple-database-or-api-calls/) where I describe it, and show some sample code.

`Get()` also takes an optional `persistWhen` predicate, which says whether the data that was loaded is worth persisting. If you don't pass one, the data is persisted as long as it isn't null. This is useful if the data may represent a failure (as an `ApiResponse<T>` does), as persisting that would hand a transient server-side failure to the client as if it were the answer, rather than letting the client simply load the data again.

If you use more than one of these on a page, pass a distinct `key` to each, as that is what the state is stored against.

[`ApiResponseView`](Readme.ApiResponseView.md) uses this internally, so if that is all you need it for, you get it for free.

## MessageBroker

Lets components send messages to each other without either of them knowing the other exists. Useful for things like a header that shows a basket count, which needs to update when a product page adds something to the basket.

Inject the registered `MessageBrokerInstance` and use its `Broker` property. As the instance is registered as a scoped service, every component in the same circuit (or the same WASM app) shares the same broker.

Messages are identified by their type, so define a type for each kind of message...

```csharp
public record BasketChanged(int ItemCount);
```

Subscribe in the component that wants to know...

```csharp
@implements IDisposable
@inject MessageBrokerInstance BrokerInstance

@code {
  protected override void OnInitialized() =>
    BrokerInstance.Broker.Subscribe<BasketChanged>(OnBasketChanged);

  private async Task OnBasketChanged(BasketChanged message) {
    _count = message.ItemCount;
    await InvokeAsync(StateHasChanged);
  }

  public void Dispose() =>
    BrokerInstance.Broker.Unsubscribe<BasketChanged>(OnBasketChanged);
}
```

...and send from wherever the change happens...

```csharp
await BrokerInstance.Broker.SendMessage(new BasketChanged(_basket.Count));
```

Handlers are `Func<T, Task>`, so they can be async. More than one component can subscribe to the same message type, and they are called in the order they subscribed.

Do remember to unsubscribe in `Dispose()`. The broker holds the handler, which holds your component, so a component that doesn't unsubscribe won't be collected, and will carry on being called after the user has navigated away.

## PasswordOptionsHelper

If your app uses ASP.NET Core Identity, then it is helpful to show the user the password requirements. It's amazing how many sites don't do this, and wait until you've submitted the form before telling you that your password isn't strong enough!

Rather than writing out the rules by hand (and forgetting to update them when you change the options), inject the helper and let it read them from your `IdentityOptions`...

```xml
@inject PasswordOptionsHelper PasswordOptionsHelper

<HtmlRaw Html="@PasswordOptionsHelper.PasswordOptions()" />
```

This produces something like "Your password must be at least 8 characters long, and contain..." followed by a list of whichever rules you have turned on.

Pass `false` to get "The password must be..." instead of "Your password must be...", which reads better on an admin page where you are setting someone else's password.

## ScrollStateService

Saves and restores a scroll position, using Blazored.LocalStorage. `Save(key, scrollTop)`, `Get(key)` and `Remove(key)` are all there is to it.

This is what [`VirtualiseWithState`](Readme.Components.md#virtualisewithstate) uses, and it is registered for you, so you only need to use it directly if you are doing your own scroll restoration.

## TryGetQueryString()

An extension method on `NavigationManager` that reads a value out of the current URL's query string...

```csharp
if (NavigationManager.TryGetQueryString("page", out int page)) {
  _page = page;
}
```

It returns false (and sets the out parameter to its default) if the key isn't there, or if the value can't be converted. Only `int`, `decimal` and `string` are supported. Taken from [Chris Sainty's blog post](https://chrissainty.com/working-with-query-strings-in-blazor/).

## PixataBaseClientService

A thin wrapper around `HttpClient` for calling API endpoints that return an [`ApiResponse<T>`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.ApiResponse.md). It turns the things that can go wrong into the matching `ApiResponse` state, so your calling code never has to catch anything...

```csharp
public class ProductService(PixataBaseClientService client) {
  public Task<ApiResponse<List<ProductDto>>> GetProducts() =>
    client.Get<List<ProductDto>>(RoutesHelper.ApiProducts);

  public Task<ApiResponse<Yunit>> Save(ProductDto product) =>
    client.Post<Yunit>(RoutesHelper.ApiProducts, product);
}
```

| Status | State you get back |
| --- | --- |
| 503 | `ServiceUnavailable` |
| 401 or 403 | `Unauthorised` |
| 404 | `NotFound` |
| 409 | `Conflict`, with the message from the body |
| Any other error | `Failure`, with the status code and body in the message |
| `HttpRequestException` | `HttpFailure`, so you can tell "no connection" from "the server said no" |

If the call succeeds, the body is deserialised as an `ApiResponse<T>`, and whatever state the server sent is preserved, so a server returning `NotFound` in the body keeps that distinction.

`PostBson()` posts an [`UploadFileDto`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.SharedModels.md#file-uploads) as BSON, which avoids the base64 inflation you get from sending a byte array as JSON. `SendRequest()` is there for anything else.

It uses the named `HttpClient` `"Default"`, so register one...

```csharp
builder.Services.AddHttpClient("Default", client =>
  client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddScoped<PixataBaseClientService>();
```

Relative URIs are resolved against the `NavigationManager`'s base URI, and anything starting with "http" is used as it stands.

## TemplateHelper

Are you fed up of writing code like this (sample from a Telerik grid, but it's the same for Microsoft's or anyone else's)...

```xml
<GridColumn Field="@nameof(TransactionView.Amount)">
  <Template>
    @{
      TransactionView tv = context as TransactionView;
      <div style="text-align: right">@tv.Amount.ToString("C2")</div>
    }
  </Template>
</GridColumn>
```

So am I, so I added the `TemplateHelper` to help. Inject it (or inherit [`CommonComponentBase<T>`](Readme.Components.md#commoncomponentbase), which injects it for you and wraps these methods so you don't have to name the type on every call).

`Text<T>()` allows you to reduce the above to...

```xml
<GridColumn Field="@nameof(TransactionView.Amount)"
            Template="@(TemplateHelper.Text<TransactionView>(tv => tv.Amount.ToString("C2"), "text-align: right"))" />
```

The method takes a `Func` that converts your entity to a `string`, which is what is displayed. There are two optional `string` parameters that allow you to set the style (as above) and/or CSS class(es).

`Link<T>()` works the same way, but also takes a `Func` for the URI, and renders the text as a link. It replaces...

```xml
<GridColumn Field="@nameof(TransactionView.Amount)">
  <Template>
    @{
      TransactionView tv = context as TransactionView;
      <div style="text-align: right">
        <a href="/transaction/@tv.Id">@tv.Amount.ToString("C2")</a>
      </div>
    }
  </Template>
</GridColumn>
```

...with...

```xml
<GridColumn Field="@nameof(TransactionView.Amount)"
            Template="@(TemplateHelper.Link<TransactionView>(tv => tv.Amount.ToString("C2"),
                                                             tv => $"/transaction/{tv.Id}",
                                                             "text-align: right"))" />
```

Both `Text<T>()` and `Link<T>()` have an overload that takes `Func<T, string>` for the style, the CSS class and the link title, instead of hard-coded strings. That lets you base the styling on the item being rendered...

```xml
<GridColumn Field="@nameof(TransactionView.Amount)"
            Template="@(TemplateHelper.Link<TransactionView>(tv => tv.Amount.ToString("C2"),
                                                             tv => $"/transaction/{tv.Id}",
                                                             styleFunc: _ => "text-align: right",
                                                             cssFunc: tv => tv.Amount >= 0 ? "" : "withdrawal"))" />
```

This adds the CSS class `withdrawal` when the transaction amount is negative. You can do similar things for the style and the link title.

`BuildLink<T>()` is the same as that last overload of `Link<T>()`, and `BuildTemplate<T>()` and `BuildTemplateLink<T>()` are the older names for `Text<T>()` and `Link<T>()`. They are all still there, so existing code keeps working.

You can see a sample of these in action on the sample project, [demo here](https://test.pixata.co.uk/TelerikGrid), [source code here](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/GridSample.razor).
