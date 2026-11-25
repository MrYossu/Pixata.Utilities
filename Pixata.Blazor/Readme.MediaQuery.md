# Media query

>Part of [Pixata.Blazor](Readme.md)

`PixataMediaQuery` tells your code whether a CSS media query matches, such as `(max-width: 720px)`, and tells it again whenever that changes, such as when the user resizes the window or turns their phone round. Use it to change what a page shows below a breakpoint, such as showing a grid with fewer columns on a phone. It renders nothing. It has the same parameters as Telerik's MediaQuery, so markup moves across by changing the component's name.

See the [live demo](https://test.pixata.co.uk/MediaQuerySample) for how it works, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/MediaQuerySample.razor) for the code.

## Setup

There is nothing to register, and no stylesheet is needed. Add `@using Pixata.Blazor.Components` to your `_Imports.razor` if you haven't already. The component uses the package's shared script, which it loads itself.

## Basic use

```razor
<PixataMediaQuery Media="(max-width: 720px)" OnChange="@(matches => _phone = matches)" />

@if (_phone) {
  <PixataGrid Data="_jobs">...the few columns that fit on a phone...</PixataGrid>
} else {
  <PixataGrid Data="_jobs">...all the columns...</PixataGrid>
}

@code {
  private bool _phone;
}
```

`OnChange` is raised with whether the query matches once the page is interactive, and again whenever that changes. Each query needs a component of its own, so use two for two breakpoints.

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `Media` | `string` | The CSS media query, such as `(max-width: 720px)`, `(orientation: portrait)` or `(prefers-color-scheme: dark)`. Required. Changing it stops watching the old query, starts watching the new one, and raises `OnChange` with whether that matches |
| `OnChange` | `EventCallback<bool>` | Raised with whether the query matches, once the page is interactive and whenever that changes |

## Prerendering and Blazor Server

While a page is being prerendered there is no browser to ask, so `OnChange` isn't raised until the page is interactive. Until then, the page shows whatever your field starts as, so start it as the layout most of your users see, to avoid the page changing as it becomes interactive.

The component works in Blazor Server, WebAssembly and Auto. In Blazor Server, each change is a message to the server, but the browser only sends one when the query starts or stops matching, not as the window is resized. If the user closes the page, nothing is thrown. The browser stops watching the query when the component is disposed.

If all you need is to style the page differently below a breakpoint, a media query in your CSS is simpler, and works while prerendering. Use this component when the markup, or your code, needs to change.
