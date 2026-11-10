# Tooltip

>Part of [Pixata.Blazor](Readme.md)

`PixataTooltip` shows a small panel next to an element when the pointer is over it, when it has the keyboard focus, or when it's tapped on a touch screen. The panel can hold any markup, so it can show more than the browser's own tooltip can, and it can be shown on a click instead. One tooltip can serve many elements, picked out by a CSS selector, and show something different for each. It has the same main parameters as Telerik's Tooltip, so markup can usually move across by changing the component's name.

See the [live demo](https://test.pixata.co.uk/TooltipSample) for how it looks, and the [source for that page](https://github.com/MrYossu/Pixata.Utilities/blob/master/Pixata.Blazor.Sample/Pages/TooltipSample.razor) for the code.

## Setup

There is nothing to register. Add the stylesheet to your host page if you haven't already (see [Styling](Readme.Styling.md))...

```xml
<link rel="stylesheet" href="_content/Pixata.Blazor/pixata.css" />
```

...and add `@using Pixata.Blazor.Components` to your `_Imports.razor`. The tooltip's script is loaded by the component when it's first used, so there's nothing to add for that either.

## Basic use

Put the element the tooltip is for inside it. With no `Template` or `Text`, the tooltip shows the element's `title`...

```xml
<PixataTooltip>
  <PixataButton Icon="PixataSvgIcons.Refresh" AriaLabel="Refresh" Title="Get the latest figures" OnClick="Refresh" />
</PixataTooltip>
```

The `title` is taken off the element while the tooltip is shown, so the browser doesn't show its own tooltip as well, and put back when it goes. A target without a `title` gets no tooltip, unless the tooltip has a `Template` or `Text` of its own. `Text` sets what it says...

```xml
<PixataTooltip Text="Saves the invoice and emails it to the customer">
  <PixataPrimaryButton OnClick="SaveAndSend">Save and send</PixataPrimaryButton>
</PixataTooltip>
```

The child content is wrapped in a `<span>` with `display: contents`, so it's laid out just as it would be without the tooltip.

## Many targets

Give the tooltip a `TargetSelector`, which is a CSS selector, and it's shown for each element on the page that matches it, including ones added later. The tooltip can then go anywhere on the page, and needs no child content...

```xml
@foreach (Invoice invoice in _invoices) {
  <span class="overdue" title="@($"{invoice.DaysOverdue} days overdue")">@invoice.Number</span>
}

<PixataTooltip TargetSelector=".overdue" />
```

With child content as well, only the elements inside it that match are targets. This gives every button in a toolbar a tooltip from its `title`...

```xml
<PixataTooltip TargetSelector="[title]" Position="PixataTooltipPosition.Bottom" ShowDelay="0">
  <div class="toolbar">
    <PixataButton Icon="PixataSvgIcons.Download" AriaLabel="Download" Title="Download as a spreadsheet" />
    <PixataButton Icon="PixataSvgIcons.Refresh" AriaLabel="Refresh" Title="Refresh" />
  </div>
</PixataTooltip>
```

## Templates

`Template` can be any markup. Its context, a `PixataTooltipTemplateContext`, is the element the tooltip is being shown for...

| Property | Type | Description |
| --- | --- | --- |
| `TargetId` | `string?` | The element's id, if it has one |
| `Title` | `string?` | The element's `title` |
| `DataAttributes` | `Dictionary<string, string>` | The element's `data-` attributes, without the "data-", so `data-invoice-id` is `"invoice-id"` |

So one tooltip can show something different for each of its targets. Put what it needs to know about each target in a `data-` attribute...

```xml
@foreach (Invoice invoice in _invoices) {
  <span class="invoice-status" tabindex="0" data-invoice="@invoice.Number">...</span>
}

<PixataTooltip TargetSelector=".invoice-status" Position="PixataTooltipPosition.Right">
  <Template>
    @if (_invoices.FirstOrDefault(i => i.Number == context.DataAttributes.GetValueOrDefault("invoice")) is Invoice invoice) {
      <strong>@invoice.Number</strong>
      <div>Due on @invoice.Due.ToString("d MMM yyyy")</div>
    }
  </Template>
</PixataTooltip>
```

For child content without a `TargetSelector`, the context is for the child element that the pointer is over or that has the focus.

## Showing and hiding

By default (`ShowOn="PixataTooltipShowEvent.Hover"`), the tooltip shows...

- when the pointer has been over the target for `ShowDelay` milliseconds (100 by default)
- straight away when the target gets the keyboard focus, so it works from the keyboard as well. Give a target that isn't a button, link or input `tabindex="0"`, so it can be tabbed to. The focus a button gets when it's clicked doesn't show the tooltip, as the pointer being over it has already done that
- straight away when the target is tapped on a touch screen

It goes `HideDelay` milliseconds (300 by default) after the pointer leaves the target, which gives time to move the pointer onto the tooltip (to select its text, say), where it stays. It stays while the target has the keyboard focus, and goes when it loses it. It also goes when the screen is tapped again, or when Escape is pressed.

With `ShowOn="PixataTooltipShowEvent.Click"`, a click (or Enter or Space) on the target shows the tooltip, and another click, a click elsewhere or Escape hides it. As it stays until then, it can hold links and buttons...

```xml
<PixataTooltip ShowOn="PixataTooltipShowEvent.Click" Position="PixataTooltipPosition.Right">
  <Template>
    Nine digits starting with GB. <a href="https://www.gov.uk/check-uk-vat-number" target="_blank">Check a VAT number</a>
  </Template>
  <ChildContent>
    <span role="button" tabindex="0" aria-label="About VAT numbers">?</span>
  </ChildContent>
</PixataTooltip>
```

When the pointer moves from one tooltip's target to another's, the first tooltip goes straight away, rather than after its `HideDelay`. `Hide()` hides the tooltip from code, and `IsShown` says whether it's shown.

## Position

`Position` says which side of the target the tooltip goes on...

| `Position` | Where it goes |
| --- | --- |
| `Top` | Above the target (the default) |
| `Bottom` | Below the target |
| `Left` | To the left of the target |
| `Right` | To the right of the target |
| `Auto` | The first side with room for it, trying above, below, right and then left |

If there isn't room on the side asked for, the tooltip goes on the opposite side. It's kept inside the window, with its arrow still pointing at the target, and it follows the target as the page scrolls. It's `position: fixed`, so it isn't cut off by a container with `overflow: hidden` or a scroll bar.

## Accessibility

While the tooltip is shown, it has `role="tooltip"`, and the target's `aria-describedby` points to it, so screen readers read it after the target's name. Any `aria-describedby` the target already had is kept, and put back as it was when the tooltip goes. The tooltip shows when the target has the keyboard focus as well as when the pointer is over it, Escape hides it, and it stays while the pointer is over it or the target has the focus, as WCAG asks of content that's shown on hover or focus.

## Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `ChildContent` | `RenderFragment?` | The target, or with a `TargetSelector`, the content the targets are in |
| `TargetSelector` | `string?` | A CSS selector for the targets. Without child content, any element on the page that matches is a target |
| `Template` | `RenderFragment<PixataTooltipTemplateContext>?` | What the tooltip shows. Its context is the target's id, title and `data-` attributes |
| `Text` | `string?` | What the tooltip shows, if it has no `Template`. Without either, it shows the target's `title` |
| `Position` | `PixataTooltipPosition` | `Top` (the default), `Bottom`, `Left`, `Right` or `Auto` |
| `ShowOn` | `PixataTooltipShowEvent` | `Hover` (the default), which includes the focus and a tap, or `Click` |
| `ShowDelay` | `int` | How many milliseconds the pointer has to be over a target before the tooltip shows. Defaults to 100 |
| `HideDelay` | `int` | How many milliseconds the tooltip stays after the pointer leaves the target. Defaults to 300 |
| `Width` | `string?` | The tooltip's width, as a CSS width. By default it's as wide as its content, up to `--pixata-blazor-tooltip-max-width` |
| `Height` | `string?` | The tooltip's height, as a CSS height. By default it's as tall as its content. Taller content scrolls |
| `Class` | `string?` | Extra classes for the tooltip |
| `Id` | `string?` | The tooltip's id, which the target's `aria-describedby` points to. One is made up if you don't give one |

A tooltip needs a `TargetSelector` or child content, and throws an `InvalidOperationException` if it has neither.

## Styling

The tooltip is styled by these custom properties, which you can set on `:root` or, with `Class`, on a particular tooltip...

| Property | Default |
| --- | --- |
| `--pixata-blazor-tooltip-bg` | `--pixata-blazor-dark`, a near black |
| `--pixata-blazor-tooltip-color` | `--pixata-blazor-dark-color`, white |
| `--pixata-blazor-tooltip-font-size` | `--pixata-blazor-font-size-sm` |
| `--pixata-blazor-tooltip-padding` | `.25rem .5rem` |
| `--pixata-blazor-tooltip-max-width` | `20rem` |
| `--pixata-blazor-tooltip-border-radius` | `--pixata-blazor-border-radius` |
| `--pixata-blazor-tooltip-shadow` | `--pixata-blazor-popup-shadow` |
| `--pixata-blazor-tooltip-z-index` | One more than `--pixata-blazor-popup-z-index`, so it shows over drop-downs and dialogs |
| `--pixata-blazor-tooltip-arrow-size` | `6px`. Set it to `0` for no arrow |
| `--pixata-blazor-tooltip-gap` | `8px`, the space between the target and the tooltip |

For a light tooltip, for example...

```css
.light-tooltip {
  --pixata-blazor-tooltip-bg: #fff;
  --pixata-blazor-tooltip-color: var(--pixata-blazor-text-color);
}
```

The elements have these classes...

| Class | Element |
| --- | --- |
| `pixata-blazor-tooltip` | The tooltip, with `data-position` set to the side it's on (`top`, `bottom`, `left` or `right`). Its `::before` is the arrow |
| `pixata-blazor-tooltip-content` | What the tooltip shows, inside it |
| `pixata-blazor-tooltip-targets` | The `<span>` the child content is wrapped in |
