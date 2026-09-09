# API responses

>Part of [Pixata.Extensions](Readme.md)

`ApiResponse<T>` is the result type used across the Pixata packages instead of throwing exceptions. A method returns one whether it succeeded or not, and the caller decides what to do about it.

It is a `record` with three members...

| Member | What it holds |
| --- | --- |
| `State` | An `ApiResponseStates` value saying what happened |
| `Data` | The result, which is only meaningful when `State` is `Success` |
| `Message` | The error message, which is only meaningful when `State` is not `Success` |

## The states

| State | Meaning |
| --- | --- |
| `Loading` | The data hasn't arrived yet. This is the state a component starts in |
| `Success` | It worked, and `Data` holds the result |
| `NotFound` | The thing you asked for isn't there |
| `Failure` | Something went wrong, and `Message` says what |
| `HttpFailure` | The call didn't reach the server (no connection, DNS failure, and so on) |
| `ServiceUnavailable` | The server answered, but said it can't help at the moment |
| `Unauthorised` | The caller isn't logged in, or isn't allowed to do this |
| `Conflict` | The request clashed with the current state of the data |

>**If you are adding a state,** append it to the end of the `enum`. The values are serialised as integers when an `ApiResponse` goes over the wire, so inserting a member in the middle changes the meaning of values that existing clients have already been given.

## Using the result

The tidiest way to handle a response is `Match()`, which makes you deal with both outcomes...

```csharp
ApiResponse<Product> response = await service.GetProduct(id);

string message = response.Match(product => $"Found {product.Name}",
                                error => $"Sorry - {error}");
```

There is an `Action`-based overload for when you want to do something rather than return something. As C# has no `void` you can hand to a generic method, it returns [`Yunit`](#yunit) instead...

```csharp
response.Match(product => _product = product,
               error => _error = error);
```

If all you need is a yes or no, there is an implicit conversion to `bool`, which is true when the state is `Success`...

```csharp
if (await service.Save(product)) {
  // It worked
}
```

## Composing responses

`Select()` transforms the data of a successful response, and leaves a failed one alone...

```csharp
ApiResponse<string> name = response.Select(product => product.Name);
```

`SelectMany()` chains calls that each return an `ApiResponse<T>`, which means LINQ query syntax works. The first failure short-circuits the rest, so you only write the happy path...

```csharp
ApiResponse<Invoice> result =
  from customer in GetCustomer(customerId)
  from order in GetOrder(orderId)
  select BuildInvoice(customer, order);
```

There is an async `SelectMany()` extension in `ApiResponseExt`, so the same syntax works when the methods return `Task<ApiResponse<T>>`, which is the usual case for API calls...

```csharp
ApiResponse<Invoice> result = await (
  from customer in GetCustomerAsync(customerId)
  from order in GetOrderAsync(orderId)
  select BuildInvoice(customer, order));
```

To mix a synchronous method into an otherwise async query, use `ToAsync()`, which wraps an `ApiResponse<T>` in a completed `Task`...

```csharp
ApiResponse<Invoice> result = await (
  from customer in GetCustomerAsync(customerId)
  from settings in GetSettings().ToAsync()   // GetSettings returns ApiResponse<Settings>
  select BuildInvoice(customer, settings));
```

## Yunit

`Yunit` is a `readonly struct` with a single value, `Yunit.yunit`. It stands in for `void` in the places where the language won't let you use `void`, such as the return type of the `Action`-based `Match()`, or as the `T` of an `ApiResponse<T>` for an operation that has nothing to return.

```csharp
using static Pixata.Extensions.Yunit;

public async Task<ApiResponse<Yunit>> DeleteProduct(int id) {
  // Delete the product
  return new ApiResponse<Yunit>(ApiResponseStates.Success, yunit);
}
```

The [Pixata.Email](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Email) package returns `ApiResponse<Yunit>` for exactly this reason.

## Displaying a response

The [Pixata.Blazor](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor) package has an `ApiResponseView` component that renders an `ApiResponse<T>`, giving you a loading indicator, an error display and your own markup for the success case without writing the same `switch` on every page. See [the Pixata.Blazor container documentation](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor/Readme.ApiResponseView.md) for details.
