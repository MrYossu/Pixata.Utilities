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

You can also set `MessagePosition` and `FeedbackType` there, which control how error messages raised by `Do()` (see below) are shown.

**Remember** that if you are working on a mixed-mode app, you will need to do this in both `Program.cs` files.

## Prerendering

The component loads its data through [`PersistentStateHelper`](Readme.Helpers.md#persistent-state-and-caching-helper), so in a Blazor web app the data loaded during prerendering is handed to the client rather than being fetched twice.

Only a successful response is persisted. Persisting anything else would hand a transient server-side failure to the client as if it were the answer, instead of letting the client simply try again.

If you have more than one `ApiResponseView` on a page, give each one a distinct `Key`, as that is what the state is stored against.

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

`HandleResponse()` does the same for a response you already have, and `Reload()` re-runs `LoadApiResponse`. The `Data` property gets or sets the loaded data, which is handy when you have changed one item and don't want to hit the server again.

## Manual loading

Some components (a data grid with its own paging, for example) want to load their own data. Set `ManualLoading="true"` and the view keeps its loader on screen, rendering your success content off-screen, until the first `Do()` or `HandleResponse()` call succeeds. That way the grid can do its thing without the user watching an empty grid while it happens.

## Other parameters

| Parameter | Default | What it does |
| --- | --- | --- |
| `Key` | `""` | The key used to persist the loaded data across prerendering. Needed if there is more than one view on a page |
| `HideNonSuccess` | `false` | Renders nothing at all for any state other than `Success`. Useful for a panel that should just not appear if its data isn't available |
| `MessageCssClass` | `""` | Extra classes for the `MessageView` that shows error messages |
| `MessagePosition` | `Top` | `Top`, `Bottom` or `None` |
| `FeedbackType` | `MessageView` | `MessageView` shows errors in an inline alert, `Notifications` shows them in a [notification area](Readme.Notifications.md) |
