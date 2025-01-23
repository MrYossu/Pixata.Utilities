# Pixata.Blazor [![Pixata.Blazor Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor)](https://www.nuget.org/packages/Pixata.Blazor/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/avion.png "Pixata") 

I have [blogged about some Blazor components I've been writing](https://www.pixata.co.uk/tag/blazor/). This project contains the source for those components.

There is a [complimentary package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents), which contains additional components for those who have a subscription to Telerik.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for this project.

## Containers
These components are intended to wrap up other parts of your page, and add functionality.

### If
I have found some issues using `@if` in Razor markup. For one, Visual Studio seems to have its own ideas about how the braces should be formatted, and these are usually different from my ideas! Also, the functionality from the rather fabulous [ZenCoding ](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ZenCoding) extension doesn't work consistently with `@if` statements.

For this reason, I have added the `If` component. Usage is pretty simple. Assume `_n` is an `int` variable...

```xml
<If>
  <Then Condition="@(_n > 10)">
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

### ForEach
In the same spirit as the above, the `ForEach` component allows you to replace usages of `@foreach` with a component...

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

To make this quicker and easier, I added the `DumpCollection` component to do this...

```html
<DumpCollection Collection="SomeCollection" Display="@(t => $"({t.Id})  {t.Name})" />
```

The component has two extra paramters, `UlClass` and `LiClass` that allow you to pass in CSS classes for the `<ul>` element and the `<li>` elements.

## Extensions

### Persistent state and caching helper
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

### TryGetQueryString()
Documentation coming soon...

## Forms
A set of components for laying out forms.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Test) to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project, but should hopefully be expanded to include sample usage of the components.
