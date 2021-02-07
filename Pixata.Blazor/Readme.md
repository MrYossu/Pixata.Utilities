# Pixata.Blazor [![Pixata.Blazor Nuget package](https://img.shields.io/nuget/v/Pixata.Blazor)](https://www.nuget.org/packages/Pixata.Blazor/)

![Pixata](https://github.com/MrYossu/Pixata.Utilities/raw/master/Pixata.Blazor/Icon/avion.png "Pixata") 

I have [blogged about some Blazor components I've been writing](https://www.pixata.co.uk/tag/blazor/). This project contains the source for those components.

There is a [complimentary package](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.TelerikComponents), which contains additional components for those who have a subscription to Telerik.

A [Nuget package](https://www.nuget.org/packages/Pixata.Blazor/) is available for this project.

## Containers
These components are intended to wrap up other parts of your page, and add functionality.

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

### HtmlRaw
Convenience component for displaying raw HTML. Instead of doing this...

    @((MarkupString)_html)

...where `_html` is a string variable in your code, you can now do...

    <HtmlRaw Html="@_html" />

...which is (for me anyway) slightly easier to remember.

### Inform
Similar to `Confirm`, but only has one button. At the moment, the pop-up id dismissed as soon as you click the button, but I intned to add the feature described above to this component as well.

## Extensions

### TryGetQueryString()
Documentation coming soon...

## Forms
A set of components for laying out forms.

## Sample project
I have added a [Blazor web project](https://github.com/MrYossu/Pixata.Utilities/tree/master/Pixata.Blazor.Test) to the repository, and intend to use that to try out and demonstrate the components. At the moment, it's a just-out-of-the-box template project, but should hopefully be expanded to include sample usage of the components.
