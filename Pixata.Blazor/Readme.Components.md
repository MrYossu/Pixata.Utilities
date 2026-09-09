# Components

>Part of [Pixata.Blazor](Readme.md)

Some general components that I found useful.

## VirtualiseWithState

I discovered the `<Virtualize>` component, which allows you to load data as it's needed, and have full flexibility over the display. This gives you the power and convenience of the virtualisation feature in the Telerik Blazor grid, but without your page looking too grid-like.

As with the grid, it's often desirable to save the state of the component, so that if the user navigates away and back, their previous state will be restored. That can all be done manually, but why bother when we can write a component to do it for us?

The `<VirtualiseWithState>` component is a wrapper around the `<Virtualize>` component that automatically saves and restores the scroll position. If you scroll down the list, then navigate to another page and come back, your previous scroll position will be restored.

It stores the scroll position using `ScrollStateService`, which in turn uses Blazored.LocalStorage, so make sure you have called [`AddPixataBlazor()`](Readme.md#registering-dependencies).

See the [live demo](https://test.pixata.co.uk/VirtualiseWithStateSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/VirtualiseWithStateSample.razor) to see the code required.

## AuditViewer

When investigating bug reports from customers, I often find that the issue is nothing to do with my code, it's that they have changed something in the database, and I need to find out what they changed, and when.

Adding auditing manually can be done, but means you end up writing the same intrusive code in every app. To combat this, I have added some auditing functionality to this repo. This consists of two parts...

- An EF Core interceptor that adds audit entries for every change to entities in your `DbContext`
- A Blazor component that allows you to browse the audit information easily

See the [audit viewer documentation](Readme.AuditViewer.md) for more information.

## SitePageTitle

This is intended to be a drop-in replacement for the built-in `PageTitle` component that allows you to include your site name in the page title, without having to do this on every page.

You need to set your site name in `Program.cs` as follows...

```csharp
SitePageTitle.SiteName = "Fred's Chippie";
```

Then `<SitePageTitle>Home</SitePageTitle>` will set the page title to "Home - Fred's Chippie".

You can change the separator as follows...

```csharp
SitePageTitle.Separator = "::";
```

...which will render the title as "Home :: Fred's Chippie".

You can also swap the order of the page title and site name as follows...

```csharp
SitePageTitle.SiteNameAtEnd = false;
```

...which will render the title as "Fred's Chippie :: Home".

**Remember** that if you are working on a mixed-mode app, you will need to do this in both `Program.cs` files.

## IdentityInspector

Useful for debugging sites that use ASP.NET Core Identity. It displays the current user's claims and policies. As Identity allows you to query the claims, you don't need any configuration for this to work, but it does not allow you to list policies, only to check if a named one exists. Therefore, it needs to know what policies (if any) you want to check.

There are two ways of specifying the policies to be checked.

You can pass a hard-coded list of policy names as follows...

```xml
<IdentityInspector Policies='["Initials", "FullName"]' />
```

I don't like hard-coded strings, and so keep things like policy names as constants in a helper class. For example, your class might look like this...

```csharp
public class PoliciesHelper {
  public const string Initials = nameof(Initials);
  public const string FullName = nameof(FullName);
}
```

In this case, you can just pass in the type...

```xml
<IdentityInspector PoliciesType="@typeof(PoliciesHelper)" />
```

If both parameters are set, then `PoliciesType` will be used, and `Policies` will be ignored.

## HebrewDatePicker

A date picker that allows you to select Hebrew dates.

It highlights Shabbos and these Yomim Tovim with a light grey background...

- Pesach (days 1, 2, 7, 8)
- Shavuos (days 1, 2)
- Rosh Hashona (days 1, 2)
- Yom Kippur
- Succos, including Shemini Atzeres and Simchas Torah

You can also set `IncludeOtherNonWorkDays="true"` to highlight Chol Hamoed, Tisha B'av and Purim in a slightly lighter grey.

## ListOrGridChooser

A two-button toggle for letting the user switch a page between a list view and a grid view. It renders as a single segmented control with an inline SVG icon on each button, and a tick on whichever one is active.

Bind it to a `ListOrGridChooser.ViewMode` value (`List` or `Grid`)...

```xml
<ListOrGridChooser @bind-Value="_viewMode" ButtonSize="ListOrGridChooser.Size.Medium" />
```

```csharp
private ListOrGridChooser.ViewMode _viewMode = ListOrGridChooser.ViewMode.Grid;
```

| Parameter | Default | What it does |
| --- | --- | --- |
| `Value` | `ViewMode.Grid` | The currently selected view. Supports `@bind-Value` |
| `ValueChanged` | | Raised when the user picks the other view. Clicking the already-selected button does nothing |
| `ButtonSize` | `Size.Small` | `Small`, `Medium` or `Large` |

>Unlike the rest of the components, this one carries its own styles rather than using [pixata.css](Readme.Styling.md), so the theme variables don't affect it.

## CommonComponentBase

As I found myself writing the same helper methods to support common tasks in components, I decided to create a base class that all my components could inherit from. This was a balance between providing what is most likely to be needed and not stuffing absolutely everything into a God class. The eventual choice reflects my own needs, but I hope that it will be useful to others as well.

The class is generic, the type parameter being the type of the item you are displaying...

```csharp
@inherits CommonComponentBase<InvestorOverview>
```

It injects instances of `AuthenticationStateProvider`, `NavigationManager` and `TemplateHelper` so you don't need to do this yourself.

### Auth helpers

As every app I write uses Identity, there are certain basic auth-related tasks that come up over and over again, specifically related to checking if a user is authed, and getting their claims. Therefore I added the following methods (all async)...

- `Task<bool> IsAuthed()` - True if the user is authenticated, false if not
- `Task<bool> HasClaim(string claim)` - True if the user has the claim, false if the user is not authenticated or does not have the claim
- `Task<string> GetClaim(string claim)` - Gets the value of a claim of the current user. If the user is not authenticated or does not have the claim, returns an empty string
- `Task<string> GetEmail()` - Gets the email address of the current user. If the user is not authenticated, returns an empty string

### Template helpers

>**Note:** The examples below use a `TelerikGrid`, merely because that's where I use this helper the most. However, it can be used with any component that allows you to specify a template.

Without these methods, creating custom content for a grid cell in a `TelerikGrid` would look something like this...

```xml
<GridColumn Field="@nameof(InvestorOverview.Name)">
  <Template>
    <a href="@($"{RouteHelper.InvestorDetails}{(context as InvestorOverview).Id}")" class="inv-text">@((context as InvestorOverview).Name)</a>
  </Template>
</GridColumn>
```

The base class gives you two methods for this...

- `Uri()` - renders a link. The link text comes from the `Func` you pass in, and the URI comes from the `ToText` function (see below)
- `Text()` - renders plain text, using the same options

Both take optional CSS classes and styles, either as hard-coded strings (`style` and `cssClass`) or as `Func<T, string>` (`styleFunc` and `cssClassFunc`) so you can base them on the item being rendered.

As the class knows the item type, you don't need to specify it on each call, and you don't need to cast `context`...

```csharp
protected override void OnInitialized() {
  ToText = i => $"{RouteHelper.InvestorDetails}{i.Id}";
  DefaultCssClass = "inv-text";
}
```

```xml
<GridColumn Field="@nameof(InvestorOverview.Name)" Template="@(Uri(i => i.Name))" />
```

`ToText` is the function that builds the URI for `Uri()`, so setting it once means every column that renders a link gets it for free.

`DefaultStyle`, `DefaultCssClass`, `DefaultStyleFunc` and `DefaultCssClassFunc` are applied to every call that doesn't override them, which saves repeating the same class on a dozen columns.

If you want to override the default for one column, pass the value explicitly...

```xml
<GridColumn Field="@nameof(InvestorOverview.Amount)"
            Template="@(Text(i => i.Amount.ToString("C2"), style: "text-align: right"))" />
```

If you don't want the base class, the same functionality is available directly on [`TemplateHelper`](Readme.Helpers.md#templatehelper), which is what these methods call.
