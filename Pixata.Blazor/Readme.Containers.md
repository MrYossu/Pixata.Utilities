# Containers

>Part of [Pixata.Blazor](Readme.md)

These components are intended to wrap up other parts of your page, and add functionality.

## HtmlRaw

Convenience component for displaying raw HTML. Instead of doing this...

    @((MarkupString)_html)

...where `_html` is a string variable in your code, you can now do...

    <HtmlRaw Html="@_html" />

...which is (for me anyway) slightly easier to remember.

## Busy

Useful when data is loading. You bind the `Data` parameter to whatever model you are using. When the page first loads, and the model is (presumably) null, a busy indicator will show. When the data has loaded, and the model is non-null, the display is automatically switched to the real content.

Sample usage...

```xml
<Busy Data="_avreich">
  <!-- HTML and other Blazor components go here... -->
</Busy>
```

By default, the message "Loading..." is displayed while the data is loading, but you can override that by setting the `Message` parameter.

You can also set the class for the container, in case you want to add your own styling, and set the classes for the spinner and spinner colour. By default, the component uses the `pixata-spinner` class from this package's stylesheet (see [Styling](Readme.Styling.md)), but you can override this to use something else if you want.

## Loader and Loader2

Two full-page loading indicators, for when `Busy` is too plain. `Loader` draws a spinning circle with a caption, and `Loader2` draws a two-tone spinner. Both are pure CSS, with no images or JavaScript.

`Loader` also takes an optional `Data` parameter and `ChildContent`, so it can be used the same way as `Busy` - it shows the spinner while `Data` is null, and your content once it isn't.

Everything about them is parameterised, so you can size and colour them to suit your app...

| Parameter | `Loader` default | What it does |
| --- | --- | --- |
| `Text` | `"Loading..."` | The caption under the spinner |
| `CircleSize` | `"100px"` | Diameter of the spinning circle |
| `BorderSize` | `"10px"` | Thickness of the circle |
| `CircleColour` | `var(--loader-colour, ...)` | Colour of the circle |
| `TextSize`, `TextColour` | `"1.5rem"`, `inherit` | Font size and colour of the caption |
| `MinHeight` | `"200px"` | Minimum height of the container, so the page doesn't jump when the content arrives |
| `Horizontal` | `false` | Put the caption beside the spinner rather than under it |
| `CssClass` | | Extra classes for the container |

`Loader2` takes `IconSize`, `BorderWidth`, `PrimaryColour`, `SecondaryColour`, `Text`, `TextFontSize` and `TextColour`.

The colours default to CSS custom properties (`--loader-colour`, `--loader-primary-colour` and `--loader-secondary-colour`), so you can set them once in your site's CSS rather than on every usage.

`Loader` is what an [`ApiResponseView`](Readme.ApiResponseView.md) shows by default while its data is loading.

## Confirm

Replaces the nasty JavaScript `confirm` function with something that looks nicer, and doesn't require any JSInterop.

See the [sample code](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/ConfirmSample.razor) ([live demo](https://test.pixata.co.uk/ConfirmSample)) for an example of how to use it. You can set the pop-up to disable the entire window, or just one section of it. You can also specify if the pop-up should disappear as soon as a button is clicked, or if it should remain visible, but disabled (with a busy indicator) until you dismiss it.

## Inform

Similar to `Confirm`, but only has one button. At the moment, the pop-up is dismissed as soon as you click the button, but I intend to add the feature described above to this component as well.

## MessageView

Displays a message in a coloured alert box, with an optional close button. Used by [`ApiResponseView`](Readme.ApiResponseView.md) to show the error from a failed `ApiResponse`, but it is perfectly usable on its own.

```xml
<MessageView Message="@_message" Error="false" />
```

| Parameter | Default | What it does |
| --- | --- | --- |
| `Message` | `""` | The message to show. Nothing is rendered when this is empty, so you can bind it to a variable and leave the component in the markup |
| `Error` | `true` | `true` styles it as a danger alert, `false` as a success one |
| `ShowCloseButton` | `true` | Whether to show the button that clears the message |
| `Class` | `""` | Extra CSS classes for the alert |

The message is rendered as raw HTML, so you can include markup in it. Don't pass anything a user typed without sanitising it first.

## Expander

Wraps a chunk of markup and shows or hides it. Set `Expanded` to say what state it starts in, then call its `Toggle()` method to switch between the two...

```xml
<button @onclick="() => _details.Toggle()">Show or hide the details</button>

<Expander @ref="_details" Expanded="false">
  <p>The gory details go here</p>
</Expander>
```

```csharp
private Expander _details = null!;
```

Note that the content is always rendered, it is just hidden with `display: none`, so this isn't the component to use for something expensive.

## DumpCollection

OK, so this isn't strictly a container, but it's close enough to put here.

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

The component has two extra parameters, `UlClass` and `LiClass`, that allow you to pass in CSS classes for the `<ul>` element and the `<li>` elements.
