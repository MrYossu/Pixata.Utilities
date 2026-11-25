# ApiResponseView

>Part of [Pixata.Blazor](Readme.md)

[`ApiResponse<T>`](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Extensions/Readme.ApiResponse.md) (from Pixata.Extensions) is the result type used across these packages instead of exceptions. `ApiResponseView` is the component that renders one, so you don't write the same "loading, then either the data or an error" markup on every page.

You give it a method that returns a `Task<ApiResponse<T>>` and the markup for the success case. It works out the rest.

```xml
<ApiResponseView LoadApiResponse="@GetTeams">
  <Success Context="teams">
    <ul>
      @foreach (Team t in teams) {
        <li>@t.Name</li>
      }
    </ul>
  </Success>
</ApiResponseView>
```

```csharp
private async Task<ApiResponse<List<Team>>> GetTeams() =>
  await _service.GetTeams();
```

There are live samples on the sample site - [standard usage](https://test.pixata.co.uk/ApiResponseViewRegular) and [manual loading](https://test.pixata.co.uk/ApiResponseViewManual).

## What it renders

While the method is running, the component shows a loader. Once it completes, the display switches to match the state of the response...

| State | What is shown |
| --- | --- |
| `Loading` | The `Loader` fragment |
| `Success` | Your `Success` fragment, with the data as its `Context` |
| `NotFound` | The `NotFound` fragment |
| `Failure` | The `Failure` fragment, with the message |
| `HttpFailure` | The `HttpFailure` fragment, which by default asks the user to check their Internet connection |
| `ServiceUnavailable` | The `ServiceUnavailable` fragment, useful when your app is being updated |
| `Unauthorised` | The `RequiresLogIn` fragment, and the user is redirected to your log-in page with a `returnUrl` |
| `Conflict` | The `Conflict` fragment, with the message |

Each of those is a parameter, so you can override any of them for a single usage.

## Setting the defaults for your app

Overriding the fragments on every page would defeat the point, so the defaults live in static fields on `ApiResponseViewConfig`. Set the ones you want in `Program.cs`, and every `ApiResponseView` in your app picks them up...

```csharp
ApiResponseViewConfig.LogInUrl = "/Identity/Account/Login";

ApiResponseViewConfig.NotFound = _ => @<div>Sorry, we couldn't find that</div>;
```

`LogInUrl` is the one you really do need to set, as it is where the component sends a user whose session has expired.

You can also set `MessagePosition`, `FeedbackType` and `MessageIsHtml` there, which control how the messages raised by `Do()` and `Notify()` (see below) are shown, and `PersistState` and `MaxPersistedBytes`, which control whether the data loaded while the page is prerendered is used once it's interactive (see [prerendering](#prerendering)).

The default `Failure` fragment shows a general "Sorry, something went wrong" message, and puts the failure's message in an HTML comment, so you can see what went wrong on a live site with view source or the browser's developer tools. If you'd rather that wasn't visible, set your own `Failure`. Any `--` in the message has a space put in the middle (`- -`), so a message that contains `-->` can't end the comment early and have the rest of it rendered as HTML.

**Remember** that if you are working on a mixed-mode app, you will need to do this in both `Program.cs` files.

## Prerendering

The component loads its data through [`PersistentStateHelper`](Readme.Helpers.md#persistent-state-and-caching-helper), so in a Blazor web app the data loaded during prerendering is handed to the client rather than being fetched twice.

Only a successful response is persisted. Persisting anything else would hand a transient server-side failure to the client as if it were the answer, instead of letting the client simply try again.

If you have more than one `ApiResponseView` on a page, give each one a distinct `Key`, as that is what the state is stored against. Without one, the key is the page's address, so the views share it. Rather than risk a view being given another's data, views that share a key don't persist their data, and each loads it again once the page is interactive.

>**Changed in v5.26.0.** The data was only persisted when the page went on to run in WebAssembly, so in a Blazor Server app (or a page that is `InteractiveServer`), every view loaded its data twice. It is now persisted for both. Also, all the views of one type on a page shared one persisted value, so only the last one's data was kept, and the others either loaded theirs again or, if they had no `Key`, could be handed the last one's data. Each view now persists its own.

### What persisting the data means for your loader

When the data is restored from the persisted state, `LoadApiResponse` isn't called again once the page is interactive. That has two consequences...

- **The loader should only load the data.** If it also sets up the page, such as setting a form's model, filling drop-down lists or reading settings, none of that happens in the interactive render, so the page isn't set up (an `EditForm` with no model throws "EditForm requires either a Model parameter, or an EditContext parameter"). Do that set-up in `OnInitialized` or in the `Success` content, or turn persisting off for the view.
- **The data has been through JSON.** It's serialised with `System.Text.Json` on the server and read back, so anything that doesn't survive that is lost. A property marked `[JsonIgnore]` (such as the side of a two-way EF Core relationship that leads back, to avoid "A possible object cycle was detected") comes back null, and the data has to be serialisable at all.

To have a view call `LoadApiResponse` again once the page is interactive, as it did before v5.26.0, set `PersistState="false"` on it, or set `ApiResponseViewConfig.PersistState = false` in `Program.cs` to do that for every view (a view can then turn it back on with `PersistState="true"`). The cost is loading the data twice, once for the prerender and once for the interactive page.

### Large data in Blazor Server

In Blazor Server, the persisted data is written into the page, and the browser sends it back to the server in the message that starts the circuit. SignalR's `MaximumReceiveMessageSize` is 32KB by default, and a page whose persisted state (for all its views together, encrypted and base64 encoded, so somewhat bigger than the JSON) is bigger than that never becomes interactive. The circuit closes straight away, the browser logs "Server returned an error on close: Connection closed with an error", and nothing is logged on the server. Even with the limit raised, every visit uploads that data from the browser, which on a phone can be slower than loading it again.

So for a view with a lot of data, such as a long list, set `PersistState="false"`. To stop any view breaking the page as its data grows, set a limit for them all...

```csharp
ApiResponseViewConfig.MaxPersistedBytes = 16 * 1024;
```

A view whose data, as JSON, is bigger than that doesn't persist it, and loads it again once the page is interactive, with a warning in the log naming its `Key`. It's null (no limit) by default. In WebAssembly, the data isn't sent back up, so there's no need for a limit there.

## Doing something with the data

The component isn't only for display. It also handles the responses from the calls you make once the page is loaded, which saves a `try`/`catch` and a state check on every button click...

```csharp
private ApiResponseView<List<Team>> _view = null!;

private async Task Delete(Team team) =>
  await _view.Do(() => _service.DeleteTeam(team.Id),
                 _ => _teams.Remove(team));
```

`Do()` calls your method and then...

- on success, runs the `onSuccess` action you pass in
- on `HttpFailure`, raises a notification telling the user to check their connection
- on `Unauthorised`, sends the user to the log-in page
- on `Failure` or `Conflict`, shows the message and runs the optional `onNotSuccess` action

Pass `changeStateOnNotSuccess: true` if you would rather the whole view switched to its failure display than show a message over the top of the data.

You can show a message of your own with `Notify()`, such as a success message once something has been saved...

```csharp
_view.Notify(NotificationType.Success, "Saved");
```

With `FeedbackType` set to `Notifications`, the message goes to the [notification area](Readme.Notifications.md). Otherwise it's shown in the view's [`MessageView`](Readme.Containers.md#messageview), in the colour for its type, so a success is green and a warning yellow. A message you set with the `Message` property is shown as an error.

The messages are shown as text, as they often quote something a user typed. If your app puts markup in its messages on purpose, set `MessageIsHtml="true"` on the view, or `ApiResponseViewConfig.MessageIsHtml = true` for every view, but then make sure that anything a user typed is HTML-encoded before it goes into a message.

>**Changed in v5.29.0.** The messages in the `MessageView` were rendered as HTML, and they were always shown as errors, even when `Notify()` was given another type, so a success message was shown in red.

`HandleResponse()` does the same for a response you already have, and `Reload()` re-runs `LoadApiResponse`. The `Data` property gets or sets the loaded data, which is handy when you have changed one item and don't want to hit the server again.

## Manual loading

Some components (a data grid with its own paging, for example) want to load their own data. Set `ManualLoading="true"` and the view keeps its loader on screen, rendering your success content off-screen, until the first `Do()` or `HandleResponse()` call succeeds. That way the grid can do its thing without the user watching an empty grid while it happens.

## Other parameters

| Parameter | Default | What it does |
| --- | --- | --- |
| `Key` | `""` | The key used to persist the loaded data across prerendering. Needed if there is more than one view on a page |
| `PersistState` | `ApiResponseViewConfig.PersistState`, which is `true` | Whether the data loaded while prerendering is persisted and used once the page is interactive, rather than calling `LoadApiResponse` again. See [prerendering](#prerendering) |
| `HideNonSuccess` | `false` | Renders nothing at all for any state other than `Success`. Useful for a panel that should just not appear if its data isn't available |
| `MessageCssClass` | `""` | Extra classes for the `MessageView` that shows error messages |
| `MessageIsHtml` | `ApiResponseViewConfig.MessageIsHtml`, which is `false` | Whether the messages in the `MessageView` are rendered as HTML rather than as text |
| `MessagePosition` | `Top` | `Top`, `Bottom` or `None` |
| `FeedbackType` | `MessageView` | `MessageView` shows errors in an inline alert, `Notifications` shows them in a [notification area](Readme.Notifications.md) |
