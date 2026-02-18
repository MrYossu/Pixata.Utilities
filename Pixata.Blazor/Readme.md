# Pixata.Blazor [![Pixata.Blazor Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor)](https://www.nuget.org/packages/Pixata.Blazor/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/avion.png "Pixata") 

I have [blogged about some Blazor components I've been writing](https://www.pixata.co.uk/tag/blazor/). This project contains the source for those components.

There is a [complimentary package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents), which contains additional components for those who have a subscription to Telerik.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for this project.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Test) to the repository, and intend to use that to try out and demonstrate the components. It doesn't contain samples for all the components yet, but I hope to add more over time.

## Registering dependencies
Some components in this package require services to be registered in the DI container. To make this easier, you can use the `AddPixataBlazor` extension method in your `Program.cs` file:

```csharp
builder.Services.AddPixataBlazor();
```

This registers the following services (all from this package)...
- MessageBrokerInstance - Used by the `MessageBroker`, which allows you to send messages between different components without them needing to know about each other.
- NotificationHelper - [sample page](https://test.pixata.co.uk/Notifications)
- PasswordOptionsHelper - If you app uses ASP.NET Core Identity, then it is helpful to show the user the password requirements (it's amazing how many sites don't do this, and wait until you've submitted the information before telling you that your password isn't strng enough!). Simple inject the component into a component, 
- PersistentStateHelper - Persists data, avoiding hitting the database twice when a page loads. Used by the [ApiResponseView](https://test.pixata.co.uk/ApiResponseViewRegular), but can be used independently. .NET 10 supports this functionality with the [<code>[PersistentState]</code> attribute](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0#declarative-model-for-persisting-state-from-components-and-services), but this component was written around .NET 8, and is still useful for projects targetting .NET versions before 10.
- TemplateHelper - usage can be seen on the [Telerik grid sample page](https://test.pixata.co.uk/TelerikGrid), although the helper can be used with any component that supports templating

Note that you need to do this in any `Program.cs` file, so if you have a mixed rendering mode (both server-side and client-side), you'll need to call `AddPixataBlazor` in both `Program.cs` files.

It is a good idea to add this line **after** your own service registrations, as it checks for duplicate registrations. Therefore, if you have already registered any of the services, you will see a message in your console...

>A service of type TemplateHelper has already been registered

This isn't actually a problem, but removing the duplicate registration will keep the code file a bit cleaner.

## Components

Some general componets that I found useful.

### PageTitleWithSiteName
This snappily-named component allows you to set the page title, and have your site name automatically appended to it. It is intended to be used instead of the built-in `PageTitle` component, and relies on you setting the site name in your app settings...

```json
  "SiteName": "My Blazor Site"
```

Then `<PageTitleWithSiteName Title="Home" />` will set the page title to "Home - My Blazor Site".

### HebrewDatePicker

A date picker that allows you to select Hebrew dates.

## Containers

These components are intended to wrap up other parts of your page, and add functionality.

### HtmlRaw

Convenience component for displaying raw HTML. Instead of doing this...

    @((MarkupString)_html)

...where `_html` is a string variable in your code, you can now do...

    <HtmlRaw Html="@_html" />

...which is (for me anyway) slightly easier to remember.

### Busy
Useful when data is loading. You bind the `Data` parameter to whatever model you are using. When the page first loads, and the model is (presumably) null, a busy indicator will show. When the data has loaded, and the model is non-null, the display is automatically switched to the real content.

Sample usage...

```
<Busy Data="_avreich">
  <!-- HTML and other Blazor components go here... -->
</Busy>
```

By default, the message "Loading..." is displayed while the data is loading, but you can override that by setting the `Message` parameter.

You can also set the class for the container, in case you want to add your own styling, and set the classes for the spinner and spinner colour. By default, the component uses the Bootstrap `spinner-border` class, bu you can override this to use something else if you want.

### Confirm
Replaces the nasty JavaScript `confirm` function with something that looks nicer, and doesn't require any JSInterop.

See the [sample code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/ConfirmSample.razor) ([live demo](https://test.pixata.co.uk/ConfirmSample)) for an example of how to use it. You can set the pop-up to disable the entire window, or just one section of it. You can also specify if the pop-up should disappear as soon as a button is clicked, or if it should remain visible, but disabled (with a busy indicator) until you dismiss it.

### Inform
Similar to `Confirm`, but only has one button. At the moment, the pop-up id dismissed as soon as you click the button, but I intned to add the feature described above to this component as well.

### DumpCollection
OK, so this isn't striclty a container, but it's close enough to put here.

I often find the need to see the contents of a collection while developing. I found myself writing code like this far too often...

```html
<ul>
  @foreach (var t in SomeCollection) {
    <li>(@t.Id) @t.Name</li>
  }
</ul>
```

...where the exact contents of the `<li>` tag varies with each usage.

To make this quicker and easier, I added the `DumpCollection` component to do this. By default, the component will just call `ToString()` on each item in the collection, allowing you to do a quick dump of the contents...

```html
<DumpCollection Collection="SomeCollection" />
```

If you want to format the output differently, you can use the `Display` parameter to pass in a lambda that formats each item...

```html
<DumpCollection Collection="SomeCollection" Display="@(t => $"({t.Id})  {t.Name})" />
```

The component has two extra paramters, `UlClass` and `LiClass` that allow you to pass in CSS classes for the `<ul>` element and the `<li>` elements.

## Extensions

### Persistent state and caching helper

>**Note:** Starting from .NET 10, there is a built-in way to do this, see [the .NET documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/prerendered-state-persistence?view=aspnetcore-10.0) for more details.

When using the Blazor web app template introduced in .NET8, you have to deal with avoiding loading the data twice, once when the code is rendered on the server, and once when is rendered again on the client.

The `PersistentStateHelper` helper class in this package does that for you. Please see [this blog post](https://www.pixata.co.uk/2024/11/21/loading-data-in-a-blazor-web-app-without-multiple-database-or-api-calls/) where I describe it, and show some sample code.

### TemplateHelper
Are you fed up of writing code like this (sample from a Telerik grid, but it's the same for Microsoft's or anyone else's)...

```html
    <GridColumn Field="@nameof(TransactionView.Amount)" >
      <Template>
        @{
          TransactionView tv = context as TransactionView;
          <div style="text-align: right">@tv.Amount.ToString("C2")</div>
        }
      </Template>
    </GridColumn>
```

So am I, so I added the `TemplateHelper` to help. It contains three methods...

`Text<T>` allows you to reduce the above code to...

```html
    <GridColumn Field="@nameof(TransactionView.Amount)"
       Template="@(MainLayout.Text<TransactionView>(tv => tv.Amount.ToString("C2"), "text-align: right"))" />
```

The method takes a `Func` that converts your entity to a `string`, which is what is displayed. There are two optional `string` parameters that allow you to set the style (as above) and/or CSS class(es).

There is a similar method named `Link` which works the same, but takes a URI, and allows you to replace...

```html
    <GridColumn Field="@nameof(TransactionView.Amount)" >
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

```html
    <GridColumn Field="@nameof(TransactionView.Amount)"
      Template="@(MainLayout.Link<TransactionView>(tv => tv.Amount.ToString("C2"),
                                                                tv => $"/transaction/{tv.Id}"
                                                               "text-align: right"))" />
```

There are also overloads for this that take `Func`s for the style, CSS and link title. For example, if you want to base your CSS on an entity property, you can do something like this...

```html
    <GridColumn Field="@nameof(TransactionView.Amount)"
      Template="@(MainLayout.BuildLink<TransactionView>(tv => tv.Amount.ToString("C2"),
                                                                tv => $"/transaction/{tv.Id}"
                                                                tv => tv.Amount >= 0 ? "" : "withdrawl"
                                                               "text-align: right"))" />
```

This will add a CSS class `withdrawl` if the transaction amount were negative. You can do similar things for the style and link title.

You can see a sample of these in action on the sample project, [demo here](https://test.pixata.co.uk/TelerikGrid), [source code here](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/GridSample.razor).

### ValidationEndpointFilter
When using fluent validation in Blazor server-side, the chances of anyone bypassing your validation are small enough that they can be ignored for most cases. However, when running in client-side (WASM), validation is handled in the WASM, and the data is then sent to the servier via API endpoints. This means that anyone can modify the request, or write a script to mimic it, and bypass your validation.

To avoid this, you can add the `ValidationEndpointFilter` to your API endpoints. This will run the same validation as in the client, but on the server, so if anyone tries to bypass the client-side validation, they will be stopped by the server-side validation. This allows you to protect your endpoints without adding much extra code.

Basic usage is very simple. First you need to add fluent validation, register the filter, tell it where to find your validators (in the server project's `Program.cs`)...

```csharp
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ContactModelValidator>();
builder.Services.AddSingleton<ValidationEndpointFilter>();
```

Then you can use the `AddEndpointFilter` extension method to add the filter to any API endpoints that need validation...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model)).AddEndpointFilter(new ValidationEndpointFilter();
```

If there were any validation errors, then the filter will return an `ApiResponse` with a `State` of `ApiResponseStates.Failure` and a `Message` containing a formatted string of the validation errors. By default, the errors are formatted as a comma-delimited string of the form `$"{e.PropertyName}: {e.ErrorMessage}"`, which would produce something like... `"Validation errors - Name: Required, Email: Invalid"`. This can be overriden as explained below.

The filter has two optional parameters.

You can pass in a `Func<ValidationFailure, string>` to format the validation errors. For example, if you wanted to include the error code in the message, you could do something like this...


```csharp
app.MapPost(RoutesHelper.ApiContact, async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model))
    .AddEndpointFilter(new ValidationEndpointFilter(err => $"({err.ErrorCode}) {err.ErrorMessage} for {err.PropertyName}"));
```

This would produce a message of the form `"Validation errors - (NotEmptyValidator) Required for Name, (EmailValidator) Invalid for Email"`.

By default, the filter will pick up any a validator for any class in the assembly you specified when registering (with the `AddValidatorsFromAssemblyContaining` method, see above). You may wish to restrict this further, and specify that only classes within a certain namepsace should be validated. You can do this as follows...

```csharp
app.MapPost("/contact-api", async (GeneralServiceInterface service, ContactModel model) =>
  await service.Contact(model)).AddEndpointFilter(new ValidationEndpointFilter(nameSpace: typeof(ContactModel).Namespace);
```

### TryGetQueryString()
Documentation coming soon...

## Forms
A set of components for laying out forms. These come in two flavours, Bootstrap style, and floating label style.

The [form page on the sample project]([link text](https://test.pixata.co.uk/FormSample)) shows examples of the Bootstrap style. A live sample of the Bootstrap style can be seen here... [live demo](https://test.pixata.co.uk/FormSample), [source code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/FormSample.razor). You can see the full collection of components by checking the ones named `FormRowAbc` in [the Forms section of the source code](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor/Forms).

I hope to add a sample for the floating label style soon. The controls are...

- `FormSingle` - a single input control with label
- `FormDouble` - a row with two input controls and labels
- `FormTriple` - a row with three input controls and labels
- `FormQuad` - a row with four input controls and labels
- `FormName` - a row with title, first name and surname input controls and labels

Sample usage of these is as follows (Telerik input controls used, but you can use any input controls you like)...

```xml
<FormSingle Label="Email" Id="Email" Required="true">
  <TelerikTextBox @bind-Value="@user.Email" Id="Email" />
  <ValidationMessage For="@(() => user.Email)" />
</FormSingle>
```

This looks like this...

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/FormSingle.png "FormSingle") 

Note that the `Required` property merely adds a red asterisk to the label, it does not enforce any validation. You still need to do that yourself.

The `FormDouble`, `FormTriple` and `FormQuad` components work in a similar way, except that the properties for setting the Ids are named   `FirstId`, `SecondId`, `ThirdId` and `FourthId`. The properties for the labels and required are named similarly.

The `FormName` component is very similar to `FormTriple`, except that the controls are sized more appropriately for names. Sample usage is as follows...`

```xml
<FormName TitleLabel="Title" TitleId="Title" TitleRequired="true"
          FirstNameLabel="First name" FirstNameId="FirstName" FirstNameRequired="true"
          SurnameLabel="Surname" SurnameId="Surname" SurnameRequired="true">
  <Title>
    <TelerikTextBox @bind-Value="@user.Title" Id="Title" />
    <ValidationMessage For="@(() => user.Title)" />
  </Title>
  <Salutation>
    <div style="max-width: 300px">
      <TelerikTextBox @bind-Value="@user.FirstName" Id="FirstName" />
      <ValidationMessage For="@(() => user.FirstName)" />
    </div>
  </Salutation>
  <Surname>
    <div style="max-width: 300px">
      <TelerikTextBox @bind-Value="@user.Surname" Id="Surname" />
      <ValidationMessage For="@(() => user.Surname)" />
    </div>
  </Surname>
</FormName>
```

This looks like this...

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/FormName.png "FormName") 

## Replacing Razor code with declarative components

> **Note:** Since adding these components, I have discovered that intermittently, my apps would randomly stop working without any exceptions or errors being surfaced. This doesn't happen very often, but once it happens, it can stick. For that reason, I recommend using these componets with caution. If you encounter any weird, intermittent issues, try replacing these components with the equivalent `@if`, `@switch` and `@foreach` to see if that resolves the problem.

I have found some issues using some `@` statements in Razor markup. For one, Visual Studio seems to have its own ideas about how the braces should be formatted, and these are usually different from my ideas! Also, the functionality from the rather fabulous [ZenCoding ](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ZenCoding) extension doesn't work consistently with `@` statements.

For this reason, I have added some components to replace these statements with Blazor components.

### If

The `If` component replaces the Razor `@if` statement. Usage is pretty simple. Assume `_n` is an `int` variable...

```xml
<If Condition="@(_n > 10)">
  <Then>
    <p>Number is greater than 10</p>
  </Then>
  <ElseIf Condition="@(_n == 5)">
    <p>Number is 5</p>
  </ElseIf>
  <ElseIf Condition="@(_n > 5)">
    <p>Number is greater than 5</p>
  </ElseIf>
  <Else>
    <p>Number is 5 or less</p>
  </Else>
</If>
```

The `ElseIf` and `Else` components are optional, and you can have as many `ElseIf` components as you like. The `Condition` parameter is a `bool` that determines if the content is displayed.

**Note:** This package also contains a deprecated `Conditional` component, which is a more basic version of the above. Apart from the fact that "if", "then" and "else" are keywords familiar to generations of programmers, `Conditonal` only allows for the "if" clause, and an optional "else". By contrast, `If` allows for as many "else if" clauses as you like.

### Switch

The `Switch` component replaces the Razor `@switch` statement. Assume the same `int` variable as above...

```xml
<Switch Variable="@_n">
  <Case Equals="1">
    <p>n is one</p>
  </Case>
  <Case Equals="2">
    <p>n is two</p>
  </Case>
  <Default>
    <p>n is not one or two</p>
  </Default>
</Switch>
```

### ForEach

The `ForEach` component allows you to replace usages of `@foreach` with a component...

```xml
<ul>
  <ForEach Collection="@Enumerable.Range(0, 3)">
    <Each Context="n">
      <li>@n</li>
    </Each>
  </ForEach>
</ul>
```

The `Collection` parameter can be any `IEnumerable<T>`. The `Context` parameter on `Each` supplies an item from the collection.

In the case above, you can use the `EachFunc` parameter to do the same with much less code...

```xml
<ul>
  <ForEach Collection="@Enumerable.Range(0, 3)" EachFunc="@(n => $"<li>{n}</li>")" />
</ul>
```

**Note:** If you specify both `Each` and `EachFunc`, `Each` alone will be used.